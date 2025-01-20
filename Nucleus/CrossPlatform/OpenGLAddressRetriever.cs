using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.CrossPlatform
{
    public static class OpenGLAddressRetriever
    {
#if COMPILED_WINDOWS

        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("opengl32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr wglGetProcAddress(string procedureName);

        public static IntPtr GetProc(string funcName) {
            //Console.WriteLine($"funcName: {funcName}");
            var ret = wglGetProcAddress(funcName);
            if (ret == IntPtr.Zero)
                ret = GetProcAddress(LoadLibrary("opengl32.dll"), funcName);

            if (ret == IntPtr.Zero)
                throw new Exception($"no {funcName}");

            return ret;
        }
#endif

#if COMPILED_OSX
    // DllImport to load dynamic libraries in macOS
        [DllImport("libdl.dylib", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr dlopen(string fileName, int flags);

        [DllImport("libdl.dylib", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        // Constants for dlopen flags
        private const int RTLD_NOW = 2;

        public static IntPtr GetProc(string funcName)
        {
            IntPtr handle = dlopen("/System/Library/Frameworks/OpenGL.framework/OpenGL", RTLD_NOW);
            if (handle == IntPtr.Zero)
                throw new Exception("Failed to load OpenGL framework.");

            IntPtr ret = dlsym(handle, funcName);

            // If function still not found, throw an exception
            if (ret == IntPtr.Zero)
                throw new Exception($"Function {funcName} not found in OpenGL.");

            return ret;
        }
#endif

#if COMPILED_LINUX
        // equivalent of LoadLibrary
        [DllImport("libc")]
        private static extern IntPtr dlopen(string filename, int flags);

        // equivalent of GetProcAddress
        [DllImport("libc")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        public static IntPtr GetProc(string funcName)
        {
            var ret = dlsym(IntPtr.Zero, funcName);

            if (ret == IntPtr.Zero)
            {
                IntPtr libHandle = dlopen("libGL.so", RTLD_NOW | RTLD_GLOBAL);
                if (libHandle != IntPtr.Zero)
                {
                    ret = dlsym(libHandle, funcName);
                    dlclose(libHandle); 
                }
            }

            if (ret == IntPtr.Zero)
                Logs.Debug($"Failed to retrieve function address for {funcName}. While the engine will continue to boot, calls to this method will crash the engine.");

            return ret;
        }

        private const int RTLD_NOW = 2;
        private const int RTLD_GLOBAL = 0x100;

        [DllImport("dl")]
        private static extern int dlclose(IntPtr handle);
#endif
    }
}
