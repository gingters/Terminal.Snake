using System.Runtime.InteropServices;

namespace TerminalSnake.Input;

public sealed class BufferedInputParser
{
    private readonly List<byte> _buffer = new();

    public IReadOnlyList<InputEvent> Feed(ReadOnlySpan<byte> incoming, bool inputFlushed = false)
    {
        for (var i = 0; i < incoming.Length; i++)
        {
            _buffer.Add(incoming[i]);
        }
        return Drain(inputFlushed);
    }

    public int PendingBytes => _buffer.Count;

    public void Reset() => _buffer.Clear();

    private List<InputEvent> Drain(bool inputFlushed)
    {
        var events = new List<InputEvent>();
        while (_buffer.Count > 0)
        {
            var span = CollectionsMarshal.AsSpan(_buffer);
            var consumed = InputDecoder.TryDecode(span, out var decoded, inputFlushed);
            if (consumed == 0)
            {
                break;
            }
            _buffer.RemoveRange(0, consumed);
            if (decoded is not null)
            {
                events.Add(decoded);
            }
        }
        return events;
    }
}
