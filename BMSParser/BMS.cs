// Credit: https://github.com/Mushus/bms-parser

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BMS
{
    public class BMS
    {
        BMSData data;
        Timeline timeline;
        internal BMS(BMSData data)
        {
            this.data = data;
            this.timeline = new Timeline(this);
        }

        public Player Player
        {
            get
            {
                return this.data.Player ?? Player.Single;
            }
        }
        public string Genre
        {
            get
            {
                return this.data.Genre ?? "";
            }
        }
        public string Title
        {
            get
            {
                return this.data.Title ?? "";
            }
        }
        public string Artist
        {
            get
            {
                return this.data.Artist ?? "";
            }
        }
        public int BPM
        {
            get
            {
                return this.data.BPM ?? 130;
            }
        }
        public string MidiFile
        {
            get
            {
                return this.data.MidiFile;
            }
        }
        public int PlayLevel
        {
            get
            {
                return this.data.PlayLevel ?? 3;
            }
        }
        public Rank Rank
        {
            get
            {
                return this.data.Rank ?? Rank.Easy;
            }
        }
        public int VolWav
        {
            get
            {
                return this.data.VolWav ?? 100;
            }
        }
        public double Total
        {
            get
            {
                // TODO: 省略時200+オブジェ数
                // 未実装
                return this.data.Total ?? 200;
            }
        }
        public ReadOnlyDictionary<int, string> Wav
        {
            get
            {
                return new ReadOnlyDictionary<int, string>(this.data.Wav);
            }
        }

        public ReadOnlyDictionary<int, string> Bmp
        {
            get
            {
                return new ReadOnlyDictionary<int, string>(this.data.Bmp);
            }
        }

        public ReadOnlyCollection<IObject> Objects
        {
            get
            {
                return this.data.Objects.AsReadOnly();
            }
        }

        public Timeline Timeline
        {
            get
            {
                return this.timeline;
            }
        }
    }
}