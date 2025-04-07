// should this be in Nucleus/UI?
// TODO: cleanup, kind of hastily made

using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
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

	public static ViewMoveResult CreateMoveResult(View target, UserInterface UI) {
		ViewMoveResult result = new ViewMoveResult();
		result.ViewTarget = target;

		switch (UI.Hovered) {
			case ViewButtonSelector buttonSelector:
				ViewDividerHeader header = buttonSelector.Parent as ViewDividerHeader ?? throw new Exception("Parent mismatch!");
				result.DivisionTarget = header.Parent as ViewDivision ?? throw new Exception("Parent mismatch!");
				// todo: tab index. Just move it to the furthest side for now
				result.TabIndex = result.DivisionTarget.Views.Count;
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
						result.DivisionTarget = viewDiv;
						result.CreatesNewViewDivision = true;
						// Figure out direction
						if (viewDiv.ActiveView != null) {
							var center = viewDiv.ActiveView.GetGlobalPosition();
							center += viewDiv.ActiveView.RenderBounds.Size / 2;
							// TODO: finish implementing this
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
public class ViewDivision : Panel
{
	public ViewPanel RootViewPanel { get; internal set; }
	public ViewDivision RootDivision { get; internal set; }
	public ViewDivision ParentDivision { get; internal set; }

	List<ViewSplit> splits = [];
	ViewDividerHeader header;
	List<ViewButtonSelector> buttons = [];

	public List<View> Views = [];
	public int ActiveIndex = -1;

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
				}
			};
			switcher.MouseReleasedOrLostEvent += (self, state, btn, lost) => {
				if (dragging) {
					Element? hoveredUI = state.HoveredUIElement;
					int index = 0;
					ViewMoveResult dividerAddingTo = ViewMoveResult.CreateMoveResult(view, UI);
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

		return view;
	}

	public T AddView<T>(T view) where T : View {
		Views.Add(view);
		view.Dock = Dock.Fill;

		SetupViewButtons(); // should call this next-frame, but dont want to make dependant on layout validation... need to refactor here
		InvalidateLayout();

		return view;
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

		foreach (var v in Views)
			v.Visible = false;
		view.Visible = true;
		UpdateViewButtonHighlight();

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