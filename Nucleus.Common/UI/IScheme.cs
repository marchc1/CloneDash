using Nucleus.Common.Types;
using Nucleus.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Nucleus.Common.UI;

public enum SchemeSettingGenericType : byte {
	Integer = 1 << 0,
	Color = 1 << 1,
	Float = 1 << 2,
	String = 1 << 3,
}
public readonly struct SchemeSettingGeneric
{
	public readonly SchemeSettingGenericType Type;
	public readonly int Integer;
	public readonly Color Color;
	public readonly float Float;
	public readonly string String;
	public SchemeSettingGeneric(int i) {
		Type = SchemeSettingGenericType.Integer;
		Integer = i;
		Float = i;
		String = "";
	}
	public SchemeSettingGeneric(Color color) {
		Type = SchemeSettingGenericType.Color;
		Color = color;
		String = "";
	}
	public SchemeSettingGeneric(float fl) {
		Type = SchemeSettingGenericType.Float;
		Float = fl;
		Integer = (int)fl;
		String = "";
	}
	public SchemeSettingGeneric(string str) {
		Type = SchemeSettingGenericType.String;
		String = str;
	}
	public readonly bool HasType(SchemeSettingGenericType type) => (Type & type) != 0;
}

public readonly struct SchemeSettingFontStyle {
	public readonly string Name;
	public readonly int Tall;
	public SchemeSettingFontStyle(string name, int tall) {
		Name = name;
		Tall = tall;
	}
}

public readonly struct SchemeSettingCustomFont {
	public readonly string PathID;
	public readonly string Path;
	public SchemeSettingCustomFont(string pathID, string path) {
		PathID = pathID;
		Path = path;
	}
}

public class SchemeSettings : IScheme
{
	readonly Dictionary<UtlSymId_t, SchemeSettingGeneric> BaseSettings = [];
	readonly Dictionary<UtlSymId_t, SchemeSettingFontStyle> FontStyles = [];
	readonly Dictionary<UtlSymId_t, Color> Colors = [];
	readonly Dictionary<UtlSymId_t, SchemeSettingCustomFont> CustomFonts = [];

	public SchemeSettings(ReadOnlySpan<char> filepath, ReadOnlySpan<char> pathID = "resource") {
		var text = filesystem.ReadAllText(pathID, filepath) ?? "{}";
		var root = JsonDocument.Parse(text, new JsonDocumentOptions {
			CommentHandling = JsonCommentHandling.Skip
		}).RootElement;

		if (root.TryGetProperty("basesettings", out var baseSettings)) {
			foreach (var prop in baseSettings.EnumerateObject()) {
				var key = new UtlSymbol(prop.Name);
				SchemeSettingGeneric generic = prop.Value.ValueKind switch {
					JsonValueKind.Number when prop.Value.TryGetInt32(out var i) => new(i),
					JsonValueKind.Number => new(prop.Value.GetSingle()),
					JsonValueKind.String => ParseBaseSetting(prop.Value.GetString() ?? ""),
					_ => new(prop.Value.ToString())
				};
				BaseSettings[key] = generic;
			}
		}

		if (root.TryGetProperty("fontStyles", out var fontStyles)) {
			foreach (var prop in fontStyles.EnumerateObject()) {
				var key = new UtlSymbol(prop.Name);
				FontStyles[key] = new(prop.Value.GetProperty("name").GetString() ?? "", prop.Value.GetProperty("tall").GetInt32());
			}
		}

		if (root.TryGetProperty("colors", out var colors)) {
			foreach (var prop in colors.EnumerateObject()) {
				var key = new UtlSymbol(prop.Name);
				Colors[key] = ParseColor(prop.Value.GetString() ?? "");
			}
		}

		if (root.TryGetProperty("customFonts", out var customFonts)) {
			foreach (var prop in customFonts.EnumerateObject()) {
				var key = new UtlSymbol(prop.Name);
				var raw = prop.Value.GetString();
				if (raw == null) continue;
				var colonIdx = raw.IndexOf(':');
				if (colonIdx == -1) continue;
				CustomFonts[key] = new(raw[..colonIdx], raw[(colonIdx + 1)..]);
			}
		}

		ResolveReferences();
	}

