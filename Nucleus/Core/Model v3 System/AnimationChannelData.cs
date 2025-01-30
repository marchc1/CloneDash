using Nucleus.Types;

namespace Nucleus.Core
{
    public class AnimationChannelData<T> : IAnimationChannelData where T : struct
    {
        public List<Keyframe<T>> Keyframes { get; internal set; } = [];
        public int Target { get; set; }
        public AnimationTargetPath Path { get; set; }
        public AnimationInterpolation Interpolation { get; set; }
        public bool End(double curtime) => Keyframes.Last().Time <= curtime;

		public T Interpolate(double curtime) {
			switch (Interpolation) {
				case AnimationInterpolation.Constant: return ConstantInterpolation(curtime);
				case AnimationInterpolation.Linear: return LinearInterpolation(curtime);
				default: throw new NotSupportedException($"AnimationChannelData: Unsupported interpolation mode '{Interpolation}'");
			}
		}

		public T LinearInterpolation(double curtime) {
			if (End(curtime)) return Keyframes.Last().Value;
			return Keyframe<T>.LinearInterpolation(Keyframes, curtime);
		}

		public T ConstantInterpolation(double curtime) {
			if (End(curtime)) return Keyframes.Last().Value;
			return Keyframe<T>.ConstantInterpolation(Keyframes, curtime);
		}
	}
}
