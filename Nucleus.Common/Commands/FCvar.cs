namespace Nucleus.Commands
{
	public enum FCvar : ulong
	{
		None = 0,
		Unregistered = 1 << 0,
		Saved = 1 << 7,
		NeverAsString = 12,
		DevelopmentOnly = 1 << 60
	}
}
