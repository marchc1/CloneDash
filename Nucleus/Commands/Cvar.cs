using Nucleus.Common.Commands;
using Nucleus.Common.Types;
using Nucleus.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Nucleus.Commands;

[MarkForStaticConstruction]
public class Cvar : ICvar
{
	public static ConCommandBase? ConCommandList;

	static readonly List<ChangeCallback> GlobalChangeCallbacks = [];
	static readonly List<IConsoleDisplayFunc> DisplayFuncs = [];

	public void CallGlobalChangeCallbacks(ConVar var, scoped ReadOnlySpan<char> oldString, double oldDouble) {
		foreach (var c in GlobalChangeCallbacks)
			c(var, oldString, oldDouble);
	}

	public void ConsoleColorPrint(in Color clr, ReadOnlySpan<char> message) {
		throw new NotImplementedException();
	}

	public void ConsolePrint(ReadOnlySpan<char> message) {
		throw new NotImplementedException();
	}

	public ConCommand? FindCommand(ReadOnlySpan<char> name) {
		ConCommandBase? var = FindCommandBase(name);
		if (var == null || !var.IsCommand())
			return null;

		return (ConCommand)var!;
	}

	public ConCommandBase? FindCommandBase(ReadOnlySpan<char> name) {
		for(ConCommandBase? cmd = GetCommands(); cmd != null; cmd = cmd.Next)
			if (cmd.Name.AsSpan().Equals(name, StringComparison.OrdinalIgnoreCase))
				return cmd;

		return null;
	}

	public ConVar? FindVar(ReadOnlySpan<char> name) {
		ConCommandBase? var = FindCommandBase(name);
		if (var == null || var.IsCommand())
			return null;

		return (ConVar)var!;
	}

	public ReadOnlySpan<char> GetCommandLineValue(ReadOnlySpan<char> variableName) {
		throw new NotImplementedException();
	}

	public ConCommandBase? GetCommands() => ConCommandList;
	public IEnumerable<ConCommandBase> GetCommandEnumerable() {
		ConCommandBase? b = ConCommandList;
		while(b != null){
			yield return b;
			b = b.Next;
		}
	}

	public void InstallConsoleDisplayFunc(IConsoleDisplayFunc displayFunc) {
		throw new NotImplementedException();
	}

	public void InstallGlobalChangeCallback(ChangeCallback callback) {
		throw new NotImplementedException();
	}

	public void RegisterConCommand(ConCommandBase commandBase) {
		if (commandBase.IsRegistered())
			return;

		commandBase.Registered = true;

		ReadOnlySpan<char> name = commandBase.GetName();
		if (name.IsEmpty) {
			commandBase.Next = null;
			return;
		}

		ConCommandBase? other = FindVar(commandBase.GetName());
		if (other != null) {
			if (commandBase.IsCommand() || other.IsCommand()) 
				Logs.Warn($"Unable to link {commandBase.GetName()} and {other.GetName()} because one or more is a ConCommand.");
			else {
				ConVar childVar = (ConVar)commandBase;
				ConVar parentVar = (ConVar)other;


				childVar.Parent = parentVar.Parent;
				parentVar.Flags |= childVar.Flags;
			}

			commandBase.Next = null;
			return;
		}

		commandBase.Next = ConCommandList;
		ConCommandList = commandBase;
	}

	public void RemoveConsoleDisplayFunc(IConsoleDisplayFunc displayFunc) {
		throw new NotImplementedException();
	}

	public void RemoveGlobalChangeCallback(ChangeCallback callback) {
		throw new NotImplementedException();
	}

	public void RevertFlaggedConVars(FCvar flag) {
		throw new NotImplementedException();
	}

	public void UnregisterConCommand(ConCommandBase commandBase) {
		throw new NotImplementedException();
	}

	public void SetAssemblyIdentifier(Assembly assembly) {

	}
}
