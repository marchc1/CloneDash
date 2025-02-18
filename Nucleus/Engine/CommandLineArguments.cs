using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Engine
{
	public static class CommandLineArguments
	{
		private static Dictionary<string, object> parameters = [];
		public static Dictionary<string, object> Params => parameters.ToDictionary();
		public static bool HasParam(string parm) => parameters.TryGetValue(parm, out var _);
		private static bool getStrBoolVal(string parm) {
			switch (parm.ToLower()) {
				case "t":
				case "true":
				case "y":
				case "yes":
				case "1":
					return true;
				case "f":
				case "false":
				case "n":
				case "no":
				case "0":
				default:
					return false;
			}
		}

		public static bool IsParamTrue(string parm, bool def = false) => parameters.TryGetValue(parm, out var p) ? (
				(p is int i && i >= 1) ||
				(p is int d && d >= 1) ||
				(p is string s && getStrBoolVal(s))
			) : def;

		public static T GetParam<T>(string parm, T def)
			=> parameters.TryGetValue(parm, out object? obj)
				? (obj is T ? (T)obj : def)
				: def;

		public static bool TryGetParam<T>(string parm, [NotNullWhen(true)] out T? ret) {
			var has = parameters.TryGetValue(parm, out object? obj);
			if (has)
				ret = (obj is T ? (T)obj : default);
			else
				ret = default;
			return has;
		}

		public static void SetParam<T>(string parm, T val) {
			object? oVal = val;
			if (oVal == null) parameters.Remove(parm);
			else parameters[parm] = oVal;
		}

		private static string readUntil(string str, ref int i, char stopChar) {
			string build = "";
			while (i < str.Length) {
				char c = str[i];
				if (c == stopChar)
					return build;
				build += c;
				i++;
			}

			return build;
		}

		private static string readValue(string str, ref int i) {
			string build = "";
			bool willBeQuoted = str[i] == '"';

			while (i < str.Length) {
				char c = str[i];
				if ((c == ' ' && !willBeQuoted) || (c == '"' && willBeQuoted && str[i - 1] != '\\'))
					return build;
				build += c;
				i++;
			}

			return build;
		}

		private static void skipWhitespace(string args, ref int i) {
			while (i < args.Length && char.IsWhiteSpace(args[i]))
				i++;
		}

		private static object trueValueType(string input) {
			if (int.TryParse(input, out int i))
				return i;

			if (double.TryParse(input, out double d))
				return d;

			return input;
		}

		private static bool checkIfValue(string input, int i) {
			if (i < input.Length && input[i] != '-' && input[i] != '+')
				return true;
			return false;
		}

		public static void FromString(string args) {
			parameters.Clear();
			for (int i = 0; i < args.Length;) {
				skipWhitespace(args, ref i);
				char c = args[i];
				switch (c) {
					case '-': // Read a parameter
					case '+': // Read a variable
						bool isParm = c == '-';
						i++;
						var parm = readUntil(args, ref i, ' ');
						skipWhitespace(args, ref i);

						bool hasValue = checkIfValue(args, i);
						string value = "";

						if (hasValue)
							value = readValue(args, ref i);
						else if (isParm)
							value = "1";

						if (isParm) {
							parameters[parm] = trueValueType(value);
						}
						else {
							// no logic right now to handle variables
							// to do as i figure out how to fit that in
						}

						break;
					default:
						break;
				}
			}
		}

		public static void FromArgs(string[] args) {
			for (int i = 0; i < args.Length; i++)
				args[i] = args[i].IndexOf(' ') > -1 ? ('"' + args[i] + '"') : args[i];

			FromString(string.Join(' ', args));
		}
	}
}
