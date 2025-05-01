using Microsoft.VisualBasic;
using Nucleus.Core;
using Nucleus.Input;
using Nucleus.ModelEditor.UI;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.Util;
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
			Right.DockMargin = RectangleF.TLRB(1);
			Right.AddParent.Clipping = true;

			ModelEditor.Active.File.ModelAdded += File_ModelAdded;
			ModelEditor.Active.File.Cleared += File_Cleared;

			ModelEditor.Active.File.OperatorActivated += File_OperatorActivated;
			ModelEditor.Active.File.OperatorDeactivated += File_OperatorDeactivated;
		}

		private void File_OperatorActivated(EditorFile self, UI.Operator op) {
			if (op.SelectableTypes == null) return;
			HashSet<Type> acceptableTypes = op.SelectableTypes.ToHashSet();
			foreach (var node in Right.AddParent.GetChildren()) {
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
			foreach (var node in Right.AddParent.GetChildren()) {
				if (node is OutlinerNode outlinerNode) {
					outlinerNode.SelectableOverride = null;
				}
			}
		}

		private void File_Cleared(EditorFile file) {
			AddParent.ClearChildren();
			RootNodes.Clear();
		}

		public delegate void OnNodeClicked(OutlinerPanel panel, OutlinerNode node, MouseButton btn);
		public event OnNodeClicked? NodeClicked;

		private void RegisterAttachmentNode(OutlinerNode parentNode, EditorAttachment attachment) {
			OutlinerNode attachmentNode = parentNode.AddNode(attachment.Name, attachment.EditorIcon);
			attachmentNode.SetRepresentingObject(attachment);

			ModelEditor.Active.File.AttachmentRenamed += (_, attachmentR, _, newName) => {
				if (attachmentR == attachment) {
					attachmentNode.Text = newName;
				}
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
					slotNode.Text = newName;
				}
			};
			ModelEditor.Active.File.SlotRemoved += (_, _, _, slotR) => {
				if (slotR == slot) {
					slotNode.Remove();
				}
			};
			ModelEditor.Active.File.AttachmentAdded += (_, slotR, attachment) => {
				if (slotR == slot)
					RegisterAttachmentNode(slotNode, attachment);
			};
		}

		private void SetupAnimationNode(OutlinerNode skinsNode, EditorModel model, EditorAnimation animation) {
			OutlinerNode boneNode = skinsNode.AddNode(animation.Name, "models/animation2.png");
			boneNode.SetRepresentingObject(animation);
			boneNode.Text = animation.Name;

			ModelEditor.Active.File.AnimationRenamed += (file, animationR, oldName, newName) => {
				if (animationR == animation)
					boneNode.Text = newName;
			};

			ModelEditor.Active.File.AnimationRemoved += (file, model, animationR) => {
				if (animationR == animation && IValidatable.IsValid(boneNode)) {
					boneNode.Remove();
				}
			};
		}
		private void SetupSkinNode(OutlinerNode skinsNode, EditorModel model, EditorSkin skin) {
			OutlinerNode boneNode = skinsNode.AddNode(skin.Name, "models/skin.png");
			boneNode.SetRepresentingObject(skin);
			boneNode.Text = skin.Name;

			ModelEditor.Active.File.SkinRenamed += (file, skinR, oldName, newName) => {
				if (skinR == skin)
					boneNode.Text = newName;
			};

			ModelEditor.Active.File.SkinRemoved += (file, model, skinR) => {
				if (skinR == skin && IValidatable.IsValid(boneNode)) {
					boneNode.Remove();
				}
			};
		}

		private void RegisterBoneEvents(OutlinerNode parentNode, EditorBone bone) {
			OutlinerNode boneNode = parentNode.AddNode(bone.Name, "models/bone.png");
			boneNode.SetRepresentingObject(bone);
			boneNode.ImageColor = bone.Color;

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
			drawOrder.ChangeChildOrder += (_, children) => {
				Dictionary<EditorSlot, int> indexOf = [];
				for (int i = 0; i < model.Slots.Count; i++) {
					indexOf[model.Slots[i]] = i;
				}

				children.Sort((x, y) => {
					EditorSlot xS = x.GetRepresentingObject<EditorSlot>() ?? throw new Exception("wtf");
					EditorSlot yS = y.GetRepresentingObject<EditorSlot>() ?? throw new Exception("wtf");

					return indexOf[yS].CompareTo(indexOf[xS]);
				});
			};

			ModelEditor.Active.File.ModelRenamed += (file, modelR, oldName, newName) => {
				if (modelR == model) {
					modelNode.Text = newName;
				}
			};
			ModelEditor.Active.File.ModelRemoved += (file, modelR) => {
				if (modelR == model) {
					modelNode.Remove();
				}
			};
			ModelEditor.Active.File.SlotAdded += (file, _model, _bone, slot) => {
				if (_model != model) return;

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
				bool startDragging = false;
				Panel? dragPanel = null;
				slotNode.MouseDragEvent += (s, fs, _) => {
					startDragging = true;

					if (!IValidatable.IsValid(dragPanel)) {
						dragPanel = UI.Add<Panel>();
						dragPanel.BorderSize = 0;
						dragPanel.BackgroundColor = new(200, 200, 255);
						dragPanel.Size = new(slotNode.RenderBounds.W, 2);
						dragPanel.OnHoverTest += Element.Passthru;
					}
					var hovered = UI.Hovered;
					if (hovered is OutlinerNode node && node.ParentNode == drawOrder) {
						// Determine if placing above or below
						Vector2F mousePos = fs.MouseState.MousePos;
						Vector2F nodePos = node.GetGlobalPosition() + AddParent.ChildRenderOffset;
						float height = node.RenderBounds.Height;
						bool below = mousePos.Y - nodePos.Y > (height / 2);

						dragPanel.Visible = true;
						dragPanel.Position = below ? nodePos + new Vector2F(0, height) : nodePos;
					}
					else {
						dragPanel.Visible = false;
					}
				};
				slotNode.MouseReleasedOrLostEvent += (s, fs, _, _) => {
					startDragging = false;
					dragPanel?.Remove();

					var hovered = UI.Hovered;
					if (hovered is OutlinerNode node && node.ParentNode == drawOrder) {
						var otherslot = node.GetRepresentingObject<EditorSlot>();
						if (otherslot == null || otherslot == slot) return;

						// Determine if placing above or below
						Vector2F mousePos = fs.MouseState.MousePos;
						Vector2F nodePos = node.GetGlobalPosition() + AddParent.ChildRenderOffset;
						float height = node.RenderBounds.Height;
						bool below = mousePos.Y - nodePos.Y > (height / 2);

						var drawOrderList = model.Slots;
						var indexOfSelf = drawOrderList.IndexOf(slot);
						if (indexOfSelf == -1) throw new Exception("Wtf?");

						var indexOfOther = drawOrderList.IndexOf(otherslot);
						if (indexOfOther == -1) throw new Exception("Wtf?");

						Console.WriteLine($"Drag completed.");
						Console.WriteLine($"Draw order length:        {drawOrderList.Count}");
						Console.WriteLine($"Current slot:             {slot.Name}");
						Console.WriteLine($"Current position:         {indexOfSelf}");
						Console.WriteLine($"Target slot:              {otherslot.Name}");
						Console.WriteLine($"Target position:          {indexOfOther}");
						Console.WriteLine($"Move current -> target:   {indexOfOther + (below ? -1 : 1)}");

						Util.Util.MoveListItem(drawOrderList, slot, indexOfOther);
						drawOrder.InvalidateNode();
					}
				};
			};

			OutlinerNode skinsNode = modelNode.AddNode("Skins", "models/skins.png");
			skinsNode.SetRepresentingObject(model.Skins);
			ModelEditor.Active.File.SkinAdded += (file, _model, skin) => {
				if (_model != model) return;
				SetupSkinNode(skinsNode, _model, skin);
			};

			OutlinerNode animationsNode = modelNode.AddNode("Animations", "models/animation.png");
			animationsNode.SetRepresentingObject(model.Animations);
			ModelEditor.Active.File.AnimationAdded += (file, _model, animation) => {
				if (_model != model) return;
				SetupAnimationNode(animationsNode, _model, animation);
			};

			//OutlinerNode animationsNode = modelNode.AddNode("Animations", "models/animation.png");
			OutlinerNode imagesNode = modelNode.AddNode("Images", "models/images.png");
			imagesNode.SetRepresentingObject(model.Images);
			AlphanumComparatorFast alphanum = new AlphanumComparatorFast();
			imagesNode.ChangeChildOrder += (_, children) => {
				children.Sort((x, y) => {
					ModelImage xS = x.GetRepresentingObject<ModelImage>() ?? throw new Exception("wtf");
					ModelImage yS = y.GetRepresentingObject<ModelImage>() ?? throw new Exception("wtf");

					return alphanum.Compare(xS.Name, yS.Name);
				});
			};

			ModelEditor.Active.File.ModelImagesScanned += (file, _model) => {
				if (_model != model) return;
				imagesNode.ClearChildNodes();
				foreach (var image in model.Images.Images) {
					var imageNode = imagesNode.AddNode(image.Name, "models/region.png");
					imageNode.SetRepresentingObject(image);
				}
			};
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
