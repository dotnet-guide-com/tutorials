using Microsoft.EntityFrameworkCore;

namespace BlogApp;

public class BlogDb : DbContext
{
    public DbSet<Blog> Blogs { get; set; } = null!;
    public DbSet<Post> Posts { get; set; } = null!;

    public BlogDb(DbContextOptions<BlogDb> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>()
            .HasKey(b => b.Id);

        modelBuilder.Entity<Post>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<Post>()
            .HasOne(p => p.Blog)
            .WithMany(b => b.Posts)
            .HasForeignKey(p => p.BlogId);

        base.OnModelCreating(modelBuilder);
    }
}
