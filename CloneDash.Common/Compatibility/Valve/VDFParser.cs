using System.Collections;

namespace CloneDash.Compatibility.Valve;

public class ValveDataFile
{
	public abstract class VDFItem : IEnumerable<KeyValuePair<string, VDFItem>>
	{
		Dictionary<string, VDFItem> dict = new Dictionary<string, VDFItem>();
		public VDFItem this[string key] {
			get {
				return dict[key];
			}
			set {
				dict[key] = value;
			}
		}
		public string GetString(string key) {
			return (dict[key] as VDFString).Content;
		}
		public IEnumerator<KeyValuePair<string, VDFItem>> GetEnumerator() {
			return dict.GetEnumerator();
		}
		public bool Contains(string key) => dict.ContainsKey(key);
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
	public class VDFString : VDFItem { public string Content; }
	public class VDFDict : VDFItem { }
	public enum TokenType
	{
		StartBracket,
		CloseBracket,
		ID
	}
	public struct Token
	{
		public TokenType Type;
		public string Data;
		public override string ToString() {
			return Data == null ? Type.ToString() : Data;
		}
	}
	public VDFItem this[string key] {
		get => data[key];
	}

	VDFDict data = new VDFDict();
	public static ValveDataFile FromFile(string path) {
		ValveDataFile vdf = new ValveDataFile();
		string data = File.ReadAllText(path);
		data = data.Replace(Environment.NewLine, "");
		data = data.Replace("\n", "");
		data = data.Replace("\t", "");
		int i = 0;
		List<Token> tokens = new List<Token>();
		while (i < data.Length) {
			char c = data[i]; i++;
			if (c == '{')
				tokens.Add(new Token() { Type = TokenType.StartBracket });
			else if (c == '}')
				tokens.Add(new Token() { Type = TokenType.CloseBracket });
			else if (c == '"') {
				Token id = new Token();
				string build = "";
				while (true) {
					if (data[i] == '"')
						if (data[i - 1] != '\\')
							break;

					build += data[i];
					if (data[i] == '\\' && i + 1 < data.Length && data[i + 1] == '\\')
						i++;
					i++;
				}
				i++;
				id.Data = build;
				id.Type = TokenType.ID;
				tokens.Add(id);
			}
			else {
				i++;
			}
		}

		i = 0;
		Stack<VDFDict> WIP = new Stack<VDFDict>();
		WIP.Push(vdf.data);
		while (i < tokens.Count) {
			if (tokens[i].Type == TokenType.CloseBracket) {
				WIP.Pop();
				i += 1;
			}
			else if (tokens[i].Type == TokenType.ID && tokens[i + 1].Type == TokenType.StartBracket) {
				VDFDict dict = new VDFDict();
				WIP.Peek()[tokens[i].Data] = dict;
				WIP.Push(dict);
				i += 2;
			}
			else if (tokens[i].Type == TokenType.ID && tokens[i + 1].Type == TokenType.ID) {
				WIP.Peek()[tokens[i].Data] = new VDFString() { Content = tokens[i + 1].Data };
				i += 2;
			}
			else {
				throw new Exception("VDF parsing failed!");
			}
		}

		return vdf;
	}
}