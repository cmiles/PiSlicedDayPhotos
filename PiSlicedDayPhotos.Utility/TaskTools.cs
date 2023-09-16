namespace PiSlicedDayPhotos.Utility;

public static class TaskTools
{
    public static async Task WhenCancelled(this CancellationToken cancellationToken)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();

        await using (cancellationToken.Register(() => { taskCompletionSource.TrySetResult(true); }))
        {
            await taskCompletionSource.Task;
        }
    }
}