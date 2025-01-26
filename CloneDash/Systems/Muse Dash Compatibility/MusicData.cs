namespace CloneDash
{
	public static partial class MuseDashCompatibility
    {
		public class MusicData
        {
            public short objId;
            public decimal tick;
            public MusicConfigData configData;
            public NoteConfigData? noteData;
            public bool isLongPressing;
            public int doubleIdx;
            public bool isDouble;
            public bool isLongPressEnd;
            public decimal longPressPTick;
            public int endIndex;
            public decimal dt;
            public int longPressNum;
            public decimal showTick;
        }
    }
}
