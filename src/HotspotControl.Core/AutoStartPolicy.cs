namespace HotspotControl.Core;

public sealed record StartAttempt(bool Success, bool Retryable, string Message);

public static class AutoStartPolicy
{
    public static async Task<StartAttempt> RunAsync(
        Func<CancellationToken, Task<StartAttempt>> attempt,
        Func<TimeSpan, CancellationToken, Task> delay,
        Action<int>? waiting = null,
        int maximumAttempts = 12,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumAttempts, 1);
        StartAttempt result = new(false, false, "Automatischer Start nicht ausgeführt.");
        for (int number = 1; number <= maximumAttempts; number++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result = await attempt(cancellationToken);
            if (result.Success || !result.Retryable || number == maximumAttempts) return result;
            waiting?.Invoke(number);
            await delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
        return result;
    }
}
