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

    public CommentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CommentDto?> CreateAsync(CreateCommentRequest request, string ipAddress, string userAgent)
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

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return ToDto(comment);
    }

    public async Task<PagedResult<CommentDto>> GetPageAsync(CommentQuery query)
    {
        var topLevel = _db.Comments
            .AsNoTracking()
            .Where(c => c.ParentId == null);

        var totalCount = await topLevel.CountAsync();

        var comments = await ApplySorting(topLevel, query)
            .Skip((query.Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return new PagedResult<CommentDto>
        {
            Items = comments.Select(ToDto).ToList(),
            Page = query.Page,
            PageSize = PageSize,
            TotalCount = totalCount
        };
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
            CreatedAt = comment.CreatedAt
        };
    }
}
