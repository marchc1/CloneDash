using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Particles;
using Nucleus.Rendering;
using Nucleus.Types;
using Raylib_cs;
using System.Numerics;

namespace CloneDash.Scenes;

public enum MuseDash1FeverTipsClip
{
	Start,
	End,
	Failed,
	Light
}

public class MuseDash1FeverTips
{
	const string FONT = "Teko";
	const string PANEL = "PnlBattleBloodheir";
	const float PIXELS_PER_UNIT = 192;
	const float TEXT_SIZE = 72 * 1.433f;
	const string TEXT = "FEVER";

	enum Property
	{
		BubbleLightAlpha,
		ParEndActive,
		ParStartActive,
		Start2Active,
		Start2Alpha,
		Start2AnchorX,
		Start2RotationZ,
		Start2ScaleX,
		Start2ScaleY,
		StartActive,
		StartAlpha,
		StartScaleX,
		StartScaleY,
		Tips2Active,
		Tips2Alpha,
		TipsActive,
		TipsAlpha,
		TipsFxActive,
		TipsFxAlpha,
		TipsFxScaleX,
		TipsFxScaleY,
		TxtTouch2Active,
		TxtTouch2Alpha,
		TxtTouch2ScaleX,
		TxtTouchActive,
		TxtTouchAlpha,
		TxtTouchScaleX,
		Count
	}

	sealed class Clip(float length, bool loop, params (Property Property, ParticleKeyframe[] Keys)[] curves)
	{
		readonly (Property Property, ParticleCurve Curve)[] curves = [.. curves.Select(curve => (curve.Property, new ParticleCurve(curve.Keys)))];

		public readonly float Length = length;

		public void Sample(float time, Span<float> values) {
			time = loop ? time % Length : Math.Min(time, Length);
			foreach (var (property, curve) in curves)
				values[(int)property] = curve.Evaluate(time);
		}
	}

	const float INF = float.PositiveInfinity;

	static readonly Color ButtonColor = new(0, 234, 255);
	static readonly Color TextColor = new(204, 255, 255);
	static readonly Color Text2Color = new(19, 255, 255);

	static readonly float[] Defaults = CreateDefaults();

	static readonly Clip EndClip = new(0.5833333f, false,
		(Property.BubbleLightAlpha, [new(0, 0, 0, 0), new(0.0833f, 1, 0, 0), new(0.4167f, 0, 0, 0), new(0.5833f, 0, INF, 0)]),
		(Property.ParEndActive, [new(0, 1, 0, 0)]),
		(Property.ParStartActive, [new(0, 0, 0, 0)]),
		(Property.Start2Active, [new(0, 0, 0, 0)]),
		(Property.StartActive, [new(0, 1, 0, 0)]),
		(Property.StartAlpha, [new(0, 0, 0, 0), new(0.0833f, 1, 0, 0), new(0.25f, 0, 0, 0)]),
		(Property.StartScaleX, [new(0, 0, 0, 34.671f), new(0.0833f, 1, 0, 0), new(0.25f, 0, 0, 0)]),
		(Property.StartScaleY, [new(0, 0, 0, 30.305f), new(0.0833f, 1, 0, 0), new(0.25f, 0, 0, 0)]),
		(Property.Tips2Active, [new(0, 1, 0, 0)]),
		(Property.Tips2Alpha, [new(0, 0, 0, 0), new(0.0833f, 1, INF, 0), new(0.5833f, 0, 0, 0)]),
		(Property.TipsActive, [new(0, 1, 0, 0)]),
		(Property.TipsAlpha, [new(0, 0, 0, 0), new(0.0833f, 1, 0, 0), new(0.4167f, 0, 0, 0), new(0.5833f, 0, INF, 0)]),
		(Property.TipsFxActive, [new(0, 1, 0, 0)]),
		(Property.TipsFxAlpha, [new(0, 0, 0, 0), new(0.0833f, 1, INF, -2), new(0.5833f, 0, -2, 0)]),
		(Property.TipsFxScaleX, [new(0, 1, 0, 0), new(0.0833f, 1, INF, 0.568f), new(0.5833f, 1.15f, 0, 0)]),
		(Property.TipsFxScaleY, [new(0, 1, 0, 0), new(0.0833f, 1, INF, 0.564f), new(0.5833f, 1.15f, 0, 0)]),
		(Property.TxtTouch2Active, [new(0, 0, 0, 0)]),
		(Property.TxtTouchActive, [new(0, 0, 0, 0)])
	);

