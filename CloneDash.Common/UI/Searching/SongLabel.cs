using System.Text.RegularExpressions;

using Nucleus.UI;
using Nucleus.Core;

namespace CloneDash.Menu.Searching;

/// <summary> A Label which always rendering CJK characters.</summary>
public class SongLabel(Element? parent) : Label(parent)
{
	private string textRaw;

	public override void SetText(ReadOnlySpan<char> text) {
		string tempRegex = new(text);
		Match boldRegexMatch = Util.BoldRegex.Match(tempRegex);
		textRaw = boldRegexMatch.Success ? boldRegexMatch.Groups[2].Value : tempRegex;
		SetFont (boldRegexMatch.Success ? Graphics2D.UI_MONO_BOLD_FONT_NAME : Graphics2D.UI_CN_JP_FONT_NAME);
		base.SetText(text);
	}
}
