# C# Language – sample code for the tutorial

This folder contains three runnable console apps used in the guide
"[C# Language Fundamentals](https://www.dotnet-guide.com/tutorials/csharp-language/)."

    🔧 PrimaryConstructorsDemo – C# 12 primary constructors
    📚 CollectionsDemo – List<T> and Dictionary<TKey, TValue>
    ♻️ RefactorApp – Procedural to OOP refactoring

Prerequisites

    .NET 8 SDK installed (dotnet --version should start with 8.)
    Any editor (VS 2022 17.8+, VS Code with C# extension, or Rider)

## Getting Started

Clone the repo:

git clone https://github.com/dotnet-guide-com/tutorials.git


Then follow the per-demo instructions below.

1) Run PrimaryConstructorsDemo (primary constructors)

What it shows:

    C# 12 primary constructors for classes
    Parameter capture and immutability

Run

cd tutorials/csharp-language/PrimaryConstructorsDemo
dotnet restore
dotnet run


Expected output:

Alice is 30 years old.
Bob is 25 years old.


For more: [Primary Constructors](https://www.dotnet-guide.com/tutorials/csharp-language/#primary-constructors)

2) Run CollectionsDemo (collections)

What it shows:

    List<T>: Add/remove/iterate items
    Dictionary<TKey, TValue>: Key-value pairs with simple querying

Run

cd tutorials/csharp-language/CollectionsDemo
dotnet restore
dotnet run

Expected output:

Fruits List:

    Apple
    Cherry
    Date

Ages Dictionary:
Alice: 30
Bob: 25
Charlie: 35
Diana: 28

People older than 28: Alice, Charlie


For more: [Collections](https://www.dotnet-guide.com/tutorials/csharp-language/#collections)

3) Run RefactorApp (refactoring)

What it shows:

    Before: Procedural code with static methods
    After: OOP class with instance methods for encapsulation

Run

cd tutorials/csharp-language/RefactorApp
dotnet restore
dotnet run

Expected output:

=== BEFORE: Procedural Version ===
Processed: PROCESED: DLROW OLLEH

=== AFTER: Refactored OOP Version ===
Processed: PROCESED: DLROW OLLEH


For more: [Methods](https://www.dotnet-guide.com/tutorials/csharp-language/#methods) & [Classes](https://www.dotnet-guide.com/tutorials/csharp-language/#classes)

Troubleshooting

dotnet run fails with version error
Confirm .NET 8 SDK: `dotnet --version`. Install/update from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0).

No output or build errors
Run `dotnet restore` first, then `dotnet build` to check for issues. Ensure ImplicitUsings and Nullable are enabled in .csproj (they are by default here).

Code doesn't match tutorial
These demos are focused excerpts—see the full tutorial for context and variations.

Examples target .NET 8 and modern C# features.
They're deliberately small for quick learning—experiment and extend them!

Happy coding! 🚀

