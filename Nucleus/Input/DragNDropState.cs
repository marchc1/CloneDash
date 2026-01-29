using Nucleus.Types;
using Nucleus.Util;

namespace Nucleus.Input
{
	public struct DragNDropItem
	{
		public string Text;
		public static implicit operator string(DragNDropItem self) => self.Text;
	}
	public struct DragNDropState
	{
		public InlineArray32<DragNDropItem> Text;
		public InlineArray32<DragNDropItem> File;
		public int Texts;
		public int Files;
	}
}
