using TerminalSnake.Rendering;

namespace TerminalSnake.Tests.Rendering;

public sealed class ViewportCalculatorTests
{
    [Fact]
    public void Centers_board_horizontally_and_places_below_top_padding()
    {
        // 80 wide, board 10 => 20 chars wide; center: (80-20)/2 = 30.
        // 24 tall, board 10, innerHeight = 24 - 2 - 4 = 18; slack (18-10)/2 = 4.
        // origin Y = 1 (border) + 2 (padding) + 4 = 7.
        var viewport = ViewportCalculator.Compute(80, 24, 10);
        Assert.Equal(10, viewport.BoardSide);
        Assert.Equal(30, viewport.BoardOriginX);
        Assert.Equal(7, viewport.BoardOriginY);
    }

    [Fact]
    public void Compute_throws_when_terminal_below_required_minimum()
    {
        var ex = Assert.Throws<TerminalTooSmallException>(() =>
            ViewportCalculator.Compute(10, 10, 10));
        Assert.Equal(10, ex.ActualWidth);
        Assert.True(ex.RequiredWidth > ex.ActualWidth);
    }

    [Fact]
    public void Compute_rejects_board_side_below_minimum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ViewportCalculator.Compute(80, 24, 2));
    }

    [Theory]
    [InlineData(80, 24, 18)]  // limited by inner height = 24 - 2 - 4 = 18
    [InlineData(40, 40, 19)]  // limited by width = (40 - 2) / 2 = 19
    [InlineData(20, 20, 9)]   // smaller terminal
    public void MaxBoardSide_chooses_smaller_of_width_and_height_capacities(
        int width, int height, int expected)
    {
        Assert.Equal(expected, ViewportCalculator.MaxBoardSide(width, height));
    }

    [Fact]
    public void MaxBoardSide_returns_zero_for_too_small_terminal()
    {
        Assert.Equal(0, ViewportCalculator.MaxBoardSide(5, 5));
    }

    [Fact]
    public void Minimum_dimensions_fit_a_minimum_sized_board()
    {
        var width = ViewportCalculator.MinimumWidth;
        var height = ViewportCalculator.MinimumHeight;
        var viewport = ViewportCalculator.Compute(width, height, ViewportCalculator.MinBoardSide);
        Assert.Equal(ViewportCalculator.MinBoardSide, viewport.BoardSide);
    }

    [Fact]
    public void TopHudRow_is_first_row_after_border()
    {
        var viewport = ViewportCalculator.Compute(80, 24, 10);
        Assert.Equal(1, viewport.TopHudRow);
        Assert.Equal(22, viewport.BottomHudRow);
    }
}
