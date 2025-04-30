using CloneDash.Data;
using CloneDash.Game;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.UI;

namespace CloneDash.Levels
{
    public class CD_Statistics : Level
    {
		ChartSheet sheet;
		StatisticsData stats;
        public override void Initialize(params object[] args) {
#nullable disable
			sheet = args[0] as ChartSheet;
			stats = args[1] as StatisticsData;
#nullable enable

			if (sheet == null) throw new NullReferenceException(nameof(sheet));
			if (stats == null) throw new NullReferenceException(nameof(stats));

			stats.Compute();

			var tempPanel = UI.Add<Panel>();
			tempPanel.Dock = Dock.Fill;
			tempPanel.PaintOverride += TempPanel_PaintOverride;
        }

		private void TempPanel_PaintOverride(Element self, float width, float height) {
			stats.Compute();
			var y = 0;
			string[] lines = [
				$"Statistics information:",
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
			foreach(var line in lines) {
				Graphics2D.DrawText(16, 16 + y, line, "Noto Sans", fs);
				y += fs + 4;
			}
		}
	}
}
