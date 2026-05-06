namespace CloneDash.Common.Data;

public struct TimeSignatureChange
{
	public int Beat;
	public float Percentage;

	public TimeSignatureChange(int beat, float percent) {
		Beat = beat;
		Percentage = percent;
	}
}
