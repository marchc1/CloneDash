using System.Collections;
using System.Collections.Specialized;
using System.Text;

namespace CloneDash.Compatibility.Unity;

public readonly record struct OdinToken(int Type, string? Name, object? Value);

public static class UnityOdinSerialization
{
	public const int NamedExternalReferenceByIndex = 11;
	public const int NamedStartOfReferenceNode = 1;
	public const int UnnamedString = 40;

	public static bool TryGetSerializationData(OrderedDictionary data, out byte[] bytes, out IList references) {
		bytes = [];
		references = Array.Empty<object>();
		if (data["serializationData"] is not OrderedDictionary serialization || serialization["SerializedBytes"] is not IList raw)
			return false;

		bytes = new byte[raw.Count];
		for (int i = 0; i < raw.Count; i++)
			bytes[i] = Convert.ToByte(raw[i]);
		references = serialization["ReferencedUnityObjects"] as IList ?? Array.Empty<object>();
		return bytes.Length > 0;
	}

	public static List<OdinToken> Tokenize(byte[] bytes) {
		List<OdinToken> tokens = [];
		int position = 0;

		string ReadString() {
			int wide = bytes[position++];
			int length = BitConverter.ToInt32(bytes, position);
			position += 4;
			string value = wide == 0 ? Encoding.Latin1.GetString(bytes, position, length) : Encoding.Unicode.GetString(bytes, position, length * 2);
			position += wide == 0 ? length : length * 2;
			return value;
		}

		void SkipTypeEntry() {
			int type = bytes[position++];
			if (type == 47) {
				position += 4;
				ReadString();
			}
			else if (type == 48)
				position += 4;
		}

		while (position < bytes.Length) {
			int type = bytes[position++];
			bool named = type is 1 or 3 or 9 or 11 or 13 or 15 or 17 or 19 or 21 or 23 or 25 or 27 or 29 or 31 or 33 or 35 or 37 or 39 or 41 or 43 or 45 or 50;
			string? name = named ? ReadString() : null;
			object? value = null;
			switch (type) {
				case 1: case 2:
					SkipTypeEntry();
					value = BitConverter.ToInt32(bytes, position);
					position += 4;
					break;
				case 3: case 4:
					SkipTypeEntry();
					break;
				case 5: case 7: case 45: case 46:
					break;
				case 6:
					value = BitConverter.ToInt64(bytes, position);
					position += 8;
					break;
				case 8:
					position += 8 + BitConverter.ToInt32(bytes, position) * BitConverter.ToInt32(bytes, position + 4);
					break;
				case 9: case 10: case 11: case 12: case 23: case 24: case 25: case 26:
					value = BitConverter.ToInt32(bytes, position);
					position += 4;
					break;
				case 13: case 14: case 35: case 36: case 41: case 42:
					position += 16;
					break;
				case 15: case 16: case 17: case 18:
					value = bytes[position++];
					break;
				case 19: case 20: case 21: case 22:
					value = BitConverter.ToInt16(bytes, position);
					position += 2;
					break;
				case 27: case 28: case 29: case 30:
					value = BitConverter.ToInt64(bytes, position);
					position += 8;
					break;
				case 31: case 32:
					value = BitConverter.ToSingle(bytes, position);
					position += 4;
					break;
				case 33: case 34:
					value = BitConverter.ToDouble(bytes, position);
					position += 8;
					break;
				case 37: case 38:
					value = (char)BitConverter.ToUInt16(bytes, position);
					position += 2;
					break;
				case 39: case 40: case 50: case 51:
					value = ReadString();
					break;
				case 43: case 44:
					value = bytes[position++] != 0;
					break;
				case 47:
					position += 4;
					value = ReadString();
					break;
				case 48:
					position += 4;
					break;
				case 49:
					return tokens;
				default:
					return tokens;
			}
			tokens.Add(new(type, name, value));
		}
		return tokens;
	}
}
