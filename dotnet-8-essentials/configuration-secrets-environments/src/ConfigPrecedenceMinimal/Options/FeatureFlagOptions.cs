namespace ConfigPrecedenceMinimal.Options;

public sealed class FeatureFlagOptions
{
    public const string SectionName =
        "Features";

    public bool AdvancedSearch
    {
        get;
        init;
    }
}