#define SECOND_ORDER_SYSTEM_MOUSE_RESPONSIVENESS

using Nucleus.Commands;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Common.UI;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Extensions;
using Nucleus.Input;
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
	NeedsRenderBoundsFlush = 1 << 15
}

public struct SchemeableSetting<T>
{
	public T SchemeValue;
	public T UserValue;
	public bool HasUserValue;
	public static SchemeableSetting<T> Default(in T value) => new() { SchemeValue = value };
}

public static class SchemeableSettingHelpers
{
	extension<T>(ref SchemeableSetting<T> schemeable)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T Get() {
			if (schemeable.HasUserValue)
				return ref schemeable.UserValue;
			else
				return ref schemeable.SchemeValue;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetUserValue(in T? value) {
			if (value != null) {
				schemeable.HasUserValue = true;
				schemeable.UserValue = value;
			}
			else {
				schemeable.HasUserValue = false;
				schemeable.UserValue = default!;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetSchemeValue(in T value) {
			schemeable.SchemeValue = value;
		}
	}
}

public class Element : IValidatable
{
	public const bool FORCE_ROUNDED_RENDERBOUNDS = true;
	public const float DYNAMIC_SIZE_W_REFERENCE = 1600;
	public const float DYNAMIC_SIZE_H_REFERENCE = 900;

	// These constants are not the true defaults, they are basic colors, defaulting is done via the engine scheme
	public static readonly Color DefaultBackgroundColor = new(0, 0, 0, 255);
	public static readonly Color DefaultForegroundColor = new(155, 155, 155, 255);
	public static readonly Color DefaultTextColor = new(255, 255, 255, 255);
	public static readonly float DefaultTextSize = 18;

	// fields
	private ElementFlags flags;
	private Vector2F _position;
	private Vector2F? _minimumSize;
	private Vector2F? _maximumSize;
	private Vector2F _size;
	// dynamic sizing really needs to be reworked entirely
	private bool _dynamicallySized = false;
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
	private bool QueueCenter = false;
	internal List<Element> Children = [];
	internal Element?[] FlushedChildren = [];
	internal int CurrentChildrenCount;
	private bool __layoutinvalid = true;

	string? name;
	IScheme? scheme;
	IScheme? lastAppliedScheme;

	private bool __usesRenderTarget = false;
	private RenderTexture2D? __RT1 = null;
	private RectangleF? __lastRTSize = null;

	// TODO: make private
	private Anchor anchor;
	private Anchor origin;

	private bool _fitToParent = false;
	private float fitPadding = 0;

	double backdropTime;
	bool backdrop;
	bool hasBackdropped;
	public double TimeToBackdropAlpha = 0.3;
	public double TimeToNoBackdropAlpha = 0.15;

	private string? __tooltipText = null;

	private bool KbInput;
	private bool MouseInput;
	private bool Visible;

	private bool clipping = true;
	private float opacity = 1.0f;

	SchemeableSetting<Color> backgroundColor = SchemeableSetting<Color>.Default(DefaultBackgroundColor);
	SchemeableSetting<Color> foregroundColor = SchemeableSetting<Color>.Default(DefaultForegroundColor);

	private float borderSize = 2;
	private float roundness = 0;

	private Vector2F childRenderOffset = Vector2F.Zero;
	internal Element? Parent;
	internal Element? ElementToPassMouseTo;
	private double lastLayoutTime = 0;

	private readonly Dictionary<string, object?> Tags = [];
	DateTime Birth = DateTime.Now;
	private IKeyboardInputMarshal keyboardInputMarshal = DefaultKeyboardInputMarshal.Instance;

	public float GetBorderSize() {
		return borderSize;
	}

	public void SetBorderSize(float value) {
		borderSize = value;
	}

	public float GetRoundness() {
		return roundness;
	}

	public void SetRoundness(float value) {
		roundness = value;
	}

	private Vector2F sizeOfAllChildren = Vector2F.Zero;

