using MongoDB.Driver;
public class MongoPostRepository : IPostRepository
{
    private readonly IMongoCollection<Post> _posts;
    private readonly PostCacheService _cache;
    private readonly PostSearchService _search;

    public MongoPostRepository(IMongoDatabase db, PostCacheService cache, PostSearchService search)
    {
        _posts = db.GetCollection<Post>("posts");  // "posts" = collection name in MongoDB
        _cache = cache;
        _search = search;
    }
    public async Task<Post?> GetById(string id)
    {
        var cached = await _cache.GetPostAsync(id);
        if (cached is not null) return cached;  // cache hit — no DB call needed

        var post = await _posts.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (post is not null) await _cache.SetPostAsync(post);  // populate cache for next time
        return post;
    }

    public async Task<List<Post>> GetByBlog(string blogId)
        => await _posts.Find(p => p.BlogId == blogId).ToListAsync();
    public async Task<Post> Create(Post post)
    {
        await _posts.InsertOneAsync(post);
        await _search.IndexPostAsync(post);  // write-through to Redis search index
        return post;
    }
    public async Task Update(Post post)
    {
        await _posts.ReplaceOneAsync(p => p.Id == post.Id, post);
        await _cache.InvalidatePostAsync(post.Id!);  // remove stale entry from cache
    }

    // DELETE /api/posts/{id}
    public async Task Delete(string id)
        => await _posts.DeleteOneAsync(p => p.Id == id);
    public async Task DeleteByBlog(string blogId)
        => await _posts.DeleteManyAsync(p => p.BlogId == blogId);
    public async Task AddComment(string postId, Comment comment)
        => await _posts.UpdateOneAsync(
            p => p.Id == postId,
            Builders<Post>.Update.Push(p => p.Comments, comment));
}
