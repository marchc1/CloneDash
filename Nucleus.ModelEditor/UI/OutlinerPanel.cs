using Microsoft.VisualBasic;
using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Input;
using Nucleus.ModelEditor.UI;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.Util;
using Raylib_cs;
using static Nucleus.Util.Util;
using static System.Net.Mime.MediaTypeNames;

namespace Nucleus.ModelEditor
{
	public interface IContainsOutlinerNodes
	{
		public OutlinerNode AddNode(string text, string? icon = null);
	}
	public class OutlinerPanel : Panel, IContainsOutlinerNodes
	{
		class TopPanel(OutlinerPanel parent) : Panel(parent)
		{
			public override void Paint(float width, float height) {
				Graphics2D.SetDrawColor(255, 255, 255);

				Graphics2D.SetTexture(textures.LoadTextureFromFile("models/viseye.png"));
				Graphics2D.DrawTexturedRectangle(new Vector2F(4, 4), new(16));

				Graphics2D.SetTexture(textures.LoadTextureFromFile("models/keyframe.png"));
				Graphics2D.DrawTexturedRectangle(new Vector2F(4 + 23, 4), new(16));

				Graphics2D.SetTexture(textures.LoadTextureFromFile("models/tree.png"));
				Graphics2D.DrawTexturedRectangle(new Vector2F(4 + 46, 4), new(16));
				
				Graphics2D.SetDrawColor(parent.GetFgColor());
				Graphics2D.DrawLine(23, 0, 23, height);
				Graphics2D.DrawLine(46, 0, 46, height);
			}
		}
		TopPanel Top;
		ScrollPanel Right;
		Panel Header;
		Textbox Search;
		Button ShowSlotsBtn;
		Button ShowAttachmentsBtn;

		public string SearchText { get; private set; } = "";
		public bool ShowSlots { get; private set; } = true;
		public bool ShowAttachments { get; private set; } = true;

		public OutlinerPanel(Element parent) : base(parent) {
			BuildHeader();
			Top = new(this);
			Right = new(this);
			DockPadding = RectangleF.TLRB(0);
			BorderSize = 0;

			Top.
			Dock = Dock.Top;
			Top.			Size = new(24);
			Top.BorderSize = 0;
			Top.SetPaintBackgroundEnabled(false);

			Right.
			Dock = Dock.Fill;
			Right.BorderSize = 0;
			Right.			DockPadding = RectangleF.TLRB(0);
			Right.SetPaintBackgroundEnabled(false);

			SetAddParent(Right.GetAddParent());
			Right.			DockPadding = RectangleF.TLRB(0);
			Right.GetAddParent().			DockPadding = RectangleF.TLRB(0);
			Right.			DockMargin = RectangleF.TLRB(1);
			Right.GetAddParent().			Clipping = true;

			ModelEditor.Active.File.ModelAdded += File_ModelAdded;
			ModelEditor.Active.File.Cleared += File_Cleared;
			ModelEditor.Active.File.ModelsReordered += (_) => InvalidateLayout();

			ModelEditor.Active.File.ItemReparented += File_ItemReparented;
			ModelEditor.Active.File.OperatorActivated += File_OperatorActivated;
			ModelEditor.Active.File.OperatorDeactivated += File_OperatorDeactivated;
			ModelEditor.Active.SelectedChanged += () => {
				if (treeClick) return;
				lastClicked = null;
				if (AutoScroll)
					RevealSelected();
			};
		}

		private void BuildHeader() {
			Header = new Panel(this);
			Header.Dock = Dock.Top;
			Header.Size = new(66);
			Header.BorderSize = 0;
			Header.SetPaintBackgroundEnabled(false);
			Header.DockPadding = RectangleF.TLRB(4);

			var searchRow = new Panel(Header);
			searchRow.Dock = Dock.Top;
			searchRow.Size = new(28);
			searchRow.BorderSize = 0;
			searchRow.SetPaintBackgroundEnabled(false);

			var next = HeaderButton(searchRow, "models/jumpNext.png", "Next match", () => StepMatch(1));
			next.Dock = Dock.Right;
			var prev = HeaderButton(searchRow, "models/jumpPrevious.png", "Previous match", () => StepMatch(-1));
			prev.Dock = Dock.Right;

			Search = new Textbox(searchRow);
			Search.Dock = Dock.Fill;
			Search.DockMargin = RectangleF.TLRB(0, 0, 4, 0);
			Search.SetHelperText("Search");
			Search.OnTextChanged += (_, _, text) => {
				SearchText = text.Trim();
				InvalidateLayout();
			};
			Search.OnUserPressedEnter += (_, _, _) => StepMatch(1);

			var filterRow = new Panel(Header);
			filterRow.Dock = Dock.Top;
			filterRow.Size = new(28);
			filterRow.DockMargin = RectangleF.TLRB(4, 0, 0, 0);
			filterRow.BorderSize = 0;
			filterRow.SetPaintBackgroundEnabled(false);

			ShowSlotsBtn = HeaderButton(filterRow, "models/slot.png", "Show slots", () => {
				ShowSlots = !ShowSlots;
				ShowSlotsBtn.Pulsing = ShowSlots;
				InvalidateLayout();
			});
			ShowSlotsBtn.Dock = Dock.Left;
			ShowSlotsBtn.Pulsing = true;
			ShowAttachmentsBtn = HeaderButton(filterRow, "models/region.png", "Show attachments", () => {
				ShowAttachments = !ShowAttachments;
				ShowAttachmentsBtn.Pulsing = ShowAttachments;
				InvalidateLayout();
			});
			ShowAttachmentsBtn.Dock = Dock.Left;
			ShowAttachmentsBtn.Pulsing = true;

			var collapse = HeaderButton(filterRow, "models/collapsed.png", "Collapse the selected nodes (double-click: all)",
				() => {}, dbl => ExpandButton(false, dbl));
			collapse.Dock = Dock.Right;
			var expand = HeaderButton(filterRow, "models/expanded.png", "Expand the selected nodes (double-click: all)",
				() => {}, dbl => ExpandButton(true, dbl));
			expand.Dock = Dock.Right;
			AutoScrollBtn = HeaderButton(filterRow, "models/search.png", "Auto scroll (right-click: scroll to the selection)",
				() => {
					AutoScroll = !AutoScroll;
					AutoScrollBtn.Pulsing = AutoScroll;
				});
			AutoScrollBtn.Dock = Dock.Right;
			AutoScrollBtn.Pulsing = AutoScroll;
			AutoScrollBtn.OnButtonClick += (_, mouseButton) => {
				if (mouseButton == ButtonCode.Mouse2)
					RevealSelected();
			};
		}

