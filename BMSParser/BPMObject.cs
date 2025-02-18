// Credit: https://github.com/Mushus/bms-parser

namespace BMS
{
    class BPMObject : Object<int>
    {
        public BPMObject(int measure, int channel, int data) : base(measure, channel, data) { }
    }
}