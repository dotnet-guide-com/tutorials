using ConfigPrecedenceMinimal.Options;
using Microsoft.Extensions.Options;

namespace ConfigPrecedenceMinimal.Validation;

public sealed class AppOptionsValidator :
    IValidateOptions<AppOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        AppOptions options)
    {
        ArgumentNullException
            .ThrowIfNull(
                options);

        List<string> failures =
        [
        ];

        if (!Uri.TryCreate(
                options.ServiceBaseUrl,
                UriKind.Absolute,
                out Uri? serviceUri)
            || serviceUri.Scheme
                is not ("http" or "https"))
        {
            failures.Add(
                "App:ServiceBaseUrl must be an absolute HTTP or HTTPS URI.");
        }
        else if (options.RequireHttps
            && serviceUri.Scheme
                != Uri.UriSchemeHttps)
        {
            failures.Add(
                "App:ServiceBaseUrl must use HTTPS when App:RequireHttps is true.");
        }

        if (string.Equals(
                options.ApiKey,
                "CHANGE_ME",
                StringComparison.OrdinalIgnoreCase)
            || options.ApiKey.StartsWith(
                "replace-",
                StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(
                "App:ApiKey still contains a placeholder value.");
        }

        return failures.Count
            == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(
                    failures);
    }
}