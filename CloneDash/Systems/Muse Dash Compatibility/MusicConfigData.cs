namespace CloneDash
{
	public static partial class MuseDashCompatibility
    {
		public class MusicConfigData
        {
            public int id;
            public decimal time;
            public string note_uid;
            public decimal length;
            public bool blood;
            public int pathway;

			public MusicConfigData Copy() {
				return new MusicConfigData() {
					id = id,
					time = time,
					note_uid = note_uid,
					length = length,
					blood = blood,
					pathway = pathway
				};
			}
        }
    }
}
