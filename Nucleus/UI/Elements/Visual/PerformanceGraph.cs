using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.Util;
using System.Diagnostics;

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
		private MemoryBackedString _lbl1 = new(64), _lbl2 = new(64);
		private MemoryBackedString _minLabel = new(64), _maxLabel = new(64);
		
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

			_lbl1.Clear();
			_lbl2.Clear();

			switch (Mode) {
				case GraphMode.CpuUpdateTime:
					_lbl1.ConcatRighthand("UPS: "); _lbl1.ConcatRighthand(1000f / statistic, "0.##");
					_lbl2.ConcatRighthand("PERF: "); _lbl2.ConcatRighthand(statistic, "0.##"); _lbl2.ConcatRighthand(" ms");
					break;
				case GraphMode.CpuRenderTime:
					_lbl1.ConcatRighthand("FPS: "); _lbl1.ConcatRighthand(1000f / statistic, "0.##");
					_lbl2.ConcatRighthand("PERF: "); _lbl2.ConcatRighthand(statistic, "0.##"); _lbl2.ConcatRighthand(" ms");
					break;
				case GraphMode.RamUsage:
					_lbl1.ConcatRighthand("GC: "); _lbl1.ConcatRighthand(statistic, "0.#"); _lbl1.ConcatRighthand("MB");
					break;
			}

			_minLabel.SetText(_millisecondsOverTime.Min(), "0.##");
			_maxLabel.SetText(_millisecondsOverTime.Max(), "0.##");
		}

		private void DrawGraph(float start, float end, float height, ConstantLengthNumericalQueue<float> items, int maxItems, Color startColor, Color endColor, float? divider = null, float offset = 32) {
			int count = items.Length;
			if (count <= 0) return;

			end = start + end;
			float min = items.Min();
			float max = items.Max();
			Graphics2D.DrawText(new Vector2F(start + offset - 2, height), _minLabel.ToSpan(), "Consolas", 9, Anchor.BottomRight);
			Graphics2D.DrawText(new Vector2F(start + offset - 2, 2), _maxLabel.ToSpan(), "Consolas", 9, Anchor.TopRight);

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

			if (_lbl2.Length == 0)
				Graphics2D.DrawText(new Vector2F(2, (height / 2) - 4), _lbl1.ToSpan(), "Consolas", 12);
			else {
				Graphics2D.DrawText(new Vector2F(3, 3), _lbl1.ToSpan(), "Consolas", 10);
				Graphics2D.DrawText(new Vector2F(3, height + 1), _lbl2.ToSpan(), "Consolas", 10, Anchor.BottomLeft);
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