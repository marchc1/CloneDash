using Nucleus.Common.Types;
using Nucleus.Models;

namespace Nucleus.ModelEditor.UI;

public enum RowKind { Overview, Group, Property }

/// <summary>
/// A row in the dopesheet or graph: the animation, a bone (or constraint), or one keyed property
/// </summary>
public class TimelineRow
{
	public RowKind Kind;
	public string Name = "";
	public string? Icon;
	public IEditorType? Owner;
	public EditorTimeline? Timeline;
	public IFCurve[] Curves = [];
	public KeyframeProperty? Property;
	public TimelineRow? Parent;
	public readonly List<TimelineRow> Children = [];
	/// <summary>
	/// Keys drawn on this row, merged on group and overview rows
	/// </summary>
	public readonly List<KeyMark> Marks = [];

	public int KeyCount => Curves.Length == 0 ? 0 : Curves[0].Count;
}

public struct KeyMark
{
	public double Time;
	public Color Color;
	public bool Selected;
	public int Count;
}

/// <summary>
/// Which rows the dopesheet and graph show, and the key operations they share
/// A key counts as selected when any of its keyframes is
/// </summary>
public class AnimationRows
{
	public static readonly Color MULTIPLE_KEY_COLOR = new(235, 235, 235);

	/// <summary>
	/// Copied keys, shared by the dopesheet and graph
	/// </summary>
	public static KeyEdit.Clip? Clipboard { get; private set; }

	readonly List<TimelineRow> all = [];
	readonly List<TimelineRow> shown = [];
	readonly HashSet<IEditorType> collapsed = [];
	readonly List<IEditorType> groupOrder = [];
	readonly List<IEditorType> lockedGroups = [];
	bool dirty = true;
	EditorAnimation? builtFor;
	int builtTimelines = -1;

	public bool Locked { get; private set; }
	/// <summary>
	/// Hides the event and draw order rows while unrelated items are selected.
	/// </summary>
	public bool HideOtherRowsOnSelection { get; init; } = true;
	public int Version { get; private set; }

	public IReadOnlyList<TimelineRow> Shown { get {
		Refresh();
		return shown;
	} }
	public IReadOnlyList<TimelineRow> All { get {
		Refresh();
		return all;
	} }

	public void MarkDirty() => dirty = true;

	public int IndexOf(TimelineRow row) {
		Refresh();
		return shown.IndexOf(row);
	}

	public void SetLocked(bool locked) {
		Locked = locked;
		lockedGroups.Clear();
		if (locked)
			lockedGroups.AddRange(GroupsFor(ModelEditor.Active.File.ActiveAnimation, false));
		dirty = true;
	}

	/// <summary>
	/// Shows the rows for the current selection, staying locked.
	/// </summary>
	public void RefreshLocked() {
		if (!Locked) return;
		lockedGroups.Clear();
		lockedGroups.AddRange(GroupsFor(ModelEditor.Active.File.ActiveAnimation, false));
		dirty = true;
	}

	public void SelectLockedBones() {
		if (!Locked || lockedGroups.Count == 0) return;
		ModelEditor.Active.UnselectAllObjects();
		ModelEditor.Active.SelectObjects([.. lockedGroups]);
	}

	public void ToggleCollapsed(IEditorType owner) {
		if (!collapsed.Remove(owner))
			collapsed.Add(owner);
		dirty = true;
	}

	public bool IsCollapsed(IEditorType owner) => collapsed.Contains(owner);

	public void MoveGroup(IEditorType owner, int toGroupIndex) {
		List<IEditorType> groups = [];
		foreach (var row in shown) {
			if (row.Kind == RowKind.Group)
				groups.Add(row.Owner!);
		}
		groups.Remove(owner);
		groups.Insert(Math.Clamp(toGroupIndex, 0, groups.Count), owner);
		groupOrder.RemoveAll(groups.Contains);
		groupOrder.InsertRange(0, groups);
		dirty = true;
	}

	private static IEditorType? GroupOf(IEditorType item) => item switch {
		EditorBone bone => bone,
		EditorSlot slot => slot.Bone,
		EditorAttachment attachment => attachment.Slot?.Bone,
		EditorConstraint constraint => constraint,
		_ => null
	};

