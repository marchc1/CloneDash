using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Nucleus.Platform
{
    
    public static class Packages
    {
        public static bool IsLinuxPackageInstalled(string name){
            #if COMPILED_LINUX
                Process p = new Process();
                p.StartInfo.FileName = "/bin/bash";
                p.StartInfo.Arguments = $"-c \"dpkg -s {name}\"";
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();

                string output = p.StandardOutput.ReadToEnd();
                string err = p.StandardOutput.ReadToEnd();

                p.WaitForExit();

                return !string.IsNullOrEmpty(output) && output.Contains("Status: install ok installed");
            #endif
            
            return false;
        }
        /// <summary>
        /// Returns true if the operating system is Linux *and* the package provided by <paramref name="name"/> is not installed.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsLinuxAndPackageNotInstalled(string name){
            #if COMPILED_LINUX
                return !IsLinuxPackageInstalled(name);
            #endif

            return false;
        }

        public static void ErrorIfLinuxAndPackageNotInstalled(string name, string? cmd = null){
            #if COMPILED_LINUX
            if(IsLinuxAndPackageNotInstalled(name)){
                throw new Exception($"You are missing the package '{name}', which is required for the Nucleus engine to run on your system. Please install this package and try again.\r\n\r\n" 
                + (cmd == null ? "The method call to ErrorIfLinuxAndPackageNotInstalled did not provide installation instructions."
                : $"You can likely install this package by running '{cmd}' in your terminal."));
            }
            #endif
        }
    }
}
