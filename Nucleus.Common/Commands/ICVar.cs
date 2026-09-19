using Nucleus.Commands;
using Nucleus.Common.Types;
using System.Reflection;

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
	void CallGlobalChangeCallbacks(ConVar var, scoped ReadOnlySpan<char> oldString, double oldDouble);

	void ConsoleColorPrint(in Color clr, ReadOnlySpan<char> message);

	void ConsolePrint(ReadOnlySpan<char> message);

	ConCommand? FindCommand(ReadOnlySpan<char> name);

	ConCommandBase? FindCommandBase(ReadOnlySpan<char> name);

	ConVar? FindVar(ReadOnlySpan<char> name);

	ReadOnlySpan<char> GetCommandLineValue(ReadOnlySpan<char> variableName);

	ConCommandBase? GetCommands();

	void InstallConsoleDisplayFunc(IConsoleDisplayFunc displayFunc);

	void InstallGlobalChangeCallback(ChangeCallback callback);

	void RegisterConCommand(ConCommandBase commandBase);

	void RemoveConsoleDisplayFunc(IConsoleDisplayFunc displayFunc);

	void RemoveGlobalChangeCallback(ChangeCallback callback);

	void RevertFlaggedConVars(FCvar flag);

	void SetAssemblyIdentifier(Assembly assembly);

	void UnregisterConCommand(ConCommandBase commandBase);
}