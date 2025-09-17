namespace Nucleus.Types
{
    public struct GameInfo()
    {
        public string AppName { get; set; }
        public string? AppVersion { get; set; }
        public string AppIdentifier { get; set; }
        public override string ToString() {
            return $"GameInfo [{AppName}]";
        }
    }
}
