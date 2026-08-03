namespace ResilientOrdersMinimal.Health;

public sealed class TrafficReadinessState
{
    private int _acceptingTraffic =
        1;

    public bool IsAcceptingTraffic =>
        Volatile.Read(
            ref _acceptingTraffic)
        == 1;

    public void BeginDrain()
    {
        Interlocked.Exchange(
            ref _acceptingTraffic,
            0);
    }
}