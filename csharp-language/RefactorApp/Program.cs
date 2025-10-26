// RefactorApp Demo
// Demonstrates refactoring procedural code (static methods) into OOP (class with instance methods)
// Inspired by tutorial sections on Methods and Classes

// BEFORE: Procedural version using static methods
Console.WriteLine("=== BEFORE: Procedural Version ===");
string input1 = ProcessStringProcedural("Hello World");
Console.WriteLine($"Processed: {input1}");

// AFTER: Refactored OOP version
Console.WriteLine("\n=== AFTER: Refactored OOP Version ===");
StringProcessor processor = new();
string input2 = processor.ProcessString("Hello World");
Console.WriteLine($"Processed: {input2}");

// Procedural version (BEFORE refactor)
static string ProcessStringProcedural(string text)
{
    string upper = ToUpper(text);
    string reversed = Reverse(upper);
    return AddPrefix(reversed);
}

static string ToUpper(string s) => s.ToUpper();
static string Reverse(string s) => string.Concat(s.Reverse());
static string AddPrefix(string s) => $"Processed: {s}";

// Refactored OOP version (AFTER)
class StringProcessor
{
    public string ProcessString(string text)
    {
        return AddPrefix(Reverse(ToUpper(text)));
    }

    private string ToUpper(string s) => s.ToUpper();
    private string Reverse(string s) => string.Concat(s.Reverse());
    private string AddPrefix(string s) => $"Processed: {s}";
}
