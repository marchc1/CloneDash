using System.Numerics;

namespace Nucleus.Types
{
    /// <summary>
    /// X is time; Y is value
    /// </summary>
    public struct Keyframe<T> where T : struct
    {
        public float Time;
        public T Value;

        public Keyframe(float time, T value) {
            Time = time;
            Value = value;
        }

        public override string ToString() {
            return $"Keyframe<{typeof(T).Name}> @ {Time} = {Value}";
        }

        private static Quaternion quaternionLinearInterpolation(Quaternion l, Quaternion r, double ratio) => Quaternion.Slerp(l, r, (float)ratio);
        private static Vector3 vector3LinearInterpolation(Vector3 l, Vector3 r, double ratio) => Vector3.Lerp(l, r, (float)ratio);

        public static T LinearInterpolation(List<Keyframe<T>> keyframes, double curtime) {
            Keyframe<T> L = new();
            Keyframe<T> R = new();

            for (int i = 0; i < keyframes.Count; i++) {
                if (keyframes[i].Time >= curtime) {
                    L = keyframes[i - 1];
                    R = keyframes[i];
                    break;
                }
            }

            var ratio = (float)NMath.Remap(curtime, L.Time, R.Time, 0, 1);
            switch (L) {
				case Keyframe<float> clv:
					Keyframe<float> crf = (Keyframe<float>)(object)R;
					return (NMath.Lerp(ratio, clv.Value, crf.Value) as T?).Value;
				case Keyframe<Vector3> clv:
					Keyframe<Vector3> crv = R;
					return (vector3LinearInterpolation(clv.Value, crv.Value, ratio) as T?).Value;
				case Keyframe<Quaternion> clq:
                    Keyframe<Quaternion> crq = R;
                    return (quaternionLinearInterpolation(clq.Value, crq.Value, ratio) as T?).Value;
                default:
                    throw new NotImplementedException($"No linear interpolation function for {typeof(T).Name}");
            }
        }

        public static implicit operator Keyframe<Vector3>(Keyframe<T> v) {
            return new Keyframe<Vector3>() {
                Time = v.Time,
                Value = (v.Value as Vector3?).Value
            };
        }
        public static implicit operator Keyframe<Quaternion>(Keyframe<T> v) {
            return new Keyframe<Quaternion>() {
                Time = v.Time,
                Value = (v.Value as Quaternion?).Value
            };
        }
    }
}