	static readonly Clip LightClip = new(0.4f, false,
		(Property.BubbleLightAlpha, [new(0, 1, 0, 0), new(0.2f, 1, INF, -0.043f), new(0.2167f, 0.9993f, -0.043f, -0.268f), new(0.2333f, 0.9948f, -0.268f, -0.702f), new(0.25f, 0.9831f, -0.702f, -1.348f), new(0.2667f, 0.9607f, -1.348f, -2.203f), new(0.2833f, 0.9239f, -2.203f, -3.263f), new(0.3f, 0.8696f, -3.263f, -4.521f), new(0.3167f, 0.7942f, -4.521f, -5.968f), new(0.3333f, 0.6947f, -5.968f, -7.592f), new(0.35f, 0.5682f, -7.592f, -9.381f), new(0.3667f, 0.4119f, -9.381f, -11.319f), new(0.3833f, 0.2232f, -11.319f, -13.392f), new(0.4f, 0, -13.392f, 0)])
	);

	static readonly Clip StandbyClip = new(0.6666667f, true,
		(Property.BubbleLightAlpha, [new(0, 0.3137f, 0, 0), new(0.1667f, 1, 0, 0), new(0.6667f, 0.3137f, 0, 0)]),
		(Property.Tips2Active, [new(0, 1, 0, 0)]),
		(Property.Tips2Alpha, [new(0, 0.7059f, 0, 0), new(0.1667f, 1, 0, 0), new(0.5f, 0.7059f, 0, 0), new(0.6667f, 0.7059f, INF, 0)]),
		(Property.TipsActive, [new(0, 1, 0, 0)]),
		(Property.TipsAlpha, [new(0, 0.7059f, 0, 0), new(0.1667f, 1, 0, 0), new(0.5f, 0.7059f, 0, 0), new(0.6667f, 0.7059f, INF, 0)]),
		(Property.TipsFxActive, [new(0, 1, 0, 0)]),
		(Property.TipsFxAlpha, [new(0, 0, 0, 0), new(0.1667f, 0.7059f, INF, -1.412f), new(0.6667f, 0, -1.412f, 0)]),
		(Property.TipsFxScaleX, [new(0, 1, 0, 0), new(0.1667f, 1, INF, 0.568f), new(0.6667f, 1.15f, 0, 0)]),
		(Property.TipsFxScaleY, [new(0, 1, 0, 0), new(0.1667f, 1, INF, 0.564f), new(0.6667f, 1.15f, 0, 0)]),
		(Property.TxtTouch2Active, [new(0, 1, 0, 0)]),
		(Property.TxtTouch2Alpha, [new(0, 0, 0, 0), new(0.1667f, 0.3137f, INF, -0.941f), new(0.5f, 0, -0.941f, 0)]),
		(Property.TxtTouch2ScaleX, [new(0, 1, 0, 0), new(0.1667f, 1, INF, 5.413f), new(0.6667f, 1.7f, 0, 0)]),
		(Property.TxtTouchActive, [new(0, 1, 0, 0)]),
		(Property.TxtTouchAlpha, [new(0, 0.7255f, 0, 0), new(0.1667f, 1, 0, 0), new(0.6667f, 0.7255f, 0, 0)]),
		(Property.TxtTouchScaleX, [new(0, 1, 0, 0), new(0.1667f, 1.05f, INF, 0), new(0.5f, 1, 0, 0)])
	);