	public Vector2F GetSizeOfAllChildren() {
		return sizeOfAllChildren;
	}

	private void SetSizeOfAllChildren(Vector2F value) {
		sizeOfAllChildren = value;
	}

	public Vector2F GetChildRenderOffset() {
		return childRenderOffset;
	}

	public void SetChildRenderOffset(Vector2F value) {
		childRenderOffset = value;
	}

	public double GetLastLayoutTime() {
		return lastLayoutTime;
	}

	private void SetLastLayoutTime(double value) {
		lastLayoutTime = value;
	}

	public bool GetClipping() {
		return clipping;
	}

	public void SetClipping(bool value) {
		clipping = value;
	}


	public float GetOpacity() {
		return opacity;
	}

	public void SetOpacity(float value) {
		opacity = value;
	}

	public IKeyboardInputMarshal GetKeyboardInputMarshal() {
		return keyboardInputMarshal;
	}

	public void SetKeyboardInputMarshal(IKeyboardInputMarshal value) {
		keyboardInputMarshal = value ?? DefaultKeyboardInputMarshal.Instance;
	}

	public KeybindSystem Keybinds { get; } = new();


	/// <summary>
	/// The <see cref="UserInterface"/> the element belongs to.
	/// </summary>
	public UserInterface UI { get; internal set; } = null!;

	public virtual void Initialize(float x, float y, float width, float height) {
		_position = new(x, y);
		_size = new(width, height);
		SetAnchor(Anchor.TopLeft);
		SetOrigin(Anchor.TopLeft);
		flags |= ElementFlags.NeedsLayout | ElementFlags.NeedsSchemeUpdate | ElementFlags.NeedsRenderBoundsFlush;
		flags |= ElementFlags.PaintEnabled;
		flags |= ElementFlags.AllowChainKeybindingToParent;
		flags |= ElementFlags.AllowChainInputToParent;
		__tooltipText = null;
		SetOpacity(1);
		SetVisible(true);
		SetMouseInputEnabled(true);
		SetKeyboardInputEnabled(true);
	}


	public Vector2F GetPos() { return _position; }

