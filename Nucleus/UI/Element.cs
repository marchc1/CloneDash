using Newtonsoft.Json.Linq;
using Nucleus.Audio;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Raylib_cs;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using MouseButton = Nucleus.Input.MouseButton;

namespace Nucleus.UI
{
	public enum Dock
	{
		None,
		Top,
		Left,
		Right,
		Bottom,
		Fill
	}
	public class Element : IValidatable
	{
		public const bool FORCE_ROUNDED_RENDERBOUNDS = true;

		/// <summary>
		/// The <see cref="UserInterface"/> the element belongs to.
		/// </summary>
		public UserInterface UI { get; internal set; }

		/// <summary>
		/// Macros to <see cref="Level.Textures"/>.
		/// </summary>
		public TextureManagement Textures => Level.Textures;

		private Vector2F _position = new(0, 0);
		public float BorderSize { get; set; } = 2;
		public Vector2F Position {
			get { return _position; }
			set {
				if (value == _position)
					return;

				_position = value; InvalidateLayout();
			}
		}

		private Vector2F _size = new(32, 32);
		private bool _dynamicallySized = false;
		public bool DynamicallySized {
			get => _dynamicallySized;
			set {
				if (_dynamicallySized == value) return;

				_dynamicallySized = value;
				InvalidateChildren(recursive: true, self: true);
			}
		}
		public Vector2F Size {
			get { return _size; }
			set {
				if (value == _size)
					return;

				_size = value;
				if (Dock != Dock.None)
					InvalidateParentAndItsChildren();
				else
					InvalidateChildren(recursive: true, self: true);
			}
		}

		private Dock _dock = Dock.None;
		private RectangleF _dockMargin = RectangleF.Zero;
		private RectangleF _dockPadding = RectangleF.Zero;

		public static readonly Color DefaultBackgroundColor = new(20, 25, 32, 127);
		public static readonly Color DefaultForegroundColor = new(85, 95, 110, 255);
		public static readonly Color DefaultTextColor = new(230, 236, 255, 255);

		public Color BackgroundColor { get; set; } = DefaultBackgroundColor;
		public Color ForegroundColor { get; set; } = DefaultForegroundColor;
		public Color TextColor { get; set; } = DefaultTextColor;

		private bool __enabled = true;
		private bool __visible = true;
		private bool __inputDisabled = false;

		private bool __engineDisabled = false;
		private bool __engineInvisible = false;

		public bool EngineDisabled {
			get => __engineDisabled;
			set {
				__engineDisabled = value;
				if (__engineDisabled != value)
					InvalidateParentAndItsChildren();
			}
		}
		public bool EngineInvisible {
			get => __engineInvisible;
			set {
				__engineInvisible = value;
				if (__engineInvisible != value)
					InvalidateParentAndItsChildren();
			}
		}

		/// <summary>
		/// Disables all functionality and blocks rendering of this element. To only block rendering, see <see cref="Visible"/>.
		/// </summary>
		public bool Enabled {
			get { return __enabled && !__engineDisabled; }
			set {
				if (__enabled != value)
					InvalidateParentAndItsChildren();
				__enabled = value;
			}
		}
		/// <summary>
		/// Blocks rendering of this element. To block rendering alongside functionality, see <see cref="Enabled"/>.
		/// </summary>
		public bool Visible {
			get { return __visible && !__engineInvisible; }
			set {
				if (__visible != value)
					InvalidateParentAndItsChildren();
				__visible = value;
			}
		}

		public bool InputDisabled {
			get { return __inputDisabled; }
			set {
				if (value == false && __inputDisabled == true)
					KeyboardUnfocus();
				__inputDisabled = value;
			}
		}

		/// <summary>
		/// Docking; allows the element to dock to a side of its parent, or to dock completely and fill the parent.
		/// </summary>
		public Dock Dock {
			get { return _dock; }
			set {
				if (value == _dock)
					return;

				_dock = value;
				InvalidateParentAndItsChildren();
			}
		}
		/// <summary>
		/// The extra space left around <i>this</i> element when docked to something.<br></br>
		/// For the extra space left around this elements children when docked; see DockPadding.
		/// </summary>
		public RectangleF DockMargin {
			get { return _dockMargin; }
			set {
				if (_dockMargin == value)
					return;

				_dockMargin = value; 
				InvalidateParentAndItsChildren();
			}
		}
		/// <summary>
		/// The extra space left around this elements children (if the child is docked inside of this element).<br></br>
		/// For the extra space left around this element when docked; see DockMargin.
		/// </summary>
		public RectangleF DockPadding {
			get { return _dockPadding; }
			set {
				if (_dockPadding == value)
					return;

				InvalidateChildren(recursive: true, self: true);
				if (AddParent != this)
					AddParent.DockPadding = value;
				else
					_dockPadding = value;
			}
		}

		// These store an internal value for when we're setting up children for the first time/after a full-child update.
		// The elements individual value is stored in the equiv. Self value.
		protected float ChildrensDockingLayoutTop = 0;
		protected float ChildrensDockingLayoutLeft = 0;
		protected float ChildrensDockingLayoutRight = 0;
		protected float ChildrensDockingLayoutBottom = 0;

		// These store the internal value so when rebuilds happen they can use an old value.
		// They will be reset on full child updates (ie. InvalidateChildren)
		protected float? SelfDockingLayoutTop = null;
		protected float? SelfDockingLayoutLeft = null;
		protected float? SelfDockingLayoutRight = null;
		protected float? SelfDockingLayoutBottom = null;

		/// <summary>
		/// Resets the docking layout. This resets all ChildrensDockingLayout values on this element, and resets all childrens SelfDockingLayout values.
		/// </summary>
		public void ResetDockingLayout() {
			ChildrensDockingLayoutTop = 0;
			ChildrensDockingLayoutLeft = 0;
			ChildrensDockingLayoutRight = 0;
			ChildrensDockingLayoutBottom = 0;

			foreach (var child in Children) {
				child.SelfDockingLayoutTop = null;
				child.SelfDockingLayoutLeft = null;
				child.SelfDockingLayoutRight = null;
				child.SelfDockingLayoutBottom = null;
			}
		}

