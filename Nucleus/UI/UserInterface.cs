using Microsoft.VisualBasic;
using Nucleus.Common.Input;
using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI.Elements;

using Raylib_cs;

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Nucleus.UI;

public struct ElementSolveState
{
	// This is the embedded panel (UserInterface right now)
	public Element EmbeddedPanel;
	public Element? KeyboardFocused;

	// Input specific state
	public Element? Hovered;
	public InlineArray10<Element?> Depressed;
}

public class ElementSchemeSystem
{
	public void ApplyScheme(Element? element, ref ElementSolveState state, bool force = false) {
		if (!IValidatable.IsValid(element)) return;

		foreach (var child in element.GetChildren())
			if (child.IsVisible() || force)
				ApplyScheme(child, ref state);

		element.PerformApplySchemeSettings();
	}
}

public class ElementInputSystem
{
	public event Action<Element?>? OnClick;
	public void SolveHovered(Element? element, ref ElementSolveState state, FrameState frameState) {
		if (!IValidatable.IsValid(element)) return;

		state.Hovered = null;

		Vector2F mousePos = frameState.Mouse.MousePos;
		state.Hovered = SolveTraverse(element, ref state, frameState, element.RenderBounds, mousePos);
		// Logs.Info($"{mousePos}, {state.Hovered}");
	}

	private Element? SolveTraverse(Element? element, ref ElementSolveState state, FrameState frameState, RectangleF globalSpaceBounds, Vector2F mousePos) {
		if (!IValidatable.IsValid(element)) return null;

		if (element.HoverTest(globalSpaceBounds, mousePos)) {
			bool selfHovered = element.HoverTest(globalSpaceBounds, mousePos);
			var children = element.GetChildren();
			// If this element contains a modal, only process that modal.
			// Also, process popups first here too.
			for (int i = children.Length - 1; i >= 0; i--) {
				Element child = children[i];
				bool modal = child.IsModal(), popup = child.IsPopup();

				if (modal || popup) {
					// Only traverse this modal child
					RectangleF childGlobalBounds = RectangleF.FromPosAndSize(globalSpaceBounds.Pos + child.RenderBounds.Pos + element.ChildRenderOffset, child.RenderBounds.Size);
					Element? subElementHovered = SolveTraverse(child, ref state, frameState, childGlobalBounds, mousePos);

					if (modal) {
						if (!IValidatable.IsValid(subElementHovered))
							return element;
						return subElementHovered;
					}
					else {
						if (IValidatable.IsValid(subElementHovered))
							return subElementHovered;
					}
				}
			}
			// Process non-modal non-popups now
			for (int i = children.Length - 1; i >= 0; i--) {
				Element child = children[i];
				bool modal = child.IsModal(), popup = child.IsPopup();
				if (!modal && !popup) {
					RectangleF childGlobalBounds = RectangleF.FromPosAndSize(globalSpaceBounds.Pos + child.RenderBounds.Pos + element.ChildRenderOffset, child.RenderBounds.Size);
					Element? subElementHovered = SolveTraverse(child, ref state, frameState, childGlobalBounds, mousePos);
					if (IValidatable.IsValid(subElementHovered))
						return subElementHovered;
				}
			}

			if (!element.IsPassthru())
				return element;
		}

		return null;
	}

