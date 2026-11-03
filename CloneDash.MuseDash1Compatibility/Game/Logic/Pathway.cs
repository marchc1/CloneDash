using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Entities;
using Nucleus.Extensions;
using Nucleus.Types;

using Raylib_cs;

namespace CloneDash.Game;

public class Pathway : LogicalEntity
{
	InputAction Action => Level.As<MuseDash1Game>().Layout[Id].Action;
	public bool IsClicked() => Level.As<MuseDash1Game>().GetInputState().ClickCount(Action) > 0;
	public bool IsPressed() => Level.As<MuseDash1Game>().GetInputState().IsHeld(Action);
	public int PressedKeysCount => Level.As<MuseDash1Game>().GetInputState().HeldKeys(Action);

	public PathwayId Id { get; }

	/// <summary>
	/// The size of the pathway hit marker is changed by both every quarter note and when an input event occurs; this animation smoother is used for input events.
	/// </summary>
	public SecondOrderSystem InputAnimator { get; private set; } = new(0.4f, 0.5f, 1f, 1);

	public Pathway(PathwayId id) : base() {
		Id = id;
	}

	public Color Color => Level.As<MuseDash1Game>().Layout[Id].Color;

	public SecondOrderSystem Animator { get; private set; } = new(8.4f, 0.5f, 1f, 1);
	public override void Think(FrameState frameState) {
		Position = Level.As<MuseDash1Game>().GetPathwayPosition(Id);
	}
	public override void PostRender(FrameState frameState) {

	}

	public void Render() {
		var lvl = Level.As<MuseDash1Game>();
		var conductor = lvl.Conductor;
		var beatInfluence = 1 - conductor.NoteDivisorRealtime(4);
		var realInfluence = Animator.Update((IsClicked() || IsPressed()) ? 2 : beatInfluence);
		var size = Raymath.Remap(realInfluence, 0, 1, 0.85f, 1f);
		var curtimeOffset = (float)NMath.Modulo(conductor.Time, 1);

		var alphaM = Math.Max(0, Math.Min(conductor.Time + 1, 1));

		float divisors = 3;
		float ring_offset = 360 / divisors / 2;

		var alpha = (int)(Raymath.Remap(realInfluence, 0, 1, 79, 130) * alphaM);
		if (lvl.HasActiveScene(out var scene))
			scene.RenderPathway(Id, alpha, size, curtimeOffset);
	}
}
