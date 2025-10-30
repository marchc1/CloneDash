using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI.Elements;

using Raylib_cs;

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

using MouseButton = Nucleus.Input.MouseButton;

namespace Nucleus.UI
{
	public class UserInterface : Element, IDisposable
	{
		public Element? Focused;
		public Element? Hovered;
		public Element? Depressed;

		//public List<Element> Popups = [];
		public List<Element> Popups { get; private set; } = [];
		public List<Element> Modals { get; private set; } = [];
		public List<Element> Elements { get; private set; } = [];
		public bool PopupActive => Popups.Count > 0;
		public bool ModalActive => Modals.Count > 0;

		public void RemovePopup(Element e) {
			Popups.Remove(e);
		}

		public void RemoveModal(Element e) {
			Modals.Remove(e);
		}

		OSWindow? window;
		public OSWindow Window {
			get => window ?? throw new NullReferenceException();
			set => window = value ?? throw new NullReferenceException();
		}
		public Element? KeyboardFocusedElement { get; internal set; } = null;
		public bool DemandedFocus { get; private set; } = false;
		public void RequestKeyboardFocus(Element self) {
			if (!IValidatable.IsValid(self))
				return;

			if (IValidatable.IsValid(KeyboardFocusedElement)) {
				if (DemandedFocus)
					return;

				KeyboardFocusedElement.KeyboardFocusLost(self, true);
			}

			KeyboardFocusedElement = self;
			self.KeyboardFocusGained(DemandedFocus);
			Window.StartTextInput();
		}
		public void DemandKeyboardFocus(Element self) {
			DemandedFocus = false;      // have to reset it even if its true so the return doesnt occur in the request method
			RequestKeyboardFocus(self);
			DemandedFocus = true;       // set this flag to true so requestkeyboardfocus fails
		}
		public void KeyboardUnfocus(Element self, bool force = false) {
			if (!IValidatable.IsValid(KeyboardFocusedElement))
				return;
			if (!IValidatable.IsValid(self))
				return;
			if (KeyboardFocusedElement.IsIndirectChildOf(self)) {
				KeyboardFocusedElement.KeyboardFocusLost(self, false);
				KeyboardFocusedElement = null;

				return;
			}
			if (self != KeyboardFocusedElement && force == false)
				return;

			KeyboardFocusedElement.KeyboardFocusLost(self, false);
			KeyboardFocusedElement = null;
			Window.StopTextInput();
		}

		public UserInterface() {
			Preprocess(EngineCore.Window.Size);
		}

		protected override void Initialize() {
			UI = this;

			Preprocess(EngineCore.Window.Size);
		}

		public void Preprocess(Vector2F size) => Preprocess(size.X, size.Y);
		public void Preprocess(float width, float height) {
			if (width != this.Size.W || height != this.Size.H) {
				this.Position = new(0, 0);
				this.Size = new(width, height);
				RenderBounds = RectangleF.FromPosAndSize(this.Position, this.Size);
				InvalidateChildren(recursive: true, self: true);
			}
		}

		public override void PostRenderChildren() {
			var text = TooltipText;
			if (text != "" && text != null) {
				var fontsize = 20;
				var size = Graphics2D.GetTextSize(text, Graphics2D.UI_FONT_NAME, fontsize) + new Vector2F(8, 4);
				var mousepos = Level.FrameState.Mouse.MousePos + new Vector2F(8, 8 + 16);

				// determine if tooltip goes over screen bounds and fix it if so
				var drawingOffset = Vector2F.Zero;
				var whereIsEnd = mousepos + size + new Vector2F(4, 4);

				if (whereIsEnd.X > EngineCore.GetScreenSize().W) drawingOffset.X -= (size.X) + 4;
				if (whereIsEnd.Y > EngineCore.GetScreenSize().H) drawingOffset.Y -= (size.Y) + 4 + 24 + 24;

				Graphics2D.SetDrawColor(50, 57, 65, 120);
				Graphics2D.DrawRectangle(mousepos + drawingOffset, size);
				Graphics2D.SetDrawColor(10, 15, 25, 225);
				Graphics2D.SetDrawColor(235, 235, 235, 255);
				Graphics2D.DrawRectangleOutline(mousepos + drawingOffset, size + new Vector2F(4, 4), 1);
				Graphics2D.DrawText((mousepos + drawingOffset) + new Vector2F(6, 4), text, Graphics2D.UI_FONT_NAME, fontsize);
			}
		}

