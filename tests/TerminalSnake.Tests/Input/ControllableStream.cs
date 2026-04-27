using System.Threading.Channels;

namespace TerminalSnake.Tests.Input;

// Stream test double that lets the test push bytes from the outside and
// observe when the consumer has read them. Each PostBytes call enqueues a
// single chunk; ReadAsync awaits the next chunk (or EOF). This lets tests
// drive StdinInputLoop synchronously without timing on real stdin.
internal sealed class ControllableStream : Stream
{
    private readonly Channel<byte[]?> _chunks = Channel.CreateUnbounded<byte[]?>();
    private byte[]? _residual;
    private int _residualOffset;
    private TaskCompletionSource _drained = NewDrainedSignal();

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public void PostBytes(byte[] bytes)
    {
        _drained = NewDrainedSignal();
        _chunks.Writer.TryWrite(bytes);
    }

    public void SignalEof() => _chunks.Writer.TryWrite(null);

    // Resolves the next time the consumer has consumed every byte we posted.
    public Task WaitUntilDrainedAsync() => _drained.Task;

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_residual is null)
        {
            var chunk = await _chunks.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (chunk is null)
            {
                return 0;
            }
            _residual = chunk;
            _residualOffset = 0;
        }
        var available = _residual.Length - _residualOffset;
        var copy = Math.Min(available, buffer.Length);
        _residual.AsSpan(_residualOffset, copy).CopyTo(buffer.Span);
        _residualOffset += copy;
        if (_residualOffset == _residual.Length)
        {
            _residual = null;
            _residualOffset = 0;
            _drained.TrySetResult();
        }
        return copy;
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private static TaskCompletionSource NewDrainedSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
