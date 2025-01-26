using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus
{
	public static class Clipboard
	{
		public static string Text {
			get => Raylib.GetClipboardText_();
			set => Raylib.SetClipboardText(value);
		}
	}
}
