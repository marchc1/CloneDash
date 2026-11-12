using AssetStudio;
using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Files;
using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Particles;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.Util;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Quaternion = System.Numerics.Quaternion;
using Rlgl = Raylib_cs.Rlgl;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace CloneDash.Compatibility.Unity;

public delegate bool UnityCanvasBehaviourHandler(UnityCanvas canvas, CanvasNode node, MonoBehaviour behaviour, string className, System.Collections.Specialized.OrderedDictionary data);

public record UnityCanvasSettings(string Source, bool Perspective, float FieldOfView, float PlaneDistance, Vector2 ReferenceResolution, float MatchWidthOrHeight);

public class CanvasImage
{
	public Vector4 Border;
	public Vector4 Color = Vector4.One;
	public float FillAmount = 1;
	public bool PreserveAspect;
	public string? SpriteName;
	public Vector2 SpriteSize;
	public string? SpriteSource;
	public ITexture? Texture;
	public string? TextureName;
	public Vector4 TextureRect;
	public Vector2 TrimOffset;
	public int Type;
}

public class CanvasOutline
{
	public Vector4 Color;
	public Vector2 Distance;
	public bool UseGraphicAlpha;
}

public class CanvasText
{
	public int Alignment;
	public Vector4 Color = Vector4.One;
	public string Font = "";
	public string? FontSource;
	public int FontSize;
	public float LineSpacing = 1;
	public bool Localized;
	public string Text = "";
}

public class CanvasTween
{
	public int AnimationType;
	public bool AutoPlay;
	public float Delay;
	public float Duration;
	public int Ease;
	public List<(float time, float value, float inSlope, float outSlope)> EaseCurve = [];
	public Vector4 EndColor;
	public float EndFloat;
	public Vector2 EndVector2;
	public Vector3 EndVector3;
	public bool IsFrom;
	public bool IsRelative;
	public int Loops;
	public int LoopType;
	public bool OptionalBool;
	public int TargetType;

	internal bool Started;
	internal double StartTime;
	internal Vector4 From, To;
}

public class CanvasNode
{
	public bool Active = true;
	public Vector2 AnchorMin, AnchorMax, AnchoredPosition, Pivot = new(0.5f, 0.5f), SizeDelta;
	public bool Button;
	public readonly List<CanvasNode> Children = [];
	public Action? CustomRender;
	public float GroupAlpha = 1;
	public bool HorizontalFit;
	public CanvasImage? Image;
	public bool IsRect = true;
	public Vector3? LocalPositionOverride;
	public float LocalZ;
	public bool Mask;
	public Vector2 MaskOverflow;
	public string Name = "";
	public CanvasOutline? Outline;
	public CanvasNode? Parent;
	public ParticleSystemDefinition? ParticleDefinition;
	public string Path = "";
	public Quaternion Rotation = Quaternion.Identity;
	public Vector3 Scale = Vector3.One;
	public bool ShowMaskGraphic = true;
	public int? SortingOrder;
	public string Source = "";
	public CanvasText? Text;
	public readonly List<CanvasTween> Tweens = [];
	public readonly List<string> Notes = [];
	public readonly List<string> Unsupported = [];

	public Vector2 RectMin { get; internal set; }
	public Vector2 RectSize { get; internal set; }
	public Matrix4x4 World { get; internal set; } = Matrix4x4.Identity;

	internal Vector3 Euler;
	internal bool EulerAnimated;
	internal ParticleCollection? ParticleCollection;
	internal int ParticleScalingMode;
	internal float RenderAlpha = 1;

	public CanvasNode? Find(ReadOnlySpan<char> path) {
		var node = this;
		foreach (var range in path.Split('/')) {
			var name = path[range];
			CanvasNode? next = null;
			foreach (var child in node.Children)
				if (name.SequenceEqual(child.Name)) {
					next = child;
					break;
				}
			if (next == null)
				return null;
			node = next;
		}
		return node;
	}

	public bool IsActiveInHierarchy() {
		for (var node = this; node != null; node = node.Parent)
			if (!node.Active)
				return false;
		return true;
	}
}

class CanvasClipBinding
{
	public int Component;
	public int Curve;
	public string Property = "";
	public CanvasNode Target = null!;
}

class CanvasClip
{
	public readonly List<CanvasClipBinding> Bindings = [];
	public DecodedClip Decoded = null!;
	public string Name = "";
	public readonly List<string> Unmapped = [];
}

public class CanvasAnimator
{
	internal readonly Dictionary<string, AnimationClip> States = [];
	internal CanvasClip? Current;
	internal readonly Dictionary<string, CanvasClip> Decoded = [];
	public string? ControllerName;
	public string? DefaultState;
	public CanvasNode Node = null!;
	public float Time;

	public IEnumerable<string> StateNames => States.Keys;
}

[MarkForStaticConstruction]
public static class UnityCanvasConVars
{
	public static ConVar ui_canvas_report = new(nameof(ui_canvas_report), "0", FCvar.None, "Logs a report of every Unity canvas when it is imported: each node, where its data came from, and which animation bindings were mapped.");
}

public class UnityCanvas
{
	const int OP_DRAW = 0;
	const int OP_POP_MASK = 2;
	const int OP_PUSH_MASK = 1;

	static readonly uint HashActive = UnityCRC32.Hash("m_IsActive");
	static readonly Dictionary<uint, string> PropertyHashes = new[] {
		"m_AnchoredPosition.x", "m_AnchoredPosition.y", "m_SizeDelta.x", "m_SizeDelta.y", "m_AnchorMin.x", "m_AnchorMin.y", "m_AnchorMax.x", "m_AnchorMax.y",
		"m_Pivot.x", "m_Pivot.y", "m_LocalPosition.z", "m_Color.r", "m_Color.g", "m_Color.b", "m_Color.a", "m_FillAmount", "m_Alpha", "m_IsActive"
	}.ToDictionary(UnityCRC32.Hash);
	static readonly Dictionary<string, float> fontEmScales = [];
	static MemorySearchPath? fontSearchPath;
	static readonly Regex RichTextTags = new("<[^>]+>");

	readonly List<CanvasAnimator> animators = [];
	readonly List<(int Order, int Index, List<(int Op, CanvasNode Node)> Ops)> layers = [];
	readonly List<(CanvasNode Node, MonoBehaviour Behaviour, string ClassName, OrderedDictionary Data)> behaviours = [];
	readonly HashSet<string> missingFiles = [];
	readonly Dictionary<(string File, long PathID), CanvasNode> nodesByGameObject = [];
	readonly List<CanvasNode> particleNodes = [];
	readonly Dictionary<long, ITexture> textures;
	float lastHeight;
	Matrix4x4 lastProjection = Matrix4x4.Identity;
	float lastWidth;
	int nextRandomSeed;
	double time;

	public UnityCanvas(CanvasNode root, UnityCanvasSettings settings) : this(root, settings, []) { }

	UnityCanvas(CanvasNode root, UnityCanvasSettings settings, Dictionary<long, ITexture> textures) {
		Root = root;
		Settings = settings;
		this.textures = textures;
	}

	public IReadOnlyList<CanvasAnimator> Animators => animators;
	public IReadOnlySet<string> MissingFiles => missingFiles;
	public float PixelScale { get; private set; } = 1;
	public CanvasNode Root { get; }
	public UnityCanvasSettings Settings { get; }
	public Vector2 Size { get; private set; }

