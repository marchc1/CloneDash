using System;

namespace AssetStudio
{
    // Represents set with 16 base colors using ANSI escape codes, which should be supported in most terminals
    // (well, except for windows editions before windows 10)
    public static class ColorConsole
    {
        public static readonly string
            Black = "\u001b[30m",
            Red = "\u001b[31m",
            Green = "\u001b[32m",
            Yellow = "\u001b[33m", //remapped to ~BrightWhite in Windows PowerShell 6
            Blue = "\u001b[34m",
            Magenta = "\u001b[35m", //remapped to ~Blue in Windows PowerShell 6
            Cyan = "\u001b[36m",
            White = "\u001b[37m",
            BrightBlack = "\u001b[30;1m",
            BrightRed = "\u001b[31;1m",
            BrightGreen = "\u001b[32;1m",
            BrightYellow = "\u001b[33;1m",
            BrightBlue = "\u001b[34;1m",
            BrightMagenta = "\u001b[35;1m",
            BrightCyan = "\u001b[36;1m",
            BrightWhite = "\u001b[37;1m";
        private static readonly string Reset = "\u001b[0m";

        public static string Color(this string str, string ansiColor)
        {
            if (!ColorConsoleHelper.isAnsiCodesSupported)
                return str;

            return $"{ansiColor}{str}{Reset}";
        }

        public static string ColorIf(this string str, bool isTrue, string ansiColor, string defaultAnsiColor = null)
        {
            if (isTrue)
                return str.Color(ansiColor);

            if (defaultAnsiColor != null)
                return str.Color(defaultAnsiColor);

            return str;
        }

        public static void AnsiCodesTest()
        {
            Console.WriteLine("ANSI escape codes test");
            Console.WriteLine($"Supported: {ColorConsoleHelper.isAnsiCodesSupported}");
            Console.WriteLine("\u001b[30m A \u001b[31m B \u001b[32m C \u001b[33m D \u001b[0m");
            Console.WriteLine("\u001b[34m E \u001b[35m F \u001b[36m G \u001b[37m H \u001b[0m");
            Console.WriteLine("\u001b[30;1m A \u001b[31;1m B \u001b[32;1m C \u001b[33;1m D \u001b[0m");
            Console.WriteLine("\u001b[34;1m E \u001b[35;1m F \u001b[36;1m G \u001b[37;1m H \u001b[0m");
        }
    }
}
