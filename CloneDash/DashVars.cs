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
        public static readonly GameVersion Version = new GameVersion(0, 0, 2);

        public static readonly Color TopPathwayColor = new Color(178, 255, 252, 120);
        public static readonly Color BottomPathwayColor = new Color(248, 178, 255, 120);
        public static readonly Color MultiColor = new Color(220, 160, 140, 255);

        /// <summary>
        /// When the game started.
        /// </summary>
        public static DateTime Startup { get; set; } = DateTime.Now;

        /// <summary>
        /// <b>Should NOT be used for game events.</b> This value will CONSTANTLY update, regardless of lag spikes, game pausing, etc... <br></br>
        /// See Game.Conductor.Time for a musically-aligned version of this function<br></br><br></br>
        /// Returns the time (in seconds) since the game started.
        /// </summary>
        public static double Curtime => (DateTime.Now - Startup).TotalSeconds;

        /// <summary>
        /// The distance (scrw * this) where the pathway meets notes
        /// </summary>
        public const float PATHWAY_XDISTANCE = 0.22f; //0.21 before

        /// <summary>
        /// The distance (scrw * (0.5 + (+/-)this)) between the two pathways
        /// </summary>
        public const float PATHWAY_YDISTANCE = 0.25f;

        /// <summary>
        /// Mashers 'max hits' is calculated by using the length of the mashers visibility * 25 in Muse Dash. Change this if you want more mashing, for some reason.<br>
        /// May become a non-constant value at some point when game wide options are implemented
        /// </summary>
        public const int MASHER_MAX_HITS_PER_SECOND = 25;
    }
}
