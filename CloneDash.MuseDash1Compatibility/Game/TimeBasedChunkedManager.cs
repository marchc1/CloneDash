using AssetStudio;
using CommunityToolkit.HighPerformance;
using System.Numerics;

namespace CloneDash.Game;

public class TimeBasedChunkedManager<T> where T : IDashChunkable
{
	public const int MIN_TEMP_BUFFER = 256;
	public const int CHUNK_INTERVAL = 6; // seconds, a chunk consists of entities in these intervals
	class Chunk
	{
		public readonly List<T> ItemsInThisChunk = [];
		public int Length => ItemsInThisChunk.Count;
		public void CopyTo(Span<T> items) => ItemsInThisChunk.CopyTo(items);
	}

	readonly HashSet<T> ItemHash = [];
	readonly List<T> Items = [];
	readonly List<Chunk> Chunks = [];
	bool Dirty = true;
	T[] TempBuffer = new T[MIN_TEMP_BUFFER];
	readonly HashSet<T> VisibleCheck = [];
	int VisibleCount;

	/// <summary>
	/// Does not clear the memory!
	/// </summary>
	Span<T> GetTempBuffer(int length) {
		if (length > TempBuffer.Length) 
			TempBuffer = new T[(int)BitOperations.RoundUpToPowerOf2((uint)length)];

		return TempBuffer.AsSpan()[..length];
	}

	public void Invalidate() {
		Dirty = true;
	}

	void Validate() {
		if (!Dirty)
			return;

		Chunks.Clear();
		if (Items.Count == 0) {
			Dirty = false;
			return;
		}

		Items.Sort(static (x, y) => x.GetChunkableTime().CompareTo(y.GetChunkableTime()));

		var lastItem = Items[^1];
		var numChunks = (int)Math.Ceiling((lastItem.GetChunkableTime() + lastItem.GetChunkablePostLength()) / CHUNK_INTERVAL);
		if (numChunks <= (int)Math.Floor(lastItem.GetChunkableTime() / CHUNK_INTERVAL))
			numChunks = (int)Math.Floor(lastItem.GetChunkableTime() / CHUNK_INTERVAL) + 1;
		for (int i = 0; i < numChunks; i++)
			Chunks.Add(new());

		var items = Items.AsSpan();
		for (int i = 0; i < items.Length; i++) {
			var item = items[i];
			var startChunk = (int)Math.Floor(item.GetChunkableTime() / CHUNK_INTERVAL);
			var endChunk = (int)Math.Ceiling((item.GetChunkableTime() + item.GetChunkablePostLength()) / CHUNK_INTERVAL);
			if (endChunk <= startChunk)
				endChunk = startChunk + 1;
			endChunk = Math.Min(endChunk, numChunks);
			for (int chunkIdx = startChunk; chunkIdx < endChunk; chunkIdx++)
				Chunks[chunkIdx].ItemsInThisChunk.Add(item);
		}

		Dirty = false;
	}

	public void Add(T item) {
		if (!ItemHash.Add(item))
			return;

		Items.Add(item);
		Invalidate();
	}

	public void Remove(T item) {
		if (!ItemHash.Remove(item))
			return;

		Items.Remove(item);
		Invalidate();
	}

	/// <summary>
	/// Returns all items registered in order of time
	/// </summary>
	/// <returns></returns>
	public Span<T> GetAll() {
		Validate();

		return Items.AsSpan();
	}

	public T? GetFirst() {
		Validate();
		if (Items.Count == 0) return default;
		return Items.AsSpan()[0];
	}

	public T? GetLast() {
		Validate();
		if (Items.Count == 0) return default;
		return Items.AsSpan()[^1];
	}

	/// <summary>
	/// Returns all visible items registered in order of time, given a curtime
	/// Uses chunking under the hood
	/// </summary>
	/// <param name="curtime"></param>
	/// <returns></returns>
	public void Rebuild(double curtime) {
		Validate();

		if (Chunks.Count == 0)
			return;

		int chunkIdx = (int)Math.Floor(curtime / CHUNK_INTERVAL);
		if (chunkIdx < -1 || chunkIdx >= Chunks.Count + 1) {
			VisibleCount = 0;
			return;
		}

		int start = Math.Clamp(chunkIdx - 1, 0, Chunks.Count - 1);
		int end = Math.Clamp(chunkIdx + 1, 0, Chunks.Count - 1);

		int totalLength = 0;
		for (int i = start; i <= end; i++)
			totalLength += Chunks[i].Length;

		VisibleCheck.Clear();
		var items = GetTempBuffer(totalLength);
		int offset = 0;
		for (int i = start; i <= end; i++) {
			var chunk = Chunks[i];
			for (int j = 0; j < chunk.ItemsInThisChunk.Count; j++) {
				var item = chunk.ItemsInThisChunk[j];
				if (VisibleCheck.Add(item) && item.IsChunkableNow())
					items[offset++] = item;
			}
		}
		items[..offset].Sort(VisibleSorter);
		VisibleCount = offset;
	}


	private static int VisibleSorter(T x, T y) {
		return x.GetChunkableSortIndex().CompareTo(y.GetChunkableSortIndex());
	}


	/// <summary>
	/// Make sure to call RebuildVisible on think!!!
	/// </summary>
	/// <returns></returns>
	public Span<T> GetLastVisible() {
		return TempBuffer.AsSpan()[..VisibleCount];
	}
}
