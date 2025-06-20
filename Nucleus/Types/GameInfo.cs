namespace Nucleus.Types
{
    public struct GameInfo()
    {
        public string GameName { get; set; }
        public override string ToString() {
            return $"GameInfo [{GameName}]";
        }
    }
}
