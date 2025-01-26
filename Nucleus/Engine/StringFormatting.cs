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
    }
}