		/// <summary>
		/// Expands and scrolls to items selected in the viewport.
		/// </summary>
		public bool AutoScroll { get; set; } = true;
		public Button AutoScrollBtn;

		// Selected nodes and their children, or everything when nothing is selected or on a double-click
		private void ExpandButton(bool expanded, bool all) {
			var selected = new List<OutlinerNode>();
			if (!all) {
				foreach (var selectedObject in ModelEditor.Active.SelectedObjects) {
					if (FindNode(RootNodes, selectedObject) is OutlinerNode selectedNode)
						selected.Add(selectedNode);
				}
			}
			if (selected.Count == 0)
				SetExpandedAll(expanded);
			else {
				foreach (var node in selected)
					node.SetBranchExpanded(expanded);
				InvalidateLayout();
			}
		}

		private Button HeaderButton(Element parent, string icon, string tip, Action click, Action<bool>? clickOrDouble = null) {
			var button = new Button(parent, "");
			button.Size = new(28);
			button.DockMargin = RectangleF.TLRB(0, 0, 4, 0);
			button.BorderSize = 0;
			button.TooltipText = tip;
			var iconImage = new ToolIcon(button, icon, false);
			iconImage.Dock = Dock.Fill;
			iconImage.DockMargin = RectangleF.TLRB(5);
			double last = -1;
			button.OnButtonClick += (_, mouseButton) => {
				if (mouseButton != ButtonCode.Mouse1) return;
				click();
				if (clickOrDouble == null) return;
				double now = EngineCore.Level.Realtime;
				bool doubleClick = last >= 0 && now - last < DOUBLE_CLICK_TIME;
				last = doubleClick ? -1 : now;
				clickOrDouble(doubleClick);
			};
			return button;
		}

		private void SetExpandedAll(bool expanded) {
			SetExpandedRecursive(RootNodes, expanded);
			InvalidateLayout();
		}

		private static void SetExpandedRecursive(List<OutlinerNode> nodes, bool expanded) {
			foreach (var node in nodes) {
				if (node.Children.Count > 0)
					node.Expanded = expanded;
				SetExpandedRecursive(node.Children, expanded);
			}
		}

		private bool Matches(OutlinerNode node) => SearchText.Length > 0 && node.Text.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

		private bool FilteredOut(OutlinerNode node) => node.GetRepresentingObject() switch {
			EditorSlot => !ShowSlots,
			EditorAttachment => !ShowAttachments,
			_ => false
		};

		private bool SubtreeMatches(OutlinerNode node) {
			if (FilteredOut(node)) return false;
			if (Matches(node)) return true;
			foreach (var child in node.Children) {
				if (SubtreeMatches(child)) return true;
			}
			return false;
		}

		private void StepMatch(int direction) {
			if (SearchText.Length == 0) return;
			List<OutlinerNode> found = [];
			FindMatches(RootNodes, found);
			if (found.Count == 0) return;

			var current = ModelEditor.Active.LastSelectedObject;
			int index = found.FindIndex(n => n.GetRepresentingObject() == current);
			if (index < 0)
				index = direction > 0 ? 0 : found.Count - 1;
			else
				index = (index + direction + found.Count) % found.Count;
			var target = found[index].GetRepresentingObject();
			if (target != null)
				ModelEditor.Active.SelectObject(target);
		}

		private void FindMatches(List<OutlinerNode> nodes, List<OutlinerNode> found) {
			foreach (var node in nodes) {
				if (FilteredOut(node)) continue;
				if (Matches(node))
					found.Add(node);
				FindMatches(node.GetChildNodesInOrder().ToList(), found);
			}
		}

		OutlinerNode? pendingReveal;
		int revealTries;

