using Microsoft.EntityFrameworkCore;
public class SqlPostRepository
{
    private readonly BlogDbContext _db;

    public SqlPostRepository(BlogDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(PostEntity post)
    {
        _db.Posts.Add(post);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(PostEntity post)
    {
        _db.Posts.Update(post);
        await _db.SaveChangesAsync();
    }

    public async Task<PostEntity?> GetByMongoIdAsync(string mongoId)
        => await _db.Posts.FirstOrDefaultAsync(p => p.MongoId == mongoId);
}