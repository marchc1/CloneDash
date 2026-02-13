using Newtonsoft.Json.Linq;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nucleus.Engine;

public class CommandLineParser : ICommandLine
{
	void ParseCommandLine() {
		CleanUpParms();
		if (cmdLine == null)
			return;

		ReadOnlySpan<char> chars = cmdLine.AsSpan();

		while (!chars.IsEmpty && char.IsWhiteSpace(chars[0]))
			chars = chars[1..];

		bool inQuotes = false;
		ReadOnlySpan<char> firstLetter = ReadOnlySpan<char>.Empty;

		for (; !chars.IsEmpty; chars = chars[1..]) {
			char c = chars[0];

			if (inQuotes) {
				if (c == '\"') {
					AddArgument(firstLetter, chars);
					firstLetter = ReadOnlySpan<char>.Empty;
					inQuotes = false;
				}
				continue;
			}

			if (firstLetter.IsEmpty) {
				if (c == '\"') {
					inQuotes = true;
					firstLetter = chars[1..];
					continue;
				}

				if (char.IsWhiteSpace(c))
					continue;

				firstLetter = chars;
				continue;
			}

			if (char.IsWhiteSpace(c)) {
				AddArgument(firstLetter, chars);
				firstLetter = ReadOnlySpan<char>.Empty;
			}
		}

		if (!firstLetter.IsEmpty)
			AddArgument(firstLetter, chars);
	}

	void CleanUpParms() {
		parms.Clear();
	}

	
	void AddArgument(ReadOnlySpan<char> first, ReadOnlySpan<char> last) {
		if (first.IsEmpty)
			return;

		int len = first.Length;

		if (!last.IsEmpty) {
			ref char firstRef = ref MemoryMarshal.GetReference(first);
			ref char lastRef = ref MemoryMarshal.GetReference(last);

			int offset = Unsafe.ByteOffset(ref firstRef, ref lastRef).ToInt32() / sizeof(char);

			if (offset > 0 && offset <= first.Length)
				len = offset;
		}

		if (len > 0)
			parms.Add(new string(first[..len]));
	}

	bool IsInvalidIndex(int index) => index == 0 || index == parms.Count - 1;
	bool IsLikelyCmdLineParameter(int index) {
		char c = parms[index][0];
		return c == '-' || c == '+';
	}

	string? cmdLine;
	List<string> parms = [];


	public CommandLineParser() { }
	public CommandLineParser(string cmdline) => CreateCmdLine(cmdline);

	public unsafe void CreateCmdLine(ReadOnlySpan<char> commandLine) {
		const int MAX_BUFFER_LEN = 4096;
		char* full = stackalloc char[MAX_BUFFER_LEN];
		full[0] = '\0';

		char* dst = full;
		fixed (char* pCommandLine = commandLine) {
			char* src = pCommandLine;
			bool inQuotes = false;
			char* inQuotesStart = null;
			while (*src > 0) {
				if (*src == '"') {
					if (src == pCommandLine || (src[-1] != '/' && src[-1] != '\\')) {
						inQuotes = !inQuotes;
						inQuotesStart = src + 1;
					}
				}

				if (*src == '*') {
					if (src == pCommandLine || (inQuotes && char.IsWhiteSpace(src[-1])) || (inQuotes && src == inQuotesStart)) {
						LoadParametersFromFile(src, dst, MAX_BUFFER_LEN - ((nint)dst - (nint)full), inQuotes);
						continue;
					}
				}

				if ((dst - full) >= (MAX_BUFFER_LEN - ((nint)dst - (nint)full) - 1))
					break;

				*dst++ = *src++;
			}

			*dst = '\0';
			string managed = new string(full);
			cmdLine = managed;
			ParseCommandLine();
		}
	}

	private unsafe void LoadParametersFromFile(char* src, char* dst, nint v, bool inQuotes) {
		throw new NotImplementedException();
	}

	public void AppendParm(string name, string? values = null) {
		throw new NotImplementedException();
	}

	public int FindParm(ReadOnlySpan<char> name) {
		for (int i = 1; i < parms.Count; i++) {
			if (name.Equals(parms[i], StringComparison.InvariantCultureIgnoreCase))
				return i;
		}

		return 0;
	}

	public bool HasParm(ReadOnlySpan<char> name) => FindParm(name) != 0;

	public string? GetCmdLine() => cmdLine;

	public string GetParm(int index) {
		if (IsInvalidIndex(index))
			return "";
		return parms[index];
	}

	public int ParmCount() => parms.Count;


	[return: NotNullIfNotNull("defaultValue")]
	public string? ParmValue(string name, string? defaultValue = null) {
		int index = FindParm(name);
		if (IsInvalidIndex(index))
			return defaultValue;

		if (IsLikelyCmdLineParameter(index + 1))
			return defaultValue;

		return parms[index + 1];
	}

	public int ParmValue(string name, int defaultValue) => int.TryParse(ParmValue(name), out int result) ? result : defaultValue;
	public float ParmValue(string name, float defaultValue) => float.TryParse(ParmValue(name), out float result) ? result : defaultValue;
	public double ParmValue(string name, double defaultValue) => double.TryParse(ParmValue(name), out double result) ? result : defaultValue;


	[return: NotNullIfNotNull("defaultValue")]
	public string? ParmValueByIndex(int index, string? defaultValue = null) {
		if (IsInvalidIndex(index))
			return defaultValue;

		if (IsLikelyCmdLineParameter(index + 1))
			return defaultValue;

		return parms[index + 1];
	}

	public unsafe void RemoveParm(string name) {
		throw new NotImplementedException();
	}

	public void SetParm(int index, string newParm) {
		throw new NotImplementedException();
	}

	public bool CheckParm(string name, out ParmInfo info) {
		info = default;
		int i = FindParm(name);
		if (i == 0)
			return false;

		info = new(this, i);
		return true;
	}
}
