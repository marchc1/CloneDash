using System.Drawing;
using Console = Colorful.Console;

namespace Nucleus;

    public static partial class Platform
    {
        public static void ConsoleInitialize(Color back, Color fore){
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
        }
        public static void ConsoleWrite(string str, Color c) => Console.Write(str, c);
        public static void ConsoleWriteLine() => Console.Write(Environment.NewLine);
    }
