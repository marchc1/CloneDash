using System.Diagnostics;

namespace CloneDash.Game;

public class ProfilerResult : IDisposable
{
	public string Identifier = "???";
	public Stopwatch Timer = new();
	public ProfilerResult? Parent;
	public List<ProfilerResult> Children = [];

	public ProfilerResult() {
		Timer.Start();
	}

	public void Stop() {
		Timer.Stop();
	}

	List<string> WriteToStringArray(List<string> input, int iteration = 0) {
		input.Add($"{new string(' ', iteration * 4)} > {Identifier}: {Timer.Elapsed.TotalMilliseconds:F4}ms");
		foreach (var child in Children) {
			child.WriteToStringArray(input, iteration + 1);
		}
		return input;
	}

	public string[] ToStringArray() => [.. WriteToStringArray([])];

	/// <summary>
	/// Releases the profiler result.
	/// </summary>
	public void End() {
		Debug.Assert(CD_StaticSequentialProfiler.Profiling);
		Debug.Assert(CD_StaticSequentialProfiler.CurrentStackFrame == this);
		CD_StaticSequentialProfiler.EndStackFrame();
	}

	public void Dispose() {
		End();
	}
}
public class ProfilerAccumulator : IDisposable
{
	public Stopwatch Timer = new();
	public int Calls { get; private set; } = 0;
	public ProfilerAccumulator() { }

	public void Dispose() {
		Timer.Stop();
	}

	public void Start() {
		Timer.Start();
		Calls++;
	}
}

public static class CD_StaticSequentialProfiler
{
	private static ProfilerResult? currentStackFrame;
	public static ProfilerResult CurrentStackFrame => currentStackFrame ?? throw new Exception("Please start the sequential profiler");

	private static Dictionary<string, ProfilerAccumulator> accumulators = [];

	public static void Start() {
		Debug.Assert(currentStackFrame == null);
		accumulators.Clear();
		currentStackFrame = new();
		currentStackFrame.Identifier = "Root";
	}

	public static void End(out ProfilerResult stack, out List<KeyValuePair<string, ProfilerAccumulator>> accumulators) {
		Debug.Assert(currentStackFrame != null && currentStackFrame.Parent == null);

		currentStackFrame.Stop();
		var r = currentStackFrame;
		currentStackFrame = null;

		stack = r;
		accumulators = CD_StaticSequentialProfiler.accumulators.ToList();
	}

	public static ProfilerAccumulator AccumulateTime(string key) {
		if (!accumulators.TryGetValue(key, out var accumulator)) {
			accumulator = new();
			accumulators[key] = accumulator;
		}

		accumulator.Start();
		return accumulator;
	}

	public static ProfilerResult? StartStackFrame(string name) {
		if (currentStackFrame == null) return null;

		var stack = new ProfilerResult();
		stack.Identifier = name;

		stack.Parent = currentStackFrame;
		currentStackFrame.Children.Add(stack);

		currentStackFrame = stack;
		return stack;
	}

	public static ProfilerResult EndStackFrame() {
		if (currentStackFrame == null) throw new Exception("Cannot stop; nothing to stop!");
		currentStackFrame.Stop();
		var r = currentStackFrame;
		currentStackFrame = currentStackFrame.Parent;
		return r;
	}

	public static bool Profiling => currentStackFrame != null;
}
