using Nucleus.Models;

namespace Nucleus.ModelEditor;

/// <summary>
/// Moves, duplicates, pastes and deletes keys on a set of timelines.
/// Call <see cref="Finish"/> after changing keys.
/// </summary>
public class KeyEdit
{
	public class Key
	{
		public required EditorTimeline Timeline { get; init; }
		public required IFCurve[] Curves { get; init; }
		public required IKeyframe[] Keyframes { get; init; }
		public required double OriginalTime { get; init; }
		internal int Order;

		public IKeyframe Primary => Keyframes[0];
		public double Time => Keyframes[0].GetTime();
	}

	/// <summary>
	/// Copied keys, with times relative to the first one
	/// </summary>
	public class Clip
	{
		internal readonly List<(EditorTimeline Timeline, IKeyframe[] Keyframes, Segment?[] Segments, double Offset, float[]? Deform)> Keys = [];
		public int Count => Keys.Count;
		public IEnumerable<EditorTimeline> Timelines {
			get {
				HashSet<EditorTimeline> seen = [];
				List<EditorTimeline> timelines = [];
				foreach (var key in Keys) {
					if (seen.Add(key.Timeline))
						timelines.Add(key.Timeline);
				}
				return timelines;
			}
		}
	}

	// Handles relative to the segment, or plain offsets when both keys have the same value
	// Abs values are the plain offsets, used once a key's value is edited
	internal record struct Segment(double RightTime, double RightValue, double LeftTime, double LeftValue, bool Flat, double RightAbs = 0, double LeftAbs = 0);

	public const double TIME_EPSILON = FCurve<float>.KEYFRAME_TIME_EPSILON;

	public EditorAnimation Animation { get; }
	public IReadOnlyList<Key> Keys => keys;

	readonly List<Key> keys = [];
	readonly Dictionary<IKeyframe, Key> byPrimary = [];
	readonly Dictionary<IKeyframe, Segment> segments = [];
	readonly Dictionary<IFCurve, DeformTimeline?> curves = [];
	readonly HashSet<IFCurve> touched = [];
	readonly HashSet<IKeyframe> valued = [];
	int order;

	public KeyEdit(EditorAnimation animation, IEnumerable<EditorTimeline> timelines, bool track = true) {
		Animation = animation;
		if (track)
			ModelEditor.Active.Actions.TrackAnimation(animation);

		foreach (var timeline in timelines.Distinct()) {
			var all = timeline.KeyCurves;
			int count = all[0].Count;
			List<IFCurve> usedList = [];
			foreach (var curve in all) {
				if (curve.Count == count)
					usedList.Add(curve);
			}
			var used = usedList.ToArray();
			foreach (var curve in used) {
				curves[curve] = timeline as DeformTimeline;
				for (int i = 0; i + 1 < count; i++) {
					if (curve.KeyAt(i) is Keyframe<float> key && curve.KeyAt(i + 1) is Keyframe<float> next)
						segments[key] = GetSegment(key, next);
				}
			}

			for (int i = 0; i < count; i++) {
				var keyframes = new IKeyframe[used.Length];
				for (int j = 0; j < used.Length; j++)
					keyframes[j] = used[j].KeyAt(i);
				Add(new Key {
					Timeline = timeline,
					Curves = used,
					Keyframes = keyframes,
					OriginalTime = used[0].KeyAt(i).GetTime()
				});
			}
		}
	}

	private void Add(Key key) {
		key.Order = order++;
		keys.Add(key);
		byPrimary[key.Primary] = key;
	}

	public Key? Find(IKeyframe primary) => byPrimary.GetValueOrDefault(primary);

	public void SetTime(Key key, double time) {
		time = Math.Max(0, time);
		for (int i = 0; i < key.Curves.Length; i++) {
			key.Curves[i].SetKeyframeTime(key.Keyframes[i], time);
			touched.Add(key.Curves[i]);
		}
	}

	public void SetValue(Key key, int curve, float value) {
		if (key.Keyframes[curve] is not Keyframe<float> keyframe) return;
		float delta = value - keyframe.Value;
		keyframe.Value = value;
		if (keyframe.LeftHandle is KeyframeHandle<float> leftHandle)
			keyframe.LeftHandle = leftHandle with { Value = leftHandle.Value + delta };
		if (keyframe.RightHandle is KeyframeHandle<float> rightHandle)
			keyframe.RightHandle = rightHandle with { Value = rightHandle.Value + delta };
		valued.Add(keyframe);
		touched.Add(key.Curves[curve]);
	}