		protected override void OnThink(FrameState frameState) {
			Clipping = false;
			//this.Position = new(0, 0);
			//this.Size = new(frameState.WindowWidth, frameState.WindowHeight);
			//RenderBounds = RectangleF.FromPosAndSize(this.Position, this.Size);
		}

		internal override void SetupLayout() {
			LayoutInvalidated = false;
		}

		public override void MouseClick(FrameState state, Input.MouseButton button) {
			KeyboardUnfocus(this, true);
		}

		private string _tooltipText = "";
		private bool disposedValue;

		public override string TooltipText {
			get {
				if (Hovered != null && Hovered != this) {
					return Hovered.TooltipText;
				}
				return _tooltipText;
			}
			set {
				_tooltipText = value;
			}
		}

		public override void Center() {
			OSMonitor screen = EngineCore.Window.Monitor;
			var mpos = screen.Position;
			var msize = screen.Bounds;

			var mposCenter = mpos + (msize / 2);
			var mposFinal = mposCenter - (EngineCore.Window.Size / 2);
			EngineCore.SetWindowPosition(mposFinal);
		}
		~UserInterface() {
			MainThread.RunASAP(Remove);
		}

		public event MouseEventDelegate? OnElementClicked;
		public event MouseEventDelegate? OnElementReleased;

		public void TriggerElementClicked(Element? e, FrameState fs, Input.MouseButton mb) => OnElementClicked?.Invoke(e!, fs, mb);
		public void TriggerElementReleased(Element e, FrameState fs, Input.MouseButton mb) => OnElementReleased?.Invoke(e, fs, mb);
		public Menu Menu() {
			return this.Add<Menu>();
		}
		public Level? EngineLevel { get; set; }

