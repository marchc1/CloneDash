using Nucleus.Common.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Rendering;

public class RaylibGraphicsHardwareConfig : IGraphicsHardwareConfig
{
	public bool SupportsBPTC { get; private set; }
	public void ConfirmCapabilities() {
		for (uint i = 0, c = (uint)OpenGL.GetInteger(GLEnum.NUM_EXTENSIONS); i < c; i++) {
			string ext = OpenGL.GetStringi(OpenGL.EXTENSIONS, i);
			switch (ext) {
				case "GL_ARB_texture_compression_bptc":
				case "GL_EXT_texture_compression_bptc":
					SupportsBPTC = true;
					break;
			}
		}
	}
}
