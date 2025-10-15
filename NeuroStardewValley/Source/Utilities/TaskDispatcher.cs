using System.Collections.Concurrent;

namespace NeuroStardewValley.Source.Utilities;

/// <summary>
/// This is here to replicate UniTask's SwitchToMainThread
/// </summary>
internal static class TaskDispatcher
{
	private static readonly ConcurrentQueue<Action> Actions = new();
	private static int _mainThreadId;

	public static void Initialize()
	{
		_mainThreadId = Environment.CurrentManagedThreadId;
	}

	public static void RunPending()
	{
		while (Actions.TryDequeue(out var action)) action();
	}

	private static bool IsMainThread => Environment.CurrentManagedThreadId == _mainThreadId;

	private static void Post(Action action)
	{
		Actions.Enqueue(action);
	}

	/// <summary>
	/// Anything after this will be run on the main thread, async methods will only switch a thread pool thread after
	/// something has been awaited. If it is already running on the main thread nothing will change. 
	/// </summary>
	public static Task SwitchToMainThread()
	{
		if (IsMainThread) return Task.CompletedTask;

		var tcs = new TaskCompletionSource();
		Post(() => tcs.SetResult());
		return tcs.Task;
	}
}