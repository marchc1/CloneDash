// should this be in Nucleus/UI?
// TODO: cleanup, kind of hastily made

using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace Nucleus.ModelEditor.UI;

// Some class 'typedefs' of sorts for switch-case reasons
public class ViewDividerHeader : Panel;
public class ViewDivider : Button;
public class ViewButtonSelector : Button;


public struct ViewMoveResult
{
	/// <summary>
	/// The View that is being moved.
	/// </summary>
	public View ViewTarget;
	/// <summary>
	/// If moving the view is impossible.
	/// </summary>
	public bool Failed;
	/// <summary>
	/// If false, will create a new sub-view tab within <see cref="DivisionTarget"/> at index <see cref="TabIndex"/>.
	/// In that case, <see cref="DivisionTarget"/> may be the same division that the <see cref="ViewTarget"/> is apart of (in the case of
	/// just moving a view from one tab position to another).
	/// <br/>
	/// <br/>
	/// If true, creates a sub-division off of <see cref="DivisionTarget"/> with <see cref="Direction"/>
	/// </summary>
	public bool CreatesNewViewDivision;
	/// <summary>
	/// This fields purpose depends on <see cref="CreatesNewViewDivision"/>
	/// </summary>
	public ViewDivision DivisionTarget;
	/// <summary>
	/// Only applicable when <see cref="CreatesNewViewDivision"/> == true.
	/// </summary>
	public Dock Direction;
	/// <summary>
	/// Only applicable when <see cref="CreatesNewViewDivision"/> == false.
	/// </summary>
	public int TabIndex;

	/// <summary>
	/// Determines the result of a view-dragging operation, separated into a static method for use during rendering + final drag release.
	/// </summary>
	/// <param name="target"></param>
	/// <param name="UI"></param>
	/// <returns>View move state</returns>
	public static ViewMoveResult CreateMoveResult(View target, UserInterface UI) {
		ViewMoveResult result = new ViewMoveResult();
		result.ViewTarget = target;

		switch (UI.Hovered) {
			case ViewButtonSelector buttonSelector:
				ViewDividerHeader header = buttonSelector.Parent as ViewDividerHeader ?? throw new Exception("Parent mismatch!");
				result.DivisionTarget = header.Parent as ViewDivision ?? throw new Exception("Parent mismatch!");
				// todo: tab index. Just move it to the furthest side for now

				var centerBtn = buttonSelector.GetGlobalPosition() + (buttonSelector.RenderBounds.Size / 2);
				var rightmost = UI.Level.FrameState.MouseState.MousePos.X > centerBtn.X;

				result.TabIndex = Math.Clamp(result.DivisionTarget.IndexOfButton(buttonSelector) + (rightmost ? 1 : 0), 0, result.DivisionTarget.Views.Count);

				break;
			case ViewDividerHeader divider:
				result.DivisionTarget = divider.Parent as ViewDivision ?? throw new Exception("Parent mismatch!");
				result.TabIndex = result.DivisionTarget.Views.Count;
				break;
			default:
				// Navigate through the parent chain until we find a ViewDivision (or just break out)
				Element? element = UI.Hovered;
				while (IValidatable.IsValid(element)) {
					if (element is ViewDivision viewDiv) {
						// If viewdivision == this viewdivision and we only have one view left,
						// moving the view to a sub-division of that view is going to cause the view
						// to just "black hole" itself, so this check prevents that
						if(viewDiv.Views.Count == 1 && viewDiv.Views.Contains(target)) {
							result.Failed = true;
							return result;
						}

						result.DivisionTarget = viewDiv;
						result.CreatesNewViewDivision = true;
						// Figure out direction
						if (viewDiv.ActiveView != null) {
							var center = viewDiv.ActiveView.GetGlobalPosition();
							center += viewDiv.ActiveView.RenderBounds.Size / 2;
							// TODO: finish implementing this
							var dir = (center - EngineCore.Level.FrameState.MouseState.MousePos).Normalize();
							var dotTB = Vector2F.Dot(dir, Vector2F.Up);
							var dotLR = Vector2F.Dot(dir, Vector2F.Right);

							bool bottom = dotTB < 0;
							bool right = dotLR < 0;

							bool weakLR = Math.Abs(dotLR) < 0.45f;

							if (!weakLR)
								result.Direction = right ? Dock.Right : Dock.Left;
							else
								result.Direction = bottom ? Dock.Bottom : Dock.Top;

						}
						result.CreatesNewViewDivision = true;

						return result;
					}

					element = element.Parent;
				}

				result.Failed = true;
				break;
		}

		return result;
	}
}


