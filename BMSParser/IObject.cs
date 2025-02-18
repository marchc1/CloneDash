// Credit: https://github.com/Mushus/bms-parser

namespace BMS
{
    public interface IObject
    {
        int Measure
        {
            get;
        }
        int Channel
        {
            get;
        }
    }
}