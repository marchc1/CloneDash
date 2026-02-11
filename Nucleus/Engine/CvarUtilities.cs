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
		if (arg[^1] == '"')
			arg = arg[..(arg.Length - 1)];
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
}
