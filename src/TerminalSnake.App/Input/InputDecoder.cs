using System.Text;

namespace TerminalSnake.Input;

public static class InputDecoder
{
    private const byte Esc = 0x1B;
    private const byte Csi = (byte)'[';
    private const byte Ss3 = (byte)'O';
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
        if (buffer.Length == 1)
        {
            return TryEmitBareEscape(inputFlushed, out decoded);
        }
        return buffer[1] switch
        {
            Csi => DecodeCsiOrWait(buffer, out decoded),
            Ss3 => DecodeSs3OrWait(buffer, out decoded),
            _ => EmitBareEscape(out decoded),
        };
    }

    private static int DecodeCsiOrWait(ReadOnlySpan<byte> buffer, out InputEvent? decoded)
    {
        if (buffer.Length == 2)
        {
            decoded = null;
            return 0;
        }
        return DecodeCsiSequence(buffer, out decoded);
    }

    private static int DecodeSs3OrWait(ReadOnlySpan<byte> buffer, out InputEvent? decoded)
    {
        // SS3 is the "application cursor keys" form many terminals emit for
        // the arrow keys (ESC O A/B/C/D). Treating them as ESC + letter
        // collapses to a "Q quit" miss-route that closed the whole app on
        // the first arrow press — see issue #18.
        if (buffer.Length == 2)
        {
            decoded = null;
            return 0;
        }
        decoded = DecodeSs3Final(buffer[2]);
        return 3;
    }

    private static int EmitBareEscape(out InputEvent? decoded)
    {
        decoded = new KeyEvent(ConsoleKey.Escape);
        return 1;
    }

    private static InputEvent? DecodeSs3Final(byte value) => value switch
    {
        (byte)'A' => new KeyEvent(ConsoleKey.UpArrow),
        (byte)'B' => new KeyEvent(ConsoleKey.DownArrow),
        (byte)'C' => new KeyEvent(ConsoleKey.RightArrow),
        (byte)'D' => new KeyEvent(ConsoleKey.LeftArrow),
        _ => null,
    };

    private static int TryEmitBareEscape(bool inputFlushed, out InputEvent? decoded)
    {
        if (inputFlushed)
        {
            decoded = new KeyEvent(ConsoleKey.Escape);
            return 1;
        }
        decoded = null;
        return 0;
    }

    private static int DecodeCsiSequence(ReadOnlySpan<byte> buffer, out InputEvent? decoded)
    {
        if (buffer[2] == Sgr)
        {
            return DecodeSgrMouse(buffer, out decoded);
        }
        decoded = DecodeCsiFinal(buffer[2]);
        return 3;
    }

    private static InputEvent? DecodePlainByte(byte value) =>
        DecodeControlByte(value) ?? DecodeAsciiByte(value);

    private static InputEvent? DecodeControlByte(byte value) => value switch
    {
        (byte)'\t' => new KeyEvent(ConsoleKey.Tab),
        (byte)'\r' => new KeyEvent(ConsoleKey.Enter),
        (byte)'\n' => new KeyEvent(ConsoleKey.Enter),
        (byte)' ' => new KeyEvent(ConsoleKey.Spacebar),
        _ => null,
    };

    private static InputEvent? DecodeAsciiByte(byte value)
    {
        if (IsLowerAsciiLetter(value))
        {
            return new KeyEvent((ConsoleKey)(value - 0x20));
        }
        if (IsUpperAsciiLetter(value))
        {
            return new KeyEvent((ConsoleKey)value, Shift: true);
        }
        if (IsAsciiDigit(value))
        {
            return new KeyEvent((ConsoleKey)value);
        }
        return null;
    }

    private static bool IsLowerAsciiLetter(byte value) =>
        value >= (byte)'a' && value <= (byte)'z';

    private static bool IsUpperAsciiLetter(byte value) =>
        value >= (byte)'A' && value <= (byte)'Z';

    private static bool IsAsciiDigit(byte value) =>
        value >= (byte)'0' && value <= (byte)'9';

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
        if (!isPress || !TryParseSgrTriplet(payload, out var button, out var column, out var row))
        {
            return null;
        }
        var mapped = MapButton(button);
        return mapped is null ? null : new MouseClickEvent(column - 1, row - 1, mapped.Value);
    }

    private static bool TryParseSgrTriplet(string payload, out int button, out int column, out int row)
    {
        button = column = row = 0;
        var parts = payload.Split(';');
        if (parts.Length != 3)
        {
            return false;
        }
        return int.TryParse(parts[0], out button)
            && int.TryParse(parts[1], out column)
            && int.TryParse(parts[2], out row);
    }

    // SGR-1006 button code layout (xterm):
    //   bits 0-1: button (0=Left, 1=Middle, 2=Right, 3=release-with-no-button)
    //   bit  2 (4) : Shift modifier
    //   bit  3 (8) : Meta/Alt modifier
    //   bit  4 (16): Ctrl modifier
    //   bit  5 (32): motion flag (sent while a button is held during drag)
    //   bit  6 (64): wheel event (button=64 wheel-up, 65 wheel-down)
    //   bit  7 (128): "extra" buttons 8-11
    // We only surface plain button presses; motion, wheel, and extra
    // buttons collapsing into Left clicks was issue #48.
    private const int MotionFlag = 32;
    private const int WheelFlag = 64;
    private const int ExtraButtonFlag = 128;
    private const int NonClickMask = MotionFlag | WheelFlag | ExtraButtonFlag;

    private static MouseButton? MapButton(int raw)
    {
        if ((raw & NonClickMask) != 0)
        {
            return null;
        }
        return (raw & 0b11) switch
        {
            0 => MouseButton.Left,
            1 => MouseButton.Middle,
            2 => MouseButton.Right,
            _ => null,
        };
    }
}
