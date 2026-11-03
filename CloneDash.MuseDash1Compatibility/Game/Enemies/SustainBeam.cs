using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.Game.Logic;
using CloneDash.Scenes;
using CloneDash.Settings;

using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Types;

using Raylib_cs;

namespace CloneDash.Game.Entities
{
	public class SustainBeam : DashEnemy
	{
		readonly HitsoundComponent hitsounds;
		readonly PathwaysComponent pathways;
		readonly ISustainManager sustains;

		public SustainBeam(DashEnemyComponents components, HitsoundComponent hitsounds, PathwaysComponent pathways, ISustainManager sustains) : base(MuseDash1EntityType.SustainBeam, components) {
			this.hitsounds = hitsounds;
			this.pathways = pathways;
			this.sustains = sustains;
			Interactivity = EntityInteractivity.Sustain;
			DoesDamagePlayer = true;
		}
		public override void Initialize() {
			base.Initialize();
		}

		public bool WasHit { get; private set; } = false;
		public bool HeldState { get; private set; } = false;
		public bool StopAcceptingInput { get; private set; } = false;

		private double lastCheckTime;

		public override void OnReset() {
			base.OnReset();
			WasHit = false;
			HeldState = false;
			StopAcceptingInput = false;
		}

		protected override void OnHit(PathwayId attackedPath, double distanceToHit) {
			if (HeldState == true)
				return;
			if (StopAcceptingInput == true)
				return;

			HeldState = true;
			WasHit = true;
			ForceDraw = true;
			lastCheckTime = GetConductor().Time;
			sustains.StartSustainBeam(this);
			Components.Combo.Add();
			Components.Rules.AddFever(FeverGiven, this);
			GetStats().Hit(this, distanceToHit);
		}

		protected override void OnMiss() {
			if (HeldState == false) {
				sustains.FailSustainBeam(this);
				PunishPlayer();
				GetStats().Miss(this);
			}
		}

		public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
			return NMath.InRange(GetVisualTimeUntilHit(), -2 - Length, 2);
		}

		protected override void OnReward() { }

		public override void Think(FrameState frameState) {
			if (HeldState) {
				// check if sustain complete
				sustains.ThinkSustainBeam(this);
			}
		}

		public float StartPosition { get; private set; }
		public float RotationDegsPerSecond = 200;
		public MD1_CurvePath? Curve { get; set; }

		private const int MAX_CURVE_SAMPLES = 256;

		private double[]? curveTimes;
		private float[]? curveValues;
		private float[]? curveDistances;

		private double curveStartTime() => HeldState ? Math.Clamp(GetConductor().Time - InputSettings.VisualOffset, HitTime, HitTime + Length) : HitTime;
		private float worldY(double time) => Curve == null ? Components.Playfield.GetPathway(this).Position.Y : Components.Scenes.GetNormalizedPosition(Curve.Evaluate(time)).Y;
		private float pathY(double time) => -worldY(time);

		public float GetHeldY() => worldY(curveStartTime());

		private void buildCurveSamples(IMuseDash1SceneInstance scene) {
			curveTimes = null;
			curveValues = null;
			curveDistances = null;

			if (Curve == null)
				return;

			Span<double> times = stackalloc double[MAX_CURVE_SAMPLES];
			Span<float> values = stackalloc float[MAX_CURVE_SAMPLES];
			int count = Curve.Sample(HitTime, HitTime + Length, Math.Max(Length / (MAX_CURVE_SAMPLES / 2), 1d / 240), times, values);

			if (count < 2)
				return;

			curveTimes = times[..count].ToArray();
			curveValues = values[..count].ToArray();
			curveDistances = new float[count];

			var scrollSpeed = ScrollSpeed;
			for (int i = 1; i < count; i++) {
				var dx = (float)((times[i] - times[i - 1]) * scrollSpeed);
				var dy = scene.GetNormalizedPosition(values[i]).Y - scene.GetNormalizedPosition(values[i - 1]).Y;
				curveDistances[i] = curveDistances[i - 1] + MathF.Sqrt(dx * dx + dy * dy);
			}
		}

