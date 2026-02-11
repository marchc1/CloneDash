using Nucleus.Commands;
using Nucleus.Common.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Common.Commands;

public interface IConsoleDisplayFunc
{
	void ColorPrint(in Color clr, ReadOnlySpan<char> message);
	void Print(ReadOnlySpan<char> message);
}

/// <summary>
/// Redesigned cvar interface, based more on the Source Engine than my previous deviations.
/// </summary>
public interface ICvar
{
	void RegisterConCommand(ConCommandBase commandBase);
	void UnregisterConCommand(ConCommandBase commandBase);
	ReadOnlySpan<char> GetCommandLineValue(ReadOnlySpan<char> variableName);

	ConCommandBase? FindCommandBase(ReadOnlySpan<char> name);
	ConVar? FindVar(ReadOnlySpan<char> name);
	ConCommand? FindCommand(ReadOnlySpan<char> name);

	ConCommandBase? GetCommands();

	void InstallGlobalChangeCallback(ChangeCallback callback);
	void RemoveGlobalChangeCallback(ChangeCallback callback);
	void CallGlobalChangeCallbacks(ConVar var, scoped ReadOnlySpan<char> oldString, double oldDouble);

	void InstallConsoleDisplayFunc(IConsoleDisplayFunc displayFunc);
	void RemoveConsoleDisplayFunc(IConsoleDisplayFunc displayFunc);

	void ConsoleColorPrint(in Color clr, ReadOnlySpan<char> message);
	void ConsolePrint(ReadOnlySpan<char> message);

	void RevertFlaggedConVars(FCvar flag);
}