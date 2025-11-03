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

		private static List<MainThreadExecutionTask> Callbacks { get; } = [];
		private static ConcurrentQueue<MainThreadExecutionTask> Actions { get; } = [];

		public static void RunASAP(Action a, ThreadExecutionTime when = ThreadExecutionTime.BeforeFrame) => Actions.Enqueue(new(a, when));
		public static void AddCallback(Action a, ThreadExecutionTime when = ThreadExecutionTime.BeforeFrame) => Callbacks.Add(new(a, when));

		static List<MainThreadExecutionTask> putBack = [];
		public static void Run(ThreadExecutionTime when) {
			lock (Actions) {
				putBack.Clear();

				while (Actions.TryDequeue(out var task)) {
					if (task.When == when)
						task.Action();
					else
						putBack.Add(task);
				}

				for (int i = 0, c = putBack.Count; i < c; i++)
					Actions.Enqueue(putBack[i]);
				putBack.Clear();

				for (int i = 0, c = Callbacks.Count; i < c; i++) {
					var callback = Callbacks[i];
					if (callback.When == when)
						callback.Action();
				}
			}
		}
	}
}