		private static OutlinerNode? FindNode(List<OutlinerNode> nodes, IEditorType target) {
			foreach (var node in nodes) {
				if (node.GetRepresentingObject() == target) return node;
				var found = FindNode(node.Children, target);
				if (found != null) return found;
			}
			return null;
		}

		public void RevealSelected() {
			var selected = ModelEditor.Active.LastSelectedObject;
			var node = selected == null ? null : FindNode(RootNodes, selected);
			if (node != null)
				Reveal(node);
		}

		public void Reveal(OutlinerNode node) {
			for (var parent = node.ParentNode; parent != null; parent = parent.ParentNode)
				parent.Expanded = true;

			pendingReveal = node;
			revealTries = 10;
			lastClicked = null;
			InvalidateLayout();
		}

		protected override void OnThink() {
			base.OnThink();
			// Rows under the mouse are different ones after a scroll.
			if (EngineCore.Level.FrameState.Mouse.MouseScroll.Y != 0)
				lastClicked = null;
			if (pendingReveal == null || pendingReveal.GetParent() != GetAddParent()) return;

			var node = pendingReveal.GetRenderBounds();
			var view = GetAddParent().GetRenderBounds();
			var scrollbar = Right.VerticalScrollbar;
			// Keep checking for a few frames, the rows and panel size settle late
			if (node.Y < scrollbar.Scroll || node.Y + node.H > scrollbar.Scroll + view.H)
				scrollbar.Scroll = node.Y + node.H / 2 - view.H / 2;
			if (--revealTries <= 0)
				pendingReveal = null;
		}

		private void File_ItemReparented(EditorFile file, IEditorType item) {
			IEditorType? parent = item switch {
				EditorBone bone => bone.Parent,
				EditorSlot slot => slot.Bone,
				EditorAttachment attachment => attachment.Slot,
				_ => null
			};
			var node = FindNode(RootNodes, item);
			var parentNode = parent == null ? null : FindNode(RootNodes, parent);
			if (node == null || parentNode == null) return;

			node.ParentNode = parentNode;
			node.SetVisible(parentNode.Expanded);
			InvalidateLayout();
		}

		/// <summary>
		/// The visible row at a window position, the pressed one while dragging
		/// </summary>
		public OutlinerNode? NodeAt(Vector2F windowPos) {
			var view = GetAddParent();
			var viewPos = view.GetGlobalPosition();
			var viewSize = view.GetRenderBounds().Size;
			if (windowPos.X < viewPos.X || windowPos.Y < viewPos.Y || windowPos.X > viewPos.X + viewSize.X || windowPos.Y > viewPos.Y + viewSize.Y)
				return null;

			foreach (var node in visibleNodes) {
				var pos = node.GetGlobalPosition();
				var size = node.GetRenderBounds().Size;
				if (windowPos.Y >= pos.Y && windowPos.Y < pos.Y + size.Y && windowPos.X >= pos.X && windowPos.X < pos.X + size.X)
					return node;
			}
			return null;
		}

		/// <summary>
		/// Node under the mouse a dragged node can drop onto
		/// </summary>
		public OutlinerNode? DropTarget(OutlinerNode dragged, Vector2F windowPos) {
			var item = dragged.GetRepresentingObject();
			if (item == null || (ModelEditor.Active.AnimationMode && item is not EditorAnimation)) return null;
			if (NodeAt(windowPos) is not OutlinerNode node || node == dragged) return null;
			var target = node.GetRepresentingObject();
			if (target == null) return null;
			bool canDrop = item is ModelImage image ? ModelEditor.Active.File.CanDropImage(image, target) : ModelEditor.Active.File.CanReparent(item, target);
			return canDrop ? node : null;
		}

		private void File_OperatorActivated(EditorFile self, UI.Operator op) {
			if (op.SelectableTypes == null) return;
			HashSet<Type> acceptableTypes = op.SelectableTypes.ToHashSet();
			foreach (var node in Right.GetAddParent().GetChildren()) {
				if (node is OutlinerNode outlinerNode) {
					IEditorType? obj = outlinerNode.GetRepresentingObject();

					// Default behavior: not selectable unless the operator explicitly said so
					// This includes null items
					if (obj == null) continue;

					outlinerNode.SelectableOverride = acceptableTypes.Contains(obj.GetType());
				}
			}
		}

		private void File_OperatorDeactivated(EditorFile self, UI.Operator op, bool canceled) {
			foreach (var node in Right.GetAddParent().GetChildren()) {
				if (node is OutlinerNode outlinerNode) {
					outlinerNode.SelectableOverride = null;
				}
			}
		}

		private void File_Cleared(EditorFile file) {
			GetAddParent().ClearChildren();
			RootNodes.Clear();
		}

		public delegate void OnNodeClicked(OutlinerPanel panel, OutlinerNode node, ButtonCode btn);
		public event OnNodeClicked? NodeClicked;
		public delegate void OnNodeDoubleClicked(OutlinerPanel panel, OutlinerNode node);
		public event OnNodeDoubleClicked? NodeDoubleClicked;

