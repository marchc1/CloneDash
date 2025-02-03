using Nucleus.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public class EditorSlot
	{
		public EditorBone Bone { get; set; }
		public string Name { get; set; }
		public List<EditorAttachment> Attachments { get; } = [];

		public EditorAttachment? FindAttachment(string name) {
			return Attachments.FirstOrDefault(x => x.Name == name);
		}
		public bool TryFindAttachment(string name, [NotNullWhen(true)] out EditorAttachment? attachment) {
			attachment = FindAttachment(name);
			return attachment != null;
		}

		private bool __isvalid = true;
	}
}
