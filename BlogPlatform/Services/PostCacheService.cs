using StackExchange.Redis;
using System.Text.Json;

// PostCacheService handles two Redis responsibilities:
//
// 1. POST CACHING (Week 12 Task 01)
//    Individual posts are cached in Redis to reduce repeated reads from MongoDB.
//    Pattern used: Cache-Aside (Lazy Loading)
//      - On read: check Redis first; on miss, fetch from MongoDB and populate Redis.
//      - On update: invalidate (delete) the cached entry so stale data isn't served.
//    Posts are stored as serialized JSON strings, keyed by their Id (e.g. "abc123").
//    TTL: 5 minutes — after that Redis evicts the entry and the next read re-populates.
//
// 2. COMMENT RATE LIMITING (Week 12 Task 02)
//    Prevents spam by tracking how many comments each user submits per hour.
//    Pattern: Redis counter with expiry.
//      - Key: "comment-count:{userId}"
//      - Increment on every comment; set 1-hour expiry on first increment (resets hourly).
//      - Reject the comment if count > 10.
//    This check happens in PostController.AddComment before the comment is written.
public class PostCacheService
{
    private readonly IDatabase _redis;

    // IConnectionMultiplexer is the StackExchange.Redis entry point.
    // It is registered as a singleton in Program.cs — one shared connection for the whole app.
    public PostCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    // ── CACHING ──────────────────────────────────────────────────────────────────

    // Try to read a post from Redis. Returns null if it's not cached (cache miss).
    // Called in MongoPostRepository.GetById before hitting MongoDB.
    public async Task<Post?> GetPostAsync(string postId)
    {
        var cached = await _redis.StringGetAsync(postId);
        return cached.HasValue
            ? JsonSerializer.Deserialize<Post>(cached.ToString())
            : null;
    }

    // Store a post in Redis as a JSON string with a 5-minute TTL.
    // Called after a cache miss — populates the cache for future reads.
    public async Task SetPostAsync(Post post)
    {
        var serialized = JsonSerializer.Serialize(post);
        await _redis.StringSetAsync(
            post.Id,
            serialized,
            TimeSpan.FromMinutes(5));  // auto-evicted after 5 minutes
    }

    // Remove a post from Redis when it is updated.
    // Without this, the cache would serve the old (stale) version until the TTL expires.
    public async Task InvalidatePostAsync(string postId)
    {
        await _redis.KeyDeleteAsync(postId);
    }

    // ── RATE LIMITING ────────────────────────────────────────────────────────────

    // Increment the comment counter for a user and return the new count.
    // The expiry is set only on the FIRST increment (count == 1), which means
    // the 1-hour window starts on the first comment and resets after an hour.
    // Called in PostController.AddComment AFTER the rate limit check passes.
    public async Task<long> IncrementCommentCountAsync(string userId)
    {
        var key = $"comment-count:{userId}";
        var count = await _redis.StringIncrementAsync(key);

        // Only set expiry on the first comment — this starts the 1-hour sliding window.
        if (count == 1)
            await _redis.KeyExpireAsync(key, TimeSpan.FromHours(1));

        return count;
    }

    // Check whether a user is still within the 10 comments/hour limit.
    // Returns true (allowed) if the key doesn't exist yet (first ever comment) or count <= 10.
    // Called in PostController.AddComment BEFORE writing the comment.
    //
    // Known limitation: AuthorName is used as the userId here, but AuthorName is a plain
    // string supplied by the client — anyone can change it to bypass the limit.
    // In production this should use an authenticated user id from a JWT token or session.
    public async Task<bool> IsWithinRateLimitAsync(string userId)
    {
        var key = $"comment-count:{userId}";
        var count = await _redis.StringGetAsync(key);

        if (!count.HasValue) return true;  // no key yet = first comment = allowed
        return (long)count <= 10;
    }
}
