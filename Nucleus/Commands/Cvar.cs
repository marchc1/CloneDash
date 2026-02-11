using Nucleus.Common.Commands;
using Nucleus.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Commands;

[MarkForStaticConstruction]
public class Cvar : ICVar
{
	public ReadOnlySpan<char> GetConfigCVar(ReadOnlySpan<char> name) => Host.GetConfigCVar(name);
	public bool HasConfigCVar(ReadOnlySpan<char> name) => Host.HasConfigCVar(name);
	public void SetConfigCVar(ReadOnlySpan<char> name, ReadOnlySpan<char> value) => Host.SetConfigCVar(name, value);
	public void UnsetConfigCVar(ReadOnlySpan<char> name) => Host.UnsetConfigCVar(name);
}
