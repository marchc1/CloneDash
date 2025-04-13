using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus
{
	public static class Extensions
	{
		public static unsafe Image ToImage(this byte[] data, int width, int height, PixelFormat format, int mipmaps) {
			var ptr = Raylib.New<byte>(data.Length);
			for (int i = 0; i < data.Length; i++) {
				ptr[i] = data[i];
			}
			var img = new Image() {
				Data = ptr,
				Format = format,
				Width = width,
				Height = height,
				Mipmaps = mipmaps
			};
			return img;
		}
	}
}
