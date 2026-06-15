namespace P2W.Cards.Infrastructure.Providers.Ebay;

public sealed class EbayRateLimiter
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private DateTime lastCallUtc = DateTime.MinValue;

    public async Task WaitAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var elapsed = DateTime.UtcNow - lastCallUtc;
            if (elapsed.TotalMilliseconds < 1000)
            {
                await Task.Delay(1000 - (int)elapsed.TotalMilliseconds, ct);
            }
            lastCallUtc = DateTime.UtcNow;
        }
        finally
        {
            gate.Release();
        }
    }
}
