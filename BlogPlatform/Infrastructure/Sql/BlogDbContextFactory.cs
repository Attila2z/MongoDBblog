using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class BlogDbContextFactory : IDesignTimeDbContextFactory<BlogDbContext>
{
    public BlogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BlogDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=blogplatform;Username=postgres;Password=postgres");

        return new BlogDbContext(optionsBuilder.Options);
    }
}