		protected float DockingLayoutTop = 0;
		protected float DockingLayoutLeft = 0;
		protected float DockingLayoutRight = 0;
		protected float DockingLayoutBottom = 0;


		public Vector2F SizeOfAllChildren { get; private set; } = Vector2F.Zero;
		public Vector2F ChildRenderOffset { get; set; } = Vector2F.Zero;

		private RectangleF __renderbounds = RectangleF.Zero;

		public virtual RectangleF RenderBounds {
			get {
				return __renderbounds;
			}
			protected set {
				__renderbounds = FORCE_ROUNDED_RENDERBOUNDS ? RectangleF.Round(value) : value;
			}
		}
		public RectangleF ScreenspaceRenderBounds {
			get {
				return RectangleF.FromPosAndSize(GetGlobalPosition(), __renderbounds.Size);
			}
		}

		/// <summary>
		/// Not recommended unless your use case involves a post-layout hook such as <see cref="PostLayoutChildren"/>
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public void SetRenderBounds(float? x = null, float? y = null, float? w = null, float? h = null) {
			if (x.HasValue) __renderbounds.X = x.Value;
			if (y.HasValue) __renderbounds.Y = y.Value;
			if (w.HasValue) __renderbounds.W = w.Value;
			if (h.HasValue) __renderbounds.H = h.Value;
		}
		public void SetRenderBounds(RectangleF bounds) {
			__renderbounds = FORCE_ROUNDED_RENDERBOUNDS ? RectangleF.Round(bounds) : bounds;
		}

		protected virtual void Initialize() { }

		protected Element() { }

		private Element? __parentToAddTo = null;

		/// <summary>
		/// The element which Add<>() adds to. Can be used to defer add operations to a different part of the element.<br></br>
		/// By default, returns itself.
		/// </summary>
		public Element AddParent {
			get {
				if (__parentToAddTo == null) {
					return this;
				}
				return __parentToAddTo;
			}
			set {
				__parentToAddTo = value;
				if (value != null) {
					value.DockPadding = DockPadding;
				}
			}
		}

		// Avoid overriding this unless needed
		public virtual T Add<T>(T? toAdd = null) where T : Element {
			return Create<T>(AddParent, toAdd);
		}

		public virtual void Add<T>([NotNull] out T? addInto) where T : Element => addInto = Add<T>();

		public static T Create<T>(Element? parent = null, T? ret = null) where T : Element {
			ret = ret ?? (T?)Activator.CreateInstance(typeof(T)) ?? throw new Exception("A fatal exception occured during element creation.");

			if (parent == null) {
				ret.Initialize();
				return ret;
			}

			ret.UI = parent.UI;
			parent.AddChild(ret);
			ret.UI.Elements.Add(ret);
			ret.Initialize();
			parent.TriggerOnChildParented(parent, ret);
			return ret;
		}

		#region Generic element methods

		private bool __markedForRemoval = false;
		public virtual void OnRemoval() { }
		public delegate void RemoveDelegate(Element e);
		public event RemoveDelegate? Removed;

		private void REMOVE() {
			if (__markedForRemoval == true)
				return;
			OnRemoval();
			Removed?.Invoke(this);

			__markedForRemoval = true;
			if (IsPopup) {
				UI.RemovePopup(this);
			}

			UI.Elements.Remove(this);
			foreach (Element element in this.Children.ToArray())
				element.REMOVE();
		}
		public void Remove() {
			REMOVE();

			// Call parent methods
			if (IValidatable.IsValid(Parent)) {
				Parent.Children.Remove(this);
				Parent.InvalidateLayout();
			}
		}

		public bool IsValid() => !__markedForRemoval;

		public delegate void ChildParentedDelegate(Element parent, Element child);
		public event ChildParentedDelegate? OnChildParented;
		public virtual void ChildParented(Element parent, Element child) { }
		public void TriggerOnChildParented(Element parent, Element child) {
			ChildParented(parent, child);
			OnChildParented?.Invoke(parent, child);
		}

		protected virtual void OnThink(FrameState frameState) { }
		private bool _firstThink = true;
		public void Think(FrameState frameState) {
			if (_firstThink) {
				_firstThink = false;
				Birth = DateTime.Now;
			}

			OnThink(frameState);
		}
		#endregion
		#region Parenting system
		public Element Parent { get; internal set; }
		internal List<Element> Children = [];

		/// <summary>
		/// Returns all children of this element. Does not allow modification of the elements children; use AddChild/SetParent functionality for that.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Element> GetChildren() {
			foreach (var child in Children)
				yield return child;
		}

		public void AddChild(Element p) {
			if (p.Parent != null) {
				p.Parent.Children.Remove(p);
				p.Parent.InvalidateLayout();
				p.Parent = null;
			}

			if (p != null) {
				p.Parent = this;
				Children.Add(p);
				InvalidateLayout();
			}

			p?.InvalidateLayout();
			p?.Parent?.TriggerOnChildParented(p.Parent, p);
		}

		public void SetParent(Element p) {
			if (Parent != null)                 // if current parent isn't null
				Parent.Children.Remove(this);

			Parent = p;                   // set parent to P

			if (p != null) {                    // if new parent isn't just null, add it to its children
				p.Children.Add(this);
				p.InvalidateLayout();
			}

			InvalidateLayout();
			p?.TriggerOnChildParented(p, this);
		}

		public void SortChildren(Comparison<Element> childSortMethod) {
			this.AddParent.Children.Sort(childSortMethod);
		}
		#endregion
		#region Layout control
		public double LastLayoutTime { get; private set; } = 0;

		private bool __layoutinvalid = true;
		public bool LayoutInvalidated {
			get => __layoutinvalid;
			protected set {
				if (__layoutinvalid == true && value == false)
					LastLayoutTime = Level.Curtime;

				__layoutinvalid = value;
			}
		}

		public void InvalidateChildren(bool immediate = false, bool recursive = false, bool self = false) {
			if (self)
				InvalidateLayout(immediate);
			ResetDockingLayout();
			foreach (Element e in Children) {
				e.InvalidateLayout(immediate);
				if (recursive)
					e.InvalidateChildren(immediate, recursive);
			}
		}

