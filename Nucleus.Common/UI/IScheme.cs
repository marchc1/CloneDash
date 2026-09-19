using Nucleus.Common.Types;

namespace Nucleus.Common.UI;

public enum SchemeSettingGenericType : byte
{
	Integer = 1 << 0,
	Color = 1 << 1,
	Float = 1 << 2,
	String = 1 << 3,
}

public readonly struct SchemeSettingCustomFont
{
	public readonly string Path;
	public readonly string PathID;

	public SchemeSettingCustomFont(string pathID, string path) {
		PathID = pathID;
		Path = path;
	}
}

public readonly struct SchemeSettingFontStyle
{
	public readonly string Name;
	public readonly int Tall;

	public SchemeSettingFontStyle(string name, int tall) {
		Name = name;
		Tall = tall;
	}
}

public readonly struct SchemeSettingGeneric
{
	public readonly Color Color;
	public readonly float Float;
	public readonly int Integer;
	public readonly string String;
	public readonly SchemeSettingGenericType Type;

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

// TODO: Move everything up here to the engine/gui system later

public interface IScheme
{
	Color GetColor(ReadOnlySpan<char> key, Color defaultValue = default);

	float GetFloat(ReadOnlySpan<char> key, float defaultValue = default);

	SchemeSettingFontStyle GetFontStyle(ReadOnlySpan<char> key);

	int GetInt(ReadOnlySpan<char> key, int defaultValue = default);

	ReadOnlySpan<char> GetString(ReadOnlySpan<char> key, ReadOnlySpan<char> defaultValue = default);
}