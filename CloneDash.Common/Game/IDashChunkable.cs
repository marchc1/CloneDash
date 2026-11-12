namespace CloneDash.Game;

public interface IDashChunkable {
	double GetChunkableTime();
	double GetChunkablePostLength();
	bool IsChunkableNow();
	int GetChunkableSortIndex();
}
