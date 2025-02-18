// Credit: https://github.com/Mushus/bms-parser

using System.IO;
using System.Text.RegularExpressions;
using Base36Library;

namespace BMS
{
    public class Parser
    {
        private static Regex DefPlayer = new Regex("^PLAYER ([1-3])$");
        private static Regex DefGenre = new Regex("^GENRE (.*)$");
        private static Regex DefTitle = new Regex("^TITLE (.*)$");
        private static Regex DefArtist = new Regex("^ARTIST (.*)$");
        private static Regex DefBPM = new Regex("^BPM (\\d+)$");
        private static Regex DefMidiFile = new Regex("^MIDIFILE (.*)$");
        private static Regex DefPlayLevel = new Regex("^PLAYLEVEL (\\d+)$");
        private static Regex DefRank = new Regex("^RANK ([0-3])$");
        private static Regex DefVolWav = new Regex("^VOLWAV (\\d+)$");
        private static Regex DefTotal = new Regex("^TOTAL ([+-]?([0-9]+(\\.[0-9]*)?|\\.[0-9]+)([eE][+-]?[0-9]+)?)$");
        private static Regex DefWav = new Regex("^WAV([0-9A-Z]{2}) (.*)$");
        private static Regex DefBmp = new Regex("^BMP([0-9A-Z]{2}) (.*)$");
        private static Regex DefObj = new Regex("^([0-9]{3})([0-9]{2}):(.*)$");
        public Parser()
        {

        }

        public BMS Parse(Stream s)
        {
            var bmsData = new BMSData();
            var sr = new StreamReader(s);
            while (sr.Peek() > -1)
            {
                var line = sr.ReadLine();
                if (line.Length == 0) continue;
                switch (line[0])
                {
                    case '*': // コメント
                        continue;
                    case '#':
                        this.parseDef(bmsData, line.Substring(1));
                        break;
                }
            }
            return new BMS(bmsData);
        }

        void parseDef(BMSData bmsData, string def)
        {
            Match m;

            m = DefPlayer.Match(def);
            if (m.Success)
            {
                var value = int.Parse(m.Groups[1].Value);
                bmsData.Player = (Player)value;
                return;
            }

            m = DefGenre.Match(def);
            if (m.Success)
            {
                bmsData.Genre = m.Groups[1].Value;
                return;
            }

            m = DefTitle.Match(def);
            if (m.Success)
            {
                bmsData.Title = m.Groups[1].Value;
                return;
            }

            m = DefArtist.Match(def);
            if (m.Success)
            {
                bmsData.Artist = m.Groups[1].Value;
                return;
            }

            m = DefBPM.Match(def);
            if (m.Success)
            {
                var value = int.Parse(m.Groups[1].Value);
                bmsData.BPM = value;
                return;
            }

            m = DefMidiFile.Match(def);
            if (m.Success)
            {
                bmsData.MidiFile = m.Groups[1].Value;
                return;
            }

            m = DefPlayLevel.Match(def);
            if (m.Success)
            {
                var value = int.Parse(m.Groups[1].Value);
                bmsData.PlayLevel = value;
                return;
            }

            m = DefRank.Match(def);
            if (m.Success)
            {
                var value = int.Parse(m.Groups[1].Value);
                bmsData.Rank = (Rank)value;
                return;
            }

            m = DefVolWav.Match(def);
            if (m.Success)
            {
                var value = int.Parse(m.Groups[1].Value);
                bmsData.VolWav = value;
                return;
            }

            m = DefTotal.Match(def);
            if (m.Success)
            {
                var value = double.Parse(m.Groups[1].Value);
                bmsData.Total = value;
                return;
            }

            m = DefWav.Match(def);
            if (m.Success)
            {
                var key = (int)Base36.Decode(m.Groups[1].Value);
                var value = m.Groups[2].Value;
                bmsData.Wav.Add(key, value);
                return;
            }

            m = DefBmp.Match(def);
            if (m.Success)
            {
                var key = (int)Base36.Decode(m.Groups[1].Value);
                var value = m.Groups[2].Value;
                bmsData.Bmp.Add(key, value);
                return;
            }

            m = DefObj.Match(def);
            if (m.Success)
            {
                var measure = int.Parse(m.Groups[1].Value);
                var channel = int.Parse(m.Groups[2].Value);
                var rawData = m.Groups[3].Value;
                var obj = this.parseObject(measure, channel, rawData);
                if (obj == null) return;
                bmsData.Objects.Add(obj);
                return;
            }
        }

