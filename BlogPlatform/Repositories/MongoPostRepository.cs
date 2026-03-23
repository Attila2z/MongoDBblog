using MongoDB.Driver;

public class MongoPostRepository : IPostRepository
{
    private readonly IMongoCollection<Post> _posts;
    private readonly PostCacheService _cache;
    private readonly PostSearchService _search;
    // All MongoDB specific code lives here only.
    // The rest of the application depends on IPostRepository interface, not this implementation
    // This means we can swap MongoDB for
    // another database by only changing this file and Program.cs.
    public MongoPostRepository(IMongoDatabase db, PostCacheService cache, PostSearchService search)
    {
         _posts = db.GetCollection<Post>("posts");
         _cache = cache;
         _search = search;
    }
    // Check cache first — only hit MongoDB if cache miss
    public async Task<Post?> GetById(string id)
    {
        var cached = await _cache.GetPostAsync(id);
        if (cached is not null) return cached;

        var post = await _posts.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (post is not null) await _cache.SetPostAsync(post);
        return post;
    }

    public async Task<List<Post>> GetByBlog(string blogId)
        => await _posts.Find(p => p.BlogId == blogId).ToListAsync();

    // Write-through: save to MongoDB AND index in Redis search at the same time
    public async Task<Post> Create(Post post)
    {
        await _posts.InsertOneAsync(post);
        await _search.IndexPostAsync(post);
        return post;
    }

    // Invalidate cache when post is updated so stale data is not served
    public async Task Update(Post post)
    {
        await _posts.ReplaceOneAsync(p => p.Id == post.Id, post);
        await _cache.InvalidatePostAsync(post.Id!);
    }

    public async Task Delete(string id)
        => await _posts.DeleteOneAsync(p => p.Id == id);
 
    public async Task DeleteByBlog(string blogId)
        => await _posts.DeleteManyAsync(p => p.BlogId == blogId);

    public async Task AddComment(string postId, Comment comment)
    // Push adds to the embedded array in a single database operation.
    // No need to fetch the document, modify it and save it back.
    // This is more efficient and avoids race conditions.
        => await _posts.UpdateOneAsync(
            p => p.Id == postId,
            Builders<Post>.Update.Push(p => p.Comments, comment));
}
