using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Util
{
	public static unsafe partial class Util
	{
		public static T* CopyManagedArrayToUnmanagedPointer<T>(ICollection<T> source) where T : unmanaged {
			T* unmanaged = Raylib.New<T>(source.Count);
			int i = 0;
			foreach (T item in source) {
				unmanaged[i] = item;
				i++;
			}
			return unmanaged;
		}
		public static T* CopyManagedArrayToUnmanagedPointer<T>(IList<T> source) where T : unmanaged {
			T* unmanaged = Raylib.New<T>(source.Count);
			for (int i = 0; i < source.Count; i++) {
				unmanaged[i] = source[i];
			}
			return unmanaged;
		}

		public static T* ToUnmanagedPointer<T>(this ICollection<T> source) where T : unmanaged => CopyManagedArrayToUnmanagedPointer(source);
		public static T* ToUnmanagedPointer<T>(this IList<T> source) where T : unmanaged => CopyManagedArrayToUnmanagedPointer(source);
	}
}