		/// <summary>
		/// Internal method to cancel any layout invalidations.
		/// </summary>
		public void RevalidateLayout() {
			LayoutInvalidated = false;
		}
		/// <summary>
		/// Invalidates the layout, registering it for a rebuild with the layout system.
		/// </summary>
		/// <param name="immediate"></param>
		public void InvalidateLayout(bool immediate = false) {
			ChildrensDockingLayoutTop = ChildrensDockingLayoutLeft = ChildrensDockingLayoutRight = ChildrensDockingLayoutBottom = 0;

			if (immediate) {
				var fs = Level.FrameState;
				LayoutRecursive(this, ref fs);
			}
			else {
				LayoutInvalidated = true;
			}
		}

		public void ValidateLayout() {
			if (LayoutInvalidated)
				SetupLayout();

			LayoutInvalidated = false;
		}
		/// <summary>
		/// Alias of calling <see cref="InvalidateLayout(bool)"/> on <seealso cref="Parent"/>
		/// </summary>
		/// <param name="immediate"></param>
		public void InvalidateParent(bool immediate = false) {
			if (Parent != null)
				Parent.InvalidateLayout(immediate);
		}
		public void InvalidateParentAndItsChildren(bool immediate = false) {
			if (Parent == null) return;

			Parent.InvalidateChildren(immediate, true, true);
		}
		protected virtual void PerformLayout(float width, float height) { }
		public delegate void PostLayoutChildrenD(Element self);
		public event PostLayoutChildrenD? OnPostLayoutChildren;
		protected virtual void PostLayoutChildren() { }
		/// <summary>
		/// Called before calculating a childs layout.
		/// </summary>
		/// <param name="element"></param>
		protected virtual void PreLayoutChild(Element element) { }
		/// <summary>
		/// Called when a childs layout is complete.
		/// </summary>
		/// <param name="element"></param>
		protected virtual void PostLayoutChild(Element element) { }
		/// <summary>
		/// Called before calculating childrens layout.
		/// </summary>
		protected virtual void PreLayoutChildren() { }
		protected virtual void ModifyLayout(ref RectangleF renderBounds) { }


		private bool __usesRenderTarget = false;
		private RenderTexture2D? __RT1 = null;
		private RenderTexture2D? __RT2 = null;
		private bool UseRT2 = false;
		private RectangleF? __lastRTSize = null;

		/// <summary>
		/// Renders this element and all of its children to a render-target rather than straight to the screen every frame.<br></br>
		/// This can be used for FPS limiting and for special effects on some elements.
		/// </summary>
		public bool UsesRenderTarget {
			get {
				return __usesRenderTarget;
			}
			set {
				if (value == __usesRenderTarget)
					return;

				__usesRenderTarget = value;
				if (value == false) {
					if (__RT1.HasValue)
						Raylib.UnloadRenderTexture(__RT1.Value);

					__RT1 = null;
					__lastRTSize = null;
				}
				else {

				}
			}
		}
		/// <summary>
		/// Only applicable when <see cref="UsesRenderTarget"/> is set to <see langword="true"/>, otherwise does not apply.<br></br>
		/// Not yet implemented
		/// </summary>
		public int RenderFPS { get; set; } = 60;

		public virtual void RenderRenderTarget() {

		}

