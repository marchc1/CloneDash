using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nucleus.Common.Commands;

[EngineComponent]
public static class CommandLineGlobals
{
	[Dependency] public static ICommandLine g_CommandLine = null!;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ICommandLine CommandLine() => g_CommandLine;
}

public ref struct ParmInfo
{
	public ICommandLine? Cmd;
	public int Index;

	public ParmInfo(ICommandLine cmd, int index) {
		Cmd = cmd;
		Index = index;
	}

	public readonly ReadOnlySpan<char> this[int i] {
		get {
			if (Cmd == null)
				return null;

			string? value = Cmd.ParmValueByIndex(i + Index + 1);
			if (value == null)
				return null;

			return value;
		}
	}
}

public interface ICommandLine
{
	public void CreateCmdLine(ReadOnlySpan<char> commandLine);
	public string? GetCmdLine();

	public bool CheckParm(string name, out ParmInfo info);
	public void RemoveParm(string name);
	public void AppendParm(string name, string? values = null);

	[return: NotNullIfNotNull(nameof(defaultValue))] public string? ParmValue(string name, string? defaultValue = null);
	public int ParmValue(string name, int defaultValue);
	public float ParmValue(string name, float defaultValue);
	public double ParmValue(string name, double defaultValue);

	public int ParmCount();
	public int FindParm(ReadOnlySpan<char> name);
	public bool HasParm(ReadOnlySpan<char> name);
	public string GetParm(int index);
	public void SetParm(int index, string newParm);
	[return: NotNullIfNotNull(nameof(defaultValue))] public string? ParmValueByIndex(int index, string? defaultValue = null);
}