using AssetStudio;
using CloneDash.Common.Compatibility.Valve;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public static partial class UnbeatableWhiteLabelCompatibility
{
	[MemberNotNull(nameof(Assets))]
	[MemberNotNull(nameof(Assemblies))]
	private static AppStatus INIT_WINDOWS() {
		if (!OperatingSystem.IsWindows())
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
			return AppStatus.OperatingSystemNotCompatible;

		if (!SteamApps.WhereIsAppInstalled(UNBEATABLE_WHITELABEL_APPID).IsOK(out string? installdir, out AppStatus err))
			return err;

		InstallDir = installdir;
		Assets = new AssetsManager();
		Assets.LoadFolder(System.IO.Path.Combine(installdir, "UNBEATABLE [white label]_Data"));

		Assemblies = new AssemblyLoader();
		Assemblies.Load(installdir);
		Assemblies.Load(System.IO.Path.Combine(installdir, "UNBEATABLE [white label]_Data", "Managed"));

		return AppStatus.OK;
#pragma warning restore CS8774 // Member must have a non-null value when exiting.
	}
}
