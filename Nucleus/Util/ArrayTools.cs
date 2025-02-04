using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Util
{
	public static unsafe partial class Util
	{
		public static int IndexOf<T>(this IList<T> items, Predicate<T> search) {
			for (int i = 0; i < items.Count; i++) {
				if (search(items[i]))
					return i;
			}

			return -1;
		}
	}
}
