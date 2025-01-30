using System.Drawing;
using Console = Colorful.Console;

namespace Nucleus.Platform
{
    
    public static class ConsoleMethods
    {
        public static void Initialize(Color back, Color fore){
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
        }
        public static void Write(string str, Color c) => Console.Write(str, c);
        public static void WriteLine() => Console.Write(Environment.NewLine);
    }
}
