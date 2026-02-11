using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Engine;

public static class Cmd
{
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