	public void SetPos(Vector2F value) {
		if (value == _position)
			return;

		_position = value;
		if (!HasFlag(ElementFlags.InPerformLayout)) {
			InvalidateLayout();
		}
		else
			AddFlag(ElementFlags.NeedsRenderBoundsFlush);
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

	public Vector2F GetSize() => _size;

	public void SetSize(Vector2F value) {
		if (value == _size)
			return;

		ClampedSizeSet(value);
	}

	public Vector2F? GetMinimumSize() => _minimumSize;
	public Vector2F? GetMaximumSize() => _maximumSize;

	public void SetMinimumSize(Vector2F? value) {
		if (value == _minimumSize)
			return;
		_minimumSize = value;
		ClampedSizeSet(_size);
	}
	public void SetMaximumSize(Vector2F? value) {
		if (value == _maximumSize)
			return;
		_maximumSize = value;
		ClampedSizeSet(_size);
	}

	void ClampedSizeSet(Vector2F size) {
		Vector2F previousSize = _size;
		Vector2F newSize = size;
		if (_minimumSize.HasValue)
			newSize = new(MathF.Max(_minimumSize.Value.X, newSize.X), MathF.Max(_minimumSize.Value.Y, newSize.Y));
		if (_maximumSize.HasValue)
			newSize = new(MathF.Min(_maximumSize.Value.X, newSize.X), MathF.Min(_maximumSize.Value.Y, newSize.Y));
		_size = newSize;
		if (newSize != previousSize) {
			if (!HasFlag(ElementFlags.InPerformLayout)) {
				if (_dock != Dock.None)
					GetParent()?.InvalidateLayout();
				InvalidateLayout();
			}
			else
				AddFlag(ElementFlags.NeedsRenderBoundsFlush);
		}
	}

	/// <summary>
	/// Docking; allows the element to dock to a side of its parent, or to dock completely and fill the parent.
	/// </summary>
	public Dock GetDock() { return _dock; }

	public void SetDock(Dock value) {
		if (value == _dock)
			return;

		_dock = value;
		GetParent()?.InvalidateLayout();
		InvalidateLayout();
	}
	/// <summary>
	/// The extra space left around <i>this</i> element when docked to something.<br></br>
	/// For the extra space left around this elements children when docked; see DockPadding.
	/// </summary>
	public RectangleF GetDockMargin() { return _dockMargin; }

	public void SetDockMargin(RectangleF value) {
		if (_dockMargin == value)
			return;

		_dockMargin = value;
		GetParent()?.InvalidateLayout();
		InvalidateLayout();
	}
	/// <summary>
	/// The extra space left around this elements children (if the child is docked inside of this element).<br></br>
	/// For the extra space left around this element when docked; see DockMargin.
	/// </summary>
	public RectangleF GetDockPadding() { return _dockPadding; }

	public void SetDockPadding(RectangleF value) {
		if (_dockPadding == value)
			return;

		GetParent()?.InvalidateLayout();
		InvalidateLayout();
		if (GetAddParent() != this)
			GetAddParent().SetDockPadding(value);
		else
			_dockPadding = value;
	}

	public virtual RectangleF GetRenderBounds() {
		return __renderbounds;
	}

	public Element() {
		Initialize(0, 0, 32, 32);
		UI?.AddElement(this);
	}
	public Element(Element? parent) {
		Initialize(0, 0, 32, 32);
		SetParent(parent);
		PerformApplySchemeSettings();
		UI?.AddElement(this);
	}
	public Element(Element? parent, ReadOnlySpan<char> name) {
		Initialize(0, 0, 32, 32);
		SetElementName(name);
		SetParent(parent);
		PerformApplySchemeSettings();
		UI?.AddElement(this);
	}
	public Element(Element? parent, ReadOnlySpan<char> name, IScheme? scheme) {
		Initialize(0, 0, 32, 32);
		SetElementName(name);
		SetParent(parent?.GetAddParent());
		SetScheme(scheme);
		PerformApplySchemeSettings();
		UI?.AddElement(this);
	}

	/// <summary>
	/// The element which Add<>() adds to. Can be used to defer add operations to a different part of the element.<br></br>
	/// By default, returns itself.
	/// </summary>
	public Element GetAddParent() {
		if (__parentToAddTo == null)
			return this;

		return __parentToAddTo;
	}

	public void SetAddParent(Element value) {
		__parentToAddTo = value;
		if (value != null) {
			value.SetDockPadding(GetDockPadding());
		}
	}

	private string? tooltipText;
	public virtual ReadOnlySpan<char> GetTooltipText() => tooltipText;
	public virtual void SetTooltipText(ReadOnlySpan<char> value) => tooltipText = (value.Length == 0 || value[0] == '\0') ? null : new(value);

	public float GetDynamicallyScaledFloat(float originalFloat, Axis axis) {
		switch (axis) {
			case Axis.Horizontal: return originalFloat * (EngineCore.GetWindowWidth() / DYNAMIC_SIZE_W_REFERENCE);
			case Axis.Vertical: return originalFloat * (EngineCore.GetWindowHeight() / DYNAMIC_SIZE_H_REFERENCE);
			default: return originalFloat;
		}
	}


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

	// Real colors
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public virtual Color GetBgColor() => backgroundColor.Get();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public virtual void SetBgColor(Color value) => backgroundColor.SetUserValue(value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public virtual Color GetFgColor() => foregroundColor.Get();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public virtual void SetFgColor(Color value) => foregroundColor.SetUserValue(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBgColor(ReadOnlySpan<char> schemeColor) {
		var scheme = GetScheme();
		if (scheme == null) return;
		SetBgColor(scheme.GetColor(schemeColor, backgroundColor.Get()));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetFgColor(ReadOnlySpan<char> schemeColor) {
		var scheme = GetScheme();
		if (scheme == null) return;
		SetFgColor(scheme.GetColor(schemeColor, foregroundColor.Get()));
	}

	public event Action<Element>? Removed;

	private void REMOVE() {
		if (__markedForRemoval == true)
			return;

		OnRemoval();
		Removed?.Invoke(this);

		if (IsPopup())
			UI.RemovePopup(this);

		if (IsModal())
			UI.RemoveModal(this);

		UI.RemoveElement(this);

		__markedForRemoval = true;
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

	public event Action<Element>? Thinking;

	internal void Think() {
		if (_firstThink) {
			_firstThink = false;
			Birth = DateTime.Now;
		}

		if (IsVisible())
			ValidateLayout();

		OnThink();
		Thinking?.Invoke(this);
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

	public ReadOnlySpan<char> GetElementName() => name;
	public void SetElementName(ReadOnlySpan<char> name) {
		if (name.IsEmpty || name[0] == '\0') {
			this.name = null;
			return;
		}

		this.name = new(name.SliceNullTerminatedString());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Element? GetParent() => Parent;

	public void SetParent(Element? p) {
		p = p?.GetAddParent();
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
		this.GetAddParent().Children.Sort(childSortMethod);
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
		if (_dock == Dock.None)
			AddFlag(ElementFlags.NeedsRenderBoundsFlush);
		foreach (var child in Children)
			if (child.GetDock() != Dock.None || child.GetAnchor() != Anchor.TopLeft || child.DynamicallySized)
				child.InvalidateLayout();
	}

	public void MarkRenderBoundsAsDirty() {
		AddFlag(ElementFlags.NeedsRenderBoundsFlush);
	}

	protected virtual void PostRenderBoundsFlush(ref RectangleF bounds) { }

	public void FlushRenderBounds() {
		if (!HasFlag(ElementFlags.NeedsRenderBoundsFlush))
			return;
		Vector2F layoutPos = _position, layoutSize = _size;
		if (_dynamicallySized && Parent != null)
			layoutSize = _size * Parent.__renderbounds.Size;

		__renderbounds.X = layoutPos.X;
		__renderbounds.Y = layoutPos.Y;
		__renderbounds.W = layoutSize.W;
		__renderbounds.H = layoutSize.H;
		PostRenderBoundsFlush(ref __renderbounds);
		RemoveFlag(ElementFlags.NeedsRenderBoundsFlush);
		if (FORCE_ROUNDED_RENDERBOUNDS) {
			__renderbounds.X = float.Floor(__renderbounds.X);
			__renderbounds.Y = float.Floor(__renderbounds.Y);
			__renderbounds.W = float.Floor(__renderbounds.W);
			__renderbounds.H = float.Floor(__renderbounds.H);
		}
		SetLastLayoutTime(globals.CurTime);
	}

	public void ValidateLayout() {
		if (IsVisible() && IsLayoutInvalid()) {
			RemoveFlag(ElementFlags.NeedsLayout);
			Layout();
		}
	}

	private void Layout() {
		// Flush render bounds if we need that
		FlushRenderBounds();
		if (QueueCenter)
			DoCentering();
		// Perform the internal layout based on our size
		AddFlag(ElementFlags.InPerformLayout);
		PerformLayout(__renderbounds.W, __renderbounds.H);
		RemoveFlag(ElementFlags.InPerformLayout);
		if (GetDock() == Dock.None)
			FlushRenderBounds();
		DoOriginAnchor();
		CommitFitToParent();
		foreach (var child in Children)
			child.FlushRenderBounds();
		// Perform child docking
		DoChildDocking();
		ComputeSizeOfAllChildren();
	}

	private void CommitFitToParent() {
		if (_fitToParent) {
			var parentBounds = GetParent()?.GetRenderBounds() ?? UI.GetRenderBounds();
			var overflow = parentBounds.GetOverflow(__renderbounds, fitPadding);
			__renderbounds.Pos += overflow;
			_fitToParent = false;
		}
	}

	private void ComputeSizeOfAllChildren() {
		SetSizeOfAllChildren(Vector2F.Zero);
		foreach (var child in Children) {
			if (child.IsVisible()) {
				var ps = child.GetRenderBounds().Pos + child.GetRenderBounds().Size;
				if (ps > GetSizeOfAllChildren())
					SetSizeOfAllChildren(ps);
			}
		}
	}

	private void DoCentering() {
		var parent = GetParent();
		if (parent == null)
			return;

		var parentBounds = parent.GetRenderBounds();
		var pb2 = new Vector2F(parentBounds.Width / 2, parentBounds.Height / 2);
		var tb2 = new Vector2F(__renderbounds.Width / 2, __renderbounds.Height / 2);
		var centered = pb2 - tb2;
		_position = centered;
		__renderbounds.X = centered.X;
		__renderbounds.Y = centered.Y;
		QueueCenter = false;
	}
	private void DoOriginAnchor() {
		Element? parent = GetParent();
		if (IValidatable.IsValid(parent) && (GetOrigin() != Anchor.TopLeft || GetAnchor() != Anchor.TopLeft)) {
			var np = GetOrigin().CalculatePosition(__renderbounds.Pos, __renderbounds.Size, true);
			var npO = GetAnchor().CalculatePosition(new(0, 0), parent.__renderbounds.Size, false);
			__renderbounds.Pos = npO + np;
		}
	}

	private void DoChildDocking() {
		RectangleF availableSpace = RectangleF.FromPosAndSize(Vector2F.Zero, __renderbounds.Size);

		// Shrink available space by dock padding
		if (!_dockPadding.IsZero) {
			availableSpace.X += _dockPadding.X;
			availableSpace.Y += _dockPadding.Y;
			availableSpace.W -= _dockPadding.X + _dockPadding.W;
			availableSpace.H -= _dockPadding.Y + _dockPadding.H;
		}

		foreach (var child in Children) {
			Dock dock = child.GetDock();
			if (dock == Dock.None)
				continue;
			if (!child.IsVisible())
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
				childBounds.X += child._dockMargin.X;
				childBounds.Y += child._dockMargin.Y;
				childBounds.W -= child._dockMargin.X + child._dockMargin.W;
				childBounds.H -= child._dockMargin.Y + child._dockMargin.H;
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsModal() => HasFlag(ElementFlags.IsModal);

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
		if (UI.MakeModal(this)) {
			AddFlag(ElementFlags.IsModal);
			Backdrop = true;
		}
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

	public DynamicSizeReference DynamicTextSizeReference = DynamicSizeReference.None;

	public float GetReferenceSize(DynamicSizeReference referenceValue) => DynamicTextSizeReference switch {
		DynamicSizeReference.None => 1f,
		DynamicSizeReference.WindowHeight => EngineCore.GetWindowHeight() / 900f,
		DynamicSizeReference.ParentHeight => GetParent() == null ? 1 : GetParent()!.GetRenderBounds().Height / 20f,
		DynamicSizeReference.SelfHeight => GetRenderBounds().Height / 20f,
		_ => throw new NotImplementedException()
	};

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
		if (!__lastRTSize.HasValue || GetRenderBounds() != __lastRTSize) {
			if (__RT1.HasValue) Raylib.UnloadRenderTexture(__RT1.Value);

			__RT1 = Graphics2D.CreateRenderTarget(GetRenderBounds().W, GetRenderBounds().H);
			__lastRTSize = GetRenderBounds();
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
		foreach (var child in this.GetAddParent().LockAndEnumerateChildren())
			child.Remove();
		this.GetAddParent().UnlockChildren();

		this.GetAddParent().Children.Clear();
		InvalidateLayout();
	}

	public void ClearChildrenNoRemove() {
		this.GetAddParent().Children.Clear();
		InvalidateLayout();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsHovered() => MouseInput && UI.GetHoveredElement() == this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsDepressed() => MouseInput && UI.GetDepressedElement() == this;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsDepressed(ButtonCode code) => MouseInput && UI.GetDepressedElement(code) == this;


	internal bool MouseClickOccur(FrameState state, ButtonCode button) {
		if (!MouseInput)
			return false;
		return MouseClick(state, button);
	}

	internal bool MouseReleaseOccur(FrameState state, ButtonCode button) {
		if (!MouseInput)
			return false;
		bool handled = MouseRelease(this, state, button);
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
		e.__mouseColorableHoverState ??= e.BuildHoveredSOS();
		e.__mouseColorableDepressState ??= e.BuildDepressedSOS();
		return MixColorBasedOnMouseState(e.__mouseColorableHoverState.Update(e.IsHovered() ? 1 : 0), e.__mouseColorableDepressState.Update(e.IsDepressed() ? 1 : 0), original, hoveredHSV, depressedHSV);
#else
		return MixColorBasedOnMouseState(e.IsHovered() ? 1 : 0, e.Depressed ? 1 : 0, original, hoveredHSV, depressedHSV);
#endif
	}

	private float GetSpecificSOSFloat(ReadOnlySpan<char> prefix, ReadOnlySpan<char> keyName, float def = 0) {
		Span<char> lookup = stackalloc char[prefix.Length + keyName.Length + 1];
		prefix.CopyTo(lookup);
		lookup[prefix.Length] = '.';
		keyName.CopyTo(lookup[(prefix.Length + 1)..]);
		return GetScheme()?.GetFloat(lookup) ?? def;
	}

	private SecondOrderSystem BuildSpecificSOS(ReadOnlySpan<char> prefix) {
		float naturalFrequency = GetSpecificSOSFloat(prefix, "NaturalFrequency", 100f);
		float dampingCoefficient = GetSpecificSOSFloat(prefix, "DampingCoefficient", 1);
		float initialResponse = GetSpecificSOSFloat(prefix, "InitialResponse");
		return new SecondOrderSystem(naturalFrequency, dampingCoefficient, initialResponse, 0);
	}

	private SecondOrderSystem BuildHoveredSOS() => BuildSpecificSOS("MouseHovered");
	private SecondOrderSystem BuildDepressedSOS() => BuildSpecificSOS("MouseDepressed");

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
		if (!Parent.IsLayoutInvalid() || IsLayoutInvalid())
			QueueCenter = true;
		else {
			DoCentering();
		}
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
			ret += t.GetRenderBounds().Pos + t.GetChildRenderOffset();
			t = t.Parent;
			if (t == null || t == t.UI) {
				break;
			}
		}
		return ret;
	}


	// TODO: Get rid of this
	// It is a backwards compatibility feature in the meantime
	/// <summary>
	/// This feature is being phased out, and will likely be fully replaced in the future. <br/> It is still a valid macro in the meantime, as texture management is still tightly coupled to level objects.
	/// </summary>
	public Level Level { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => EngineCore.Level; }
	public Anchor GetOrigin() => origin;
	public void SetOrigin(Anchor value) => origin = value;
	public Anchor GetAnchor() => anchor;
	public void SetAnchor(Anchor value) => anchor = value;
	// TODO: what the hell is the difference???
	public Vector2F CursorPos() => Level.FrameState.Mouse.MousePos - GetGlobalPosition();
	public Vector2F GetMousePos() => EngineCore.MousePos - this.GetGlobalPosition();

	public void SizeToChildren(bool sizeW = true, bool sizeH = true) {
		this.SetSize(new(sizeW ? 0 : this.GetSize().W, sizeH ? 0 : this.GetSize().H));
		InvalidateLayout();
		SetSize(new(sizeW ? GetSizeOfAllChildren().W : GetSize().W, sizeH ? GetSizeOfAllChildren().H : GetSize().H));
	}

	public virtual void ProvideExample(Panel buildHere) { }

	public static Elements.Window CreateExampleWindow() {
		UserInterface UI = EngineCore.Level.RootPanel;

		var examples = new Elements.Window(UI);
		examples.SetSize(new(1280, 720));
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

	public IScheme? GetScheme() => scheme ?? GetParent()?.GetScheme();
	public void SetScheme(ReadOnlySpan<char> scheme) => SetScheme(ElementSchemeSystem.GetSchemeByName(scheme));
	public void SetScheme(IScheme? scheme) {
		if (this.scheme == scheme) return;

		if (scheme != null) {
			AddFlag(ElementFlags.NeedsSchemeUpdate);
		}
		OnSchemeChanged(this.scheme, scheme);
		this.scheme = scheme;
	}

	/// <summary>
	/// Allows flushing out fields
	/// </summary>
	/// <param name="prev"></param>
	/// <param name="now"></param>
	public virtual void OnSchemeChanged(IScheme? prev, IScheme? now) {
		__mouseColorableHoverState = null;
		__mouseColorableDepressState = null;
	}

	public Element GetMouseElement() => ElementToPassMouseTo ?? this;
	public void PassMouseTo(Element? c) {
		ElementToPassMouseTo = c;
	}

	// Virtual overrides
	protected virtual void ChildParented(Element parent, Element child) { }
	protected virtual void OnRemoval() { }
	protected virtual void PerformLayout(float width, float height) { }
	protected virtual void PreLayoutChild(Element element) { }
	protected virtual void PostLayoutChild(Element element) { }
	protected virtual void PreLayoutChildren() { }
	public virtual void ApplySchemeSettings(IScheme scheme) {
		backgroundColor.SetSchemeValue(scheme.GetColor("Nucleus.Background"));
		foregroundColor.SetSchemeValue(scheme.GetColor("Nucleus.Border"));

		SetFlag(ElementFlags.NeedsSchemeUpdate, false);
	}
	public virtual void PaintBackground(float width, float height) {
		Color back = GetBgColor(), fore = GetFgColor();
		float borderSize = GetBorderSize(), roundness = GetRoundness();

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
	public virtual void Paint(float width, float height) { }
	public virtual void PaintBorder(float width, float height) {
		Color back = GetBgColor(), fore = GetFgColor();
		float borderSize = GetBorderSize(), roundness = GetRoundness();

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
	public virtual void PostChildPaint() { }

	public virtual int GetPaintChildStartIndex() => 0;
	public virtual int GetPaintChildEndIndex() => Children.Count;
	public virtual bool ShouldPaintChild(Element child) {
		if (!clipping) return true;
		// otherwise, make sure rect in rect
		// TODO: Make this not suck
		RectangleF parent = GetRenderBounds();
		RectangleF childRect = child.GetRenderBounds();
		childRect.Pos += GetChildRenderOffset();
		return childRect.X < parent.W &&
		   childRect.X + childRect.W > 0 &&
		   childRect.Y < parent.H &&
		   childRect.Y + childRect.H > 0;
	}

	public virtual bool HoverTest(RectangleF bounds, Vector2F mousePos) => bounds.ContainsPoint(mousePos);

	public bool TryGainKeyboardFocus(Element? lastFocus, ref Element? passTo) => OnGainingKeyboardFocus(lastFocus, ref passTo);
	public bool TryLoseKeyboardFocus(Element? newFocus) => OnLosingKeyboardFocus(newFocus);
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
		IScheme? current = GetScheme();

		if (current != lastAppliedScheme) {
			AddFlag(ElementFlags.NeedsSchemeUpdate);
			lastAppliedScheme = current;
		}

		if (HasFlag(ElementFlags.NeedsSchemeUpdate) && current != null)
			ApplySchemeSettings(current);
	}
}

[Nucleus.MarkForStaticConstruction]
public static class ElementConsoleInfo
{
	public static ConCommand nucleus_ui_examples = new("nucleus_ui_examples", (_, in _) => Element.CreateExampleWindow());
}
