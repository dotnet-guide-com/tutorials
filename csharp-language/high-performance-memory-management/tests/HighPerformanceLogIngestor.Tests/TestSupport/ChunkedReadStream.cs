namespace HighPerformanceLogIngestor.Tests.TestSupport;

internal sealed class ChunkedReadStream(
    byte[] data,
    int maximumChunkSize) :
    Stream
{
    private int _position;

    public override bool CanRead =>
        true;

    public override bool CanSeek =>
        false;

    public override bool CanWrite =>
        false;

    public override long Length =>
        data.Length;

    public override long Position
    {
        get =>
            _position;

        set =>
            throw new NotSupportedException();
    }

    public override int Read(
        byte[] buffer,
        int offset,
        int count)
    {
        ArgumentNullException
            .ThrowIfNull(
                buffer);

        return ReadCore(
            buffer.AsSpan(
                offset,
                count));
    }

    public override int Read(
        Span<byte> buffer) =>
            ReadCore(
                buffer);

    public override ValueTask<int>
        ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken =
                default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        return ValueTask.FromResult(
            ReadCore(
                buffer.Span));
    }

    public override void Flush()
    {
    }

    public override long Seek(
        long offset,
        SeekOrigin origin) =>
            throw new NotSupportedException();

    public override void SetLength(
        long value) =>
            throw new NotSupportedException();

    public override void Write(
        byte[] buffer,
        int offset,
        int count) =>
            throw new NotSupportedException();

    private int ReadCore(
        Span<byte> destination)
    {
        if (_position
            >= data.Length)
        {
            return 0;
        }

        int count =
            Math.Min(
                maximumChunkSize,
                Math.Min(
                    destination.Length,
                    data.Length
                    - _position));

        data.AsSpan(
                _position,
                count)
            .CopyTo(
                destination);

        _position +=
            count;

        return count;
    }
}

internal sealed class BlockingReadStream :
    Stream
{
    public override bool CanRead =>
        true;

    public override bool CanSeek =>
        false;

    public override bool CanWrite =>
        false;

    public override long Length =>
        throw new NotSupportedException();

    public override long Position
    {
        get =>
            throw new NotSupportedException();

        set =>
            throw new NotSupportedException();
    }

    public override int Read(
        byte[] buffer,
        int offset,
        int count) =>
            throw new NotSupportedException();

    public override async ValueTask<int>
        ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken =
                default)
    {
        await Task.Delay(
            Timeout.InfiniteTimeSpan,
            cancellationToken);

        return 0;
    }

    public override void Flush()
    {
    }

    public override long Seek(
        long offset,
        SeekOrigin origin) =>
            throw new NotSupportedException();

    public override void SetLength(
        long value) =>
            throw new NotSupportedException();

    public override void Write(
        byte[] buffer,
        int offset,
        int count) =>
            throw new NotSupportedException();
}