	private static IEditorType? GroupOf(EditorTimeline timeline) => timeline switch {
		IBoneTimeline bone => bone.Bone,
		ISlotTimeline slot => slot.Slot.Bone,
		_ => ConstraintTimelines.ConstraintOf(timeline)
	};

	// Selected bones in selection order, or every keyed bone and constraint
	private List<IEditorType> GroupsFor(EditorAnimation? animation, bool useLock) {
		if (animation == null) return [];
		if (useLock && Locked) return [.. lockedGroups];

		List<IEditorType> groups = [];
		foreach (var selected in ModelEditor.Active.SelectedObjects) {
			if (GroupOf(selected) is IEditorType group && !groups.Contains(group))
				groups.Add(group);
		}
		if (groups.Count > 0) return groups;

		HashSet<IEditorType> keyed = [];
		foreach (var timeline in animation.Timelines) {
			if (GroupOf(timeline) is IEditorType group && HasKeys(timeline))
				keyed.Add(group);
		}
		foreach (var bone in animation.Model.GetAllBones()) {
			if (keyed.Contains(bone))
				groups.Add(bone);
		}
		List<EditorConstraint> constraints = [];
		foreach (var item in keyed) {
			if (item is not EditorConstraint constraint) continue;
			int at = constraints.Count;
			while (at > 0 && constraints[at - 1].Order > constraint.Order)
				at--;
			constraints.Insert(at, constraint);
		}
		groups.AddRange(constraints);
		return groups;
	}

	/// <summary>
	/// True when the rows come from the selection rather than every keyed bone
	/// </summary>
	public bool FollowingSelection => !Locked && ModelEditor.Active.SelectedObjects.Any(x => GroupOf(x) != null);

	private static bool HasKeys(EditorTimeline timeline) => timeline.KeyCurves[0].Count > 0;

	private static int Rank(EditorTimeline timeline) => timeline switch {
		RotationTimeline => 0,
		TranslateTimeline => 1, TranslateXTimeline => 2, TranslateYTimeline => 3,
		ScaleTimeline => 4, ScaleXTimeline => 5, ScaleYTimeline => 6,
		ShearTimeline => 7, ShearXTimeline => 8, ShearYTimeline => 9,
		ActiveAttachmentTimeline => 10,
		SlotColorTimeline => 11, SlotColorRGBTimeline => 11, SlotColorAlphaTimeline => 11, SlotTwoColorTimeline => 11,
		DeformTimeline => 12,
		_ => 20
	};

	public static (string Name, string Icon, KeyframeProperty? Property, IEditorType? Owner) Describe(
		EditorTimeline timeline) => timeline switch {
		RotationTimeline rotate =>
			("Rotate", "models/rotate_color.png", KeyframeProperty.Bone_Rotation, rotate.Bone),
		TranslateTimeline translate =>
			("Translate", "models/translate_color.png", KeyframeProperty.Bone_Translation, translate.Bone),
		TranslateXTimeline translateX =>
			("Translate X", "models/translate_color_x.png", KeyframeProperty.Bone_Translation, translateX.Bone),
		TranslateYTimeline translateY =>
			("Translate Y", "models/translate_color_y.png", KeyframeProperty.Bone_Translation, translateY.Bone),
		ScaleTimeline scale =>
			("Scale", "models/scale_color.png", KeyframeProperty.Bone_Scale, scale.Bone),
		ScaleXTimeline scaleX =>
			("Scale X", "models/scale_color_x.png", KeyframeProperty.Bone_Scale, scaleX.Bone),
		ScaleYTimeline scaleY =>
			("Scale Y", "models/scale_color_y.png", KeyframeProperty.Bone_Scale, scaleY.Bone),
		ShearTimeline shear =>
			("Shear", "models/shear_color.png", KeyframeProperty.Bone_Shear, shear.Bone),
		ShearXTimeline shearX =>
			("Shear X", "models/shear_color_x.png", KeyframeProperty.Bone_Shear, shearX.Bone),
		ShearYTimeline shearY =>
			("Shear Y", "models/shear_color_y.png", KeyframeProperty.Bone_Shear, shearY.Bone),
		ActiveAttachmentTimeline attachment =>
			($"Attachment: {attachment.Slot.Name}", "models/paperclip.png",
				KeyframeProperty.Slot_Attachment, attachment.Slot),
		SlotColorTimeline color =>
			($"Color: {color.Slot.Name}", "models/rgba.png", KeyframeProperty.Slot_Color, color.Slot),
		SlotColorRGBTimeline rgb =>
			($"RGB: {rgb.Slot.Name}", "models/rgba.png", KeyframeProperty.Slot_Color, rgb.Slot),
		SlotColorAlphaTimeline alpha =>
			($"Alpha: {alpha.Slot.Name}", "models/rgba.png", KeyframeProperty.Slot_Color, alpha.Slot),
		SlotTwoColorTimeline twoColor =>
			($"Color: {twoColor.Slot.Name}", "models/rgba.png", KeyframeProperty.Slot_Color, twoColor.Slot),
		DeformTimeline deform =>
			($"Deform: {deform.Attachment?.Name ?? deform.Slot.Name}", "models/deform.png", null, deform.Attachment),
		EventTimeline => ("Events", "models/event.png", null, null),
		DrawOrderTimeline => ("Draw Order", "models/draworder.png", null, null),
		_ when ConstraintTimelines.ConstraintOf(timeline) is EditorConstraint constraint =>
			(ConstraintTimelines.Label(timeline), constraint.EditorIcon, null, constraint),
		_ => (timeline.GetType().Name, "models/keyframe.png", null, null)
	};