		/// <summary>
		/// Don't override this unless you 100% know what you're doing. This is NOT the same as PerformLayout. <br></br>
		/// This method performs the calculations for this panels docking offsets, etc...<br></br>
		/// It's only exposed for elements (such as the root UserInterface) to modify their logic.
		/// </summary>
		internal virtual void SetupLayout() {
			ChildrensDockingLayoutTop = 0;
			ChildrensDockingLayoutLeft = 0;
			ChildrensDockingLayoutRight = 0;
			ChildrensDockingLayoutBottom = 0;


			var layoutSize = _size;
			if (_dynamicallySized && Parent != null) {
				var parentSize = Parent.RenderBounds.Size;
				layoutSize = _size * parentSize;
			}

			__renderbounds = RectangleF.FromPosAndSize(_position, layoutSize);

			if (FORCE_ROUNDED_RENDERBOUNDS)
				__renderbounds.RoundInPlace();

			ModifyLayout(ref __renderbounds);
			//PerformLayout(_size.w, _size.h); used to be here...

			RectangleF currentBounds = __renderbounds;
			if (Dock != Dock.None) {
				var dT = SelfDockingLayoutTop ?? Parent.ChildrensDockingLayoutTop;
				var dL = SelfDockingLayoutLeft ?? Parent.ChildrensDockingLayoutLeft;
				var dR = SelfDockingLayoutRight ?? Parent.ChildrensDockingLayoutRight;
				var dB = SelfDockingLayoutBottom ?? Parent.ChildrensDockingLayoutBottom;

				SelfDockingLayoutTop = dT;
				SelfDockingLayoutLeft = dL;
				SelfDockingLayoutRight = dR;
				SelfDockingLayoutBottom = dB;

				float parentWidth = 0, parentHeight = 0;
				float childWidth = currentBounds.W, childHeight = currentBounds.H;

				if (!IValidatable.IsValid(Parent)) {
					parentWidth = UI.Size.W;
					parentHeight = UI.Size.H;
				}
				else {
					parentWidth = Parent.RenderBounds.Width;
					parentHeight = Parent.RenderBounds.Height;
				}

				switch (Dock) {
					case Dock.Top:
						currentBounds.X = dL;
						currentBounds.Y = dT;
						currentBounds.W = (parentWidth - dR) - dL;
						currentBounds.H = childHeight;
						Parent.ChildrensDockingLayoutTop += childHeight;
						break;
					case Dock.Left:
						currentBounds.X = dL;
						currentBounds.Y = dT;
						currentBounds.W = childWidth;
						currentBounds.H = (parentHeight - dT) - dB;
						Parent.ChildrensDockingLayoutLeft += childWidth;
						break;
					case Dock.Right:
						currentBounds.X = (parentWidth - childWidth) - dR;
						currentBounds.Y = dT;
						currentBounds.W = childWidth;
						currentBounds.H = (parentHeight - dB) - dT;
						Parent.ChildrensDockingLayoutRight += childWidth;
						break;
					case Dock.Bottom:
						currentBounds.X = dL;
						currentBounds.Y = (parentHeight - childHeight) - dB;
						currentBounds.W = (parentWidth - dL) - dR;
						currentBounds.H = childHeight;
						Parent.ChildrensDockingLayoutBottom += childHeight;
						break;
					case Dock.Fill:
						currentBounds.X = dL;
						currentBounds.Y = dT;
						currentBounds.W = (parentWidth - dL) - dR;
						currentBounds.H = (parentHeight - dT) - dB;
						break;
				}

				if (!DockMargin.IsZero) {
					currentBounds.X += DockMargin.Left;
					currentBounds.W -= DockMargin.Left;
					currentBounds.W -= DockMargin.Right;

					currentBounds.Y += DockMargin.Top;
					currentBounds.H -= DockMargin.Top;
					currentBounds.H -= DockMargin.Bottom;
				}

				if (IValidatable.IsValid(Parent) && !Parent.DockPadding.IsZero) {
					switch (Dock) {
						case Dock.Top:
							currentBounds.X += Parent.DockPadding.Left;
							currentBounds.W -= Parent.DockPadding.Left;
							currentBounds.W -= Parent.DockPadding.Right;

							currentBounds.Y += Parent.DockPadding.Top;
							currentBounds.H -= Parent.DockPadding.Top;
							currentBounds.H -= Parent.DockPadding.Bottom;
							break;
						case Dock.Left:
							currentBounds.X += Parent.DockPadding.Left;
							currentBounds.W -= Parent.DockPadding.Left;
							currentBounds.W -= Parent.DockPadding.Right;

							currentBounds.Y += Parent.DockPadding.Top;
							currentBounds.H -= Parent.DockPadding.Top;
							currentBounds.H -= Parent.DockPadding.Bottom;
							break;
						case Dock.Right:
							currentBounds.X += Parent.DockPadding.Left;
							currentBounds.W -= Parent.DockPadding.Left;
							currentBounds.W -= Parent.DockPadding.Right;

							currentBounds.Y += Parent.DockPadding.Top;
							currentBounds.H -= Parent.DockPadding.Top;
							currentBounds.H -= Parent.DockPadding.Bottom;
							break;
						case Dock.Bottom:
							currentBounds.X += Parent.DockPadding.Left;
							currentBounds.W -= Parent.DockPadding.Left;
							currentBounds.W -= Parent.DockPadding.Right;

							currentBounds.Y += Parent.DockPadding.Top;
							currentBounds.H -= Parent.DockPadding.Top;
							currentBounds.H -= Parent.DockPadding.Bottom;
							break;
						case Dock.Fill:
							currentBounds.X += Parent.DockPadding.Left;
							currentBounds.W -= Parent.DockPadding.Left;
							currentBounds.W -= Parent.DockPadding.Right;

							currentBounds.Y += Parent.DockPadding.Top;
							currentBounds.H -= Parent.DockPadding.Top;
							currentBounds.H -= Parent.DockPadding.Bottom;
							break;
					}
				}
			}
			else if (Origin != Anchor.TopLeft || Anchor != Anchor.TopLeft) {
				var np = Origin.CalculatePosition(currentBounds.Pos, currentBounds.Size, true);
				var npO = Anchor.CalculatePosition(new(0, 0), Parent.RenderBounds.Size, false);
				currentBounds.Pos = npO + np;
			}

			if (_fitToParent) {
				var parentBounds = Parent?.RenderBounds ?? UI.RenderBounds;
				var overflow = parentBounds.GetOverflow(currentBounds, fitPadding);
				currentBounds.Pos += overflow;
				_fitToParent = false;
			}

			if (FORCE_ROUNDED_RENDERBOUNDS)
				currentBounds.RoundInPlace();

			RenderBounds = currentBounds;

			LayoutInvalidated = false;
			PerformLayout(RenderBounds.Width, RenderBounds.Height);
		}

		public bool Parented => Parent != null;
		public bool HasChildren => Children.Count > 0;

		public bool IsPopup { get; private set; }
		public void MakePopup() {
			IsPopup = true;
			UI.Popups.Add(this);
		}

		// NOTE: the three checks in these MoveToX methods confirm the following conditions are not true:
		// - there is not an immediate parent (almost always false, but worth confirming if you tried UI.MoveToFront() or something stupid)
		// - there is only one child (== 1 is valid because if Count was 0, then that would mean this element isn't a child, in which case something went horribly wrong anyway)
		// - is the element already at the front/back

		// and if any of the three conditions are met, it breaks out, since that would be an invalid state for these methods to work anyway
		// (and avoids unnecessary layout recalcs for the 3rd condition)
		public void MoveToFront() {
			if (Parent == null || Parent.Children.Count == 1 || Parent.Children.Last() == this)
				return;

			Parent.Children.Remove(this);
			Parent.Children.Add(this);
			Parent.InvalidateLayout();
		}
		public void MoveToBack() {
			if (Parent == null || Parent.Children.Count == 1 || Parent.Children.First() == this)
				return;

			Parent.Children.Remove(this);
			Parent.Children.Insert(0, this);
			Parent.InvalidateLayout();
		}
		#endregion
		#region Rendering/visuals
		public virtual void Paint(float width, float height) {
			ImageDrawing();
		}
		public delegate void PaintEvent(Element self, float width, float height);
		public event PaintEvent? PaintOverride;

		private string __text = "Panel";
		public string TextNocall {
			set {
				__text = value;
			}
		}
		public string Text {
			get {
				return __text;
			}
			set {
				if (value == __text)
					return;

				var oldText = __text;
				__text = value;
				TextChanged(oldText, value);
				TextChangedEvent?.Invoke(this, oldText, __text);
			}
		}
		public virtual void TextChanged(string oldText, string newText) { }
		public delegate void TextChangedDelegate(Element self, string oldText, string newText);
		public event TextChangedDelegate? TextChangedEvent;

		public string Font { get; set; } = "Noto Sans";
		public float TextSize { set; get; } = 18;

		public bool Clipping { get; set; } = true;
		#endregion

		// The element cycle

