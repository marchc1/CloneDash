using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Models
{
	public class FCurve<T> : IFCurve<T>
	{
		public List<Keyframe<T>> Keyframes { get; set; } = [];

		public IEnumerable<Keyframe<T>> GetKeyframes() {
			Recompute();

			foreach (var keyframe in Keyframes)
				yield return keyframe;
		}

		[JsonIgnore] private bool valid = false;

		public Keyframe<T> First {
			get {
				Recompute();
				return Keyframes.FirstOrDefault();
			}
		}
		public Keyframe<T> Last {
			get {
				Recompute();
				return Keyframes.LastOrDefault();
			}
		}

		public void AddKeyframe(Keyframe<T> keyframe) {
			Keyframes.RemoveAll(x => keyframe.Time == x.Time);
			Keyframes.Add(keyframe);
			valid = false;
		}

		public void RemoveKeyframe(Keyframe<T> keyframe) {
			Keyframes.Remove(keyframe);
			valid = false;
		}

		public bool TryFindKeyframe(Predicate<Keyframe<T>> predicate, [NotNullWhen(true)] out Keyframe<T>? keyframe) {
			Recompute();

			keyframe = null;
			foreach (var kf in Keyframes) {
				if (predicate(kf)) {
					keyframe = kf;
					break;
				}
			}

			return keyframe != null;
		}

		public bool TryFindKeyframe(double time, [NotNullWhen(true)] out Keyframe<T>? keyframe)
			=> TryFindKeyframe(x => x.Time == time, out keyframe);

		public void Recompute() {
			if (valid == false) {
				Keyframes.Sort((x, y) => x.Time.CompareTo(y.Time));
				valid = true;
			}
		}

		[JsonIgnore] public bool Invalid => !valid;

		public T? DetermineValueAtTime(double time, KeyframeInterpolation? interpolationOverride = null) {
			Recompute();

			int count = Keyframes.Count;
			switch (count) {
				case 0: return default;
				case 1: return Keyframes[0].Value;
				case 2: return Keyframe<T>.DetermineValue(time, Keyframes[0], Keyframes[1], interpolationOverride);
				default:
					var firstKeyframe = Keyframes[0];
					if (time <= firstKeyframe.Time)
						return firstKeyframe.Value;

					for (int i = 1; i < Keyframes.Count; i++) {
						var keyframe = Keyframes[i];
						if (time < keyframe.Time) {
							return Keyframe<T>.DetermineValue(time, Keyframes[i - 1], keyframe, interpolationOverride);
						}
					}

					return Keyframes[count - 1].Value;
			}
		}
	}
}
