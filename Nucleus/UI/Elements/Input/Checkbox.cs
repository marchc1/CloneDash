using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Common.Input;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Input;
using Nucleus.Interfaces;
using Nucleus.Types;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI;

public class Checkbox : Button, IBindableToConVar
{
	public bool Checked { get; set; } = false;

	public delegate void CheckboxClicked(Checkbox self);
	public event CheckboxClicked? OnCheckedChanged;

	private HashSet<Checkbox> __otherRadioButtons = [];

	public void BindToConVar(string convar) {
		ConVar? cv = cvar.FindVar(convar);
		Debug.Assert(cv != null, "Tried to bind to a non-existant convar");
		if (cv == null) return;

		BindToConVar(cv);
	}

	public void BindToConVar(IConVar cv) {
		Checked = cv.GetBool();
		OnCheckedChanged += _ => cv.SetValue(Checked);
	}

	public bool Radio { get; set; } = false;
	public void LinkRadioButton(Checkbox other) {
		if (__otherRadioButtons.Contains(other)) return;
		__otherRadioButtons.Add(other);
		other.OnCheckedChanged += (e) => {
			if (other.Checked && other.Radio)
				this.Checked = false;
		};
		other.LinkRadioButton(this);
	}

	private float? CheckAnim = null;

	public Checkbox(Element? parent) : base(parent, "") { }
	public Checkbox(Element? parent, ReadOnlySpan<char> name) : base(parent, "", name) { }

	public static float AnimationTime = 0.15f;

	// The box or circle, square and centered in whatever space we were given
	private RectangleF GetBox(float width, float height) {
		float side = MathF.Floor(Math.Min(width, height));
		return RectangleF.XYWH(MathF.Floor((width - side) / 2), MathF.Floor((height - side) / 2), side, side);
	}

	public override void PaintBackground(float width, float height) {
		ColorStateSetup(out var back, out _);
		var box = GetBox(width, height);
		Graphics2D.SetDrawColor(back);
		if (Radio)
			Graphics2D.DrawCircle(box.Pos + box.Size / 2, box.W / 2);
		else
			Graphics2D.DrawRectangle(box);
	}

	public override void PaintBorder(float width, float height) {
		ColorStateSetup(out _, out var fore);
		if (IsKeyboardFocused())
			fore = GetFocusColor();
		var box = GetBox(width, height);
		float thickness = Math.Max(1, BorderSize);
		Graphics2D.SetDrawColor(fore);
		if (Radio)
			Graphics2D.DrawRing(box.Pos + box.Size / 2, box.W / 2 - thickness, box.W / 2, 0, 360, 48);
		else
			Graphics2D.DrawRectangleOutline(box, thickness);
	}

	public override void Paint(float width, float height) {
		float c = CheckAnim ?? (Checked ? 1 : 0);
		c = Math.Clamp(c + ((float)Level.RendertimeDelta / AnimationTime * (Checked ? 1 : -1)), 0, 1);
		CheckAnim = c;

		if (c <= 0)
			return;

		var box = GetBox(width, height);
		Graphics2D.SetDrawColor(GetTextColor());

		if (Radio) {
			float radius = NMath.Ease.OutQuart(c) * box.W * 0.22f;
			Graphics2D.DrawCircle(box.Pos + box.Size / 2, radius);
			return;
		}

		// Short arm down to the corner, long arm up, drawn on by length
		Vector2F start = box.Pos + box.Size * new Vector2F(0.24f, 0.52f);
		Vector2F corner = box.Pos + box.Size * new Vector2F(0.42f, 0.70f);
		Vector2F end = box.Pos + box.Size * new Vector2F(0.77f, 0.31f);
		float thickness = Math.Max(2, MathF.Round(box.W / 12));

		float first = (corner - start).Length;
		float second = (end - corner).Length;
		float drawn = NMath.Ease.OutCubic(c) * (first + second);

		Vector2F firstEnd = drawn >= first ? corner : Vector2F.Lerp(drawn / first, start, corner);
		Graphics2D.DrawLine(start, firstEnd, thickness);
		if (drawn > first) {
			Graphics2D.DrawLine(corner, Vector2F.Lerp((drawn - first) / second, corner, end), thickness);
			// fill the corner joint
			Graphics2D.DrawCircle(corner, thickness / 2);
		}
	}

	protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
		if (!IsHovered()) return true;
		if (Radio)
			Checked = true;
		else
			Checked = !Checked;
		OnCheckedChanged?.Invoke(this);
		return true;
	}
}
