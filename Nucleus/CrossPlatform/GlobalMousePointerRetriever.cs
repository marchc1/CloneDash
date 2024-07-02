using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.CrossPlatform
{
    public static class GlobalMousePointerRetriever
    {
#if COMPILED_WINDOWS
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
#endif

        public static unsafe Vector2F GetMousePos() {
#if COMPILED_WINDOWS 
            GetCursorPos(out var pt);
            ScreenToClient((nint)Raylib.GetWindowHandle(), ref pt);
            return new Vector2F(pt.X, pt.Y);
#else
            throw new NotImplementedException("No OS platform support for MousePos!");
#endif
        }
    }
}
