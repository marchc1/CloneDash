using Nucleus;
using Nucleus.Commands;
using Nucleus.Common;
using System.Globalization;

namespace CloneDash.Common;

public delegate ReadOnlySpan<char> LanguageStringFn(in HumanLanguage desiredLanguage, out HumanLanguage returnedLanguage);

/// <summary>
/// Generic human language enum with bit-packed character codes
/// </summary>
[MarkForStaticConstruction]
public record struct HumanLanguage
{
	public static readonly ConVar language = new("language", "en", FCvar.Saved, "Game language");
	public static HumanLanguage GetCurrentLanguage() {
		return new(language.GetString());
	}

	public CultureInfo Culture;


	public HumanLanguage(ReadOnlySpan<char> code){
		Culture = CultureInfo.GetCultureInfo(new(code.SliceNullTerminatedString()), false);
	}

	public static readonly HumanLanguage Any = new("iv");

	public static readonly HumanLanguage Chinese = new("zh");
	public static readonly HumanLanguage English = new("en");
	public static readonly HumanLanguage Japanese = new("ja");
	public static readonly HumanLanguage Korean = new("ko");
}
