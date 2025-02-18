// Credit: https://github.com/Mushus/bms-parser

using System.Collections.Generic;

namespace BMS
{
    /// <summary>
    /// BMS データ
    /// </summary>
    internal class BMSData
    {
        public Player? Player;
        /// <summary>
        /// ジャンル
        /// </summary>
        /// <remark>曲のジャンル情報</remark>
        public string Genre;

        public string Title;
        public string Artist;
        public int? BPM;
        public string MidiFile;
        public int? PlayLevel;
        public Rank? Rank;
        public int? VolWav;
        public double? Total;
        public Dictionary<int, string> Bmp = new Dictionary<int, string>();
        public Dictionary<int, string> Wav = new Dictionary<int, string>();
        public List<IObject> Objects = new List<IObject>();
    }
}