using AssetStudio;
using CloneDash.Compatibility.Unity;
using System.Collections;
using System.Collections.Specialized;
using Vector2 = System.Numerics.Vector2;

namespace CloneDash.Scenes;

public class MuseDash1SongNameMarquee
{
	const float START_DELAY = 1f;

	CanvasImage? maskImage;
	float offset;
	float time;
	float width;

	public CanvasNode? Backup;
	public CanvasNode? LeftMask;
	public int LongBound;
	public CanvasNode Mask = null!;
	public CanvasNode? Middle;
	/// <summary>LongSongNameController.normalFont (Source Han Sans): covers CJK, unlike the language's global UI font (Teko in English).</summary>
	public AssetStudio.Font? NormalFont;
	public CanvasNode? RightMask;
	public CanvasNode? Simple;
	public float Speed = 70;

	public bool Scrolling { get; private set; }

	public void SetText(string text) {
		bool needsNormalFont = NormalFont != null && text.Any(c => c > 0x7F);
		foreach (var node in new[] { Middle, Simple, Backup })
			if (node?.Text != null) {
				node.Text.Text = text;
				if (needsNormalFont)
					UnityCanvas.SetFont(node.Text, NormalFont!);
			}

		width = Middle?.Text != null ? UnityCanvas.MeasureTextWidth(Middle.Text) : 0;
		Scrolling = width > LongBound;
		offset = 0;
		time = 0;

		if (Middle != null)
			Middle.Active = !Scrolling;
		foreach (var node in new[] { Simple, Backup, LeftMask, RightMask })
			if (node != null)
				node.Active = Scrolling;
		maskImage ??= Mask.Image;
		Mask.Mask = Scrolling;
		Mask.Image = Scrolling ? maskImage : null;
		Place();
	}

	public void Update(float deltaTime) {
		if (!Scrolling)
			return;

		time += deltaTime;
		if (time > START_DELAY && width > 0)
			offset = (offset + Speed * deltaTime) % width;
		Place();
	}

	void Place() {
		if (Simple != null) {
			Simple.SizeDelta = Simple.SizeDelta with { X = width };
			Simple.AnchoredPosition = Simple.AnchoredPosition with { X = -offset };
		}
		if (Backup != null) {
			Backup.SizeDelta = Backup.SizeDelta with { X = width };
			Backup.AnchoredPosition = Backup.AnchoredPosition with { X = width - offset };
		}
	}
}

public class MuseDash1LikeButton
{
	public CanvasNode Button = null!;
	public string ButtonName = "";
	public CanvasNode? Hide;
	public float HoldTime;
	public CanvasNode? Like;
}

/// <summary>
/// One of PnlFail's scrolling barrage lines (FailTxtBarrageAnimation): slides from StartPos to TargetX over Duration, after Delay.
/// </summary>
public class MuseDash1FailBarrageLine
{
	public float Delay;
	public float Duration;
	public CanvasNode Node = null!;
	public Vector2 Start;
	public float TargetX;

	public void Update(float time) {
		float t = Duration > 0 ? Math.Clamp((time - Delay) / Duration, 0, 1) : 1;
		Node.AnchoredPosition = new(Start.X + (TargetX - Start.X) * t, Start.Y);
	}
}

public class MuseDash1CanvasBehaviours
{
	UnityCanvas? canvas;

	public List<CanvasNode> GradeObjects { get; } = [];
	public MuseDash1LikeButton? LikeButton { get; private set; }
	public List<MuseDash1SongNameMarquee> Marquees { get; } = [];
	/// <summary>FailBarrage.barrages, in the order the fail_barrage config fills them.</summary>
	public List<CanvasNode> FailBarrageTexts { get; } = [];
	public List<MuseDash1FailBarrageLine> FailBarrageLines { get; } = [];

	public bool Capture(UnityCanvas canvas, CanvasNode node, MonoBehaviour behaviour, string className, OrderedDictionary data) {
		if (this.canvas != canvas) {
			this.canvas = canvas;
			GradeObjects.Clear();
			LikeButton = null;
			Marquees.Clear();
			FailBarrageTexts.Clear();
			FailBarrageLines.Clear();
		}

		switch (className) {
			case "BtnLikeForGameMain":
				LikeButton = new MuseDash1LikeButton {
					Button = node,
					ButtonName = data["m_ButtonName"] as string ?? "",
					Hide = Resolve(canvas, behaviour, data["imgHide"]),
					HoldTime = Convert.ToSingle(data["m_HoldTime"]),
					Like = Resolve(canvas, behaviour, data["imgLike"])
				};
				node.Notes.Add($"like button: like={LikeButton.Like?.Path} hide={LikeButton.Hide?.Path} button={LikeButton.ButtonName} holdTime={LikeButton.HoldTime}");
				return true;
			case "FailBarrage":
				if (data["barrages"] is IList texts)
					foreach (OrderedDictionary pointer in texts)
						if (Resolve(canvas, behaviour, pointer) is CanvasNode text)
							FailBarrageTexts.Add(text);
				node.Notes.Add($"fail barrage: {string.Join(", ", FailBarrageTexts.Select(text => text.Name))}");
				return true;
			case "FailTxtBarrageAnimation":
				var start = (OrderedDictionary)data["m_StartPos"]!;
				FailBarrageLines.Add(new MuseDash1FailBarrageLine {
					Delay = Convert.ToSingle(data["m_StartDelay"]),
					Duration = Convert.ToSingle(data["m_Duration"]),
					Node = node,
					Start = new(Convert.ToSingle(start["x"]), Convert.ToSingle(start["y"])),
					TargetX = Convert.ToSingle(data["m_TargetPosX"])
				});
				return true;
			case "GameMainGrade":
				if (data["gradeObjects"] is IList grades)
					foreach (OrderedDictionary pointer in grades)
						if (Resolve(canvas, behaviour, pointer) is CanvasNode grade)
							GradeObjects.Add(grade);
				node.Notes.Add($"grade objects: {string.Join(", ", GradeObjects.Select(grade => grade.Name))}");
				return true;
			case "LongSongNameController":
				var marquee = new MuseDash1SongNameMarquee {
					Backup = Resolve(canvas, behaviour, data["m_BackupName"]),
					LeftMask = Resolve(canvas, behaviour, data["m_LMask"]),
					LongBound = Convert.ToInt32(data["m_LongBound"]),
					Mask = node,
					Middle = Resolve(canvas, behaviour, data["m_MidSimpleName"]),
					NormalFont = data["normalFont"] is OrderedDictionary font && canvas.TryResolve<AssetStudio.Font>(behaviour, font, out var normalFont) ? normalFont : null,
					RightMask = Resolve(canvas, behaviour, data["m_RMask"]),
					Simple = Resolve(canvas, behaviour, data["m_SimpleName"]),
					Speed = Convert.ToSingle(data["m_Speed"])
				};
				Marquees.Add(marquee);
				node.Notes.Add($"song name marquee: middle={marquee.Middle?.Path} simple={marquee.Simple?.Path} backup={marquee.Backup?.Path} longBound={marquee.LongBound} speed={marquee.Speed}");
				return true;
		}
		return false;
	}

	static CanvasNode? Resolve(UnityCanvas canvas, MonoBehaviour behaviour, object? pointer)
		=> pointer is OrderedDictionary dictionary && canvas.TryResolve<AssetStudio.Object>(behaviour, dictionary, out var target) ? canvas.FindNode(target) : null;
}