		private void drawStartQuad(ref FrameState fs, float x) {
			x -= (float)InputSettings.VisualOffset;

			var tex = start;
			if (tex == null) return;

			var xpos = (HeldState ? Components.Playfield.GetPathway(this).Position.X : (float)XPosFromTimeOffset(x));
			var ypos = pathY(curveStartTime());
			var rot = (float)((GetConductor().Time * RotationDegsPerSecond) % 360) * -1;

			var w = tex.GetWidth() * MuseDash1Gamemode.GlobalScale;
			var h = tex.GetHeight() * MuseDash1Gamemode.GlobalScale;

			if (tex.HasPublicFlags(PublicTextureFlags.RequiresFlippedV))
				Raylib.DrawTexturePro(tex.ToRaylibTexture(), new(0, 0, tex.GetWidth(), -tex.GetHeight()), new(xpos, ypos, w * 2, h * 2), new(w, h), rot, Color.White with { A = beamAlpha });
			else
				Raylib.DrawTexturePro(tex.ToRaylibTexture(), new(0, 0, tex.GetWidth(), tex.GetHeight()), new(xpos, ypos, w * 2, h * 2), new(w, h), rot, Color.White with { A = beamAlpha });
		}
		private void drawEndQuad(ref FrameState fs, float x) {
			x -= (float)InputSettings.VisualOffset;
			var tex = end;
			if (tex == null) return;

			var xpos = (float)XPosFromTimeOffset(x);
			var ypos = pathY(HitTime + Length);
			var rot = (float)((GetConductor().Time * RotationDegsPerSecond) % 360) * -1;

			var w = tex.GetWidth() * MuseDash1Gamemode.GlobalScale;
			var h = tex.GetHeight() * MuseDash1Gamemode.GlobalScale;

			if (tex.HasPublicFlags(PublicTextureFlags.RequiresFlippedV))
				Raylib.DrawTexturePro(tex.ToRaylibTexture(), new(0, 0, tex.GetWidth(), -tex.GetHeight()), new(xpos, ypos, w * 2, h * 2), new(w, h), rot, Color.White with { A = beamAlpha });
			else
				Raylib.DrawTexturePro(tex.ToRaylibTexture(), new(0, 0, tex.GetWidth(), tex.GetHeight()), new(xpos, ypos, w * 2, h * 2), new(w, h), rot, Color.White with { A = beamAlpha });
		}

		private SecondOrderSystem sosFail = new(2, 1, 1, 0);
		private byte beamAlpha;
		private void drawScrollStrip(double[] times, float[] values, float[] distances, ITexture tex, float xOffset, float yOffset) {
			double from = curveStartTime();

			int first = 1;
			while (first < times.Length && times[first] <= from)
				first++;

			if (first >= times.Length)
				return;

			int count = times.Length - first + 1;
			double t0 = times[first - 1], t1 = times[first];
			float f = t1 > t0 ? (float)((from - t0) / (t1 - t0)) : 1;

			Span<double> stripTimes = stackalloc double[count];
			Span<float> stripValues = stackalloc float[count];
			Span<float> stripDistances = stackalloc float[count];

			stripTimes[0] = from;
			stripValues[0] = values[first - 1] + (values[first] - values[first - 1]) * f;
			stripDistances[0] = distances[first - 1] + (distances[first] - distances[first - 1]) * f;
			times.AsSpan(first).CopyTo(stripTimes[1..]);
			values.AsSpan(first).CopyTo(stripValues[1..]);
			distances.AsSpan(first).CopyTo(stripDistances[1..]);

			float voffset = -(float)InputSettings.VisualOffset;
			Span<Vector2F> points = stackalloc Vector2F[count];
			for (int i = 0; i < count; i++)
				points[i] = new((float)XPosFromTimeOffset((float)(stripTimes[i] - HitTime) + voffset) + (i == 0 ? xOffset : 0), -Components.Scenes.GetNormalizedPosition(stripValues[i]).Y + yOffset);

			Span<Vector2F> top = stackalloc Vector2F[count];
			Span<Vector2F> bottom = stackalloc Vector2F[count];
			Span<float> u = stackalloc float[count];

			var height = tex.GetHeight() * MuseDash1Gamemode.GlobalScale;
			var texLength = tex.GetWidth() * MuseDash1Gamemode.GlobalScale * 2;
			for (int i = 0; i < count; i++) {
				u[i] = stripDistances[i] / texLength;

				var tangent = points[Math.Min(i + 1, count - 1)] - points[Math.Max(i - 1, 0)];
				var normal = tangent.Length > 0 ? new Vector2F(-tangent.Y, tangent.X) * (height / tangent.Length) : new Vector2F(0, height);
				top[i] = points[i] - normal;
				bottom[i] = points[i] + normal;
			}

			Graphics2D.SetTexture(tex);
			Graphics2D.SetDrawColor(255, 255, 255, beamAlpha);
			Graphics2D.DrawTexturedStrip(top, bottom, u);
		}

