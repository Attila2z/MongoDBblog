using MongoDB.Driver;

public class MongoBlogRepository : IBlogRepository
{
    private readonly IMongoCollection<Blog> _blogs;
    private readonly IPostRepository _posts;

    public MongoBlogRepository(IMongoDatabase db, IPostRepository posts)
    {
        _blogs = db.GetCollection<Blog>("blogs");
        _posts = posts;
    }

    public async Task<Blog?> GetById(string id)
        => await _blogs.Find(b => b.Id == id).FirstOrDefaultAsync();

    public async Task<List<Blog>> GetAll()
        => await _blogs.Find(_ => true).ToListAsync();

    public async Task<Blog> Create(Blog blog)
    {
        await _blogs.InsertOneAsync(blog);
        return blog;
    }

    public async Task Delete(string id)
    {
        await _posts.DeleteByBlog(id);
        await _blogs.DeleteOneAsync(b => b.Id == id);
    }
}