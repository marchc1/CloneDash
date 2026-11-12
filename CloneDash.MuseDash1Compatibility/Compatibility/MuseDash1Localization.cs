using AssetStudio;
using CloneDash.Compatibility.Unity;
using System.Collections;
using System.Collections.Specialized;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace CloneDash.Compatibility.MuseDash;

public static class MuseDash1Localization
{
	public const string ENGLISH = "English";
	public const string OTHERS = "Others";

	public static readonly string[] ActiveOptions = [ENGLISH, "16:9", "Keyboard", "PC"];

	public static bool Apply(UnityCanvas canvas, CanvasNode node, MonoBehaviour behaviour, string className, OrderedDictionary data, string language = ENGLISH) {
		if (className != "Localization" || !UnityOdinSerialization.TryGetSerializationData(data, out var bytes, out var references))
			return false;

		var tokens = UnityOdinSerialization.Tokenize(bytes);
		string source = SourceType(tokens);
		var target = ResolveTarget(canvas, node, behaviour, tokens, references);
		var pairs = ReadPairs(tokens);
		if (pairs.Count == 0)
			return false;

		var chosen = pairs[0];
		string chosenName = "";
		foreach (var pair in pairs) {
			if (pair.Entry < 0 || pair.Entry >= references.Count || !canvas.TryResolve<MonoBehaviour>(behaviour, (OrderedDictionary)references[pair.Entry]!, out var entry))
				continue;
			if (entry.m_Name == language || ActiveOptions.Contains(entry.m_Name)) {
				chosen = pair;
				chosenName = entry.m_Name;
				break;
			}
			if (entry.m_Name == OTHERS && chosenName != OTHERS) {
				chosen = pair;
				chosenName = OTHERS;
			}
		}
		language = chosenName.Length > 0 ? chosenName : language;

		var value = chosen.Value;
		switch (source) {
			case "TextSource":
				if (target.Text == null)
					return true;
				target.Text.Localized = true;
				if (value.FirstOrDefault(token => token.Value is string).Value is string text) {
					target.Text.Text = text;
					node.Notes.Add($"localized ({language}) text of {target.Path} = \"{text}\"");
				}
				return true;
			case "FontSizeSource":
				if (target.Text != null && Numbers(value).FirstOrDefault() is float size and > 0) {
					target.Text.FontSize = (int)size;
					node.Notes.Add($"localized ({language}) font size of {target.Path} = {size}");
				}
				return true;
			case "RectTramPosSource":
				if (Numbers(value) is [float x, float y, ..]) {
					target.AnchoredPosition = new Vector2(x, y);
					node.Notes.Add($"localized ({language}) position of {target.Path} = ({x}, {y})");
				}
				return true;
			case "RectTramSizeSource":
				if (Numbers(value) is [float width, float height, ..]) {
					target.SizeDelta = new Vector2(width, height);
					node.Notes.Add($"localized ({language}) size of {target.Path} = ({width}, {height})");
				}
				return true;
			case "ScaleSource":
				if (Numbers(value) is [float scaleX, float scaleY, float scaleZ, ..]) {
					target.Scale = new Vector3(scaleX, scaleY, scaleZ);
					node.Notes.Add($"localized ({language}) scale of {target.Path} = ({scaleX}, {scaleY}, {scaleZ})");
				}
				return true;
			case "GameObjectSource":
				if (value.FirstOrDefault(token => token.Value is bool).Value is bool active) {
					target.Active = active;
					node.Notes.Add($"localized ({language}) active of {target.Path} = {active}");
				}
				return true;
			case "ComponentSource":
				if (value.FirstOrDefault(token => token.Value is bool).Value is bool enabled && !enabled) {
					target.Text = null;
					target.Image = null;
					node.Notes.Add($"localized ({language}) disabled graphic of {target.Path}");
				}
				return true;
			case "ImageSource":
				if (target.Image != null && Reference<Sprite>(canvas, behaviour, value, references) is Sprite sprite) {
					canvas.SetSprite(target.Image, sprite);
					node.Notes.Add($"localized ({language}) sprite of {target.Path} = {sprite.m_Name}");
				}
				return true;
			case "ChildrenSource":
				if (Reference<AssetStudio.Object>(canvas, behaviour, value, references) is AssetStudio.Object child && canvas.FindNode(child) is CanvasNode activeChild) {
					foreach (var sibling in target.Children)
						sibling.Active = sibling == activeChild;
					node.Notes.Add($"localized ({language}) active child of {target.Path} = {activeChild.Name}");
				}
				return true;
			default:
				node.Unsupported.Add($"Localization {source} [{behaviour.assetsFile.fileName}#{behaviour.m_PathID}]");
				return true;
		}
	}

