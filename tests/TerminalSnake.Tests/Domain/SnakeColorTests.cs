using TerminalSnake.Domain;

namespace TerminalSnake.Tests.Domain;

public sealed class SnakeColorTests
{
    [Fact]
    public void Enum_exposes_eight_distinct_colors()
    {
        var values = Enum.GetValues<SnakeColor>();
        Assert.Equal(8, values.Length);
        Assert.Equal(values.Length, values.Distinct().Count());
    }
}
