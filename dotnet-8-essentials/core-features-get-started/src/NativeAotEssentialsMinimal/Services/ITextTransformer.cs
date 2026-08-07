namespace NativeAotEssentialsMinimal.Services;

public interface ITextTransformer
{
    string Normalize(
        string value);
}