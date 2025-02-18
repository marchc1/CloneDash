// Credit: https://github.com/Mushus/bms-parser

namespace BMS
{
    class BeatObject : Object<double>
    {
        public BeatObject(int measure, int channel, double data) : base(measure, channel, data) { }
    }
}