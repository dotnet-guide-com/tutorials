namespace HybridSearch.Api.Services;

public sealed class TextChunker
{
    public IReadOnlyList<string> Split(
        string text,
        int maxWords = 120,
        int overlapWords = 20)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        if (maxWords <= 0) throw new ArgumentOutOfRangeException(nameof(maxWords));
        if (overlapWords < 0 || overlapWords >= maxWords)
        {
            throw new ArgumentOutOfRangeException(nameof(overlapWords));
        }

        string[] words = text.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var chunks = new List<string>();
        int start = 0;

        while (start < words.Length)
        {
            int end = Math.Min(start + maxWords, words.Length);
            chunks.Add(string.Join(" ", words[start..end]));

            if (end == words.Length) break;
            start = end - overlapWords;
        }

        return chunks;
    }
}