using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Extensions;
using Nucleus.UI;

namespace CloneDash.Common.UI
{
    public class CloneDashUI : UserInterface
    {
        public const string FontNormal = "Afacad Medium";
        public const string FontBold = "Afacad Bold";

        public static string GetBoldFont(IScheme? scheme) =>
            scheme?.GetFontStyle("Nucleus.DefaultBold").Name ?? FontBold;

        public static float GetFontSize(float size) => size * 1.4f;

        public CloneDashUI()
        {
            SetScheme(ElementSchemeSystem.LoadScheme("resource", "clonedash"));
        }
    }
}