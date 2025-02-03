using Newtonsoft.Json;
using Nucleus.Engine;
using Raylib_cs;
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
		[JsonIgnore] public EditorBone Bone { get; set; }
		public string Name { get; set; }
		public List<EditorAttachment> Attachments { get; } = [];

		public Color Color { get; set; } = Color.WHITE;
		public bool TintBlack { get; set; } = false;
		public Color DarkColor { get; set; } = Color.BLACK;
		public BlendMode Blending { get; set; } = BlendMode.Normal;

		public EditorAttachment? FindAttachment(string name) {
			return Attachments.FirstOrDefault(x => x.Name == name);
		}
		public bool TryFindAttachment(string name, [NotNullWhen(true)] out EditorAttachment? attachment) {
			attachment = FindAttachment(name);
			return attachment != null;
		}
	}
}
