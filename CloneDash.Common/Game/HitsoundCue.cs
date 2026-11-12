using System.Runtime.CompilerServices;

namespace CloneDash.Common.Game;

public readonly struct HitsoundCue
{
	public readonly int Index;
	public readonly string Name;

	readonly int enumType;
	readonly int enumValue;

	internal HitsoundCue(int index, string name, int enumType, int enumValue) {
		Index = index;
		Name = name;
		this.enumType = enumType;
		this.enumValue = enumValue;
	}

	public bool TryGetEnum<TEnum>(out TEnum value) where TEnum : struct, Enum {
		if (enumType != HitsoundEnumType<TEnum>.Value) {
			value = default;
			return false;
		}

		int raw = enumValue;
		value = Unsafe.As<int, TEnum>(ref raw);
		return true;
	}
}
