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
		public long Tick;
		public int FirstArgS;
		public int BufferSize;
	}

	char[] argsBuffer = new char[ARGS_BUFFER_LENGTH];
	int lastUsedArgSize;
	int argBufferSize;
	LinkedList<Command> Commands = [];
	long currentTick;
	long lastTickToProcess;
	long waitDelayTicks;
	LinkedListNode<Command>? nextCommand;
	int maxArgBufferLength;
	bool isProcessingCommands;
	bool waitEnabled;

	TokenizedCommand currentCommand;

	public unsafe bool DequeueNextCommand() {
		currentCommand.Reset();
		Debug.Assert(isProcessingCommands);
		if (Commands.Count == 0)
			return false;

		LinkedListNode<Command>? command = Commands.First;
		if (command == null)
			return false;

		Command cmd = command.Value;

		currentTick = cmd.Tick;
		if (cmd.BufferSize > 0)
			currentCommand.Tokenize(argsBuffer.AsSpan()[cmd.FirstArgS..(cmd.FirstArgS + cmd.BufferSize)]);

		Commands.Remove(cmd);
		nextCommand = Commands.First;
		return true;
	}

	public CommandBuffer() {
		lastUsedArgSize = 0;
		argBufferSize = 0;
		currentTick = 0;
		lastTickToProcess = -1;
		waitDelayTicks = 1;
		nextCommand = null;
		maxArgBufferLength = ARGS_BUFFER_LENGTH;
		isProcessingCommands = false;
		waitEnabled = false;
	}

	public ref TokenizedCommand GetCommand() => ref currentCommand;

	public unsafe bool AddText(ReadOnlySpan<char> text, long tickDelay = 0) {
		long tick = currentTick + tickDelay;

		int tlen = text.IndexOf('\0');
		if (tlen == -1) tlen = text.Length;
		ReadOnlySpan<char> currentCommand = text[..tlen];

		Span<char> argV0 = stackalloc char[1024];
		int len = currentCommand.Length;
		int offsetToNextCommand = 0;
		for (; len > 0; len -= offsetToNextCommand, currentCommand = currentCommand[(offsetToNextCommand)..]) {
			GetNextCommandLength(currentCommand, len, out int commandLength, out offsetToNextCommand);
			if (commandLength <= 0)
				continue;

			StringReader reader = new StringReader(new(currentCommand[..commandLength]));
			ParseArgV0(reader, argV0[..commandLength], out ReadOnlySpan<char> argS);

			if (!InsertCommand(currentCommand[..commandLength], tick))
				return false;
		}

		return true;
	}

	unsafe void Compact() {
		argBufferSize = 0;
		Span<char> tempBuffer = stackalloc char[ARGS_BUFFER_LENGTH];
		Span<char> writeBuffer = argsBuffer.AsSpan();
		foreach (Command command in Commands) {
			writeBuffer[command.FirstArgS..].CopyTo(tempBuffer[argBufferSize..]);
			command.FirstArgS = argBufferSize;
			argBufferSize += command.BufferSize;
		}

		tempBuffer[..argBufferSize].CopyTo(writeBuffer);
	}

	private unsafe bool InsertCommand(ReadOnlySpan<char> argS, long tick) {
		int commandSize = argS.Length;

		if (commandSize > TokenizedCommand.MaxCommandLength) {
			Logs.Warn($"Command too long... ignoring!");
			return false;
		}

		if (argBufferSize + commandSize + 1 > maxArgBufferLength) {
			Compact();
			if (argBufferSize + commandSize + 1 > maxArgBufferLength)
				return false;
		}

		fixed (char* pArgSBuffer = argsBuffer)
			argS.CopyTo(new Span<char>((void*)(((nint)pArgSBuffer) + (argBufferSize * sizeof(char))), argsBuffer.Length));

		argsBuffer[argBufferSize + commandSize] = '\0';
		++commandSize;

		Command command = new();
		command.Tick = tick;
		command.FirstArgS = argBufferSize;
		command.BufferSize = commandSize;
		argBufferSize += commandSize;

		if (!isProcessingCommands || (tick > currentTick)) {
			InsertCommandAtAppropriateTime(command);
		}
		else {
			InsertImmediateCommand(command);
		}

		return true;
	}

	private void InsertImmediateCommand(Command command) {
		if (nextCommand == null)
			Commands.AddLast(command);
		else
			Commands.AddAfter(nextCommand, command);
	}

	private void InsertCommandAtAppropriateTime(Command command) {
		LinkedListNode<Command>? i;
		for (i = Commands.First; i != null; i = i.Next) {
			if (i.Value.Tick > command.Tick)
				break;
		}
		if (i == null)
			Commands.AddFirst(command);
		else
			Commands.AddBefore(i, command);
	}

	private bool ParseArgV0(StringReader buf, Span<char> argV0, out ReadOnlySpan<char> argS) {
		throw new NotImplementedException();
	}

	private void GetNextCommandLength(ReadOnlySpan<char> text, int maxLen, out int commandLength, out int nextCommandOffset) { // FIXME!!! Multiple commands in the same input ("test;test") WILL cause this to infinitely loop!
		commandLength = 0;
		bool isQuoted = false;
		bool isCommented = false;
		for (nextCommandOffset = 0; nextCommandOffset < maxLen; ++nextCommandOffset, commandLength += isCommented ? 0 : 1) {
			char c = text[nextCommandOffset];
			if (!isCommented) {
				if (c == '"') {
					isQuoted = !isQuoted;
					continue;
				}

				if (!isQuoted && c == '/') {
					isCommented = (nextCommandOffset < maxLen - 1) && text[nextCommandOffset + 1] == '/';
					if (isCommented) {
						++nextCommandOffset;
						continue;
					}
				}

				if (!isQuoted && c == ';') {
					++nextCommandOffset;
					break;
				}
			}

			if (c == '\n') {
				nextCommandOffset++;
				break;
			}
		}
	}

	public void BeginProcessingCommands(int deltaTicks) {
		Debug.Assert(!isProcessingCommands);
		isProcessingCommands = true;
		lastTickToProcess = currentTick + deltaTicks - 1;
		nextCommand = Commands.First;
	}

	public void EndProcessingCommands() {
		isProcessingCommands = false;
		currentTick = lastTickToProcess + 1;
		nextCommand = null;
	}

	public bool IsProcessingCommands() => isProcessingCommands;
	public LinkedListNode<Command>? GetNextCommandHandle() => nextCommand;
	public int GetArgumentBufferSize() => argBufferSize;
	public int GetMaxArgumentBufferSize() => maxArgBufferLength;
	public void SetWaitEnabled(bool wait) => waitEnabled = wait;
}

public static class Cbuf
{
	public const int MAX_ALIAS_NAME = 32;
	public const int MAX_COMMAND_LENGTH = 1024;
	public static readonly CommandBuffer CommandBuffer = new CommandBuffer();

	public static readonly CommandBuffer Buffer = new();

	public static void ExecuteCommand(ref TokenizedCommand command) {
		Cmd.ExecuteCommand(ref command);
	}

	public static void AddText(ReadOnlySpan<char> text) {
		lock (Buffer) {
			if (!Buffer.AddText(text))
				Logs.Warn("CBuf.AddText: buffer overflow!!!");
		}
	}

	public static void InsertText(ReadOnlySpan<char> text) {
		lock (Buffer) {
			Debug.Assert(Buffer.IsProcessingCommands());
			AddText(text);
		}
	}

	public static void Execute() {
		lock (Buffer) {
			Buffer.BeginProcessingCommands(1);
			while (Buffer.DequeueNextCommand()) 
				ExecuteCommand(ref Buffer.GetCommand());
			Buffer.EndProcessingCommands();
		}
	}
}
