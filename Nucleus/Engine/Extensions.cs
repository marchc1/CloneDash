using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

		/// <summary>
		/// A convenience method to iterate over an <see cref="IList{T}"/> and return the first item that matches <see cref="Predicate{T}"/>'s index. Returns -1 when not found.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="cond"></param>
		/// <returns>The index of the found item, or -1 if not found in the <see cref="IList{T}"/></returns>
		public static int FirstOrDefaultIndex<T>(this IList<T> list, Predicate<T> cond) {
			for (int i = 0, len = list.Count; i < len; i++)
				if (cond(list[i])) 
					return i;

			return -1;
		}

		public static bool InRange(this int n, int min, int max) => n >= min && n <= max;
		public static bool InRange(this float n, float min, float max) => n >= min && n <= max;
		public static bool InRange(this double n, double min, double max) => n >= min && n <= max;
		public static bool InRange(this uint n, uint min, uint max) => n >= min && n <= max;
		public static bool InRange(this long n, long min, long max) => n >= min && n <= max;
	}
}
