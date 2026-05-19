#define SECOND_ORDER_SYSTEM_MOUSE_RESPONSIVENESS

using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;

using Nucleus.Audio;
using Nucleus.Commands;
using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.ManagedMemory;
using Nucleus.Types;

using Raylib_cs;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Nucleus.UI;

public enum Dock
{
	None,
	Top,
	Left,
	Right,
	Bottom,
	Fill
}
public enum DynamicSizeReference
{
	None,
	WindowHeight,
	ParentHeight,
	SelfHeight
}

public enum ElementFlags : uint
{
	MarkedForRemoval = 1 << 0,
	NeedsRepaint = 1 << 1,
	PaintBorderEnabled = 1 << 2,
	PaintBackgroundEnabled = 1 << 3,
	PaintEnabled = 1 << 4,
	PostChildPaintEnabled = 1 << 5,
	NeedsLayout = 1 << 6,
	NeedsSchemeUpdate = 1 << 7,
	AllowChainInputToParent = 1 << 8,
	AllowChainKeybindingToParent = 1 << 9,
	InPerformLayout = 1 << 10,
	IsProportional = 1 << 11, // TODO
	MousePassthru = 1 << 12,
	IsPopup = 1 << 13,
	IsModal = 1 << 14,
	NeedsRenderBoundsFlush = 1 << 15,
}

public class Element : IValidatable
{
	public const bool FORCE_ROUNDED_RENDERBOUNDS = true;

	// fields
	private ElementFlags flags;
	private Vector2F _position;
	private Vector2F _size;
	private bool _dynamicallySized = false;
	private bool _dynamicallySizedText = false;
	private DynamicSizeReference _dynamicSizeReference = DynamicSizeReference.WindowHeight;
	private Dock _dock = Dock.None;
	private RectangleF _dockMargin = RectangleF.Zero;
	private RectangleF _dockPadding = RectangleF.Zero;

	private bool __enabled = true;
	private bool __visible = true;
	private bool __inputDisabled = false;

	private bool __engineDisabled = false;
	private bool __engineInvisible = false;

	private RectangleF __renderbounds = RectangleF.Zero;

	private Element? __parentToAddTo = null;
	private bool __markedForRemoval = false;
	private bool _firstThink = true;
	internal List<Element> Children = [];
	internal Element?[] FlushedChildren = [];
	internal int CurrentChildrenCount;
	private bool __layoutinvalid = true;

	string? name;
	IScheme? scheme;

	private bool __usesRenderTarget = false;
	private RenderTexture2D? __RT1 = null;
	private RectangleF? __lastRTSize = null;

	// TODO: make private
	public Anchor Anchor;
	public Anchor Origin;

	private bool _fitToParent = false;
	private float fitPadding = 0;

	double backdropTime;
	bool backdrop;
	bool hasBackdropped;
	public double TimeToBackdropAlpha = 0.3;
	public double TimeToNoBackdropAlpha = 0.15;

	private string __text = "Panel";
	private string? __tooltipText = null;
	private float __textSize = 18;

	private bool KbInput;
	private bool MouseInput;
	private bool Visible;

	// properties with backing (should be fields)

	public float BorderSize { get; set; } = 2;
	public float Roundness { get; set; } = 0;
	public Color BackgroundColor { get; set; } = DefaultBackgroundColor;
	public Color ForegroundColor { get; set; } = DefaultForegroundColor;
	public Color TextColor { get; set; } = DefaultTextColor;
	public Vector2F SizeOfAllChildren { get; private set; } = Vector2F.Zero;
	public Vector2F ChildRenderOffset { get; set; } = Vector2F.Zero;
	internal Element? Parent;
	public double LastLayoutTime { get; private set; } = 0;

	public bool Clipping { get; set; } = true;

	public float Opacity { get; set; } = 1.0f;

	public Dictionary<string, object?> Tags { get; } = [];

	public bool Depressed { get; internal set; }
	public bool Dragged { get; internal set; } = false;
	public Vector2F DragVector { get; internal set; } = Vector2F.Zero;

	public DateTime Birth { get; private set; } = DateTime.Now;

	public IKeyboardInputMarshal KeyboardInputMarshal { get; set; } = DefaultKeyboardInputMarshal.Instance;

	public KeybindSystem Keybinds { get; } = new();

	public ITexture? Image { get; set; }
	public ImageOrientation ImageOrientation { get; set; } = ImageOrientation.None;

	public Vector2F ImageOffset { get; set; } = new(0);
	public Vector2F ImagePadding { get; set; } = new(0);
	public float ImageRotation { get; set; } = 0;
	public bool ImageFlipX { get; set; } = false;
	public bool ImageFlipY { get; set; } = false;

