namespace Nucleus.Models;
public interface IKeyframable<T> {
	void AddKeyframe(Keyframe<T> kf);
	void RemoveKeyframe(Keyframe<T> kf);
}