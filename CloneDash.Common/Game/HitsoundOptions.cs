namespace CloneDash.Common.Game;

[Flags]
public enum HitsoundOptions
{
	None = 0,
	AllowRepeat = 1 << 0,
	PlayWhileSeeking = 1 << 1
}