	public Key Duplicate(Key key) {
		var copies = new IKeyframe[key.Keyframes.Length];
		for (int i = 0; i < copies.Length; i++) {
			copies[i] = key.Curves[i].DuplicateKeyframe(key.Keyframes[i], key.Time);
			touched.Add(key.Curves[i]);
			if (segments.TryGetValue(key.Keyframes[i], out var segment))
				segments[copies[i]] = segment;
		}
		var copy = new Key {
			Timeline = key.Timeline,
			Curves = key.Curves,
			Keyframes = copies,
			OriginalTime = key.OriginalTime
		};
		Add(copy);
		return copy;
	}

	public void Remove(Key key) {
		for (int i = 0; i < key.Curves.Length; i++) {
			key.Curves[i].RemoveKeyframe(key.Keyframes[i]);
			touched.Add(key.Curves[i]);
		}
		keys.Remove(key);
		byPrimary.Remove(key.Primary);
	}

	public Clip Copy(IEnumerable<Key> copied) {
		var clip = new Clip();
		List<Key> source = [.. copied];
		var times = new double[source.Count];
		var indices = new int[source.Count];
		for (int i = 0; i < source.Count; i++) {
			times[i] = source[i].Time;
			indices[i] = i;
		}
		Array.Sort(indices, (left, right) => {
			int byTime = times[left].CompareTo(times[right]);
			return byTime != 0 ? byTime : left.CompareTo(right);
		});
		List<Key> list = new(source.Count);
		foreach (var index in indices)
			list.Add(source[index]);
		if (list.Count == 0) return clip;
		double first = list[0].Time;
		foreach (var key in list) {
			var frames = new IKeyframe[key.Keyframes.Length];
			var keySegments = new Segment?[frames.Length];
			for (int i = 0; i < frames.Length; i++) {
				frames[i] = key.Curves[i].DuplicateKeyframe(key.Keyframes[i], key.Time);
				key.Curves[i].RemoveKeyframe(frames[i]);
				keySegments[i] = segments.TryGetValue(key.Keyframes[i], out var segment) ? segment : null;
			}
			float[]? deform = null;
			if (key.Timeline is DeformTimeline deformTimeline && key.Primary is Keyframe<float> keyframe) {
				int index = (int)Math.Round(keyframe.Value);
				if (index >= 0 && index < deformTimeline.Offsets.Count)
					deform = (float[])deformTimeline.Offsets[index].Clone();
			}
			clip.Keys.Add((key.Timeline, frames, keySegments, key.Time - first, deform));
		}
		return clip;
	}

	/// <summary>
	/// Adds the clip's keys starting at <paramref name="time"/>.
	/// </summary>
	/// <param name="target">Timeline for each key, null skips it</param>
	public List<Key> Paste(Clip clip, double time, Func<EditorTimeline, EditorTimeline?> target) {
		List<Key> pasted = [];
		foreach (var entry in clip.Keys) {
			var timeline = target(entry.Timeline);
			if (timeline == null || timeline.GetType() != entry.Timeline.GetType()) continue;
			var used = timeline.KeyCurves;
			if (used.Length != entry.Keyframes.Length) continue;

			double keyTime = Math.Max(0, time + entry.Offset);
			var copies = new IKeyframe[used.Length];
			for (int i = 0; i < used.Length; i++) {
				copies[i] = used[i].DuplicateKeyframe(entry.Keyframes[i], keyTime);
				curves.TryAdd(used[i], timeline as DeformTimeline);
				touched.Add(used[i]);
				if (entry.Segments[i] is Segment segment)
					segments[copies[i]] = segment;
			}
			if (timeline is DeformTimeline deformTimeline && copies[0] is Keyframe<float> keyframe) {
				deformTimeline.Offsets.Add(entry.Deform ?? new float[deformTimeline.Offsets.Count > 0 ? deformTimeline.Offsets[0].Length : 0]);
				keyframe.Value = deformTimeline.Offsets.Count - 1;
			}
			var key = new Key { Timeline = timeline, Curves = used, Keyframes = copies, OriginalTime = keyTime };
			Add(key);
			pasted.Add(key);
		}
		return pasted;
	}

	/// <summary>
	/// Deletes keys sharing a time with one of <paramref name="moved"/>, except the moved keys themselves
	/// </summary>
	public void ResolveOverlaps(IEnumerable<Key> moved) {
		HashSet<Key> movedSet = [.. moved];
		Dictionary<EditorTimeline, List<Key>> byTimeline = [];
		List<List<Key>> groups = [];
		foreach (var key in keys) {
			if (!byTimeline.TryGetValue(key.Timeline, out var group)) {
				group = [];
				byTimeline[key.Timeline] = group;
				groups.Add(group);
			}
			group.Add(key);
		}
		foreach (var group in groups) {
			List<Key> sorted = [.. group];
			sorted.Sort((left, right) => {
				int byTime = left.Time.CompareTo(right.Time);
				if (byTime != 0) return byTime;
				int byMoved = movedSet.Contains(left).CompareTo(movedSet.Contains(right));
				if (byMoved != 0) return byMoved;
				return left.Order.CompareTo(right.Order);
			});
			for (int i = 0; i < sorted.Count;) {
				int end = i + 1;
				while (end < sorted.Count && sorted[end].Time - sorted[i].Time <= TIME_EPSILON)
					end++;
				bool anyMoved = false;
				for (int j = i; j < end; j++) {
					if (movedSet.Contains(sorted[j])) {
						anyMoved = true;
						break;
					}
				}
				// The last one sorts as moved and newest
				if (end - i > 1 && anyMoved) {
					for (int j = i; j < end - 1; j++)
						Remove(sorted[j]);
				}
				i = end;
			}
		}
		Finish();
	}

