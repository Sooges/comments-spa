using CommentsApi.Dtos;

namespace CommentsApi.Services;

public interface ICommentService
{
    /// <summary>
    /// Saves a new comment. Returns null if the parent comment does not exist.
    /// </summary>
    Task<CommentDto?> CreateAsync(CreateCommentRequest request, string ipAddress, string userAgent);
}
