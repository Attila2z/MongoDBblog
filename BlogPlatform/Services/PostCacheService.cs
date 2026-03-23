using StackExchange.Redis;
using System.Text.Json;

public class PostCacheService
{
    private readonly IDatabase _redis;

    public PostCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    // Check Redis for a post — returns null if not cached
    public async Task<Post?> GetPostAsync(string postId)
    {
        var cached = await _redis.StringGetAsync(postId);
        return cached.HasValue
            ? JsonSerializer.Deserialize<Post>(cached.ToString())
            : null;
    }

    // Save post to Redis with 5 minute expiry
    public async Task SetPostAsync(Post post)
    {
        var serialized = JsonSerializer.Serialize(post);
        await _redis.StringSetAsync(
            post.Id,
            serialized,
            TimeSpan.FromMinutes(5));
    }

    // Delete post from Redis when updated
    public async Task InvalidatePostAsync(string postId)
    {
        await _redis.KeyDeleteAsync(postId);
    }
}