	void ResolveReferences() {
		var keys = new List<UtlSymId_t>(BaseSettings.Keys);
		foreach (var key in keys) {
			var value = BaseSettings[key];
			if (!value.HasType(SchemeSettingGenericType.String)) continue;
			if (string.IsNullOrEmpty(value.String) || !value.String.Contains('#')) continue;

			BaseSettings[key] = ResolveValue(value.String, []);
		}
	}

	SchemeSettingGeneric ResolveValue(string raw, HashSet<string> visited) {
		var start = raw.IndexOf('#');
		var end = raw.IndexOf('#', start + 1);
		if (start == -1 || end == -1) return new(raw);

		var reference = raw.AsSpan()[(start + 1)..end];
		var colonIdx = reference.IndexOf(':');
		if (colonIdx == -1) return new(raw);

		var table = reference[..colonIdx].ToString();
		var refKey = reference[(colonIdx + 1)..].ToString();

		var refId = $"{table}:{refKey}";
		if (!visited.Add(refId)) {
			Logs.Warn($"SchemeSettings: circular reference detected for {refId}, defaulting");
			return new(default(Color));
		}

		var refSymbol = new UtlSymbol(refKey);

		switch (table) {
			case "colors": {
					if (!Colors.TryGetValue(refSymbol, out var color)) {
						Logs.Warn($"SchemeSettings: unresolved reference {refId}");
						return new(default(Color));
					}

					var remainder = raw.AsSpan()[(end + 1)..].Trim();
					if (remainder.StartsWith("with"))
						color = ApplyWithModifier(color, remainder);

					return new(color);
				}
			case "basesettings": {
					if (!BaseSettings.TryGetValue(refSymbol, out var setting)) {
						Logs.Warn($"SchemeSettings: unresolved reference {refId}");
						return new(raw);
					}

					if (setting.HasType(SchemeSettingGenericType.String) && setting.String.Contains('#'))
						return ResolveValue(setting.String, visited);

					return setting;
				}
			default:
				Logs.Warn($"SchemeSettings: unknown table '{table}' in reference {refId}");
				return new(raw);
		}
	}

	static Color ApplyWithModifier(Color color, ReadOnlySpan<char> modifier) {
		var braceStart = modifier.IndexOf('{');
		var braceEnd = modifier.LastIndexOf('}');
		if (braceStart == -1 || braceEnd == -1) return color;

		var inner = modifier[(braceStart + 1)..braceEnd];

		byte r = color.R, g = color.G, b = color.B, a = color.A;

		foreach (var assignment in new CommaTokenizer(inner)) {
			var trimmed = assignment.Trim();
			var eqIdx = trimmed.IndexOf('=');
			if (eqIdx == -1) continue;

			var field = trimmed[..eqIdx].Trim();
			var valSpan = trimmed[(eqIdx + 1)..].Trim();

			if (!byte.TryParse(valSpan, out var val)) continue;

			if (field.Length == 1) {
				switch (field[0]) {
					case 'R' or 'r': r = val; break;
					case 'G' or 'g': g = val; break;
					case 'B' or 'b': b = val; break;
					case 'A' or 'a': a = val; break;
				}
			}
		}

		return new Color(r, g, b, a);
	}

	ref struct CommaTokenizer
	{
		ReadOnlySpan<char> _remaining;

		public CommaTokenizer(ReadOnlySpan<char> span) => _remaining = span;
		public CommaTokenizer GetEnumerator() => this;
		public ReadOnlySpan<char> Current { get; private set; }

		public bool MoveNext() {
			while (_remaining.Length > 0 && _remaining[0] == ',')
				_remaining = _remaining[1..];

			if (_remaining.IsEmpty) return false;

			var commaIdx = _remaining.IndexOf(',');
			if (commaIdx == -1) {
				Current = _remaining;
				_remaining = default;
			}
			else {
				Current = _remaining[..commaIdx];
				_remaining = _remaining[(commaIdx + 1)..];
			}
			return true;
		}
	}

