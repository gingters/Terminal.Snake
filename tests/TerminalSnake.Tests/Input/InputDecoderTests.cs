using System.Text;
using TerminalSnake.Input;

namespace TerminalSnake.Tests.Input;

public sealed class InputDecoderTests
{
    [Fact]
    public void Empty_buffer_consumes_nothing()
    {
        var consumed = InputDecoder.TryDecode(ReadOnlySpan<byte>.Empty, out var evt);
        Assert.Equal(0, consumed);
        Assert.Null(evt);
    }

    [Theory]
    [InlineData('\t', ConsoleKey.Tab)]
    [InlineData('\r', ConsoleKey.Enter)]
    [InlineData('\n', ConsoleKey.Enter)]
    [InlineData(' ', ConsoleKey.Spacebar)]
    [InlineData('q', ConsoleKey.Q)]
    [InlineData('r', ConsoleKey.R)]
    [InlineData('n', ConsoleKey.N)]
    [InlineData('1', ConsoleKey.D1)]
    public void Plain_bytes_map_to_the_expected_key(char input, ConsoleKey expected)
    {
        var buffer = new[] { (byte)input };
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(1, consumed);
        var key = Assert.IsType<KeyEvent>(evt);
        Assert.Equal(expected, key.Key);
    }

    [Fact]
    public void Uppercase_letter_sets_shift_modifier()
    {
        var consumed = InputDecoder.TryDecode(new[] { (byte)'Q' }, out var evt);
        Assert.Equal(1, consumed);
        var key = Assert.IsType<KeyEvent>(evt);
        Assert.Equal(ConsoleKey.Q, key.Key);
        Assert.True(key.Shift);
    }

    [Fact]
    public void Bare_escape_emits_nothing_until_input_is_flushed()
    {
        var consumed = InputDecoder.TryDecode(new[] { (byte)0x1B }, out var evt);
        Assert.Equal(0, consumed);
        Assert.Null(evt);
    }

    [Fact]
    public void Bare_escape_emits_escape_key_after_flush()
    {
        var consumed = InputDecoder.TryDecode(new[] { (byte)0x1B }, out var evt, inputFlushed: true);
        Assert.Equal(1, consumed);
        Assert.Equal(new KeyEvent(ConsoleKey.Escape), evt);
    }

    [Theory]
    [InlineData('A', ConsoleKey.UpArrow)]
    [InlineData('B', ConsoleKey.DownArrow)]
    [InlineData('C', ConsoleKey.RightArrow)]
    [InlineData('D', ConsoleKey.LeftArrow)]
    public void Arrow_key_escape_sequences_parse_correctly(char finalChar, ConsoleKey expected)
    {
        var buffer = new[] { (byte)0x1B, (byte)'[', (byte)finalChar };
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(3, consumed);
        var key = Assert.IsType<KeyEvent>(evt);
        Assert.Equal(expected, key.Key);
    }

    [Fact]
    public void Shift_tab_escape_sequence_parses_with_shift_modifier()
    {
        var buffer = new[] { (byte)0x1B, (byte)'[', (byte)'Z' };
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(3, consumed);
        var key = Assert.IsType<KeyEvent>(evt);
        Assert.Equal(ConsoleKey.Tab, key.Key);
        Assert.True(key.Shift);
    }

    [Fact]
    public void Incomplete_csi_sequence_consumes_nothing()
    {
        var buffer = new[] { (byte)0x1B, (byte)'[' };
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(0, consumed);
        Assert.Null(evt);
    }

    [Fact]
    public void Sgr_mouse_press_parses_column_row_and_button()
    {
        var sequence = "\x1b[<0;12;7M";
        var buffer = Encoding.ASCII.GetBytes(sequence);
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(buffer.Length, consumed);
        var click = Assert.IsType<MouseClickEvent>(evt);
        Assert.Equal(11, click.Column);
        Assert.Equal(6, click.Row);
        Assert.Equal(MouseButton.Left, click.Button);
    }

    [Fact]
    public void Sgr_mouse_release_emits_no_event()
    {
        var buffer = Encoding.ASCII.GetBytes("\x1b[<0;12;7m");
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(buffer.Length, consumed);
        Assert.Null(evt);
    }

    [Fact]
    public void Sgr_mouse_incomplete_payload_returns_zero()
    {
        var buffer = Encoding.ASCII.GetBytes("\x1b[<0;12;7");
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(0, consumed);
        Assert.Null(evt);
    }

    [Theory]
    [InlineData(0, MouseButton.Left)]
    [InlineData(1, MouseButton.Middle)]
    [InlineData(2, MouseButton.Right)]
    [InlineData(4, MouseButton.Left)] // shift modifier + left
    public void Sgr_mouse_button_mask_is_decoded(int button, MouseButton expected)
    {
        var buffer = Encoding.ASCII.GetBytes($"\x1b[<{button};5;5M");
        InputDecoder.TryDecode(buffer, out var evt);
        var click = Assert.IsType<MouseClickEvent>(evt);
        Assert.Equal(expected, click.Button);
    }