	private static List<EditorTimeline> SortTimelines(List<EditorTimeline> timelines, EditorBone? bone) {
		List<EditorTimeline> sorted = [];
		List<int> slotKeys = [];
		List<int> rankKeys = [];
		foreach (var timeline in timelines) {
			int slotKey = timeline is ISlotTimeline slotTimeline && bone != null ? bone.Slots.IndexOf(slotTimeline.Slot) : -1;
			int rankKey = Rank(timeline);
			int at = sorted.Count;
			while (at > 0 && (slotKeys[at - 1] > slotKey || (slotKeys[at - 1] == slotKey && rankKeys[at - 1] > rankKey)))
				at--;
			sorted.Insert(at, timeline);
			slotKeys.Insert(at, slotKey);
			rankKeys.Insert(at, rankKey);
		}
		return sorted;
	}

	private static TimelineRow PropertyRow(EditorTimeline timeline, TimelineRow? parent) {
		var description = Describe(timeline);
		var row = new TimelineRow {
			Kind = RowKind.Property, Name = description.Name, Icon = description.Icon,
			Property = description.Property, Owner = description.Owner,
			Timeline = timeline, Curves = timeline.KeyCurves, Parent = parent
		};
		parent?.Children.Add(row);
		return row;
	}

	public void Refresh() {
		var animation = ModelEditor.Active.File.ActiveAnimation;
		if (!ModelEditor.Active.AnimationMode)
			animation = null;
		int timelineCount = animation?.Timelines.Count ?? -1;
		if (!dirty && animation == builtFor && timelineCount == builtTimelines) return;
		dirty = false;
		builtFor = animation;
		builtTimelines = timelineCount;
		Version++;
		all.Clear();
		shown.Clear();
		if (animation == null) return;

		Dictionary<IEditorType, List<EditorTimeline>> byGroup = [];
		List<EditorTimeline> other = [];
		foreach (var timeline in animation.Timelines) {
			if (!HasKeys(timeline)) continue;
			if (GroupOf(timeline) is IEditorType group) {
				if (!byGroup.TryGetValue(group, out var list))
					byGroup[group] = list = [];
				list.Add(timeline);
			}
			else
				other.Add(timeline);
		}

		var overview = new TimelineRow { Kind = RowKind.Overview, Name = animation.Name, Owner = animation };
		all.Add(overview);
		shown.Add(overview);

		var natural = GroupsFor(animation, true);
		List<IEditorType> groups = [];
		foreach (var group in groupOrder) {
			if (natural.Contains(group))
				groups.Add(group);
		}
		foreach (var group in natural) {
			if (!groupOrder.Contains(group))
				groups.Add(group);
		}
		bool anySelected = ModelEditor.Active.SelectedObjectsCount > 0;
		foreach (var owner in groups) {
			byGroup.TryGetValue(owner, out var timelines);
			var row = new TimelineRow {
				Kind = RowKind.Group, Owner = owner, Parent = overview,
				Name = owner.GetName(),
				Icon = owner is EditorConstraint constraint ? constraint.EditorIcon : "models/bone.png"
			};
			overview.Children.Add(row);
			all.Add(row);
			shown.Add(row);
			if (timelines == null) continue;

			var bone = owner as EditorBone;
			foreach (var timeline in SortTimelines(timelines, bone)) {
				var property = PropertyRow(timeline, row);
				all.Add(property);
				if (!collapsed.Contains(owner))
					shown.Add(property);
			}
		}

		// Events and draw order go at the bottom when nothing (or one of their items) is selected
		foreach (var timeline in other.OrderBy(t => t is DrawOrderTimeline ? 1 : 0)) {
			bool related = timeline is EventTimeline && ModelEditor.Active.SelectedObjects.Any(x => x is EditorEvent);
			if (HideOtherRowsOnSelection && anySelected && !related && !Locked) continue;
			var row = PropertyRow(timeline, overview);
			all.Add(row);
			shown.Add(row);
		}
	}

