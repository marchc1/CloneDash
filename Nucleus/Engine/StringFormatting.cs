using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus
{
    public static class StringFormatting
    {
        public static string FormatNumberByThousands(int n) => $"{n:n0}";
        public static string FormatNumberByThousands(double n) => n % 1 == 0 ? $"{n:n0}" : $"{n:n}";

		// todo: test
		public static string SplitAndCommas(this IEnumerable<string> stringSequence) {
			var count = 0;
			var builder = new StringBuilder();
			string? lastString = null;
			foreach(var str in stringSequence) {
				switch (count) {
					case 0: break;
					case 1: builder.Append(lastString); break;
					default: builder.Append($", {lastString}"); break;
				}
				lastString = str;
				count++;
			}

			switch (count) {
				case 0: return "";
				case 1: return lastString;
				case 2: return $"{builder.ToString()} and {lastString}";
				default: return $"{builder.ToString()}, and {lastString}";
			}
		}
    }
}
