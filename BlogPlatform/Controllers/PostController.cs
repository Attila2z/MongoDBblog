using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class PostController : ControllerBase
{
    private readonly IPostRepository _posts;
    private readonly PostCacheService _cache;
    private readonly PostSearchService _search;
    private readonly CreatePostCommandHandler _createHandler;
    private readonly UpdatePostCommandHandler _updateHandler;
    private readonly GetPostQueryHandler _getHandler;

    public PostController(
        IPostRepository posts,
        PostCacheService cache,
        PostSearchService search,
        CreatePostCommandHandler createHandler,
        UpdatePostCommandHandler updateHandler,
        GetPostQueryHandler getHandler)
    {
        _posts = posts;
        _cache = cache;
        _search = search;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _getHandler = getHandler;
    }

    // COMMANDS (write via handlers)
    [HttpPost("blogs/{blogId}/posts")]
    public async Task<IActionResult> Create(string blogId, Post post)
    {
        var command = new CreatePostCommand
        {
            BlogId = blogId,
            Title = post.Title,
            Body = post.Body
        };
        var created = await _createHandler.HandleAsync(command);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("posts/{id}")]
    public async Task<IActionResult> Update(string id, Post post)
    {
        await _updateHandler.HandleAsync(new UpdatePostCommand
        {
            Id = id,
            Title = post.Title,
            Body = post.Body
        });
        return NoContent();
    }

    // QUERY (read via handler)
    [HttpGet("posts/{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var post = await _getHandler.HandleAsync(new GetPostQuery { PostId = id });
        if (post is null) return NotFound();
        return Ok(post);
    }

    // UNCHANGED from week 12
    [HttpDelete("posts/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _posts.Delete(id);
        return NoContent();
    }

    [HttpPost("posts/{id}/comments")]
    public async Task<IActionResult> AddComment(string id, Comment comment)
    {
        if (!await _cache.IsWithinRateLimitAsync(comment.AuthorName))
            return StatusCode(429, "Too many comments. Please wait before commenting again.");

        await _cache.IncrementCommentCountAsync(comment.AuthorName);
        await _posts.AddComment(id, comment);
        return NoContent();
    }

    [HttpGet("posts/search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrEmpty(q))
            return BadRequest("Search query cannot be empty");

        var results = await _search.SearchAsync(q);
        return Ok(results);
    }
}