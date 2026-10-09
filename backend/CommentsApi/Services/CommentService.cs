using CommentsApi.Data;
using CommentsApi.Dtos;
using CommentsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentsApi.Services;

public class CommentService : ICommentService
{
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
