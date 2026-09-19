using Nucleus.Common.OS;

namespace Nucleus.Types;

public enum AppType
{
	NotSpecified,

	Application,
	Game,
	MediaPlayer
}

public struct StartupInfo()
{
	public string? AppCopyright { get; set; }
	public string? AppCreator { get; set; }
	public string AppIdentifier { get; set; }
	public string AppName { get; set; }
	public AppType AppType { get; set; }
	public string? AppURL { get; set; }
	public string? AppVersion { get; set; }

	public override string ToString() {
		return $"GameInfo [{AppName}]";
	}
}

public struct WindowInitialState
{
	public ConfigFlags Flags;
	public int Height;
	public int Width;
}