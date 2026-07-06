using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.Util;

namespace Nucleus.UI.Elements.Visual
{
	public class PerformanceGraph(Element? parent) : Panel(parent)
	{
		public const int MaxItems = 1000 / 100 * 4;

		public GraphMode Mode;
		private readonly ConstantLengthNumericalQueue<float> _millisecondsOverTime = new(MaxItems);

		private double _msMean;
		private long _msCount;
		private DateTime _lastQuery;

		private static ThrottledUpdater _labelThrottle = new(250);
		private string _lbl1 = "", _lbl2 = "";
		private string _minLabel = "", _maxLabel = "";

		private void Update() {
			double statistic = Mode switch {
				GraphMode.CpuUpdateTime => EngineCore.GetTimeToUpdate().TotalMilliseconds,
				GraphMode.CpuRenderTime => EngineCore.GetTimeToRender().TotalMilliseconds,
				GraphMode.RamUsage => GC.GetTotalMemory(false) / 1024d / 1024d,
				_ => throw new NotImplementedException()
			};

			DateTime now = DateTime.UtcNow;
			_msMean += statistic;
			_msCount++;
			if ((now - _lastQuery).TotalMilliseconds > 100) {
				_millisecondsOverTime.Add((float)(_msMean / _msCount));
				_lastQuery = now;
				_msCount = 0;
				_msMean = 0;
			}

			if (!_labelThrottle.TryUpdate(EngineCore.CurrentAppTime)) return;

			switch (Mode) {
				case GraphMode.CpuUpdateTime:
					_lbl1 = $"UPS: {1000f / statistic:0.##}";
					_lbl2 = $"PERF: {statistic:0.##} ms";
					break;
				case GraphMode.CpuRenderTime:
					_lbl1 = $"FPS: {1000f / statistic:0.##}";
					_lbl2 = $"PERF: {statistic:0.##} ms";
					break;
				case GraphMode.RamUsage:
					_lbl1 = $"GC: {statistic:0.#} MB";
					_lbl2 = "";
					break;
			}

			_minLabel = $"{_millisecondsOverTime.Min():0.##}";
			_maxLabel = $"{_millisecondsOverTime.Max():0.##}";
		}

		private void DrawGraph(float start, float end, float height, ConstantLengthNumericalQueue<float> items, int maxItems, Color startColor, Color endColor, float? divider = null, float offset = 32) {
			int count = items.Length;
			if (count <= 0) return;

			end = start + end;
			float min = items.Min();
			float max = items.Max();
			Graphics2D.DrawText(new Vector2F(start + offset - 2, height), _minLabel, "Consolas", 9, Anchor.BottomRight);
			Graphics2D.DrawText(new Vector2F(start + offset - 2, 2), _maxLabel, "Consolas", 9, Anchor.TopRight);

			for (int i = 0; i < maxItems; i += 1) {
				if (i + 1 >= count)
					break;

				int finalPos = i + 1;
				float x1 = NMath.Remap(i, 0, maxItems, start + offset, end);
				float x2 = NMath.Remap(finalPos, 0, maxItems, start + offset, end);
				float y1 = items[i];
				float y2 = items[finalPos];
				float c1 = items[i] / divider ?? max;
				float c2 = items[finalPos] / divider ?? max;


				Graphics2D.DrawLine(
					new Vector2F(x1, NMath.Remap(y1, min, max, height - 4, 4)),
					NMath.LerpColor(Math.Clamp(c1, 0, 1), startColor, endColor, 255),
					new Vector2F(x2, NMath.Remap(y2, min, max, height - 4, 4)),
					NMath.LerpColor(Math.Clamp(c2, 0, 1), startColor, endColor, 255),
					2
				);
			}
		}

		public override void Paint(float width, float height) {
			base.Paint(width, height);
			
			Update();

			Graphics2D.SetDrawColor(255, 255, 255);

			if (_lbl2 == "")
				Graphics2D.DrawText(new Vector2F(2, (height / 2) - 4), _lbl1, "Consolas", 12);
			else {
				Graphics2D.DrawText(new Vector2F(3, 3), _lbl1, "Consolas", 10);
				Graphics2D.DrawText(new Vector2F(3, height + 1), _lbl2, "Consolas", 10, Anchor.BottomLeft);
			}

			Color color1 = Color.White;
			Color color2 = Color.White;

			switch (Mode) {
				case GraphMode.CpuUpdateTime:
					color1 = new Color(30, 255, 90, 255);
					color2 = new Color(255, 70, 30, 255);
					break;
				case GraphMode.CpuRenderTime:
					color1 = new Color(30, 255, 90, 255);
					color2 = new Color(255, 70, 30, 255);
					break;
				case GraphMode.RamUsage:
					color1 = new Color(55, 35, 210, 255);
					color2 = new Color(255, 54, 185, 255);
					break;
			}

			DrawGraph(90, width - 84, height, _millisecondsOverTime, MaxItems, color1, color2, 1000f / 60f);
		}

		public override bool HoverTest(RectangleF bounds, Vector2F mousePos) {
			return false;
		}

		public enum GraphMode
		{
			CpuUpdateTime,
			CpuRenderTime,
			RamUsage
		}
	}
}