using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Commands
{
	public class ConCommandArguments
	{
		private string? __raw;
		public string Raw {
			get {
				if (__raw == null)
					return string.Join(' ', AsStringArray);

				return __raw;
			}
			set {
				__raw = value;
			}
		}
#nullable disable
		private CVValue[] __args;
#nullable enable
		public int Length => __args.Length;
		public string[] AsStringArray {
			get {
				string[] ret = new string[__args.Length];
				for (int i = 0; i < __args.Length; i++) {
					ret[i] = __args[i].String;
				}
				return ret;
			}
		}
		public static ConCommandArguments FromArray(string[] args) {
			CVValue[] cvValueArgs = new CVValue[args.Length];
			for (int i = 0; i < args.Length; i++) {
				CVValue cv = new();
				CVValue.Update(ref cv, args[i]);
				cvValueArgs[i] = cv;
			}

			return new() {
				__args = cvValueArgs
			};
		}
		public static ConCommandArguments FromString(string args, int curWritePos, out int curArgPos) {
			var arguments = new List<string>();
			curArgPos = -1;

			bool inQuotes = false;
			int argStart = -1;
			int currentArgIndex = 0;

			for (int i = 0; i <= args.Length; i++) {
				bool isEnd = i == args.Length;
				char c = !isEnd ? args[i] : '\0';

				if (!inQuotes && (isEnd || char.IsWhiteSpace(c))) {
					if (argStart != -1) {
						string arg = args.Substring(argStart, i - argStart);
						arguments.Add(arg);

						if (curWritePos >= argStart && curWritePos <= i)
							curArgPos = currentArgIndex;

						currentArgIndex++;
						argStart = -1;
					}
				}
				else if (c == '"') {
					if (inQuotes) {
						if (argStart != -1) {
							string arg = args.Substring(argStart, i - argStart);
							arguments.Add(arg);

							if (curWritePos >= argStart && curWritePos <= i)
								curArgPos = currentArgIndex;

							currentArgIndex++;
							argStart = -1;
						}
						inQuotes = false;
					}
					else {
						inQuotes = true;
						argStart = i + 1; // skip quote
					}
				}
				else if (argStart == -1) {
					argStart = i;
				}
			}

			var ret = FromArray(arguments.ToArray());
			ret.Raw = args;
			return ret;
		}
		public static ConCommandArguments FromString(string args) => FromString(args, 0, out _);
		public bool GetInt(int pos, [NotNullWhen(true)] out int ret) {
			if (pos < 0) goto IsNull;
			if (pos >= __args.Length) goto IsNull;

			CVValue v = __args[pos];
			if (v.AsInt == null) goto IsNull;

			ret = v.AsInt ?? default;
			return v.AsInt.HasValue;

		IsNull:
			ret = default;
			return false;
		}
		public int? GetInt(int pos) => GetInt(pos, out int ret) ? ret : null;

		public bool GetDouble(int pos, [NotNullWhen(true)] out double? ret) {
			if (pos < 0) goto IsNull;
			if (pos >= __args.Length) goto IsNull;

			CVValue v = __args[pos];
			if (v.AsDouble == null) goto IsNull;

			ret = v.AsDouble;
			return true;

		IsNull:
			ret = null;
			return false;
		}
		public double? GetDouble(int pos) => GetDouble(pos, out double? ret) ? ret : null;

		public bool GetString(int pos, [NotNullWhen(true)] out string? ret) {
			if (pos < 0) goto IsNull;
			if (pos >= __args.Length) goto IsNull;

			CVValue v = __args[pos];
			ret = v.String;
			return true;

		IsNull:
			ret = null;
			return false;
		}
		public string? GetString(int pos) => GetString(pos, out string? ret) ? ret : null;
	}
}
