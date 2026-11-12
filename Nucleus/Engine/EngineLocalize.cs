using Nucleus.Commands;
using Nucleus.Common.Localization;
using Nucleus.Util;
using System.Collections.Concurrent;
using System.Globalization;

namespace Nucleus.Engine;

public class SingleLanguage : ConcurrentDictionary<UtlSymId_t, string>;

[MarkForStaticConstruction]
public class EngineLocalize : ILocalize
{
	public static readonly ConVar language = new("language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName, FCvar.Saved, "Language used for localization.", callback: LN_Changed);
	static CultureInfo currentCulture = CultureInfo.InvariantCulture;
	private static void LN_Changed(ConVar self, ReadOnlySpan<char> oldStr, double oldDouble) {
		CultureInfo lastCulture = currentCulture;
		try {
			currentCulture = CultureInfo.GetCultureInfo(new string(self.GetString()));
		}
		catch (CultureNotFoundException) {
			currentCulture = CultureInfo.InvariantCulture;
		}

		if (lastCulture != currentCulture)
			((EngineLocalize)localize).CallLanguageChangeCallbacks(lastCulture, currentCulture);
	}

	readonly ConcurrentDictionary<CultureInfo, SingleLanguage> languages = new();

	public void AddString(ReadOnlySpan<char> key, ReadOnlySpan<char> value, CultureInfo culture) {
		if (key.IsEmpty) {
			Logs.Warn($"Attempted to add empty localization string (ignored");
			return;
		}
		if (key[0] != '#') {
			Logs.Warn($"Attempted to add localization string '{key}', which does not start with '#' (ignored");
			return;
		}
		UtlSymId_t hash = key.Hash();
		SingleLanguage language = languages.GetOrAdd(culture, static _ => new());
		language.TryAdd(hash, new(value.SliceNullTerminatedString()));
	}

	public CultureInfo GetCurrentCulture() => currentCulture;

	public ReadOnlySpan<char> Find(ReadOnlySpan<char> key) {
		if (key.IsEmpty || key[0] != '#')
			return key;
		UtlSymId_t hash = key.Hash();

		if (languages.TryGetValue(currentCulture, out SingleLanguage? language) && language.TryGetValue(hash, out string? value))
			return value;

		if (currentCulture != CultureInfo.InvariantCulture
			&& languages.TryGetValue(CultureInfo.InvariantCulture, out SingleLanguage? invariant)
			&& invariant.TryGetValue(hash, out string? invariantValue))
			return invariantValue;

		return key;
	}

	public ReadOnlySpan<char> Find(ReadOnlySpan<char> key, Span<char> buffer) {
		ReadOnlySpan<char> result = Find(key);
		int length = Math.Min(result.Length, buffer.Length);
		result[..length].CopyTo(buffer);
		return buffer[..length];
	}

	public void SetCurrentCulture(CultureInfo culture) {
		language.SetValue(culture.Name);
	}

	public CultureInfo? TryGetCultureByName(ReadOnlySpan<char> cultureName) {
		try {
			return CultureInfo.GetCultureInfo(new string(cultureName));
		}
		catch (CultureNotFoundException) {
			return null;
		}
	}

	event LanguageChangedFn? LanguageChanged;
	public void InstallLanguageChangeCallback(LanguageChangedFn cb) => LanguageChanged += cb;
	public void RemoveLanguageChangeCallback(LanguageChangedFn cb) => LanguageChanged -= cb;
	public void CallLanguageChangeCallbacks(CultureInfo lastCulture, CultureInfo currentCulture) => LanguageChanged?.Invoke(this, lastCulture, currentCulture);
}