        IObject parseObject(int measure, int channel, string rawData)
        {
            // バックコーラス
            if (channel == 1)
            {
                return this.parseKeyObject(measure, channel, rawData);
            }
            // 小節の短縮
            if (channel == 2)
            {
                return this.parseBeatObject(measure, channel, rawData);
            }

            // BPM値の途中変更
            if (channel == 3)
            {
                return this.parseBPMObject(measure, channel, rawData);
            }

            // BGA
            if (channel == 4)
            {
                return this.parseKeyObject(measure, channel, rawData);
            }

            // Extended Object(For BM98 YANEURAO ver.)
            if (channel == 5)
            {
                return null;
            }

            // Poorアニメーションの変更
            if (channel == 6)
            {
                return this.parseKeyObject(measure, channel, rawData);
            }

            // BGA Layer
            if (channel == 7)
            {
                return this.parseKeyObject(measure, channel, rawData);
            }

            // 拡張BPM
            if (channel == 8)
            {
                // TODO: 未実装
                return null;
            }

            // STOP
            if (channel == 9)
            {
                // TODO: 未実装
                return null;
            }

            // 予約
            if (channel == 10)
            {
                return null;
            }

            // 1Pの可視オブジェクト定義
            if (11 <= channel && channel <= 19)
            {
                return this.parseKeyObject(measure, channel, rawData);
            }

            // 予約
            if (channel == 20)
            {
                return null;
            }

            // 2Pの可視オブジェクト定義
            if (21 <= channel && channel <= 29)
            {
                return this.parseKeyObject(measure, channel, rawData);
            }

            // 予約
            if (channel == 30)
            {
                return null;
            }

            // 1Pの不可視オブジェクト定義
            if (31 <= channel && channel <= 39)
            {
                return this.parseKeyObject(measure, channel, rawData);
            }

            // 予約
            if (channel == 40)
            {
                return null;
            }

            // 2Pの不可視オブジェクト定義
            if (41 <= channel && channel <= 49)
            {
                return this.parseKeyObject(measure, channel, rawData);
            }

            // 予約
            if (channel == 50)
            {
                return null;
            }

            // 1Pロングノート(フリーズアロー)定義
            if (51 <= channel && channel <= 59)
            {
                return this.parseKeyObject(measure, channel, rawData);
            }

            // 予約
            if (channel == 60)
            {
                return null;
            }

            // 2Pロングノート(フリーズアロー)定義
            if (61 <= channel && channel <= 69)
            {
                return this.parseKeyObject(measure, channel, rawData);
            }

            return null;
        }
        KeyObject parseKeyObject(int measure, int channel, string rawData)
        {
            var data = new int[rawData.Length / 2];
            for (var i = 0; i < rawData.Length / 2; i++)
            {
                var chunkedData = rawData.Substring(i * 2, 2);
                data[i] = (int)Base36.Decode(chunkedData);
            }

            return new KeyObject(measure, channel, data);
        }

        BeatObject parseBeatObject(int measure, int channel, string rawData)
        {
            var data = double.Parse(rawData);
            return new BeatObject(measure, channel, data);
        }

        BPMObject parseBPMObject(int measure, int channel, string rawData)
        {
            var data = int.Parse(rawData);
            return new BPMObject(measure, channel, data);
        }
    }
}