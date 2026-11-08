using System.Text;

namespace DevDashboard.Core.Middleware;

/// <summary>
/// Writes every byte through to the real response stream while keeping a bounded copy in memory.
/// The copy is decoded into a string once the request completes, so large downloads are never
/// buffered in full.
/// </summary>
internal sealed class ResponseCaptureStream : Stream
{
    private readonly Stream _inner;
    private readonly MemoryStream _capture = new();
    private readonly int _limitChars;
    private long _capturedBytes;

    public ResponseCaptureStream(Stream inner, int limitChars)
    {
        _inner = inner;
        _limitChars = Math.Max(0, limitChars);
    }

    /// <summary>UTF-8 bytes we are willing to keep; allows for multi-byte characters.</summary>
    private long ByteLimit => (long)_limitChars * 4;

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _inner.Write(buffer, offset, count);
        Capture(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _inner.Write(buffer);
        Capture(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        Capture(buffer.Span);
    }

    public override void Flush() => _inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    private void Capture(ReadOnlySpan<byte> buffer)
    {
        var room = ByteLimit - _capturedBytes;
        if (room <= 0) { _capturedBytes += buffer.Length; return; }
        var take = (int)Math.Min(room, buffer.Length);
        _capture.Write(buffer[..take]);
        _capturedBytes += buffer.Length;
    }

    /// <summary>Decoded, truncated body — or <see langword="null"/> when nothing was written.</summary>
    public string? GetCapturedText()
    {
        if (_capture.Length == 0) return null;
        var text = Encoding.UTF8.GetString(_capture.GetBuffer(), 0, (int)_capture.Length);
        if (text.Length > _limitChars) text = text[.._limitChars] + "…";
        return text;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
