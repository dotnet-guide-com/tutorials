using System.ComponentModel.DataAnnotations;

namespace ConfigPrecedenceMinimal.Options;

public sealed class AppOptions
{
    public const string SectionName =
        "App";

    [Required]
    [Url]
    public string ServiceBaseUrl
    {
        get;
        init;
    } = "";

    [Range(
        1,
        120)]
    public int TimeoutSeconds
    {
        get;
        init;
    } = 10;

    public bool RequireHttps
    {
        get;
        init;
    } = true;

    [Required]
    [MinLength(
        12)]
    public string ApiKey
    {
        get;
        init;
    } = "";
}