		public static int LayoutRecursive(Element element, ref FrameState frameState) {
			if (!element.Enabled) return 0;

			int returning = 0;

			if (element is UserInterface ui)
				ui.Preprocess(frameState.WindowWidth, frameState.WindowHeight);

			element.SizeOfAllChildren = Vector2F.Zero;
			var wasInvalid = element.LayoutInvalidated;
			if (element.LayoutInvalidated) {
				element.SetupLayout();

				returning += 1;
			}
			foreach (Element child in element.Children.ToArray()) {
				returning += LayoutRecursive(child, ref frameState);
				if (child.Enabled) {
					var ps = (child.RenderBounds.Pos + child.RenderBounds.Size);
					if (ps > element.SizeOfAllChildren)
						element.SizeOfAllChildren = ps;
				}
				else {
					child.RenderBounds = RectangleF.Zero;
				}
			}
			if (wasInvalid) {
				element.OnPostLayoutChildren?.Invoke(element);
				element.PostLayoutChildren();
			}

			return returning;
		}

		public delegate bool HoverTestDelegate(Element self, RectangleF bounds, Vector2F mousePos);
		public event HoverTestDelegate? OnHoverTest;
		public virtual bool HoverTest(RectangleF bounds, Vector2F mousePos) {
			var containsPoint = bounds.ContainsPoint(mousePos);
			if (containsPoint && IValidatable.IsValid(Parent)) {
				var scissor = RectangleF.FromPosAndSize(Parent.GetGlobalPosition() - Parent.ChildRenderOffset, Parent.RenderBounds.Size);
				return scissor.ContainsPoint(mousePos);
			}

			return containsPoint;
		}
		public static Element? ResolveElementHoveringState(Element element, FrameState frameState, Vector2F offset, RectangleF lastBounds, Element? lastHovered = null, bool popupActive = false) {
			if (!element.Enabled) return lastHovered;
			if (!element.Visible) return lastHovered;
			if (!element.CanInput()) return lastHovered;

			if (element.Parent != null)
				offset += element.Parent.ChildRenderOffset;

			var boundsOfSelf = lastBounds.FitInto(element.RenderBounds.AddPosition(offset));

			if (popupActive || (element is UserInterface ui && ui.PopupActive)) {
				if (element == element.UI.Popups.Last())
					popupActive = false;
				else
					popupActive = true;

				offset += element.RenderBounds.Pos;

				foreach (Element child in element.Children)
					lastHovered = ResolveElementHoveringState(child, frameState, offset, boundsOfSelf, lastHovered, popupActive);

				return lastHovered;
			}

			var bounds = element.RenderBounds.AddPosition(offset);

			if (element.OnHoverTest == null) {
				if (element.HoverTest(bounds, frameState.Mouse.MousePos))
					lastHovered = element;
			}
			else if (element.OnHoverTest(element, bounds, frameState.Mouse.MousePos))
				lastHovered = element;

			offset += element.RenderBounds.Pos;

			foreach (Element child in element.Children)
				lastHovered = ResolveElementHoveringState(child, frameState, offset, boundsOfSelf, lastHovered);

			return lastHovered;
		}
		public delegate void ElementSingleArg(Element self);
		public event ElementSingleArg? Thinking;

		public static void ThinkRecursive(Element element, FrameState frameState) {
			if (!element.Enabled) return;

			element.Think(frameState);
			element.Thinking?.Invoke(element);

			element.Children.RemoveAll(x => x.__markedForRemoval);
			foreach (Element child in element.Children.ToArray())
				ThinkRecursive(child, frameState);
		}

		~Element() {
			if (__RT1.HasValue) {
				MainThread.RunASAP(() => {
					Raylib.UnloadRenderTexture(__RT1.Value);
				});
			}
			//OnRemoval();
		}

		public virtual string? TooltipText { get; set; }

		public virtual void PreRender() { }
		public virtual void PostRender() { }
		public virtual void PostRenderChildren() { }
		public virtual bool PostRenderChildRT(Element element) => true;

		public float Opacity { get; set; } = 1.0f;

		public static void DrawRecursive(Element element, int iteration = 0) {
			if (!element.Enabled) return;
			if (!element.Visible) return;
			if (element.IsPopup) {
				Raylib.DrawRectangle(0, 0, (int)element.UI.Size.X, (int)element.UI.Size.Y, new(0, 0, 0,
					(int)NMath.Remap(element.Lifetime, 0, 0.2f, 0, 100, clampOutput: true)
					));
			}
			if (element.UsesRenderTarget) {
				// quick check if needing to create a new RT
				if (!element.__lastRTSize.HasValue || element.RenderBounds != element.__lastRTSize) {
					if (element.__RT1.HasValue) Raylib.UnloadRenderTexture(element.__RT1.Value);
					if (element.__RT2.HasValue) Raylib.UnloadRenderTexture(element.__RT2.Value);

					element.__RT1 = Graphics2D.CreateRenderTarget(element.RenderBounds.W, element.RenderBounds.H);
					element.__lastRTSize = element.RenderBounds;
				}
				if (element.__RT1.HasValue) {
					var offset = Graphics2D.Offset;             // Store the offset so it can be restored later
					Graphics2D.ResetDrawingOffset();
					Graphics2D.BeginRenderTarget(element.__RT1.Value);

					if (element.PaintOverride != null)
						element.PaintOverride.Invoke(element, element.RenderBounds.Width, element.RenderBounds.Height);
					else
						element.Paint(element.RenderBounds.Width, element.RenderBounds.Height);

					foreach (Element child in element.Children)
						DrawRecursive(child, iteration + 1);

					Graphics2D.EndRenderTarget();
					Graphics2D.OffsetDrawing(offset);           // Reset the offset now that rendering is complete
					if (IValidatable.IsValid(element.Parent)) {
						Graphics2D.OffsetDrawing(element.ChildRenderOffset);

						if (element.Parent.PostRenderChildRT(element) == true) {
							Graphics2D.OffsetDrawing(element.RenderBounds.Pos);
							element.PreRender();
							var t = (byte)Math.Clamp(element.Opacity * 255, 0, 255);
							Graphics2D.SetDrawColor(t, t, t, t);
							Graphics2D.DrawRenderTexture(element.__RT1.Value, element.RenderBounds.Size);
							element.PostRender();
						}

						Graphics2D.OffsetDrawing(-element.ChildRenderOffset);
					}
					Graphics2D.OffsetDrawing(-element.RenderBounds.Pos);
				}
				else
					Logs.Error("No render-target for element??");

				return;
			}

			if (IValidatable.IsValid(element.Parent))
				Graphics2D.OffsetDrawing(element.ChildRenderOffset);
			Graphics2D.OffsetDrawing(element.RenderBounds.Pos);

			if (element.Clipping)
				Graphics2D.ScissorRect(RectangleF.FromPosAndSize(Graphics2D.Offset - element.ChildRenderOffset, element.RenderBounds.Size)); // ?
																																			 //else
																																			 //Graphics2D.ScissorRect();

			element.PreRender();
			if (element.PaintOverride != null)
				element.PaintOverride?.Invoke(element, element.RenderBounds.Width, element.RenderBounds.Height);
			else
				element.Paint(element.RenderBounds.Width, element.RenderBounds.Height);
			element.PostRender();


			foreach (Element child in element.Children)
				DrawRecursive(child, iteration + 1);
			element.PostRenderChildren();

			if (element.Clipping)
				Graphics2D.ScissorRect();

			//Graphics2D.DrawText(new(0, 0), $"Pos: {element.RenderBounds.Pos}", "Arial", 20);

			Graphics2D.OffsetDrawing(-element.RenderBounds.Pos);
			if (IValidatable.IsValid(element.Parent))
				Graphics2D.OffsetDrawing(-element.ChildRenderOffset);

			/*if (element.Hovered && element.Parent != null) {
                Graphics2D.SetDrawColor(100, 255, 100, 50);
                Graphics2D.DrawRectangle(element.RenderBounds);
                Graphics2D.SetDrawColor(Color.WHITE);
            }*/
		}