	public static UnityCanvas Import(GameObject root, UnityCanvasSettings settings, Func<IReadOnlySet<string>, bool>? loadMissingFiles = null, UnityCanvasBehaviourHandler? behaviourHandler = null) {
		Dictionary<long, ITexture> textures = [];
		UnityCanvas canvas;
		do {
			canvas = new UnityCanvas(new CanvasNode(), settings, textures);
			canvas.ImportNode(root, canvas.Root, "");
			foreach (var (node, behaviour, className, data) in canvas.behaviours)
				if (behaviourHandler == null || !behaviourHandler(canvas, node, behaviour, className, data))
					node.Unsupported.Add($"{className} [{Describe(behaviour)}]");
		} while (canvas.missingFiles.Count > 0 && loadMissingFiles != null && loadMissingFiles(canvas.missingFiles));

		foreach (var animator in canvas.animators)
			foreach (var state in animator.StateNames.ToList())
				canvas.GetClip(animator, state);
		canvas.Root.Active = true;
		return canvas;
	}

	public void Report() {
		if (UnityCanvasConVars.ui_canvas_report.GetBool())
			Logs.Info(Describe());
	}

	public string Describe() {
		var builder = new StringBuilder();
		builder.AppendLine($"==== Unity canvas import: {Root.Name} ({Root.Source})");
		if (missingFiles.Count > 0)
			builder.AppendLine($"unresolved files: {string.Join(", ", missingFiles)}");
		builder.AppendLine($"canvas settings (from {Settings.Source}): perspective={Settings.Perspective} fov={Settings.FieldOfView} planeDistance={Settings.PlaneDistance} reference={Settings.ReferenceResolution.X}x{Settings.ReferenceResolution.Y} match={Settings.MatchWidthOrHeight}");
		DescribeNode(builder, Root, 0);
		foreach (var animator in animators) {
			builder.AppendLine($"animator {animator.Node.Path} controller={animator.ControllerName} states=[{string.Join(", ", animator.States.Select(s => $"{s.Key}->{s.Value.m_Name}"))}]");
			foreach (var state in animator.States.Keys) {
				var clip = GetClip(animator, state);
				if (clip == null)
					continue;
				builder.AppendLine($"  clip {clip.Name}: {clip.Decoded.StopTime - clip.Decoded.StartTime:0.###}s, {clip.Bindings.Count} mapped curves, {clip.Unmapped.Count} unmapped");
				foreach (var group in clip.Bindings.GroupBy(b => b.Target.Path))
					builder.AppendLine($"    {(group.Key.Length == 0 ? "(self)" : group.Key)}: {string.Join(", ", group.Select(b => b.Property).Distinct())}");
				foreach (var unmapped in clip.Unmapped)
					builder.AppendLine($"    UNMAPPED {unmapped}");
			}
		}
		return builder.ToString();
	}

	public CanvasNode? Find(ReadOnlySpan<char> path) => path.Length == 0 ? Root : Root.Find(path);

	public bool Play(ReadOnlySpan<char> animatorPath, string state, float normalizedTime = 0) {
		var node = Find(animatorPath);
		var animator = animators.FirstOrDefault(a => a.Node == node);
		if (animator == null)
			return false;

		animator.Current = GetClip(animator, state);
		animator.Time = animator.Current == null ? 0 : normalizedTime * (animator.Current.Decoded.StopTime - animator.Current.Decoded.StartTime);
		if (animator.Current != null)
			Evaluate(animator.Current, Math.Min(animator.Time, animator.Current.Decoded.StopTime));
		return animator.Current != null;
	}

	public void Render(float width, float height) {
		Layout(width, height);

		layers.Clear();
		layers.Add((0, 0, []));
		Collect(Root, 0, 1);
		layers.Sort(static (a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Index.CompareTo(b.Index));

		Rlgl.DrawRenderBatchActive();
		bool culling = OpenGL.IsEnabled(OpenGL.CULL_FACE);
		var projection = Rlgl.GetMatrixProjection();
		var modelview = Rlgl.GetMatrixModelview();
		lastProjection = GetProjection(width, height);
		lastWidth = width;
		lastHeight = height;
		Rlgl.SetMatrixProjection(Matrix4x4.Transpose(lastProjection));
		Rlgl.SetMatrixModelView(Matrix4x4.Identity);
		Rlgl.MatrixMode(Raylib_cs.MatrixMode.ModelView);
		Rlgl.DisableBackfaceCulling();
		OpenGL.Clear(OpenGL.STENCIL_BUFFER_BIT);
		OpenGL.Enable(OpenGL.STENCIL_TEST);

		foreach (var (_, _, ops) in layers) {
			int depth = 0;
			SetStencil(depth, OpenGL.KEEP);
			foreach (var (op, node) in ops) {
				switch (op) {
					case OP_DRAW:
						DrawNode(node);
						if (node.CustomRender != null) {
							OpenGL.Enable(OpenGL.STENCIL_TEST);
							OpenGL.ColorMask(true, true, true, true);
							SetStencil(depth, OpenGL.KEEP);
						}
						break;
					case OP_PUSH_MASK:
						SetStencil(depth, OpenGL.INCR);
						if (!node.ShowMaskGraphic)
							OpenGL.ColorMask(false, false, false, false);
						DrawNode(node, true);
						Rlgl.DrawRenderBatchActive();
						OpenGL.ColorMask(true, true, true, true);
						SetStencil(++depth, OpenGL.KEEP);
						break;
					case OP_POP_MASK:
						SetStencil(depth, OpenGL.DECR);
						OpenGL.ColorMask(false, false, false, false);
						DrawNode(node, true, true);
						Rlgl.DrawRenderBatchActive();
						OpenGL.ColorMask(true, true, true, true);
						SetStencil(--depth, OpenGL.KEEP);
						break;
				}
			}
		}

		Rlgl.DrawRenderBatchActive();
		OpenGL.Disable(OpenGL.STENCIL_TEST);
		if (culling)
			Rlgl.EnableBackfaceCulling();
		Rlgl.SetMatrixProjection(projection);
		Rlgl.SetMatrixModelView(modelview);
	}

	public void Update(float deltaTime) {
		time += deltaTime;
		foreach (var animator in animators) {
			if (animator.Current == null)
				continue;

			animator.Time += deltaTime;
			Evaluate(animator.Current, Math.Min(animator.Time, animator.Current.Decoded.StopTime));
		}
		UpdateTweens(Root);
		foreach (var node in particleNodes)
			UpdateParticles(node, deltaTime);
	}

	bool AddMissingFile(string fileName) {
		missingFiles.Add(fileName);
		return false;
	}

	static float Ease(int ease, float t, List<(float time, float value, float inSlope, float outSlope)> curve) {
		const float BACK = 1.70158f;
		t = Math.Clamp(t, 0, 1);
		switch (ease) {
			case 0:
			case 6: return 1 - (1 - t) * (1 - t);
			case 1: return t;
			case 2: return 1 - MathF.Cos(t * MathF.PI / 2);
			case 3: return MathF.Sin(t * MathF.PI / 2);
			case 4: return -(MathF.Cos(MathF.PI * t) - 1) / 2;
			case 5: return t * t;
			case 7: return t < 0.5f ? 2 * t * t : 1 - MathF.Pow(-2 * t + 2, 2) / 2;
			case 8: return t * t * t;
			case 9: return 1 - MathF.Pow(1 - t, 3);
			case 10: return t < 0.5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
			case 11: return t * t * t * t;
			case 12: return 1 - MathF.Pow(1 - t, 4);
			case 13: return t < 0.5f ? 8 * t * t * t * t : 1 - MathF.Pow(-2 * t + 2, 4) / 2;
			case 14: return t * t * t * t * t;
			case 15: return 1 - MathF.Pow(1 - t, 5);
			case 16: return t < 0.5f ? 16 * MathF.Pow(t, 5) : 1 - MathF.Pow(-2 * t + 2, 5) / 2;
			case 17: return t == 0 ? 0 : MathF.Pow(2, 10 * t - 10);
			case 18: return t == 1 ? 1 : 1 - MathF.Pow(2, -10 * t);
			case 19: return t == 0 ? 0 : t == 1 ? 1 : t < 0.5f ? MathF.Pow(2, 20 * t - 10) / 2 : (2 - MathF.Pow(2, -20 * t + 10)) / 2;
			case 20: return 1 - MathF.Sqrt(1 - t * t);
			case 21: return MathF.Sqrt(1 - (t - 1) * (t - 1));
			case 22: return t < 0.5f ? (1 - MathF.Sqrt(1 - 4 * t * t)) / 2 : (MathF.Sqrt(1 - MathF.Pow(-2 * t + 2, 2)) + 1) / 2;
			case 23: return t == 0 || t == 1 ? t : -MathF.Pow(2, 10 * t - 10) * MathF.Sin((t * 10 - 10.75f) * (2 * MathF.PI / 3));
			case 24: return t == 0 || t == 1 ? t : MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * (2 * MathF.PI / 3)) + 1;
			case 25: return t == 0 || t == 1 ? t : t < 0.5f
				? -(MathF.Pow(2, 20 * t - 10) * MathF.Sin((20 * t - 11.125f) * (2 * MathF.PI / 4.5f))) / 2
				: MathF.Pow(2, -20 * t + 10) * MathF.Sin((20 * t - 11.125f) * (2 * MathF.PI / 4.5f)) / 2 + 1;
			case 26: return (BACK + 1) * t * t * t - BACK * t * t;
			case 27: return 1 + (BACK + 1) * MathF.Pow(t - 1, 3) + BACK * MathF.Pow(t - 1, 2);
			case 28: {
				float c = BACK * 1.525f;
				return t < 0.5f ? MathF.Pow(2 * t, 2) * ((c + 1) * 2 * t - c) / 2 : (MathF.Pow(2 * t - 2, 2) * ((c + 1) * (t * 2 - 2) + c) + 2) / 2;
			}
			case 29: return 1 - OutBounce(1 - t);
			case 30: return OutBounce(t);
			case 31: return t < 0.5f ? (1 - OutBounce(1 - 2 * t)) / 2 : (1 + OutBounce(2 * t - 1)) / 2;
			case 36: return 1;
			case 37: return DecodedClip.Eval(curve, t);
			default: return t;
		}
	}