	public void DispatchEvents(ref ElementSolveState solveState, FrameState frameState) {
		ref MouseState mouse = ref frameState.Mouse;
		ref KeyboardState keyboard = ref frameState.Keyboard;

		Element? hovered = solveState.Hovered;
		// Handle mouse clicking
		if (IValidatable.IsValid(hovered)) {
			for (ButtonCode i = ButtonCode.MouseFirst; i < ButtonCode.MouseLast + 1; i++) {
				bool clicked = mouse.Clicked(i);
				if (clicked && hovered.IsMouseInputEnabled() && hovered.MouseClickOccur(frameState, i)) {
					mouse.SetClicked(i, false); // disengage input from game
					mouse.SetHeld(i, false); // disengage input from game
					solveState.Depressed[i - ButtonCode.MouseFirst] = hovered;
				}
				if (clicked)
					OnClick?.Invoke(hovered);
			}
		}

		// Handle mouse dragging
		if (!mouse.MouseDelta.IsZero()) {
			for (ButtonCode i = ButtonCode.MouseFirst; i < ButtonCode.MouseLast + 1; i++) {
				ref Element? depressed = ref solveState.Depressed[i - ButtonCode.MouseFirst];
				if (IValidatable.IsValid(depressed) && depressed.IsMouseInputEnabled())
					depressed.MouseDragOccur(frameState, mouse.MouseDelta);
			}
		}

		// Handle mouse releases
		// A click might invalidate via removing, so a second guard is done
		if (IValidatable.IsValid(hovered)) {
			for (ButtonCode i = ButtonCode.MouseFirst; i < ButtonCode.MouseLast + 1; i++) {
				ref Element? depressed = ref solveState.Depressed[i - ButtonCode.MouseFirst];
				if (mouse.Released(i)) {
					if (IValidatable.IsValid(depressed) && depressed.IsMouseInputEnabled() && depressed.MouseReleaseOccur(frameState, i)) {
						mouse.SetHeld(i, false); // disengage input from game
						mouse.SetReleased(i, false); // disengage input from game
						depressed = null;
					}
				}
			}
		}

		// Handle mouse scrolling
		if (!mouse.MouseScroll.IsZero()) {
			if (IValidatable.IsValid(hovered)) {
				// Do a backwards search first
				var checkBack = hovered;
				while (IValidatable.IsValid(checkBack)) {
					if (checkBack.IsMouseInputEnabled() && checkBack.MouseScrollOccur(hovered, frameState, mouse.MouseDelta))
						break;
					checkBack = checkBack.GetParent();
				}
				// Forwards check maybe? todo
			}
		}

		if (!TestKeyboard(solveState.KeyboardFocused, ref keyboard))
			TestKeyboard(solveState.EmbeddedPanel, ref keyboard);
	}

	public bool TestKeyboard(Element? keyboardFocused, ref KeyboardState keyboard) {
		KeyboardState emulated;
		bool keybindChainingAllowed = true;
		while (IValidatable.IsValid(keyboardFocused)) {
			if (!keyboardFocused.IsKeyboardInputEnabled())
				return false;

			emulated = keyboard;
			keyboardFocused.KeyboardInputMarshal.State(ref emulated);
			if (keybindChainingAllowed && keyboardFocused.Keybinds.TestKeybinds(ref emulated, out Keybind? keybind)) {
				keyboard.ConsumeFirstKeyPress(keybind.FinalKey);
				return true;
			}

			for (int i = emulated.TotalKeysThisFrame - 1; i >= 0; i--) {
				int key = emulated.KeysThisFrame[i];

				if (emulated.WasKeyPressed(key))
					if (keyboardFocused.KeyPressedOccur(in emulated, key.ToButtonCode()))
						keyboard.ConsumeKeyPressAtIndex(i);

				if (emulated.WasKeyReleased(key))
					if (keyboardFocused.KeyReleasedOccur(in emulated, key.ToButtonCode()))
						keyboard.ConsumeKeyReleaseAtIndex(i);
			}

			for (int i = emulated.GetTextInputsThisFrame() - 1; i >= 0; i--)
				if (keyboardFocused.TextInputOccur(in emulated, emulated.GetTextInputThisFrameAtIndex(i)))
					keyboard.ConsumeTextAtIndex(i);

			if (keybindChainingAllowed && !keyboardFocused.HasFlag(ElementFlags.AllowChainKeybindingToParent))
				keybindChainingAllowed = false;
			if (keyboardFocused.HasFlag(ElementFlags.AllowChainInputToParent))
				keyboardFocused = keyboardFocused.GetParent();
			else
				return false;
		}

		return false;
	}
}

public class ElementThinkingSystem
{
	public void Think(Element? element, ref ElementSolveState state) {
		ThinkTraverse(element, ref state);
	}

	private void ThinkTraverse(Element? element, ref ElementSolveState state) {
		if (!IValidatable.IsValid(element)) return;

		element.Think();

		foreach (var child in element.GetChildren())
			if (child.IsVisible())
				ThinkTraverse(child, ref state);
	}
}

public enum ElementPaintPopupMode
{
	DontCare,
	NoPopups,
	OnlyPopups
}

