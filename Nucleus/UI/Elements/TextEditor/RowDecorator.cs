using Raylib_cs;

namespace Nucleus.UI
{
	public struct RowDecorator
	{
		public Color Color;
		public string Text;
		public RowDecorator() {
			Color = Color.White;
		}

		public override string ToString() {
			return $"{Text}";
		}
	}
}