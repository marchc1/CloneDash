using Nucleus.Particles;
using Nucleus.Types;
using Curve = (int Node, CloneDash.Scenes.MuseDash1HudProperty Property, Nucleus.Particles.ParticleKeyframe[] Keys);

namespace CloneDash.Scenes;

public static class MuseDash1BattleTexts
{
	const float INF = float.PositiveInfinity;

	public static MuseDash1HudPanel FullCombo() => new(new(0, 19.9995f), 0.9994f, true,
		new MuseDash1HudClip(F(120), [
			.. FullComboLetter(0, 0),
			.. FullComboLetter(1, 6),
			.. FullComboLetter(2, 12),
			.. FullComboLetter(3, 18),
			.. FullComboLetter(4, 54),
			.. FullComboLetter(5, 56),
			.. FullComboLetter(6, 58),
			.. FullComboLetter(7, 60),
			.. FullComboLetter(8, 62),
			.. FullComboLetter(9, 64),
			(4, MuseDash1HudProperty.AnchorX, [new(F(54), 55, INF, 0), new(F(66), 15, 0, 0)]),
			(5, MuseDash1HudProperty.AnchorX, [new(F(56), 95, INF, 0), new(F(68), 115, 0, 0)]),
			(6, MuseDash1HudProperty.AnchorX, [new(0, 175, 0, 0), new(F(58), 175, INF, 0), new(F(70), 225, 0, 0)]),
			(7, MuseDash1HudProperty.AnchorX, [new(F(60), 300, INF, 0), new(F(72), 325, 0, 0)]),
			(8, MuseDash1HudProperty.AnchorX, [new(0, 350, 0, 0), new(F(62), 350, INF, 0), new(F(74), 430, 0, 0)]),
			(9, MuseDash1HudProperty.AnchorX, [new(0, 550, 0, 0), new(F(64), 455, 0, 0), new(F(76), 550, 0, 0)])
		]),
		Letter("fullcombo_f", -457, -60, 87),
		Letter("fullcombo_u", -370, -60, 87),
		Letter("fullcombo_l", -280, -60, 87),
		Letter("fullcombo_l", -179, -60, 87),
		Letter("fullcombo_c", 15, -60, 87),
		Letter("fullcombo_o", 115, -60, 102),
		Letter("fullcombo_m", 225, -60, 87),
		Letter("fullcombo_b", 325, -60, 87),
		Letter("fullcombo_o", 430, -60, 102),
		Letter("fullcombo_!", 550, -60, 87));

	public static MuseDash1HudPanel FullComboGrooveCoaster() => new(new(0, 19.9995f), 0.9994f, true,
		new MuseDash1HudClip(F(90), [
			(0, MuseDash1HudProperty.Width, [new(0, 1700, 0, 0), new(F(5), 110, 0, 0)]),
			(0, MuseDash1HudProperty.AnchorX, [new(0, 1400, 0, 0), new(F(1), -610, 0.029f, 0)]),
			.. FullComboGrooveCoasterLetter(1, 5, -500.0011f, 0, 110),
			.. FullComboGrooveCoasterLetter(2, 10, -390.0011f, 0, 110),
			.. FullComboGrooveCoasterLetter(3, 15, -280.0011f, 0, 110),
			.. FullComboGrooveCoasterLetter(4, 50, -60, 0, 110),
			.. FullComboGrooveCoasterLetter(5, 55, 50, 0, 110),
			.. FullComboGrooveCoasterLetter(6, 60, 160, 0.015f, 110),
			.. FullComboGrooveCoasterLetter(7, 65, 270, 0, 110),
			.. FullComboGrooveCoasterLetter(8, 70, 380, 0, 110),
			.. FullComboGrooveCoasterLetter(9, 75, 514.9999f, 0, 60)
		]),
		SlicedLetter("fullcombo_f_gc", -610, -60, 110, 128, 0, 35),
		SlicedLetter("fullcombo_u_gc", -500.0011f, -60, 110, 128, 0, 35),
		SlicedLetter("fullcombo_l_gc", -390.0011f, -60, 110, 128, 0, 35),
		SlicedLetter("fullcombo_l_gc", -280.0011f, -60, 110, 128, 0, 35),
		SlicedLetter("fullcombo_c_gc", -60, -60, 110, 128, 0, 35),
		SlicedLetter("fullcombo_o_gc", 50, -60, 110, 128, 0, 35),
		SlicedLetter("fullcombo_m_gc", 160, -60, 110, 128, 0, 35),
		SlicedLetter("fullcombo_b_gc", 270, -60, 110, 128, 0, 35),
		SlicedLetter("fullcombo_o_gc", 380, -60, 110, 128, 0, 35),
		SlicedLetter("fullcombo_!_gc", 514.9999f, -60, 60, 128, 0, 30));