	// Keys

	public IEnumerable<TimelineRow> PropertiesUnder(TimelineRow row) {
		switch (row.Kind) {
			case RowKind.Property: yield return row; break;
			case RowKind.Group:
				foreach (var child in row.Children)
					yield return child;
				break;
			case RowKind.Overview:
				foreach (var propertyRow in all) {
					if (propertyRow.Kind == RowKind.Property)
						yield return propertyRow;
				}
				break;
		}
	}

	/// <summary>
	/// Every curve's keyframe for key <paramref name="index"/> of a property row
	/// </summary>
	public static IEnumerable<IKeyframe> KeyGroup(TimelineRow row, int index) {
		int count = row.KeyCount;
		foreach (var curve in row.Curves) {
			if (curve.Count == count)
				yield return curve.KeyAt(index);
		}
	}

	public static bool IsKeySelected(TimelineRow row, int index) {
		int count = row.KeyCount;
		foreach (var curve in row.Curves) {
			if (curve.Count == count && ModelEditor.Active.IsKeyframeSelected(curve.KeyAt(index))) return true;
		}
		return false;
	}

	/// <summary>
	/// The keyframes of every key under <paramref name="row"/> at <paramref name="time"/>
	/// </summary>
	public IEnumerable<IKeyframe> KeysAt(TimelineRow row, double time) {
		foreach (var property in PropertiesUnder(row)) {
			var curve = property.Curves[0];
			for (int i = 0; i < curve.Count; i++) {
				if (Math.Abs(curve.KeyAt(i).GetTime() - time) <= KeyEdit.TIME_EPSILON) {
					foreach (var key in KeyGroup(property, i))
						yield return key;
				}
			}
		}
	}

	public IEnumerable<IKeyframe> KeysOf(TimelineRow row) {
		foreach (var property in PropertiesUnder(row)) {
			for (int i = 0; i < property.KeyCount; i++) {
				foreach (var key in KeyGroup(property, i))
					yield return key;
			}
		}
	}

	/// <summary>
	/// Timelines of the active animation holding a selected key, shown or not.
	/// </summary>
	public List<EditorTimeline> SelectedTimelines() {
		List<EditorTimeline> result = [];
		if (animation == null || !ModelEditor.Active.KeyframesSelected) return result;
		foreach (var timeline in animation.Timelines) {
			foreach (var curve in timeline.KeyCurves) {
				bool any = false;
				for (int i = 0; i < curve.Count && !any; i++)
					any = ModelEditor.Active.IsKeyframeSelected(curve.KeyAt(i));
				if (any) {
					result.Add(timeline);
					break;
				}
			}
		}
		return result;
	}

	public static bool IsSelected(KeyEdit.Key key) => key.Keyframes.Any(ModelEditor.Active.IsKeyframeSelected);

	public static void Select(IEnumerable<KeyEdit.Key> keys, bool additive = false) {
		List<IKeyframe> keyframes = [];
		foreach (var key in keys)
			keyframes.AddRange(key.Keyframes);
		ModelEditor.Active.SelectKeyframes(keyframes, additive);
	}

	readonly List<(double Time, Color Color, bool Selected)> merge = [];

