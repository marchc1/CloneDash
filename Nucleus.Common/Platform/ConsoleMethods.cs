using System.Drawing;
using Console = Colorful.Console;

namespace Nucleus;

public static partial class Platform
{
	public static void ConsoleInitialize(Color back, Color fore) {
		Console.ForegroundColor = fore;
	}
	public static void ConsoleWrite(ReadOnlySpan<char> str, Color c) {
		ConsoleColor foregroundColor = System.Console.ForegroundColor;
		Console.ForegroundColor = c;
		for (int i = 0; i < str.Length; i++) 
			Console.Write(str[i]);
		System.Console.ForegroundColor = foregroundColor;
	}
	public static void ConsoleWriteLine() => Console.Write(Environment.NewLine);
}
