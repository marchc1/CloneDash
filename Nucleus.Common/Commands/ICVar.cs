using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Common.Commands;

public interface ICVar {
	void SetConfigCVar(ReadOnlySpan<char> name, ReadOnlySpan<char> value);
	void UnsetConfigCVar(ReadOnlySpan<char> name);
	bool HasConfigCVar(ReadOnlySpan<char> name);
	ReadOnlySpan<char> GetConfigCVar(ReadOnlySpan<char> name);
}