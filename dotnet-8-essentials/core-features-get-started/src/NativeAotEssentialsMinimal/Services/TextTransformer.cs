namespace NativeAotEssentialsMinimal.Services;

public sealed class TextTransformer :
    ITextTransformer
{
    public string Normalize(
        string value)
    {
        ArgumentNullException
            .ThrowIfNull(
                value);

        string[] words =
            value.Split(
                ' ',
                StringSplitOptions
                    .RemoveEmptyEntries
                | StringSplitOptions
                    .TrimEntries);

        return string.Join(
                ' ',
                words)
            .ToUpperInvariant();
    }
}