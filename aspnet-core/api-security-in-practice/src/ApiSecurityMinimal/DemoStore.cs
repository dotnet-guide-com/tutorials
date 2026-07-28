using System.Collections.Concurrent;

namespace ApiSecurityMinimal;

public sealed class DemoStore
{
    private readonly ConcurrentDictionary<string, DemoUser> _users = new();
    private readonly ConcurrentDictionary<string, Note> _notes = new();
    private int _nextNoteNumber;

    public DemoStore()
    {
        Seed();
    }

    private void Seed()
    {
        var user = new DemoUser(
            Id: "user-001",
            Email: "demo@example.com",
            Password: "DemoPass123!",
            Role: "User");
        _users.TryAdd(user.Email, user);
    }

    public DemoUser? FindUserByEmail(string email) =>
        _users.TryGetValue(email, out var user) ? user : null;

    public Note CreateNote(string ownerId, string title, string body)
    {
        int number = Interlocked.Increment(ref _nextNoteNumber);
        var note = new Note(
            Id: $"note-{number:D4}",
            OwnerId: ownerId,
            Title: title,
            Body: body);
        _notes.TryAdd(note.Id, note);
        return note;
    }

    public IEnumerable<Note> GetNotesByOwner(string ownerId) =>
        _notes.Values.Where(n => n.OwnerId == ownerId);

    public Note? FindNote(string noteId) =>
        _notes.TryGetValue(noteId, out var note) ? note : null;

    public bool DeleteNote(string noteId, string ownerId)
    {
        if (!_notes.TryGetValue(noteId, out var note))
            return false;

        if (note.OwnerId != ownerId)
            return false;

        return _notes.TryRemove(noteId, out _);
    }
}