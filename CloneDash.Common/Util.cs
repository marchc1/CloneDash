using System.Text.RegularExpressions;

namespace CloneDash;

public static partial class Util
{
	/// <summary>
	/// This is used to parse out text in Strawberry Godzilla from Muse Dash. <br/>
	/// <b>TODO</b>: More accurate Regex? 
	/// </summary>
	public static readonly Regex BoldRegex = BoldRegexGenerator();

	[GeneratedRegex("^(.*)<b>(.+)<\\/b>(.*)$")]
	private static partial Regex BoldRegexGenerator();
}