using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Util
{
	public static unsafe partial class Util
	{
		public static void MoveListItem<T>(this List<T> list, T item, int newIndex) {
			var oldIndex = list.IndexOf(item);
			if (oldIndex == -1) throw new Exception();

			// exit if positions are equal or outside array
			if ((oldIndex == newIndex) || (0 > oldIndex) || (oldIndex >= list.Count) || (0 > newIndex) ||
				(newIndex >= list.Count)) return;
			// local variables
			var i = 0;
			T tmp = list[oldIndex];
			// move element down and shift other elements up
			if (oldIndex < newIndex) {
				for (i = oldIndex; i < newIndex; i++) {
					list[i] = list[i + 1];
				}
			}
			// move element up and shift other elements down
			else {
				for (i = oldIndex; i > newIndex; i--) {
					list[i] = list[i - 1];
				}
			}
			// put element from position 1 to destination
			list[newIndex] = tmp;
		}
		public static int IndexOf<T>(this IList<T> items, Predicate<T> search) {
			for (int i = 0; i < items.Count; i++) {
				if (search(items[i]))
					return i;
			}

			return -1;
		}

		// thanks Ashton
		public static MemoryStream ToMemoryStream(this Stream stream) {
			var ms = new MemoryStream();
			try {
				stream.CopyTo(ms);
				ms.Position = 0;
			}
			catch (Exception ex) {
				Logs.Warn(ex.Message);
			}

			return ms;
		}

		public class AlphanumComparatorFast : IComparer
		{
			public int Compare(object x, object y) {
				string s1 = x as string;
				if (s1 == null) {
					return 0;
				}
				string s2 = y as string;
				if (s2 == null) {
					return 0;
				}

				int len1 = s1.Length;
				int len2 = s2.Length;
				int marker1 = 0;
				int marker2 = 0;

				// Walk through two the strings with two markers.
				while (marker1 < len1 && marker2 < len2) {
					char ch1 = s1[marker1];
					char ch2 = s2[marker2];

					// Some buffers we can build up characters in for each chunk.
					char[] space1 = new char[len1];
					int loc1 = 0;
					char[] space2 = new char[len2];
					int loc2 = 0;

					// Walk through all following characters that are digits or
					// characters in BOTH strings starting at the appropriate marker.
					// Collect char arrays.
					do {
						space1[loc1++] = ch1;
						marker1++;

						if (marker1 < len1) {
							ch1 = s1[marker1];
						}
						else {
							break;
						}
					} while (char.IsDigit(ch1) == char.IsDigit(space1[0]));

					do {
						space2[loc2++] = ch2;
						marker2++;

						if (marker2 < len2) {
							ch2 = s2[marker2];
						}
						else {
							break;
						}
					} while (char.IsDigit(ch2) == char.IsDigit(space2[0]));

					// If we have collected numbers, compare them numerically.
					// Otherwise, if we have strings, compare them alphabetically.
					string str1 = new string(space1);
					string str2 = new string(space2);

					int result;

					if (char.IsDigit(space1[0]) && char.IsDigit(space2[0])) {
						int thisNumericChunk = int.Parse(str1);
						int thatNumericChunk = int.Parse(str2);
						result = thisNumericChunk.CompareTo(thatNumericChunk);
					}
					else {
						result = str1.CompareTo(str2);
					}

					if (result != 0) {
						return result;
					}
				}
				return len1 - len2;
			}
		}
	}
}
