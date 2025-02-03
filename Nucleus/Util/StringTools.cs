using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Util
{
	public static unsafe partial class Util
	{
		public static string CapitalizeFirstCharacter(this string s) => s.Length == 0 ? "" : char.ToUpper(s[0]) + s.Substring(1);
	}
}
