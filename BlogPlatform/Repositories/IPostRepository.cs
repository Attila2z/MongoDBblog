
public interface IPostRepository
{
    Task<Post?> GetById(string id);
    Task<List<Post>> GetByBlog(string blogId);
    Task<Post> Create(Post post);
    Task Update(Post post);
    Task Delete(string id);
    Task AddComment(string postId, Comment comment);
    Task DeleteByBlog(string blogId);
}
