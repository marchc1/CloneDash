namespace Nucleus
{
	public static class ThreadSystem
	{
		public static Thread SpawnBackgroundWorker(string name, Action action) {
			Thread t = new(() => action());
			t.IsBackground = true;
			t.Name = name;
			t.Start();
			return t;
		}
		public static Thread SpawnBackgroundWorker(Action action) {
			Thread t = SpawnBackgroundWorker("", action);
			t.Name = $"Nucleus AnonymousBackgroundThread #{action.GetHashCode():X4}";
			return t;
		}
	}
}
