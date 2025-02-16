namespace Nucleus.ManagedMemory
{
	/// <summary>
	/// Represents the multiplier to convert a value of Data Unit into bits. 
	/// <br></br>
	/// So Bit == 1, Byte == 8, etc...
	/// </summary>
	public enum DataUnit : ulong{
        Bit = 1,
        Byte = 8,

        Kilobit = 1000,
        Kilobyte = 8000,

        Megabit = 1000000,
        Megabyte = 8000000,

        Gigabit = 1000000000,
        Gigabyte = 8000000000,
        
        b = Bit,
        B = Byte,
        Mb = Megabit,
        MB = Megabyte,
        Gb = Gigabit,
        GB = Gigabyte
    }
    public interface IManagedMemory : IValidatable, IDisposable
    {
        public ulong UsedBits { get; }
        public ulong UsedBytes => UsedBits / 8;

        public static ulong Convert(ulong data, DataUnit from, DataUnit to){
            return (ulong)(((double)data * (double)from) / (double)to);
        }
        public static string NiceBytes(ulong data, DataUnit unit = DataUnit.Byte){
            if(unit != DataUnit.Byte) data = Convert(data, unit, DataUnit.Byte);
            if(data < (ulong)DataUnit.Kilobyte) return $"{data:0.000}B";
            else if(data < (ulong)DataUnit.Megabyte) return $"{data / (double)DataUnit.Kilobyte:0.000}KB";
            else if(data < (ulong)DataUnit.Gigabyte) return $"{data / (double)DataUnit.Megabyte:0.000}MB";
            else return $"{data / (ulong)DataUnit.Gigabyte:0.000}GB";
        }
        public static string NiceBytes(IManagedMemory inf) => NiceBytes(inf.UsedBytes);
    }
}
