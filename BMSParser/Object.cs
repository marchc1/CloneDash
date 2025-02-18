// Credit: https://github.com/Mushus/bms-parser

namespace BMS
{
    class Object<T> : IObject
    {
        public Object(int measure, int channel, T data)
        {
            this.measure = measure;
            this.channel = channel;
            this.data = data;
        }
        private int measure;
        private int channel;
        private T data;

        public int Measure
        {
            get
            {
                return this.measure;
            }
        }

        public int Channel
        {
            get
            {
                return this.channel;
            }
        }

        public T Data
        {
            get
            {
                return this.data;
            }
        }
    }
}