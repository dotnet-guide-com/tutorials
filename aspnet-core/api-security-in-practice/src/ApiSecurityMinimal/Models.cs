namespace ApiSecurityMinimal;

public sealed record DemoUser(
    string Id,
    string Email,
    string Password,
    string Role);

public sealed record Note(
    string Id,
    string OwnerId,
    string Title,
    string Body);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record CreateNoteRequest(
    string Title,
    string Body);