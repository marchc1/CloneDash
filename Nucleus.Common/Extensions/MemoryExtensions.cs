namespace Nucleus.ManagedMemory;

public static class MemoryExtensions
{
	extension(ulong data)
	{
		public ulong Convert(DataUnit from, DataUnit to) {
			return (ulong)(((double)data * (double)from) / (double)to);
		}
		public string NiceBytes(DataUnit treatDataAs = DataUnit.Byte) {
			if (treatDataAs != DataUnit.Byte) data = Convert(data, treatDataAs, DataUnit.Byte);
			if (data < (ulong)DataUnit.Kilobyte) return $"{data:0.000}B";
			else if (data < (ulong)DataUnit.Megabyte) return $"{data / (double)DataUnit.Kilobyte:0.000}KB";
			else if (data < (ulong)DataUnit.Gigabyte) return $"{data / (double)DataUnit.Megabyte:0.000}MB";
			else return $"{data / (ulong)DataUnit.Gigabyte:0.000}GB";
		}
	}
	extension(IManagedMemoryUnit inf)
	{
		public string NiceBytes() => NiceBytes(inf.UsedBytes);
	}
}
