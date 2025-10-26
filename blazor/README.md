# Blazor – sample code for the tutorial

This folder contains two runnable Blazor WebAssembly apps used in the guide
"[Blazor Web Development](https://www.dotnet-guide.com/tutorials/blazor/)."

    🔢 CounterApp – Basic components, data binding, and events
    📝 TodoApp – Forms, dependency injection, and state management

## Prerequisites

.NET 8 SDK installed (dotnet --version should start with 8.)  
Any editor (VS 2022 17.8+, VS Code with C# extension, or Rider)  
A modern browser (Chrome, Edge, Firefox)

## Getting Started

Clone the repo:

git clone https://github.com/dotnet-guide-com/tutorials.git

Then follow the per-demo instructions below. Each is a minimal Blazor WebAssembly app—run `dotnet restore`, then `dotnet run` to serve it locally (opens in browser at http://localhost:5000).

### 1) Run CounterApp (basic components)

**What it shows:**

Blazor components with @page routing  
Data binding (@bind) and event handling (@onclick)

**Run**

cd tutorials/blazor/CounterApp
dotnet restore
dotnet run

Open http://localhost:5000/counter in your browser.

**Expected output:**

A simple page with "Counter" header, "Current count: 0", and a "Click me" button.  
Clicking the button increments the count (e.g., to 1, 2, ...).

For more: [Components](https://www.dotnet-guide.com/tutorials/blazor/#components), [Data Binding](https://www.dotnet-guide.com/tutorials/blazor/#data-binding), [Event Handling](https://www.dotnet-guide.com/tutorials/blazor/#event-handling)

### 2) Run TodoApp (forms and state)

**What it shows:**

EditForm for input and validation  
Dependency injection (@inject) and service-based state updates

**Run**

cd tutorials/blazor/TodoApp
dotnet restore
dotnet run


Open http://localhost:5000/todo in your browser.

**Expected output:**

A "Todo List" page with an input field and "Add Todo" button.  
Enter text (e.g., "Learn Blazor") and submit— it appears in a list.  
Click "Delete" to remove items. Starts with "No todos yet!" if empty.

For more: [Forms](https://www.dotnet-guide.com/tutorials/blazor/#forms), [Dependency Injection](https://www.dotnet-guide.com/tutorials/blazor/#dependency-injection), [State Management](https://www.dotnet-guide.com/tutorials/blazor/#state-management)

## Troubleshooting

**App doesn't load in browser**  
Check console (F12) for errors. Run `dotnet dev-certs https --trust` if using HTTPS. Use HTTP URL if issues persist.

**Build errors or missing files**  
These are minimal demos—add standard Blazor files (App.razor, index.html) from `dotnet new blazorwasm` if needed. Run `dotnet build` to diagnose.

**UI not updating**  
Ensure browser cache is cleared (Ctrl+F5). Blazor WebAssembly requires internet for initial load (downloads framework).

Examples target .NET 8 and focus on core Blazor features.  
They're small for quick learning—add routing or CSS to extend them!

Happy building! 🚀