		public const double DOUBLE_CLICK_TIME = 0.4;
		/// <summary>
		/// Is a tree row click still waiting to become a double-click?
		/// </summary>
		public bool AwaitingDoubleClick => lastClicked != null && EngineCore.Level.Realtime - lastClickTime < DOUBLE_CLICK_TIME;
		private static readonly Common.Types.Color IMAGE_USED = new(110, 215, 110), IMAGE_UNUSED = new(255, 160, 60);
		OutlinerNode? lastClicked;
		double lastClickTime;
		bool treeClick;

		private static string AttachmentLabel(EditorAttachment attachment)
			=> attachment.Skin == null ? attachment.Name : $"{attachment.Name}  [{attachment.Skin.Name}]";

		private void RegisterAttachmentNode(OutlinerNode parentNode, EditorAttachment attachment) {
			OutlinerNode attachmentNode = parentNode.AddNode(AttachmentLabel(attachment), attachment.EditorIcon);
			attachmentNode.SetRepresentingObject(attachment);

			ModelEditor.Active.File.AttachmentRenamed += (_, attachmentR, _, newName) => {
				if (attachmentR == attachment) {
					attachmentNode.Text = AttachmentLabel(attachment);
				}
			};
			ModelEditor.Active.File.AttachmentSkinChanged += (_, attachmentR) => {
				if (attachmentR == attachment && IValidatable.IsValid(attachmentNode))
					attachmentNode.Text = AttachmentLabel(attachment);
			};
			ModelEditor.Active.File.SkinRenamed += (_, skin, _, _) => {
				if (attachment.Skin == skin && IValidatable.IsValid(attachmentNode))
					attachmentNode.Text = AttachmentLabel(attachment);
			};
			ModelEditor.Active.File.AttachmentRemoved += (_, _, attachmentR) => {
				if (attachmentR == attachment) {
					attachmentNode.Remove();
				}
			};
		}

		private void RegisterSlotEvents(OutlinerNode parentNode, EditorSlot slot) {
			OutlinerNode slotNode = parentNode.AddNode(slot.Name, "models/slot.png");
			slotNode.SetRepresentingObject(slot);

			ModelEditor.Active.File.SlotRenamed += (_, slotR, _, newName) => {
				if (slotR == slot) {
					slotNode.					Text = newName;
				}
			};
			ModelEditor.Active.File.SlotRemoved += (_, _, _, slotR) => {
				if (slotR == slot) {
					slotNode.Remove();
				}
			};
			ModelEditor.Active.File.AttachmentAdded += (_, slotR, attachment) => {
				if (slotR == slot && IValidatable.IsValid(slotNode))
					RegisterAttachmentNode(slotNode, attachment);
			};
		}

		private void SetupConstraintsNode(OutlinerNode modelNode, EditorModel model) {
			OutlinerNode constraintsNode = modelNode.AddNode("Constraints", "models/constraints.png");
			constraintsNode.Expanded = false;
			constraintsNode.ChangeChildOrder += (_, children) => children.Sort((x, y) =>
				(x.GetRepresentingObject<EditorConstraint>()?.Order ?? 0)
					.CompareTo(y.GetRepresentingObject<EditorConstraint>()?.Order ?? 0));

			foreach (var constraint in model.GetAllConstraints())
				SetupConstraintNode(constraintsNode, constraint);

			var file = ModelEditor.Active.File;
			file.ConstraintAdded += (_, constraint) => {
				if (constraint.Model == model && IValidatable.IsValid(constraintsNode))
					SetupConstraintNode(constraintsNode, constraint);
			};
			file.ConstraintOrderChanged += (_, constraint) => {
				if (constraint.Model == model && IValidatable.IsValid(constraintsNode))
					constraintsNode.InvalidateNode();
			};
		}

		private void SetupConstraintNode(OutlinerNode parentNode, EditorConstraint constraint) {
			OutlinerNode node = parentNode.AddNode(constraint.Name, constraint.EditorIcon);
			node.SetRepresentingObject(constraint);
			node.ImageColor = constraint is EditorPathConstraint ? constraint.DisplayColor : Common.Types.Color.White;

			var file = ModelEditor.Active.File;
			file.ConstraintRenamed += (_, constraintR, _, newName) => {
				if (constraintR == constraint && IValidatable.IsValid(node))
					node.Text = newName;
			};
			file.ConstraintRemoved += (_, constraintR) => {
				if (constraintR == constraint && IValidatable.IsValid(node))
					node.Remove();
			};
		}

		private void SetupEventsNode(OutlinerNode modelNode, EditorModel model) {
			OutlinerNode eventsNode = modelNode.AddNode("Events", "models/event.png");
			eventsNode.Expanded = false;
			AlphanumComparatorFast alphanum = new AlphanumComparatorFast();
			eventsNode.ChangeChildOrder += (_, children) => children.Sort((x, y) => alphanum.Compare(x.Text.ToString(), y.Text.ToString()));

			foreach (var editorEvent in model.Events)
				SetupEventNode(eventsNode, editorEvent);

			ModelEditor.Active.File.EventAdded += (_, editorEvent) => {
				if (editorEvent.Model == model && IValidatable.IsValid(eventsNode))
					SetupEventNode(eventsNode, editorEvent);
			};
		}

