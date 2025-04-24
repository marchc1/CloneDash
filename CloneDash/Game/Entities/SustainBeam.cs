
using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game.Entities
{
	public class SustainBeam : CD_BaseEnemy
	{
		public SustainBeam() : base(EntityType.SustainBeam) {
			Interactivity = EntityInteractivity.Hit;
			DoesDamagePlayer = true;
		}
		public override void Initialize() {
			base.Initialize();
		}

		public bool HeldState { get; private set; } = false;
		public bool StopAcceptingInput { get; private set; } = false;

		public Pathway PathwayCheck;

		public override void OnReset() {
			base.OnReset();
			HeldState = false;
			StopAcceptingInput = false;
		}

		protected override void OnHit(PathwaySide attackedPath) {
			if (HeldState == true)
				return;
			if (StopAcceptingInput == true)
				return;

			PathwayCheck = Level.As<CD_GameLevel>().GetPathway(attackedPath);
			HeldState = true;
			ForceDraw = true;
			Level.As<CD_GameLevel>().SetSustain(Pathway, this);
			Level.As<CD_GameLevel>().AddCombo();
			Level.As<CD_GameLevel>().AddFever(FeverGiven);
		}

		protected override void OnMiss() {
			if (HeldState == false) {
				Level.As<CD_GameLevel>().SetSustain(Pathway, null);
				PunishPlayer();
			}
		}

		public override bool VisTest(float gamewidth, float gameheight, float xPosition) {
			return base.VisTest(gamewidth, gameheight, xPosition);
		}

		public override void Think(FrameState frameState) {
			if (HeldState) {
				var endPos = DistanceToEnd;

				// check if sustain complete

				var sustainComplete = PathwayCheck.IsPressed && endPos <= 0;
				var sustainEarlyButStillSuccess = !PathwayCheck.IsPressed && NMath.InRange(endPos, -0.05f, 0.05f);

				if (sustainComplete || sustainEarlyButStillSuccess) {
					HeldState = false;
					StopAcceptingInput = true;
					ShouldDraw = false;
					RewardPlayer();
					Level.As<CD_GameLevel>().AddCombo();
					Level.As<CD_GameLevel>().AddFever(FeverGiven);
					Level.As<CD_GameLevel>().SetSustain(Pathway, null);
					Level.As<CD_GameLevel>().Scene.PlayPunch();
				}
				// check if pathway being held
				else if (!PathwayCheck.IsPressed) {
					HeldState = false;
					StopAcceptingInput = true;
					ShouldDraw = false;
					Level.As<CD_GameLevel>().SetSustain(Pathway, null);
					PunishPlayer();
				}
			}
		}

		public float StartPosition { get; private set; }
		public float RotationDegsPerSecond { get; set; } = 120;
		private void drawStartQuad(CD_GameLevel game, ref FrameState fs, float x) {
			var tex = start;
			var xpos = (HeldState ? game.GetPathway(Pathway).Position.X : (float)XPosFromTimeOffset(ref fs, x));
			var ypos = game.GetPathway(Pathway).Position.Y;
			var rot = (float)((game.Conductor.Time * RotationDegsPerSecond) % 360) * -1;
			Raylib.DrawTexturePro(tex, new(0, 0, tex.Width, tex.Height), new(xpos, ypos, tex.Width * 2, tex.Height * 2), new(tex.Width, tex.Height), rot, Color.White);
		}
		private void drawEndQuad(CD_GameLevel game, ref FrameState fs, float x) {
			var tex = end;
			var xpos = (float)XPosFromTimeOffset(ref fs, x);
			var ypos = game.GetPathway(Pathway).Position.Y;
			var rot = (float)((game.Conductor.Time * RotationDegsPerSecond) % 360) * -1;
			Raylib.DrawTexturePro(tex, new(0, 0, tex.Width, tex.Height), new(xpos, ypos, tex.Width * 2, tex.Height * 2), new(tex.Width, tex.Height), rot, Color.White);
		}

		public void drawScrollQuad(CD_GameLevel game, Texture tex, ref FrameState fs, float xOffset, float yOffset) {
			var xStart = (float)XPosFromTimeOffset(ref fs, 0);
			var xMid = HeldState ? game.GetPathway(Pathway).Position.X : xStart;
			var xEnd = (float)XPosFromTimeOffset(ref fs, (float)Length);
			var ypos = game.GetPathway(Pathway).Position.Y + yOffset;
			var height = tex.Height;

			Rlgl.Begin(DrawMode.TRIANGLES);
			Rlgl.DisableBackfaceCulling();
			Rlgl.Color4ub(255, 255, 255, 255);

			var maxLength = (xEnd - xStart) / (tex.Width * 2);
			var length = maxLength - ((xEnd - xMid) / (tex.Width * 2));

			xMid = xMid + xOffset;
			Rlgl.SetTexture(tex.HardwareID);
			{
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
			var game = Level.As<CD_GameLevel>();

			drawScrollQuad(game, body, ref frameState, 0, 0);

			var time = game.Conductor.Time * 5;
			var sv = (float)(Math.Sin(time) * 10);
			var cv = (float)(Math.Cos(time) * 10);

			drawScrollQuad(game, up, ref frameState, cv / 2, sv);
			drawScrollQuad(game, down, ref frameState, sv / 2, cv);

			drawStartQuad(game, ref frameState, 0);
			drawEndQuad(game, ref frameState, (float)Length);
		}

		private Texture start;
		private Texture end;
		private Texture body;
		private Texture up;
		private Texture down;

		public override void Build() {
			var level = Level.As<CD_GameLevel>();
			var scene = level.Scene;
			var sustains = scene.Sustains;

			RotationDegsPerSecond = sustains.RotationDegsPerSecond;
			start = sustains.GetStartTexture(Pathway);
			end = sustains.GetEndTexture(Pathway);
			body = sustains.GetBodyTexture(Pathway);
			up = sustains.GetUpTexture(Pathway);
			down = sustains.GetDownTexture(Pathway);
		}
	}
}