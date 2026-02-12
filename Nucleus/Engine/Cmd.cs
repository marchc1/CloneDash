using Newtonsoft.Json.Linq;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Core;
using Nucleus.Files;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Nucleus.Engine;

internal class OldHostStore
{
	public Dictionary<string, string> CVars = [];
	public Dictionary<string, string> DataStore = [];
}

public static class Cmd
{
	[ConCommand(Name: "stuffcmds")]
	public static void StuffCmds(in TokenizedCommand args) {
		if (args.ArgC() != 1) {
			Logs.Print("stuffcmds : execute command line parameters");
			return;
		}

		using MemoryStream stream = new();
		using StreamWriter writer = new(stream);

		for (int i = 1; i < CommandLine().ParmCount(); i++) {
			ReadOnlySpan<char> parm = CommandLine().GetParm(i);
			if (parm.IsEmpty)
				continue;

			if (parm[0] == '-') {
				ReadOnlySpan<char> value = CommandLine().ParmValueByIndex(i);
				if (!value.IsEmpty)
					i++;
				continue;
			}
			else if (parm[0] == '+') {
				ReadOnlySpan<char> value = CommandLine().ParmValueByIndex(i);
				if (!value.IsEmpty) {
					writer.Write($"{parm[1..]} {value}\n");
					i++;
				}
				else {
					writer.Write($"{parm[1..]}\n");
				}
			}
		}

		writer.Flush();
		stream.Seek(0, SeekOrigin.Begin);

		if (stream.Length <= 0)
			return;

		using StreamReader reader = new(stream, leaveOpen: true);
		Cbuf.InsertText(reader.ReadToEnd());
	}

	[ConCommand(Name: "exec")]
	public static void Exec(in TokenizedCommand args) {
		int argCount = args.ArgC();
		if (argCount != 2) {
			Logs.Print("exec <filename>: execute a script file");
			return;
		}

		ReadOnlySpan<char> file = args[1];

	readText:
		string? text = Filesystem.ReadAllText("cfg", new string(file));
		if (text == null) {
			Logs.Print($"exec: couldn't exec {file}");
			return;
		}

		bool ranBackwardsCompatibilityHack = false;
		// Temporary backwards compatibility hack... can probably get rid of this ~later 2026.
		// A new normal config.cfg (newline delimited cbuf text) would not start with a {,
		// while the old config.cfg (json-based) would start with a {.
		if (stricmp("config.cfg", file) == 0 && text.StartsWith('{')) {
			if (ranBackwardsCompatibilityHack)
				throw new Exception("Got { at config.cfg[0]. The engine attempted to load a deprecated HostStore json structure twice. (this should never happen)");

			OldHostStore hoststore = JSON.Deserialize<OldHostStore>(text) ?? throw new Exception("Got { at config.cfg[0]. The engine attempted to load a deprecated HostStore json structure, which failed to deserialize.");
			Host.ReformatOldHostStore(hoststore);
			// ^^ will guarantee config.cfg and datastore.cfg are now compliant. But the text is outdated now, so refetch it for this config file.
			ranBackwardsCompatibilityHack = true; // just in case this somehow happens
			goto readText;
		}

		using TextReader reader = new StringReader(text);
		while (true) {
			var line = reader.ReadLine();
			if (line != null) {
				Cbuf.InsertText(line);

				var commandHandle = Cbuf.CommandBuffer.GetNextCommandHandle();

				while (Cbuf.CommandBuffer.GetNextCommandHandle() != commandHandle) {
					if (Cbuf.CommandBuffer.DequeueNextCommand())
						Cbuf.ExecuteCommand(ref Cbuf.CommandBuffer.GetCommand());
					else {
						Debug.Assert(false);
						break;
					}
				}
			}
			else
				break;
		}
	}

	public static void Dispatch(ConCommandBase commandBase, in TokenizedCommand command) => ((ConCommand)commandBase).Dispatch(in command);
	public static ConCommandBase? ExecuteCommand(in TokenizedCommand command) {
		if (command.ArgC() == 0)
			return null;

		// TODO: Aliases

		ConCommandBase? commandBase = cvar.FindCommandBase(command[0]);

		if (commandBase != null && commandBase.IsCommand()) {
			Dispatch(commandBase, in command);
			return commandBase;
		}

		if (cv.IsCommand(in command))
			return commandBase;

		Logs.Print($"Unknown command \"{command[0]}\"");
		return null;
	}
}