		private void SetupEventNode(OutlinerNode parentNode, EditorEvent editorEvent) {
			OutlinerNode node = parentNode.AddNode(editorEvent.Name, "models/event.png");
			node.SetRepresentingObject(editorEvent);

			var file = ModelEditor.Active.File;
			file.EventRenamed += (_, eventR, _, newName) => {
				if (eventR == editorEvent && IValidatable.IsValid(node))
					node.Text = newName;
			};
			file.EventRemoved += (_, eventR) => {
				if (eventR == editorEvent && IValidatable.IsValid(node))
					node.Remove();
			};
		}

		private void SetupSkinNode(OutlinerNode skinsNode, EditorModel model, EditorSkin skin) {
			OutlinerNode boneNode = skinsNode.AddNode(skin.Name, "models/skin.png");
			boneNode.SetRepresentingObject(skin);
			boneNode.			Text = skin.Name;

			// skin bones and constraints
			foreach (var bone in skin.Bones) {
				var node = boneNode.AddNode(bone.Name, "models/bone.png");
				node.SetRepresentingObject(bone);
				node.ImageColor = bone.Color;
			}
			foreach (var constraint in skin.Constraints) {
				var node = boneNode.AddNode(constraint.Name, constraint.EditorIcon);
				node.SetRepresentingObject(constraint);
				ModelEditor.Active.File.ConstraintRemoved += (_, constraintR) => {
					if (constraintR == constraint && IValidatable.IsValid(node))
						node.Remove();
				};
			}
			if (skin.Bones.Count + skin.Constraints.Count > 0)
				boneNode.Expanded = false;

			ModelEditor.Active.File.SkinRenamed += (file, skinR, oldName, newName) => {
				if (skinR == skin)
					boneNode.					Text = newName;
			};

			ModelEditor.Active.File.SkinRemoved += (file, model, skinR) => {
				if (skinR == skin && IValidatable.IsValid(boneNode)) {
					boneNode.Remove();
				}
			};
		}

		class SlotOutlinerNode(Element parent) : OutlinerNode(parent)
		{
			bool startDragging = false;
			Panel? dragPanel = null;
			public OutlinerNode drawOrder = null!;
			public Element slotNode = null!;
			internal EditorSlot slot;
			internal EditorModel model;

			protected override bool MouseDrag(Element self, FrameState fs, Vector2F delta) {
				startDragging = true;

				if (!IValidatable.IsValid(dragPanel)) {
					dragPanel = new Panel(UI);
					dragPanel.BorderSize = 0;
					dragPanel.SetBgColor(new Common.Types.Color(200, 200, 255));
					dragPanel.					Size = new(slotNode.GetRenderBounds().W, 2);
					dragPanel.SetPassthru(true);
				}
				var hovered = UI.GetHoveredElement();
				if (hovered is OutlinerNode node && node.ParentNode == drawOrder) {
					// Determine if placing above or below
					Vector2F mousePos = fs.Mouse.MousePos;
					Vector2F nodePos = node.GetGlobalPosition() + GetAddParent().RenderOffset;
					float height = node.GetRenderBounds().Height;
					bool below = mousePos.Y - nodePos.Y > (height / 2);

					dragPanel.SetVisible(true);
					dragPanel.					Position = below ? nodePos + new Vector2F(0, height) : nodePos;
				}
				else {
					dragPanel.SetVisible(false);
				}

				return base.MouseDrag(self, fs, delta);
			}
			protected override bool MouseRelease(Element self, FrameState fs, ButtonCode button) {
				startDragging = false;
				dragPanel?.Remove();

				var hovered = UI.GetHoveredElement();
				if (hovered is OutlinerNode node && node.ParentNode == drawOrder) {
					var otherslot = node.GetRepresentingObject<EditorSlot>();
					if (otherslot == null || otherslot == slot) return true;

					// Determine if placing above or below
					Vector2F mousePos = fs.Mouse.MousePos;
					Vector2F nodePos = node.GetGlobalPosition() + GetAddParent().RenderOffset;
					float height = node.GetRenderBounds().Height;
					bool below = mousePos.Y - nodePos.Y > (height / 2);

					// The list is front to back, above = drawn after
					var order = new List<EditorSlot>();
					foreach (var drawSlot in model.DrawOrder) {
						if (drawSlot != slot)
							order.Add(drawSlot);
					}
					int other = order.IndexOf(otherslot);
					ModelEditor.Active.File.SetSlotDrawOrder(slot, below ? other : other + 1);
				}

				return base.MouseRelease(self, fs, button);
			}
		}

