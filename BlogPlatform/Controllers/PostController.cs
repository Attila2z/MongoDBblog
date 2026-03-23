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

    // POST api/blogs/{blogId}/posts
    [HttpPost("blogs/{blogId}/posts")]
    public async Task<IActionResult> Create(string blogId, Post post)
    {
        // Assign the blogId from the route to the post
        post.BlogId = blogId;
        var created = await _posts.Create(post);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // GET api/posts/{id}
    [HttpGet("posts/{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var post = await _posts.GetById(id);
        if (post is null) return NotFound();
        return Ok(post);
    }

    // PUT api/posts/{id}
    [HttpPut("posts/{id}")]
    public async Task<IActionResult> Update(string id, Post post)
    {
        // Make sure the id from the route is used
        post.Id = id;
        await _posts.Update(post);
        return NoContent();
    }

    // DELETE api/posts/{id}
    [HttpDelete("posts/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _posts.Delete(id);
        return NoContent();
    }

    // POST api/posts/{id}/comments
    // POST api/posts/{id}/comments
    [HttpPost("posts/{id}/comments")]
    public async Task<IActionResult> AddComment(string id, Comment comment)
    {
        // Check if user has exceeded the rate limit
        if (!await _cache.IsWithinRateLimitAsync(comment.AuthorName))
            return StatusCode(429, "Too many comments. Please wait before commenting again.");

        // Increment the comment counter for this user
        await _cache.IncrementCommentCountAsync(comment.AuthorName);

        // Pushes comment into the embedded array inside the post
        // No need to fetch the whole post first
        await _posts.AddComment(id, comment);
        return NoContent();
    }

    // GET api/posts/search?q=keyword
    [HttpGet("posts/search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrEmpty(q))
            return BadRequest("Search query cannot be empty");

        var results = await _search.SearchAsync(q);
        return Ok(results);
    }
    
}