	static Vector2 GetVector2(OrderedDictionary data, string key) {
		var value = (OrderedDictionary)data[key]!;
		return new(Convert.ToSingle(value["x"]), Convert.ToSingle(value["y"]));
	}

	static Vector3 GetVector3(OrderedDictionary data, string key) {
		var value = (OrderedDictionary)data[key]!;
		return new(Convert.ToSingle(value["x"]), Convert.ToSingle(value["y"]), Convert.ToSingle(value["z"]));
	}

	static Vector4 GetColor(OrderedDictionary data, string key) {
		var value = (OrderedDictionary)data[key]!;
		return new(Convert.ToSingle(value["r"]), Convert.ToSingle(value["g"]), Convert.ToSingle(value["b"]), Convert.ToSingle(value["a"]));
	}

	static bool GetBool(OrderedDictionary data, string key) => data.Contains(key) && Convert.ToInt32(data[key]) != 0;

	static float GetFloat(OrderedDictionary data, string key, float fallback = 0) => data.Contains(key) ? Convert.ToSingle(data[key]) : fallback;

	static int GetInt(OrderedDictionary data, string key, int fallback = 0) => data.Contains(key) ? Convert.ToInt32(data[key]) : fallback;

	public CanvasNode? FindNode(AssetStudio.Object obj) {
		GameObject? gameObject = obj as GameObject;
		if (gameObject == null && obj is AssetStudio.Component component)
			component.m_GameObject.TryGet(out gameObject);
		return gameObject != null && nodesByGameObject.TryGetValue((gameObject.assetsFile.fileName, gameObject.m_PathID), out var node) ? node : null;
	}

	public void SetSprite(CanvasImage image, Sprite sprite) => ImportSprite(image, sprite);

	public bool TryResolve<T>(AssetStudio.Object owner, OrderedDictionary pointer, out T result) where T : AssetStudio.Object
		=> TryResolve(owner.assetsFile, new PPtr<T>(Convert.ToInt32(pointer["m_FileID"]), Convert.ToInt64(pointer["m_PathID"]), owner.assetsFile), out result);

	bool TryResolve<T>(AssetStudio.Object owner, OrderedDictionary data, string key, out T result) where T : AssetStudio.Object {
		result = null!;
		if (data[key] is not OrderedDictionary pointer)
			return false;

		return TryResolve(owner.assetsFile, new PPtr<T>(Convert.ToInt32(pointer["m_FileID"]), Convert.ToInt64(pointer["m_PathID"]), owner.assetsFile), out result);
	}

	bool TryResolve<T>(SerializedFile file, PPtr<T> pptr, out T result) where T : AssetStudio.Object {
		result = null!;
		if (pptr.m_PathID == 0)
			return false;
		if (pptr.TryGet(out result!))
			return true;

		if (pptr.m_FileID > 0 && pptr.m_FileID <= file.m_Externals.Count)
			missingFiles.Add(Path.GetFileName(file.m_Externals[pptr.m_FileID - 1].fileName));
		return false;
	}

	public static string Describe(AssetStudio.Object obj) => $"{obj.assetsFile.fileName}#{obj.m_PathID}";

	public static void SetFont(CanvasText text, AssetStudio.Font font) {
		text.Font = RegisterFont(font);
		text.FontSource = Describe(font);
	}

