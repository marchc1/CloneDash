using Nucleus.Core;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Model = Nucleus.Models.Model;
using MouseButton = Nucleus.Types.MouseButton;

namespace Nucleus.ModelEditor
{
	public enum RootNodeContext
	{
		None,
		Model,
		RootBone,
		ChildSlots,
		ChildBones,
		DrawOrder,
		DrawOrderSlot,
		Animations,
		Images
	}
	public class OutlinerPanel : Panel
	{
		public static float ITEM_HEIGHT => 16f;

		public MouseButton Button;
		public Vector2F HoverPos;
		public Vector2F? ReleasePos;
		public bool Released;

		public object? HoveredObject;
		public object? ClickedObject;
		public object? DraggedObject;

		protected override void Initialize() {
			base.Initialize();
			DrawPanelBackground = false;
		}

		protected override void OnThink(FrameState frameState) {
			base.OnThink(frameState);
			HoverPos = GetMousePos();
		}

		public override void MouseClick(FrameState state, MouseButton button) {
			base.MouseClick(state, button);
			ClickedObject = HoveredObject;
			ClickedContext = HoveredContext;
		}

		public override void MouseDrag(Element self, FrameState state, Vector2F delta) {
			base.MouseDrag(self, state, delta);
			if (delta.Length > 1.5f) {
				switch (ClickedContext) {
					// No dragging logic
					case RootNodeContext.Model:
					case RootNodeContext.RootBone:
					case RootNodeContext.DrawOrder:
						return;
					default:
						DraggedObject = ClickedObject;
						DraggingContext = ClickedContext;
						return;
				}
			}
		}

		public override void MouseRelease(Element self, FrameState state, MouseButton button) {
			base.MouseRelease(self, state, button);
			ReleasePos = HoverPos;
			Released = true;
			Button = button;

			ClickedObject = null;
			DraggedObject = null;

			ClickedContext = RootNodeContext.None;
			DraggingContext = RootNodeContext.None;
		}

		public override void MouseScroll(Element self, FrameState state, Vector2F delta) {
			base.MouseScroll(self, state, delta);
			ScrollY += delta.Y * -8f;
		}

		float itemHeight = 18;
		float itemYPadding = 4;
		float itemXOffset = 16;

		RootNodeContext RenderingContext = RootNodeContext.None;
		RootNodeContext HoveredContext = RootNodeContext.None;
		RootNodeContext ClickedContext = RootNodeContext.None;
		RootNodeContext DraggingContext = RootNodeContext.None;