		private void RegisterBoneEvents(OutlinerNode parentNode, EditorBone bone) {
			OutlinerNode boneNode = parentNode.AddNode(bone.Name, "models/bone.png");
			boneNode.SetRepresentingObject(bone);
			boneNode.ImageColor = bone.Color;

			ModelEditor.Active.File.BoneAdded += (file, model, boneA) => {
				if (boneA.Parent == bone && IValidatable.IsValid(boneNode)) {
					RegisterBoneEvents(boneNode, boneA);
				}
			};
			ModelEditor.Active.File.BoneRenamed += (file, boneR, oldName, newName) => {
				if (boneR == bone)
					boneNode.					Text = newName;
			};
			ModelEditor.Active.File.BoneRemoved += (file, model, boneR) => {
				if (boneR == bone && IValidatable.IsValid(boneNode)) {
					boneNode.Remove();
				}
			};
			ModelEditor.Active.File.SlotAdded += (file, model, _bone, slot) => {
				if (_bone == bone && IValidatable.IsValid(boneNode))
					RegisterSlotEvents(boneNode, slot);
			};
			ModelEditor.Active.File.BoneColorChanged += (file, _bone) => {
				if (_bone == bone)
					boneNode.ImageColor = bone.Color;
			};

			boneNode.ChangeChildOrder += (_, children) => {
				// The ZZZZZZ/AAAAAA is a quick hack to get slots to show before bones in the
				// child order. This was the best way I could think of to do it at the time
				children.Sort((x, y) => {
					string xName = "", yName = "";
					switch (x.GetRepresentingObject()) {
						case EditorBone bone: xName = $"ZZZZZZ_{bone.Name}"; break;
						case EditorSlot slot: xName = $"AAAAAA_{slot.Name}"; break;
					}
					switch (y.GetRepresentingObject()) {
						case EditorBone bone: yName = $"ZZZZZZ_{bone.Name}"; break;
						case EditorSlot slot: yName = $"AAAAAA_{slot.Name}"; break;
					}

					return xName.CompareTo(yName);
				});
			};
		}

		private void File_ModelAdded(EditorFile file, EditorModel model) {
			OutlinerNode modelNode = AddNode(model.Name, "models/model.png");
			modelNode.SetRepresentingObject(model);
			RegisterBoneEvents(modelNode, model.Root);

			OutlinerNode drawOrder = modelNode.AddNode("Draw Order", "models/draworder.png");
			drawOrder.KeyTarget = (model, KeyframeProperty.Model_DrawOrder);
			// follows the animated order in animate mode
			List<EditorSlot> shownOrder = [.. model.DrawOrder];
			drawOrder.Thinking += (_) => {
				var order = model.DrawOrder;
				if (order.SequenceEqual(shownOrder)) return;
				shownOrder = [.. order];
				drawOrder.InvalidateNode();
				InvalidateLayout();
			};
			ModelEditor.Active.File.DrawOrderChanged += (_, modelR) => {
				if (modelR == model && IValidatable.IsValid(drawOrder)) {
					drawOrder.InvalidateNode();
					InvalidateLayout();
				}
			};
			drawOrder.ChangeChildOrder += (_, children) => {
				Dictionary<EditorSlot, int> indexOf = [];
				var order = model.DrawOrder;
				for (int i = 0; i < order.Count; i++) {
					indexOf[order[i]] = i;
				}

				children.Sort((x, y) => {
					EditorSlot xS = x.GetRepresentingObject<EditorSlot>() ?? throw new Exception("wtf");
					EditorSlot yS = y.GetRepresentingObject<EditorSlot>() ?? throw new Exception("wtf");

					return indexOf[yS].CompareTo(indexOf[xS]);
				});
			};

			ModelEditor.Active.File.ModelRenamed += (file, modelR, oldName, newName) => {
				if (modelR == model) {
					modelNode.					Text = newName;
				}
			};
			ModelEditor.Active.File.ModelRemoved += (file, modelR) => {
				if (modelR == model) {
					modelNode.Remove();
				}
			};
			ModelEditor.Active.File.SlotAdded += (file, _model, _bone, slot) => {
				if (_model != model || !IValidatable.IsValid(modelNode)) return;

				SlotOutlinerNode slotNode = drawOrder.AddNode<SlotOutlinerNode>(slot.Name, p => new(p), "models/slot.png");
				slotNode.slot = slot;
				slotNode.model = model;
				slotNode.SetRepresentingObject(slot);
				ModelEditor.Active.File.SlotRenamed += (file, slotR, oldName, newName) => {
					if (slotR == slot) {
						slotNode.						Text = newName;
					}
				};
				ModelEditor.Active.File.SlotRemoved += (_, _, _, slotR) => {
					if (slotR == slot) {
						slotNode.Remove();
					}
				};
				slotNode.slotNode = slotNode;
				slotNode.drawOrder = drawOrder;
			};

			OutlinerNode skinsNode = modelNode.AddNode("Skins", "models/skins.png");
			skinsNode.SetRepresentingObject(model.Skins);
			ModelEditor.Active.File.SkinAdded += (file, _model, skin) => {
				if (_model != model || !IValidatable.IsValid(modelNode)) return;
				SetupSkinNode(skinsNode, _model, skin);
			};

			SetupConstraintsNode(modelNode, model);
			SetupEventsNode(modelNode, model);

			OutlinerNode animationsNode = modelNode.AddNode("Animations", "models/animation.png");
			animationsNode.SetRepresentingObject(model.Animations);
			// Folders are name prefixes, rebuild the whole list on any change
			Dictionary<string, AnimationFolder> folderObjects = [];
			Dictionary<string, bool> folderExpanded = [];
			var animFile = ModelEditor.Active.File;
			animFile.AnimationAdded += (_, _model, _) => {
				if (_model == model)
					RebuildAnimations(model, animationsNode, folderObjects, folderExpanded);
			};
			animFile.AnimationRemoved += (_, _model, _) => {
				if (_model == model)
					RebuildAnimations(model, animationsNode, folderObjects, folderExpanded);
			};
			animFile.AnimationRenamed += (_, animation, _, _) => {
				if (animation.Model == model)
					RebuildAnimations(model, animationsNode, folderObjects, folderExpanded);
			};
			animFile.AnimationFoldersChanged += (_model) => {
				if (_model == model)
					RebuildAnimations(model, animationsNode, folderObjects, folderExpanded);
			};
			RebuildAnimations(model, animationsNode, folderObjects, folderExpanded);

			OutlinerNode imagesNode = modelNode.AddNode("Images", "models/images.png");
			imagesNode.SetRepresentingObject(model.Images);
			imagesNode.ChangeChildOrder += SortImages;

			ModelEditor.Active.File.ModelImagesScanned += (file, _model) => {
				if (_model != model || !IValidatable.IsValid(modelNode)) return;
				foreach (var child in imagesNode.Children.ToArray())
					RemoveNodeTree(child);
				imagesNode.ClearChildNodes();

				Dictionary<string, OutlinerNode> folders = [];
				model.Images.Folders.Clear();

				foreach (var image in model.Images.Images) {
					int slash = image.Name.LastIndexOf('/');
					var parent = FolderFor(slash < 0 ? "" : image.Name.Substring(0, slash), model, imagesNode, folders);
					var imageNode = parent.AddNode(slash < 0 ? image.Name : image.Name.Substring(slash + 1),
						"models/image.png");
					imageNode.SetRepresentingObject(image);
					// Orange when no attachment uses it, green when one does
					imageNode.Thinking += (_) => imageNode.ImageColor = model.IsImageUsed(image.Name) ? IMAGE_USED : IMAGE_UNUSED;
				}
			};
		}

