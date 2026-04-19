using StackExchange.Redis;
using System.Text.Json;

// PostSearchService handles full-text search over blog posts using Redis. (Week 12 Task 03)
//
// Pattern used: Write-Through
//   When a post is created in MongoDB, it is ALSO indexed in Redis at the same time.
//   This keeps Redis in sync with MongoDB without a separate sync job.
//   Key format: "post:{postId}" (e.g. "post:abc123")
//
// How search works:
//   SearchAsync scans all "post:*" keys in Redis, deserializes each post,
//   and returns those whose Title or Body contains the search query (case-insensitive).
//   Results are capped at 10.
//
// IMPORTANT — Known limitation vs the course spec:
//   The assignment recommended using Redis's FT (RediSearch) module with a proper index
//   (FT.CREATE, FT.SEARCH) for production-quality full-text search.
//   This implementation uses a simpler KEYS scan + in-memory string filter instead.
//   It works correctly for small datasets but does NOT scale — scanning all keys is O(N)
//   and will slow down as the number of posts grows.
//   Also, Update and Delete do NOT currently remove/update the "post:*" entry —
//   only Create indexes. That means search can return stale post data after an update.
//   These are the two main things to improve in the next iteration.
public class PostSearchService
{
    private readonly IDatabase _redis;

    public PostSearchService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    // Index a post into Redis when it is created (write-through).
    // Called in MongoPostRepository.Create immediately after inserting into MongoDB.
    // Stores the full serialized post JSON at key "post:{id}".
    public async Task IndexPostAsync(Post post)
    {
        var key = $"post:{post.Id}";
        await _redis.StringSetAsync(key, JsonSerializer.Serialize(post));
        // Note: no TTL set here — search index entries persist until manually removed.
        // Currently there is no cleanup on Update or Delete, so stale entries can exist.
    }

    // Search posts by keyword — scans all "post:*" keys and filters in memory.
    // Returns up to 10 matching posts.
    //
    // How it works:
    //   1. GetServer().KeysAsync("post:*") returns all keys with that prefix.
    //   2. For each key, we fetch the JSON and deserialize it.
    //   3. We check if Title or Body contains the query string (case-insensitive).
    //   4. We return the first 10 matches.
    //
    // Limitation: KeysAsync is a full key scan — fine for development, slow at scale.
    // The production approach is to use Redis FT.SEARCH with a proper index.
    public async Task<List<Post>> SearchAsync(string query)
    {
        var server = _redis.Multiplexer.GetServer(
            _redis.Multiplexer.GetEndPoints().First());

        var results = new List<Post>();

        await foreach (var key in server.KeysAsync(pattern: "post:*"))
        {
            var value = await _redis.StringGetAsync(key);
            if (!value.HasValue) continue;

            var post = JsonSerializer.Deserialize<Post>(value.ToString());
            if (post is null) continue;

            // Case-insensitive match against title or body
            if (post.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                post.Body.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(post);
            }
        }

        return results.Take(10).ToList();
    }
}