    [Theory]
    [InlineData(8)]   // meta/alt + left
    [InlineData(16)]  // ctrl + left
    [InlineData(20)]  // shift + ctrl + left
    public void Sgr_mouse_modifier_bits_decode_to_left_click(int button)
    {
        var buffer = Encoding.ASCII.GetBytes($"\x1b[<{button};5;5M");
        InputDecoder.TryDecode(buffer, out var evt);
        var click = Assert.IsType<MouseClickEvent>(evt);
        Assert.Equal(MouseButton.Left, click.Button);
    }

    [Theory]
    [InlineData(17)]  // ctrl + middle
    [InlineData(18)]  // ctrl + right
    [InlineData(6)]   // shift + right
    public void Sgr_mouse_modifier_bits_do_not_corrupt_button(int button)
    {
        var buffer = Encoding.ASCII.GetBytes($"\x1b[<{button};5;5M");
        InputDecoder.TryDecode(buffer, out var evt);
        var click = Assert.IsType<MouseClickEvent>(evt);
        var expected = (button & 0b11) switch
        {
            1 => MouseButton.Middle,
            2 => MouseButton.Right,
            _ => MouseButton.Left,
        };
        Assert.Equal(expected, click.Button);
    }

    [Theory]
    [InlineData(64)]  // wheel up
    [InlineData(65)]  // wheel down
    [InlineData(66)]  // wheel left (rare)
    [InlineData(67)]  // wheel right (rare)
    [InlineData(68)]  // wheel + shift
    [InlineData(80)]  // wheel + ctrl
    public void Sgr_mouse_wheel_events_emit_no_click(int button)
    {
        var sequence = $"\x1b[<{button};5;5M";
        var buffer = Encoding.ASCII.GetBytes(sequence);
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(buffer.Length, consumed);
        Assert.Null(evt);
    }

    [Theory]
    [InlineData(35)]  // motion no-button (32 | 3 = "no button" in xterm)
    [InlineData(32)]  // motion + left
    [InlineData(33)]  // motion + middle
    [InlineData(34)]  // motion + right
    public void Sgr_mouse_motion_events_emit_no_click(int button)
    {
        var sequence = $"\x1b[<{button};5;5M";
        var buffer = Encoding.ASCII.GetBytes(sequence);
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(buffer.Length, consumed);
        Assert.Null(evt);
    }

    [Theory]
    [InlineData(128)] // extra button 8
    [InlineData(129)] // extra button 9
    public void Sgr_mouse_extra_buttons_emit_no_click(int button)
    {
        var sequence = $"\x1b[<{button};5;5M";
        var buffer = Encoding.ASCII.GetBytes(sequence);
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(buffer.Length, consumed);
        Assert.Null(evt);
    }

    [Theory]
    [InlineData('A', ConsoleKey.UpArrow)]
    [InlineData('B', ConsoleKey.DownArrow)]
    [InlineData('C', ConsoleKey.RightArrow)]
    [InlineData('D', ConsoleKey.LeftArrow)]
    public void Application_keypad_arrow_sequences_parse_correctly(char finalChar, ConsoleKey expected)
    {
        // Terminals that have "application cursor keys" enabled (DECCKM on)
        // send ESC O X instead of ESC [ X for the arrow keys. Before the
        // fix for issue #18 the decoder interpreted that as a bare Escape
        // followed by stray letters, which in turn closed the app via the
        // Q / Escape quit path.
        var buffer = new[] { (byte)0x1B, (byte)'O', (byte)finalChar };
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(3, consumed);
        var key = Assert.IsType<KeyEvent>(evt);
        Assert.Equal(expected, key.Key);
    }

    [Fact]
    public void Incomplete_ss3_sequence_consumes_nothing()
    {
        var buffer = new[] { (byte)0x1B, (byte)'O' };
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(0, consumed);
        Assert.Null(evt);
    }

    [Fact]
    public void Non_standard_escape_prefix_treated_as_escape_key()
    {
        var buffer = new[] { (byte)0x1B, (byte)'a' };
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(1, consumed);
        Assert.Equal(new KeyEvent(ConsoleKey.Escape), evt);
    }

    [Fact]
    public void Unknown_byte_returns_no_event_but_consumes_one()
    {
        var buffer = new byte[] { 0x01 };
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(1, consumed);
        Assert.Null(evt);
    }

    [Fact]
    public void Unknown_csi_final_still_advances()
    {
        var buffer = new[] { (byte)0x1B, (byte)'[', (byte)'X' };
        var consumed = InputDecoder.TryDecode(buffer, out var evt);
        Assert.Equal(3, consumed);
        Assert.Null(evt);
    }
}