		private static readonly AlphanumComparatorFast imageNameComparer = new AlphanumComparatorFast();

		// folders first, then by name
		private static void SortImages(OutlinerNode _, List<OutlinerNode> children) => children.Sort((first, second) => {
			bool firstFolder = first.GetRepresentingObject() is ModelImageFolder;
			bool secondFolder = second.GetRepresentingObject() is ModelImageFolder;
			if (firstFolder != secondFolder) return firstFolder ? -1 : 1;
			return imageNameComparer.Compare(first.Text.ToString(), second.Text.ToString());
		});

		private static OutlinerNode FolderFor(string path, EditorModel model, OutlinerNode imagesNode, Dictionary<string, OutlinerNode> folders) {
			if (path.Length == 0) return imagesNode;
			if (folders.TryGetValue(path, out var existing)) return existing;
			int slash = path.LastIndexOf('/');
			var parent = FolderFor(slash < 0 ? "" : path.Substring(0, slash), model, imagesNode, folders);
			var node = parent.AddNode(slash < 0 ? path : path.Substring(slash + 1), "models/images.png");
			var folder = new ModelImageFolder() { Model = model, Path = path };
			model.Images.Folders.Add(folder);
			node.SetRepresentingObject(folder);
			node.ChangeChildOrder += SortImages;
			node.Expanded = false;
			folders[path] = node;
			return node;
		}

		private void RebuildAnimations(EditorModel model, OutlinerNode animationsNode, Dictionary<string, AnimationFolder> folderObjects, Dictionary<string, bool> folderExpanded) {
			if (!IValidatable.IsValid(animationsNode)) return;
			Remember(animationsNode, folderExpanded);
			foreach (var child in animationsNode.Children.ToArray())
				RemoveNodeTree(child);
			animationsNode.ClearChildNodes();

			Dictionary<string, OutlinerNode> folderNodes = [];
			var alphanumFolders = new AlphanumComparatorFast();
			var sortedPaths = ModelEditor.Active.File.AnimationFolderPaths(model)
				.OrderBy(x => x, Comparer<string>.Create((a, b) => alphanumFolders.Compare(a, b)));
			foreach (var path in sortedPaths)
				FolderNode(path, model, animationsNode, folderObjects, folderExpanded, folderNodes);
			foreach (var animation in model.Animations) {
				var parent = FolderNode(AnimationFolder.Parent(animation.Name), model, animationsNode, folderObjects, folderExpanded, folderNodes);
				var node = parent.AddNode(AnimationFolder.Leaf(animation.Name), "models/animation2.png");
				node.SetRepresentingObject(animation);
			}
			InvalidateLayout();
		}

		private static OutlinerNode FolderNode(string path, EditorModel model, OutlinerNode animationsNode,
			Dictionary<string, AnimationFolder> folderObjects, Dictionary<string, bool> folderExpanded,
			Dictionary<string, OutlinerNode> folderNodes) {
			if (path.Length == 0) return animationsNode;
			if (folderNodes.TryGetValue(path, out var existing)) return existing;
			var parent = FolderNode(AnimationFolder.Parent(path), model, animationsNode, folderObjects, folderExpanded, folderNodes);
			if (!folderObjects.TryGetValue(path, out var folder))
				folderObjects[path] = folder = new AnimationFolder() { Model = model, Path = path };
			var node = parent.AddNode(AnimationFolder.Leaf(path), "models/images.png");
			node.SetRepresentingObject(folder);
			node.Expanded = folderExpanded.GetValueOrDefault(path, true);
			return folderNodes[path] = node;
		}