	public static MuseDash1HudPanel ReadyGo() => new(new(0, 20), 1, false,
		new MuseDash1HudClip(F(252), [
			(0, MuseDash1HudProperty.ScaleX, [new(0, 1, 0, 0), new(F(176), 1, INF, 0), new(F(183), 0, -25.186f, 0)]),
			(0, MuseDash1HudProperty.ScaleY, [new(0, 1, 0, 0), new(F(176), 1, INF, 0), new(F(183), 0, -21.661f, 0)]),
			.. ReadyLetter(1, 114),
			.. ReadyLetter(2, 122),
			.. ReadyLetter(3, 130),
			.. ReadyLetter(4, 138),
			.. ReadyLetter(5, 146),
			(6, MuseDash1HudProperty.ScaleX, [new(0, 0, 0, 0), new(F(219), 0, INF, 42.409f), new(F(224), 1.3f, 0, 0), new(F(227), 0.9f, 0, 0), new(F(230), 1, 6.005f, 0), new(F(245), 1, INF, 4.814f), new(F(248), 1.1f, 0, 0), new(F(252), 0, -47.693f, 0)]),
			(6, MuseDash1HudProperty.ScaleY, [new(0, 0, 0, 0), new(F(219), 0, INF, 42.511f), new(F(224), 1.3f, 0, 0), new(F(227), 0.9f, 0, 0), new(F(230), 1, 5.817f, 0), new(F(245), 1, INF, 4.996f), new(F(248), 1.1f, 0, 0), new(F(252), 0, -49.595f, 0)])
		]),
		new(-1, null, new(0, 0), new(100, 100), new(0.5f, 0.5f)),
		new(0, "ready_go_1", new(-207.8f, -56.5f), new(102, 113), new(0.5f, 0)),
		new(0, "ready_go_2", new(-102.6f, -60.5012f), new(93, 121), new(0.5f, 0)),
		new(0, "ready_go_3", new(0, -59.5012f), new(100, 119), new(0.5f, 0)),
		new(0, "ready_go_4", new(105.7f, -59.0012f), new(110, 118), new(0.5f, 0)),
		new(0, "ready_go_5", new(213.9f, -58.5012f), new(107, 117), new(0.5f, 0)),
		new(-1, "ready_go_6", new(0, 0), new(484, 206), new(0.5f, 0.5f)));

