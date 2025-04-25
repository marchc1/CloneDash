#if COMPILED_WINDOWS
using Microsoft.Win32;
#endif
using System.Reflection;

namespace Nucleus;

public static partial class Platform
{
	public static void RegisterFileAssociation(string extension, string progId, string description, string? openWith = null) {

#if COMPILED_WINDOWS
		// TODO: if the folder path contains .dll this doesn't work... fix that
		string appPath = openWith ?? (Assembly.GetEntryAssembly()?.Location.Replace(".dll", ".exe")) ?? throw new Exception("Wtf");
		if (!extension.StartsWith("."))
			extension = "." + extension;

		try {
			using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}")) 
				if (extKey != null) 
					extKey.SetValue("", progId); 
				
			using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}")) {
				if (progIdKey != null) {
					progIdKey.SetValue("", description);

					using (var defaultIconKey = progIdKey.CreateSubKey("DefaultIcon")) 
						defaultIconKey?.SetValue("", $"{appPath},0");
					
					using (var shellKey = progIdKey.CreateSubKey(@"shell\open\command")) 
						shellKey?.SetValue("", $"\"{appPath}\" \"%1\""); 
				}
			}

			Logs.Debug($"'{extension}' registered for the current user.");
			Logs.Debug($"    > ProgID:      {progId}");
			Logs.Debug($"    > Description: {description}");
			Logs.Debug($"    > OpenWith:    {appPath}");
		}
		catch (Exception ex) {
			Logs.Warn($"Failed to register file type: {ex.Message}");
		}

		RefreshExplorer();
#else
		Logs.Info("Cannot register file extensions with the current platform. Only supports WINDOWS.");
		Logs.Info("    (if you wish to contribute platform-specific code, see Nucleus/Platform/FileAssociations.cs)");
#endif
	}

#if COMPILED_WINDOWS
	private static void RefreshExplorer() {
		try {
			SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
			Logs.Debug("Windows Explorer was notified about the file association.");
		}
		catch (Exception ex) {
			Logs.Debug("Failed to notify Windows Explorer about the file association: " + ex.Message);
		}
	}

	[System.Runtime.InteropServices.DllImport("Shell32.dll")]
	private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
#endif
}