	public void ComputeMarks() {
		foreach (var row in all) {
			row.Marks.Clear();
			if (row.Kind != RowKind.Property) continue;
			var curve = row.Curves[0];
			var color = row.Timeline!.Color;
			for (int i = 0; i < curve.Count; i++)
				row.Marks.Add(new KeyMark {
					Time = curve.KeyAt(i).GetTime(), Color = color, Selected = IsKeySelected(row, i), Count = 1
				});
		}

		foreach (var row in all) {
			if (row.Kind == RowKind.Property) continue;
			merge.Clear();
			foreach (var property in PropertiesUnder(row)) {
				foreach (var mark in property.Marks)
					merge.Add((mark.Time, mark.Color, mark.Selected));
			}
			merge.Sort((a, b) => a.Time.CompareTo(b.Time));

			int minimum = row.Kind == RowKind.Group && !collapsed.Contains(row.Owner!) ? 2 : 1;
			for (int i = 0; i < merge.Count;) {
				int end = i + 1;
				while (end < merge.Count && merge[end].Time - merge[i].Time <= KeyEdit.TIME_EPSILON)
					end++;
				var color = merge[i].Color;
				bool mixed = false;
				bool selected = true;
				for (int j = i; j < end; j++) {
					if (merge[j].Color != color)
						mixed = true;
					selected &= merge[j].Selected;
				}
				if (end - i >= minimum)
					row.Marks.Add(new KeyMark {
						Time = merge[i].Time,
						Color = mixed ? MULTIPLE_KEY_COLOR : color,
						Selected = selected,
						Count = end - i
					});
				i = end;
			}
		}
	}

	// Key operations

	static EditorAnimation? animation => ModelEditor.Active.File.ActiveAnimation;

	public void DeleteSelected() {
		if (animation == null || !ModelEditor.Active.KeyframesSelected) return;
		var edit = new KeyEdit(animation, SelectedTimelines());
		List<KeyEdit.Key> selected = [];
		foreach (var key in edit.Keys) {
			if (IsSelected(key))
				selected.Add(key);
		}
		foreach (var key in selected)
			edit.Remove(key);
		edit.Finish();
		ModelEditor.Active.UnselectAllKeyframes();
		dirty = true;
	}

	public void CopySelected() {
		if (animation == null || !ModelEditor.Active.KeyframesSelected) return;
		var edit = new KeyEdit(animation, SelectedTimelines(), false);
		Clipboard = edit.Copy(edit.Keys.Where(IsSelected));
	}

	public void CutSelected() {
		CopySelected();
		DeleteSelected();
	}

	public void Paste() {
		var activeAnimation = animation;
		if (activeAnimation == null || Clipboard == null) return;

		Func<EditorTimeline, EditorTimeline?> target = source => {
			if (activeAnimation.Timelines.Contains(source)) return source;
			var owner = Describe(source).Owner;
			return activeAnimation.Timelines.FirstOrDefault(candidate =>
				candidate.GetType() == source.GetType() && Describe(candidate).Owner == owner);
		};

		List<EditorTimeline> timelines = [];
		foreach (var source in Clipboard.Timelines) {
			if (target(source) is EditorTimeline timeline)
				timelines.Add(timeline);
		}
		var edit = new KeyEdit(activeAnimation, timelines);
		var pasted = edit.Paste(Clipboard, ModelEditor.Active.File.Timeline.Frame, target);
		edit.ResolveOverlaps(pasted);
		Select(pasted);
		dirty = true;
	}

	/// <summary>
	/// Selects every key in the rows of the selected keys, or every shown key after that.
	/// </summary>
	public void SelectAllKeys() {
		List<IKeyframe> rowKeys = [];
		foreach (var row in Shown) {
			if (row.Kind != RowKind.Property) continue;
			bool anySelected = false;
			for (int i = 0; i < row.KeyCount; i++) {
				if (IsKeySelected(row, i)) {
					anySelected = true;
					break;
				}
			}
			if (anySelected)
				rowKeys.AddRange(KeysOf(row));
		}
		if (rowKeys.Count > 0 && !rowKeys.All(ModelEditor.Active.IsKeyframeSelected))
			ModelEditor.Active.SelectKeyframes(rowKeys);
		else
			ModelEditor.Active.SelectKeyframes(KeysOf(shown[0]).ToList());
	}

