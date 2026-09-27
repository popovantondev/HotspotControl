using System.Threading;

namespace HotspotControl.App;

public sealed class InstanceCoordinator : IDisposable
{
    private Mutex? instance;
    private EventWaitHandle? activation;
    private RegisteredWaitHandle? activationWait;
    private bool ownsInstance;
    public bool IsFirst => ownsInstance;

    private InstanceCoordinator() { }

    public static InstanceCoordinator Acquire(string prefix)
    {
        var coordinator = new InstanceCoordinator();
        try
        {
            coordinator.instance = new Mutex(true, prefix + ".Instance", out coordinator.ownsInstance);
            coordinator.activation = new EventWaitHandle(false, EventResetMode.AutoReset, prefix + ".Activate");
            return coordinator;
        }
        catch
        {
            coordinator.Dispose();
            throw;
        }
    }

    public void NotifyFirst() => activation?.Set();

    public void Listen(Action restore)
    {
        if (!IsFirst || activation is null || activationWait is not null)
            throw new InvalidOperationException("Invalid instance listener state");
        activationWait = ThreadPool.RegisterWaitForSingleObject(activation, (_, _) => restore(),
            null, Timeout.Infinite, false);
    }

    public void Dispose()
    {
        activationWait?.Unregister(null);
        activationWait = null;
        activation?.Dispose(); activation = null;
        if (ownsInstance) instance?.ReleaseMutex();
        ownsInstance = false;
        instance?.Dispose(); instance = null;
    }
}
