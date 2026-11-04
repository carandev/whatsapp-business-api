namespace WhatsAppCloudApi.Tests.Fakes;

/// <summary>
/// A seekable stream that reports a given <see cref="Length"/> without allocating or holding any
/// backing content. Used to assert oversized-content rejection cheaply — the size-ceiling check
/// only reads <see cref="CanSeek"/>/<see cref="Length"/> before throwing, so no byte is ever read
/// from an instance used in a "throws before any HTTP call" assertion.
/// </summary>
internal sealed class SizedSeekableStream : Stream
{
    public SizedSeekableStream(long length)
    {
        Length = length;
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length { get; }

    public override long Position { get; set; }

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("This fake reports Length only; reading was not expected for this assertion.");

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Flush()
    {
    }
}
