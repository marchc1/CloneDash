namespace Nucleus.Debugging
{
	public struct DebugRecordState
	{
		public int MaxKeySize;
		public int MaxKeyPlusSpacingSize;
		public int MaxValueSize;
		public int LargestKeyIdx;
		public int LargestValueIdx;
		public static DebugRecordState Max(in DebugRecordState state1, in DebugRecordState state2) {
			return new() {
				MaxKeySize = Math.Max(state1.MaxKeySize, state2.MaxKeySize),
				MaxKeyPlusSpacingSize = Math.Max(state1.MaxKeyPlusSpacingSize, state2.MaxKeyPlusSpacingSize),
				MaxValueSize = Math.Max(state1.MaxValueSize, state2.MaxValueSize),
				LargestKeyIdx = Math.Max(state1.LargestKeyIdx, state2.LargestKeyIdx),
				LargestValueIdx = Math.Max(state1.LargestValueIdx, state2.LargestValueIdx),
			};
		}
	}
}