public class ViewSplit
{
	public ViewDivision Division { get; set; }
	public ViewDivider Divider { get; set; }
	public Dock Direction {
		get => Division.Dock;
		set => Division.Dock = value;
	}
	private float sizePercentage = 0.5f;
	public float SizePercentage {
		get => sizePercentage;
		set {
			sizePercentage = value;
			Division.RootDivision.InvalidateLayout();
			Division.RootDivision.InvalidateChildren(false, true);
		}
	}

	public ViewSplit(ViewDivision div, ViewDivider divider) {
		Division = div;
		Divider = divider;

		divider.MouseDragEvent += Divider_MouseDragEvent;
	}

	private void Divider_MouseDragEvent(Element self, FrameState state, Vector2F deltapos) {
		float delta = 0;
		bool horizontal = false;

		self = Division.ParentDivision;

		switch (Direction) {
			case Dock.Left:
				delta = deltapos.X / self.RenderBounds.W;
				horizontal = true;
				break;
			case Dock.Right:
				delta = deltapos.X / -self.RenderBounds.W;
				horizontal = true;
				break;
			case Dock.Top:
				delta = deltapos.Y / self.RenderBounds.H;
				horizontal = false;
				break;
			case Dock.Bottom:
				delta = deltapos.Y / -self.RenderBounds.H;
				horizontal = false;
				break;
		}

		SizePercentage = SizePercentage + delta;
	}
}

// I am not the biggest fan of this, but this is the easiest way to
// set up a post-renderer that takes care of itself when no longer needed...
public class DragRenderer : LogicalEntity {
	public View Dragging;

	public override void Initialize() {
		base.Initialize();
	}

	public override void PostRender(FrameState frameState) {
		base.PostRender(frameState);
		var hovered = frameState.HoveredUIElement;

		ViewMoveResult dividerAddingTo = ViewMoveResult.CreateMoveResult(Dragging, this.Level.UI);
		if (dividerAddingTo.Failed) return;

		if (dividerAddingTo.CreatesNewViewDivision) {
			var div = dividerAddingTo.DivisionTarget.ActiveView;
			Debug.Assert(div != null);

			var divpos = div.GetGlobalPosition();
			var divsize = div.RenderBounds.Size;

			var dzi = 3;
			var dzi2 = dzi - 1;
			switch (dividerAddingTo.Direction) {
				case Dock.Right:
					divpos.X += (divsize.W / dzi) * dzi2;
					goto case Dock.Left;
				case Dock.Bottom:
					divpos.Y += (divsize.H / dzi) * dzi2;
					goto case Dock.Top;
				case Dock.Left:
					divsize.W = divsize.W / dzi;
					break;
				case Dock.Top:
					divsize.H = divsize.H / dzi;
					break;
			}

			Graphics2D.SetDrawColor(225, 80, 10, 100);
			Graphics2D.DrawRectangle(divpos, divsize);
			Graphics2D.SetDrawColor(225, 80, 10, 255);
			Graphics2D.DrawRectangleOutline(divpos, divsize, 2);
		}
		else {
			var divider = dividerAddingTo.DivisionTarget;
			var header = divider.Header;
			var index = dividerAddingTo.TabIndex;

			Graphics2D.SetTexture(Level.Textures.LoadTextureFromFile("models/viewheadermovearrow.png"));
			Graphics2D.SetDrawColor(255, 255, 255);
			Vector2F pos;
			if (index == divider.Views.Count)
				pos = divider.GetButton(index - 1).GetGlobalPosition() + new Vector2F(divider.GetButton(index - 1).RenderBounds.W, 0);
			else
				pos = divider.GetButton(index).GetGlobalPosition();
			pos = pos - new Vector2F(16, 16);
			Graphics2D.DrawTexture(pos, new(32, 32));
		}
	}

