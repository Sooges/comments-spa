using CommentsApi.Data;
using CommentsApi.Dtos;
using CommentsApi.Events;
using CommentsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentsApi.Services;

public class CommentService : ICommentService
{
    private const int PageSize = 25;

    // Replies can be nested very deep, but not without a limit: the whole tree is sent to the browser
    // as one JSON, and a too deep tree can not be written (see MaxDepth in Program.cs).
    private const int MaxReplyDepth = 100;
    private const int MaxUserAgentLength = 512;

    private readonly AppDbContext _db;
    private readonly IFileStorage _fileStorage;
    private readonly ICommentEvents _events;
    private readonly ICommentCache _cache;

    public CommentService(AppDbContext db, IFileStorage fileStorage, ICommentEvents events, ICommentCache cache)
    {
        _db = db;
        _fileStorage = fileStorage;
        _events = events;
        _cache = cache;
    }

    public async Task<CreateCommentResult> CreateAsync(
        CreateCommentRequest request,
        PreparedAttachment? attachment,
        string ipAddress,
        string userAgent)
    {
        if (request.ParentId != null)
        {
            var parentDepth = await GetDepthAsync(request.ParentId.Value);
            if (parentDepth < 0)
            {
                return CreateCommentResult.Failed(CreateCommentError.ParentNotFound);
            }

            if (parentDepth >= MaxReplyDepth)
            {
                return CreateCommentResult.Failed(CreateCommentError.TooDeep);
            }
        }

        var comment = new Comment
        {
            ParentId = request.ParentId,
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            HomePage = string.IsNullOrWhiteSpace(request.HomePage) ? null : request.HomePage.Trim(),
            Text = request.Text.Trim(),
            CreatedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent.Length > MaxUserAgentLength ? userAgent[..MaxUserAgentLength] : userAgent
        };

        string? storedFileName = null;
        if (attachment != null)
        {
            storedFileName = await _fileStorage.SaveAsync(attachment.Content, attachment.Extension);

            comment.Attachment = new Attachment
            {
                Kind = attachment.Kind,
                FileName = attachment.OriginalName,
                StoredFileName = storedFileName,
                ContentType = attachment.ContentType,
                Size = attachment.Content.Length,
                Width = attachment.Width,
                Height = attachment.Height
            };
        }

        _db.Comments.Add(comment);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            // do not leave a file without a comment
            if (storedFileName != null)
            {
                _fileStorage.Delete(storedFileName);
            }

            throw;
        }

        _events.RaiseCommentCreated(comment.Id, comment.ParentId);

        return CreateCommentResult.Created(ToDto(comment));
    }

    // How deep the comment is: 0 for a top-level comment, 1 for a reply to it and so on.
    // Returns -1 if there is no such comment.
    private async Task<int> GetDepthAsync(int commentId)
    {
        var depth = 0;
        int? currentId = commentId;

        // we go up to the top-level comment, one query for every step
        while (true)
        {
            var parentIds = await _db.Comments
                .AsNoTracking()
                .Where(c => c.Id == currentId)
                .Select(c => new { c.ParentId })
                .ToListAsync();

            if (parentIds.Count == 0)
            {
                return -1;
            }

            if (parentIds[0].ParentId == null || depth >= MaxReplyDepth)
            {
                return depth;
            }

            depth++;
            currentId = parentIds[0].ParentId;
        }
    }

    public Task<PagedResult<CommentDto>> GetPageAsync(CommentQuery query)
    {
        return _cache.GetOrCreateAsync(query, () => LoadPageAsync(query));
    }

    private async Task<PagedResult<CommentDto>> LoadPageAsync(CommentQuery query)
    {
        var topLevel = _db.Comments
            .AsNoTracking()
            .Where(c => c.ParentId == null);

        var totalCount = await topLevel.CountAsync();

        var comments = await ApplySorting(topLevel, query)
            .Include(c => c.Attachment)
            .Skip((query.Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var items = comments.Select(ToDto).ToList();
        await LoadRepliesAsync(items);

        return new PagedResult<CommentDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = PageSize,
            TotalCount = totalCount
        };
    }

    // Loads the replies level by level: one query for every nesting level
    private async Task LoadRepliesAsync(List<CommentDto> parents)
    {
        var currentLevel = parents;

        while (currentLevel.Count > 0)
        {
            var parentIds = currentLevel.Select(c => c.Id).ToList();

            var replies = await _db.Comments
                .AsNoTracking()
                .Include(c => c.Attachment)
                .Where(c => c.ParentId != null && parentIds.Contains(c.ParentId.Value))
                .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
                .ToListAsync();

            var parentsById = currentLevel.ToDictionary(c => c.Id);
            var nextLevel = new List<CommentDto>();

            foreach (var reply in replies)
            {
                var dto = ToDto(reply);
                parentsById[reply.ParentId!.Value].Replies.Add(dto);
                nextLevel.Add(dto);
            }

            currentLevel = nextLevel;
        }
    }

    private static IQueryable<Comment> ApplySorting(IQueryable<Comment> comments, CommentQuery query)
    {
        var desc = query.SortOrder == SortDirection.Desc;

        // Id is the second sort key, so the order stays the same between pages
        return query.SortBy switch
        {
            CommentSortField.UserName => desc
                ? comments.OrderByDescending(c => c.UserName).ThenByDescending(c => c.Id)
                : comments.OrderBy(c => c.UserName).ThenBy(c => c.Id),
            CommentSortField.Email => desc
                ? comments.OrderByDescending(c => c.Email).ThenByDescending(c => c.Id)
                : comments.OrderBy(c => c.Email).ThenBy(c => c.Id),
            _ => desc
                ? comments.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
                : comments.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
        };
    }

    private static CommentDto ToDto(Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            ParentId = comment.ParentId,
            UserName = comment.UserName,
            Email = comment.Email,
            HomePage = comment.HomePage,
            Text = comment.Text,
            CreatedAt = comment.CreatedAt,
            Attachment = comment.Attachment == null ? null : ToDto(comment.Attachment)
        };
    }

    private static AttachmentDto ToDto(Attachment attachment)
    {
        return new AttachmentDto
        {
            Id = attachment.Id,
            Kind = attachment.Kind.ToString().ToLowerInvariant(),
            FileName = attachment.FileName,
            Size = attachment.Size,
            Width = attachment.Width,
            Height = attachment.Height
        };
    }
}
