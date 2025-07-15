namespace CloneDash.Compatibility.MuseDash;

public class MusicData
{
	public short objId;
	public decimal tick;
	public MusicConfigData? configData;
	public NoteConfigData? noteData;
	public bool isLongPressing;
	public int doubleIdx;
	public bool isDouble;
	public bool isLongPressEnd;
	public decimal longPressPTick;
	public int endIndex;
	public decimal dt;
	public bool isBossNote;
	public int longPressNum;
	public decimal showTick;

	public MusicConfigData GetConfigData() => configData ?? throw new NullReferenceException("Config data not set!");
	public NoteConfigData GetNoteData() => noteData ?? throw new NullReferenceException("Config data not set!");

	public bool isLongPressType => isLongPressStart || isLongPressing || isLongPressEnd;
	public bool isAir => GetNoteData().pathway == 1;
	public bool isMul => GetConfigData().length > 0m && GetNoteData().type == 8U;
	public bool isLongPressStart => GetConfigData().length > 0m && GetNoteData().type == 3U;
	public int longPressCount => (int)Math.Ceiling(GetConfigData().length / .1m);
}