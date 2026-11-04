namespace Carandev.WhatsApp.Sdk.Tests.Fakes;

/// <summary>
/// Wraps a byte array but reports <see cref="CanSeek"/> as <see langword="false"/> and throws on
/// <see cref="Length"/>/<see cref="Position"/> access, simulating a genuinely non-seekable source
/// stream (e.g. a network response body) for tests exercising the buffered-validation path in
/// <c>WhatsAppMediaClient.UploadAsync(Stream, ...)</c>.
/// </summary>
internal sealed class NonSeekableStream : Stream
{
    private readonly MemoryStream _inner;

    public NonSeekableStream(byte[] content)
    {
        _inner = new MemoryStream(content, writable: false);
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _inner.ReadAsync(buffer, cancellationToken);

    public override void Flush() => _inner.Flush();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