	static readonly Clip StartClip = new(0.5833333f, false,
		(Property.BubbleLightAlpha, [new(0, 0, 0, 0), new(0.0833f, 1, 0, 0), new(0.4167f, 0.7059f, -1.373f, -1.373f), new(0.5833f, 0.3137f, 0, 0)]),
		(Property.ParStartActive, [new(0, 0, 0, 0), new(0.0833f, 1, INF, 0)]),
		(Property.Start2Active, [new(0, 1, 0, 0)]),
		(Property.Start2Alpha, [new(0, 0, 0, 0), new(0.0833f, 1, 0, 0), new(0.25f, 0, 0, 0)]),
		(Property.Start2AnchorX, [new(0, -274, 0, 0), new(0.25f, 371.7f, 0, 0)]),
		(Property.Start2RotationZ, [new(0, 0, 0, 0), new(0.25f, -100, -400, 0)]),
		(Property.Start2ScaleX, [new(0, 0, 0, 0), new(0.0833f, 1, 0, 0)]),
		(Property.Start2ScaleY, [new(0, 0, 0, 0), new(0.0833f, 1, 0, 0)]),
		(Property.StartActive, [new(0, 1, 0, 0)]),
		(Property.StartAlpha, [new(0, 0, 0, 0), new(0.0833f, 1, 0, 0), new(0.25f, 0, 0, 0)]),
		(Property.StartScaleX, [new(0, 0, 0, 14.928f), new(0.0833f, 1, 0, 0), new(0.25f, 0, 0, 0)]),
		(Property.StartScaleY, [new(0, 0, 0, 15.994f), new(0.0833f, 1, 0, 0), new(0.25f, 0, 0, 0)]),
		(Property.Tips2Active, [new(0, 1, 0, 0)]),
		(Property.Tips2Alpha, [new(0, 0, 0, 0), new(0.0833f, 1, INF, 0), new(0.5833f, 0, 0, 0)]),
		(Property.TipsActive, [new(0, 1, 0, 0)]),
		(Property.TipsAlpha, [new(0, 0, 0, 0), new(0.0833f, 1, 0, 0), new(0.4167f, 0.7059f, 0, 0), new(0.5833f, 0.7059f, INF, 0)]),
		(Property.TipsFxActive, [new(0, 1, 0, 0)]),
		(Property.TipsFxAlpha, [new(0, 0, 0, 0), new(0.0833f, 0.7059f, INF, -1.412f), new(0.5833f, 0, -1.412f, 0)]),
		(Property.TipsFxScaleX, [new(0, 1, 0, 0), new(0.0833f, 1, INF, 0.568f), new(0.5833f, 1.15f, 0, 0)]),
		(Property.TipsFxScaleY, [new(0, 1, 0, 0), new(0.0833f, 1, INF, 0.564f), new(0.5833f, 1.15f, 0, 0)])
	);

	readonly ParticleSystemManager particles = new();
	readonly float[] values = [.. Defaults];

	ITexture? bubbleLight;
	ITexture? button;
	Clip? clip;
	float clipTime;
	List<(ParticleSystemDefinition Definition, Matrix4x4 Transform)> endBursts = [];
	bool endPlayed;
	float lightTime = INF;
	ITexture? start;
	List<(ParticleSystemDefinition Definition, Matrix4x4 Transform)> startBursts = [];
	bool startPlayed;

	public void Initialize(Func<string, ITexture?> loadTexture) {
		bubbleLight = loadTexture("power_slider_white");
		button = loadTexture("fever_button");
		start = loadTexture("BtnFeverTipsStart");

		HashSet<AssetStudio.SerializedFile> pinned = [];
		var tips = Find(MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.GameObject>(PANEL), "Below", "UpUI", "SldPower", "Fill Area", "FeverButtonTips");
		startBursts = LoadBursts(Find(tips, "ParFeverTipsStart"), pinned);
		endBursts = LoadBursts(Find(tips, "ParFeverTipsEnd"), pinned);

		foreach (var file in pinned)
			MuseDash1Compatibility.StreamingAssets.Release(file);
	}

