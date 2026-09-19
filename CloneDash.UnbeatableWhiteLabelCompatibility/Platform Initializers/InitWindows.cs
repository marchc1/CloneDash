using AssetStudio;
using CloneDash.Common.Compatibility.Valve;
using CloneDash.Compatibility.Valve;

using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel
{
	public static partial class UnbeatableWhiteLabelCompatibility
	{
		[MemberNotNull(nameof(Assets))]
		[MemberNotNull(nameof(Assemblies))]
		private static UWLCompatLayerInitResult INIT_WINDOWS() {
			if (!OperatingSystem.IsWindows())
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
				return UWLCompatLayerInitResult.OperatingSystemNotCompatible;

			if (SteamGames.WhereIsSteamInstalled() == null) return UWLCompatLayerInitResult.SteamNotInstalled;
			var installdir = SteamGames.WhereIsGameInstalled(UNBEATABLE_WHITELABEL_APPID);
			if (installdir == null) return UWLCompatLayerInitResult.UnbeatableNotInstalled;

			InstallDir = installdir;
			Assets = new AssetsManager();
			Assets.LoadFolder(System.IO.Path.Combine(installdir, "UNBEATABLE [white label]_Data"));

			Assemblies = new AssemblyLoader();
			Assemblies.Load(installdir);
			Assemblies.Load(System.IO.Path.Combine(installdir, "UNBEATABLE [white label]_Data", "Managed"));

			return UWLCompatLayerInitResult.OK;
#pragma warning restore CS8774 // Member must have a non-null value when exiting.
		}
	}
}
