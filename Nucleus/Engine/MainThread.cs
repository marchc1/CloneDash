using System.Collections.Concurrent;

namespace Nucleus
{
	public enum ThreadExecutionTime
	{
		/// <summary>
		/// The function will run before any frame-code.
		/// </summary>
		BeforeFrame,
		/// <summary>
		/// The function will run after a FrameState is constructed, and before <see cref="Level.Think"/> happens.
		/// </summary>
		AfterFrameStateConstructed,
		/// <summary>
		/// The function will run after <see cref="Level.Think"/> executes.
		/// </summary>
		AfterThink,
		/// <summary>
		/// The function will run after frame-code is complete.
		/// </summary>
		AfterFrame
	}
	public record MainThreadExecutionTask(Action Action, ThreadExecutionTime When);

	/// <summary>
	/// A way to run parameterless and returnless methods/anonymous delegates on the main thread.
	/// </summary>
	public static class MainThread
	{
		private static Thread? _thread = null;
		private static Thread? _gamethread = null;
		public static bool ThreadSet => _thread != null;
		public static bool GameThreadSet => _gamethread != null;
		public static bool Initialized => _thread != null && _gamethread != null;

		public static Thread Thread {
			get => _thread ?? throw new Exception("For some reason, the MainThread.Thread property was accessed before it was set. EngineCore initialization sets this variable.");
			set {
				if (_thread != null)
					throw new Exception("The MainThread.Thread property can not be set again.");
				_thread = value;
				_thread.Name = "Nucleus - Main Thread";
			}
		}

		public static Thread GameThread {
			get => _gamethread ?? throw new Exception("For some reason, the MainThread.GameThread property was accessed before it was set. EngineCore initialization sets this variable.");
			set {
				if (_gamethread != null)
					throw new Exception("The MainThread.GameThread property can not be set again.");
				_gamethread = value;
				_gamethread.Name = "Nucleus - Game Thread";
			}
		}

		private static ConcurrentBag<MainThreadExecutionTask> Callbacks { get; } = [];
		private static ConcurrentQueue<MainThreadExecutionTask> Actions { get; } = [];

		public static void RunASAP(Action a, ThreadExecutionTime when = ThreadExecutionTime.BeforeFrame) => Actions.Enqueue(new(a, when));
		public static void AddCallback(Action a, ThreadExecutionTime when = ThreadExecutionTime.BeforeFrame) => Callbacks.Add(new(a, when));

		public static void Run(ThreadExecutionTime when) {
			lock (Actions) {
				List<MainThreadExecutionTask> putBack = [];

				while (Actions.TryDequeue(out var task)) {
					if (task.When == when)
						task.Action();
					else
						putBack.Add(task);
				}

				foreach (var task in putBack)
					Actions.Enqueue(task);

				foreach (var callback in Callbacks) {
					if (callback.When == when)
						callback.Action();
				}
			}
		}
	}
}