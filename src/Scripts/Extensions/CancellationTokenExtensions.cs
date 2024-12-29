using System.Threading;
using System.Threading.Tasks;

namespace TD.Extensions;

public static class CancellationTokenExtensions
{
    /// Creates a task that completes when the cancellation token is canceled
    public static Task AsTask(this CancellationToken cancellationToken)
    {
        TaskCompletionSource tcs = new();
        cancellationToken.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);
        return tcs.Task;
    }
}