using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI.Elements
{
	public enum PerfGraphMode
	{
		CPU_Frametime,
		RAM_Usage
	}
	public class PerfGraph : Panel
	{
		public const int MAX_PERFGRAPH_ITEMS = (1000 / 100) * 4;
		public PerfGraphMode Mode;
		ConstantLengthNumericalQueue<float> MillisecondsOverTime = new(MAX_PERFGRAPH_ITEMS);
		ConstantLengthNumericalQueue<float> MemoryOverTime = new(MAX_PERFGRAPH_ITEMS);
		float MSMean = 0;
		int MSCount = 0;
		DateTime lastQuery;
		private void DrawGraph(float start, float end, float height, ConstantLengthNumericalQueue<float> items, int maxItems, Color startColor, Color endColor, float? divider = null, float offset = 32) {
			var count = items.Length;
			if (count <= 0) return;

			end = start + end;
			float min = items.Min();
			float max = items.Max();
			Graphics2D.DrawText(new(start + offset - 2, height - 0), $"{min:0.##}", "Consolas", 10, Types.Anchor.BottomRight);
			Graphics2D.DrawText(new(start + offset - 2, 4), $"{max:0.##}", "Consolas", 10, Types.Anchor.TopRight);

			for (int i = 0; i < maxItems; i += 1) {
				if (i + 1 >= count)
					break;

				var finalPos = i + 1;
				float x1 = (float)NMath.Remap(i, 0, maxItems, start + offset, end);
				float x2 = (float)NMath.Remap(finalPos, 0, maxItems, start + offset, end);
				float y1 = items[i];
				float y2 = items[finalPos];
				float c1 = items[i] / divider ?? max;
				float c2 = items[finalPos] / divider ?? max;
				var mult = 16;


				Graphics2D.DrawLine(
					new(x1, (float)NMath.Remap(y1, min, max, height - 4, 4)),
					NMath.LerpColor(Math.Clamp(c1, 0, 1), startColor, endColor, 255),
					new(x2, (float)NMath.Remap(y2, min, max, height - 4, 4)),
					NMath.LerpColor(Math.Clamp(c2, 0, 1), startColor, endColor, 255),
					2
				);
			}
		}
		private Process proc = System.Diagnostics.Process.GetCurrentProcess();
		public override void Paint(float width, float height) {
			base.Paint(width, height);

			//var mem = proc.WorkingSet64;
			Graphics2D.SetDrawColor(255, 255, 255);

			string lbl1 = "UNDEFINED 1", lbl2 = "UNDEFINED 2";
			switch (Mode) {
				case PerfGraphMode.CPU_Frametime:
					lbl1 = $"FPS: {EngineCore.FPS}";
					lbl2 = $"PERF: {EngineCore.FrameCostMS:0.##}";
					break;
				case PerfGraphMode.RAM_Usage:
					lbl1 = $"GC: {(System.GC.GetTotalMemory(false) / 1024f / 1024f):0.#} MB";
					lbl2 = "";
					break;
			}
			if (lbl2 == "") {
				Graphics2D.DrawText(new(4 + 6, (height / 2) - 4), lbl1, "Consolas", 11);
			}
			else {
				Graphics2D.DrawText(new(5, 4 + 2), lbl1, "Consolas", 11);
				Graphics2D.DrawText(new(5, 18 + 2), lbl2, "Consolas", 11);
			}

			DateTime now = DateTime.UtcNow;
			switch (Mode) {
				case PerfGraphMode.CPU_Frametime:
					MSMean += EngineCore.FrameCostMS;
					break;
				case PerfGraphMode.RAM_Usage:
					MSMean += (System.GC.GetTotalMemory(false) / 1024f / 1024f);
					break;
			}

			MSCount++;
			if ((now - lastQuery).TotalMilliseconds > 100) {
				MillisecondsOverTime.Add(MSMean / MSCount);

				lastQuery = now;
				MSCount = 0;
				MSMean = 0;
			}
			Color color1 = Color.White;
			Color color2 = Color.White;

			switch (Mode) {
				case PerfGraphMode.CPU_Frametime:
					color1 = new Color(30, 255, 90, 255);
					color2 = new Color(255, 70, 30, 255);
					break;
				case PerfGraphMode.RAM_Usage:
					color1 = new(55, 35, 210, 255);
					color2 = new(255, 54, 185, 255);
					break;
			}

			DrawGraph(90, width - 90, height, MillisecondsOverTime, MAX_PERFGRAPH_ITEMS, color1, color2, 1000f / 60f);
		}

		public override bool HoverTest(RectangleF bounds, Vector2F mousePos) {
			return false;
		}
	}
}
