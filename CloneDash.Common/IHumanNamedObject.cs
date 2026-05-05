namespace CloneDash.Common;

public interface IHumanNamedObject
{
	/// <summary>
	/// Get the human-friendly name. If no translation exists for the desired language, you'll get
	/// whatever the object decides to give you. You can confirm the language was your desired language
	/// via comparison of the returnedLanguage and desiredLanguage
	/// </summary>
	ReadOnlySpan<char> GetName(in HumanLanguage desiredLanguage, out HumanLanguage returnedLanguage);
}