		public delegate void MouseEventDelegate(Element self, FrameState state, MouseButton button);
		public delegate void MouseReleaseDelegate(Element self, FrameState state, MouseButton button, bool lost);
		public delegate void MouseV2Delegate(Element self, FrameState state, Vector2F delta);

		public event MouseEventDelegate? MouseClickEvent;
		public virtual void MouseClick(FrameState state, MouseButton button) { EngineCore.KeyboardUnfocus(this, true); }

		public Dictionary<string, object> Tags { get; } = [];
		public T GetTag<T>(string key) => (T)Tags[key];
		public T? GetTagSafely<T>(string key) => Tags.ContainsKey(key) ? (T)Tags[key] : default(T);
		public void SetTag<T>(string key, T value) => Tags[key] = value;
		public void UnsetTag<T>(string key) => Tags.Remove(key);


		public event MouseEventDelegate MouseReleaseEvent;
		public virtual void MouseRelease(Element self, FrameState state, MouseButton button) { }

		public event MouseEventDelegate? MouseLostEvent;
		public virtual void MouseLost(Element self, FrameState state, MouseButton button) { }
		public event MouseReleaseDelegate? MouseReleasedOrLostEvent;
		public virtual void MouseReleasedOrLost(Element self, FrameState state, MouseButton button) { }

		public event MouseV2Delegate? MouseDragEvent;
		public virtual void MouseDrag(Element self, FrameState state, Vector2F delta) { }

		public event MouseV2Delegate? MouseScrollEvent;
		public virtual void MouseScroll(Element self, FrameState state, Vector2F delta) { }

		public void ClearChildren() {
			foreach (var child in this.AddParent.Children.ToArray()) {
				child.Remove();
			}
			this.AddParent.Children.Clear();
			InvalidateLayout();
		}
		public void ClearChildrenNoRemove() {
			this.AddParent.Children.Clear();
			InvalidateLayout();
		}
		public bool Hovered => UI.Hovered == this;
		public bool Depressed { get; internal set; }
		public bool Dragged { get; internal set; } = false;
		public Vector2F DragVector { get; internal set; } = Vector2F.Zero;

		internal void MouseClickOccur(FrameState state, MouseButton button) {
			Depressed = true;
			MouseClick(state, button);
			MouseClickEvent?.Invoke(this, state, button);
			UI.TriggerElementClicked(this, state, button);
		}
		internal void MouseReleaseOccur(FrameState state, MouseButton button, bool forced = false) {
			Depressed = false;

			if (!Hovered && !forced)
				return;

			MouseRelease(this, state, button);
			MouseReleaseEvent?.Invoke(this, state, button);

			MouseReleasedOrLost(this, state, button);
			MouseReleasedOrLostEvent?.Invoke(this, state, button, false);

			Dragged = false;
			DragVector = Vector2F.Zero;
			UI.TriggerElementReleased(this, state, button);
		}
		internal void MouseLostOccur(FrameState state, MouseButton button, bool forced = false) {
			Depressed = false;

			MouseLost(this, state, button);
			MouseLostEvent?.Invoke(this, state, button);

			MouseReleasedOrLost(this, state, button);
			MouseReleasedOrLostEvent?.Invoke(this, state, button, true);

			Dragged = false;
			DragVector = Vector2F.Zero;
		}
		internal void MouseDragOccur(FrameState state, Vector2F delta) {
			MouseDrag(this, state, delta);
			MouseDragEvent?.Invoke(this, state, delta);
		}
		internal void MouseScrollOccur(FrameState state, Vector2F delta) {
			MouseScroll(this, state, delta);
			MouseScrollEvent?.Invoke(this, state, delta);
		}

		public bool ConsumedScrollEvent { get; internal set; }
		public void ConsumeScrollEvent() => ConsumedScrollEvent = true;

		public static Color MixColorBasedOnMouseState(Element e, Color original, Vector4 hoveredHSV, Vector4 depressedHSV) {
			return MixColorBasedOnMouseState(e.Hovered ? 1 : 0, e.Depressed ? 1 : 0, original, hoveredHSV, depressedHSV);
		}
		/// <summary>
		/// This function expects HSVA in the format of hueAdditional, saturationMultiplied, valueMultiplied, alphaMultiplied
		/// </summary>
		public static Color MixColorBasedOnMouseState(float hoverRatio, float depressedRatio, Color original, Vector4 hoveredHSVA, Vector4 depressedHSVA) {
			var originalHSV = original.ToHSV();

			var hoveredColor = ColorExtensions.FromHSV(originalHSV.X + hoveredHSVA.X, originalHSV.Y * hoveredHSVA.Y, originalHSV.Z * hoveredHSVA.Z);
			hoveredColor.A = (byte)Math.Clamp(original.A * hoveredHSVA.W, 0, 255);

			var depressedColor = ColorExtensions.FromHSV(originalHSV.X + depressedHSVA.X, originalHSV.Y * depressedHSVA.Y, originalHSV.Z * depressedHSVA.Z);
			depressedColor.A = (byte)Math.Clamp(hoveredColor.A * depressedHSVA.W, 0, 255);

			return NMath.LerpColor(depressedRatio, NMath.LerpColor(hoverRatio, original, hoveredColor), depressedColor);
		}

