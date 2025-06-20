using Newtonsoft.Json;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Models
{
	public class FCurve<T> : IFCurve
	{
		public List<Keyframe<T>> Keyframes { get; set; } = [];
		public int Count => Keyframes.Count;
		public IEnumerable<Keyframe<T>> GetKeyframes() {
			Recompute();

			foreach (var keyframe in Keyframes)
				yield return keyframe;
		}
		public void ScaleTime(double scale) {
			foreach (var kf in Keyframes) {
				kf.SetTime(kf.GetTime() * scale);
			}
			valid = false;
		}

		public FCurve<T> Copy(double scale = 1) {
			FCurve<T> copy = new FCurve<T>();
			foreach (var kf in GetKeyframes()) {
				copy.AddKeyframe(kf.Copy(scale));
			}
			copy.valid = false;
			return copy;
		}

		[JsonIgnore] private bool valid = false;

		public Keyframe<T>? First {
			get {
				Recompute();
				return Keyframes.FirstOrDefault();
			}
		}
		public Keyframe<T>? Last {
			get {
				Recompute();
				return Keyframes.LastOrDefault();
			}
		}

		public void SetKeyframeTime(IKeyframe keyframe, double time) {
#if DEBUG
			Debug.Assert(Keyframes.Contains(keyframe));
			keyframe.SetTime(time);
			valid = false;
#endif
		}

		public Keyframe<T> this[int index] {
			get {
				Recompute();
				return Keyframes[index];
			}
			set {
				valid = false;
				Keyframes[index] = value;
			}
		}

		public Keyframe<T> AddKeyframe(Keyframe<T> keyframe) {
			Keyframes.RemoveAll(x => keyframe.Time == x.Time);
			Keyframes.Add(keyframe);
			valid = false;
			return keyframe;
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

		public bool TryFindKeyframe(double time, [NotNullWhen(true)] out Keyframe<T>? keyframe) {
			Recompute();

			var search = BinarySearchKeyframe(time);
			if (search == -1) {
				keyframe = null;
				return false;
			}

			keyframe = Keyframes[search];
			return true;
		}

		public int BinarySearchKeyframe(double time) {
			int len = Keyframes.Count;
			if (len == 0) return -1;

#nullable disable
			if (time <= First.Time) return 0; // pre-first
			if (time >= Last.Time) return len - 2; // post-last
#nullable enable

			int start = 0;
			int end = len - 1;

			while (end - start > 1) {
				int middle = (start + end) / 2;
				var kf = Keyframes[middle];
				if (kf.Time == time) // exact
					return middle;
				else if (kf.Time > time) // collapse backwards
					end = middle;
				else // collapse forwards
					start = middle;
			}

			return start;
		}

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

					var start = BinarySearchKeyframe(time);
					return Keyframe<T>.DetermineValue(time, Keyframes[start], Keyframes[start + 1], interpolationOverride);
			}
		}
	}
}