		public void DrawItem(object item, float xI, float xScroll, ref float y, float width, float height) {
			ModelEditor editor = ModelEditor.Active;
			float x = xI * itemXOffset;
			bool passedRenderChecks = false;
			bool showVisibility = false;
			bool useExpanderLogic = false;
			switch (item) {
				case Model model: useExpanderLogic = true; showVisibility = true; break;
				case Bone bone: useExpanderLogic = (bone.Slots.Count + bone.Children.Count) > 0; showVisibility = true; break;
				case List<Slot> drawOrder: useExpanderLogic = drawOrder.Count > 0; showVisibility = false; break;
				case Slot slot: useExpanderLogic = false; showVisibility = true; break;
				default: break;
			}
			bool expanded = useExpanderLogic && editor.GetObjectState(item).Expanded;

			bool releasetest1 = false;
			bool releasetest2 = false;
			bool releasetest3 = false;
			bool releasetest4 = false;

			if ((y + itemHeight) <= 0)
				goto end;
			if (y > RenderBounds.Height)
				goto end;

			passedRenderChecks = true;

			// Button parameters.
			// 1 refers to the visibility column
			// 2 refers to the keyframing column
			// 3 refers to the expansion column (if the item contains children of some kind)
			// 4 refers to "empty space" (for selection mostly)
			float x1 = 0, w1 = itemHeight;
			float x2 = itemHeight * 1, w2 = itemHeight;
			float x3 = (itemHeight * 2) - xScroll, w3 = itemHeight + x;
			float x4 = x3 + w3, w4 = width - x4;

			Vector2F hoverPos = HoverPos;

			bool hovertest1 = RectangleF.ContainsPoint(x1, y, w1, itemHeight, hoverPos);
			bool hovertest2 = RectangleF.ContainsPoint(x2, y, w2, itemHeight, hoverPos);
			bool hovertest3 = RectangleF.ContainsPoint(x3, y, w3, itemHeight, hoverPos);
			bool hovertest4 = RectangleF.ContainsPoint(x4, y, w4, itemHeight, hoverPos);

			bool depresstest1 = hovertest1 && Depressed;
			bool depresstest2 = hovertest2 && Depressed;
			bool depresstest3 = hovertest3 && Depressed;
			bool depresstest4 = hovertest4 && Depressed;

			releasetest1 = hovertest1 && Released;
			releasetest2 = hovertest2 && Released;
			releasetest3 = hovertest3 && Released;
			releasetest4 = hovertest4 && Released;

			var colorMult = 1f;
			if (DraggingContext != RootNodeContext.None) {
				if (RenderingContext != DraggingContext) {
					switch (item) {
						case List<Slot>:
							if(DraggingContext != RootNodeContext.DrawOrderSlot)
								colorMult = 0.5f;
							break;
						default:
							colorMult = 0.5f;
							break;
					}
				}
			}

			Graphics2D.ScissorRect(RectangleF.XYWH(Graphics2D.Offset.X, Graphics2D.Offset.Y + 16, width, height - 16));
			bool generallyHovered = hovertest1 || hovertest2 || hovertest3 || hovertest4;
			if (editor.IsObjectSelected(item)) {
				Graphics2D.SetDrawColor(150, 200, 255, 70);
				Graphics2D.DrawRectangle(x1, y - 2, width, itemHeight + 4);
			}
			else if (generallyHovered) {
				Graphics2D.SetDrawColor(255, 255, 255, 30);
				Graphics2D.DrawRectangle(x1, y - 2, width, itemHeight + 4);
			}

			if (generallyHovered) {
				HoveredObject = item;
				HoveredContext = RenderingContext;
			}
			Graphics2D.ScissorRect();

			Graphics2D.ScissorRect(RectangleF.XYWH(Graphics2D.Offset.X, Graphics2D.Offset.Y + 16, itemHeight * 2, height - 16));
			int c;
			// Side-bar drawing operations.
			if (showVisibility) {
				switch (item) {
					default:
						c = depresstest1 ? 100 : hovertest1 ? 200 : 155;
						Graphics2D.SetDrawColor(c, c, c);
						Graphics2D.DrawCircle(new(x1 + (itemHeight / 2), y + (itemHeight / 2)), itemHeight / 6);
						break;
				}
			}
			Graphics2D.ScissorRect();

			Graphics2D.ScissorRect(RectangleF.XYWH(Graphics2D.Offset.X + (itemHeight * 2), Graphics2D.Offset.Y + 16, width - (itemHeight * 2), height - 16));

			c = (int)((depresstest3 ? 100 : hovertest3 ? 200 : 155) * colorMult);
			Vector2F center = new Vector2F(x3 + (itemHeight / 2), y + (itemHeight / 2));
			Graphics2D.OffsetDrawing(new(x, 0));

			// Determine if expanded or not for button 3.
			// Also decide things like names/icons for button 4
			string? text = null;
			string? icon = null;
			switch (item) {
				case Model model:
					text = model.Name;
					icon = "models/model.png";
					break;
				case Bone bone:
					text = bone.Name;
					icon = "models/bone.png";
					break;
				case List<Slot> drawOrder:
					text = "Draw Order";
					icon = "models/draworder.png";
					break;
				case Slot slot:
					text = slot.Name;
					icon = "models/slot.png";
					break;

				default:
					break;
			}

			if (useExpanderLogic && releasetest3)
				editor.GetObjectState(item).Expanded = !editor.GetObjectState(item).Expanded;


			if (useExpanderLogic) {
				Graphics2D.SetDrawColor(c, c, c);
				if (expanded)
					Graphics2D.SetTexture(UI.Level.Textures.LoadTextureFromFile("models/expanded.png"));
				else
					Graphics2D.SetTexture(UI.Level.Textures.LoadTextureFromFile("models/collapsed.png"));

				Graphics2D.DrawImage(center - new Vector2F(8), new(16, 16));
			}
			var c2 = (int)(235 * colorMult);
			if (text != null) {
				Graphics2D.SetDrawColor(c2, c2, c2);
				Graphics2D.DrawText(new(center.X + 32, center.Y), text, "Noto Sans", 17, Anchor.CenterLeft);
			}
			if (icon != null) {
				Graphics2D.SetDrawColor(c2, c2, c2);
				var tex = UI.Level.Textures.LoadTextureFromFile(icon);
				Graphics2D.SetTexture(tex);
				Graphics2D.DrawImage(new(center.X + 12, center.Y - 8), new(16, 16));
			}
			Graphics2D.OffsetDrawing(new(-x, 0));

			Graphics2D.ScissorRect();

		end: y += itemHeight + itemYPadding;


			if (releasetest4 && Button.Button == 1) {
				ModelEditor.Active.SelectObject(item);
			}
			// Ddetermine if the item builds more nodes after this or not.
			if (expanded) {
				var firstChildNode = true;
				switch (item) {
					case Model model:
						// Process the root bone
						DrawConnector(xI, y, true);
						RenderingContext = RootNodeContext.RootBone;
						DrawItem(model.Root, xI + 1, xScroll, ref y, width, height);
						RenderingContext = RootNodeContext.DrawOrder;
						DrawItem(model.Slots, xI + 1, xScroll, ref y, width, height);
						break;
					case Bone bone:
						RenderingContext = RootNodeContext.ChildSlots;
						// Process all slots
						foreach (var child in bone.Slots) {
							DrawConnector(xI, y, firstChildNode);
							DrawItem(child, xI + 1, xScroll, ref y, width, height);
							firstChildNode = false;
						}
						RenderingContext = RootNodeContext.ChildBones;
						// Process all children
						foreach (var child in bone.Children) {
							DrawConnector(xI, y, firstChildNode);
							DrawItem(child, xI + 1, xScroll, ref y, width, height);
							firstChildNode = false;
						}
						break;
					case List<Slot> drawOrder:
						foreach (var slot in drawOrder) {
							RenderingContext = RootNodeContext.DrawOrderSlot;
							DrawConnector(xI, y, firstChildNode);
							DrawItem(slot, xI + 1, xScroll, ref y, width, height);
							firstChildNode = false;
						}
						break;
				}
			}
		}