		public DateTime Birth { get; private set; } = DateTime.Now;
		public float Lifetime => (float)(DateTime.Now - Birth).TotalSeconds;

		public virtual void Center() {
			ValidateLayout();
			var parentBounds = Parent.RenderBounds;
			var pb2 = new Vector2F(parentBounds.Width / 2, parentBounds.Height / 2);
			var tb2 = new Vector2F(RenderBounds.Width / 2, RenderBounds.Height / 2);
			this.Position = pb2 - tb2;
			//InvalidateLayout();
			//InvalidateChildren(self: true, recursive: true);
		}

		public virtual void KeyboardFocusGained(bool demanded) {

		}

		public virtual void KeyboardFocusLost(Element lostTo, bool demanded) {

		}

		/// <summary>
		/// Requests keyboard focus from the engine. Keyboard events are then able to be sent to this element.<br></br>
		/// Will silently fail if an element demanded keyboard focus, see <see cref="DemandKeyboardFocus"/>
		/// </summary>
		public virtual void RequestKeyboardFocus() => EngineCore.RequestKeyboardFocus(this);
		/// <summary>
		/// Demands keyboard focus from the engine, which blocks RequestKeyboardFocus from working until KeyboardUnfocus is called from the element.<br></br>
		/// An example use case where the difference matters; say you want to request keyboard focus when hovering over some elements in an editor. But when a text box needs <br></br>
		/// keyboard focus, you dont want hovering over something else to cause the textbox to lose focus; in this case, you'd demand keyboard focus from the textbox to avoid that.<br></br>
		/// Note that demands don't respect demands.
		/// </summary>
		public virtual void DemandKeyboardFocus() => EngineCore.DemandKeyboardFocus(this);
		public virtual void KeyboardUnfocus() => EngineCore.KeyboardUnfocus(this);

		public IKeyboardInputMarshal KeyboardInputMarshal { get; set; } = DefaultKeyboardInputMarshal.Instance;

		public void KeyPressedOccur(KeyboardState keyboardState, Input.KeyboardKey key) {
			KeyPressed(keyboardState, key);
			OnKeyPressed?.Invoke(this, keyboardState, key);
		}
		public void KeyReleasedOccur(KeyboardState keyboardState, Input.KeyboardKey key) {
			KeyReleased(keyboardState, key);
			OnKeyReleased?.Invoke(this, keyboardState, key);
		}

		public virtual void KeyPressed(KeyboardState keyboardState, Input.KeyboardKey key) { }
		public virtual void KeyReleased(KeyboardState keyboardState, Input.KeyboardKey key) { }

		public delegate void KeyDelegate(Element self, KeyboardState state, Input.KeyboardKey key);
		public event KeyDelegate? OnKeyPressed;
		public event KeyDelegate? OnKeyReleased;

		public bool IsIndirectChildOf(Element parent) {
			var p = this;

			while (p != null) {
				p = p.Parent;
				if (p == parent)
					return true;
			}

			return false;
		}

		public bool KeyboardFocused => EngineCore.KeyboardFocusedElement == this;

		public KeybindSystem Keybinds { get; } = new();
		public Anchor Anchor { get; set; } = Anchor.TopLeft;
		public Anchor Origin { get; set; } = Anchor.TopLeft;

		public Texture? Image { get; set; }
		public ImageOrientation ImageOrientation { get; set; } = ImageOrientation.None;

		public Vector2F ImagePadding { get; set; } = new(0);
		public float ImageRotation { get; set; } = 0;
		public bool ImageFlipX { get; set; } = false;
		public bool ImageFlipY { get; set; } = false;
		public Vector2F GetGlobalPosition() {
			Vector2F ret = new Vector2F(0, 0);
			Element? t = this;
			while (true) {
				ret += t.RenderBounds.Pos + t.ChildRenderOffset;
				t = t.Parent;
				if (t == null || t == t.UI) {
					break;
				}
			}
			return ret;
		}

