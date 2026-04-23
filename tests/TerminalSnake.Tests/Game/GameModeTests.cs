using TerminalSnake.Game;

namespace TerminalSnake.Tests.Game;

public sealed class GameModeTests
{
    [Fact]
    public void Enum_has_player_and_demo_values()
    {
        var values = Enum.GetValues<GameMode>();
        Assert.Contains(GameMode.Player, values);
        Assert.Contains(GameMode.Demo, values);
    }
}
