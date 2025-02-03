using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public abstract class EditorAttachment
	{
		public string Name { get; set; }
		public EditorSlot Slot { get; set; }
	}
}
