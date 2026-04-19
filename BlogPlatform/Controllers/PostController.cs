using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class PostController : ControllerBase
{
    private readonly IPostRepository _posts;
    private readonly PostCacheService _cache;
    private readonly PostSearchService _search;

    public PostController(IPostRepository posts, PostCacheService cache, PostSearchService search)
    {
        _posts = posts;
        _cache = cache;
        _search = search;
    }

    // POST /api/blogs/{blogId}/posts
    [HttpPost("blogs/{blogId}/posts")]
    public async Task<IActionResult> Create(string blogId, Post post)
    {
        post.BlogId = blogId;  // override with route value to prevent tampering
        var created = await _posts.Create(post);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // GET /api/posts/{id}
    [HttpGet("posts/{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var post = await _posts.GetById(id);
        if (post is null) return NotFound();
        return Ok(post);
    }

    // PUT /api/posts/{id}
    [HttpPut("posts/{id}")]
    public async Task<IActionResult> Update(string id, Post post)
    {
        post.Id = id;  // enforce route id — don't trust what the client sends in the body
        await _posts.Update(post);
        return NoContent();  // 204 — success, no body
    }

    // DELETE /api/posts/{id}
    [HttpDelete("posts/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _posts.Delete(id);
        return NoContent();
    }

    // POST /api/posts/{id}/comments
    [HttpPost("posts/{id}/comments")]
    public async Task<IActionResult> AddComment(string id, Comment comment)
    {
        if (!await _cache.IsWithinRateLimitAsync(comment.AuthorName))
            return StatusCode(429, "Too many comments. Please wait before commenting again.");

        await _cache.IncrementCommentCountAsync(comment.AuthorName);  // track the count
        await _posts.AddComment(id, comment);                          // write the comment
        return NoContent();
    }

    // GET /api/posts/search?q=keyword
    [HttpGet("posts/search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrEmpty(q))
            return BadRequest("Search query cannot be empty");

        var results = await _search.SearchAsync(q);
        return Ok(results);
    }
}
