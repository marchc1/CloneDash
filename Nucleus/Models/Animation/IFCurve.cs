namespace Nucleus.Models;
public interface IFCurve<T> {
	void AddKeyframe(Keyframe<T> kf);
	void RemoveKeyframe(Keyframe<T> kf);
}