	public void Finish() {
		foreach (var curve in touched) {
			int count = curve.Count;
			if (count > 0)
				curve.KeyAt(0);

			if (curves.GetValueOrDefault(curve) is DeformTimeline deform && deform.Curve == curve)
				SyncDeform(deform);

			for (int i = 0; i + 1 < count; i++) {
				if (curve.KeyAt(i) is Keyframe<float> key && curve.KeyAt(i + 1) is Keyframe<float> next)
					Apply(key, next, segments.TryGetValue(key, out var segment) ? segment : null, valued.Contains(key) || valued.Contains(next));
			}
		}
		if (touched.Count == 0) return;
		ModelEditor.Active.File.NotifyAnimationEdited(Animation);
		ModelEditor.Active.QueueAnimationUpdate();
	}

	// Key k has the value k and picks Offsets[k]
	internal static void SyncDeform(DeformTimeline deform) {
		var old = deform.Offsets;
		List<float[]> offsets = [];
		HashSet<float[]> used = [];
		for (int i = 0; i < deform.Curve.Count; i++) {
			var keyframe = deform.Curve[i];
			int index = (int)Math.Round(keyframe.Value);
			float[] values;
			if (index >= 0 && index < old.Count)
				values = old[index];
			else
				values = new float[old.Count > 0 ? old[0].Length : 0];
			offsets.Add(used.Add(values) ? values : (float[])values.Clone());
			keyframe.Value = i;
		}
		deform.Offsets = offsets;
	}

	private static Segment GetSegment(Keyframe<float> key, Keyframe<float> next) {
		double dt = next.Time - key.Time;
		double valueDelta = next.Value - key.Value;
		bool flat = Math.Abs(valueDelta) < 1e-6;
		const double THIRD = 1d / 3;

		double rightTime = THIRD;
		double rightValue = flat ? 0 : THIRD;
		double leftTime = THIRD;
		double leftValue = flat ? 0 : THIRD;
		if (key.RightHandle is KeyframeHandle<float> rightHandle) {
			if (dt > 0)
				rightTime = (rightHandle.Time - key.Time) / dt;
			rightValue = flat ? rightHandle.Value - key.Value : (rightHandle.Value - key.Value) / valueDelta;
		}
		if (next.LeftHandle is KeyframeHandle<float> leftHandle) {
			if (dt > 0)
				leftTime = (next.Time - leftHandle.Time) / dt;
			leftValue = flat ? next.Value - leftHandle.Value : (next.Value - leftHandle.Value) / valueDelta;
		}
		return new(rightTime, rightValue, leftTime, leftValue, flat,
			(key.RightHandle?.Value ?? key.Value + valueDelta / 3) - key.Value,
			next.Value - (next.LeftHandle?.Value ?? next.Value - valueDelta / 3));
	}

	private static void Apply(Keyframe<float> key, Keyframe<float> next, Segment? segment, bool absolute) {
		double dt = next.Time - key.Time;
		double valueDelta = next.Value - key.Value;
		// a key that had nothing after it gets a straight curve
		if (segment == null) {
			absolute = false;
			segment = new Segment(1d / 3, 1d / 3, 1d / 3, 1d / 3, false);
		}
		var handles = segment.Value;
		double right;
		double left;
		if (absolute) {
			right = handles.RightAbs;
			left = handles.LeftAbs;
		}
		else if (handles.Flat) {
			right = handles.RightValue;
			left = handles.LeftValue;
		}
		else {
			right = handles.RightValue * valueDelta;
			left = handles.LeftValue * valueDelta;
		}

		if (key.RightHandle is KeyframeHandle<float> rightHandle)
			key.RightHandle = rightHandle with {
				Time = key.Time + handles.RightTime * dt,
				Value = (float)(key.Value + right)
			};
		if (next.LeftHandle is KeyframeHandle<float> leftHandle)
			next.LeftHandle = leftHandle with {
				Time = next.Time - handles.LeftTime * dt,
				Value = (float)(next.Value - left)
			};
	}
}
