using Nucleus.Types;
using Nucleus.Util;

namespace Nucleus.Input
{
	public struct DragNDropItem
	{
		public string Text;
		public Vector2F Position;
		public static implicit operator string(DragNDropItem self) => self.Text;
		public static implicit operator Vector2F(DragNDropItem self) => self.Position;
	}
	public struct DragNDropState
	{
		public bool Dragged;
		public bool Active;
		public bool Dropped;
		public Vector2F Position;
		public InlineArray32<DragNDropItem> Text;
		public InlineArray32<DragNDropItem> File;
		public int Texts;
		public int Files;
	}
}