	public bool ImageFollowsText { get; set; } = false;
	public Color? ImageColor { get; set; } = null;
	public bool ShouldDrawImage { get; set; } = true;

	/// <summary>
	/// The <see cref="UserInterface"/> the element belongs to.
	/// </summary>
	public UserInterface UI { get; internal set; } = null!;

	public virtual void Initialize(float x, float y, float width, float height) {
		_position = new(x, y);
		_size = new(width, height);
		Anchor = Anchor.TopLeft;
		Origin = Anchor.TopLeft;
		flags |= ElementFlags.NeedsLayout | ElementFlags.NeedsSchemeUpdate;
		flags |= ElementFlags.PaintBackgroundEnabled | ElementFlags.PaintBorderEnabled | ElementFlags.PaintEnabled;
		flags |= ElementFlags.AllowChainKeybindingToParent;
		flags |= ElementFlags.AllowChainInputToParent;
		__tooltipText = null;
		Opacity = 1;
		SetVisible(true);
		SetMouseInputEnabled(true);
		SetKeyboardInputEnabled(true);
	}


	public Vector2F Position {
		get { return _position; }
		set {
			if (value == _position)
				return;

			_position = value; InvalidateLayout();
		}
	}

	public DynamicSizeReference DynamicSizeReference {
		get => _dynamicSizeReference;
		set {
			_dynamicSizeReference = value;
			InvalidateChildren(recursive: true, self: true);
		}
	}
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
			InvalidateLayout();
		}
	}

	public static readonly Color DefaultBackgroundColor = new(20, 25, 32, 127);
	public static readonly Color DefaultForegroundColor = new(85, 95, 110, 255);
	public static readonly Color DefaultTextColor = new(230, 236, 255, 255);

	/// <summary>
	/// Docking; allows the element to dock to a side of its parent, or to dock completely and fill the parent.
	/// </summary>
	public Dock Dock {
		get { return _dock; }
		set {
			if (value == _dock)
				return;

			_dock = value;
			GetParent()?.InvalidateLayout();
			InvalidateLayout();
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
			GetParent()?.InvalidateLayout();
			InvalidateLayout();
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

			GetParent()?.InvalidateLayout();
			InvalidateLayout();
			if (AddParent != this)
				AddParent.DockPadding = value;
			else
				_dockPadding = value;
		}
	}

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

	public Element() {
		Initialize(0, 0, 32, 32);
		UI?.AddElement(this);
	}
	public Element(Element? parent) {
		Initialize(0, 0, 32, 32);
		SetParent(parent?.AddParent);
		UI?.AddElement(this);
	}
	public Element(Element? parent, ReadOnlySpan<char> name) {
		Initialize(0, 0, 32, 32);
		SetName(name);
		SetParent(parent?.AddParent);
		UI?.AddElement(this);
	}
	public Element(Element? parent, ReadOnlySpan<char> name, IScheme? scheme) {
		Initialize(0, 0, 32, 32);
		SetName(name);
		SetParent(parent?.AddParent);
		SetScheme(scheme);
		UI?.AddElement(this);
	}

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

	public virtual string? TooltipText { get; set; } // todo: remove me, turn into methods

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool HasFlag(ElementFlags flag) => (flags & flag) != 0;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void AddFlag(ElementFlags flag) => flags |= flag;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void RemoveFlag(ElementFlags flag) => flags &= ~flag;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFlag(ElementFlags flag, bool state) { if (state) AddFlag(flag); else RemoveFlag(flag); }

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsMarkedForRemoval() => HasFlag(ElementFlags.MarkedForRemoval);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsLayoutInvalid() => HasFlag(ElementFlags.NeedsLayout);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsSchemeInvalid() => HasFlag(ElementFlags.NeedsSchemeUpdate);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsPaintBorderEnabled() => HasFlag(ElementFlags.PaintBorderEnabled);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsPaintBackgroundEnabled() => HasFlag(ElementFlags.PaintBackgroundEnabled);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsPaintEnabled() => HasFlag(ElementFlags.PaintEnabled);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsPostChildPaintEnabled() => HasFlag(ElementFlags.PostChildPaintEnabled);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsPassthru() => HasFlag(ElementFlags.MousePassthru);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetPaintBorderEnabled(bool state) => SetFlag(ElementFlags.PaintBorderEnabled, state);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetPaintBackgroundEnabled(bool state) => SetFlag(ElementFlags.PaintBackgroundEnabled, state);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetPaintEnabled(bool state) => SetFlag(ElementFlags.PaintEnabled, state);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetPostChildPaintEnabled(bool state) => SetFlag(ElementFlags.PostChildPaintEnabled, state);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetPassthru(bool state) => SetFlag(ElementFlags.MousePassthru, state);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetKeyboardInputEnabled(bool state) {
		// if (state == false && KbInput == true) // todo: disable keyboard state
		KbInput = state;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMouseInputEnabled(bool state) => MouseInput = state;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsKeyboardInputEnabled() {
		Element? e = this;
		while (e != null) {
			if (!e.KbInput)
				return false;
			e = e.Parent;
		}
		return KbInput;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsMouseInputEnabled() {
		Element? e = this;
		while (e != null) {
			if (!e.MouseInput)
				return false;
			e = e.Parent;
		}
		return MouseInput;
	}

	public event Action<Element>? Removed;

	private void REMOVE() {
		if (__markedForRemoval == true)
			return;

		OnRemoval();
		Removed?.Invoke(this);

		__markedForRemoval = true;

		if (IsPopup())
			UI.RemovePopup(this);

		if (IsModal())
			UI.RemoveModal(this);

		UI.RemoveElement(this);

		foreach (Element element in this.LockAndEnumerateChildren())
			element.REMOVE();
		this.UnlockChildren();
	}
	public void Remove() {
		REMOVE();

		// Call parent methods
		if (IValidatable.IsValid(Parent)) {
			Parent.Children.Remove(this);
			Parent.InvalidateLayout();
		}
	}

	protected virtual bool OnTextDropped(string text, Vector2F pos) => false;
	protected virtual bool OnFileDropped(string filepath, Vector2F pos) => false;

	internal bool TextDropped(string text, Vector2F pos) => OnTextDropped(text, pos);
	internal bool FileDropped(string filepath, Vector2F pos) => OnFileDropped(filepath, pos);

	public bool IsValid() => !__markedForRemoval;

	internal void TriggerOnChildParented(Element parent, Element child) {
		ChildParented(parent, child);
	}

	protected virtual void OnThink() { }

	internal void Think() {
		if (_firstThink) {
			_firstThink = false;
			Birth = DateTime.Now;
		}

		if (IsVisible())
			ValidateLayout();

		OnThink();
	}

	/// <summary>
	/// Use this for child-critical contexts rather than creating a full copy to throw away!!!
	/// </summary>
	/// <returns></returns>
	internal Span<Element> LockAndEnumerateChildren() {
		CurrentChildrenCount = Children.Count;

		if (CurrentChildrenCount > FlushedChildren.Length)
			FlushedChildren = new Element[CurrentChildrenCount];

		for (int i = 0; i < CurrentChildrenCount; i++)
			FlushedChildren[i] = Children[i];

		return FlushedChildren.AsSpan()[..CurrentChildrenCount]!;
	}
	internal void UnlockChildren() {
		for (int i = 0; i < CurrentChildrenCount; i++)
			FlushedChildren[i] = null;
	}

	/// <summary>
	/// Returns all children of this element. Does not allow modification of the elements children; use AddChild/SetParent functionality for that.
	/// </summary>
	/// <returns></returns>
	public ReadOnlySpan<Element> GetChildren() => LockAndEnumerateChildren();

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

	public ReadOnlySpan<char> GetName() => name;
	public void SetName(ReadOnlySpan<char> name) {
		if (name.IsEmpty || name[0] == '\0') {
			this.name = null;
			return;
		}

		this.name = new(name.SliceNullTerminatedString());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Element? GetParent() => Parent;

	public void SetParent(Element? p) {
		if (p == this)
			return; // not valid at all

		// Detach ourselves from the current parent
		if (Parent != null) {
			Parent.Children.Remove(this);
			p?.InvalidateLayout();
		}

		// Set up fields from the new parent
		Parent = p;                         // set parent to P
		UI = p?.UI!;

		// Attach ourselves to the new parent
		if (p != null) {
			p.Children.Add(this);
			p.InvalidateLayout();
		}

		// Trigger invalidations on ourselves just in case
		InvalidateLayout();
		p?.TriggerOnChildParented(p, this);
	}

	public void SortChildren(Comparison<Element> childSortMethod) {
		this.AddParent.Children.Sort(childSortMethod);
	}

	public void InvalidateChildren(bool recursive = false, bool self = false) {
		if (self)
			InvalidateLayout();

		foreach (Element e in Children) {
			e.InvalidateLayout();
			if (recursive)
				e.InvalidateChildren(recursive);
		}
	}

	/// <summary>
	/// Invalidates the layout, registering it for a rebuild with the layout system.
	/// </summary>
	/// <param name="immediate"></param>
	public void InvalidateLayout() {
		AddFlag(ElementFlags.NeedsLayout);
		AddFlag(ElementFlags.NeedsRenderBoundsFlush);
	}

	public void MarkRenderBoundsAsDirty() {
		AddFlag(ElementFlags.NeedsRenderBoundsFlush);
	}

	public void FlushRenderBounds() {
		if (!HasFlag(ElementFlags.NeedsRenderBoundsFlush))
			return;
		__renderbounds.X = _position.X;
		__renderbounds.Y = _position.Y;
		__renderbounds.W = _size.W;
		__renderbounds.H = _size.H;
		RemoveFlag(ElementFlags.NeedsRenderBoundsFlush);
	}

	public void ValidateLayout() {
		if (IsLayoutInvalid()) {
			Layout();
			RemoveFlag(ElementFlags.NeedsLayout);
		}
	}

	private void Layout() {
		// Flush render bounds if we need that
		FlushRenderBounds();
		DoOriginAnchor();
		// Perform the internal layout based on our size
		PerformLayout(__renderbounds.W, __renderbounds.H);
		// Perform child docking
		DoChildDocking();
	}

	private void DoOriginAnchor() {
		Element? parent = GetParent();
		if (IValidatable.IsValid(parent) && (Origin != Anchor.TopLeft || Anchor != Anchor.TopLeft)) {
			var np = Origin.CalculatePosition(__renderbounds.Pos, __renderbounds.Size, true);
			var npO = Anchor.CalculatePosition(new(0, 0), parent.__renderbounds.Size, false);
			__renderbounds.Pos = npO + np;
		}
	}

	private void DoChildDocking() {
		RectangleF availableSpace = RectangleF.FromPosAndSize(Vector2F.Zero, __renderbounds.Size);

		// Shrink available space by dock padding
		if (!_dockPadding.IsZero) {
			availableSpace.AddPosition(new(_dockPadding.X, _dockPadding.Y));
			availableSpace.AddSize(new(_dockPadding.W * -2, _dockPadding.H * -2));
		}

		foreach (var child in Children) {
			Dock dock = child.Dock;
			if (dock == Dock.None)
				continue;

			// We will modify the render bounds of the child, and mark its render bounds as NOT dirty after
			// This is kind of a hack but its the cleanest way to do it probably, forcing the render bounds flush
			child.AddFlag(ElementFlags.NeedsRenderBoundsFlush);
			child.FlushRenderBounds();
			RectangleF childBoundsPreEdit = child.__renderbounds;
			ref RectangleF childBounds = ref child.__renderbounds;
			// Aligning child bounds to our bounds...
			switch (dock) {
				case Dock.Top:
					childBounds.X = availableSpace.X;
					childBounds.Y = availableSpace.Y;
					childBounds.W = availableSpace.W;
					// height untouched
					break;
				case Dock.Left:
					childBounds.X = availableSpace.X;
					childBounds.Y = availableSpace.Y;
					childBounds.H = availableSpace.H;
					// width untouched
					break;
				case Dock.Right:
					childBounds.X = availableSpace.X + availableSpace.W - childBounds.W;
					childBounds.Y = availableSpace.Y;
					childBounds.H = availableSpace.H;
					// width untouched
					break;
				case Dock.Bottom:
					childBounds.X = availableSpace.X;
					childBounds.Y = availableSpace.Y + availableSpace.H - childBounds.H;
					childBounds.W = availableSpace.W;
					// height untouched
					break;
				case Dock.Fill:
					childBounds.X = availableSpace.X;
					childBounds.Y = availableSpace.Y;
					childBounds.W = availableSpace.W;
					childBounds.H = availableSpace.H;
					break;
			}
			// ... then fixing up our bounds
			switch (dock) {
				case Dock.Top:
					availableSpace.Y += childBounds.H;
					availableSpace.H -= childBounds.H;
					break;
				case Dock.Left:
					availableSpace.X += childBounds.W;
					availableSpace.W -= childBounds.W;
					break;
				// we only need to shrink size for the next two
				case Dock.Right:
					availableSpace.W -= childBounds.W;
					break;
				case Dock.Bottom:
					availableSpace.H -= childBounds.H;
					break;
			}

			// Then shrink the child by DockMargin..
			if (!child._dockMargin.IsZero) {
				childBounds.AddPosition(new(child._dockMargin.X, child._dockMargin.Y));
				childBounds.AddSize(new(child._dockMargin.W * -2, child._dockMargin.H * -2));
			}

			// manual layout flag setting here
			if (childBoundsPreEdit != childBounds)
				child.AddFlag(ElementFlags.NeedsLayout);

			child.RemoveFlag(ElementFlags.NeedsRenderBoundsFlush);
		}
	}

	public bool IsUsingRenderTarget() => __usesRenderTarget;
	/// <summary>
	/// Renders this element and all of its children to a render-target rather than straight to the screen every frame.<br></br>
	/// This can be used for FPS limiting and for special effects on some elements.
	/// </summary>
	public void SetUseRenderTarget(bool value) {
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsParented() => Parent != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool HasChildren() => Children.Count > 0;

	public bool Backdrop {
		get => backdrop;
		set {
			backdrop = value;
			// TODO: set this to accommodate for mid-backdrop-alpha values
			backdropTime = Lifetime;
			if (value)
				hasBackdropped = true;
		}
	}

	public double BackdropAlpha {
		get {
			if (!hasBackdropped)
				return 0;

			double ret;
			if (backdrop)
				ret = NMath.Remap(Lifetime - backdropTime, 0, TimeToBackdropAlpha, 0, 1, true);
			else
				ret = NMath.Remap(Lifetime - backdropTime, TimeToNoBackdropAlpha, 0, 0, 1, false, true);

			if (ret <= 0 && !backdrop)
				hasBackdropped = false;
			return ret;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsPopup() => HasFlag(ElementFlags.IsPopup);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsModal() => HasFlag(ElementFlags.IsPopup);

	/// <summary>
	/// Requires a valid parent, otherwise this just resolves to parenting to itself (invalid)
	/// </summary>
	public void ReparentToRoot() {
		var parent = this;
		while (parent != null) {
			var last = parent.GetParent();
			if (last == null) {
				SetParent(parent);
				break;
			}
			parent = last;
		}
	}

	/// <summary>
	/// A popup ensures the following behavior: <br/>
	/// - That, out of all children in the parent, its input will be handled first <br/>
	/// - That, out of all children in the parent, its rendering will be done last <br/>
	/// - It also ensures that the parent children get a chance at input, unlike modals. <br/>
	/// - It also bypasses clipping setups and ensures that it renderes last in the frame loop
	/// </summary>
	public void MakePopup() {
		if (UI.MakePopup(this))
			AddFlag(ElementFlags.IsPopup);
	}

	public void MakeModal() {
		if (UI.MakeModal(this))
			AddFlag(ElementFlags.IsModal);
	}

	public bool IsParentedToPopup([NotNullWhen(true)] out Element? parent) {
		parent = Parent;
		while (parent != null) {
			if (parent.IsPopup())
				return true;
			parent = parent.Parent;
		}
		return false;
	}

	public bool IsParentedToModal([NotNullWhen(true)] out Element? parent) {
		parent = Parent;
		while (parent != null) {
			if (parent.IsModal())
				return true;
			parent = parent.Parent;
		}
		return false;
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
		}
	}

	public string Font { get; set; } = Graphics2D.UI_FONT_NAME;
	public DynamicSizeReference DynamicTextSizeReference = DynamicSizeReference.None;

	public float GetReferenceSize(DynamicSizeReference referenceValue) => DynamicTextSizeReference switch {
		DynamicSizeReference.None => 1f,
		DynamicSizeReference.WindowHeight => EngineCore.GetWindowHeight() / 900f,
		DynamicSizeReference.ParentHeight => GetParent() == null ? 1 : GetParent()!.RenderBounds.Height / 20f,
		DynamicSizeReference.SelfHeight => RenderBounds.Height / 20f,
		_ => throw new NotImplementedException()
	};

	public float TextSize {
		get {
			if (!DynamicallySized)
				return __textSize;

			var heightRatio = GetReferenceSize(DynamicTextSizeReference);
			return Math.Clamp(__textSize * heightRatio, 8, 160);
		}
		set {
			__textSize = value;
		}
	}

	~Element() {
		if (__RT1.HasValue) {
			MainThread.RunASAP(() => {
				Raylib.UnloadRenderTexture(__RT1.Value);
			});
		}
		//OnRemoval();
	}

	public virtual void PreRenderRT() { }
	public virtual void PostRenderRT() { }
	public virtual bool PostRenderChildRT(Element element) => true;

	public virtual void SetVisible(bool visible) {
		if (Visible == visible)
			return;

		Visible = visible;
		GetParent()?.InvalidateLayout();
		InvalidateLayout();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual bool IsVisible() {
		return Visible;
	}

	public bool IsRenderTargetAvailable(out RenderTexture2D rt) {
		if (!__lastRTSize.HasValue || RenderBounds != __lastRTSize) {
			if (__RT1.HasValue) Raylib.UnloadRenderTexture(__RT1.Value);

			__RT1 = Graphics2D.CreateRenderTarget(RenderBounds.W, RenderBounds.H);
			__lastRTSize = RenderBounds;
		}

		if (!__RT1.HasValue) {
			rt = default;
			return false;
		}

		rt = __RT1.Value;
		return true;
	}

	public bool HasTag(string key) => Tags.ContainsKey(key);
	public T? GetTag<T>(string key) => Tags.TryGetValue(key, out object? v) ? (T?)v : default;
	public void SetTag<T>(string key, T? value) => Tags[key] = value;
	public void UnsetTag(string key) => Tags.Remove(key);


	public void ClearChildren() {
		foreach (var child in this.AddParent.LockAndEnumerateChildren())
			child.Remove();
		this.AddParent.UnlockChildren();

		this.AddParent.Children.Clear();
		InvalidateLayout();
	}
	public void ClearChildrenNoRemove() {
		this.AddParent.Children.Clear();
		InvalidateLayout();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsHovered() {
		return MouseInput && UI.Hovered == this;
	}

	internal bool MouseClickOccur(FrameState state, ButtonCode button) {
		if (!MouseInput)
			return false;
		Depressed = true;
		return MouseClick(state, button);
	}

	internal bool MouseReleaseOccur(FrameState state, ButtonCode button) {
		if (!MouseInput)
			return false;
		Dragged = false;
		DragVector = Vector2F.Zero;

		bool handled = MouseRelease(this, state, button);
		Depressed = false; // todo: this is breaking, but it might be a good idea to keep this true during the release
		return handled;
	}
	internal bool MouseDragOccur(FrameState state, Vector2F delta) {
		if (!MouseInput)
			return false;
		return MouseDrag(this, state, delta);
	}
	internal bool MouseScrollOccur(Element element, FrameState state, Vector2F delta) {
		if (!MouseInput)
			return false;
		return MouseScroll(element, state, delta);
	}

	public double Lifetime => (DateTime.Now - Birth).TotalSeconds;

#if SECOND_ORDER_SYSTEM_MOUSE_RESPONSIVENESS
	private SecondOrderSystem? __mouseColorableHoverState;
	private SecondOrderSystem? __mouseColorableDepressState;
#endif

	public static Color MixColorBasedOnMouseState(Element e, Color original, Vector4 hoveredHSV, Vector4 depressedHSV) {
#if SECOND_ORDER_SYSTEM_MOUSE_RESPONSIVENESS
		e.__mouseColorableHoverState ??= new SecondOrderSystem(4.1f, 0.5f, 0.94f, 0);
		e.__mouseColorableDepressState ??= new SecondOrderSystem(4.1f, 0.5f, 0.94f, 0);
		return MixColorBasedOnMouseState(e.__mouseColorableDepressState.Update(e.IsHovered() ? 1 : 0), e.__mouseColorableHoverState.Update(e.Depressed ? 1 : 0), original, hoveredHSV, depressedHSV);
#else
		return MixColorBasedOnMouseState(e.Hovered ? 1 : 0, e.Depressed ? 1 : 0, original, hoveredHSV, depressedHSV);
#endif
	}
	/// <summary>
	/// This function expects HSVA in the format of hueAdditional, saturationMultiplied, valueMultiplied, alphaMultiplied
	/// </summary>
	public static Color MixColorBasedOnMouseState(float hoverRatio, float depressedRatio, Color original, Vector4 hoveredHSVA, Vector4 depressedHSVA) {
		var originalHSV = original.RGBubToHSVf();


		var hoveredColor = ColorExtensions.FromHSVf(originalHSV.X + hoveredHSVA.X, originalHSV.Y * hoveredHSVA.Y, originalHSV.Z * hoveredHSVA.Z);
		hoveredColor.A = (byte)Math.Clamp(original.A * hoveredHSVA.W, 0, 255);

		var depressedColor = ColorExtensions.FromHSVf(originalHSV.X + depressedHSVA.X, originalHSV.Y * depressedHSVA.Y, originalHSV.Z * depressedHSVA.Z);
		depressedColor.A = (byte)Math.Clamp(hoveredColor.A * depressedHSVA.W, 0, 255);

		return NMath.LerpColor(depressedRatio, NMath.LerpColor(hoverRatio, original, hoveredColor), depressedColor);
	}

	public virtual void Center() {
		if (Parent == null)
			return;
		var parentBounds = Parent.RenderBounds;
		var pb2 = new Vector2F(parentBounds.Width / 2, parentBounds.Height / 2);
		var tb2 = new Vector2F(RenderBounds.Width / 2, RenderBounds.Height / 2);
		this.Position = pb2 - tb2;
	}


	internal bool KeyPressedOccur(in KeyboardState keyboardState, ButtonCode key) {
		if (!KbInput)
			return false;
		return KeyPressed(in keyboardState, key);
	}
	internal bool KeyReleasedOccur(in KeyboardState keyboardState, ButtonCode key) {
		if (!KbInput)
			return false;
		return KeyReleased(in keyboardState, key);
	}
	internal bool TextInputOccur(in KeyboardState keyboardState, string text) {
		if (!KbInput)
			return false;
		return TextInput(in keyboardState, text);
	}

	public bool IsIndirectChildOf(Element parent) {
		var p = this;

		while (p != null) {
			p = p.Parent;
			if (p == parent)
				return true;
		}

		return false;
	}

	public bool IsKeyboardFocused() => UI.GetKeyboardFocusedElement() == this;

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

	public void ImageDrawing(Vector2F? pos = null, Vector2F? size = null, Color? color = null) {
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
					var ratio = (float)Image.Height / Image.Width;
					destRect.Width = width;
					destRect.Height = width * ratio;
					destRect.Y += (height / 2) - (width / 2);
				}
				else {
					var ratio = (float)Image.Width / Image.Height;
					destRect.Height = height;
					destRect.Width = height * ratio;
					destRect.X += (width / 2) - (height / 2);
				}

				break;
			case ImageOrientation.Fit:
				var clampWidth = Math.Clamp(width, 0, Image.Width);
				var clampHeight = Math.Clamp(height, 0, Image.Height);
				if (clampWidth <= clampHeight) { // Width is the bottleneck
					var ratio = (float)Image.Height / Image.Width;
					destRect.Width = clampWidth;
					destRect.Height = clampWidth * ratio;
					destRect.Y += (height / 2) - (width / 2);
				}
				else {
					var ratio = (float)Image.Width / Image.Height;
					destRect.Height = clampHeight;
					destRect.Width = clampHeight * ratio;
					destRect.X += (width / 2) - (height / 2);
				}

				break;
		}

		destRect.X += ImagePadding.X + ImageOffset.X;
		destRect.Y += ImagePadding.Y + ImageOffset.Y;
		destRect.Width -= ImagePadding.X * 2;
		destRect.Height -= ImagePadding.Y * 2;

		Color thisC = ImageColor ?? TextColor;

		if (!IsMouseInputEnabled())
			thisC = thisC.Adjust(0, 0, -.5f);

		if (Image.HasPublicFlags(PublicTextureFlags.RequiresFlippedV))
			sourceRect.Height *= -1;

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

			Raylib.DrawTexturePro((Texture)Image, sourceRect, destRect, new(destRect.Width / 2, destRect.Height / 2), ImageRotation, color ?? thisC);
		}
		else
			Raylib.DrawTexturePro((Texture)Image, sourceRect, destRect, new(0, 0), ImageRotation, color ?? thisC);
	}

	// TODO: Get rid of this
	// It is a backwards compatibility feature in the meantime
	/// <summary>
	/// This feature is being phased out, and will likely be fully replaced in the future. <br/> It is still a valid macro in the meantime, as texture management is still tightly coupled to level objects.
	/// </summary>
	public Level Level { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => EngineCore.Level; }

	public Vector2F CursorPos() {
		return Level.FrameState.Mouse.MousePos - GetGlobalPosition();
	}

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
		UserInterface UI = EngineCore.Level.RootPanel;

		var examples = new Elements.Window(UI);
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

	public IScheme? GetScheme() {
		return scheme;
	}
	public void SetScheme(IScheme? scheme) {
		if (this.scheme == scheme) return;

		this.scheme = scheme;
		if (scheme != null)
			AddFlag(ElementFlags.NeedsSchemeUpdate);
	}

	public virtual void ApplySchemeSettings(IScheme scheme) {
		BackgroundColor = scheme.GetColor("Nucleus.Background");
		ForegroundColor = scheme.GetColor("Nucleus.Border");
		TextColor = scheme.GetColor("Nucleus.Text");

		var fontStyle = scheme.GetFontStyle("Nucleus.Default");
		Font = fontStyle.Name;
		TextSize = fontStyle.Tall;
	}

	// Virtual overrides
	protected virtual void ChildParented(Element parent, Element child) { }
	protected virtual void OnRemoval() { }

	protected virtual void PerformLayout(float width, float height) { }
	protected virtual void PostLayoutChildren() { }

	protected virtual void TextChanged(string oldText, string newText) { }

	protected virtual void PreLayoutChild(Element element) { }
	protected virtual void PostLayoutChild(Element element) { }
	protected virtual void PreLayoutChildren() { }
	protected virtual void ModifyLayout(ref RectangleF renderBounds) { }

	public virtual void PaintBackground(float width, float height) {
		Color back = BackgroundColor, fore = ForegroundColor;
		float borderSize = BorderSize, roundness = Roundness;

		Graphics2D.SetDrawColor(back);

		if (roundness <= 0) {
			Graphics2D.DrawRectangle(0, 0, width, height);
		}
		else {
			// Prevent roundness from exceeding bounds of the element
			roundness = Math.Clamp(roundness, 0, width / 2);
			roundness = Math.Clamp(roundness, 0, height / 2);
			int segments = (int)Math.Clamp(roundness * 1.5f, 0, 12);
			Graphics2D.DrawRectangleRounded(0, 0, width, height, roundness, segments);
		}
	}
	public virtual void Paint(float width, float height) {

	}
	public virtual void PaintBorder(float width, float height) {
		Color back = BackgroundColor, fore = ForegroundColor;
		float borderSize = BorderSize, roundness = Roundness;

		if (roundness <= 0) {
			Graphics2D.SetDrawColor(IsKeyboardFocused() ? new Color(210, 255, 225, 255) : fore);
			Graphics2D.DrawRectangleOutline(0, 0, width, height, borderSize);
		}
		else {
			// Prevent roundness from exceeding bounds of the element
			roundness = Math.Clamp(roundness, 0, width / 2);
			roundness = Math.Clamp(roundness, 0, height / 2);
			int segments = (int)Math.Clamp(roundness * 1.5f, 0, 12);
			Graphics2D.SetDrawColor(IsKeyboardFocused() ? new Color(210, 255, 225, 255) : fore);
			Graphics2D.DrawRectangleRoundedOutline(0, 0, width, height, roundness, borderSize, segments);
		}
	}
	public virtual void PostChildPaint() {

	}


	public virtual bool HoverTest(RectangleF bounds, Vector2F mousePos) {
		if (IsPassthru())
			return false;

		var containsPoint = bounds.ContainsPoint(mousePos);
		if (containsPoint && IValidatable.IsValid(Parent)) {
			var scissor = RectangleF.FromPosAndSize(Parent.GetGlobalPosition() - Parent.ChildRenderOffset, Parent.RenderBounds.Size);
			return scissor.ContainsPoint(mousePos);
		}

		return containsPoint;
	}


	public bool CanKeyboardFocusGainedOccur(Element? lastFocus, ref Element? passTo) => OnGainingKeyboardFocus(lastFocus, ref passTo);
	public bool CanKeyboardFocusLostOccur(Element? newFocus) => OnLosingKeyboardFocus(newFocus);

	public bool KeyboardFocus() => UI.SetKeyboardFocusedElement(this);
	public bool KeyboardUnfocus() => UI.SetKeyboardFocusedElement(null);

	protected virtual bool MouseClick(FrameState state, ButtonCode button) => true;
	protected virtual bool MouseRelease(Element self, FrameState state, ButtonCode button) => true;
	protected virtual bool MouseDrag(Element self, FrameState state, Vector2F delta) => true;
	protected virtual bool MouseScroll(Element self, FrameState state, Vector2F delta) => false;

	protected virtual bool OnGainingKeyboardFocus(Element? lastFocus, ref Element? passTo) => true;
	protected virtual bool OnLosingKeyboardFocus(Element? newFocus) => true;

	protected virtual bool KeyPressed(in KeyboardState keyboardState, ButtonCode key) => false;
	protected virtual bool KeyReleased(in KeyboardState keyboardState, ButtonCode key) => false;
	protected virtual bool TextInput(in KeyboardState keyboardState, string text) => false;

	internal void PerformApplySchemeSettings() {
		if (HasFlag(ElementFlags.NeedsSchemeUpdate)) {
			IScheme? scheme = GetScheme();
			if (scheme != null) {
				ApplySchemeSettings(scheme);
			}
		}
	}
}

[Nucleus.MarkForStaticConstruction]
public static class ElementConsoleInfo
{
	public static ConCommand nucleus_ui_examples = new("nucleus_ui_examples", (_, in _) => Element.CreateExampleWindow());
}
