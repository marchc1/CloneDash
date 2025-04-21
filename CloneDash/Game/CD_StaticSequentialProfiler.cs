using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash.Game;

public class ProfilerResult {
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
		return input;
	}

	public string[] ToStringArray() => [.. WriteToStringArray([])];
}

public static class CD_StaticSequentialProfiler
{
	private static ProfilerResult? currentStackFrame;
	public static ProfilerResult CurrentStackFrame => currentStackFrame ?? throw new Exception("Please start the sequential profiler");

	public static void Start() {
		Debug.Assert(currentStackFrame == null);
		currentStackFrame = new();
		currentStackFrame.Identifier = "Root";
	}

	public static ProfilerResult End() {
		Debug.Assert(currentStackFrame != null && currentStackFrame.Parent == null);

		currentStackFrame.Stop();
		var r = currentStackFrame;
		currentStackFrame = null;
		return r;
	}

	public static void StartStackFrame(string name) {
		Debug.Assert(currentStackFrame != null);

		var stack = new ProfilerResult();
		stack.Identifier = name;

		stack.Parent = currentStackFrame;
		currentStackFrame.Children.Add(stack);

		currentStackFrame = stack;
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
