using Nucleus.Commands;

namespace Nucleus.Common.Commands;

public interface IConVar : IConCommandBase
{
	void AddFlags(FCvar flag);

	bool GetBool();

	double GetDouble();

	int GetInt();

	ReadOnlySpan<char> GetSaveString();

	ReadOnlySpan<char> GetString();

	bool IsFlagSet(FCvar flag);

	bool IsLocked();

	void RemoveFlags(FCvar flag);

	void SetValue(ReadOnlySpan<char> str);

	void SetValue(int i);

	void SetValue(double d);

	void SetValue(bool b);
}