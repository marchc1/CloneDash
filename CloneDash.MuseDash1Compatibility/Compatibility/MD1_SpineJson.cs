using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace CloneDash.Compatibility.MuseDash;

public static class MD1_SpineJson
{
	class Writer
	{
		public readonly MemoryStream Stream = new();
		public readonly List<string> RefStrings = [];
		readonly Dictionary<string, int> refIndex = [];

		public void Byte(byte value) => Stream.WriteByte(value);
		public void Bool(bool value) => Stream.WriteByte(value ? (byte)1 : (byte)0);
		public void Int(int value) {
			Span<byte> buffer = stackalloc byte[4];
			BinaryPrimitives.WriteInt32BigEndian(buffer, value);
			Stream.Write(buffer);
		}
		public void Float(float value) => Int(BitConverter.SingleToInt32Bits(value));
		public void VarInt(int value) {
			uint remaining = (uint)value;
			while (remaining >= 0x80) {
				Stream.WriteByte((byte)(remaining | 0x80));
				remaining >>= 7;
			}
			Stream.WriteByte((byte)remaining);
		}
		public void SignedVarInt(int value) => VarInt((value << 1) ^ (value >> 31));
		public void String(string? text) {
			if (text == null) {
				VarInt(0);
				return;
			}
			var bytes = Encoding.UTF8.GetBytes(text);
			VarInt(bytes.Length + 1);
			Stream.Write(bytes);
		}
		public void Ref(string? text) {
			if (text == null) {
				VarInt(0);
				return;
			}
			if (!refIndex.TryGetValue(text, out int index)) {
				index = refIndex[text] = RefStrings.Count;
				RefStrings.Add(text);
			}
			VarInt(index + 1);
		}
	}

