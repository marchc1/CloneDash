global using static Nucleus.Engine.CvarUtilities;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Engine;

public class CvarUtilities
{
	public static readonly CvarUtilities cv = new();

	public bool IsCommand(in TokenizedCommand args) {
		int c = args.ArgC();
		if (c == 0)
			return false;

		ConVar? var = cvar.FindVar(args[0]);
		if (var == null)
			return false;

		if (var.IsFlagSet(FCvar.DevelopmentOnly))
			return false;

		if (c == 1) {
			ConVar.PrintDescription(var);
			return true;
		}

		if (var.IsFlagSet(FCvar.AlwaysDefault)){
			Logs.Warn($"Can't change {var.GetName()} while locked to its default value");
			return true;
		}

		if (var.IsFlagSet(FCvar.NotInGame)) {
			var level = EngineCore.Level;
			if (level != null && level.IsInGame) {
				Logs.Warn($"Can't change {var.GetName()} mid-game!");
				return true;
			}
		}

		Span<char> arg = stackalloc char[1024];

		ReadOnlySpan<char> argS = args.ArgS();
		int len = argS.IndexOf('\0');
		if (len == -1)
			len = argS.Length;

		bool quoted = argS[0] == '"';
		if (!quoted)
			args.ArgS().CopyTo(arg);
		else{
			len--;
			args.ArgS()[1..].CopyTo(arg);
		}
		arg = arg.Trim();
		if (arg[len - 1] == '"')
			arg[len - 1] = '\0';
		arg = arg.Trim('\0');
		arg = arg.Trim();
		SetDirect(var, arg);
		return true;
	}

	public void SetDirect(ConVar var, ReadOnlySpan<char> arg) {
		if (var.IsFlagSet(FCvar.NeverAsString))
			var.SetValue(double.TryParse(arg, out double d) ? d : 0);
		else
			var.SetValue(arg);
	}

	public void WriteVariables(StreamWriter writer, bool allVars) {
		for (ConCommandBase? var = cvar.GetCommands(); var != null; var = var.Next) {
			if (var.IsCommand())
				continue;

			bool save = var.IsFlagSet(FCvar.Saved);
			bool alwaysDefault = var.IsFlagSet(FCvar.AlwaysDefault);
			if (save && !alwaysDefault) {
				ConVar convar = (ConVar)var;
				if(allVars || strcmp(convar.GetString(), convar.GetDefault()) != 0)
					writer.WriteLine($"{var.GetName()} \"{convar.GetString()}\"");
			}
		}
	}
}