	public void Play(MuseDash1FeverTipsClip which) {
		switch (which) {
			case MuseDash1FeverTipsClip.Start: SetClip(StartClip); break;
			case MuseDash1FeverTipsClip.End: SetClip(EndClip); break;
			case MuseDash1FeverTipsClip.Failed: SetClip(null); break;
			case MuseDash1FeverTipsClip.Light: lightTime = 0; break;
		}
		Sample();
	}

	public void Render() {
		DrawCentered(bubbleLight, Property.BubbleLightAlpha, Color.White, new(2.1f, -38.5f), new Vector2F(860, 110) * 0.99938f, 0);

		DrawButton(Property.Tips2Active, Property.Tips2Alpha, 1, 1);
		DrawButton(Property.TipsActive, Property.TipsAlpha, 1, 1);
		DrawButton(Property.TipsFxActive, Property.TipsFxAlpha, Value(Property.TipsFxScaleX), Value(Property.TipsFxScaleY));

		if (Active(Property.StartActive))
			DrawCentered(start, Property.StartAlpha, Color.White, new(0, -56), new(3000 * Value(Property.StartScaleX), 600 * Value(Property.StartScaleY)), 0);
		if (Active(Property.Start2Active))
			DrawCentered(start, Property.Start2Alpha, Color.White, new(Value(Property.Start2AnchorX), -56), new(340 * Value(Property.Start2ScaleX), 495 * Value(Property.Start2ScaleY)), -Value(Property.Start2RotationZ));

		particles.Render(Matrix4x4.CreateScale(PIXELS_PER_UNIT, -PIXELS_PER_UNIT, 1));

		DrawText(Property.TxtTouch2Active, Property.TxtTouch2Alpha, Text2Color, Value(Property.TxtTouch2ScaleX));
		DrawText(Property.TxtTouchActive, Property.TxtTouchAlpha, TextColor, Value(Property.TxtTouchScaleX));
	}

	public void Reset() {
		clip = null;
		clipTime = 0;
		lightTime = INF;
		particles.Clear();
		Sample();
	}

	public void Think(double dt) {
		clipTime += (float)dt;
		lightTime += (float)dt;
		if (clip == StartClip && clipTime >= StartClip.Length) {
			clipTime -= StartClip.Length;
			clip = StandbyClip;
		}

		Sample();
		particles.Simulate((float)dt);
	}

	static float Alpha(float value) => Math.Clamp(value, 0, 1) * 255;

	static float[] CreateDefaults() {
		var values = new float[(int)Property.Count];
		values[(int)Property.Start2Alpha] = 1;
		values[(int)Property.Start2ScaleX] = 1;
		values[(int)Property.Start2ScaleY] = 1;
		values[(int)Property.StartAlpha] = 1;
		values[(int)Property.StartScaleX] = 1;
		values[(int)Property.StartScaleY] = 1;
		values[(int)Property.Tips2Alpha] = 0.541f;
		values[(int)Property.TipsAlpha] = 0.541f;
		values[(int)Property.TipsFxAlpha] = 0.541f;
		values[(int)Property.TipsFxScaleX] = 1;
		values[(int)Property.TipsFxScaleY] = 1;
		values[(int)Property.TxtTouch2Alpha] = 1;
		values[(int)Property.TxtTouch2ScaleX] = 1;
		values[(int)Property.TxtTouchAlpha] = 1;
		values[(int)Property.TxtTouchScaleX] = 1;
		return values;
	}

	static AssetStudio.GameObject? Find(AssetStudio.GameObject? root, params string[] path) {
		var transform = root?.GetFirstComponent<AssetStudio.Transform>();
		foreach (var name in path)
			transform = transform?.GetChildren().FirstOrDefault(child => child.GetGameObject()?.m_Name == name);
		return transform?.GetGameObject();
	}

