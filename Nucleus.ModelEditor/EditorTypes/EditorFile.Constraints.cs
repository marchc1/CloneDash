using Nucleus.Models;

namespace Nucleus.ModelEditor;

public partial class EditorFile
{
	// ============================================================================================== //
	// Constraints
	// ============================================================================================== //

	public delegate void ConstraintGeneric(EditorFile file, EditorConstraint constraint);
	public delegate void ConstraintRename(EditorFile file, EditorConstraint constraint, string oldName, string newName);

	/// <summary>
	/// Called when a constraint is created or comes back
	/// </summary>
	public event ConstraintGeneric? ConstraintAdded;
	public event ConstraintGeneric? ConstraintRemoved;
	public event ConstraintRename? ConstraintRenamed;
	public event ConstraintGeneric? ConstraintOrderChanged;

	public EditorResult RenameConstraint(EditorConstraint constraint, string newName) {
		if (constraint.IsNameTaken(newName))
			return new($"The model already has a constraint named '{newName}'.");

		var oldName = constraint.Name;
		constraint.Name = newName;
		ConstraintRenamed?.Invoke(this, constraint, oldName, newName);
		Actions.Push(("rename", constraint), $"Rename {oldName}",
			() => RenameConstraint(constraint, newName),
			() => RenameConstraint(constraint, oldName));
		return EditorResult.OK;
	}

	private static IList<EditorConstraint> ListOf(EditorConstraint constraint) => constraint switch {
		EditorIkConstraint => new TypedListView<EditorIkConstraint, EditorConstraint>(constraint.Model.IkConstraints),
		EditorTransformConstraint => new TypedListView<EditorTransformConstraint, EditorConstraint>(constraint.Model.TransformConstraints),
		EditorPathConstraint => new TypedListView<EditorPathConstraint, EditorConstraint>(constraint.Model.PathConstraints),
		_ => throw new NotSupportedException()
	};

	// Orders have to stay 0..n-1
	private static Dictionary<EditorConstraint, int> CaptureOrders(EditorModel model) => model.GetAllConstraints().ToDictionary(x => x, x => x.Order);
	private static void RestoreOrders(Dictionary<EditorConstraint, int> orders) {
		foreach (var pair in orders)
			pair.Key.Order = pair.Value;
	}
	private static void Renumber(EditorModel model) {
		int i = 0;
		foreach (var constraint in model.GetAllConstraints().ToList())
			constraint.Order = i++;
	}

	/// <summary>
	/// Name like "ik", "ik2", "ik3" that isn't taken in <paramref name="model"/>
	/// </summary>
	public static string UniqueConstraintName(EditorModel model, string name) {
		HashSet<string> taken = [];
		foreach (var constraint in model.GetAllConstraints())
			taken.Add(constraint.Name);
		if (!taken.Contains(name)) return name;
		for (int i = 2; ; i++) {
			if (!taken.Contains($"{name}{i}")) return $"{name}{i}";
		}
	}

	public EditorResult AddConstraint(EditorConstraint constraint) {
		var model = constraint.Model;
		var list = ListOf(constraint);
		constraint.Order = model.GetAllConstraints().Count();

		Action add = () => {
			list.Add(constraint);
			ConstraintAdded?.Invoke(this, constraint);
			ModelEditor.Active.QueueAnimationUpdate();
		};

		Action take = () => {
			if (constraint.Selected)
				ModelEditor.Active.UnselectObject(constraint);
			list.Remove(constraint);
			Renumber(model);
			ConstraintRemoved?.Invoke(this, constraint);
			ModelEditor.Active.QueueAnimationUpdate();
		};

		add();
		Actions.Push($"New {constraint.SingleName}", add, take);
		return EditorResult.OK;
	}

	public EditorResult RemoveConstraint(EditorConstraint constraint) {
		var model = constraint.Model;
		var list = ListOf(constraint);
		int index = list.IndexOf(constraint);
		if (index < 0) return new("Constraint isn't in its model.");

		var orders = CaptureOrders(model);
		List<(EditorSkin Skin, int Index)> skins = [];
		foreach (var skin in model.Skins) {
			if (skin.Constraints.Contains(constraint))
				skins.Add((skin, skin.Constraints.IndexOf(constraint)));
		}
		List<(EditorAnimation Animation, EditorTimeline Timeline, int Index)> timelines = [];
		foreach (var animation in model.Animations) {
			for (int i = 0; i < animation.Timelines.Count; i++) {
				if (ConstraintTimelines.ConstraintOf(animation.Timelines[i]) == constraint)
					timelines.Add((animation, animation.Timelines[i], i));
			}
		}

		Action take = () => {
			if (constraint.Selected)
				ModelEditor.Active.UnselectObject(constraint);
			list.Remove(constraint);
			foreach (var entry in skins)
				entry.Skin.Constraints.Remove(constraint);
			foreach (var item in timelines) {
				item.Animation.Timelines.Remove(item.Timeline);
				TimelineRemoved?.Invoke(item.Animation, item.Timeline);
			}
			Renumber(model);
			ConstraintRemoved?.Invoke(this, constraint);
		};

		Action restore = () => {
			list.Insert(Math.Min(index, list.Count), constraint);
			foreach (var entry in skins)
				entry.Skin.Constraints.Insert(Math.Min(entry.Index, entry.Skin.Constraints.Count), constraint);
			foreach (var item in timelines) {
				item.Animation.Timelines.Insert(Math.Min(item.Index, item.Animation.Timelines.Count), item.Timeline);
				TimelineCreated?.Invoke(item.Animation, item.Timeline);
			}
			RestoreOrders(orders);
			ConstraintAdded?.Invoke(this, constraint);
			ModelEditor.Active.QueueAnimationUpdate();
		};

		take();
		Actions.Push($"Delete {constraint.Name}", take, restore);
		return EditorResult.OK;
	}

