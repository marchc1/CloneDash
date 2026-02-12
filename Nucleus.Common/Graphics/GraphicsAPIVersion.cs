namespace Nucleus.Common.Graphics;

public enum GraphicsAPIVersion : ulong{
	// Last 16 bits reserved for API type
	OpenGL = 1 << 48,

	OpenGL_3_30 = OpenGL | 330,
	OpenGL_4_30 = OpenGL | 430,
	// OpenGL v4.60 would be nice one day!
}