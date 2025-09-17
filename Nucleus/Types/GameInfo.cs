namespace Nucleus.Types
{
    public struct GameInfo()
    {
        public string AppName { get; set; }
        public string? AppVersion { get; set; }
        public string AppIdentifier { get; set; }
        public string? AppCreator { get; set; }
        public string? AppCopyright { get; set; }
        public string? AppURL { get; set; }
        public AppType AppType { get; set; }
        public override string ToString() {
            return $"GameInfo [{AppName}]";
        }
    }

	public enum AppType {
		NotSpecified,

		Application,
		Game,
		MediaPlayer
	}
}