		public void drawScrollQuad(ITexture? tex, ref FrameState fs, float xOffset, float yOffset) {
			if (tex == null)
				return;

			if (curveTimes != null && curveValues != null && curveDistances != null) {
				drawScrollStrip(curveTimes, curveValues, curveDistances, tex, xOffset, yOffset);
				return;
			}

			float voffset = -(float)InputSettings.VisualOffset;
			var xStart = (float)XPosFromTimeOffset(voffset);
			var xMid = HeldState ? Components.Playfield.GetPathway(this).Position.X : xStart;
			var xEnd = (float)XPosFromTimeOffset((float)Length + voffset);
			var ypos = -Components.Playfield.GetPathway(this).Position.Y + yOffset;
			var height = tex.GetHeight() * MuseDash1Gamemode.GlobalScale;

			Rlgl.Begin(DrawMode.TRIANGLES);
			Rlgl.DisableBackfaceCulling();

			Rlgl.Color4ub(255, 255, 255, beamAlpha);

			var maxLength = (xEnd - xStart) / (tex.GetWidth() * MuseDash1Gamemode.GlobalScale * 2);
			var length = maxLength - ((xEnd - xMid) / (tex.GetWidth() * MuseDash1Gamemode.GlobalScale * 2));

			xMid = xMid + xOffset;
			Rlgl.SetTexture(tex.GetTextureHandle());
			if (tex.HasPublicFlags(PublicTextureFlags.RequiresFlippedV)) {
				Rlgl.TexCoord2f(length, 1); Rlgl.Vertex2f(xMid, ypos + -height);
				Rlgl.TexCoord2f(length, 0); Rlgl.Vertex2f(xMid, ypos + height);
				Rlgl.TexCoord2f(maxLength, 0); Rlgl.Vertex2f(xEnd, ypos + height);

				Rlgl.TexCoord2f(maxLength, 0); Rlgl.Vertex2f(xEnd, ypos + height);
				Rlgl.TexCoord2f(maxLength, 1); Rlgl.Vertex2f(xEnd, ypos + -height);
				Rlgl.TexCoord2f(length, 1); Rlgl.Vertex2f(xMid, ypos + -height);
			}
			else {
				Rlgl.TexCoord2f(length, 0); Rlgl.Vertex2f(xMid, ypos + -height);
				Rlgl.TexCoord2f(length, 1); Rlgl.Vertex2f(xMid, ypos + height);
				Rlgl.TexCoord2f(maxLength, 1); Rlgl.Vertex2f(xEnd, ypos + height);

				Rlgl.TexCoord2f(maxLength, 1); Rlgl.Vertex2f(xEnd, ypos + height);
				Rlgl.TexCoord2f(maxLength, 0); Rlgl.Vertex2f(xEnd, ypos + -height);
				Rlgl.TexCoord2f(length, 0); Rlgl.Vertex2f(xMid, ypos + -height);
			}
			Rlgl.End();
			Rlgl.DrawRenderBatchActive();
		}

		public override void Render(FrameState frameState) {
			if (!ShouldDraw) return;

			bool isSingleHit = Length == 0;
			beamAlpha = Convert.ToByte(NMath.Remap(sosFail.Update(DidPunishPlayer ? 1 : 0), 0, 1, 255, 127, true));

			if(!isSingleHit)
			drawScrollQuad(body, ref frameState, 0, 0);

			if (!isSingleHit) {
			var time = GetConductor().Time * 5;
			var sv = (float)(Math.Sin(time) * 10) * MuseDash1Gamemode.GlobalScale;
			var cv = (float)(Math.Cos(time) * 10) * MuseDash1Gamemode.GlobalScale;

				drawScrollQuad(up, ref frameState, cv / 2, sv);
				drawScrollQuad(down, ref frameState, sv / 2, cv);
			}

			drawStartQuad(ref frameState, 0);
			if (!isSingleHit)
				drawEndQuad(ref frameState, (float)Length);
		}

		private ITexture? start;
		private ITexture? end;
		private ITexture? body;
		private ITexture? up;
		private ITexture? down;

		public override void OnBuildVisuals(DashEnemyVisuals visuals) {
			base.OnBuildVisuals(visuals);
			visuals.Scene.GetSustainResources(pathways.PrimaryPathway(Pathway), out start, out end, out body, out up, out down, out RotationDegsPerSecond);
			XPosSetup(visuals);
			buildCurveSamples(visuals.Scene);
		}

		internal void Complete(double distanceToEnd) {
			HeldState = false;
			StopAcceptingInput = true;
			ShouldDraw = false;
			GetStats().HitEnd(this, distanceToEnd);
			RewardPlayer();

			Components.Combo.Add();
			Components.Rules.AddFever(FeverGiven, this);
			sustains.CompleteSustainBeam(this);
			hitsounds.Play(SceneSound.StartedHold, 1);
		}
		internal void Fail() {
			HeldState = false;
			StopAcceptingInput = true;
			ShouldDraw = false;
			PunishPlayer();

			sustains.FailSustainBeam(this);

			GetStats().MissEnd(this);
		}
		internal void Hold() {
			var now = GetConductor().Time;
			var delta = now - lastCheckTime;
			if (delta >= 0.1) { // Give 10 score for every 100ms held (should this be done differently?)
				lastCheckTime = now;
				Components.Rules.AddScore(10, this);
			}
		}
	}
}