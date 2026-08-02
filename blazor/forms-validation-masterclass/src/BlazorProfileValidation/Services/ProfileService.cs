using BlazorProfileValidation.Models;

namespace BlazorProfileValidation.Services;

public sealed class ProfileService
{
    private ProfileModel _savedProfile =
        new()
        {
            Username =
                "dotnet_reader",

            Email =
                "reader@example.com",

            DisplayName =
                "DOTNET Reader",

            EmailNotifications =
                true
        };

    public int SaveAttempts
    {
        get;
        private set;
    }

    public ProfileModel LastSavedProfile =>
        _savedProfile.Copy();

    public ProfileModel Load() =>
        _savedProfile.Copy();

    public Task<ProfileSaveResult> SaveAsync(
        ProfileModel profile,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        SaveAttempts++;

        var errors =
            new Dictionary<
                string,
                string[]>(
                StringComparer.Ordinal);

        if (string.Equals(
                profile.Username,
                "reserved",
                StringComparison.OrdinalIgnoreCase))
        {
            errors[
                nameof(
                    ProfileModel.Username)] =
            [
                "This username is reserved by the profile service."
            ];
        }

        if (profile.Email.EndsWith(
                "@blocked.example",
                StringComparison.OrdinalIgnoreCase))
        {
            errors[
                nameof(
                    ProfileModel.Email)] =
            [
                "This email domain is blocked by the profile service."
            ];
        }

        if (errors.Count > 0)
        {
            return Task.FromResult(
                ProfileSaveResult
                    .Rejected(
                        errors));
        }

        _savedProfile =
            profile.Copy();

        return Task.FromResult(
            ProfileSaveResult
                .Accepted());
    }
}

public sealed record ProfileSaveResult(
    bool Succeeded,
    IReadOnlyDictionary<
        string,
        string[]> Errors)
{
    public static ProfileSaveResult
        Accepted() =>
            new(
                true,
                new Dictionary<
                    string,
                    string[]>());

    public static ProfileSaveResult
        Rejected(
            IReadOnlyDictionary<
                string,
                string[]> errors) =>
            new(
                false,
                errors);
}