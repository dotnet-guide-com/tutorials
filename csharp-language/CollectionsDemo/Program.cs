// Collections Demo
// Demonstrates C# collections: List<T> and Dictionary<TKey, TValue>

// List<T> example
List<string> fruits = new() { "Apple", "Banana", "Cherry" };
fruits.Add("Date");
fruits.Remove("Banana");

Console.WriteLine("Fruits List:");
foreach (string fruit in fruits)
{
    Console.WriteLine($"- {fruit}");
}

// Dictionary<TKey, TValue> example
Dictionary<string, int> ages = new()
{
    ["Alice"] = 30,
    ["Bob"] = 25,
    ["Charlie"] = 35
};

ages["Diana"] = 28; // Add new entry

Console.WriteLine("\nAges Dictionary:");
foreach (var kvp in ages)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

// Query example: Get names older than 28
var olderThan28 = ages.Where(kvp => kvp.Value > 28).Select(kvp => kvp.Key);
Console.WriteLine($"\nPeople older than 28: {string.Join(", ", olderThan28)}");