		private static void Remember(OutlinerNode node, Dictionary<string, bool> folderExpanded) {
			foreach (var child in node.Children) {
				if (child.GetRepresentingObject() is AnimationFolder folder)
					folderExpanded[folder.Path] = child.Expanded;
				Remember(child, folderExpanded);
			}
		}










		private static void RemoveNodeTree(OutlinerNode node) {
			foreach (var child in node.Children.ToArray())
				RemoveNodeTree(child);
			node.Remove();
		}

		public OutlinerNode AddNode(string text, string? icon = null) {
			OutlinerNode node = SetupNode(this, 0, null, text, icon);
			RootNodes.Add(node);
			return node;
		}
		public List<OutlinerNode> RootNodes = [];
		private void CollectVisibleNodes(OutlinerNode node, List<OutlinerNode> into, int layer = 0) {
			bool searching = SearchText.Length > 0;
			if (FilteredOut(node) || (searching && !SubtreeMatches(node)))
				return;

			node.Layer = layer;
			into.Add(node);
			if (!searching && node.Expanded == false)
				return;

			foreach (var child in node.GetChildNodesInOrder())
				CollectVisibleNodes(child, into, layer + 1);
		}

		readonly List<OutlinerNode> visibleNodes = [];

		public List<OutlinerNode> NodesBetween(OutlinerNode first, OutlinerNode last) {
			int firstIndex = visibleNodes.IndexOf(first);
			int lastIndex = visibleNodes.IndexOf(last);
			if (firstIndex < 0 || lastIndex < 0) return [last];
			return visibleNodes.GetRange(Math.Min(firstIndex, lastIndex), Math.Abs(firstIndex - lastIndex) + 1);
		}

		public void Relayout() {
			GetAddParent().InvalidateChildren();
		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);

			visibleNodes.Clear();
			var models = ModelEditor.Active.File.Models;
			if (RootNodes.Count > 1)
				RootNodes.Sort((x, y) => models.IndexOf((x.GetRepresentingObject() as EditorModel)!).CompareTo(models.IndexOf((y.GetRepresentingObject() as EditorModel)!)));
			foreach (var node in RootNodes)
				CollectVisibleNodes(node, visibleNodes);

			// re-parenting dirties the layout again, only do it when the rows changed
			bool same;
			bool reordered = false;
			using (var current = GetAddParent().GetChildren()) {
				same = current.Length == visibleNodes.Count;
				for (int i = 0; same && i < visibleNodes.Count; i++)
					same = current[i] == visibleNodes[i];
				if (!same && current.Length == visibleNodes.Count) {
					var rows = new HashSet<Element>(current.Length);
					for (int i = 0; i < current.Length; i++)
						rows.Add(current[i]);
					reordered = visibleNodes.All(rows.Contains);
				}
			}
			if (same) return;

			// Same rows in a new order, like the draw order following an animation
			if (reordered) {
				var index = new Dictionary<Element, int>(visibleNodes.Count);
				for (int i = 0; i < visibleNodes.Count; i++)
					index[visibleNodes[i]] = i;
				SortChildren((x, y) => index[x].CompareTo(index[y]));
				InvalidateLayout();
				return;
			}

			ClearChildrenNoRemove();
			foreach (var node in visibleNodes) {
				node.SetParent(this);
				node.SetVisible(true);
			}
			Relayout();
		}
		public static OutlinerNode SetupNode(OutlinerPanel panel, int layer, OutlinerNode? parent = null, string text = "Node", string? icon = null) => SetupNode<OutlinerNode>(panel, layer, p => new(p), parent, text, icon);
		public static T SetupNode<T>(OutlinerPanel panel, int layer, Func<Element, T> factory, OutlinerNode? parent = null, string text = "Node", string? icon = null) where T : OutlinerNode {
			var node = factory(panel);

			node.Outliner = panel;
			node.Layer = layer;
			if (parent != null) {
				node.ParentNode = parent;
			}
			node.			Text = text;
			if (icon != null)
				node.ImageTexture = textures.LoadTextureFromFile(icon);

			node.OnButtonClick += (_, btn) => {
				// Right click anywhere on a node expands or collapses the branch.
				if (btn == ButtonCode.Mouse2) {
					node.ToggleBranch();
					return;
				}
				double now = EngineCore.Level.Realtime;
				var first = panel.lastClicked;
				var keys = EngineCore.Level.FrameState.Keyboard;
				bool doubleClick = first == node && now - panel.lastClickTime < DOUBLE_CLICK_TIME && !keys.ControlDown && !keys.ShiftDown;
				panel.lastClicked = doubleClick ? null : node;
				panel.lastClickTime = now;
				if (doubleClick) {
					panel.NodeDoubleClicked?.Invoke(panel, first!);
					return;
				}
				// Auto scroll is for viewport selections, the clicked node is already in view.
				panel.treeClick = true;
				try { panel.NodeClicked?.Invoke(panel, node, btn); }
				finally { panel.treeClick = false; }
			};

			panel.InvalidateLayout();
			panel.InvalidateChildren();

			return node;
		}

		public override void Paint(float width, float height) {
			base.Paint(width, height);
		}
	}
}
