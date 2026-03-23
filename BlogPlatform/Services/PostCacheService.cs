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
    // Add 1 to the user's comment counter and set expiry of 1 hour
    // Each user gets a fresh limit every hour
    public async Task<long> IncrementCommentCountAsync(string userId)
    {
    var key = $"comment-count:{userId}";
    var count = await _redis.StringIncrementAsync(key);
    // Set expiry only on first comment — resets the window after 1 hour
    if (count == 1)
        await _redis.KeyExpireAsync(key, TimeSpan.FromHours(1));
    return count;
    }

    // Check if user is still within the limit
    public async Task<bool> IsWithinRateLimitAsync(string userId)
    {
    var key = $"comment-count:{userId}";
    var count = await _redis.StringGetAsync(key);
    // If no key exists yet → first comment → allowed
    if (!count.HasValue) return true;
    return (long)count <= 10;
    }
}