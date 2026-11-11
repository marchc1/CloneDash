using Nucleus.Common.Graphics;
using Nucleus.Core;
using Nucleus.Particles;
using Nucleus.Rendering;
using Nucleus.Types;

namespace CloneDash.Scenes;

public enum MuseDash1HudProperty
{
	Active,
	Alpha,
	AnchorX,
	PivotX,
	ScaleX,
	ScaleY,
	Width,
	Count
}

public sealed class MuseDash1HudNode(int parent, string? sprite, Vector2F position, Vector2F size, Vector2F pivot, float scale = 1, bool active = true, float border = 0)
{
	public readonly float Border = border;
	public readonly int Parent = parent;
	public readonly Vector2F Pivot = pivot;
	public readonly Vector2F Position = position;
	public readonly float Scale = scale;
	public readonly Vector2F Size = size;
	public readonly string? Sprite = sprite;
	public readonly bool StartsActive = active;
	public ITexture? Texture;

	internal readonly float[] Values = new float[(int)MuseDash1HudProperty.Count];

	internal void ResetValues() {
		Values[(int)MuseDash1HudProperty.Active] = StartsActive ? 1 : 0;
		Values[(int)MuseDash1HudProperty.Alpha] = 1;
		Values[(int)MuseDash1HudProperty.AnchorX] = Position.X;
		Values[(int)MuseDash1HudProperty.PivotX] = Pivot.X;
		Values[(int)MuseDash1HudProperty.ScaleX] = Scale;
		Values[(int)MuseDash1HudProperty.ScaleY] = Scale;
		Values[(int)MuseDash1HudProperty.Width] = Size.W;
	}

	internal float Value(MuseDash1HudProperty property) => Values[(int)property];
}

public sealed class MuseDash1HudClip(float length, params (int Node, MuseDash1HudProperty Property, ParticleKeyframe[] Keys)[] curves)
{
	readonly (int Node, MuseDash1HudProperty Property, ParticleCurve Curve)[] curves = [.. curves.Select(curve => (curve.Node, curve.Property, new ParticleCurve(curve.Keys)))];

	public readonly float Length = length;

	public void Sample(float time, MuseDash1HudNode[] nodes) {
		time = Math.Clamp(time, 0, Length);
		foreach (var (node, property, curve) in curves)
			nodes[node].Values[(int)property] = curve.Evaluate(time);
	}
}

public sealed class MuseDash1HudPanel(Vector2F position, float scale, bool hold, MuseDash1HudClip clip, params MuseDash1HudNode[] nodes)
{
	const float REFERENCE_WIDTH = 1920;

	readonly Vector2F[] positions = new Vector2F[nodes.Length];
	readonly Vector2F[] scales = new Vector2F[nodes.Length];
	readonly bool[] visible = new bool[nodes.Length];

	public bool Playing { get; private set; }
	public float Time { get; set; }

	public void Initialize(Func<string, ITexture?> loadTexture) {
		foreach (var node in nodes)
			if (node.Sprite != null)
				node.Texture = loadTexture(node.Sprite);
	}

	public void Play() {
		Playing = true;
		Time = 0;
	}

	public void Render(float width, float height) {
		if (!Playing || Time < 0 || (!hold && Time > clip.Length))
			return;

		foreach (var node in nodes)
			node.ResetValues();
		clip.Sample(Time, nodes);

		float unit = width / REFERENCE_WIDTH;
		Vector2F center = new(width / 2, height / 2);
		for (int i = 0; i < nodes.Length; i++) {
			var node = nodes[i];
			Vector2F local = new(node.Value(MuseDash1HudProperty.AnchorX), node.Position.Y);
			Vector2F nodeScale = new(node.Value(MuseDash1HudProperty.ScaleX), node.Value(MuseDash1HudProperty.ScaleY));
			bool active = node.Value(MuseDash1HudProperty.Active) >= 0.5f;

			Vector2F parentPosition = node.Parent < 0 ? position : positions[node.Parent];
			Vector2F parentScale = node.Parent < 0 ? new(scale, scale) : scales[node.Parent];
			visible[i] = active && (node.Parent < 0 || visible[node.Parent]);
			positions[i] = new(parentPosition.X + local.X * parentScale.X, parentPosition.Y + local.Y * parentScale.Y);
			scales[i] = new(parentScale.X * nodeScale.X, parentScale.Y * nodeScale.Y);

			if (visible[i])
				Draw(node, positions[i], scales[i], center, unit);
		}
	}

	public void Stop() {
		Playing = false;
		Time = 0;
	}

	public void Think(float dt) {
		if (Playing)
			Time += dt;
	}

	static void Draw(MuseDash1HudNode node, Vector2F position, Vector2F scale, Vector2F center, float unit) {
		var texture = node.Texture;
		float alpha = node.Value(MuseDash1HudProperty.Alpha);
		float width = node.Value(MuseDash1HudProperty.Width), height = node.Size.H;
		if (texture == null || alpha <= 0 || scale.X <= 0 || scale.Y <= 0 || width <= 0)
			return;

		float left = -node.Value(MuseDash1HudProperty.PivotX) * width;
		float top = center.Y - (position.Y + scale.Y * (1 - node.Pivot.Y) * height) * unit;
		float screenHeight = scale.Y * height * unit;

		Graphics2D.SetTexture(texture);
		Graphics2D.SetDrawColor(255, 255, 255, (int)(Math.Clamp(alpha, 0, 1) * 255));

		void DrawSlice(float x, float w, float sU, float eU) {
			if (w > 0)
				Graphics2D.DrawTexturedRectangle(new Vector2F(center.X + (position.X + scale.X * x) * unit, top), new Vector2F(scale.X * w * unit, screenHeight), sU: sU, eU: eU);
		}

		if (node.Border <= 0) {
			DrawSlice(left, width, 0, 1);
			return;
		}

		float border = Math.Min(node.Border, width / 2);
		float u = node.Border / texture.GetWidth();
		DrawSlice(left, border, 0, u);
		DrawSlice(left + border, width - border * 2, u, 1 - u);
		DrawSlice(left + width - border, border, 1 - u, 1);
	}
}