	public static MuseDash1HudPanel ReadyGoGrooveCoaster() => new(new(0, 20), 1, false,
		new MuseDash1HudClip(F(194), [
			.. ReadyGrooveCoasterLetter(0, 45, 49, -267.475f, 0, 165, -123, 169, 0),
			.. ReadyGrooveCoasterLetter(1, 49, 53, -167.4749f, 0, 168, -22.525f, 172, 0),
			.. ReadyGrooveCoasterLetter(2, 53, 57, -67.475f, 0, 171, 77.475f, 175, 0),
			.. ReadyGrooveCoasterLetter(3, 57, 61, 27.5251f, 0, 174, 172.475f, 178, 0),
			.. ReadyGrooveCoasterLetter(4, 61, 65, 122.5251f, 0.015f, 177, 267.475f, 181, 0),
			.. ReadyGrooveCoasterLetter(5, 90, 94, -687.475f, 0, 165, -542.5251f, 169, 0),
			.. ReadyGrooveCoasterLetter(6, 94, 98, -592.475f, 0, 167, -447.5251f, 171, 0),
			.. ReadyGrooveCoasterLetter(7, 98, 102, -497.4749f, 0, 169, -352.525f, 173, 0),
			.. ReadyGrooveCoasterLetter(8, 102, 106, -402.4749f, -0.029f, 171, -257.525f, 175, 0),
			.. ReadyGrooveCoasterLetter(9, 106, 110, -302.475f, -0.029f, 173, -157.5251f, 177, 0),
			.. ReadyGrooveCoasterLetter(10, 110, 114, -207.475f, 0.029f, 175, -62.5251f, 179, 0),
			.. ReadyGrooveCoasterLetter(11, 114, 118, -17.475f, 0, 177, 127.475f, 181, 0),
			.. ReadyGrooveCoasterLetter(12, 118, 122, 77.5251f, -0.015f, 179, 222.475f, 183, 0),
			.. ReadyGrooveCoasterLetter(13, 122, 126, 172.525f, 0, 181, 317.475f, 185, 0),
			.. ReadyGrooveCoasterLetter(14, 126, 130, 267.5251f, 0, 183, 412.4749f, 187, 0),
			.. ReadyGrooveCoasterLetter(15, 130, 134, 362.5251f, 0.029f, 185, 507.4749f, 189, 0.029f),
			.. ReadyGrooveCoasterLetter(16, 134, 138, 457.5251f, 0, 187, 602.475f, 191, -0.029f),
			.. ReadyGrooveCoasterLetter(17, 138, 143, 557.525f, 0, 189, 702.4749f, 194, 0)
		]),
		CoasterLetter("coaster_l", -267.475f, 75, 55),
		CoasterLetter("coaster_e", -167.4749f, 75, 55),
		CoasterLetter("coaster_t", -67.475f, 75, 55),
		CoasterLetter("coaster_mark", 27.5251f, 75, 65),
		CoasterLetter("coaster_s", 122.5251f, 75, 55),
		CoasterLetter("coaster_g", -687.475f, -45, 55),
		CoasterLetter("coaster_r", -592.475f, -45, 55),
		CoasterLetter("coaster_o", -497.4749f, -45, 55),
		CoasterLetter("coaster_o", -402.4749f, -45, 55),
		CoasterLetter("coaster_v", -302.475f, -45, 55),
		CoasterLetter("coaster_e", -207.475f, -45, 55),
		CoasterLetter("coaster_c", -17.475f, -45, 55),
		CoasterLetter("coaster_o", 77.5251f, -45, 55),
		CoasterLetter("coaster_a", 172.525f, -45, 55),
		CoasterLetter("coaster_s", 267.5251f, -45, 55),
		CoasterLetter("coaster_t", 362.5251f, -45, 55),
		CoasterLetter("coaster_e", 457.5251f, -45, 55),
		CoasterLetter("coaster_r", 557.525f, -45, 55));

	public static MuseDash1HudPanel Resume321() => new(new(0, 0), 1, false,
		new MuseDash1HudClip(F(180), [
			(0, MuseDash1HudProperty.ScaleX, [new(0, 10, 0, -131.571f), new(F(10), 0.7f, 0, 0), new(F(15), 1.2f, 0, 0), new(F(19), 1, 0, 0), new(F(60), 1, INF, 0)]),
			(0, MuseDash1HudProperty.ScaleY, [new(0, 10, 0, -132.895f), new(F(10), 0.7f, 0, 0), new(F(15), 1.2f, 0, 0), new(F(19), 1, 0, 0), new(F(60), 1, INF, 0)]),
			(0, MuseDash1HudProperty.Alpha, [new(0, 0, 0, 0), new(F(15), 1, 0, 0)]),
			(0, MuseDash1HudProperty.Active, [new(0, 1, 0, 0), new(F(60), 0, INF, 0)]),
			(1, MuseDash1HudProperty.ScaleX, [new(0, 10, 0, 0), new(F(60), 10, INF, -131.571f), new(F(70), 0.7f, 0, 0), new(F(75), 1.2f, 0, 0), new(F(79), 1, 0, 0), new(F(120), 1, INF, 0)]),
			(1, MuseDash1HudProperty.ScaleY, [new(0, 10, 0, 0), new(F(60), 10, INF, -132.895f), new(F(70), 0.7f, 0, 0), new(F(75), 1.2f, 0, 0), new(F(79), 1, 0, 0), new(F(120), 1, INF, 0)]),
			(1, MuseDash1HudProperty.Alpha, [new(0, 1, 0, 0), new(F(60), 0, 0, 0), new(F(75), 1, 0, 0)]),
			(1, MuseDash1HudProperty.Active, [new(0, 0, 0, 0), new(F(60), 1, INF, 0), new(F(120), 0, INF, 0)]),
			(2, MuseDash1HudProperty.ScaleX, [new(0, 10, 0, 0), new(F(120), 10, INF, -131.571f), new(F(130), 0.7f, 0, 0), new(F(135), 1.2f, 0, 0), new(F(139), 1, 0, 0), new(F(170), 1, INF, 0), new(F(175), 1.2f, 0, 0), new(F(180), 0, -43.169f, 0)]),
			(2, MuseDash1HudProperty.ScaleY, [new(0, 10, 0, 0), new(F(120), 10, INF, -132.895f), new(F(130), 0.7f, 0, 0), new(F(135), 1.2f, 0, 0), new(F(139), 1, 0, 0), new(F(170), 1, INF, 0), new(F(175), 1.2f, 0, 0), new(F(180), 0, -41.326f, 0)]),
			(2, MuseDash1HudProperty.Alpha, [new(0, 1, 0, 0), new(F(120), 0, 0, 0), new(F(135), 1, 0, 0)]),
			(2, MuseDash1HudProperty.Active, [new(0, 0, 0, 0), new(F(120), 1, INF, 0), new(F(180), 0, INF, 0)])
		]),
		new(-1, "Resume3", new(0, 0), new(160, 177), new(0.5f, 0.5f), active: false),
		new(-1, "Resume2", new(0, 0), new(160, 177), new(0.5f, 0.5f), active: false),
		new(-1, "Resume1", new(0, 0), new(160, 177), new(0.5f, 0.5f), active: false));

