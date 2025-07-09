using Raylib_cs;
using System.Text.RegularExpressions;

namespace Nucleus.UI
{
	public class ConsoleLogHighlighter : SyntaxHighlighter
	{
		public override string Name => "blank";
		public override Color Color => new Color(155, 155, 155, 255);
		public static Regex GetData = new(@"\[(.*?(?=]))\] \[(.*?(?=]))\] (.*)");

		public static readonly Color COLOR_TEXT = new Color(245, 245, 245, 255);
		public override void Rebuild(SafeArray<string> rows) {
			Rows.Clear();
			foreach (var row in rows) {
				var result = GetData.Match(row);

				Rows.Add([
					new RowDecorator() { Color = COLOR_TEXT, Text = $"[{result.Groups[1].Value}] " }, 
					new RowDecorator() { Color = Logs.LevelToColor(Logs.ConsoleStringToLevel(result.Groups[2].Value)), Text = $"[{result.Groups[2].Value}] " },
					new RowDecorator() { Color = COLOR_TEXT, Text = result.Groups[3].Value }
				]);
			}
		}
	}
}
