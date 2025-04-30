using Raylib_cs;
using System.Text.RegularExpressions;

namespace Nucleus.UI
{
	public class LuaSyntaxHighlighter : SyntaxHighlighter
	{
		public override string Name => "lua";
		public override Color Color => new Color(100, 125, 155, 255);

		private static bool isDigit(char c) => c >= '0' && c <= '9';
		private static bool isHexDigit(char c) => isDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
		private static bool isIdentifierStartSymbol(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';

		private static Color LUA_NUMBER => new Color(115, 245, 165, 255);
		private static Color LUA_IDENTIFIERGREEN => new Color(105, 245, 225, 255);
		private static Color LUA_IDENTIFIERYELLOW => new Color(245, 225, 165, 255);


		/// <summary>
		/// Tries to parse a Lua Number, from the Lua 5.1 docs:<br></br><br></br>
		/// A numerical constant can be written with an optional decimal part and an optional decimal exponent. Lua also accepts integer hexadecimal constants, by prefixing them with 0x. Examples of valid numerical constants are:<br></br>
		/// <code>3   3.0   3.1416   314.16e-2   0.31416E1   0xff   0x56</code>
		/// </summary>
		/// <param name="row"></param>
		/// <param name="rowPtr"></param>
		/// <returns></returns>
		public static RowDecorator readNumber(string row, ref int rowPtr) {
			string str = "";

			int localI = 0;
			bool usedDecimal = false;
			while (rowPtr < row.Length) {
				char c = row[rowPtr];

				if (c == '.' || char.IsDigit(c) || c == 'x' || c == 'X' || c == 'e' || c == 'E')
					str += c;
				else
					break;

				rowPtr++;
				localI++;
			}


			return new() {
				Color = LUA_NUMBER,
				Text = str
			};
		}

		/// <summary>
		/// Parses an identifier. Keywords are filtered out based on <see cref="Keywords"/>
		/// </summary>
		/// <param name="row"></param>
		/// <param name="rowPtr"></param>
		/// <returns></returns>
		private static string? readIdentifier(string row, ref int rowPtr) {
			var t = Regex.Match(row.Substring(rowPtr), @"(^[a-zA-Z_][a-zA-Z0-9_]*)");
			if (t.Success) {
				var str = t.Groups[0].Value;
				rowPtr += str.Length;
				return str;
			}
			else {
				return null;
			}
		}

		/// <summary>
		/// Read a single line string, "like this"
		/// </summary>
		/// <param name="row"></param>
		/// <param name="rowPtr"></param>
		/// <returns></returns>
		private static string? readSinglelineString(string row, ref int rowPtr) {
			var raw = row.Substring(rowPtr);
			var c = raw[0];
			var t = Regex.Match(raw, c + ("(?:[^" + c + "\\\\]|\\\\.)*") + c);
			if (t.Success) {
				var str = t.Groups[0].Value;
				rowPtr += str.Length;
				return str;
			}
			else {
				return null;
			}
		}
		/// <summary>
		/// Lua's reserved keywords
		/// </summary>
		private static HashSet<string> Keywords { get; } = ["break",
			"do",
			"else",
			"elseif",
			"end",
			"false",
			"for",
			"function",
			"goto",
			"if",
			"in",
			"local",
			"nil",
			"not",
			"and",
			"or",
			"repeat",
			"then",
			"true",
			"until",
			"while",
			"return",
		];
		private static HashSet<string> SpecialVariables { get; } = ["_G", "self"];

		// to-do: cleanup
		public override void Rebuild(SafeArray<string> rows) {
			Rows.Clear();
			bool commentMulti = false;
			foreach (var row in rows) {
				List<RowDecorator> rowDecs = [];
				int rowPtr = 0;

				if (row.Length > 0) {
					while (rowPtr < row.Length) {
						var c = row[rowPtr];

						bool handled = false;

						if (commentMulti) {
							string part = "";
							bool foundOtherBrack = false;
							commentMulti = true;
							while (rowPtr < row.Length) {
								part += row[rowPtr];
								rowPtr++;

								if (row[rowPtr - 1] == ']') {
									if (foundOtherBrack) {
										commentMulti = false;
										break;
									}
									else {
										foundOtherBrack = true;
									}
								}
								else {
									foundOtherBrack = false;
								}
							}
							rowDecs.Add(new() { Color = new Color(55, 155, 75, 255), Text = part });
							handled = true;
						}
						else if (c == ' ') {
							rowDecs.Add(new() { Color = Color.White, Text = " " });
							rowPtr += 1;
							handled = true;
						}
						else if (isDigit(c) || (c == '.' && rowPtr + 1 != row.Length && isDigit(row[rowPtr + 1]))) {
							rowDecs.Add(readNumber(row, ref rowPtr));
							handled = true;
						}
						else if (isIdentifierStartSymbol(c)) {
							char? cb = null;
							if (rowPtr > 0)
								cb = row[rowPtr - 1];
							var s = readIdentifier(row, ref rowPtr);
							if (s != null) {
								bool identified = false;
								bool unidentified = false;
								if (cb != null) {
									if (cb.Value == '.') {
										Color cR = LUA_IDENTIFIERGREEN;
										if (rowPtr < row.Length) {
											char c2 = row[rowPtr];
											if (c2 == '(') cR = LUA_IDENTIFIERYELLOW;
										}

										rowDecs.Add(new() { Color = cR, Text = s });
										identified = true;
									}
								}

								if (SpecialVariables.Contains(s)) rowDecs.Add(new() { Color = new Color(245, 165, 90, 255), Text = s });
								else if (Keywords.Contains(s)) rowDecs.Add(new() { Color = new Color(90, 140, 245, 255), Text = s });
								else if (!identified) {
									if (rowPtr < row.Length) {
										int test = rowPtr;
										while (test < row.Length - 1 && row[test] == ' ')
											test++;

										char c2 = row[test];
										if (c2 == '(' || c2 == '{' || c2 == '"' || c2 == '\'') rowDecs.Add(new() { Color = LUA_IDENTIFIERYELLOW, Text = s });
										else if (c2 == ':') rowDecs.Add(new() { Color = LUA_IDENTIFIERGREEN, Text = s });
										else unidentified = true;
									}
									else unidentified = true;
								}

								if (unidentified) {
									Color cR = new Color(190, 200, 245, 255);
									rowDecs.Add(new() { Color = cR, Text = s });
								}
								handled = true;
							}
						}
						else if (c == '\'' || c == '"') {
							var s = readSinglelineString(row, ref rowPtr);
							if (s != null) {
								rowDecs.Add(new() { Color = new Color(245, 155, 120, 255), Text = s });
								handled = true;
							}
						}
						else if (c == '-' && rowPtr + 1 < row.Length && row[rowPtr + 1] == '-') {
							// read to end
							if (rowPtr + 2 < row.Length && row[rowPtr + 2] == '[' && rowPtr + 3 < row.Length && row[rowPtr + 3] == '[') {
								rowPtr += 4;
								string part = "--[[";
								bool foundOtherBrack = false;
								commentMulti = true;
								rowDecs.Add(new() { Color = new Color(55, 155, 75, 255), Text = part });
								handled = true;
							}
							else {
								var comment = row.Substring(rowPtr);
								rowPtr += comment.Length;

								rowDecs.Add(new() { Color = new Color(55, 155, 75, 255), Text = comment });
								handled = true;
							}
						}

						if (!handled) {
							var skip = row.Substring(rowPtr);
							if (skip.Length == 0)
								break;

							var skipUntil = 1; //skip.IndexOf(' ');

							if (skipUntil != -1)
								skip = skip.Substring(0, skipUntil);

							rowDecs.Add(new() { Color = Color.White, Text = skip });
							rowPtr += skip.Length;
						}
					}
				}
				Rows.Add(rowDecs);
			}
		}
	}
}