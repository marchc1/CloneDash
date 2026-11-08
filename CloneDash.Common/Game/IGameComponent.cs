namespace CloneDash.Common.Game;

public interface IRhythmGameComponent
{
	void Prepare() { }
	void Load() { }
	void Reset();
	void SeekTo(double time) { }
}
