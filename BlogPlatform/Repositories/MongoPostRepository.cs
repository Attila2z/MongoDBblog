using MongoDB.Driver;

public class MongoPostRepository : IPostRepository
{
    private readonly IMongoCollection<Post> _posts;
    
    // All MongoDB specific code lives here only.
    // The rest of the application depends on IPostRepository interface, not this implementation
    // This means we can swap MongoDB for
    // another database by only changing this file and Program.cs.
    public MongoPostRepository(IMongoDatabase db)
        => _posts = db.GetCollection<Post>("posts");

    public async Task<Post?> GetById(string id)
        => await _posts.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task<List<Post>> GetByBlog(string blogId)
        => await _posts.Find(p => p.BlogId == blogId).ToListAsync();

    public async Task<Post> Create(Post post)
    {
        await _posts.InsertOneAsync(post);
        return post;
    }

    public async Task Update(Post post)
        => await _posts.ReplaceOneAsync(p => p.Id == post.Id, post);

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
