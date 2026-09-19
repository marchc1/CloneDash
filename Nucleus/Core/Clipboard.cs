using Nucleus.Engine;

namespace Nucleus
{
	public static class Clipboard
	{
		public static string Text {
			get => OS.GetClipboardText();
			set => OS.SetClipboardText(value);
		}
	}
}