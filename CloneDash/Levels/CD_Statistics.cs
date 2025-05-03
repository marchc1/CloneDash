using CloneDash.Data;
using CloneDash.Game.Statistics;
using CloneDash.Modding.Descriptors;
using CloneDash.Modding.Settings;
using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Input;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;

namespace CloneDash.Levels
{
	public class CD_Statistics : Level
	{
		ChartSheet sheet;
		CD_StatisticsData stats;
		CharacterDescriptor character;
		ModelInstance model;
		AnimationHandler anims;
		public override void Initialize(params object[] args) {
#nullable disable
			sheet = args[0] as ChartSheet;
			stats = args[1] as CD_StatisticsData;
#nullable enable

			if (sheet == null) throw new NullReferenceException(nameof(sheet));
			if (stats == null) throw new NullReferenceException(nameof(stats));

			CharacterDescriptor character = CharacterMod.GetCharacterData();
			if (character == null) return;
			if (character.Filename == null) return;
			this.character = character;

			model = Models.CreateInstanceFromFile("character", character.GetVictoryModel());
			anims = new(model.Data);
			anims.SetAnimation(0, character.Victory.Standby, true);

			stats.Compute();

			var tempPanel = UI.Add<Panel>();
			tempPanel.Dock = Dock.Fill;
			tempPanel.PaintOverride += TempPanel_PaintOverride;

			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.R], () => {
				EngineCore.LoadLevel(new CD_Statistics(), sheet, stats);
			});

			var bottom = UI.Add<Panel>();
			bottom.DrawPanelBackground = false;

			bottom.DynamicallySized = true;
			bottom.Size = new(0.07f);
			bottom.Dock = Dock.Bottom;

			var restart = bottom.Add<Button>();
			restart.DynamicallySized = true;
			restart.Size = new(.2f);
			restart.Text = "Restart";
			restart.Dock = Dock.Left;

			var back = bottom.Add<Button>();
			back.DynamicallySized = true;
			back.Size = new(.2f);
			back.Text = "Main Menu";
			back.Dock = Dock.Right;
		}

		private void TempPanel_PaintOverride(Element self, float width, float height) {
			stats.Compute();
			var y = 0;
			string[] lines = [
				$"[{sheet.Rating}] -  {sheet.Song.Name}",
				$"      Grade: {stats.Grade}",
				$"      Accuracy: {stats.Accuracy}",
				$"      Score: {stats.Score}",
				$"      Max Combo: {stats.MaxCombo}",
				"",
				$"      Perfects: {stats.Perfects}",
				$"      Greats: {stats.Greats}",
				$"      Passes: {stats.Passes}",
				$"      Misses: {stats.Misses}",
				"",
				$"      Earlys: {stats.Earlys}",
				$"      Exacts: {stats.Exacts}",
				$"      Lates: {stats.Lates}",
				"",
				$"      Registered: {stats.OrderedEnemies.Count}",
			];
			Graphics2D.SetDrawColor(255, 255, 255);
			var fs = 24;
			foreach (var line in lines) {
				Graphics2D.DrawText(16, 16 + y, line, "Noto Sans", fs);
				y += fs + 4;
			}
		}

		public override void Think(FrameState frameState) {
			base.Think(frameState);
			if (model != null) {
				model.Position = new(frameState.WindowWidth / 2, (1 - (float)NMath.Ease.OutElastic(Math.Clamp(Curtime * 0.2, 0, 1))) * (frameState.WindowHeight));

				anims?.AddDeltaTime(CurtimeDelta);
				anims?.Apply(model);
			}
		}

		public override void Render2D(FrameState frameState) {
			base.Render2D(frameState);
			EngineCore.Window.BeginMode2D(new() {
				Zoom = frameState.WindowHeight / 900 / 2.4f,
				Offset = new(frameState.WindowWidth / 2 - frameState.WindowWidth * .2f, frameState.WindowHeight / 1)
			});

			model?.Render();

			EngineCore.Window.EndMode2D();
		}
	}
}
