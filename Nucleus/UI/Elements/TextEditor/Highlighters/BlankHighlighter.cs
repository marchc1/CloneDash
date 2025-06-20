using Raylib_cs;

namespace Nucleus.UI
{
	public class BlankHighlighter : SyntaxHighlighter
	{
		public override string Name => "blank";
		public override Color Color => new Color(155, 155, 155, 255);
		public override void Rebuild(SafeArray<string> rows) {
			Rows.Clear();
			foreach (var row in rows) {
				Rows.Add([new RowDecorator() { Color = Color.White, Text = row ?? "" }]);
			}
		}
	}
}
