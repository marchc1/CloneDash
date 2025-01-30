using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Platform
{
    public static class TinyFileDialogs
    {
#if COMPILED_WINDOWS
        public const string mDllLocation = "tinyfiledialogs64.dll";
#endif
#if COMPILED_OSX
        public const string mDllLocation = "tinyfiledialogs64.dll";
#endif
        [DllImport(mDllLocation, EntryPoint = "tinyfd_beep", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Beep();

        // cross platform UTF8
        [DllImport(mDllLocation, EntryPoint = "tinyfd_notifyPopup", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int __NotifyPopup(string aTitle, string aMessage, string aIconType);


        [DllImport(mDllLocation, EntryPoint = "tinyfd_messageBox", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int __MessageBox(string aTitle, string aMessage, string aDialogTyle, string aIconType, int aDefaultButton);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_inputBox", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr __InputBox(string aTitle, string aMessage, string aDefaultInput);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_saveFileDialog", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr __SaveFileDialog(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_openFileDialog", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr __OpenFileDialog(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription, int aAllowMultipleSelects);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_selectFolderDialog", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr __SelectFolderDialog(string aTitle, string aDefaultPathAndFile);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_colorChooser", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr __ColorChooser(string aTitle, string aDefaultHexRGB, byte[] aDefaultRGB, byte[] aoResultRGB);

        // windows only utf16
        [DllImport(mDllLocation, EntryPoint = "tinyfd_notifyPopupW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int __NotifyPopupW(string aTitle, string aMessage, string aIconType);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_messageBoxW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int __MessageBoxW(string aTitle, string aMessage, string aDialogTyle, string aIconType, int aDefaultButton);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_inputBoxW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr __InputBoxW(string aTitle, string aMessage, string aDefaultInput);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_saveFileDialogW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr __SaveFileDialogW(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_openFileDialogW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr __OpenFileDialogW(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription, int aAllowMultipleSelects);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_selectFolderDialogW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr __SelectFolderDialogW(string aTitle, string aDefaultPathAndFile);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_colorChooserW", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr __ColorChooserW(string aTitle, string aDefaultHexRGB, byte[] aDefaultRGB, byte[] aoResultRGB);

        // cross platform
        [DllImport(mDllLocation, EntryPoint = "tinyfd_getGlobalChar", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr __GetGlobalChar(string aCharVariableName);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_getGlobalInt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int __GetGlobalInt(string aIntVariableName);

        [DllImport(mDllLocation, EntryPoint = "tinyfd_setGlobalInt", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int __SetGlobalInt(string aIntVariableName, int aValue);

        private static string? stringFromAnsi(IntPtr ptr) => Marshal.PtrToStringAnsi(ptr);


        private static string? stringFromUni(IntPtr ptr) => Marshal.PtrToStringUni(ptr);


        public static string TinyFD_Version {
            get {
                IntPtr lversiontxt = __GetGlobalChar("tinyfd_version");
                string lversionstr = stringFromAnsi(lversiontxt) ?? "<null>";
                return lversionstr;
            }
        }

        public enum NotifyIconType
        {
            Info = 0,
            Warning = 1,
            Error = 2
        }
        public struct DialogResult
        {
            public string? Result;
            public static implicit operator string?(DialogResult self) => self.Result;
            public static implicit operator DialogResult(string? str) => new() { Result = str };

            [MemberNotNullWhen(false, "Result")]
            public bool Cancelled => Result == null;
            public string[] Files => Cancelled ? [] : Result.Split('|');
        }

        public static DialogResult InputBox(string title, string message, string? def = null) {
            IntPtr ptr = __InputBoxW(title, message, def ?? "");
            return stringFromUni(ptr) ?? "<null>";
        }

        public static DialogResult NotifyPopup(string title, string message, NotifyIconType iconType = NotifyIconType.Info) {
            IntPtr ptr = __NotifyPopupW(title, message, Enum.GetName(iconType)?.ToLower() ?? "info");
            return stringFromAnsi(ptr);
        }

        public static DialogResult SaveFileDialog(string title, string defaultPathOrFile, string[] filterPatterns, string filterDescription) {
            IntPtr ptr = __SaveFileDialogW(title, defaultPathOrFile, filterPatterns.Length, filterPatterns, filterDescription);
            return stringFromUni(ptr);
        }

        public static DialogResult OpenFileDialog(string title, string defaultPathOrFile, string[] filterPatterns, string filterDescription, bool allowMultipleSelects = false) {
            IntPtr ptr = __OpenFileDialogW(title, defaultPathOrFile, filterPatterns.Length, filterPatterns, filterDescription, allowMultipleSelects ? 1 : 0);
            return stringFromUni(ptr);
        }
        public static DialogResult SelectFolderDialog(string title, string defaultPathOrFile) {
            IntPtr ptr = __SelectFolderDialogW(title, defaultPathOrFile);
            return stringFromUni(ptr);
        }
    }
}
