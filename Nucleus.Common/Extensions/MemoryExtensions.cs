namespace Nucleus.ManagedMemory;

public static class MemoryExtensions
{
	extension(ulong data)
	{
		public ulong Convert(DataUnit from, DataUnit to) {
			return (ulong)(((double)data * (double)from) / (double)to);
		}
		public string NiceBytes(DataUnit unit = DataUnit.Byte) {
			if (unit != DataUnit.Byte) data = Convert(data, unit, DataUnit.Byte);
			if (data < (ulong)DataUnit.Kilobyte) return $"{data:0.000}B";
			else if (data < (ulong)DataUnit.Megabyte) return $"{data / (double)DataUnit.Kilobyte:0.000}KB";
			else if (data < (ulong)DataUnit.Gigabyte) return $"{data / (double)DataUnit.Megabyte:0.000}MB";
			else return $"{data / (ulong)DataUnit.Gigabyte:0.000}GB";
		}
	}
	extension(IManagedMemory inf)
	{
		public string NiceBytes() => NiceBytes(inf.UsedBytes);
	}
}