	static MuseDash1HudNode CoasterLetter(string sprite, float x, float y, float border) => new(-1, sprite, new(x, y), new(145, 145), new(0, 0.5f), 0.9997f, false, border);

	static float F(int frame) => frame / 60f;

	static Curve[] FullComboGrooveCoasterLetter(int node, int frame, float x, float slope, float width) => [
		(node, MuseDash1HudProperty.Width, [new(0, width, 0, 0), new(F(frame), 1700, 0, 0), new(F(frame + 5), width, 0, 0)]),
		(node, MuseDash1HudProperty.AnchorX, [new(0, 1400, 0, 0), new(F(frame), 1400, INF, 0), new(F(frame + 1), x, slope, 0)])
	];

	static Curve[] FullComboLetter(int node, int frame) => [
		(node, MuseDash1HudProperty.ScaleY, [new(F(frame), 0, INF, 37.983f), new(F(frame + 6), 1.2745f, 0, 0), new(F(frame + 12), 0.9035f, 0, 0), new(F(frame + 15), 1, 0, 0)])
	];

	static MuseDash1HudNode Letter(string sprite, float x, float y, float width) => new(-1, sprite, new(x, y), new(width, 113), new(0.5f, 0));

	static Curve[] ReadyGrooveCoasterLetter(int node, int enter, int settled, float x, float enterSlope, int leave, float leaveX, int exit, float exitSlope) => [
		(node, MuseDash1HudProperty.Width, [new(0, 1700, 0, 0), new(F(enter), 1700, INF, 0), new(F(settled), 145, 0, 0), new(F(leave + 1), 145, INF, 0), new(F(exit), 1700, 0, 0)]),
		(node, MuseDash1HudProperty.AnchorX, [new(0, 1400, 0, 0), new(F(enter), 1400, INF, 0), new(F(enter + 1), x, enterSlope, 0), new(F(leave), x, INF, 0), new(F(leave + 1), leaveX, 0, 0), new(F(exit - 1), leaveX, INF, 0), new(F(exit), -1400, exitSlope, 0)]),
		(node, MuseDash1HudProperty.PivotX, [new(0, 0, 0, 0), new(F(leave), 0, INF, 0), new(F(leave + 1), 1, 0, 0)]),
		(node, MuseDash1HudProperty.Active, [new(0, 1, 0, 0), new(F(194), 0, INF, 0)])
	];

	static Curve[] ReadyLetter(int node, int frame) => [
		(node, MuseDash1HudProperty.ScaleX, [new(0, 1, 0, 0), new(F(frame), 1, INF, 0), new(F(frame + 8), 0.9f, 0, 0), new(F(frame + 14), 1.1f, 0, 0), new(F(frame + 17), 0.9f, 0, 0), new(F(frame + 20), 1, 0, 0)]),
		(node, MuseDash1HudProperty.ScaleY, [new(0, 0, 0, 0), new(F(frame), 0, INF, 0), new(F(frame + 8), 1.2f, 0, 0), new(F(frame + 14), 0.8f, 0, 0), new(F(frame + 17), 1.1f, 0, 0), new(F(frame + 20), 1, 0, 0)])
	];

	static MuseDash1HudNode SlicedLetter(string sprite, float x, float y, float width, float height, float pivotX, float border) => new(-1, sprite, new(x, y), new(width, height), new(pivotX, 0), 1, true, border);
}
