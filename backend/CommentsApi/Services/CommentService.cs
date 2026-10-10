using CommentsApi.Data;
using CommentsApi.Dtos;
using CommentsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentsApi.Services;

public class CommentService : ICommentService
{
    private const int PageSize = 25;
    private const int MaxUserAgentLength = 512;

    private readonly AppDbContext _db;
    private readonly IFileStorage _fileStorage;

    public CommentService(AppDbContext db, IFileStorage fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    public async Task<CommentDto?> CreateAsync(
        CreateCommentRequest request,
        PreparedAttachment? attachment,
        string ipAddress,
        string userAgent)
    {
        if (request.ParentId != null)
        {
            var parentExists = await _db.Comments.AnyAsync(c => c.Id == request.ParentId);
            if (!parentExists)
            {
                return null;
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

        return ToDto(comment);
    }

    public async Task<PagedResult<CommentDto>> GetPageAsync(CommentQuery query)
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