	public static void ApplyGlobalFonts(UnityCanvas canvas, IEnumerable<SerializedFile> files, string language = ENGLISH) {
		var settings = files.SelectMany(file => file.Objects).OfType<MonoBehaviour>()
			.FirstOrDefault(behaviour => behaviour.m_Script.TryGet(out var script) && script.m_ClassName == "LocalizationSettings");
		if (settings == null || !UnityOdinSerialization.TryGetSerializationData(settings.ToType(), out var bytes, out var references))
			return;

		HashSet<string> replaceable = [];
		AssetStudio.Font? languageFont = null;
		int fontIndex = -1;
		foreach (var token in UnityOdinSerialization.Tokenize(bytes)) {
			if (token.Name == "schemeEntry")
				break;
			if (token.Type != UnityOdinSerialization.NamedExternalReferenceByIndex || token.Value is not int index || index < 0 || index >= references.Count)
				continue;

			if (token.Name == "font")
				fontIndex = index;
			else if (token.Name == "optionEntry" && fontIndex >= 0) {
				if (canvas.TryResolve<AssetStudio.Font>(settings, (OrderedDictionary)references[fontIndex]!, out var font)) {
					replaceable.Add(UnityCanvas.Describe(font));
					if (canvas.TryResolve<MonoBehaviour>(settings, (OrderedDictionary)references[index]!, out var entry) && entry.m_Name == language)
						languageFont = font;
				}
				fontIndex = -1;
			}
		}

		if (languageFont != null)
			ReplaceFonts(canvas.Root, replaceable, languageFont, language);
	}

	static void ReplaceFonts(CanvasNode node, HashSet<string> replaceable, AssetStudio.Font font, string language) {
		if (node.Text is CanvasText text && text.Localized && text.FontSource != null && replaceable.Contains(text.FontSource) && text.FontSource != UnityCanvas.Describe(font)) {
			node.Notes.Add($"localized ({language}) global font {text.Font} -> {font.m_Name}");
			UnityCanvas.SetFont(text, font);
		}
		foreach (var child in node.Children)
			ReplaceFonts(child, replaceable, font, language);
	}

	static List<float> Numbers(List<OdinToken> tokens) {
		List<float> numbers = [];
		foreach (var token in tokens)
			switch (token.Value) {
				case float number when token.Type is 31 or 32: numbers.Add(number); break;
				case int number when token.Type is 23 or 24: numbers.Add(number); break;
				case double number: numbers.Add((float)number); break;
			}
		return numbers;
	}

	static List<(List<OdinToken> Value, int Entry)> ReadPairs(List<OdinToken> tokens) {
		List<(List<OdinToken> Value, int Entry)> pairs = [];
		List<OdinToken>? value = null;
		foreach (var token in tokens) {
			if (token.Name == "value") {
				value = [token];
				continue;
			}
			if (value == null)
				continue;
			if (token.Name == "optionEntry" && token.Type == UnityOdinSerialization.NamedExternalReferenceByIndex) {
				pairs.Add((value, (int)token.Value!));
				value = null;
				continue;
			}
			value.Add(token);
		}
		return pairs;
	}

	static T? Reference<T>(UnityCanvas canvas, MonoBehaviour behaviour, List<OdinToken> value, IList references) where T : AssetStudio.Object {
		var token = value.FirstOrDefault(token => token.Type == UnityOdinSerialization.NamedExternalReferenceByIndex);
		if (token.Value is not int index || index < 0 || index >= references.Count)
			return null;
		return canvas.TryResolve<T>(behaviour, (OrderedDictionary)references[index]!, out var result) ? result : null;
	}

	static CanvasNode ResolveTarget(UnityCanvas canvas, CanvasNode node, MonoBehaviour behaviour, List<OdinToken> tokens, IList references) {
		var token = tokens.FirstOrDefault(token => token.Name == "target" && token.Type == UnityOdinSerialization.NamedExternalReferenceByIndex);
		if (token.Value is int index && index >= 0 && index < references.Count
			&& canvas.TryResolve<AssetStudio.Object>(behaviour, (OrderedDictionary)references[index]!, out var target)
			&& canvas.FindNode(target) is CanvasNode targetNode)
			return targetNode;
		return node;
	}

	static string SourceType(List<OdinToken> tokens) {
		int index = tokens.FindIndex(token => token.Name == "sourceType");
		for (int i = index + 1; index >= 0 && i < tokens.Count; i++)
			if (tokens[i].Type == UnityOdinSerialization.UnnamedString && tokens[i].Value is string type) {
				var name = type.Split(',')[0];
				return name[(name.LastIndexOf('.') + 1)..];
			}
		return "";
	}
}
