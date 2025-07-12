using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Engine
{
	public class CommandLineParser
	{
		private Dictionary<string, object> parameters = [];
		public Dictionary<string, object> Params => parameters.ToDictionary();
		public bool HasParam(string parm) => parameters.TryGetValue(parm, out var _);
		private bool getStrBoolVal(string parm) {
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

		public bool IsParamTrue(string parm, bool def = false) => parameters.TryGetValue(parm, out var p) ? (
				(p is int i && i >= 1) ||
				(p is int d && d >= 1) ||
				(p is string s && getStrBoolVal(s))
			) : def;

		public T GetParam<T>(string parm, T def) {
			if (!parameters.TryGetValue(parm, out object? obj)) return def;

			var tAs = def switch {
				double _ => obj switch {
					int o => (T)(object)Convert.ToDouble(o),
					_ => throw new InvalidCastException()
				},
				_ => (T)obj
			};


			return tAs;
		}

		public bool TryGetParam<T>(string parm, [NotNullWhen(true)] out T? ret) {
			var has = parameters.TryGetValue(parm, out object? obj);
			if (has)
				ret = (obj is T ? (T)obj : default);
			else
				ret = default;
			return has;
		}

		public void SetParam<T>(string parm, T val) {
			object? oVal = val;
			if (oVal == null) parameters.Remove(parm);
			else parameters[parm] = oVal;
		}

		private string readUntil(string str, ref int i, char stopChar) {
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

		private string readValue(string str, ref int i) {
			string build = "";
			bool willBeQuoted = str[i] == '"';
			if (willBeQuoted) i++;

			while (i < str.Length) {
				char c = str[i];
				if ((c == ' ' && !willBeQuoted) || (c == '"' && willBeQuoted && str[i - 1] != '\\')) {
					i++;
					return build;
				}
				build += c;
				i++;
			}

			return build;
		}

		private void skipWhitespace(string args, ref int i) {
			while (i < args.Length && char.IsWhiteSpace(args[i]))
				i++;
		}

		private object trueValueType(string input) {
			if (int.TryParse(input, out int i))
				return i;

			if (double.TryParse(input, out double d))
				return d;

			return input;
		}

		private bool checkIfValue(string input, int i) {
			if (i < input.Length && input[i] != '-' && input[i] != '+')
				return true;
			return false;
		}

		public void FromString(string args) {
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

		public void FromArgs(string[] args) {
			for (int i = 0; i < args.Length; i++)
				args[i] = args[i].IndexOf(' ') > -1 ? ('"' + args[i] + '"') : args[i];

			FromString(string.Join(' ', args));
		}
	}

	public static class CommandLine
	{
		public readonly static CommandLineParser Singleton = new();
		public static bool HasParam(string parm) => Singleton.HasParam(parm);
		public static bool IsParamTrue(string parm, bool def = false) => Singleton.IsParamTrue(parm, def);
		public static T GetParam<T>(string parm, T def) => Singleton.GetParam(parm, def);
		public static bool TryGetParam<T>(string parm, [NotNullWhen(true)] out T? ret) => Singleton.TryGetParam(parm, out ret);
		public static void SetParam<T>(string parm, T val) => Singleton.SetParam(parm, val);
		public static void FromString(string args) => Singleton.FromString(args);
		public static void FromArgs(string[] args) => Singleton.FromArgs(args);
	}
}
