using CommentsApi.Dtos;

namespace CommentsApi.Services;

public interface ICommentService
{
    /// <summary>
    /// Saves a new comment. The result has an error if the parent comment does not exist
    /// or the replies are already too deep.
    /// </summary>
    Task<CreateCommentResult> CreateAsync(
        CreateCommentRequest request,
        PreparedAttachment? attachment,
        string ipAddress,
        string userAgent);

    /// <summary>
    /// Returns one page of top-level comments.
    /// </summary>
    Task<PagedResult<CommentDto>> GetPageAsync(CommentQuery query);
}
