using AssetStudio;

using CloneDash.Compatibility.MuseDash;

using Nucleus;

using System.Collections.ObjectModel;

namespace CloneDash.Compatibility.CustomAlbums
{
	internal static class MusicDataManager
	{
		private static readonly List<MusicData> MusicDataList = new();

		static MusicDataManager() {
			// Add an empty music data to the list as a placeholder
			MusicDataList.Add(new MusicData());
		}

		public static ReadOnlyCollection<MusicData> Data => MusicDataList.AsReadOnly();

		public static void Add(MusicData data) {
			if (MusicDataList.Count >= short.MaxValue - 1) {
				Logger.Warning($"BMS is too large to load. Max MusicData allowed is {short.MaxValue}!");
				return;
			}

			MusicDataList.Add(data);
		}

		public static void Sort() {
			// Remove placeholder music data
			MusicDataList.RemoveAt(0);

			// Sort the list
			MusicDataList.Sort((l, r) => !(r.tick - r.dt - (l.tick - l.dt) > 0) ? 1 : -1);

			// Add the placeholder music data back
			MusicDataList.Insert(0, new MusicData());

			// Reapply object IDs and round tick to nearest 3 decimal places
			for (var i = 1; i < MusicDataList.Count; i++) {
				var musicData = MusicDataList[i];
				musicData.tick = decimal.Round(musicData.tick, 3);
				musicData.objId = (short)i;

				MusicDataList[i] = musicData;
			}

			Logs.Info("Sorted MusicData");
		}

		public static void Set(int index, MusicData data) {
			MusicDataList[index] = data;
		}

		public static void Clear() {
			// Clear the list
			MusicDataList.Clear();

			// Add an empty music data to the list as a placeholder
			MusicDataList.Add(new MusicData());
		}
	}
}