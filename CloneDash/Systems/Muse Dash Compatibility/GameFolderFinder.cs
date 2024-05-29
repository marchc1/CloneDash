using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloneDash
{
    public static partial class MuseDashCompatibility
    {
        public const uint MUSEDASH_APPID = 774171;
        public static string? WhereIsMuseDashInstalled { get; set; } = null;
        public static bool IsMuseDashInstalled => WhereIsMuseDashInstalled != null;
    }
}
