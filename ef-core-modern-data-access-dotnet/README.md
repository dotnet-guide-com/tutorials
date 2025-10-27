# EF Core 8 Fundamentals – sample code for the tutorial

This folder contains a runnable .NET 8 console app demo used in the guide
"[EF Core 8 Fundamentals: Modern Data Access with .NET 8](https://www.dotnet-guide.com/tutorials/ef-core/modern-data-access-dotnet/)."

    🗄️ BlogApp – Models, DbContext, migrations, seeding, and CRUD with LINQ

## Prerequisites

.NET 8 SDK installed (dotnet --version should start with 8.)  
Any editor (VS 2022 17.8+, VS Code with C# extension, or Rider)  
EF Core CLI tools (install with: `dotnet tool install --global dotnet-ef --version 8.0.0`)

## Getting Started

Clone the repo:

git clone https://github.com/dotnet-guide-com/tutorials.git

Then follow the instructions below. The demo uses SQLite (creates `blog.db` file).

### 1) Run BlogApp (full EF Core demo)

**What it shows:**

Entity models with relationships (Blog ↔ Post)  
DbContext setup, migrations, seeding, and CRUD operations (LINQ queries, Include for eager loading)

**Setup & Run**

cd tutorials/ef-core-modern-data-access-dotnet/BlogApp
dotnet restore
Create migration and update DB (SQLite file: blog.db)

dotnet ef migrations add InitialCreate
dotnet ef database update
Run the app

dotnet run

**Expected output:**

Seeded sample data.

=== EF Core 8 CRUD Demo ===
Created new post with ID: 3

All Blogs and Posts:

    Blog ID: 1, URL: https://example.com/blog
        Post: First Post (ID: 1)
        Post: Second Post (ID: 2)
    Blog ID: 2, URL: https://example.com/new
        Post: New Post (ID: 3)

Updated post 'New Post' content.
Deleted the updated post.

Remaining Posts Count: 2


For more: [Models](https://www.dotnet-guide.com/tutorials/ef-core/modern-data-access-dotnet/#models-and-configurations), [DbContext](https://www.dotnet-guide.com/tutorials/ef-core/modern-data-access-dotnet/#dbcontext-and-configurations), [Migrations](https://www.dotnet-guide.com/tutorials/ef-core/modern-data-access-dotnet/#migrations-and-seeding), [CRUD](https://www.dotnet-guide.com/tutorials/ef-core/modern-data-access-dotnet/#crud-operations)

## Troubleshooting

**dotnet-ef not found**  
Install/update: `dotnet tool install --global dotnet-ef --version 8.0.0` (restart terminal).

**Migration errors or DB locked**  
Delete `blog.db` and `Migrations/` folder, then re-run `dotnet ef migrations add InitialCreate` and `dotnet ef database update`.

**No output or build issues**  
Run `dotnet build` first. Ensure SQLite package is restored. For in-memory DB (no file), change connection in Program.cs to `"Data Source=:memory:"`.

**Relationship errors**  
Check OnModelCreating in BlogDb.cs—ensure FK config matches models.

Example targets .NET 8 and EF Core 8 with SQLite for simplicity.  
It's focused for learning—swap to SQL Server or add raw SQL queries to extend!

Happy querying! 🚀
