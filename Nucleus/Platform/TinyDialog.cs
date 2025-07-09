using System.Diagnostics.CodeAnalysis;
using TinyDialogsNet;
using TinyDialogs = TinyDialogsNet.TinyDialogs;

namespace Nucleus;

public static partial class Platform
{
	public struct DialogResult
	{
		private string[]? result;
		public static implicit operator string?(DialogResult self) => self.Result;
		public static implicit operator DialogResult(string? str) => str == null ? new() { result = null } : new() { result = [str] };

		[MemberNotNullWhen(false, "result")]
		public bool Cancelled => result == null;
		public string Result => result?[0] ?? throw new Exception("The operation was cancelled! (developer forgot to check Cancelled property...)");
		public string[] Files => result ?? throw new Exception("The operation was cancelled! (developer forgot to check Cancelled property...)");

		// I switched out the old bindings for TinyDialogsNet. These methods are just
		// to make it easier and not have to replace DialogResult where it's already used.
		public static implicit operator DialogResult((bool cancelled, string text) fromTDN) => new() { result = fromTDN.cancelled ? null : [fromTDN.text] };
		public static implicit operator DialogResult((bool cancelled, IEnumerable<string> paths) fromTDN) => new() { result = fromTDN.cancelled ? null : fromTDN.paths.ToArray() };
	}

	public static DialogResult InputBox(InputBoxType type, string title, string message, string? def = null)
		=> TinyDialogs.InputBox(type, title, message, def ?? "");
	public static void NotifyPopup(string title, string message, NotificationIconType iconType = NotificationIconType.Information)
		=> TinyDialogs.NotifyPopup(iconType, title, message);
	public static DialogResult SaveFileDialog(string title, string defaultPathOrFile, string[] filterPatterns, string filterDescription)
		=> TinyDialogs.SaveFileDialog(title, defaultPathOrFile, new(filterDescription, filterPatterns));

	public static DialogResult OpenFileDialog(string title, string defaultPathOrFile, string[] filterPatterns, string filterDescription, bool allowMultipleSelects = false)
		=> TinyDialogs.OpenFileDialog(title, defaultPathOrFile, allowMultipleSelects, new(filterDescription, filterPatterns));

	public static DialogResult SelectFolderDialog(string title, string defaultPathOrFile)
		=> TinyDialogs.SelectFolderDialog(title, defaultPathOrFile);
}
