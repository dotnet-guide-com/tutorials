using ProviderAgnosticChatGateway.Tools;

namespace ProviderAgnosticChatGateway.Tests;

public sealed class ToolTests
{
    [Theory]
    [InlineData(0, "c", "f", 32)]
    [InlineData(100, "celsius", "fahrenheit", 212)]
    [InlineData(32, "f", "c", 0)]
    [InlineData(273.15, "k", "c", 0)]
    public void ConvertTemperature_ReturnsExpectedValue(
        double value,
        string from,
        string to,
        double expected)
    {
        double actual =
            TemperatureTool.ConvertTemperature(
                value,
                from,
                to);

        Assert.Equal(expected, actual, precision: 2);
    }

    [Fact]
    public void ConvertTemperature_RejectsUnsupportedUnit()
    {
        Assert.Throws<ArgumentException>(
            () => TemperatureTool.ConvertTemperature(
                10,
                "rankine",
                "c"));
    }
}
