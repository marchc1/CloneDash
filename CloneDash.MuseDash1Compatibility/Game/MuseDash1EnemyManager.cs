using AssetStudio;
using CommunityToolkit.HighPerformance;
using System.Numerics;

namespace CloneDash.Game;

public class MuseDash1EnemyManager
{
	public const int MIN_TEMP_ENTITY_BUFFER = 256;
	public const int CHUNK_INTERVAL = 6; // seconds, a chunk consists of entities in these intervals
	class DashEnemyChunk
	{
		public readonly List<DashEnemy> EnemiesInThisChunk = [];
		public int Length => EnemiesInThisChunk.Count;
		public void CopyTo(Span<DashEnemy> enemies) => EnemiesInThisChunk.CopyTo(enemies);
	}

	readonly HashSet<DashEnemy> EnemyHash = [];
	readonly List<DashEnemy> Enemies = [];
	readonly List<DashEnemyChunk> Chunks = [];
	bool Dirty = true;
	DashEnemy[] TempEnemyBuffer = new DashEnemy[MIN_TEMP_ENTITY_BUFFER];
	readonly HashSet<DashEnemy> VisibleEnemiesCheck = [];
	int VisibleEnemiesCount;

	/// <summary>
	/// Does not clear the memory!
	/// </summary>
	Span<DashEnemy> GetTempEnemyBuffer(int length) {
		if (length > TempEnemyBuffer.Length) {
			TempEnemyBuffer = new DashEnemy[(int)BitOperations.RoundUpToPowerOf2((uint)length)];
		}

		return TempEnemyBuffer.AsSpan()[..length];
	}

	public void Invalidate() {
		Dirty = true;
	}

	void Validate() {
		if (!Dirty)
			return;

		Chunks.Clear();
		if (Enemies.Count == 0) {
			Dirty = false;
			return;
		}

		Enemies.Sort((x, y) => x.HitTime.CompareTo(y.HitTime));

		var lastEnemy = Enemies[^1];
		var numChunks = (int)Math.Ceiling((lastEnemy.HitTime + lastEnemy.Length) / CHUNK_INTERVAL);
		if (numChunks <= (int)Math.Floor(lastEnemy.HitTime / CHUNK_INTERVAL))
			numChunks = (int)Math.Floor(lastEnemy.HitTime / CHUNK_INTERVAL) + 1;
		for (int i = 0; i < numChunks; i++)
			Chunks.Add(new());

		var enemies = Enemies.AsSpan();
		for (int i = 0; i < enemies.Length; i++) {
			var enemy = enemies[i];
			var startChunk = (int)Math.Floor(enemy.HitTime / CHUNK_INTERVAL);
			var endChunk = (int)Math.Ceiling((enemy.HitTime + enemy.Length) / CHUNK_INTERVAL);
			if (endChunk <= startChunk)
				endChunk = startChunk + 1;
			endChunk = Math.Min(endChunk, numChunks);
			for (int chunkIdx = startChunk; chunkIdx < endChunk; chunkIdx++)
				Chunks[chunkIdx].EnemiesInThisChunk.Add(enemy);
		}

		Dirty = false;
	}

	public void AddEnemy(DashEnemy enemy) {
		if (!EnemyHash.Add(enemy))
			return;

		Enemies.Add(enemy);
		Invalidate();
	}

	public void RemoveEnemy(DashEnemy enemy) {
		if (!EnemyHash.Remove(enemy))
			return;

		Enemies.Remove(enemy);
		Invalidate();
	}

	/// <summary>
	/// Returns all enemies registered in order of time
	/// </summary>
	/// <returns></returns>
	public Span<DashEnemy> GetAllEnemies() {
		Validate();

		return Enemies.AsSpan();
	}

	public DashEnemy? GetFirstEnemy() {
		Validate();
		if (Enemies.Count == 0) return null;
		return Enemies.AsSpan()[0];
	}

	public DashEnemy? GetLastEnemy() {
		Validate();
		if (Enemies.Count == 0) return null;
		return Enemies.AsSpan()[^1];
	}

	/// <summary>
	/// Returns all visible enemies registered in order of time, given a curtime
	/// Uses chunking under the hood
	/// </summary>
	/// <param name="curtime"></param>
	/// <returns></returns>
	public void RebuildVisibleEnemies(double curtime) {
		Validate();

		int chunkIdx = (int)Math.Floor(curtime / CHUNK_INTERVAL);
		if (chunkIdx < -1 || chunkIdx >= Chunks.Count + 1) {
			VisibleEnemiesCount = 0;
			return;
		}

		int start = Math.Clamp(chunkIdx - 1, 0, Chunks.Count - 1);
		int end = Math.Clamp(chunkIdx + 1, 0, Chunks.Count - 1);

		int totalLength = 0;
		for (int i = start; i <= end; i++)
			totalLength += Chunks[i].Length;

		VisibleEnemiesCheck.Clear();
		var enemies = GetTempEnemyBuffer(totalLength);
		int offset = 0;
		for (int i = start; i <= end; i++) {
			var chunk = Chunks[i];
			for (int j = 0; j < chunk.EnemiesInThisChunk.Count; j++) {
				var enemy = chunk.EnemiesInThisChunk[j];
				if (VisibleEnemiesCheck.Add(enemy) && (enemy.CheckVisTest() || enemy.ForceDraw) && enemy.ShouldDraw)
					enemies[offset++] = enemy;
			}
		}
		enemies[..offset].Sort(VisibleEntitySorter);
		VisibleEnemiesCount = offset;
	}


	private static int VisibleEntitySorter(DashEnemy x, DashEnemy y) {
		return x.SortIndex.CompareTo(y.SortIndex);
	}


	/// <summary>
	/// Make sure to call RebuildVisibleEnemies on think!!!
	/// </summary>
	/// <returns></returns>
	public Span<DashEnemy> GetLastVisibleEnemies() {
		return TempEnemyBuffer.AsSpan()[..VisibleEnemiesCount];
	}
}
