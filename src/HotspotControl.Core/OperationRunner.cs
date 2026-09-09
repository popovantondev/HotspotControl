namespace HotspotControl.Core;

public sealed class OperationBusyException : Exception;

// A timeout stops waiting. The original operation keeps the lock until it ends.
public sealed class OperationRunner
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public bool IsBusy => gate.CurrentCount == 0;

    public async Task<T> RunAsync<T>(Func<Task<T>> operation, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!gate.Wait(0)) throw new OperationBusyException();
        var pending = Task.Run(async () =>
        {
            try { return await operation().ConfigureAwait(false); }
            finally { gate.Release(); }
        });
        // Observe a late failure even if the caller has already stopped waiting.
        _ = pending.ContinueWith(task => _ = task.Exception,
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return await pending.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
    }
}
