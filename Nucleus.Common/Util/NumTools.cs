using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Util
{
	public static unsafe partial class Util
	{
		public static uint RoundUpToPowerOf2(this uint x) {
			return BitOperations.RoundUpToPowerOf2(x);
		}
	}
}
