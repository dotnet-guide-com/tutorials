using HybridSearch.Api.Search;

namespace HybridSearch.Api.Tests;

public sealed class SearchModeParserTests
{
    [Theory]
    [InlineData("keyword", SearchMode.Keyword)]
    [InlineData("VECTOR", SearchMode.Vector)]
    [InlineData(" hybrid ", SearchMode.Hybrid)]
    [InlineData(null, SearchMode.Hybrid)]
    public void Supported_modes_parse(string? value, SearchMode expected)
    {
        Assert.Equal(expected, SearchModeParser.Parse(value));
    }

    [Fact]
    public void Unsupported_mode_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => SearchModeParser.Parse("semantic-only"));
    }
}