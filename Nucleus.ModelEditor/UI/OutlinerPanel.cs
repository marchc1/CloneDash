using glTFLoader.Schema;
using Microsoft.VisualBasic;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using static System.Net.Mime.MediaTypeNames;

namespace Nucleus.ModelEditor
{
	public interface IContainsOutlinerNodes
	{
		public OutlinerNode AddNode(string text, string? icon = null);
	}
	public class OutlinerPanel : Panel, IContainsOutlinerNodes
	{
		Panel Top;
		ScrollPanel Right;
		protected override void Initialize() {
			base.Initialize();
			Add(out Top);
			Add(out Right);
			DockPadding = RectangleF.TLRB(0);

			Top.Dock = Dock.Top;
			Top.Size = new(24);
			Top.BorderSize = 0;
			Top.DrawPanelBackground = false;
			Top.PaintOverride += Top_PaintOverride;

			Right.Dock = Dock.Fill;
			Right.BorderSize = 0;
			Right.DockPadding = RectangleF.TLRB(0);
			Right.DrawPanelBackground = false;

			AddParent = Right.AddParent;
			Right.DockPadding = RectangleF.TLRB(0);
			Right.AddParent.DockPadding = RectangleF.TLRB(0);

			ModelEditor.Active.File.ModelAdded += File_ModelAdded;
			ModelEditor.Active.File.Cleared += File_Cleared;
		}

		private void File_Cleared(EditorFile file) {
			AddParent.ClearChildren();
			RootNodes.Clear();
		}

		public delegate void OnNodeClicked(OutlinerPanel panel, OutlinerNode node, MouseButton btn);
		public event OnNodeClicked? NodeClicked;

		private void RegisterSlotEvents(OutlinerNode parentNode, EditorSlot slot) {
			OutlinerNode slotNode = parentNode.AddNode(slot.Name, "models/slot.png");
			slotNode.SetRepresentingObject(slot);

			ModelEditor.Active.File.SlotRenamed += (_, slotR, _, newName) => {
				if(slotR == slot) {
					slotNode.Text = newName;
				}
			};
			ModelEditor.Active.File.SlotRemoved += (_, _, _, slotR) => {
				if (slotR == slot) {
					slotNode.Remove();
				}
			};
		}

		private void RegisterBoneEvents(OutlinerNode parentNode, EditorBone bone) {
			OutlinerNode boneNode = parentNode.AddNode(bone.Name, "models/bone.png");
			boneNode.SetRepresentingObject(bone);

			ModelEditor.Active.File.BoneAdded += (file, model, boneA) => {
				if (boneA.Parent == bone) {
					RegisterBoneEvents(boneNode, boneA);
				}
			};
			ModelEditor.Active.File.BoneRenamed += (file, boneR, oldName, newName) => {
				if (boneR == bone)
					boneNode.Text = newName;
			};
			ModelEditor.Active.File.BoneRemoved += (file, model, boneR) => {
				if (boneR == bone && IValidatable.IsValid(boneNode)) {
					boneNode.Remove();
				}
			};
			ModelEditor.Active.File.SlotAdded += (file, model, _bone, slot) => {
				if (_bone == bone)
					RegisterSlotEvents(boneNode, slot);
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
			drawOrder.ChangeChildOrder += (_, children) => {
				Dictionary<EditorSlot, int> indexOf = [];
				for (int i = 0; i < model.Slots.Count; i++) {
					indexOf[model.Slots[i]] = i;
				}

				children.Sort((x, y) => {
					EditorSlot xS = x.GetRepresentingObject<EditorSlot>() ?? throw new Exception("wtf");
					EditorSlot yS = y.GetRepresentingObject<EditorSlot>() ?? throw new Exception("wtf");

					return indexOf[xS].CompareTo(indexOf[yS]);
				});
			};

			ModelEditor.Active.File.ModelRemoved += (file, modelR) => {
				if (modelR == model) {
					modelNode.Remove();
				}
			};
			ModelEditor.Active.File.SlotAdded += (file, _model, _bone, slot) => {
				OutlinerNode slotNode = drawOrder.AddNode(slot.Name, "models/slot.png");
				slotNode.SetRepresentingObject(slot);
				ModelEditor.Active.File.SlotRenamed += (file, slotR, oldName, newName) => {
					if (slotR == slot) {
						slotNode.Text = newName;
					}
				};
				ModelEditor.Active.File.SlotRemoved += (_, _, _, slotR) => {
					if (slotR == slot) {
						slotNode.Remove();
					}
				};
			};

			//OutlinerNode animationsNode = modelNode.AddNode("Animations", "models/animation.png");
			OutlinerNode imagesNode = modelNode.AddNode("Images", "models/images.png");
			//imagesNode.SetRepresentingObject(model.Images);
		}










		public OutlinerNode AddNode(string text, string? icon = null) {
			OutlinerNode node = SetupNode(this, 0, null, text, icon);
			RootNodes.Add(node);
			return node;
		}
		public List<OutlinerNode> RootNodes = [];
		private void ReaddNodeIntoChildren(OutlinerNode node, int layer = 0) {
			node.Layer = layer;
			AddParent.AddChild(node);
			if (node.Expanded == false) {
				return;
			}
			foreach (var child in node.GetChildNodesInOrder())
				ReaddNodeIntoChildren(child, layer + 1);
		}

		public void Relayout() {
			AddParent.InvalidateChildren();
		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
			ClearChildrenNoRemove();
			foreach (var node in RootNodes)
				ReaddNodeIntoChildren(node);
			RevalidateLayout();
			Relayout();
		}
		public static OutlinerNode SetupNode(OutlinerPanel panel, int layer, OutlinerNode? parent = null, string text = "Node", string? icon = null) {
			var node = panel.Add<OutlinerNode>();

			node.Outliner = panel;
			node.Layer = layer;
			if (parent != null) {
				node.ParentNode = parent;
			}
			node.Text = text;
			if (icon != null)
				node.ImageTexture = panel.UI.Level.Textures.LoadTextureFromFile(icon);

			node.MouseReleaseEvent += (_, _, btn) => {
				panel.NodeClicked?.Invoke(panel, node, btn);
			};

			panel.InvalidateLayout();
			panel.InvalidateChildren();

			return node;
		}

		private void Top_PaintOverride(Element self, float width, float height) {
			Graphics2D.SetDrawColor(255, 255, 255);

			Graphics2D.SetTexture(UI.Level.Textures.LoadTextureFromFile("models/viseye.png"));
			Graphics2D.DrawImage(new(4, 4), new(16));

			Graphics2D.SetTexture(UI.Level.Textures.LoadTextureFromFile("models/keyframe.png"));
			Graphics2D.DrawImage(new(4 + 23, 4), new(16));

			Graphics2D.SetTexture(UI.Level.Textures.LoadTextureFromFile("models/tree.png"));
			Graphics2D.DrawImage(new(4 + 46, 4), new(16));

			Graphics2D.SetDrawColor(ForegroundColor);
			Graphics2D.DrawLine(23, 0, 23, height);
			Graphics2D.DrawLine(46, 0, 46, height);
		}

		public override void Paint(float width, float height) {
			base.Paint(width, height);
		}
	}
}