		// Tries to trash up a bunch of references
#nullable disable
		private void trashElement(Element e) {
			e.UI = null;
			foreach (var child in e.Children)
				trashElement(child);
			e.Children.Clear();
			e.Parent = null;

			foreach (var ev in e.GetType().GetEvents(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
				var field = e.GetType().GetField(ev.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				if (field != null) {
					field.SetValue(e, null);
				}
				else {
					var altField = e.GetType().GetField($"_{ev.Name}", BindingFlags.Instance | BindingFlags.NonPublic);
					if (altField != null) {
						altField.SetValue(e, null);
					}
					else {
						//Debug.Assert(false);
					}
				}
			}
		}
#nullable enable

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					EngineLevel = null;
					// trash the element
					this.Remove();
					trashElement(this);
					Elements.Clear();
					Popups.Clear();
					Focused = null;
					Hovered = null;
					Depressed = null;
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~UserInterface()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		readonly List<Element> activePopups = [];

		// TODO: Is this more efficient than recursive in this case?
		readonly Queue<Element> popupWork = [];

		Element? lastPopup;
		Element? lastModal;

		void DetermineLasts() {
			lastPopup = null;
			lastModal = null;
			popupWork.Clear();
			popupWork.Enqueue(UI);
			while (popupWork.TryDequeue(out Element? el)) {
				if (el.IsPopup)
					lastPopup = el;
				if (el.IsModal)
					lastModal = el;
				foreach (var child in el.Children)
					popupWork.Enqueue(child);
			}
		}

		bool tryRunKeybinds(Element? target, FrameState frameState) {
			bool ranKeybinds = false;
			if (IValidatable.IsValid(target)) {
				KeyboardState emulatedState = target.KeyboardInputMarshal.State(ref frameState.Keyboard);
				ranKeybinds = target.Keybinds.TestKeybinds(emulatedState);

				if (!ranKeybinds) {
					ranKeybinds = Keybinds.TestKeybinds(emulatedState);

					if (!ranKeybinds) {
						for (int i = 0; i < KeyboardState.MAXIMUM_KEY_ARRAY_LENGTH; i++) {
							var pressed = emulatedState.WasKeyPressed(i);
							var released = emulatedState.WasKeyReleased(i);
							if (pressed) 
								DoKeyPressed(target, emulatedState, KeyboardLayout.USA.FromInt(i), target == lastModal);
							if (released) 
								DoKeyReleased(target, emulatedState, KeyboardLayout.USA.FromInt(i), target == lastModal && !lastModal.MarkedForDeath);

							if (!ranKeybinds)
								ranKeybinds = WasKeyEventConsumed();
						}

						for (int i = 0, c = emulatedState.GetTextInputsThisFrame(); i < c; i++)
							target?.TextInputOccur(in emulatedState, emulatedState.GetTextInputThisFrameAtIndex(i));
					}
				}
			}
			return ranKeybinds;
		}
		public void HandleInput() {
			DetermineLasts();
			activePopups.Clear();

			FrameState frameState = Level.FrameState;

			if (frameState.Keyboard.TotalKeysThisFrame > 0 || frameState.Keyboard.GetTextInputsThisFrame() > 0) {
				bool ranKeybinds = false;
				Element? target;
				if (IValidatable.IsValid(lastModal)) {
					if (IValidatable.IsValid(KeyboardFocusedElement) && KeyboardFocusedElement.IsIndirectChildOf(lastModal) && KeyboardFocusedElement.Enabled && KeyboardFocusedElement.Visible)
						target = KeyboardFocusedElement;
					else if (lastModal.Enabled && lastModal.Visible)
						target = lastModal;
					else
						target = null;
				}
				else if (IValidatable.IsValid(lastPopup)) {
					if (IValidatable.IsValid(KeyboardFocusedElement) && KeyboardFocusedElement.IsIndirectChildOf(lastPopup) && KeyboardFocusedElement.Enabled && KeyboardFocusedElement.Visible)
						target = KeyboardFocusedElement;
					else if (lastPopup.Enabled && lastPopup.Visible)
						target = lastPopup;
					else
						target = null;
				}
				else if (IValidatable.IsValid(KeyboardFocusedElement) && KeyboardFocusedElement.Enabled && KeyboardFocusedElement.Visible)
					target = KeyboardFocusedElement;
				else
					target = null;

				ranKeybinds = tryRunKeybinds(target, frameState);
				if (!ranKeybinds) {
					// Lets try again, but only if the target wasnt keyboard focused.
					// Because we might have a passive keyboard focused element that wants keybinds.
					if (target != KeyboardFocusedElement)
						ranKeybinds = tryRunKeybinds(KeyboardFocusedElement, frameState);
				}

				if (!ranKeybinds)
					Level.RunKeybinds();
			}
			int rebuilds = Element.LayoutRecursive(this, ref frameState);

			Element? hoveredElement = Element.ResolveElementHoveringState(this, frameState, EngineCore.GetGlobalScreenOffset(), EngineCore.GetScreenBounds());
			Hovered = hoveredElement;

			if (frameState.Mouse.MouseClicked) {
				if (Hovered != null) {
					Depressed = Hovered;

					if (frameState.Mouse.Mouse1Clicked) DoMouseClick(Hovered, frameState, MouseButton.Mouse1);
					if (frameState.Mouse.Mouse2Clicked) DoMouseClick(Hovered, frameState, MouseButton.Mouse2);
					if (frameState.Mouse.Mouse3Clicked) DoMouseClick(Hovered, frameState, MouseButton.Mouse3);
					if (frameState.Mouse.Mouse4Clicked) DoMouseClick(Hovered, frameState, MouseButton.Mouse4);
					if (frameState.Mouse.Mouse5Clicked) DoMouseClick(Hovered, frameState, MouseButton.Mouse5);
				}
				else {
					if (frameState.Mouse.Mouse1Clicked) TriggerElementClicked(null, frameState, MouseButton.Mouse1);
					if (frameState.Mouse.Mouse2Clicked) TriggerElementClicked(null, frameState, MouseButton.Mouse2);
					if (frameState.Mouse.Mouse3Clicked) TriggerElementClicked(null, frameState, MouseButton.Mouse3);
					if (frameState.Mouse.Mouse4Clicked) TriggerElementClicked(null, frameState, MouseButton.Mouse4);
					if (frameState.Mouse.Mouse5Clicked) TriggerElementClicked(null, frameState, MouseButton.Mouse5);
				}
			}

			if (frameState.Mouse.MouseReleased) {
				if (Depressed != null) {
					if (Hovered == Depressed) {
						if (frameState.Mouse.Mouse1Released)
							Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse1);
						if (frameState.Mouse.Mouse2Released)
							Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse2);
						if (frameState.Mouse.Mouse3Released)
							Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse3);
						if (frameState.Mouse.Mouse4Released)
							Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse4);
						if (frameState.Mouse.Mouse5Released)
							Hovered.MouseReleaseOccur(frameState, MouseButton.Mouse5);
					}
					else {
						if (frameState.Mouse.Mouse1Released)
							Depressed.MouseLostOccur(frameState, MouseButton.Mouse1);
						if (frameState.Mouse.Mouse2Released)
							Depressed.MouseLostOccur(frameState, MouseButton.Mouse2);
						if (frameState.Mouse.Mouse3Released)
							Depressed.MouseLostOccur(frameState, MouseButton.Mouse3);
						if (frameState.Mouse.Mouse4Released)
							Depressed.MouseLostOccur(frameState, MouseButton.Mouse4);
						if (frameState.Mouse.Mouse5Released)
							Depressed.MouseLostOccur(frameState, MouseButton.Mouse5);
					}
					if (Depressed != null) {
						Depressed.Depressed = false;
						Depressed = null;
					}
				}
			}

