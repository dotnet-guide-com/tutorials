using BlazorProfileValidation.Models;
using FluentValidation;

namespace BlazorProfileValidation.Validation;

public sealed class ProfileValidator :
    AbstractValidator<ProfileModel>
{
    public ProfileValidator()
    {
        RuleFor(
                profile =>
                    profile.DisplayName)
            .MinimumLength(2)
            .When(
                profile =>
                    !string.IsNullOrWhiteSpace(
                        profile.DisplayName))
            .WithMessage(
                "Display name must contain at least two characters.");

        RuleFor(
                profile =>
                    profile.DisplayName)
            .Must(
                (
                    profile,
                    displayName) =>
                    string.IsNullOrWhiteSpace(
                        displayName)
                    ||
                    !string.Equals(
                        displayName.Trim(),
                        profile.Username.Trim(),
                        StringComparison.OrdinalIgnoreCase))
            .WithMessage(
                "Display name must differ from the username.");
    }
}