	static List<(ParticleSystemDefinition Definition, Matrix4x4 Transform)> LoadBursts(AssetStudio.GameObject? root, HashSet<AssetStudio.SerializedFile> pinned) {
		List<(ParticleSystemDefinition Definition, Matrix4x4 Transform)> bursts = [];
		var transform = root?.GetFirstComponent<AssetStudio.Transform>();
		if (root == null || transform == null)
			return bursts;

		void Add(AssetStudio.GameObject gameObject, float x, float y) {
			if (gameObject.GetFirstComponent<AssetStudio.ParticleSystem>() == null)
				return;

			var definition = UnityParticleConverter.ConvertSystem(gameObject, texture => UnityAssetUtils.ConvertTexture(EngineCore.Level, texture),
				fileName => MuseDash1Compatibility.StreamingAssets.LoadDependenciesContaining(PANEL, new HashSet<string> { fileName }, pinned));
			bursts.Add((definition, Matrix4x4.CreateTranslation(x / PIXELS_PER_UNIT, y / PIXELS_PER_UNIT, 0)));
		}

		float rootX = transform.m_LocalPosition.X, rootY = transform.m_LocalPosition.Y;
		Add(root, rootX, rootY);
		foreach (var child in transform.GetChildren())
			if (child.GetGameObject() is AssetStudio.GameObject childObject)
				Add(childObject, rootX + child.m_LocalPosition.X, rootY + child.m_LocalPosition.Y);
		return bursts;
	}

	bool Active(Property property) => Value(property) >= 0.5f;

	void DrawButton(Property active, Property alpha, float scaleX, float scaleY) {
		if (button == null || !Active(active) || Value(alpha) <= 0)
			return;

		float width = 1140 * scaleX, height = 160 * scaleY;
		float left = 149 * scaleX, right = 147 * scaleX;
		float x = 2.101f - width / 2, y = -height;
		float textureWidth = button.GetWidth();

		Graphics2D.SetTexture(button);
		Graphics2D.SetDrawColor(ButtonColor.R, ButtonColor.G, ButtonColor.B, (int)Alpha(Value(alpha)));
		Graphics2D.DrawTexturedRectangle(new Vector2F(x, y), new Vector2F(left, height), eU: 149 / textureWidth);
		Graphics2D.DrawTexturedRectangle(new Vector2F(x + left, y), new Vector2F(width - left - right, height), sU: 149 / textureWidth, eU: 1 - 147 / textureWidth);
		Graphics2D.DrawTexturedRectangle(new Vector2F(x + width - right, y), new Vector2F(right, height), sU: 1 - 147 / textureWidth);
	}

	void DrawCentered(ITexture? texture, Property alpha, Color color, Vector2F center, Vector2F size, float rotation) {
		if (texture == null || Value(alpha) <= 0)
			return;

		Graphics2D.SetTexture(texture);
		Graphics2D.SetDrawColor(color.R, color.G, color.B, (int)Alpha(Value(alpha)));
		Graphics2D.DrawTexturedRectangle(center, size, rotation, size / 2);
	}

	void DrawText(Property active, Property alpha, Color color, float scaleX) {
		if (!Active(active) || Value(alpha) <= 0)
			return;

		Rlgl.PushMatrix();
		Rlgl.Translatef(0, -93, 0);
		Rlgl.Scalef(scaleX, 1, 1);
		Graphics2D.SetDrawColor(color.R, color.G, color.B, (int)Alpha(Value(alpha)));
		Graphics2D.DrawText(Vector2F.Zero, TEXT, FONT, TEXT_SIZE, Anchor.Center);
		Rlgl.PopMatrix();
	}

	void Sample() {
		Defaults.CopyTo(values, 0);
		clip?.Sample(clipTime, values);
		if (lightTime <= LightClip.Length)
			LightClip.Sample(lightTime, values);

		bool startActive = Active(Property.ParStartActive);
		if (startActive && !startPlayed)
			Spawn(startBursts);
		startPlayed = startActive;

		bool endActive = Active(Property.ParEndActive);
		if (endActive && !endPlayed)
			Spawn(endBursts);
		endPlayed = endActive;
	}

	void SetClip(Clip? next) {
		clip = next;
		clipTime = 0;
	}

	void Spawn(List<(ParticleSystemDefinition Definition, Matrix4x4 Transform)> bursts) {
		foreach (var (definition, transform) in bursts)
			particles.Create(definition, transform);
	}

	float Value(Property property) => values[(int)property];
}
