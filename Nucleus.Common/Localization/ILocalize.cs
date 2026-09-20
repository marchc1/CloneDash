using System.Globalization;

namespace Nucleus.Common.Localization;

public delegate void LanguageChangedFn(ILocalize localization, CultureInfo oldCulture, CultureInfo newCulture);

public interface ILocalize
{
	public static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
	public static readonly CultureInfo ChineseSimplified = CultureInfo.GetCultureInfo("zh-CN");
	public static readonly CultureInfo ChineseTraditional = CultureInfo.GetCultureInfo("zh-TW");
	public static readonly CultureInfo Japanese = CultureInfo.GetCultureInfo("ja");
	public static readonly CultureInfo Korean = CultureInfo.GetCultureInfo("ko");

	void AddString(ReadOnlySpan<char> key, ReadOnlySpan<char> value, CultureInfo culture);
	CultureInfo GetCurrentCulture();
	ReadOnlySpan<char> Find(ReadOnlySpan<char> key);
	ReadOnlySpan<char> Find(ReadOnlySpan<char> key, Span<char> buffer);
	void SetCurrentCulture(CultureInfo culture);
	CultureInfo? TryGetCultureByName(ReadOnlySpan<char> cultureName);

	void InstallLanguageChangeCallback(LanguageChangedFn cb);
	void RemoveLanguageChangeCallback(LanguageChangedFn cb);
}