	// Selected float keyframes with the key after them
	private IEnumerable<(KeyframeRef<float> Key, KeyframeRef<float>? Next, KeyframeRef<float>? Previous)>
		SelectedFloatKeys() {
		foreach (var timeline in SelectedTimelines()) {
			foreach (var curve in timeline.KeyCurves) {
				for (int i = 0; i < curve.Count; i++) {
					if (curve.KeyAt(i) is KeyframeRef<float> key && ModelEditor.Active.IsKeyframeSelected(key))
						yield return (key, i + 1 < curve.Count ? curve.KeyAt(i + 1) as KeyframeRef<float> : null, i > 0 ? curve.KeyAt(i - 1) as KeyframeRef<float> : null);
				}
			}
		}
	}

	private void Edited() {
		ModelEditor.Active.File.NotifyAnimationEdited(animation!);
		ModelEditor.Active.QueueAnimationUpdate();
	}

	public void SetInterpolation(KeyframeInterpolation interpolation) {
		if (animation == null || !ModelEditor.Active.KeyframesSelected) return;
		ModelEditor.Active.Actions.TrackAnimation(animation);
		foreach (var item in SelectedFloatKeys().ToList()) {
			var key = item.Key;
			var next = item.Next;
			key.Interpolation = interpolation;
			if (interpolation != KeyframeInterpolation.Bezier || next == null) continue;
			double dt = next.Time - key.Time;
			double deltaValue = next.Value - key.Value;
			key.RightHandle = new() {
				HandleType = KeyframeHandleType.Aligned, Time = key.Time + dt * 0.25, Value = key.Value
			};
			next.LeftHandle = new() {
				HandleType = KeyframeHandleType.Aligned,
				Time = key.Time + dt * 0.75,
				Value = (float)(key.Value + deltaValue)
			};
		}
		Edited();
	}

	public enum HandlePreset { Automatic, Separate, Flat, Bounce, EaseOut, EaseIn }

	public void ApplyPreset(HandlePreset preset) {
		if (animation == null || !ModelEditor.Active.KeyframesSelected) return;
		ModelEditor.Active.Actions.TrackAnimation(animation);
		foreach (var item in SelectedFloatKeys().ToList()) {
			var key = item.Key;
			var next = item.Next;
			var previous = item.Previous;
			key.Interpolation = KeyframeInterpolation.Bezier;
			double outLength = next != null ? (next.Time - key.Time) / 3 : 0;
			double inLength = previous != null ? (key.Time - previous.Time) / 3 : 0;
			KeyframeHandle<float> left = key.LeftHandle ?? new() { Time = key.Time - inLength, Value = key.Value };
			KeyframeHandle<float> right = key.RightHandle ?? new() { Time = key.Time + outLength, Value = key.Value };

			switch (preset) {
				case HandlePreset.Automatic:
					left.HandleType = right.HandleType = KeyframeHandleType.AutoClamped;
					break;
				case HandlePreset.Separate:
					left.HandleType = right.HandleType = KeyframeHandleType.Free;
					break;
				case HandlePreset.Flat:
					left = left with {
						HandleType = KeyframeHandleType.Aligned, Time = key.Time - inLength, Value = key.Value
					};
					right = right with {
						HandleType = KeyframeHandleType.Aligned, Time = key.Time + outLength, Value = key.Value
					};
					break;
				case HandlePreset.Bounce:
					left = left with {
						HandleType = KeyframeHandleType.Free, Time = key.Time - inLength,
						Value = previous == null ? key.Value : (float)(key.Value + (previous.Value - key.Value) / 3)
					};
					right = right with {
						HandleType = KeyframeHandleType.Free, Time = key.Time + outLength,
						Value = next == null ? key.Value : (float)(key.Value + (next.Value - key.Value) / 3)
					};
					break;
				// slow near this key
				case HandlePreset.EaseOut:
					right = right with {
						HandleType = KeyframeHandleType.Free, Time = key.Time + outLength * 2, Value = key.Value
					};
					break;
				// slow near the next key
				case HandlePreset.EaseIn:
					if (next?.LeftHandle is KeyframeHandle<float> nextLeft) {
						next.LeftHandle = nextLeft with {
							HandleType = KeyframeHandleType.Free, Time = next.Time - outLength * 2, Value = next.Value
						};
					}
					right = right with {
						HandleType = KeyframeHandleType.Free, Time = key.Time + outLength, Value = key.Value
					};
					break;
			}
			key.LeftHandle = left;
			key.RightHandle = right;
		}
		Edited();
	}
}
