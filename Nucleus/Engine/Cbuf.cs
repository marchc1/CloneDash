using Nucleus.Common.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Nucleus.Engine;

public class CmdAlias
{
	public static CmdAlias? Head;

	public CmdAlias? Next;
	public readonly char[] Name = new char[Cbuf.MAX_ALIAS_NAME];
	public string? Value;
}

public class CommandBuffer
{
	public const int ARGS_BUFFER_LENGTH = 2 << 13;

	public class Command
	{
		public int FirstArgS;
		public int BufferSize;
		public Command? Next;
	}

	readonly char[] Buffer = new char[ARGS_BUFFER_LENGTH];
	readonly LinkedList<Command> Commands = [];

	int WritePos;
	int ReadPos;
	int UsedSpace;
	bool ProcessingCommands;

	TokenizedCommand CurrentCommand;

	public int GetCapacity() => ARGS_BUFFER_LENGTH;
	public int GetFreeSpace() => ARGS_BUFFER_LENGTH - UsedSpace;
	public int GetUsedSpace() => UsedSpace;

	public bool IsProcessingCommands() => ProcessingCommands;


	public void BeginProcessingCommands() {
		Debug.Assert(!ProcessingCommands);
		ProcessingCommands = true;
	}

	public void EndProcessingCommands() {
		Debug.Assert(ProcessingCommands);
		ProcessingCommands = false;
	}

	public ref TokenizedCommand GetCommand() => ref CurrentCommand;


	public ReadOnlySpan<char> FirstTextSlice(ReadOnlySpan<char> message) {
		int indexOf = message.IndexOf(';'); // command delimiter
		if (indexOf == -1)
			return message; // allow ; to be missing the first slice
		return message[..indexOf];
	}

	public ReadOnlySpan<char> NextTextSlice(ReadOnlySpan<char> message) {
		int indexOf = message.IndexOf(';'); // command delimiter
		if (indexOf == -1)
			return default; // no remaining delimiter
		return message[..indexOf];
	}

	public bool AddText(ReadOnlySpan<char> text) {
		for (ReadOnlySpan<char> slice = FirstTextSlice(text); !slice.IsEmpty; slice = NextTextSlice(slice))
			if (!InsertCommand(slice))
				return false;

		return true;
	}

	public bool InsertCommand(ReadOnlySpan<char> command) {
		int len = command.Length;

		if (len > GetCapacity()) {
			Logs.Warn("CommandBuffer.AddText: command too long, ignoring!");
			return false;
		}

		if (len > GetFreeSpace()) {
			Logs.Warn("CommandBuffer.AddText: buffer overflow");
			return false;
		}

		int start = WritePos;

		if (len > 0) {
			int firstChunk = Math.Min(len, GetCapacity() - WritePos);
			command[..firstChunk].CopyTo(Buffer.AsSpan(WritePos, firstChunk));

			if (firstChunk < len)
				command[firstChunk..].CopyTo(Buffer.AsSpan(0, len - firstChunk));

			WritePos = (WritePos + len) % GetCapacity();
			UsedSpace += len;
		}

		Command next = new() { FirstArgS = start, BufferSize = len };
		Commands.AddLast(next);

		return true;
	}

	public LinkedListNode<Command>? GetNextCommandHandle() {
		Debug.Assert(ProcessingCommands);
		return Commands.First;
	}

	public bool DequeueNextCommand() {
		if (Commands.First == null)
			return false;

		Command nextCommand = Commands.First.Value;
		Commands.RemoveFirst();

		Span<char> destination = stackalloc char[nextCommand.BufferSize];

		if (nextCommand.BufferSize > 0) {
			int firstChunk = Math.Min(nextCommand.BufferSize, ARGS_BUFFER_LENGTH - nextCommand.FirstArgS);

			Buffer.AsSpan(nextCommand.FirstArgS, firstChunk).CopyTo(destination[..firstChunk]);

			if (firstChunk < nextCommand.BufferSize)
				Buffer.AsSpan(0, nextCommand.BufferSize - firstChunk).CopyTo(destination[firstChunk..]);

			ReadPos = (ReadPos + nextCommand.BufferSize) % ARGS_BUFFER_LENGTH;
			UsedSpace -= nextCommand.BufferSize;

			CurrentCommand.Tokenize(destination);

			return true;
		}

		return false;
	}
}

public static class Cbuf
{
	public const int MAX_ALIAS_NAME = 32;
	public const int MAX_COMMAND_LENGTH = 1024;
	public static readonly CommandBuffer CommandBuffer = new CommandBuffer();
	public static readonly object BufferLock = new();

	public static void ExecuteCommand(ref TokenizedCommand command) {
		Cmd.ExecuteCommand(in command);
	}

	public static void AddText(ReadOnlySpan<char> text) {
		lock (BufferLock) {
			if (!CommandBuffer.AddText(text))
				Logs.Warn("CBuf.AddText: buffer overflow!!!");
		}
	}

	public static void InsertText(ReadOnlySpan<char> text) {
		lock (BufferLock) {
			Debug.Assert(CommandBuffer.IsProcessingCommands());
			AddText(text);
		}
	}

	public static void Execute() {
		lock (BufferLock) {
			CommandBuffer.BeginProcessingCommands();
			while (CommandBuffer.DequeueNextCommand())
				ExecuteCommand(ref CommandBuffer.GetCommand());
			CommandBuffer.EndProcessingCommands();
		}
	}
}
