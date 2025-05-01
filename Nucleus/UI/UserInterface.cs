using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI.Elements;
using Raylib_cs;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Nucleus.UI
{
	public class UserInterface : Element, IDisposable
	{
		public Element? Focused;
		public Element? Hovered;
		public Element? Depressed;

		//public List<Element> Popups = [];
		public List<Element> Popups { get; private set; } = [];
		public List<Element> Elements { get; private set; } = [];
		public bool PopupActive => Popups.Count > 0;

		public void RemovePopup(Element e) {
			Popups.Remove(e);
		}

		public UserInterface() {
			Preprocess(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
		}

		protected override void Initialize() {
			UI = this;

			Preprocess(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
		}

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
				var font = "Noto Sans";
				var fontsize = 20;
				var size = Graphics2D.GetTextSize(text, font, fontsize) + new Vector2F(8, 4);
				var mousepos = EngineCore.CurrentFrameState.MouseState.MousePos + new Vector2F(8, 8 + 16);

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
				Graphics2D.DrawText((mousepos + drawingOffset) + new Vector2F(6, 4), text, font, fontsize);
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
			EngineCore.KeyboardUnfocus(this, true);
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
			var screen = Raylib.GetCurrentMonitor();
			var mpos = Raylib.GetMonitorPosition(screen).ToNucleus();
			var msize = new Vector2F(Raylib.GetMonitorWidth(screen), Raylib.GetMonitorHeight(screen));

			var mposCenter = mpos + (msize / 2);
			var mposFinal = mposCenter - new Vector2F(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f);
			EngineCore.SetWindowPosition(mposFinal);
		}
		~UserInterface() {
			MainThread.RunASAP(Remove);
		}

		public event MouseEventDelegate? OnElementClicked;
		public event MouseEventDelegate? OnElementReleased;

		public void TriggerElementClicked(Element? e, FrameState fs, Input.MouseButton mb) => OnElementClicked?.Invoke(e, fs, mb);
		public void TriggerElementReleased(Element? e, FrameState fs, Input.MouseButton mb) => OnElementReleased?.Invoke(e, fs, mb);
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
	}
}