public class ElementPaintSystem
{
	public void Paint(Element? element, ref ElementSolveState state, ElementPaintPopupMode skipPopups) => PaintTraverse(element, ref state, skipPopups);
	void PaintTraverse(Element? element, ref ElementSolveState state, ElementPaintPopupMode skipPopups) {
		if (!IValidatable.IsValid(element)) return;
		if (!element.IsVisible()) return;

		switch (skipPopups) {
			case ElementPaintPopupMode.OnlyPopups:
				if (!element.IsPopup() && element.GetParent() != null)
					return;
				if (element.IsPopup())
					skipPopups = ElementPaintPopupMode.DontCare;
				break;
			case ElementPaintPopupMode.NoPopups:
				if (element.IsPopup())
					return;
				break;
		}

		Element? parent = element.GetParent();

		if (element.BackdropAlpha > 0) {
			if (IValidatable.IsValid(parent)) {
				RectangleF size = parent.RenderBounds;
				Graphics2D.SetDrawColor(0, 0, 0, (int)float.Lerp(0, 100, (float)element.BackdropAlpha));
				Graphics2D.DrawRectangle(size.X, size.Y, size.W, size.H);
			}
		}

		RectangleF renderBounds = element.RenderBounds;

		if (element.IsUsingRenderTarget()) {
			// quick check if needing to create a new RT
			if (element.IsRenderTargetAvailable(out RenderTexture2D rt)) {
				var offset = Graphics2D.Offset;             // Store the offset so it can be restored later
				Graphics2D.ResetDrawingOffset();
				{
					Graphics2D.BeginRenderTarget(rt);
					PaintElement(element, ref state, skipPopups);
					Graphics2D.EndRenderTarget();
				}
				Graphics2D.OffsetDrawing(offset);           // Reset the offset now that rendering is complete

				if (IValidatable.IsValid(parent)) {
					Graphics2D.OffsetDrawing(element.ChildRenderOffset);
					if (parent.PostRenderChildRT(element) == true) {
						Graphics2D.OffsetDrawing(renderBounds.Pos);
						{
							element.PreRenderRT();
							var t = (byte)Math.Clamp(element.Opacity * 255, 0, 255);
							Graphics2D.SetDrawColor(t, t, t, t);
							Graphics2D.DrawRenderTexture(rt, renderBounds.Size);
							element.PostRenderRT();
						}
						Graphics2D.OffsetDrawing(-renderBounds.Pos);
					}
					Graphics2D.OffsetDrawing(-element.ChildRenderOffset);
				}
			}
			else
				Logs.Error("No render-target for element??");

			return;
		}

		Vector2F childRenderOffset = IValidatable.IsValid(parent) ? element.ChildRenderOffset : Vector2F.Zero;
		childRenderOffset = childRenderOffset.Round(5);

		Graphics2D.OffsetDrawing(childRenderOffset);
		{
			Graphics2D.OffsetDrawing(renderBounds.Pos);
			{
				PaintElement(element, ref state, skipPopups);
			}
			Graphics2D.OffsetDrawing(-renderBounds.Pos);
		}
		Graphics2D.OffsetDrawing(-childRenderOffset);
	}

	private void PaintElement(Element? element, ref ElementSolveState state, ElementPaintPopupMode skipPopups) {
		if (!IValidatable.IsValid(element)) return;
		var renderBounds = element.RenderBounds;
		float w = renderBounds.Width, h = renderBounds.Height;
		if ((w <= 0 || h <= 0) && element.Clipping) 
			return;

		if (element.Clipping)
			Graphics2D.ScissorRect(RectangleF.FromPosAndSize(Graphics2D.Offset - element.ChildRenderOffset, renderBounds.Size));
		{
			Graphics2D.PushAlpha(element.Opacity * 255);
			{
				// Calculate border insetting
				float iw = w, ih = h;
				float border = element.BorderSize;
				iw -= (border * 2);
				ih -= (border * 2);
				bool rounded = element.Roundness != 0;
				Vector2F drawingOffset = new(border);
				if ((iw > 0 && ih > 0) || !element.Clipping) {
					if (element.IsPaintBackgroundEnabled()) {
						if (rounded) // kinda hacky but required for border/background right now. Fixme
							element.PaintBackground(w, h);
						else {
							Graphics2D.OffsetDrawing(drawingOffset);
							element.PaintBackground(iw, ih);
							Graphics2D.OffsetDrawing(-drawingOffset);
						}
					}

					if (element.IsPaintEnabled()) {
						Graphics2D.OffsetDrawing(drawingOffset);
						element.Paint(iw, ih);
						Graphics2D.OffsetDrawing(-drawingOffset);
					}
				}

				if (element.IsPaintBorderEnabled())
					element.PaintBorder(w, h);

				foreach (Element child in element.GetChildren())
					PaintTraverse(child, ref state, skipPopups);

				if (element.IsPostChildPaintEnabled())
					element.PostChildPaint();
			}
			Graphics2D.PopAlpha();
		}
		if (element.Clipping)
			Graphics2D.ScissorRect();
	}
}

public class UserInterface : Element, IDisposable
{
	public UserInterface() : base(null) {
		UI = this;
		SetPaintBorderEnabled(false);
		SetPaintBackgroundEnabled(false);
		SetPaintEnabled(false);

		Input.OnClick += Input_OnClick;
	}

