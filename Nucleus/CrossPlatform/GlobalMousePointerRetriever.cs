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
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y) {
                X = x;
                Y = y;
            }
        }
#if COMPILED_WINDOWS
        [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
#endif
#if COMPILED_LINUX
        [DllImport("libX11.so")] private static extern IntPtr XOpenDisplay(IntPtr display);
        [DllImport("libX11.so")] private static extern int XQueryPointer(
            IntPtr display, IntPtr w, out IntPtr root_return, out IntPtr child_return, out int root_x_return, out int root_y_return,
            out int win_x_return, out int win_y_return, out uint mask_return);
        [DllImport("libX11.so")] private static extern IntPtr XRootWindow(IntPtr display, int screen_number);

        private static IntPtr display;
        private static bool __initialized = false;
        private static void init(){
            if(__initialized) return;

            display = XOpenDisplay(IntPtr.Zero);
            if(display == IntPtr.Zero){
                throw new Exception("Unable to open X11 display.");
            }

            __initialized = true;
        }

        private static IntPtr Display {
            get {
                init();
                return display;
            }
        }

        private static POINT GetMousePosX11(){
            IntPtr root = XRootWindow(Display, 0);
            int rootX, rootY, winX, winY;
            IntPtr rootReturn, childReturn;
            uint maskReturn;

            if(XQueryPointer(Display, root, out rootReturn, out childReturn, out rootX, out rootY, out winX, out winY, out maskReturn) == 0){
                Logs.Warn("Unable to query the pointer position from X11!");
                return new(0, 0);
            }

            return new(rootX, rootY);
        }
#endif


        public static unsafe Vector2F GetMousePos() {
#if COMPILED_WINDOWS
            GetCursorPos(out var pt);
            ScreenToClient((nint)Raylib.GetWindowHandle(), ref pt);
            return new Vector2F(pt.X, pt.Y);
#endif
#if COMPILED_LINUX
            var ret = GetMousePosX11();
            return new(ret.X, ret.Y);
#endif
        }
    }
}
