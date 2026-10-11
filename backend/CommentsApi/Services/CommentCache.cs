using CommentsApi.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace CommentsApi.Services;

public class CommentCache : ICommentCache
{
    // a page is also removed after some time, even if nobody added a comment
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;

    // It is a part of every key. When the number changes, all the old pages can not be found any more
    // (they are removed from the memory when their time is over).
    private int _version;

    public CommentCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<PagedResult<CommentDto>> GetOrCreateAsync(
        CommentQuery query,
        Func<Task<PagedResult<CommentDto>>> load)
    {
        // The key is made before the loading. If a comment is added while the page is loading,
        // the old page is saved under the old version and nobody will take it.
        var key = GetKey(query);

        if (_cache.TryGetValue(key, out PagedResult<CommentDto>? cached) && cached != null)
        {
            return cached;
        }

        var page = await load();

        // The pages after the last one are empty and there can be a lot of them (any page number is allowed),
        // so they are not kept in the memory.
        if (query.Page <= Math.Max(page.TotalPages, 1))
        {
            _cache.Set(key, page, Lifetime);
        }

        return page;
    }

    public void Clear()
    {
        Interlocked.Increment(ref _version);
    }

    private string GetKey(CommentQuery query)
    {
        return $"comments:{Volatile.Read(ref _version)}:{query.SortBy}:{query.SortOrder}:{query.Page}";
    }
}
