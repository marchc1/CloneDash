namespace CloneDash.Compatibility.MuseDash;

public class StageInfo
{
	public List<MusicData> musicDatas = new();
	public decimal delay;
	public string mapName;
	public string music;
	public string scene;
	public int difficulty;
	public string md5;
	public float bpm;
	public List<SceneEvent> sceneEvents = new();
	public Dictionary<string, List<GameDialogArgs>> dialogEvents = new();

	public SerializationData serializationData = new();
	public byte[] MusicStream;
}