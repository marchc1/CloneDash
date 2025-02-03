using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor
{
	public class EditorRegionAttachment : EditorAttachment
	{
		public string Path { get; set; }
		public Color Color { get; set; }
		public Vector2F[] Position { get; set; } = new Vector2F[4];
		public Vector2F[] TexCoords { get; set; } = new Vector2F[4];
		public Vector2F Size { get; set; }
		public Vector2F Scale { get; set; }
		public Vector2F[] VertexOffset { get; set; } = new Vector2F[4];
		public Vector2F RegionSize { get; set; }
		public Vector2F RegionOffset { get; set; }
		public Vector2F RegionOriginalSize { get; set; }
		public float Rotation { get; set; }
	}
}