	static SchemeSettingGeneric ParseBaseSetting(string raw) {
		if (raw.StartsWith('#') && raw.Contains(':')) 
			return new (raw);

		if (int.TryParse(raw, out var i)) 
			return new (i);

		if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f))
			return new(f);

		if (TryParseColor(raw, out var color))
			return new(color);

		return new(raw);
	}

	static Color ParseColor(string raw) {
		TryParseColor(raw, out var color);
		return color;
	}

	static bool TryParseColor(string raw, out Color color) {
		color = default;
		var s = raw.AsSpan().Trim();

		if (s.Length > 0 && s[0] == '#')
			s = s[1..];

		if ((s.Length == 6 || s.Length == 8) && IsHex(s)) {
			byte r = byte.Parse(s[0..2], System.Globalization.NumberStyles.HexNumber);
			byte g = byte.Parse(s[2..4], System.Globalization.NumberStyles.HexNumber);
			byte b = byte.Parse(s[4..6], System.Globalization.NumberStyles.HexNumber);
			byte a = s.Length == 8
				? byte.Parse(s[6..8], System.Globalization.NumberStyles.HexNumber)
				: (byte)255;

			color = new Color(r, g, b, a);
			return true;
		}

		Span<byte> components = stackalloc byte[4];
		int count = 0;

		Span<char> buf = stackalloc char[s.Length];
		s.CopyTo(buf);
		for (int i = 0; i < buf.Length; i++)
			if (buf[i] == ',') buf[i] = ' ';

		foreach (var token in new SpanTokenizer(buf)) {
			if (count >= 4) return false;
			if (!byte.TryParse(token, out components[count]))
				return false;
			count++;
		}

		if (count == 3) {
			color = new Color(components[0], components[1], components[2], (byte)255);
			return true;
		}
		if (count == 4) {
			color = new Color(components[0], components[1], components[2], components[3]);
			return true;
		}

		return false;
	}

	static bool IsHex(ReadOnlySpan<char> s) {
		foreach (var c in s)
			if (!char.IsAsciiHexDigit(c)) return false;
		return true;
	}

	public ReadOnlySpan<char> GetString(ReadOnlySpan<char> key) 
		=> BaseSettings.TryGetValue(new UtlSymbol(key), out var value) 
			? value.String 
			: null;

	public int GetInt(ReadOnlySpan<char> key) 
		=> BaseSettings.TryGetValue(new UtlSymbol(key), out var value) 
			? value.Integer
			: 0;

	public float GetFloat(ReadOnlySpan<char> key) 
		=> BaseSettings.TryGetValue(new UtlSymbol(key), out var value) 
			? value.Float 
			: 0;

	public Color GetColor(ReadOnlySpan<char> key) 
		=> BaseSettings.TryGetValue(new UtlSymbol(key), out var value) 
			? value.Color 
			: Colors.TryGetValue(new UtlSymbol(key), out var value2) 
				? value2 
				: new (0, 0, 0, 255);

	public SchemeSettingFontStyle GetFontStyle(ReadOnlySpan<char> key) 
		=> FontStyles.TryGetValue(new UtlSymbol(key), out var value) 
			? value 
			: FontStyles.TryGetValue(new UtlSymbol("Nucleus.Default"), out value) 
				? value 
				: default;

	ref struct SpanTokenizer
	{
		ReadOnlySpan<char> _remaining;

		public SpanTokenizer(ReadOnlySpan<char> span) => _remaining = span;
		public SpanTokenizer GetEnumerator() => this;
		public ReadOnlySpan<char> Current { get; private set; }

		public bool MoveNext() {
			while (_remaining.Length > 0 && char.IsWhiteSpace(_remaining[0]))
				_remaining = _remaining[1..];

			if (_remaining.IsEmpty) return false;

			int i = 0;
			while (i < _remaining.Length && !char.IsWhiteSpace(_remaining[i]))
				i++;

			Current = _remaining[..i];
			_remaining = _remaining[i..];
			return true;
		}
	}
}

// TODO: Move everything up here to the engine/gui system later

public interface IScheme
{
	ReadOnlySpan<char> GetString(ReadOnlySpan<char> key);
	int GetInt(ReadOnlySpan<char> key);
	float GetFloat(ReadOnlySpan<char> key);
	Color GetColor(ReadOnlySpan<char> key);
	SchemeSettingFontStyle GetFontStyle(ReadOnlySpan<char> key);
}
