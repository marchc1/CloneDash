using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Nucleus.Common.Commands;

public struct TokenizedCommand
{
	const int COMMAND_MAX_ARGC = 64;
	const int COMMAND_MAX_LENGTH = 512;
	public static int MaxCommandLength => COMMAND_MAX_LENGTH - 1;

	int argCount;
	int strlen;

	char[]? argSBuffer;
	Range[] ppArgs;

	/// <summary>
	/// How many arguments are in the tokenized command? Note that this also contains the command itself. So a command
	/// executed with no arguments will return 1 here, for example.
	/// </summary>
	/// <returns></returns>
	public readonly int ArgC() => argCount;
	/// <summary>
	/// The argument buffer past the provided argument.
	/// </summary>
	/// <returns>All text, as a <see cref="ReadOnlySpan{char}"/> slice of the internal command buffer, after the provided arguments starting position (0 returning all text, 1 returning all after the initial command, etc..)</returns>
	public readonly ReadOnlySpan<char> ArgS(int startingArg = 1, ReadOnlySpan<char> def = default) {
		// Null/overflow checking
		if (argSBuffer == null)
			return def;
		if (argCount <= startingArg)
			return def;

		// Start at the first argument requested, and end at the last argument in ppArgs
		Index startIdx = ppArgs[startingArg].Start;
		Index endIdx = ppArgs[argCount - 1].End;

		return argSBuffer.AsSpan()[startIdx..endIdx];
	}

	/// <summary>
	/// Gets a single index from the command, and attempts to convert it to a 32-bit integer.
	/// </summary>
	/// <param name="index">A zero-indexed argument, zero will return the command name, and one is the start of the command arguments.</param>
	public readonly int Arg(int index, int def = default) {
		if (int.TryParse(Arg(index), null, out int r))
			return r;
		return def;
	}

	/// <summary>
	/// Gets a single index from the command, and attempts to convert it to a float.
	/// </summary>
	/// <param name="index">A zero-indexed argument, zero will return the command name, and one is the start of the command arguments.</param>
	public readonly float Arg(int index, float def = default) {
		if (float.TryParse(Arg(index), null, out float r))
			return r;
		return def;
	}

	/// <summary>
	/// Gets a single index from the command, and attempts to convert it to a double.
	/// </summary>
	/// <param name="index">A zero-indexed argument, zero will return the command name, and one is the start of the command arguments.</param>
	public readonly double Arg(int index, double def = default) {
		if (int.TryParse(Arg(index), null, out int r))
			return r;
		return def;
	}

	/// <summary>
	/// Gets a single index from the command.
	/// </summary>
	/// <param name="index">A zero-indexed argument, zero will return the command name, and one is the start of the command arguments.</param>
	/// <returns></returns>
	public readonly ReadOnlySpan<char> Arg(int index) {
		if (argSBuffer == null)
			return [];

		if (index < 0 || index >= argCount)
			return [];

		Range range = ppArgs[index];
		int start = range.Start.Value, end = range.End.Value;

		if (start < 0)
			start = 0;
		if (end >= argSBuffer.Length)
			end = argSBuffer.Length - 1;

		return argSBuffer.AsSpan()[start..end];
	}

	public readonly Span<char> GetCommandStringForWrite() => argSBuffer;
	public readonly ReadOnlySpan<char> GetCommandString() => argSBuffer;

	public readonly void CopyTo(Span<char> target) {
		ArgS(0).CopyTo(target);
	}

	[MemberNotNull(nameof(argSBuffer))]
	[MemberNotNull(nameof(ppArgs))]
	public void Reset() {
		argCount = 0;
		strlen = 0;
		argSBuffer ??= new char[COMMAND_MAX_LENGTH];
		ppArgs ??= new Range[COMMAND_MAX_ARGC];
		for (int i = 0; i < COMMAND_MAX_LENGTH; i++) {
			argSBuffer[i] = '\0';
		}
		for (int i = 0; i < ppArgs.Length; i++) {
			ppArgs[i] = new Range(0, 0);
		}
	}

	public readonly ReadOnlySpan<char> this[int index] {
		get => Arg(index);
	}

	public bool Tokenize(ReadOnlySpan<char> command) {
		Reset();

		command.CopyTo(argSBuffer.AsSpan()[..command.Length]);
		strlen = command.Length;

		int start = 0;
		int argIdx = 0;
		ReadOnlySpan<char> substr = command;

		while (!substr.IsEmpty && argIdx < COMMAND_MAX_ARGC) {
			int endIndex = substr.IndexOf(' ');
			if (endIndex == -1)
				endIndex = substr.Length;

			ppArgs[argIdx++] = new(start, start + endIndex);
			start += endIndex;

			if (start >= command.Length)
				break;

			start++;
			substr = command[start..];
		}

		argCount = argIdx;

		return true;
	}

	public readonly ReadOnlySpan<char> FindArg(ReadOnlySpan<char> name) {
		for (int i = 1; i < argCount; i++) {
			if (Arg(i).Equals(name, StringComparison.OrdinalIgnoreCase))
				return (i + 1) < argCount ? Arg(i + 1) : "";
		}
		return null;
	}
}