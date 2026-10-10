using CommentsApi.Dtos;

namespace CommentsApi.Services;

public interface ICommentCache
{
    /// <summary>
    /// Returns the page from the cache, or loads it and puts it into the cache.
    /// </summary>
    Task<PagedResult<CommentDto>> GetOrCreateAsync(CommentQuery query, Func<Task<PagedResult<CommentDto>>> load);

    /// <summary>
    /// Removes all the pages, it is called when the comments change.
    /// </summary>
    void Clear();
}