		public bool ImageFollowsText { get; set; } = false;
		public Color? ImageColor { get; set; } = null;
		public void ImageDrawing(Vector2F? pos = null, Vector2F? size = null) {
			if (Image == null)
				return;

			var offset = Graphics2D.Offset + (pos ?? new Vector2F(0));
			var bounds = RenderBounds;
			if (size != null) {
				bounds.W = size.Value.X;
				bounds.H = size.Value.Y;
			}

			Rectangle sourceRect = new(0, 0, Image.Width, Image.Height);
			Rectangle destRect = new(offset.X, offset.Y, Image.Width, Image.Height);
			var scldiv2 = RenderBounds.Size / 2;

			var width = RenderBounds.Size.W;
			var height = RenderBounds.Size.H;

			switch (ImageOrientation) {
				case ImageOrientation.None:
					destRect.X += pos?.X ?? 0;
					destRect.Y += pos?.Y ?? 0;
					destRect.Width = size?.X ?? destRect.Width;
					destRect.Height = size?.Y ?? destRect.Height;
					break;
				case ImageOrientation.Centered:
					var x = (bounds.Width / 2) - (Image.Width / 2);
					var y = (bounds.Height / 2) - (Image.Height / 2);
					destRect.X += x;
					destRect.Y += y;
					break;
				case ImageOrientation.Stretch:
					destRect.Width = width;
					destRect.Height = height;
					break;
				case ImageOrientation.Zoom:
					if (width <= height) { // Width is the bottleneck
						var ratio = Image.Height / Image.Width;
						destRect.Width = width;
						destRect.Height = width * ratio;
						destRect.Y += (height / 2) - (width / 2);
					}
					else {
						var ratio = Image.Width / Image.Height;
						destRect.Height = height;
						destRect.Width = height * ratio;
						destRect.X += (width / 2) - (height / 2);
					}

					break;
				case ImageOrientation.Fit:
					var clampWidth = Math.Clamp(width, 0, Image.Width);
					var clampHeight = Math.Clamp(height, 0, Image.Height);
					if (clampWidth <= clampHeight) { // Width is the bottleneck
						var ratio = Image.Height / Image.Width;
						destRect.Width = clampWidth;
						destRect.Height = clampWidth * ratio;
						destRect.Y += (height / 2) - (width / 2);
					}
					else {
						var ratio = Image.Width / Image.Height;
						destRect.Height = clampHeight;
						destRect.Width = clampHeight * ratio;
						destRect.X += (width / 2) - (height / 2);
					}

					break;
			}

			destRect.X += ImagePadding.X;
			destRect.Y += ImagePadding.Y;
			destRect.Width -= ImagePadding.X * 2;
			destRect.Height -= ImagePadding.Y * 2;

			Color thisC = ImageColor ?? TextColor;

			if (!CanInput())
				thisC = thisC.Adjust(0, 0, -.5f);

			if (ImageRotation != 0 || ImageFlipX || ImageFlipY) {
				destRect.X += destRect.Width / 2;
				destRect.Y += destRect.Height / 2;

				if (ImageFlipX) {
					sourceRect.X = sourceRect.Width;
					sourceRect.Width *= -1;
				}
				if (ImageFlipY) {
					sourceRect.Y = sourceRect.Height;
					sourceRect.Height *= -1;
				}

				Raylib.DrawTexturePro(Image, sourceRect, destRect, new(destRect.Width / 2, destRect.Height / 2), ImageRotation, thisC);
			}
			else
				Raylib.DrawTexturePro(Image, sourceRect, destRect, new(0, 0), ImageRotation, thisC);
		}

		public Level Level => UI.EngineLevel ?? throw new Exception("No level associated with the user interface object!");

		public RenderTexture2D GetRenderTarget() => __RT1 ?? throw new Exception("No render target.");

		public void AddAndInitializeIncompleteElement<T>(T? element) where T : Element {
			if (element == null)
				return;
			if (element.UI != null) {
				//Logs.Info("Tried to initialize a supposedly incomplete element, but it was complete. Ignoring.");
				return;
			}
			Add(element);
		}

		public Vector2F CursorPos() {
			return EngineCore.CurrentFrameState.Mouse.MousePos - GetGlobalPosition();
		}

		public bool ShouldDrawImage { get; set; } = true;

		public static void PaintBackground(Element e, float width, float height, Color back, Color fore, float borderSize) {
			Graphics2D.SetDrawColor(back);
			Graphics2D.DrawRectangle(0, 0, width, height);
			if (e.ShouldDrawImage)
				e.ImageDrawing();
			Graphics2D.SetDrawColor(e.KeyboardFocused ? new Color(210, 255, 225, 255) : fore);
			Graphics2D.DrawRectangleOutline(0, 0, width, height, borderSize);
		}

		public static void PaintBackground(Element e, float width, float height)
			=> PaintBackground(e, width, height, e.BackgroundColor, e.ForegroundColor, e.BorderSize);

		public Vector2F GetMousePos() {
			return EngineCore.MousePos - this.GetGlobalPosition();
		}

		public void SizeToChildren(bool sizeW = true, bool sizeH = true) {
			this.Size = new(sizeW ? 0 : this.Size.W, sizeH ? 0 : this.Size.H);
			InvalidateLayout();
			Size = new(sizeW ? SizeOfAllChildren.W : Size.W, sizeH ? SizeOfAllChildren.H : Size.H);
		}

		public virtual void ProvideExample(Panel buildHere) { }

		public static Elements.Window CreateExampleWindow() {
			UserInterface UI = EngineCore.Level.UI;

			var examples = UI.Add<Elements.Window>();
			examples.Size = new(1280, 720);
			examples.Center();
			examples.Title = "Nucleus - UI Element Examples";

			var listOfElements = (
				from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
				from type in domainAssembly.GetTypes()
				where typeof(Element).IsAssignableFrom(type) && type.Name != "Element"
				select type).ToArray();

			foreach (var elementType in listOfElements) {
				//var instance = (Element)
				Logs.Debug(elementType.Name);
			}

			return examples;
		}

		private bool _fitToParent = false;
		private float fitPadding = 0;
		public void FitToParent(float? padding = null) {
			fitPadding = padding ?? 0;
			_fitToParent = true;
			InvalidateLayout();
		}

		/// <summary>
		/// Passable static method for <see cref="OnHoverTest"/>. Causes hover/click events to "pass through" the element.
		/// </summary>
		/// <param name="self"></param>
		/// <param name="bounds"></param>
		/// <param name="mousePos"></param>
		/// <returns></returns>
		public static bool Passthru(Element self, RectangleF bounds, Vector2F mousePos) => false;

		/// <summary>
		/// Allows you to pass mouse events into another element and make this element passthru instead.
		/// </summary>
		/// <param name="other"></param>
		public void PassMouseTo(Element other) {
			// mark ourselves as passthru
			OnHoverTest += Passthru;
			other.OnHoverTest += (self, bounds, mousePos) => {
				return bounds.ContainsPoint(mousePos) || ScreenspaceRenderBounds.ContainsPoint(mousePos);
			};
		}

		/// <summary>
		/// Recursively searches through parents to confirm if input is disabled or not.
		/// <br></br>
		/// Returns true if all elements have input enabled; or false if even one parent has input disabled.
		/// <br></br>
		/// Also includes the element itself.
		/// </summary>
		/// <returns></returns>
		public virtual bool CanInput() {
			if (InputDisabled)
				return false;

			return Parent?.CanInput() ?? true;
		}
	}

	[Nucleus.MarkForStaticConstruction]
	public static class ElementConsoleInfo
	{
		public static ConCommand nucleus_ui_examples = ConCommand.Register("nucleus_ui_examples", (_, _) => Element.CreateExampleWindow());
	}
}
