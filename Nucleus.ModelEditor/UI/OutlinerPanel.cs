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

				Graphics2D.SetTexture((ITexture)UI.Level.Textures.LoadTextureFromFile("models/viseye.png"));
				Graphics2D.DrawTexturedRectangle(new Vector2F(4, 4), new(16));

				Graphics2D.SetTexture((ITexture)UI.Level.Textures.LoadTextureFromFile("models/keyframe.png"));
				Graphics2D.DrawTexturedRectangle(new Vector2F(4 + 23, 4), new(16));

				Graphics2D.SetTexture((ITexture)UI.Level.Textures.LoadTextureFromFile("models/tree.png"));
				Graphics2D.DrawTexturedRectangle(new Vector2F(4 + 46, 4), new(16));
				
				Graphics2D.SetDrawColor(parent.GetFgColor());
				Graphics2D.DrawLine(23, 0, 23, height);
				Graphics2D.DrawLine(46, 0, 46, height);
			}
		}
		TopPanel Top;
		ScrollPanel Right;
		public OutlinerPanel(Element parent) : base(parent) {
			Top = new(this);
			Right = new(this);
			SetDockPadding(RectangleF.TLRB(0));
			SetBorderSize(0);

			Top.SetDock(Dock.Top);
			Top.SetSize(new(24));
			Top.SetBorderSize(0);
			Top.SetPaintBackgroundEnabled(false);

			Right.SetDock(Dock.Fill);
			Right.SetBorderSize(0);
			Right.SetDockPadding(RectangleF.TLRB(0));
			Right.SetPaintBackgroundEnabled(false);

			SetAddParent(Right.GetAddParent());
			Right.SetDockPadding(RectangleF.TLRB(0));
			Right.GetAddParent().SetDockPadding(RectangleF.TLRB(0));
			Right.SetDockMargin(RectangleF.TLRB(1));
			Right.GetAddParent().SetClipping(true);

			ModelEditor.Active.File.ModelAdded += File_ModelAdded;
			ModelEditor.Active.File.Cleared += File_Cleared;

			ModelEditor.Active.File.OperatorActivated += File_OperatorActivated;
			ModelEditor.Active.File.OperatorDeactivated += File_OperatorDeactivated;
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

		private void RegisterAttachmentNode(OutlinerNode parentNode, EditorAttachment attachment) {
			OutlinerNode attachmentNode = parentNode.AddNode(attachment.Name, attachment.EditorIcon);
			attachmentNode.SetRepresentingObject(attachment);

			ModelEditor.Active.File.AttachmentRenamed += (_, attachmentR, _, newName) => {
				if (attachmentR == attachment) {
					attachmentNode.SetText(newName);
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
					slotNode.SetText(newName);
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
			boneNode.SetText(animation.Name);

			ModelEditor.Active.File.AnimationRenamed += (file, animationR, oldName, newName) => {
				if (animationR == animation)
					boneNode.SetText(newName);
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
			boneNode.SetText(skin.Name);

			ModelEditor.Active.File.SkinRenamed += (file, skinR, oldName, newName) => {
				if (skinR == skin)
					boneNode.SetText(newName);
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
					dragPanel.SetBorderSize(0);
					dragPanel.SetBgColor(new Common.Types.Color(200, 200, 255));
					dragPanel.SetSize(new(slotNode.GetRenderBounds().W, 2));
					dragPanel.SetPassthru(true);
				}
				var hovered = UI.GetHoveredElement();
				if (hovered is OutlinerNode node && node.ParentNode == drawOrder) {
					// Determine if placing above or below
					Vector2F mousePos = fs.Mouse.MousePos;
					Vector2F nodePos = node.GetGlobalPosition() + GetAddParent().GetChildRenderOffset();
					float height = node.GetRenderBounds().Height;
					bool below = mousePos.Y - nodePos.Y > (height / 2);

					dragPanel.SetVisible(true);
					dragPanel.SetPos(below ? nodePos + new Vector2F(0, height) : nodePos);
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
					Vector2F nodePos = node.GetGlobalPosition() + GetAddParent().GetChildRenderOffset();
					float height = node.GetRenderBounds().Height;
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

				return base.MouseRelease(self, fs, button);
			}
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
					boneNode.SetText(newName);
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
					modelNode.SetText(newName);
				}
			};
			ModelEditor.Active.File.ModelRemoved += (file, modelR) => {
				if (modelR == model) {
					modelNode.Remove();
				}
			};
			ModelEditor.Active.File.SlotAdded += (file, _model, _bone, slot) => {
				if (_model != model) return;

				SlotOutlinerNode slotNode = drawOrder.AddNode<SlotOutlinerNode>(slot.Name, p => new(p), "models/slot.png");
				slotNode.slot = slot;
				slotNode.model = model;
				slotNode.SetRepresentingObject(slot);
				ModelEditor.Active.File.SlotRenamed += (file, slotR, oldName, newName) => {
					if (slotR == slot) {
						slotNode.SetText(newName);
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
			node.SetParent(this);
			if (node.Expanded == false) 
				return;
			
			foreach (var child in node.GetChildNodesInOrder())
				ReaddNodeIntoChildren(child, layer + 1);
		}

		public void Relayout() {
			GetAddParent().InvalidateChildren();
		}

		protected override void PerformLayout(float width, float height) {
			base.PerformLayout(width, height);
			ClearChildrenNoRemove();
			foreach (var node in RootNodes)
				ReaddNodeIntoChildren(node);
			InvalidateLayout();
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
			node.SetText(text);
			if (icon != null)
				node.ImageTexture = panel.UI.Level.Textures.LoadTextureFromFile(icon);

			node.OnButtonClick += (_, btn) => {
				panel.NodeClicked?.Invoke(panel, node, btn);
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
