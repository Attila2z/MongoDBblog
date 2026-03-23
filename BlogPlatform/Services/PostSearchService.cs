using StackExchange.Redis;
using System.Text.Json;

public class PostSearchService
{
    private readonly IDatabase _redis;

    public PostSearchService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    // Save post to Redis when created — write-through pattern
    public async Task IndexPostAsync(Post post)
    {
        var key = $"post:{post.Id}";
        await _redis.StringSetAsync(key, JsonSerializer.Serialize(post));
    }

    // Search posts by keyword in title or body
    public async Task<List<Post>> SearchAsync(string query)
    {
        var server = _redis.Multiplexer.GetServer(
            _redis.Multiplexer.GetEndPoints().First());

        var results = new List<Post>();

        // Scan all post keys and filter by keyword
        await foreach (var key in server.KeysAsync(pattern: "post:*"))
        {
            var value = await _redis.StringGetAsync(key);
            if (!value.HasValue) continue;

            var post = JsonSerializer.Deserialize<Post>(value.ToString());
            if (post is null) continue;

            // Check if title or body contains the search query
            if (post.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                post.Body.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(post);
            }
        }

        return results.Take(10).ToList();
    }
}