	public override void Think(FrameState frameState) {
		if (!frameState.MouseHeld(MouseButton.Mouse1))
			this.Remove();
	}
}
public class ViewDivision : Panel
{
	public ViewPanel RootViewPanel { get; internal set; }
	public ViewDivision RootDivision { get; internal set; }
	public ViewDivision ParentDivision { get; internal set; }

	List<ViewSplit> splits = [];
	ViewDividerHeader header;
	List<ViewButtonSelector> buttons = [];

	public int IndexOfButton(ViewButtonSelector selector) => buttons.IndexOf(selector);
	public ViewButtonSelector GetButton(int index) => buttons[index];

	public List<View> Views = [];
	public int ActiveIndex = -1;

	public ViewDividerHeader Header => header;

	public void UpdateViewButtonHighlight() {
		for (int i = 0; i < buttons.Count; i++) {
			var active = i == ActiveIndex;
			ViewButtonSelector button = buttons[i];
			button.BackgroundColor = new(150, 160, 170, active ? 90 : 35);
		}
	}

	public void SetupViewButtons() {
		header.ClearChildren();
		header.DockPadding = RectangleF.Zero;
		buttons.Clear();
		foreach (var view in Views) {
			var switcher = header.Add<ViewButtonSelector>();
			switcher.Dock = Dock.Left;
			switcher.AutoSize = true;
			switcher.BorderSize = 0;
			switcher.TextPadding = new(12, 0);
			switcher.Text = view.Name;
			switcher.SetTag("view", view);
			switcher.MouseReleaseEvent += (_, _, _) => {
				SetActive(view);
			};
			Vector2F clickPos = Vector2F.Zero;
			bool dragging = false;
			Button? showDraggedTab = null;
			ViewDivider draggingFrom;
			switcher.MouseClickEvent += (self, state, btn) => {
				clickPos = state.MouseState.MousePos;
			};
			switcher.MouseDragEvent += (self, state, delta) => {
				if (showDraggedTab != null) {
					showDraggedTab.Position = state.MouseState.MousePos + new Vector2F(0, -14);
				}

				if (dragging) return;

				var distance = state.MouseState.MousePos.Distance(clickPos);
				if (distance > 8) {
					dragging = true;
					showDraggedTab = UI.Add<Button>();
					showDraggedTab.Origin = Anchor.Center;
					showDraggedTab.Position = state.MouseState.MousePos + new Vector2F(0, -16);
					showDraggedTab.OnHoverTest += Passthru;
					showDraggedTab.AutoSize = true;
					showDraggedTab.TextPadding = new(12);
					showDraggedTab.Text = view.Name;

					var renderer = Level.Add<DragRenderer>();
					renderer.Dragging = view;
				}
			};
			switcher.MouseReleasedOrLostEvent += (self, state, btn, lost) => {
				if (dragging) {
					Element? hoveredUI = state.HoveredUIElement;
					int index = 0;
					ViewMoveResult dividerAddingTo = ViewMoveResult.CreateMoveResult(view, UI);
					
					// TODO: Need a solution to black-holing, ie where moving a parent divider into a child or child-child divider etc
					// causes the removal of the parent divider, and hence the child itself is removed as well.
					if (!dividerAddingTo.Failed) {
						if (dividerAddingTo.CreatesNewViewDivision) {
							var split = dividerAddingTo.DivisionTarget.SplitApart(dividerAddingTo.Direction);
							split.SizePercentage = 1f / 3f;
							this.RemoveView(view);
							split.Division.AddView(view);
						}
						else {
							var divider = dividerAddingTo.DivisionTarget;
							var header = divider.Header;
							var setIndex = dividerAddingTo.TabIndex;

							this.RemoveView(view);
							divider.AddView(view, setIndex);
							divider.SetActive(view);
						}

						if (this.Views.Count <= 0) {
							this.ParentDivision.RemoveSplit(this);
						}
					}
				}

				showDraggedTab?.Remove();
				showDraggedTab = null;
				clickPos = Vector2F.Zero;
				dragging = false;
			};
			buttons.Add(switcher);
		}

		UpdateViewButtonHighlight();
	}

