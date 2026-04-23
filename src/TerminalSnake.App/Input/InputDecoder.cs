using System.Text;

namespace TerminalSnake.Input;

public static class InputDecoder
{
    private const byte Esc = 0x1B;
    private const byte Csi = (byte)'[';
    private const byte Sgr = (byte)'<';

    public static int TryDecode(ReadOnlySpan<byte> buffer, out InputEvent? decoded, bool inputFlushed = false)
    {
        decoded = null;
        if (buffer.Length == 0)
        {
            return 0;
        }
        if (buffer[0] != Esc)
        {
            decoded = DecodePlainByte(buffer[0]);
            return 1;
        }
        return DecodeEscapeSequence(buffer, inputFlushed, out decoded);
    }

    private static int DecodeEscapeSequence(ReadOnlySpan<byte> buffer, bool inputFlushed, out InputEvent? decoded)
    {
        decoded = null;
        if (buffer.Length == 1)
        {
            if (!inputFlushed)
            {
                return 0;
            }
            decoded = new KeyEvent(ConsoleKey.Escape);
            return 1;
        }
        if (buffer[1] != Csi)
        {
            decoded = new KeyEvent(ConsoleKey.Escape);
            return 1;
        }
        if (buffer.Length == 2)
        {
            return 0;
        }
        if (buffer[2] == Sgr)
        {
            return DecodeSgrMouse(buffer, out decoded);
        }
        decoded = DecodeCsiFinal(buffer[2]);
        return decoded is null ? 3 : 3;
    }

    private static InputEvent? DecodePlainByte(byte value) => value switch
    {
        (byte)'\t' => new KeyEvent(ConsoleKey.Tab),
        (byte)'\r' or (byte)'\n' => new KeyEvent(ConsoleKey.Enter),
        (byte)' ' => new KeyEvent(ConsoleKey.Spacebar),
        _ when value is >= (byte)'a' and <= (byte)'z' => new KeyEvent((ConsoleKey)(value - 0x20)),
        _ when value is >= (byte)'A' and <= (byte)'Z' => new KeyEvent((ConsoleKey)value, Shift: true),
        _ when value is >= (byte)'0' and <= (byte)'9' => new KeyEvent((ConsoleKey)value),
        _ => null,
    };

    private static InputEvent? DecodeCsiFinal(byte value) => value switch
    {
        (byte)'A' => new KeyEvent(ConsoleKey.UpArrow),
        (byte)'B' => new KeyEvent(ConsoleKey.DownArrow),
        (byte)'C' => new KeyEvent(ConsoleKey.RightArrow),
        (byte)'D' => new KeyEvent(ConsoleKey.LeftArrow),
        (byte)'Z' => new KeyEvent(ConsoleKey.Tab, Shift: true),
        _ => null,
    };

    private static int DecodeSgrMouse(ReadOnlySpan<byte> buffer, out InputEvent? decoded)
    {
        for (var i = 3; i < buffer.Length; i++)
        {
            var terminator = buffer[i];
            if (terminator == (byte)'M' || terminator == (byte)'m')
            {
                var payload = Encoding.ASCII.GetString(buffer.Slice(3, i - 3));
                decoded = BuildMouseEvent(payload, isPress: terminator == (byte)'M');
                return i + 1;
            }
        }
        decoded = null;
        return 0;
    }

    private static MouseClickEvent? BuildMouseEvent(string payload, bool isPress)
    {
        if (!isPress)
        {
            return null;
        }
        var parts = payload.Split(';');
        if (parts.Length != 3)
        {
            return null;
        }
        if (!int.TryParse(parts[0], out var button) ||
            !int.TryParse(parts[1], out var column) ||
            !int.TryParse(parts[2], out var row))
        {
            return null;
        }
        return new MouseClickEvent(column - 1, row - 1, MapButton(button));
    }

    private static MouseButton MapButton(int raw) => (raw & 0b11) switch
    {
        0 => MouseButton.Left,
        1 => MouseButton.Middle,
        2 => MouseButton.Right,
        _ => MouseButton.Left,
    };
}
