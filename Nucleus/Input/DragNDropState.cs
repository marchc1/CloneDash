using Nucleus.Types;
using Nucleus.Util;

namespace Nucleus.Input
{
	public struct DragNDropState{
		public bool Dragged;
		public bool Active;
		public bool Dropped;
		public Vector2F Position;
		public InlineArray32<string?> Text;
		public InlineArray32<string?> File;
		public int Texts;
		public int Files;
	}
}
