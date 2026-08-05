using System.Buffers;

namespace HighPerformanceLogIngestor.Tests.TestSupport;

internal static class SegmentedSequence
{
    public static ReadOnlySequence<byte>
        Create(
            params byte[][] segments)
    {
        ArgumentNullException
            .ThrowIfNull(
                segments);

        if (segments.Length
            == 0)
        {
            return ReadOnlySequence<byte>
                .Empty;
        }

        var first =
            new Segment(
                segments[0]);

        Segment last =
            first;

        for (int index = 1;
            index < segments.Length;
            index++)
        {
            last =
                last.Append(
                    segments[index]);
        }

        return new ReadOnlySequence<byte>(
            first,
            0,
            last,
            last.Memory.Length);
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        internal Segment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = next;

            return next;
        }
    }
}