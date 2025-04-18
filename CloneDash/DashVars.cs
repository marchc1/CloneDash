using Raylib_cs;

namespace CloneDash
{
    public struct GameVersion {
        public int Major;
        public int Minor;
        public int Micro;

        public GameVersion(int major, int minor, int micro) {
            this.Major = major;
            this.Minor = minor;
            this.Micro = micro;
        }
    }
    public static class DashVars
    {
        public static readonly GameVersion Version = new GameVersion(0, 2, 0);

        /// <summary>
        /// Mashers 'max hits' is calculated by using the length of the mashers visibility * 25 in Muse Dash. Change this if you want more mashing, for some reason.<br>
        /// May become a non-constant value at some point when game wide options are implemented
        /// </summary>
        public const int MASHER_MAX_HITS_PER_SECOND = 25;
    }
}
