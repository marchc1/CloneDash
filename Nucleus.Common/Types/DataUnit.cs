namespace Nucleus.ManagedMemory;

/// <summary>
/// Represents the multiplier to convert a value of Data Unit into bits. 
/// <br></br>
/// So Bit == 1, Byte == 8, etc...
/// </summary>
public enum DataUnit : ulong
{
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
