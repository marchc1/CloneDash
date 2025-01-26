using System.Text.Json.Serialization;

namespace CloneDash
{
	public static partial class MuseDashCompatibility
    {
		public class MuseDashAlbum
        {
            [JsonPropertyName("uid")] public string UID { get; set; } = "";
            [JsonPropertyName("title")] public string Title { get; set; } = "";
            [JsonPropertyName("tag")] public string Tag { get; set; } = "";
            [JsonPropertyName("jsonName")] public string JsonName { get; set; } = "";
            [JsonPropertyName("prefabsName")] public string PrefabsName { get; set; } = "";

            public List<MuseDashSong> Songs { get; set; } = [];

            public override string ToString() => $"{Title} [{Songs.Count} songs]";
        }
    }
}