	public EditorResult SetConstraintOrder(EditorConstraint constraint, int order) {
		var model = constraint.Model;
		var all = model.GetAllConstraints().ToList();
		order = Math.Clamp(order, 0, Math.Max(0, all.Count - 1));
		if (constraint.Order == order && all.IndexOf(constraint) == order) return EditorResult.OK;

		var before = CaptureOrders(model);
		all.Remove(constraint);
		all.Insert(order, constraint);
		for (int i = 0; i < all.Count; i++)
			all[i].Order = i;
		var after = CaptureOrders(model);
		ConstraintOrderChanged?.Invoke(this, constraint);

		Actions.Push(("order", constraint), $"Reorder {constraint.Name}",
			() => {
				RestoreOrders(after);
				ConstraintOrderChanged?.Invoke(this, constraint);
			},
			() => {
				RestoreOrders(before);
				ConstraintOrderChanged?.Invoke(this, constraint);
			});
		return EditorResult.OK;
	}

	// ============================================================================================== //
	// Events
	// ============================================================================================== //

	public delegate void EventGeneric(EditorFile file, EditorEvent editorEvent);
	public delegate void EventRename(EditorFile file, EditorEvent editorEvent, string oldName, string newName);

	/// <summary>
	/// Called when an event comes back from an undo
	/// </summary>
	public event EventGeneric? EventAdded;
	public event EventGeneric? EventRemoved;
	public event EventRename? EventRenamed;

	public EditorResult RenameEvent(EditorEvent editorEvent, string newName) {
		if (editorEvent.IsNameTaken(newName))
			return new($"The model already has an event named '{newName}'.");

		var oldName = editorEvent.Name;
		editorEvent.Name = newName;
		EventRenamed?.Invoke(this, editorEvent, oldName, newName);
		Actions.Push(("rename", editorEvent), $"Rename {oldName}",
			() => RenameEvent(editorEvent, newName),
			() => RenameEvent(editorEvent, oldName));
		return EditorResult.OK;
	}

	public EditorResult RemoveEvent(EditorEvent editorEvent) {
		var model = editorEvent.Model;
		int index = model.Events.IndexOf(editorEvent);
		if (index < 0) return new("Event isn't in its model.");

		var keys = new List<(EventTimeline Timeline, KeyframeRef<List<EditorEventKey>> Key, List<EditorEventKey> Before)>();
		foreach (var animation in model.Animations) {
			foreach (var timeline in animation.Timelines.OfType<EventTimeline>()) {
				foreach (var key in timeline.Keys.GetKeyframes()) {
					if (key.Value != null && key.Value.Any(x => x.Event == editorEvent))
						keys.Add((timeline, key, [.. key.Value]));
				}
			}
		}

		Action take = () => {
			if (editorEvent.Selected)
				ModelEditor.Active.UnselectObject(editorEvent);
			model.Events.Remove(editorEvent);
			foreach (var item in keys) {
				item.Key.Value!.RemoveAll(x => x.Event == editorEvent);
				if (item.Key.Value.Count == 0)
					item.Timeline.Keys.RemoveKeyframe(item.Key);
			}
			EventRemoved?.Invoke(this, editorEvent);
			foreach (var animation in model.Animations)
				NotifyAnimationEdited(animation);
		};

		Action restore = () => {
			model.Events.Insert(Math.Min(index, model.Events.Count), editorEvent);
			foreach (var item in keys) {
				item.Key.Value = [.. item.Before];
				if (!item.Timeline.Keys.Contains(item.Key))
					item.Timeline.Keys.RestoreKeyframe(item.Key);
			}
			EventAdded?.Invoke(this, editorEvent);
			foreach (var animation in model.Animations)
				NotifyAnimationEdited(animation);
		};

		take();
		Actions.Push($"Delete {editorEvent.Name}", take, restore);
		return EditorResult.OK;
	}
}

/// <summary>
/// A typed list seen as a list of its base type
/// </summary>
class TypedListView<TItem, TBase>(List<TItem> inner) : IList<TBase> where TItem : class, TBase
{
	public TBase this[int index] { get => inner[index]; set => inner[index] = (TItem)value!; }
	public int Count => inner.Count;
	public bool IsReadOnly => false;
	public void Add(TBase item) => inner.Add((TItem)item!);
	public void Clear() => inner.Clear();
	public bool Contains(TBase item) => item is TItem typed && inner.Contains(typed);
	public void CopyTo(TBase[] array, int arrayIndex) {
		for (int i = 0; i < inner.Count; i++)
			array[arrayIndex + i] = inner[i];
	}
	public IEnumerator<TBase> GetEnumerator() => inner.Cast<TBase>().GetEnumerator();
	public int IndexOf(TBase item) => item is TItem typed ? inner.IndexOf(typed) : -1;
	public void Insert(int index, TBase item) => inner.Insert(index, (TItem)item!);
	public bool Remove(TBase item) => item is TItem typed && inner.Remove(typed);
	public void RemoveAt(int index) => inner.RemoveAt(index);
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
