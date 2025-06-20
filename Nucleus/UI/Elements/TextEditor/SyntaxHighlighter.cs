using Raylib_cs;

namespace Nucleus.UI
{
	public abstract class SyntaxHighlighter
	{
		public static SyntaxHighlighter Blank => new BlankHighlighter();
		public virtual string Name { get; } = "unknown";
		public virtual Color Color { get; } = new Color(155, 155, 155, 255);
		public SafeArray<List<RowDecorator>> Rows { get; } = [];

		public abstract void Rebuild(SafeArray<string> rows);
	}
}
