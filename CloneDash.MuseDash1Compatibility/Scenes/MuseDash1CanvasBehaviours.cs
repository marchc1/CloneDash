using AssetStudio;
using CloneDash.Compatibility.Unity;
using System.Collections;
using System.Collections.Specialized;

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
	public CanvasNode? RightMask;
	public CanvasNode? Simple;
	public float Speed = 70;

	public bool Scrolling { get; private set; }

	public void SetText(string text) {
		foreach (var node in new[] { Middle, Simple, Backup })
			if (node?.Text != null)
				node.Text.Text = text;

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

public class MuseDash1CanvasBehaviours
{
	UnityCanvas? canvas;

	public List<CanvasNode> GradeObjects { get; } = [];
	public MuseDash1LikeButton? LikeButton { get; private set; }
	public List<MuseDash1SongNameMarquee> Marquees { get; } = [];

	public bool Capture(UnityCanvas canvas, CanvasNode node, MonoBehaviour behaviour, string className, OrderedDictionary data) {
		if (this.canvas != canvas) {
			this.canvas = canvas;
			GradeObjects.Clear();
			LikeButton = null;
			Marquees.Clear();
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