	public static bool IsJson(byte[] data) {
		int i = data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF ? 3 : 0;
		while (i < data.Length && data[i] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
			i++;
		return i < data.Length && data[i] == '{';
	}

	private static float Num(JsonElement element, string name, float fallback = 0)
		=> element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetSingle() : fallback;
	private static int Integer(JsonElement element, string name, int fallback = 0)
		=> element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : fallback;
	private static bool Flag(JsonElement element, string name, bool fallback)
		=> element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;
	private static string? Str(JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
			? value.GetString() : null;

	private static IEnumerable<JsonProperty> Props(JsonElement element, string name)
		=> element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value.EnumerateObject() : [];
	private static IEnumerable<JsonElement> Items(JsonElement element, string name)
		=> element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : [];

	private static void Unsupported(JsonElement element, string name, string what) {
		if (!element.TryGetProperty(name, out var value)) return;
		bool empty = value.ValueKind switch {
			JsonValueKind.Array => value.GetArrayLength() == 0,
			JsonValueKind.Object => !value.EnumerateObject().Any(),
			_ => true
		};
		if (!empty)
			throw new NotSupportedException($"JSON skeletons with {what} aren't supported yet.");
	}

	// RRGGBBAA, dark colors RRGGBB
	private static int Color(string? hex, int fallback) => string.IsNullOrEmpty(hex) ? fallback : (int)Convert.ToUInt32(hex, 16);

	private static void Curve(Writer writer, JsonElement key, bool last) {
		if (last) return;
		if (!key.TryGetProperty("curve", out var curve)) {
			writer.Byte(MuseDash1ModelConverter.CURVE_LINEAR);
			return;
		}
		switch (curve.ValueKind) {
			case JsonValueKind.String:
				writer.Byte(MuseDash1ModelConverter.CURVE_STEPPED);
				break;
			case JsonValueKind.Array: {
					// older exports: [c1, c2, c3, c4]
					var values = new List<float>();
					foreach (var element in curve.EnumerateArray())
						values.Add(element.GetSingle());
					writer.Byte(MuseDash1ModelConverter.CURVE_BEZIER);
					foreach (var value in values)
						writer.Float(value);
					break;
				}
			default:
				writer.Byte(MuseDash1ModelConverter.CURVE_BEZIER);
				writer.Float(curve.GetSingle());
				writer.Float(Num(key, "c2", 0));
				writer.Float(Num(key, "c3", 1));
				writer.Float(Num(key, "c4", 1));
				break;
		}
	}

	private static byte TransformMode(JsonElement bone) {
		switch (Str(bone, "transform")) {
			case "onlyTranslation": return MuseDash1ModelConverter.TRANSFORM_ONLY_TRANSLATION;
			case "noRotationOrReflection": return MuseDash1ModelConverter.TRANSFORM_NO_ROTATION_OR_REFLECTION;
			case "noScale": return MuseDash1ModelConverter.TRANSFORM_NO_SCALE;
			case "noScaleOrReflection": return MuseDash1ModelConverter.TRANSFORM_NO_SCALE_OR_REFLECTION;
		}
		// 2.x
		if (!Flag(bone, "inheritRotation", true)) return MuseDash1ModelConverter.TRANSFORM_NO_ROTATION_OR_REFLECTION;
		if (!Flag(bone, "inheritScale", true)) return MuseDash1ModelConverter.TRANSFORM_NO_SCALE;
		return MuseDash1ModelConverter.TRANSFORM_NORMAL;
	}

	private static byte BlendMode(string? blend) => blend switch {
		"additive" => MuseDash1ModelConverter.BLEND_MODE_ADDITIVE,
		"multiply" => MuseDash1ModelConverter.BLEND_MODE_MULTIPLY,
		"screen" => MuseDash1ModelConverter.BLEND_MODE_SCREEN,
		_ => MuseDash1ModelConverter.BLEND_MODE_NORMAL
	};

	private static void SkinAttachments(Writer writer, JsonElement attachments, Dictionary<string, int> slots) {
		var slotEntries = attachments.ValueKind == JsonValueKind.Object ? attachments.EnumerateObject().ToList() : [];
		writer.VarInt(slotEntries.Count);
		foreach (var slot in slotEntries) {
			writer.VarInt(slots[slot.Name]);
			var entries = slot.Value.EnumerateObject().ToList();
			writer.VarInt(entries.Count);
			foreach (var entry in entries) {
				var attachment = entry.Value;
				string type = Str(attachment, "type") ?? "region";
				if (type != "region")
					throw new NotSupportedException($"JSON skeletons with {type} attachments aren't supported yet.");

				writer.Ref(entry.Name);
				writer.Ref(Str(attachment, "name"));
				writer.Byte(MuseDash1ModelConverter.ATTACHMENT_REGION);
				writer.Ref(Str(attachment, "path"));
				writer.Float(Num(attachment, "rotation"));
				writer.Float(Num(attachment, "x"));
				writer.Float(Num(attachment, "y"));
				writer.Float(Num(attachment, "scaleX", 1));
				writer.Float(Num(attachment, "scaleY", 1));
				writer.Float(Num(attachment, "width", 32));
				writer.Float(Num(attachment, "height", 32));
				writer.Int(Color(Str(attachment, "color"), -1));
			}
		}
	}

	public static byte[] ToBinary(byte[] json) {
		int start = json.Length >= 3 && json[0] == 0xEF && json[1] == 0xBB && json[2] == 0xBF ? 3 : 0;
		using var doc = JsonDocument.Parse(json.AsMemory(start));
		var root = doc.RootElement;
		var writer = new Writer();

		Unsupported(root, "ik", "IK constraints");
		Unsupported(root, "transform", "transform constraints");
		Unsupported(root, "path", "path constraints");

		Dictionary<string, int> bones = [];
		var boneList = Items(root, "bones").ToList();
		writer.VarInt(boneList.Count);
		for (int i = 0; i < boneList.Count; i++) {
			var bone = boneList[i];
			string name = Str(bone, "name")!;
			bones[name] = i;
			writer.String(name);
			if (i != 0)
				writer.VarInt(bones[Str(bone, "parent")!]);
			writer.Float(Num(bone, "rotation"));
			writer.Float(Num(bone, "x"));
			writer.Float(Num(bone, "y"));
			writer.Float(Num(bone, "scaleX", 1));
			writer.Float(Num(bone, "scaleY", 1));
			writer.Float(Num(bone, "shearX"));
			writer.Float(Num(bone, "shearY"));
			writer.Float(Num(bone, "length"));
			writer.Byte(TransformMode(bone));
			writer.Bool(Flag(bone, "skin", false));
		}

		Dictionary<string, int> slots = [];
		var slotList = Items(root, "slots").ToList();
		writer.VarInt(slotList.Count);
		for (int i = 0; i < slotList.Count; i++) {
			var slot = slotList[i];
			string name = Str(slot, "name")!;
			slots[name] = i;
			writer.String(name);
			writer.VarInt(bones[Str(slot, "bone")!]);
			writer.Int(Color(Str(slot, "color"), -1));
			writer.Int(Color(Str(slot, "dark"), -1));
			writer.Ref(Str(slot, "attachment"));
			writer.Byte(BlendMode(Str(slot, "blend")));
		}

		// ik, transform, path
		writer.VarInt(0);
		writer.VarInt(0);
		writer.VarInt(0);

		// 3.8 has a list of skins, older exports map name -> attachments
		List<(string Name, JsonElement Attachments, JsonElement? Skin)> skins = [];
		if (root.TryGetProperty("skins", out var skinsEl)) {
			if (skinsEl.ValueKind == JsonValueKind.Array) {
				foreach (var skin in skinsEl.EnumerateArray())
					skins.Add((Str(skin, "name") ?? "default",
						skin.TryGetProperty("attachments", out var attachments) ? attachments : default, skin));
			}
			else if (skinsEl.ValueKind == JsonValueKind.Object) {
				foreach (var entry in skinsEl.EnumerateObject())
					skins.Add((entry.Name, entry.Value, null));
			}
		}

		var defaultSkin = skins.FirstOrDefault(s => s.Name == "default");
		if (defaultSkin.Name == null)
			writer.VarInt(0);
		else
			SkinAttachments(writer, defaultSkin.Attachments, slots);

		var named = new List<(string Name, JsonElement Attachments, JsonElement? Skin)>();
		foreach (var skin in skins) {
			if (skin.Name != "default")
				named.Add(skin);
		}
		writer.VarInt(named.Count);
		foreach (var skin in named) {
			writer.Ref(skin.Name);
			var skinBones = skin.Skin is { } skinElement ? Items(skinElement, "bones").ToList() : [];
			writer.VarInt(skinBones.Count);
			foreach (var skinBone in skinBones)
				writer.VarInt(bones[skinBone.GetString()!]);
			if (skin.Skin is { } skinJson) {
				Unsupported(skinJson, "ik", "skin constraints");
				Unsupported(skinJson, "transform", "skin constraints");
				Unsupported(skinJson, "path", "skin constraints");
			}
			writer.VarInt(0);
			writer.VarInt(0);
			writer.VarInt(0);
			SkinAttachments(writer, skin.Attachments, slots);
		}

		Dictionary<string, int> events = [];
		List<JsonElement> eventList = [];
		var eventProps = Props(root, "events").ToList();
		writer.VarInt(eventProps.Count);
		foreach (var eventProp in eventProps) {
			events[eventProp.Name] = eventList.Count;
			eventList.Add(eventProp.Value);
			writer.Ref(eventProp.Name);
			writer.SignedVarInt(Integer(eventProp.Value, "int"));
			writer.Float(Num(eventProp.Value, "float"));
			writer.String(Str(eventProp.Value, "string") ?? "");
			string? audio = Str(eventProp.Value, "audio");
			writer.String(audio);
			if (audio != null) {
				writer.Float(Num(eventProp.Value, "volume", 1));
				writer.Float(Num(eventProp.Value, "balance"));
			}
		}

		var animations = Props(root, "animations").ToList();
		writer.VarInt(animations.Count);
		foreach (var animation in animations) {
			var animationJson = animation.Value;
			writer.String(animation.Name);

			Unsupported(animationJson, "ik", "IK timelines");
			Unsupported(animationJson, "transform", "transform timelines");
			Unsupported(animationJson, "path", "path timelines");
			Unsupported(animationJson, "deform", "deform timelines");
			Unsupported(animationJson, "ffd", "deform timelines");

			var slotTimelines = Props(animationJson, "slots").ToList();
			writer.VarInt(slotTimelines.Count);
			foreach (var slot in slotTimelines) {
				writer.VarInt(slots[slot.Name]);
				var timelines = slot.Value.EnumerateObject().ToList();
				writer.VarInt(timelines.Count);
				foreach (var timeline in timelines) {
					var keys = timeline.Value.EnumerateArray().ToList();
					switch (timeline.Name) {
						case "attachment":
							writer.Byte(MuseDash1ModelConverter.SLOT_ATTACHMENT);
							writer.VarInt(keys.Count);
							foreach (var key in keys) {
								writer.Float(Num(key, "time"));
								writer.Ref(Str(key, "name"));
							}
							break;
						case "color":
							writer.Byte(MuseDash1ModelConverter.SLOT_COLOR);
							writer.VarInt(keys.Count);
							for (int i = 0; i < keys.Count; i++) {
								writer.Float(Num(keys[i], "time"));
								writer.Int(Color(Str(keys[i], "color"), -1));
								Curve(writer, keys[i], i == keys.Count - 1);
							}
							break;
						case "twoColor":
							writer.Byte(MuseDash1ModelConverter.SLOT_TWO_COLOR);
							writer.VarInt(keys.Count);
							for (int i = 0; i < keys.Count; i++) {
								writer.Float(Num(keys[i], "time"));
								writer.Int(Color(Str(keys[i], "light"), -1));
								writer.Int(Color(Str(keys[i], "dark"), 0));
								Curve(writer, keys[i], i == keys.Count - 1);
							}
							break;
						default:
							throw new NotSupportedException($"JSON slot timeline '{timeline.Name}' isn't supported yet.");
					}
				}
			}

			var boneTimelines = Props(animationJson, "bones").ToList();
			writer.VarInt(boneTimelines.Count);
			foreach (var bone in boneTimelines) {
				writer.VarInt(bones[bone.Name]);
				var timelines = bone.Value.EnumerateObject().ToList();
				writer.VarInt(timelines.Count);
				foreach (var timeline in timelines) {
					var keys = timeline.Value.EnumerateArray().ToList();
					byte type = timeline.Name switch {
						"rotate" => MuseDash1ModelConverter.BONE_ROTATE,
						"translate" => MuseDash1ModelConverter.BONE_TRANSLATE,
						"scale" => MuseDash1ModelConverter.BONE_SCALE,
						"shear" => MuseDash1ModelConverter.BONE_SHEAR,
						_ => throw new NotSupportedException($"JSON bone timeline '{timeline.Name}' isn't supported yet.")
					};
					float fallback = type == MuseDash1ModelConverter.BONE_SCALE ? 1 : 0;
					writer.Byte(type);
					writer.VarInt(keys.Count);
					for (int i = 0; i < keys.Count; i++) {
						writer.Float(Num(keys[i], "time"));
						if (type == MuseDash1ModelConverter.BONE_ROTATE)
							writer.Float(Num(keys[i], "angle"));
						else {
							writer.Float(Num(keys[i], "x", fallback));
							writer.Float(Num(keys[i], "y", fallback));
						}
						Curve(writer, keys[i], i == keys.Count - 1);
					}
				}
			}

			// ik, transform, path, deform
			writer.VarInt(0);
			writer.VarInt(0);
			writer.VarInt(0);
			writer.VarInt(0);

			var drawOrder = new List<JsonElement>(Items(animationJson, "drawOrder"));
			drawOrder.AddRange(Items(animationJson, "draworder"));
			writer.VarInt(drawOrder.Count);
			foreach (var key in drawOrder) {
				writer.Float(Num(key, "time"));
				var offsets = Items(key, "offsets").ToList();
				writer.VarInt(offsets.Count);
				foreach (var offset in offsets) {
					writer.VarInt(slots[Str(offset, "slot")!]);
					writer.VarInt(Integer(offset, "offset"));
				}
			}

			var eventKeys = Items(animationJson, "events").ToList();
			writer.VarInt(eventKeys.Count);
			foreach (var key in eventKeys) {
				var data = eventList[events[Str(key, "name")!]];
				writer.Float(Num(key, "time"));
				writer.VarInt(events[Str(key, "name")!]);
				writer.SignedVarInt(Integer(key, "int", Integer(data, "int")));
				writer.Float(Num(key, "float", Num(data, "float")));
				string? text = Str(key, "string");
				writer.Bool(text != null);
				if (text != null)
					writer.String(text);
				if (Str(data, "audio") != null) {
					writer.Float(Num(key, "volume", Num(data, "volume", 1)));
					writer.Float(Num(key, "balance", Num(data, "balance")));
				}
			}
		}

		var head = new Writer();
		var skeleton = root.TryGetProperty("skeleton", out var skeletonProp) ? skeletonProp : default;
		head.String(skeleton.ValueKind == JsonValueKind.Object ? Str(skeleton, "hash") ?? "" : "");
		head.String(skeleton.ValueKind == JsonValueKind.Object ? Str(skeleton, "spine") ?? "" : "");
		head.Float(skeleton.ValueKind == JsonValueKind.Object ? Num(skeleton, "x") : 0);
		head.Float(skeleton.ValueKind == JsonValueKind.Object ? Num(skeleton, "y") : 0);
		head.Float(skeleton.ValueKind == JsonValueKind.Object ? Num(skeleton, "width") : 0);
		head.Float(skeleton.ValueKind == JsonValueKind.Object ? Num(skeleton, "height") : 0);
		head.Bool(false);
		head.VarInt(writer.RefStrings.Count);
		foreach (var refString in writer.RefStrings)
			head.String(refString);

		writer.Stream.Position = 0;
		writer.Stream.CopyTo(head.Stream);
		return head.Stream.ToArray();
	}
}
