using System.ComponentModel.DataAnnotations;

namespace BlazorProfileValidation.Models;

public sealed class ProfileModel
{
    [Required(
        ErrorMessage =
            "Username is required.")]
    [StringLength(
        30,
        MinimumLength = 3,
        ErrorMessage =
            "Username must contain between 3 and 30 characters.")]
    [RegularExpression(
        "^[a-z0-9_]+$",
        ErrorMessage =
            "Username can contain lowercase letters, numbers, and underscores only.")]
    public string Username
    {
        get;
        set;
    } = string.Empty;

    [Required(
        ErrorMessage =
            "Email is required.")]
    [EmailAddress(
        ErrorMessage =
            "Enter a valid email address.")]
    public string Email
    {
        get;
        set;
    } = string.Empty;

    [StringLength(
        100,
        ErrorMessage =
            "Display name must contain 100 characters or fewer.")]
    public string DisplayName
    {
        get;
        set;
    } = string.Empty;

    public bool EmailNotifications
    {
        get;
        set;
    } = true;

    public ProfileModel Copy() =>
        new()
        {
            Username =
                Username,

            Email =
                Email,

            DisplayName =
                DisplayName,

            EmailNotifications =
                EmailNotifications
        };
}