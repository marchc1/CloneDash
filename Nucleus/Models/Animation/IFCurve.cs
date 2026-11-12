namespace Nucleus.Models;

public interface IFCurve
{
	public void SetKeyframeTime(IKeyframe keyframe, double newTime);

	public int Count { get; }
	public IKeyframe KeyAt(int index);
	public void RemoveKeyframe(IKeyframe keyframe);
	/// <summary>
	/// Adds a copy of <paramref name="keyframe"/> at <paramref name="time"/>, keeping keys already there.
	/// </summary>
	public IKeyframe DuplicateKeyframe(IKeyframe keyframe, double time);

	/// <summary>
	/// Copies the keyframes for <see cref="LoadKeyframes(object)"/>.
	/// The same keyframe objects get reused on load.
	/// </summary>
	public object SaveKeyframes();
	public void LoadKeyframes(object saved);
	public bool MatchesKeyframes(object saved);
}