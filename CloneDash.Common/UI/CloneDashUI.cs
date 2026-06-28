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

		public static Color AccentPrimary { get; } = ColorExtensions.ParseHex("#7FD0FD");
		public static Color AccentBackground { get; } = ColorExtensions.ParseHex("#182025");
		public static Color AccentText { get; } = ColorExtensions.ParseHex("#E9F5FF");

		public static Color CharacterPrimary { get; } = ColorExtensions.ParseHex("#FFC8AC");
		public static Color CharacterBackground { get; } = ColorExtensions.ParseHex("#251C18");
		public static Color CharacterText { get; } = ColorExtensions.ParseHex("#FFF0E9");

		public static Color OptionsPrimary { get; } = ColorExtensions.ParseHex("#FFEDAC");
		public static Color OptionsBackground { get; } = ColorExtensions.ParseHex("#252218");
		public static Color OptionsText { get; } = ColorExtensions.ParseHex("#FFFAE9");

		public static float GetFontSize(float size) => size * 1.4f;
		
		public CloneDashUI() {
			SetScheme(ElementSchemeSystem.LoadScheme("resource", "clonedash"));
		}
	}
}