using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using BlogApp;

// Setup SQLite in-memory for demo (or use file: "Data Source=blog.db")
var options = new DbContextOptionsBuilder<BlogDb>()
    .UseSqlite("Data Source=:memory:")
    .Options;

using var db = new BlogDb(options);

// Ensure DB is created (run migrations first in real use)
db.Database.EnsureCreated();

// Seed data if no blogs exist
if (!db.Blogs.Any())
{
    var blog = new Blog { Url = "https://example.com/blog" };
    db.Blogs.Add(blog);
    db.SaveChanges();

    db.Posts.AddRange(
        new Post { Title = "First Post", Content = "Hello World!", BlogId = blog.Id },
        new Post { Title = "Second Post", Content = "EF Core is great!", BlogId = blog.Id }
    );
    db.SaveChanges();

    Console.WriteLine("Seeded sample data.");
}

// CRUD Demo
Console.WriteLine("\n=== EF Core 8 CRUD Demo ===");

// CREATE: Add a new post
var newBlog = new Blog { Url = "https://example.com/new" };
db.Blogs.Add(newBlog);
db.SaveChanges();

var newPost = new Post { Title = "New Post", Content = "Added via EF!", BlogId = newBlog.Id };
db.Posts.Add(newPost);
db.SaveChanges();
Console.WriteLine($"Created new post with ID: {newPost.Id}");

// READ: Query blogs and their posts (LINQ eager loading)
var blogsWithPosts = db.Blogs
    .Include(b => b.Posts)
    .ToList();

Console.WriteLine("\nAll Blogs and Posts:");
foreach (var blog in blogsWithPosts)
{
    Console.WriteLine($"- Blog ID: {blog.Id}, URL: {blog.Url}");
    foreach (var post in blog.Posts)
    {
        Console.WriteLine($"  * Post: {post.Title} (ID: {post.Id})");
    }
}

// UPDATE: Update a post
var postToUpdate = db.Posts.First(p => p.Title == "New Post");
postToUpdate.Content += " Updated!";
db.SaveChanges();
Console.WriteLine($"\nUpdated post '{postToUpdate.Title}' content.");

// DELETE: Remove a post
db.Posts.Remove(postToUpdate);
db.SaveChanges();
Console.WriteLine("Deleted the updated post.");

// Final read to verify
var remainingPosts = db.Posts.ToList();
Console.WriteLine($"\nRemaining Posts Count: {remainingPosts.Count}");
