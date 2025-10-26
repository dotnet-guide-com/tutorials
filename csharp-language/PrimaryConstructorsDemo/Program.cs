// Primary Constructors Demo
// Demonstrates C# 12 primary constructors for classes

Person person1 = new("Alice", 30);
Person person2 = new("Bob", 25);

Console.WriteLine(person1);
Console.WriteLine(person2);

class Person(string name, int age)
{
    public string Name { get; } = name;
    public int Age { get; } = age;

    public override string ToString() => $"{Name} is {Age} years old.";
}
