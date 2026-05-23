using System.Diagnostics.CodeAnalysis;
using TinyDialogsNet;
using TinyDialogs = TinyDialogsNet.TinyDialogs;

#if COMPILED_WINDOWS
using System.Runtime.InteropServices;
#endif

namespace Nucleus;

public static partial class Platform
{
	public struct DialogResult
	{
		private string[]? result;
		public static implicit operator string?(DialogResult self) => self.Result;
		public static implicit operator DialogResult(string? str) => str == null ? new() { result = null } : new() { result = [str] };

		[MemberNotNullWhen(false, "result")]
		public bool Cancelled => result == null;
		public string Result => result?[0] ?? throw new Exception("The operation was cancelled! (developer forgot to check Cancelled property...)");
		public string[] Files => result ?? throw new Exception("The operation was cancelled! (developer forgot to check Cancelled property...)");

		// I switched out the old bindings for TinyDialogsNet. These methods are just
		// to make it easier and not have to replace DialogResult where it's already used.
		public static implicit operator DialogResult((bool cancelled, string text) fromTDN) => new() { result = fromTDN.cancelled ? null : [fromTDN.text] };
		public static implicit operator DialogResult((bool cancelled, IEnumerable<string> paths) fromTDN) => new() { result = fromTDN.cancelled ? null : fromTDN.paths.ToArray() };
	}
	static string platStr(string str) =>
#if COMPILED_WINDOWS
		str;
#else
		$"{str}\0";
#endif
	static string[] platStr(string[] strs) => strs.Select(platStr).ToArray();

	public static DialogResult InputBox(InputBoxType type, string title, string message, string? def = null)
		=> TinyDialogs.InputBox(type, platStr(title), platStr(message), platStr(def ?? ""));
	public static void NotifyPopup(string title, string message, NotificationIconType iconType = NotificationIconType.Information)
		=> TinyDialogs.NotifyPopup(iconType, platStr(title), platStr(message));
	public static DialogResult SaveFileDialog(string title, string defaultPathOrFile, string[] filterPatterns, string filterDescription)
		=> TinyDialogs.SaveFileDialog(platStr(title), platStr(defaultPathOrFile), new(platStr(filterDescription), platStr(filterPatterns)));

	public static DialogResult OpenFileDialog(string title, string defaultPathOrFile, string[] filterPatterns, string filterDescription, bool allowMultipleSelects = false)
		=> TinyDialogs.OpenFileDialog(platStr(title), platStr(defaultPathOrFile), allowMultipleSelects, new(platStr(filterDescription), platStr(filterPatterns)));

#if COMPILED_WINDOWS
	[DllImport("ole32.dll")] private static extern int CoCreateInstance(ref Guid clsid, IntPtr pUnkOuter, uint dwClsContext, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IFileOpenDialog ppv);
	[DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern int SHCreateItemFromParsingName(string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

	[DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();

	[ComImport, Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IFileOpenDialog
	{
		[PreserveSig] int Show(IntPtr hwndOwner);
		void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
		void SetFileTypeIndex(uint iFileType);
		void GetFileTypeIndex(out uint piFileType);
		void Advise(IntPtr pfde, out uint pdwCookie);
		void Unadvise(uint dwCookie);
		void SetOptions(uint fos);
		void GetOptions(out uint pfos);
		void SetDefaultFolder([MarshalAs(UnmanagedType.Interface)] IShellItem psi);
		void SetFolder([MarshalAs(UnmanagedType.Interface)] IShellItem psi);
		void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
		void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
		void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
		void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
		void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
		void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
		void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
		void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
		void AddPlace([MarshalAs(UnmanagedType.Interface)] IShellItem psi, int fdap);
		void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
		void Close(int hr);
		void SetClientGuid(ref Guid guid);
		void ClearClientData();
		void SetFilter(IntPtr pFilter);
		void GetResults(out IntPtr ppenum);
		void GetSelectedItems(out IntPtr ppsai);
	}

	[ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellItem
	{
		void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
		void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
		void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
		void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
		void Compare([MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint hint, out int piOrder);
	}
	private static Guid CLSID_FileOpenDialog = new("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
	private static Guid IID_IFileOpenDialog = new("D57C7288-D4AD-4768-BE02-9D969532D960");
	private static Guid IID_IShellItem = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");
#endif

	public static DialogResult SelectFolderDialog(string title, string defaultPathOrFile)
#if COMPILED_WINDOWS // Windows callsite in tiny dialogs sucks here, it uses the ancient Windows folder dialog
	{
#pragma warning disable CA1416 // Validate platform compatibility
		var hr = CoCreateInstance(ref CLSID_FileOpenDialog, IntPtr.Zero, 1 /* CLSCTX_INPROC_SERVER */, ref IID_IFileOpenDialog, out var dialog);

		if (hr != 0)
			return default;

		try {
			if (!string.IsNullOrEmpty(title))
				dialog.SetTitle(title);

			dialog.GetOptions(out uint options);
			dialog.SetOptions(options | 0x00000020u /* FOS_PICKFOLDERS */ | 0x00000040u /* FOS_FORCEFILESYSTEM */);

			if (!string.IsNullOrEmpty(defaultPathOrFile)) {
				var dirPath = Directory.Exists(defaultPathOrFile) ? defaultPathOrFile : Path.GetDirectoryName(defaultPathOrFile);

				if (!string.IsNullOrEmpty(dirPath)) {
					SHCreateItemFromParsingName(dirPath, IntPtr.Zero, ref IID_IShellItem, out var folder);
					if (folder != null) {
						dialog.SetFolder(folder);
						Marshal.ReleaseComObject(folder);
					}
				}
			}

			hr = dialog.Show(GetActiveWindow());
			if (hr != 0)
				return default;

			dialog.GetResult(out var item);
			item.GetDisplayName(0x80058000 /* SIGDN_FILESYSPATH */, out var path);
			Marshal.ReleaseComObject(item);

			return path;
		}
		finally {
			Marshal.ReleaseComObject(dialog);
		}
#pragma warning restore CA1416 // Validate platform compatibility
	}
#else
		=> TinyDialogs.SelectFolderDialog(platStr(title), platStr(defaultPathOrFile));
#endif
}
