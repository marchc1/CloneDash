using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.UI;

namespace CloneDash.Menu.Searching;

/// <summary> A Label which always renders CJK characters.</summary>
public class SongLabel(Element? parent) : Label(parent)
{
	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		// The scheme's default font (e.g. Afacad) is Latin-only, so song titles/authors
		// must use the culture-appropriate Noto CJK font to render kanji/kana/hangul.
		Font = Graphics2D.UI_CN_JP_FONT_NAME;
	}
}
