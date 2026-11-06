namespace CloneDash.Common.Game;

public interface IRhythmGameComponent
{
	void Load() { }
	void Reset();
	void SeekTo(double time) { }
}
