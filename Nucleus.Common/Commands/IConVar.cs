using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Nucleus.Common.Commands;

public interface IConVar : IConCommandBase
{
	double GetDouble();
	int GetInt();
	ReadOnlySpan<char> GetString();
	bool GetBool();
	void SetValue(ReadOnlySpan<char> str);
	void SetValue(int i);
	void SetValue(double d);
	void SetValue(bool b);
}