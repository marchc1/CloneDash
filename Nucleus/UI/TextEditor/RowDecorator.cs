using Raylib_cs;

namespace Nucleus.UI
{
	public struct RowDecorator
	{
		public Color Color { get; set; }
		public string Text { get; set; }
		public RowDecorator() {
			Color = Color.WHITE;
		}

		public override string ToString() {
			return $"{Text}";
		}
	}
}