	static void DescribeNode(StringBuilder builder, CanvasNode node, int depth) {
		var indent = new string(' ', depth * 2);
		builder.Append($"{indent}{(node.Name.Length == 0 ? "(root)" : node.Name)} [{node.Source}] active={node.Active}");
		if (node.IsRect)
			builder.Append($" anchors=({node.AnchorMin.X:0.###},{node.AnchorMin.Y:0.###})-({node.AnchorMax.X:0.###},{node.AnchorMax.Y:0.###}) pos=({node.AnchoredPosition.X:0.##},{node.AnchoredPosition.Y:0.##}) size=({node.SizeDelta.X:0.##},{node.SizeDelta.Y:0.##}) pivot=({node.Pivot.X:0.###},{node.Pivot.Y:0.###})");
		if (node.LocalZ != 0)
			builder.Append($" z={node.LocalZ:0.##}");
		if (node.Rotation != Quaternion.Identity)
			builder.Append($" rot=({node.Rotation.X:0.###},{node.Rotation.Y:0.###},{node.Rotation.Z:0.###},{node.Rotation.W:0.###})");
		if (node.Scale != Vector3.One)
			builder.Append($" scale=({node.Scale.X:0.###},{node.Scale.Y:0.###},{node.Scale.Z:0.###})");
		if (node.SortingOrder is int order)
			builder.Append($" sortingOrder={order}");
		if (node.GroupAlpha != 1)
			builder.Append($" groupAlpha={node.GroupAlpha:0.###}");
		builder.AppendLine();

		if (node.Image is CanvasImage image)
			builder.AppendLine($"{indent}  image sprite={image.SpriteName ?? "none"} [{image.SpriteSource}] texture={image.TextureName} rect=({image.TextureRect.X},{image.TextureRect.Y},{image.TextureRect.Z},{image.TextureRect.W}) type={image.Type} border=({image.Border.X},{image.Border.Y},{image.Border.Z},{image.Border.W}) color={FormatColor(image.Color)} preserveAspect={image.PreserveAspect}");
		if (node.Text is CanvasText text)
			builder.AppendLine($"{indent}  text \"{text.Text}\" font={text.Font} [{text.FontSource}] size={text.FontSize} emScale={EmScale(text.Font):0.###} alignment={text.Alignment} color={FormatColor(text.Color)} lineSpacing={text.LineSpacing}");
		if (node.Outline is CanvasOutline outline)
			builder.AppendLine($"{indent}  outline color={FormatColor(outline.Color)} distance=({outline.Distance.X},{outline.Distance.Y})");
		if (node.Mask)
			builder.AppendLine($"{indent}  mask showGraphic={node.ShowMaskGraphic}");
		if (node.HorizontalFit)
			builder.AppendLine($"{indent}  content size fitter: horizontal preferred");
		if (node.Button)
			builder.AppendLine($"{indent}  button");
		foreach (var tween in node.Tweens)
			builder.AppendLine($"{indent}  tween type={tween.AnimationType} target={tween.TargetType} delay={tween.Delay} duration={tween.Duration} ease={tween.Ease} loops={tween.Loops} loopType={tween.LoopType} from={tween.IsFrom} relative={tween.IsRelative} endFloat={tween.EndFloat} endV3=({tween.EndVector3.X},{tween.EndVector3.Y},{tween.EndVector3.Z}) endV2=({tween.EndVector2.X},{tween.EndVector2.Y}) endColor={FormatColor(tween.EndColor)} uniform={tween.OptionalBool}");
		foreach (var note in node.Notes)
			builder.AppendLine($"{indent}  {note}");
		foreach (var unsupported in node.Unsupported)
			builder.AppendLine($"{indent}  ignored {unsupported}");

		foreach (var child in node.Children)
			DescribeNode(builder, child, depth + 1);
	}

	static string FormatColor(Vector4 color) => $"({color.X:0.###},{color.Y:0.###},{color.Z:0.###},{color.W:0.###})";

	static float OutBounce(float t) {
		const float N = 7.5625f, D = 2.75f;
		if (t < 1 / D)
			return N * t * t;
		if (t < 2 / D)
			return N * (t -= 1.5f / D) * t + 0.75f;
		if (t < 2.5f / D)
			return N * (t -= 2.25f / D) * t + 0.9375f;
		return N * (t -= 2.625f / D) * t + 0.984375f;
	}

	static float EmScale(string font) => fontEmScales.TryGetValue(font, out var scale) ? scale : 1;

	static float ReadEmScale(byte[] data) {
		static int Read16(byte[] data, int offset) => (short)(data[offset] << 8 | data[offset + 1]);
		static int Read32(byte[] data, int offset) => data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3];

		if (data.Length < 12)
			return 1;

