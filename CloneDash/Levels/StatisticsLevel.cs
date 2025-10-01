using CloneDash.Characters;
using CloneDash.Data;
using CloneDash.Game;
using CloneDash.Game.Statistics;

using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Input;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Nucleus.UI;

using System.Text.RegularExpressions;

namespace CloneDash.Levels
{
	public class StatisticsLevel : Level
	{
		ChartSheet sheet;
		StatisticsData stats;
		ICharacterDescriptor character;
		ModelInstance model;
		AnimationHandler anims;
		public override void Initialize(params object[] args) {
#nullable disable
			sheet = args[0] as ChartSheet;
			stats = args[1] as StatisticsData;
#nullable enable

			if (sheet == null) throw new NullReferenceException(nameof(sheet));
			if (stats == null) throw new NullReferenceException(nameof(stats));

			ICharacterDescriptor? character = CharacterMod.GetCharacterData();
			if (character == null) return;
			this.character = character;

			model = character.GetVictoryModel(this).Instantiate();
			anims = new(model.Data);
			anims.SetAnimation(0, character.GetVictoryStandby(), true);

			stats.Compute();

			var tempPanel = UI.Add<Panel>();
			tempPanel.Dock = Dock.Fill;
			tempPanel.PaintOverride += TempPanel_PaintOverride;

			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.R], () => {
				EngineCore.LoadLevel(new StatisticsLevel(), sheet, stats);
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
			back.MouseReleaseEvent += (_, _, _) => EngineCore.LoadLevel(new MainMenuLevel());
		}

		private void TempPanel_PaintOverride(Element self, float width, float height) {
			stats.Compute();
			var y = 0;
			string[] lines = [
				$"      Rating: {sheet.Rating}",
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
			// Strawberry Godzilla from Muse Dash
			Regex boldRegex = new("^<b>(.+)<\\/b>$");
			Match boldRegexMatch = boldRegex.Match(sheet.Song.Name);
			Graphics2D.DrawText(16, 16 + y,
								boldRegexMatch.Success ? boldRegexMatch.Groups[1].Value : sheet.Song.Name,
								boldRegexMatch.Success ? Graphics2D.NotoSansMonoBoldFontName : Graphics2D.NotoSansCJRegionFontName,
								fs);
			y += fs + 4;
			foreach (var line in lines) {
				Graphics2D.DrawText(16, 16 + y, line, Graphics2D.UiFontName, fs);
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
