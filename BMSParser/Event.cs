// Credit: https://github.com/Mushus/bms-parser

namespace BMS
{
    public class Event
    {
        long millisecond;
        int channel;
        int reference;

        internal Event(long millisecond, int channel, int reference)
        {
            this.millisecond = millisecond;
            this.channel = channel;
            this.reference = reference;
        }

        public long Millisecond
        {
            get
            {
                return this.millisecond;
            }
        }
        public int Channel
        {
            get
            {
                return this.channel;
            }
        }
        public int Reference
        {
            get
            {
                return this.reference;
            }
        }
    }
}