			if (!frameState.Mouse.MouseScroll.IsZero()) {
				if (IValidatable.IsValid(Hovered) && Hovered.InputDisabled == false) {
					Element e = Hovered;
					for (int i = 0; i < 1000; i++) {
						if (!IValidatable.IsValid(e))
							break;

						e.ConsumedScrollEvent = false;
						e.MouseScrollOccur(frameState, frameState.Mouse.MouseScroll);
						if (e.ConsumedScrollEvent)
							break;

						e = e.Parent;
					}
				}
			}

			if (frameState.Mouse.MouseHeld && !frameState.Mouse.MouseClicked && !frameState.Mouse.MouseDelta.IsZero() && IValidatable.IsValid(Depressed) && Depressed.InputDisabled == false)
				Depressed.MouseDragOccur(frameState, frameState.Mouse.MouseDelta);
		}


		private bool DoKeyPressed(Element element, in KeyboardState emulatedState, Input.KeyboardKey keyboardKey, bool recurseChildren) {
			ResetKeyEventConsumed();
			element.KeyPressedOccur(in emulatedState, keyboardKey);
			if (WasKeyEventConsumed())
				return true;

			if (recurseChildren) 
				foreach(var child in element.Children) 
					if (DoKeyPressed(child, in emulatedState, keyboardKey, true))
						return true;
			
			return false;
		}

		private bool DoKeyReleased(Element element, in KeyboardState emulatedState, Input.KeyboardKey keyboardKey, bool recurseChildren) {
			ResetKeyEventConsumed();
			element.KeyReleasedOccur(in emulatedState, keyboardKey);
			if (WasKeyEventConsumed())
				return true;

			if (recurseChildren)
				foreach (var child in element.Children)
					if (DoKeyReleased(child, in emulatedState, keyboardKey, true))
						return true;

			return false;
		}

		public void Render() {
			activePopups.Clear();
			Element.DrawRecursive(UI, activePopups);

			foreach (var popup in activePopups) {
				Element.DrawRecursive(popup);
			}
		}

		internal void HandleThinking() => Element.ThinkRecursive(UI, Level.FrameState);

		bool KeyEventConsumed = true;
		public bool ResetKeyEventConsumed() => KeyEventConsumed = true;
		public bool MarkKeyEventNotConsumed() => KeyEventConsumed = false;
		public bool WasKeyEventConsumed() => KeyEventConsumed;

		bool MouseEventConsumed = true;
		public bool ResetMouseEventConsumed() => MouseEventConsumed = true;
		public bool MarkMouseEventNotConsumed() => MouseEventConsumed = false;
		public bool WasMouseEventConsumed() => MouseEventConsumed;

		private void DoMouseClick(Element hovered, FrameState frameState, MouseButton button) {
			if (hovered.IsPopup)
				hovered.MoveToFront();
			else if (hovered.IsParentedToPopup(out Element? parent))
				parent.MoveToFront();

			hovered.MouseClickOccur(frameState, button);
		}
	}
}