	public T AddView<T>() where T : View {
		T view = Add<T>();
		Views.Add(view);
		view.Dock = Dock.Fill;

		SetupViewButtons(); // should call this next-frame, but dont want to make dependant on layout validation... need to refactor here
		InvalidateLayout();
		InvalidateChildren(false, true);

		return view;
	}

	public T AddView<T>(T view) where T : View {
		Views.Add(view);
		view.Dock = Dock.Fill;

		SetupViewButtons(); // should call this next-frame, but dont want to make dependant on layout validation... need to refactor here
		InvalidateLayout();
		InvalidateChildren(false, true);

		return view;
	}

	public T AddView<T>(T view, int index) where T : View {
		Views.Insert(Math.Clamp(index, 0, Views.Count), view);
		view.Dock = Dock.Fill;

		SetupViewButtons(); // should call this next-frame, but dont want to make dependant on layout validation... need to refactor here
		InvalidateLayout();
		InvalidateChildren(false, true);

		return view;
	}

	public void RemoveView<T>(T view) where T : View {
		if (ActiveView == view)
			ActiveIndex = ActiveIndex - 1;

		Views.Remove(view);
		SetupViewButtons();
		InvalidateLayout();
		InvalidateChildren(false, true);
	}

	public void RemoveSplit(ViewDivision division) {
		foreach(var split in splits.Where(x => x.Division == division)) {
			split.Divider.Remove();
			// We need to remove the views from the children of the division,
			// or they'll get cleaned up here, which leads to views being unrecoverable
			split.Division.ClearChildrenNoRemove();
			split.Division.Remove();
		}
		splits.RemoveAll(x => x.Division == division);
		InvalidateLayout();
		InvalidateChildren(false, true);
	}

	public ViewDivider AddDivider() {
		ViewDivider divider = Add<ViewDivider>();
		divider.Size = new(8);
		divider.BorderSize = 0;
		divider.PaintOverride += (e, w, h) => {
			var c = MixColorBasedOnMouseState(e, e.ForegroundColor, new(0, 1, 1.5f, 1), new(0, 1, 0.5f, 1));
			Graphics2D.SetDrawColor(c);
			var offset = 2;
			if (e.Dock == Dock.Left || e.Dock == Dock.Right)
				Graphics2D.DrawLine(w / 2, offset, w / 2, h - (offset * 2));
			else if (e.Dock == Dock.Top || e.Dock == Dock.Bottom)
				Graphics2D.DrawLine(offset, h / 2, w - (offset * 2), h / 2);
		};
		divider.Text = "";
		return divider;
	}

	public ViewSplit SplitApart(Dock direction) {
		ViewDivision div;
		Add(out div);
		div.RootDivision = this.RootDivision;
		div.RootViewPanel = this.RootViewPanel;
		div.ParentDivision = this;
		ViewSplit split = new(div, AddDivider());
		split.Direction = direction;
		splits.Add(split);
		return split;
	}

	public bool ShowHeader {
		get => header.Visible;
		set {
			header.Visible = value;
			header.Enabled = value;
			InvalidateLayout();
		}
	}

	public IEnumerable<ViewSplit> Splits {
		get {
			foreach (var split in splits)
				yield return split;
		}
	}

