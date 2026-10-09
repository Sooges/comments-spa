using CommentsApi.Dtos;

namespace CommentsApi.Services;

public interface ICommentService
{
    /// <summary>
    /// Saves a new comment. Returns null if the parent comment does not exist.
    /// </summary>
    Task<CommentDto?> CreateAsync(CreateCommentRequest request, string ipAddress, string userAgent);

    /// <summary>
    /// Returns one page of top-level comments.
    /// </summary>
    Task<PagedResult<CommentDto>> GetPageAsync(CommentQuery query);
}