		public void DrawConnector(float xI, float y, bool gap) {
			var yPrevious = y - (itemHeight + itemYPadding);
			var xPrevious = (xI - 1) * itemXOffset;
			var xOffset = itemHeight * 3.38f;
			Graphics2D.ScissorRect(RectangleF.XYWH(Graphics2D.Offset.X, Graphics2D.Offset.Y + 16, RenderBounds.W, RenderBounds.H - 16));
			Graphics2D.DrawLineStrip([
				new(xPrevious+xOffset, yPrevious + (gap ? itemHeight : 0)),
				new(xPrevious+xOffset, y + (itemHeight / 2)),
				new(((xI + 0.25f)* itemXOffset)+xOffset, y + (itemHeight / 2))
			]);
			Graphics2D.ScissorRect();
		}

		// Painting logic

		float ScrollX = 0;
		float ScrollY = 0;

		public override void Paint(float width, float height) {
			// Render the first parts here
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.SetTexture(UI.Level.Textures.LoadTextureFromFile("models/viseye.png"));
			Graphics2D.DrawImage(new(0, 0), new(16, 16));

			Graphics2D.SetTexture(UI.Level.Textures.LoadTextureFromFile("models/keyframe.png"));
			Graphics2D.DrawImage(new(itemHeight, 0), new(16, 16));

			Graphics2D.SetTexture(UI.Level.Textures.LoadTextureFromFile("models/tree.png"));
			Graphics2D.DrawImage(new(itemHeight * 2, 0), new(16, 16));

			Graphics2D.SetDrawColor(130, 130, 130);
			Graphics2D.DrawLine(0, 16, width, 16);
			Graphics2D.DrawLine(itemHeight, 0, itemHeight, 16);
			Graphics2D.DrawLine(itemHeight * 2, 0, itemHeight * 2, 16);

			var XPosition = ScrollX;
			var YPosition = 20 - ScrollY;
			HoveredObject = null;
			HoveredContext = RootNodeContext.None;

			foreach (var model in ModelEditor.Active.File.Models) {
				RenderingContext = RootNodeContext.Model;
				DrawItem(model, 0, XPosition, ref YPosition, width, height);
			}

			Graphics2D.DrawText(new(0, height), $"fps: {EngineCore.FPS}", "Noto Sans", 16, Anchor.BottomLeft);

			if (DraggingContext != RootNodeContext.None) {
				var cursor = GetMousePos();
				string text = "";
				switch (DraggedObject) {
					case Bone bone: text = bone.Name; break;
					case Slot slot: text = slot.Name; break;
				}
				string font = "Noto Sans";
				float fontSize = 16;
				var cursorOffset = new Vector2F(12);
				var size = Graphics2D.GetTextSize(text, font, fontSize);
				Graphics2D.SetDrawColor(20, 20, 20, 180);
				Graphics2D.DrawRectangle(cursor + cursorOffset, size + new Vector2F(8));
				Graphics2D.SetDrawColor(245, 245, 245, 255);
				Graphics2D.DrawText(cursor + cursorOffset + new Vector2F(4), text, font, fontSize);
			}

			ReleasePos = null;
			Released = false;
		}
	}
}