	public bool IsSplit => splits.Count > 0;
	public View? ActiveView => (ActiveIndex < 0 || ActiveIndex >= Views.Count) ? null : Views[ActiveIndex];
	public bool SetActive(View view) {
		var index = Views.IndexOf(view);
		if (index != -1)
			ActiveIndex = index;
		else
			return false;

		foreach (var v in Views) {
			v.Visible = false;
			v.Enabled = false;
		}
		view.Visible = true;
		view.Enabled = true;
		UpdateViewButtonHighlight();
		InvalidateLayout();
		InvalidateChildren(false, true);
		return true;
	}

	protected override void Initialize() {
		RootDivision = this;

		base.Initialize();
		Add(out header);
		header.Dock = Dock.Top;
		header.Size = new(0, 28);
		DrawPanelBackground = false;
		header.DrawPanelBackground = false;
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);

		if (ActiveIndex < 0) {
			if (Views.Count > 0) {
				SetActive(Views[0]);
			}
			else return;
		}

		View? view = this.ActiveView;
		if (view == null) return;
		view.Visible = view.Enabled = true;

		view.SetParent(this);
		view.MoveToFront();
		view.Dock = Dock.Fill;
		if (splits.Count > 0) {
			splits.Reverse();
			foreach (var split in splits) {
				split.Divider.MoveToBack();
				split.Divider.Dock = split.Direction;

				split.Division.Size = new(width * split.SizePercentage, height * split.SizePercentage);
				split.Division.Dock = split.Direction;
				split.Division.MoveToBack();
			}
			splits.Reverse();
		}

		UpdateViewButtonHighlight();
		RevalidateLayout(); // otherwise SetParent/etc calls result in per-frame layout loop
	}
}

public class View : Panel
{
	public virtual string Name { get; set; } = "View";
	public virtual string? Icon { get; set; } = null;

	protected override void Initialize() {
		base.Initialize();
		Visible = false;
		BorderSize = 0;
	}
}

public class ViewPanel : Panel
{
	private Dictionary<string, ViewDivision> workspaceNames = [];
	private List<ViewDivision> workspaces = [];
	private ViewDivision? activeWorkspace;
	public ViewDivision? ActiveWorkspace {
		get => activeWorkspace;
		set {
			activeWorkspace = value;
			foreach (var workspace in workspaces) {
				workspace.Visible = workspace.Enabled = workspace == activeWorkspace;
			}
			InvalidateLayout();
			InvalidateChildren(false, true);
		}
	}

	public void SetActiveWorkspaceByName(string name) {
		if (!workspaceNames.TryGetValue(name, out var workspace))
			throw new KeyNotFoundException($"No workspace by the name of '{name}'");

		ActiveWorkspace = workspace;
		InvalidateLayout();
		InvalidateChildren(false, true);
	}

	protected override void Initialize() {
		DrawPanelBackground = true;
		DockPadding = RectangleF.Zero;
	}

	public ViewDivision AddWorkspace(string name) {
		bool empty = workspaces.Count <= 0;

		Add(out ViewDivision workspace);
		workspace.Dock = Dock.Fill;
		workspace.ShowHeader = false;
		workspace.RootViewPanel = this;
		workspace.Visible = workspace.Enabled = empty;

		workspaceNames.Add(name, workspace);
		workspaces.Add(workspace);

		return workspace;
	}

	public ViewDivision CopyWorkspace(string from, string newName) {
		if (!workspaceNames.TryGetValue(from, out var copyFrom))
			throw new KeyNotFoundException($"No workspace by the name of '{from}'");

		var newWorkspace = AddWorkspace(newName);
		CopyWorkspace(copyFrom, newWorkspace);
		return newWorkspace;
	}
	public void CopyWorkspace(ViewDivision from, ViewDivision to) {
		foreach (var view in from.Views)
			to.AddView(view);

		foreach (var splitFrom in from.Splits) {
			var splitTo = to.SplitApart(splitFrom.Direction);
			splitTo.SizePercentage = splitFrom.SizePercentage;
			CopyWorkspace(splitFrom.Division, splitTo.Division);
		}
	}

}