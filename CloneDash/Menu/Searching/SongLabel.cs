using System.Text.RegularExpressions;

using Nucleus.UI;
using Nucleus.Core;

namespace CloneDash.Menu.Searching;

/// <summary> A Label which always rendering CJK characters.</summary>
public class SongLabel : Label
{
	private string textRaw;

	public new string Text
	{
		get => textRaw;

		set
		{
			Match boldRegexMatch = Util.BoldRegex.Match(value);
			textRaw = boldRegexMatch.Success ? boldRegexMatch.Groups[2].Value : value;
			Font = boldRegexMatch.Success ? Graphics2D.UI_MONO_BOLD_FONT_NAME : Graphics2D.UI_CN_JP_FONT_NAME;
			base.Text = textRaw;
		}
	}
}