		int unitsPerEm = 0, ascent = 0, descent = 0;
		int tables = (ushort)Read16(data, 4);
		for (int i = 0; i < tables && 12 + i * 16 + 16 <= data.Length; i++) {
			int record = 12 + i * 16;
			int offset = Read32(data, record + 8);
			switch (System.Text.Encoding.ASCII.GetString(data, record, 4)) {
				case "head" when offset + 20 <= data.Length:
					unitsPerEm = (ushort)Read16(data, offset + 18);
					break;
				case "hhea" when offset + 8 <= data.Length:
					ascent = Read16(data, offset + 4);
					descent = Read16(data, offset + 6);
					break;
			}
		}
		return unitsPerEm > 0 && ascent - descent > 0 ? (float)(ascent - descent) / unitsPerEm : 1;
	}

	static string RegisterFont(AssetStudio.Font font) => MainThread.Invoke(() => RegisterFontOnGameThread(font));

	static string RegisterFontOnGameThread(AssetStudio.Font font) {
		string name = $"{font.m_Name}_{font.assetsFile.fileName}_{font.m_PathID}";
		if (!fontEmScales.ContainsKey(name) && font.m_FontData != null)
			fontEmScales[name] = ReadEmScale(font.m_FontData);

		var key = name.AsSpan().Hash(invariant: false);
		if (Graphics2D.FontManager.FontNameToFilepath.ContainsKey(key))
			return name;

		if (fontSearchPath == null) {
			fontSearchPath = new MemorySearchPath();
			filesystem.AddSearchPath("fonts", fontSearchPath);
		}
		using (var writer = fontSearchPath.Open(name, FileAccess.Write, FileMode.Create))
			writer!.Write(font.m_FontData);
		Graphics2D.FontManager.FontNameToFilepath[key] = new(name, "fonts");
		return name;
	}

	static void SetStencil(int reference, int pass) {
		Rlgl.DrawRenderBatchActive();
		OpenGL.StencilFunc(OpenGL.EQUAL, reference, 0xFF);
		OpenGL.StencilOp(OpenGL.KEEP, OpenGL.KEEP, pass);
	}

	static void Vertex(float x, float y, float u, float v) {
		Rlgl.TexCoord2f(u, v);
		Rlgl.Vertex3f(x, y, 0);
	}

	void ApplyTween(CanvasNode node, CanvasTween tween) {
		if (!tween.Started) {
			tween.Started = true;
			tween.StartTime = time;
			var start = ReadTweenValue(node, tween);
			var end = tween.AnimationType switch {
				5 => tween.OptionalBool ? new Vector4(tween.EndFloat, tween.EndFloat, tween.EndFloat, 0) : new Vector4(tween.EndVector3, 0),
				6 => tween.EndColor,
				7 => new Vector4(tween.EndFloat),
				21 => new Vector4(tween.EndVector2, 0, 0),
				_ => new Vector4(tween.EndVector3, 0)
			};
			if (tween.IsRelative)
				end += start;
			(tween.From, tween.To) = tween.IsFrom ? (end, start) : (start, end);
		}

		float elapsed = (float)(time - tween.StartTime) - tween.Delay;
		if (elapsed < 0 && !tween.IsFrom)
			return;

		float progress = 0;
		if (elapsed > 0 && tween.Duration > 0) {
			int loop = (int)(elapsed / tween.Duration);
			float local = elapsed - loop * tween.Duration;
			int loops = tween.Loops == 0 ? 1 : tween.Loops;
			if (loops > 0 && loop >= loops) {
				loop = loops - 1;
				local = tween.Duration;
			}
			progress = local / tween.Duration;
			if (tween.LoopType == 1 && loop % 2 == 1)
				progress = 1 - progress;
		}
		else if (elapsed > 0)
			progress = 1;

		WriteTweenValue(node, tween, Vector4.Lerp(tween.From, tween.To, Ease(tween.Ease, progress, tween.EaseCurve)));
	}

	void Collect(CanvasNode node, int layer, float alpha) {
		if (!node.Active)
			return;

		if (node != Root && node.SortingOrder is int order) {
			layer = layers.Count;
			layers.Add((order, layer, []));
		}

		alpha *= node.GroupAlpha;
		node.RenderAlpha = alpha;
		var ops = layers[layer].Ops;
		ops.Add((node.Mask ? OP_PUSH_MASK : OP_DRAW, node));
		foreach (var child in node.Children)
			Collect(child, layer, alpha);
		if (node.Mask)
			ops.Add((OP_POP_MASK, node));
	}

	void DrawImage(CanvasNode node, CanvasImage image, bool stencilOnly) {
		var color = image.Color;
		color.W *= node.RenderAlpha;
		if (!stencilOnly && color.W <= 0)
			return;

		var min = node.RectMin;
		var size = node.RectSize;
		if (stencilOnly) {
			min -= node.MaskOverflow;
			size += node.MaskOverflow * 2;
		}
		var texture = image.Texture;
		bool flipped = texture?.HasPublicFlags(PublicTextureFlags.RequiresFlippedV) ?? false;
		float width = texture?.GetWidth() ?? 1, height = texture?.GetHeight() ?? 1;
		float U(float x) => x / width;
		float V(float y) => flipped ? y / height : 1 - y / height;

		if (texture != null && image.Type == 0 && image.SpriteSize.X > 0) {
			if (image.PreserveAspect) {
				float aspect = image.SpriteSize.X / image.SpriteSize.Y;
				var fitted = size.X / size.Y > aspect ? new Vector2(size.Y * aspect, size.Y) : new Vector2(size.X, size.X / aspect);
				min += (size - fitted) * node.Pivot;
				size = fitted;
			}
			var scale = size / image.SpriteSize;
			min += image.TrimOffset * scale;
			size = new Vector2(image.TextureRect.Z, image.TextureRect.W) * scale;
		}

		Rlgl.SetTexture(texture?.GetTextureHandle() ?? 0);
		Rlgl.Begin(Raylib_cs.DrawMode.QUADS);
		Rlgl.Color4f(color.X, color.Y, color.Z, color.W);

		var rect = image.TextureRect;
		if (texture != null && image.Type == 1 && image.Border != Vector4.Zero) {
			Span<float> xs = [min.X, min.X + image.Border.X, min.X + size.X - image.Border.Z, min.X + size.X];
			Span<float> ys = [min.Y, min.Y + image.Border.Y, min.Y + size.Y - image.Border.W, min.Y + size.Y];
			Span<float> us = [U(rect.X), U(rect.X + image.Border.X), U(rect.X + rect.Z - image.Border.Z), U(rect.X + rect.Z)];
			Span<float> vs = [V(rect.Y), V(rect.Y + image.Border.Y), V(rect.Y + rect.W - image.Border.W), V(rect.Y + rect.W)];
			for (int y = 0; y < 3; y++)
				for (int x = 0; x < 3; x++) {
					Vertex(xs[x], ys[y], us[x], vs[y]);
					Vertex(xs[x + 1], ys[y], us[x + 1], vs[y]);
					Vertex(xs[x + 1], ys[y + 1], us[x + 1], vs[y + 1]);
					Vertex(xs[x], ys[y + 1], us[x], vs[y + 1]);
				}
		}
		else {
			float u0 = texture == null ? 0 : U(rect.X), u1 = texture == null ? 1 : U(rect.X + rect.Z);
			float v0 = texture == null ? 0 : V(rect.Y), v1 = texture == null ? 1 : V(rect.Y + rect.W);
			Vertex(min.X, min.Y, u0, v0);
			Vertex(min.X + size.X, min.Y, u1, v0);
			Vertex(min.X + size.X, min.Y + size.Y, u1, v1);
			Vertex(min.X, min.Y + size.Y, u0, v1);
		}

		Rlgl.End();
		Rlgl.SetTexture(0);
	}

	void DrawNode(CanvasNode node, bool stencilOnly = false, bool popping = false) {
		if (node.Image == null && node.Text == null && node.CustomRender == null && node.ParticleCollection == null && !stencilOnly)
			return;

		Rlgl.PushMatrix();
		Rlgl.LoadIdentity();
		Rlgl.MultMatrixf(Matrix4x4.Transpose(node.World));
		if (node.Image != null)
			DrawImage(node, node.Image, stencilOnly);
		else if (stencilOnly)
			DrawImage(node, new CanvasImage(), true);
		Rlgl.PopMatrix();

		if (node.Text != null && !popping)
			DrawText(node, node.Text);

		if (node.CustomRender != null && !stencilOnly) {
			Rlgl.DrawRenderBatchActive();
			Rlgl.PushMatrix();
			Rlgl.LoadIdentity();
			Rlgl.MultMatrixf(Matrix4x4.Transpose(Matrix4x4.CreateScale(1, -1, 1) * node.World));
			node.CustomRender();
			Rlgl.DrawRenderBatchActive();
			Rlgl.PopMatrix();
		}

		if (node.ParticleCollection != null && !stencilOnly && GetParticleFrame(node, out _, out var particleFrame)) {
			Rlgl.DrawRenderBatchActive();
			Rlgl.PushMatrix();
			Rlgl.LoadIdentity();
			Rlgl.MultMatrixf(Matrix4x4.Transpose(particleFrame));
			node.ParticleCollection.Render(Matrix4x4.Identity);
			Rlgl.PopMatrix();
		}
	}

	static bool GetParticleFrame(CanvasNode node, out Matrix4x4 controlPoint, out Matrix4x4 frame) {
		if (!Matrix4x4.Decompose(node.World, out var scale, out var rotation, out var translation)) {
			controlPoint = frame = Matrix4x4.Identity;
			return false;
		}

		var frameScale = node.ParticleScalingMode switch {
			0 => scale,
			1 => node.Scale,
			_ => Vector3.One
		};
		var shapeScale = node.ParticleScalingMode == 2 ? scale : Vector3.One;
		controlPoint = Matrix4x4.CreateScale(shapeScale) * Matrix4x4.CreateFromQuaternion(rotation);
		frame = Matrix4x4.CreateScale(frameScale) * Matrix4x4.CreateTranslation(translation);
		return true;
	}

	public bool HitTest(CanvasNode node, float x, float y) {
		if (!node.IsActiveInHierarchy() || lastWidth <= 0 || lastHeight <= 0)
			return false;

		var transform = node.World * lastProjection;
		Span<Vector2> corners = stackalloc Vector2[4];
		var min = node.RectMin;
		var max = node.RectMin + node.RectSize;
		Span<Vector2> local = [min, new(max.X, min.Y), max, new(min.X, max.Y)];
		for (int i = 0; i < 4; i++) {
			var clip = Vector4.Transform(new Vector4(local[i], 0, 1), transform);
			if (clip.W <= 0)
				return false;
			corners[i] = new((clip.X / clip.W + 1) / 2 * lastWidth, (1 - clip.Y / clip.W) / 2 * lastHeight);
		}

		bool? sign = null;
		for (int i = 0; i < 4; i++) {
			var a = corners[i];
			var b = corners[(i + 1) % 4];
			bool side = (b.X - a.X) * (y - a.Y) - (b.Y - a.Y) * (x - a.X) >= 0;
			if (sign != null && sign != side)
				return false;
			sign = side;
		}
		return true;
	}

	public static float MeasureTextWidth(CanvasText text)
		=> Graphics2D.GetTextSize(RichTextTags.Replace(text.Text, ""), text.Font, text.FontSize * EmScale(text.Font)).X;

	void DrawText(CanvasNode node, CanvasText text) {
		var color = text.Color;
		color.W *= node.RenderAlpha;
		if (color.W <= 0 || text.Text.Length == 0)
			return;

		float scale = Math.Max(PixelScale, 0.01f);
		var frame = Matrix4x4.CreateScale(1 / scale, -1 / scale, 1) * Matrix4x4.CreateTranslation(node.RectMin.X, node.RectMin.Y + node.RectSize.Y, 0) * node.World;
		var size = node.RectSize * scale;
		var horizontal = (TextAlignment)(text.Alignment % 3);
		var vertical = (TextAlignment)(text.Alignment / 3);
		float x = horizontal switch { TextAlignment.Center => size.X / 2, TextAlignment.Right => size.X, _ => 0 };
		float y = vertical switch { TextAlignment.Center => size.Y / 2, TextAlignment.Bottom => size.Y, _ => 0 };
		var content = RichTextTags.Replace(text.Text, "");
		float fontSize = text.FontSize * EmScale(text.Font) * scale;

		Rlgl.PushMatrix();
		Rlgl.LoadIdentity();
		Rlgl.MultMatrixf(Matrix4x4.Transpose(frame));
		if (node.Outline is CanvasOutline outline) {
			var outlineColor = outline.Color;
			outlineColor.W *= outline.UseGraphicAlpha ? color.W : node.RenderAlpha;
			SetDrawColor(outlineColor);
			var distance = outline.Distance * scale;
			foreach (var (dx, dy) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
				Graphics2D.DrawText(x + distance.X * dx, y - distance.Y * dy, content, text.Font, fontSize, horizontal, vertical);
		}
		SetDrawColor(color);
		Graphics2D.DrawText(x, y, content, text.Font, fontSize, horizontal, vertical);
		Rlgl.DrawRenderBatchActive();
		Rlgl.PopMatrix();
	}

	void Evaluate(CanvasClip clip, float t) {
		foreach (var binding in clip.Bindings) {
			var keys = clip.Decoded.Curves[binding.Curve].Keys;
			var node = binding.Target;
			switch (binding.Property) {
				case "m_IsActive":
					node.Active = DecodedClip.EvalStepped(keys, t) > 0.5f;
					continue;
			}

			float value = DecodedClip.Eval(keys, t);
			switch (binding.Property) {
				case "m_AnchoredPosition.x": node.AnchoredPosition.X = value; break;
				case "m_AnchoredPosition.y": node.AnchoredPosition.Y = value; break;
				case "m_SizeDelta.x": node.SizeDelta.X = value; break;
				case "m_SizeDelta.y": node.SizeDelta.Y = value; break;
				case "m_AnchorMin.x": node.AnchorMin.X = value; break;
				case "m_AnchorMin.y": node.AnchorMin.Y = value; break;
				case "m_AnchorMax.x": node.AnchorMax.X = value; break;
				case "m_AnchorMax.y": node.AnchorMax.Y = value; break;
				case "m_Pivot.x": node.Pivot.X = value; break;
				case "m_Pivot.y": node.Pivot.Y = value; break;
				case "m_LocalPosition.z": node.LocalZ = value; break;
				case "m_Alpha": node.GroupAlpha = value; break;
				case "m_FillAmount":
					if (node.Image != null)
						node.Image.FillAmount = value;
					break;
				case "m_Color.r": SetColor(node, 0, value); break;
				case "m_Color.g": SetColor(node, 1, value); break;
				case "m_Color.b": SetColor(node, 2, value); break;
				case "m_Color.a": SetColor(node, 3, value); break;
				case "position":
					var position = node.LocalPositionOverride ?? new Vector3(node.AnchoredPosition, node.LocalZ);
					position[binding.Component] = value;
					node.LocalPositionOverride = position;
					break;
				case "rotation":
					var rotation = node.Rotation;
					switch (binding.Component) {
						case 0: rotation.X = value; break;
						case 1: rotation.Y = value; break;
						case 2: rotation.Z = value; break;
						case 3: rotation.W = value; break;
					}
					node.Rotation = rotation;
					break;
				case "scale":
					var scale = node.Scale;
					scale[binding.Component] = value;
					node.Scale = scale;
					break;
				case "euler":
					node.Euler[binding.Component] = value;
					node.EulerAnimated = true;
					break;
			}
		}

		foreach (var binding in clip.Bindings)
			if (binding.Target.EulerAnimated) {
				var euler = binding.Target.Euler * (MathF.PI / 180);
				binding.Target.Rotation = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
				binding.Target.EulerAnimated = false;
			}
	}

	CanvasClip? GetClip(CanvasAnimator animator, string state) {
		if (animator.Decoded.TryGetValue(state, out var cached))
			return cached;
		if (!animator.States.TryGetValue(state, out var unityClip))
			return null;

		var clip = new CanvasClip { Decoded = DecodedClip.Decode(unityClip), Name = unityClip.m_Name };
		Dictionary<uint, CanvasNode> paths = [];
		MapPaths(animator.Node, "", paths);

		var curves = clip.Decoded.Curves;
		for (int i = 0; i < curves.Length; i++) {
			var binding = curves[i].Binding;
			int component = 0;
			for (int j = i - 1; j >= 0 && curves[j].Binding == binding; j--)
				component++;

			string? property = null;
			if (binding.typeID == ClassIDType.Transform)
				property = binding.attribute switch { 1 => "position", 2 => "rotation", 3 => "scale", 4 => "euler", _ => null };
			else if (binding.typeID != ClassIDType.AudioSource && binding.isPPtrCurve == 0 && PropertyHashes.TryGetValue(binding.attribute, out var name))
				property = name;

			if (property == null || !paths.TryGetValue(binding.path, out var target)) {
				if (component == 0)
					clip.Unmapped.Add($"path=0x{binding.path:X8}{(paths.TryGetValue(binding.path, out var known) ? $" ({known.Path})" : "")} type={binding.typeID} attribute=0x{binding.attribute:X8} pptr={binding.isPPtrCurve}");
				continue;
			}

			clip.Bindings.Add(new CanvasClipBinding { Component = component, Curve = i, Property = property, Target = target });
		}

		return animator.Decoded[state] = clip;
	}

	Matrix4x4 GetProjection(float width, float height) {
		if (!Settings.Perspective)
			return new Matrix4x4(2 / Size.X, 0, 0, 0, 0, 2 / Size.Y, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);

		const float NEAR = 0.3f, FAR = 1000f;
		float focal = 1 / MathF.Tan(Settings.FieldOfView * MathF.PI / 360);
		float a = (FAR + NEAR) / (FAR - NEAR), b = -2 * FAR * NEAR / (FAR - NEAR);
		return new Matrix4x4(focal * height / width, 0, 0, 0, 0, focal, 0, 0, 0, 0, a, 1, 0, 0, b, 0);
	}

	void ImportComponent(CanvasNode node, AssetStudio.Object component) {
		if (component is AssetStudio.Transform transform && component is not RectTransform && component.serializedType?.m_Type == null) {
			node.IsRect = false;
			node.LocalZ = transform.m_LocalPosition.Z;
			node.AnchoredPosition = new(transform.m_LocalPosition.X, transform.m_LocalPosition.Y);
			node.Rotation = new(transform.m_LocalRotation.X, transform.m_LocalRotation.Y, transform.m_LocalRotation.Z, transform.m_LocalRotation.W);
			node.Scale = new(transform.m_LocalScale.X, transform.m_LocalScale.Y, transform.m_LocalScale.Z);
			return;
		}

		if (component.serializedType?.m_Type == null) {
			node.Unsupported.Add($"{component.type} (no type tree) [{Describe(component)}]");
			return;
		}

		var data = component.ToType();
		string kind = component.type.ToString();
		if (component is MonoBehaviour behaviour && behaviour.m_Script.TryGet(out var script))
			kind = script.m_ClassName;

		switch (kind) {
			case "RectTransform":
			case "Transform":
				node.IsRect = kind == "RectTransform";
				var position = GetVector3(data, "m_LocalPosition");
				var rotation = (OrderedDictionary)data["m_LocalRotation"]!;
				node.Rotation = new(Convert.ToSingle(rotation["x"]), Convert.ToSingle(rotation["y"]), Convert.ToSingle(rotation["z"]), Convert.ToSingle(rotation["w"]));
				node.Scale = GetVector3(data, "m_LocalScale");
				node.LocalZ = position.Z;
				if (node.IsRect) {
					node.AnchorMin = GetVector2(data, "m_AnchorMin");
					node.AnchorMax = GetVector2(data, "m_AnchorMax");
					node.AnchoredPosition = GetVector2(data, "m_AnchoredPosition");
					node.SizeDelta = GetVector2(data, "m_SizeDelta");
					node.Pivot = GetVector2(data, "m_Pivot");
				}
				else {
					node.AnchoredPosition = new(position.X, position.Y);
					node.AnchorMin = node.AnchorMax = new(0.5f, 0.5f);
				}
				break;
			case "Image":
				var image = new CanvasImage {
					Color = GetColor(data, "m_Color"),
					FillAmount = GetFloat(data, "m_FillAmount", 1),
					PreserveAspect = GetBool(data, "m_PreserveAspect"),
					Type = GetInt(data, "m_Type")
				};
				if (TryResolve<Sprite>(component, data, "m_Sprite", out var sprite))
					ImportSprite(image, sprite);
				node.Image = image;
				break;
			case "Text":
				var fontData = (OrderedDictionary)data["m_FontData"]!;
				var text = new CanvasText {
					Alignment = GetInt(fontData, "m_Alignment"),
					Color = GetColor(data, "m_Color"),
					FontSize = GetInt(fontData, "m_FontSize"),
					LineSpacing = GetFloat(fontData, "m_LineSpacing", 1),
					Text = data["m_Text"] as string ?? ""
				};
				if (TryResolve<AssetStudio.Font>(component, fontData, "m_Font", out var font)) {
					text.Font = RegisterFont(font);
					text.FontSource = Describe(font);
				}
				node.Text = text;
				break;
			case "Mask":
				node.Mask = true;
				node.ShowMaskGraphic = GetBool(data, "m_ShowMaskGraphic");
				break;
			case "Outline":
				node.Outline = new CanvasOutline {
					Color = GetColor(data, "m_EffectColor"),
					Distance = GetVector2(data, "m_EffectDistance"),
					UseGraphicAlpha = GetBool(data, "m_UseGraphicAlpha")
				};
				break;
			case "Canvas":
				if (GetBool(data, "m_OverrideSorting"))
					node.SortingOrder = GetInt(data, "m_SortingOrder");
				break;
			case "CanvasGroup":
				node.GroupAlpha = GetFloat(data, "m_Alpha", 1);
				break;
			case "ContentSizeFitter":
				node.HorizontalFit = GetInt(data, "m_HorizontalFit") == 2;
				break;
			case "Button":
			case "Toggle":
				node.Button = true;
				break;
			case "Animator":
				ImportAnimator(node, component, data);
				break;
			case "DOTweenAnimation":
				ImportTween(node, data);
				break;
			case "ParticleSystem":
				if (component is AssetStudio.Component particleComponent && particleComponent.m_GameObject.TryGet(out var particleObject)) {
					node.ParticleDefinition = UnityParticleConverter.ConvertSystem(particleObject, LoadTexture, AddMissingFile);
					node.ParticleScalingMode = GetInt(data, "scalingMode");
					particleNodes.Add(node);
				}
				break;
			case "ParticleSystemRenderer":
				node.SortingOrder = GetInt(data, "m_SortingOrder");
				break;
			case "CanvasRenderer":
			case "GraphicRaycaster":
				break;
			default:
				if (component is MonoBehaviour monoBehaviour)
					behaviours.Add((node, monoBehaviour, kind, data));
				else
					node.Unsupported.Add($"{kind} [{Describe(component)}]");
				break;
		}
	}

	void ImportAnimator(CanvasNode node, AssetStudio.Object component, OrderedDictionary data) {
		if (!TryResolve<AssetStudio.Object>(component, data, "m_Controller", out var controllerObject) || controllerObject is not AnimatorController controller)
			return;

		var animator = new CanvasAnimator { ControllerName = controller.m_Name, Node = node };
		var names = controller.m_TOS.ToDictionary(pair => pair.Key, pair => pair.Value);
		foreach (var machine in controller.m_ControllerConstant.m_StateMachineArray)
			for (int i = 0; i < machine.m_StateConstantArray.Length; i++) {
				var state = machine.m_StateConstantArray[i];
				if (!names.TryGetValue(state.m_NameID, out var stateName) || state.m_BlendTreeConstantArray.Length == 0 || state.m_BlendTreeConstantArray[0].m_NodeArray.Length == 0)
					continue;
				if (i == machine.m_DefaultState)
					animator.DefaultState ??= stateName;

				uint clipIndex = state.m_BlendTreeConstantArray[0].m_NodeArray[0].m_ClipID;
				if (clipIndex < controller.m_AnimationClips.Count && controller.m_AnimationClips[(int)clipIndex].TryGet(out var clip))
					animator.States[stateName] = clip;
			}
		animators.Add(animator);
	}

	void ImportNode(GameObject gameObject, CanvasNode node, string path) {
		node.Name = gameObject.m_Name;
		node.Active = gameObject.m_IsActive;
		node.Path = path;
		node.Source = Describe(gameObject);
		nodesByGameObject[(gameObject.assetsFile.fileName, gameObject.m_PathID)] = node;

		AssetStudio.Transform? transform = null;
		foreach (var pointer in gameObject.m_Components) {
			if (!pointer.TryGet<AssetStudio.Object>(out var component))
				continue;
			if (component is AssetStudio.Transform t)
				transform = t;
			ImportComponent(node, component);
		}

		if (transform == null)
			return;

		foreach (var childTransform in transform.GetChildren()) {
			if (childTransform.GetGameObject() is not GameObject childObject)
				continue;

			var child = new CanvasNode { Parent = node };
			node.Children.Add(child);
			ImportNode(childObject, child, path.Length == 0 ? childObject.m_Name : $"{path}/{childObject.m_Name}");
		}
	}

	void ImportSprite(CanvasImage image, Sprite sprite) {
		image.SpriteName = sprite.m_Name;
		image.SpriteSource = Describe(sprite);
		image.SpriteSize = new(sprite.m_Rect.width, sprite.m_Rect.height);
		image.Border = new(sprite.m_Border.X, sprite.m_Border.Y, sprite.m_Border.Z, sprite.m_Border.W);
		var textureRect = sprite.m_RD.textureRect;
		image.TextureRect = new(textureRect.x, textureRect.y, textureRect.width, textureRect.height);
		image.TrimOffset = new(sprite.m_RD.textureRectOffset.X, sprite.m_RD.textureRectOffset.Y);

		var texture = sprite.m_RD.GetTexture();
		if (texture == null && !TryResolve(sprite.assetsFile, sprite.m_RD.texture, out texture))
			return;

		image.TextureName = $"{texture.m_Name} [{Describe(texture)}]";
		image.Texture = LoadTexture(texture);
	}

	void ImportTween(CanvasNode node, OrderedDictionary data) {
		var tween = new CanvasTween {
			AnimationType = GetInt(data, "animationType"),
			AutoPlay = GetBool(data, "autoPlay"),
			Delay = GetFloat(data, "delay"),
			Duration = GetFloat(data, "duration"),
			Ease = GetInt(data, "easeType"),
			EndColor = GetColor(data, "endValueColor"),
			EndFloat = GetFloat(data, "endValueFloat"),
			EndVector2 = GetVector2(data, "endValueV2"),
			EndVector3 = GetVector3(data, "endValueV3"),
			IsFrom = GetBool(data, "isFrom"),
			IsRelative = GetBool(data, "isRelative"),
			Loops = GetInt(data, "loops"),
			LoopType = GetInt(data, "loopType"),
			OptionalBool = GetBool(data, "optionalBool0"),
			TargetType = GetInt(data, "targetType")
		};
		if (data["easeCurve"] is OrderedDictionary curve && curve["m_Curve"] is System.Collections.IList keys)
			foreach (OrderedDictionary key in keys)
				tween.EaseCurve.Add((GetFloat(key, "time"), GetFloat(key, "value"), GetFloat(key, "inSlope"), GetFloat(key, "outSlope")));
		node.Tweens.Add(tween);
	}

	ITexture LoadTexture(Texture2D texture) {
		if (!textures.TryGetValue(texture.m_PathID, out var converted))
			textures[texture.m_PathID] = converted = MainThread.Invoke(() => UnityAssetUtils.ConvertTexture(EngineCore.Level, texture));
		return converted;
	}

	void Layout(float width, float height) {
		var reference = Settings.ReferenceResolution;
		float logWidth = MathF.Log2(width / reference.X), logHeight = MathF.Log2(height / reference.Y);
		PixelScale = MathF.Pow(2, logWidth + (logHeight - logWidth) * Settings.MatchWidthOrHeight);
		Size = new(width / PixelScale, height / PixelScale);

		var world = Matrix4x4.Identity;
		if (Settings.Perspective) {
			float scale = 2 * Settings.PlaneDistance * MathF.Tan(Settings.FieldOfView * MathF.PI / 360) / Size.Y;
			world = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(0, 0, Settings.PlaneDistance);
		}

		LayoutNode(Root, -Size / 2, Size, world);
	}

	void LayoutNode(CanvasNode node, Vector2 parentMin, Vector2 parentSize, Matrix4x4 parentWorld) {
		var size = (node.AnchorMax - node.AnchorMin) * parentSize + node.SizeDelta;
		if (node.HorizontalFit && node.Text != null)
			size.X = Graphics2D.GetTextSize(RichTextTags.Replace(node.Text.Text, ""), node.Text.Font, node.Text.FontSize * EmScale(node.Text.Font)).X;

		var anchor = parentMin + (node.AnchorMin + (node.AnchorMax - node.AnchorMin) * node.Pivot) * parentSize;
		var local = node.LocalPositionOverride ?? new Vector3(anchor + node.AnchoredPosition, node.LocalZ);
		if (!node.IsRect)
			local = new Vector3(node.AnchoredPosition, node.LocalZ);

		node.World = Matrix4x4.CreateScale(node.Scale) * Matrix4x4.CreateFromQuaternion(node.Rotation) * Matrix4x4.CreateTranslation(local) * parentWorld;
		node.RectSize = size;
		node.RectMin = -node.Pivot * size;
		foreach (var child in node.Children)
			LayoutNode(child, node.RectMin, size, node.World);
	}

	void MapPaths(CanvasNode node, string path, Dictionary<uint, CanvasNode> paths) {
		paths.TryAdd(UnityCRC32.Hash(path), node);
		foreach (var child in node.Children)
			MapPaths(child, path.Length == 0 ? child.Name : $"{path}/{child.Name}", paths);
	}

	Vector4 ReadTweenValue(CanvasNode node, CanvasTween tween) => tween.AnimationType switch {
		1 or 2 => new Vector4(node.AnchoredPosition, node.LocalZ, 0),
		3 or 4 => Vector4.Zero,
		5 => new Vector4(node.Scale, 0),
		6 => node.Image?.Color ?? node.Text?.Color ?? Vector4.One,
		7 => new Vector4(tween.TargetType == 2 ? node.GroupAlpha : (node.Image?.Color.W ?? node.Text?.Color.W ?? 1)),
		21 => new Vector4(node.SizeDelta, 0, 0),
		_ => Vector4.Zero
	};

	void SetColor(CanvasNode node, int component, float value) {
		if (node.Image != null) {
			var color = node.Image.Color;
			color[component] = value;
			node.Image.Color = color;
		}
		if (node.Text != null) {
			var color = node.Text.Color;
			color[component] = value;
			node.Text.Color = color;
		}
	}

	static void SetDrawColor(Vector4 color) => Graphics2D.SetDrawColor((int)(Math.Clamp(color.X, 0, 1) * 255), (int)(Math.Clamp(color.Y, 0, 1) * 255), (int)(Math.Clamp(color.Z, 0, 1) * 255), (int)(Math.Clamp(color.W, 0, 1) * 255));

	void UpdateParticles(CanvasNode node, float deltaTime) {
		if (node.ParticleDefinition == null || !node.IsActiveInHierarchy()) {
			node.ParticleCollection = null;
			return;
		}

		GetParticleFrame(node, out var transform, out _);
		if (node.ParticleCollection == null)
			node.ParticleCollection = new ParticleCollection(node.ParticleDefinition, transform, nextRandomSeed += 7919);
		else
			node.ParticleCollection.SetControlPoint(0, transform);
		node.ParticleCollection.Simulate(deltaTime);
	}

	void UpdateTweens(CanvasNode node) {
		if (!node.Active)
			return;

		foreach (var tween in node.Tweens)
			if (tween.AutoPlay)
				ApplyTween(node, tween);
		foreach (var child in node.Children)
			UpdateTweens(child);
	}

	void WriteTweenValue(CanvasNode node, CanvasTween tween, Vector4 value) {
		switch (tween.AnimationType) {
			case 1:
			case 2:
				node.AnchoredPosition = new(value.X, value.Y);
				node.LocalZ = value.Z;
				break;
			case 3:
			case 4:
				var euler = new Vector3(value.X, value.Y, value.Z) * (MathF.PI / 180);
				node.Rotation = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
				break;
			case 5:
				node.Scale = new(value.X, value.Y, value.Z);
				break;
			case 6:
				if (node.Image != null)
					node.Image.Color = value;
				if (node.Text != null)
					node.Text.Color = value;
				break;
			case 7:
				if (tween.TargetType == 2)
					node.GroupAlpha = value.X;
				else
					SetColor(node, 3, value.X);
				break;
			case 21:
				node.SizeDelta = new(value.X, value.Y);
				break;
		}
	}
}

public class UnityCanvasPanel : Panel
{
	public UnityCanvasPanel(Element? parent, UnityCanvas canvas) : base(parent) {
		Canvas = canvas;
		BorderSize = 0;
		SetPaintBackgroundEnabled(false);
	}

	public UnityCanvas Canvas { get; }
	public Func<float, float, bool>? Clicked;
	public List<UnityCanvas> Overlays { get; } = [];

	public override void Paint(float width, float height) {
		Canvas.Render(width, height);
		foreach (var overlay in Overlays)
			overlay.Render(width, height);
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (button != ButtonCode.MouseLeft || Clicked == null)
			return true;

		var position = GetMousePos();
		return Clicked(position.x, position.y);
	}

	protected override void OnThink() {
		float deltaTime = Math.Min(Level.RealtimeDeltaF, 0.1f);
		Canvas.Update(deltaTime);
		foreach (var overlay in Overlays)
			overlay.Update(deltaTime);
	}
}
