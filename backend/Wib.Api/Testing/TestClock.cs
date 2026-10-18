namespace Wib.Api.Testing;

public sealed class TestClock : TimeProvider
{
    private long _offsetTicks;

    public override DateTimeOffset GetUtcNow() => System.GetUtcNow().AddTicks(Interlocked.Read(ref _offsetTicks));

    public void SetUtcNow(DateTimeOffset utcNow) =>
        Interlocked.Exchange(ref _offsetTicks, (utcNow - System.GetUtcNow()).Ticks);

    public void Reset() => Interlocked.Exchange(ref _offsetTicks, 0);
}
