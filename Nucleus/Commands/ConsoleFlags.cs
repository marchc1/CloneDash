namespace Nucleus.Commands
{
	public enum ConsoleFlags : ulong
	{
		None = 0,
		Unregistered = 1 << 0,
		Saved = 1 << 7,
		DevelopmentOnly = 1 << 60
	}
}
