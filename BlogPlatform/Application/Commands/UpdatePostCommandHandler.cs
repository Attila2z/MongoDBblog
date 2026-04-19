public class UpdatePostCommandHandler
{
    private readonly IPostRepository _mongo;
    private readonly SqlPostRepository _sql;

    public UpdatePostCommandHandler(IPostRepository mongo, SqlPostRepository sql)
    {
        _mongo = mongo;
        _sql = sql;
    }

    public async Task HandleAsync(UpdatePostCommand command)
    {
        // 1. Update MongoDB
        var post = await _mongo.GetById(command.Id);
        if (post is null) return;

        post.Title = command.Title;
        post.Body = command.Body;
        await _mongo.Update(post);

        // 2. Update PostgreSQL
        var entity = await _sql.GetByMongoIdAsync(command.Id);
        if (entity is null) return;

        entity.Title = command.Title;
        entity.Body = command.Body;
        await _sql.UpdateAsync(entity);
    }
}