	private void Input_OnClick(Element? obj) {
		if (obj != GetKeyboardFocusedElement())
			SetKeyboardFocusedElement(null);
	}

	ElementSolveState SolveState;

	readonly List<Element> Popups = [];
	readonly List<Element> Modals = [];

	public readonly ElementSchemeSystem Scheme = new();
	public readonly ElementThinkingSystem Thinking = new();
	public readonly ElementInputSystem Input = new();
	public readonly ElementPaintSystem Painting = new();

	internal bool MakePopup(Element? element) {
		if (!IValidatable.IsValid(element)) return false;
		if (!Popups.Contains(element))
			Popups.Add(element);
		return true;
	}
	internal bool RemovePopup(Element? element) {
		if (!IValidatable.IsValid(element)) return false;
		return Popups.Remove(element);
	}
	internal bool MakeModal(Element? element) {
		if (!IValidatable.IsValid(element)) return false;
		if (!Modals.Contains(element))
			Modals.Add(element);
		return true;
	}
	internal bool RemoveModal(Element? element) {
		if (!IValidatable.IsValid(element)) return false;
		return Modals.Remove(element);
	}

	public ref ElementSolveState ProduceSolveState() {
		SolveState.EmbeddedPanel = this;
		return ref SolveState;
	}

	OSWindow? window;
	public OSWindow Window {
		get => window ?? throw new NullReferenceException();
		set => window = value ?? throw new NullReferenceException();
	}

	public override void PostChildPaint() {
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

	protected override void OnThink() {
		Clipping = false;
		//this.Position = new(0, 0);
		//this.Size = new(frameState.WindowWidth, frameState.WindowHeight);
		//RenderBounds = RectangleF.FromPosAndSize(this.Position, this.Size);
	}

	private string? _tooltipText;
	private bool disposedValue;

	public override string? TooltipText {
		get {
			if (IValidatable.IsValid(GetHoveredElement()) && GetHoveredElement() != this) {
				return GetHoveredElement()!.TooltipText;
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

	public Menu Menu() {
		return new Menu(this);
	}

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
				// trash the element
				this.Remove();
				trashElement(this);
				Popups.Clear();
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

	readonly List<Element> Elements = [];
	public ReadOnlySpan<Element> GetAllElements() => Elements.AsSpan();

	internal void AddElement(Element element) {
		if (!IValidatable.IsValid(element)) return;
		Elements.Add(element);
	}

	internal bool RemoveElement(Element element) {
		if (!IValidatable.IsValid(element)) return false;
		return Elements.Remove(element);
	}

	public Element? GetHoveredElement() => IValidatable.IsValid(SolveState.Hovered) ? SolveState.Hovered: null;
	public Element? GetDepressedElement(ButtonCode? code = null) {
		Element? ret = null;
		if (code.HasValue) {
			ret = SolveState.Depressed[(int)code.Value];
		}
		else {
			for (ButtonCode i = ButtonCode.MouseFirst; i < ButtonCode.MouseLast + 1; i++) {
				if (SolveState.Depressed[(int)(i - ButtonCode.MouseFirst)] != null)
					ret =  SolveState.Depressed[(int)(i - ButtonCode.MouseFirst)];
			}
		}
		return IValidatable.IsValid(ret) ? SolveState.Hovered : null;
	}
	public Element? GetKeyboardFocusedElement() => IValidatable.IsValid(SolveState.KeyboardFocused) ? SolveState.KeyboardFocused : null;

	ulong keyboardFocusReentrantID = 0;

	public bool SetKeyboardFocusedElement(Element? element) {
		ulong currentFunctionID = ++keyboardFocusReentrantID;

		Element? keyboardFocused = SolveState.KeyboardFocused;
		if (IValidatable.IsValid(keyboardFocused)) {
			if (!keyboardFocused.CanKeyboardFocusLostOccur(element))
				return false;
		}

		EngineCore.Window.StopTextInput();
		if (!IValidatable.IsValid(element)) {
			SolveState.KeyboardFocused = null;
			return true;
		}

		if (!element.CanKeyboardFocusGainedOccur(keyboardFocused, ref element))
			return false;

		if (keyboardFocusReentrantID != currentFunctionID)
			return false; // If another caller calls into this function in a keyboard focus hook, it would cause their focus to be
						  // lost. The intention of this check is to determine if a call happened in the hooks, and if so, to ignore
						  // the result to not immediately override it. Although you should just use the ref element if you can. 
						  // (or maybe we should just have the re-entrant check and nix the ref... todo)
		EngineCore.Window.StartTextInput();
		SolveState.KeyboardFocused = element;
		return true;
	}
}
