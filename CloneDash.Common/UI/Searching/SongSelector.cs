using CloneDash.Characters;
using CloneDash.Charts;
using CloneDash.Common;
using CloneDash.Common.Songs;
using CloneDash.Game;
using CloneDash.Settings;
using CloneDash.Systems;
using FftSharp;
using Nucleus;
using Nucleus.Audio;
using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;



namespace CloneDash.Menu.Searching;

public struct ChartSongSourceMoveInit
{
	public bool OperationExecuted;
	public bool ImmediatelyAvailable;
}

public struct ChartSongSourceMoveFinish
{
	public bool OperationExecuted;
	public int Movement;
}

public delegate void ChartSongSourceMoveFinishFn(in ChartSongSourceMoveFinish finishResult);

public class SongSelector : Panel, IMainMenuPanel
{
	public void SetRichPresence() {
		RichPresenceSystem.SetPresence(new() {
			Details = "Main Menu",
			State = "Picking a chart"
		});
	}
	public string GetName() => "Song Selector";
	public void OnHidden() { }
	public void OnShown() { }

	SongSearchBar SearchBar = null!;
	SongLabel FilterResults = null!;
	SongSearchDialog? ActiveDialog;
	IChartSongFilter? SearchFilter;

	ISongSourceState? Source;
	public void SetSource(ISongSourceState source) {
		Source = source;
		ClearSongs();
	}

	public void TriggerUserInitializeSearch() {
		if (Source == null) return;
		SearchFilter = Source.NewFilter();

		ActiveDialog = new(UI);
		ActiveDialog.MakeModal();
		ActiveDialog.Selector = this;
		ActiveDialog.Bar = SearchBar;
		ActiveDialog.OnUserSubmit += () => TriggerUserSubmittedSearch();

		SearchFilter.PopulateFields(ActiveDialog);
	}

	public void TriggerUserSubmittedSearch() {
		if (Source == null) return;
		if (!IValidatable.IsValid(ActiveDialog)) return;

		if (SearchFilter == null) {
			ClearFilter();
			return;
		}

		Source = ActiveDialog.Apply(Source, SearchFilter);

		ClearSongs();
	}

	public void ClearSongs() {
		InvalidateLayout();
		ResetDiskTrack();
		UpdateFilterText();
	}

	protected override void OnThink() {
		base.OnThink();
		ThinkDiscs();
	}

	public bool IsFiltered => Source?.GetParentSource() != null;
	public int SongCountFiltered => Source?.GetSongCount() ?? 0;
	public int SongCountTotal => Source?.GetRootSource()?.GetSongCount() ?? 0;

	public string GetFilterText() {
		if (Source == null)
			return "Source == null?";

		string text;
		if (!IsFiltered)
			text = $"{SongCountTotal} songs available";
		else
			text = $"{SongCountFiltered}/{SongCountTotal} songs filtered";

		return text;
	}

	public void UpdateFilterText() => FilterResults.SetText(GetFilterText());

	public void ClearFilter() {
		Source = Source?.GetRootSource();
		UpdateFilterText();
	}

	public delegate void UserWantsMore();
	public event UserWantsMore? UserWantsMoreSongs;

	public double DiscRotateAnimation { get; set; } = 0;

	public SecondOrderSystem DiscRotateSOS = new(2f, 0.94f, 1.1f, 0);
	public SecondOrderSystem FlyAwaySOS = new(1.5f, 0.94f, 1.1f, 0);

	protected void GetMoreSongs() {
		Loading.SetVisible(true);
		Loading.MoveToFront();
		UserWantsMoreSongs?.Invoke();
	}

	public class SongDiscButton : Button
	{
		SongSelector selector;
		int i;
		Image imageRenderer;
		public SongDiscButton(SongSelector selector, int i) : base(selector) {
			this.selector = selector;
			this.i = i;
			imageRenderer = new(this);
			imageRenderer.SetDock(Dock.Fill);
		}
		public override void Paint(float w, float h) {
			float a;
			if (selector.InSheetSelection)
				a = i == selector.IntegerMidpoint ? selector.FlyAway : 1;
			else
				a = 1;
			var c = MixColorBasedOnMouseState(this, new(35, (int)(255 * a)), new(0, 1, 2, 1), new(0, 1, 0.5f, 1));
			Graphics2D.SetDrawColor(c);
			Graphics2D.DrawCircle(new(w / 2, h / 2), w / 2 - 8);
			Opacity = a;
		}

		internal void SetImageRotation(float value) => imageRenderer.SetImageRotation(value);
		internal void SetImageOrientation(ImageOrientation value) => imageRenderer.SetImageOrientation(value);
		internal void SetImagePadding(Vector2F value) => imageRenderer.SetImagePadding(value);
		internal void SetImage(ITexture value) => imageRenderer.SetTexture(value);
		internal void SetImageFlipX(bool value) => imageRenderer.SetImageFlipX(value);
		internal void SetImageFlipY(bool value) => imageRenderer.SetImageFlipY(value);
		internal void SetImageColor(Color value) => imageRenderer.SetImageColor(value);
	}

	public SongLabel CurrentTrackName = null!;
	public SongLabel CurrentTrackAuthor = null!;
	public SongDiscButton[] Discs = null!;
	public readonly SecondOrderSystem DiscAnimationOffset = new(4.5f, 1, 1, 0);

	public void MoveLeft() {
		if (Source == null)
			return;

		Source.MoveLeft(CommitMove);
	}

	public void MoveRight() {
		if (Source == null)
			return;

		Source.MoveRight(CommitMove);
	}

	private void CommitMove(in ChartSongSourceMoveFinish finished) {
		if (!finished.OperationExecuted)
			return;
		if (finished.Movement == 0)
			return;
		DiscAnimationOffset.ResetTo(finished.Movement);
		ResetDiskTrack();
		InvalidateLayout();
		UpdateFilterText();
	}

	public static int GetButtonLocalIndex(SongDiscButton discButton) => discButton.GetTag<int>("localDiscIndex");

	public ISong? GetDiscSong(SongDiscButton discButton) => Source?.At(GetButtonLocalIndex(discButton));
	public ISong? GetDiscSong(int idx) => Source?.At(idx);

	public int DiscIndexToSelectIndex(int idx) => idx - (VisibleDiscs / 2);
	public int SelectIndexToDiscIndex(int idx) => idx + (VisibleDiscs / 2);


	public float DiscVibrate = 0;
	public float FlyAway = 0;

	public SongDiscButton GetActiveDisc() => Discs[Discs.Length / 2];

	AudioPlaybackHandle activeTrack;
	bool doNotTryToGetTrackAgain;
	public AudioPlaybackHandle ActiveTrack => activeTrack;

	public void ResetDiskTrack() {
		if (IValidatable.IsValid(activeTrack)) {
			audiosystem.DestroyPlayback(activeTrack);
			activeTrack = AudioPlaybackHandle.Null;
		}
		doNotTryToGetTrackAgain = false;
	}

	protected override bool MouseScroll(Element self, FrameState state, Vector2F delta) {
		if (delta.Y == 0) return true;

		for (int i = 0; i < Math.Abs(delta.Y); i++) {
			if (delta.Y > 0)
				MoveLeft();
			else
				MoveRight();
		}

		InvalidateLayout();
		return true;
	}

	bool wasBusy;
	public void FigureOutDisk() {
		if (IValidatable.IsValid(activeTrack))
			audiosystem.UpdatePlayback(activeTrack);

		if (Source == null || Source.IsBusy()) {
			wasBusy = true;
			return;
		}
		if (Source.GetSongCount() <= 0) return;

		if (!Source.IsBusy() && wasBusy) {
			wasBusy = false;
			InvalidateLayout();
			UpdateFilterText();
		}

		if (doNotTryToGetTrackAgain)
			return;

		// Should play track?
		if (Math.Abs(DiscAnimationOffset.Out) < 0.3) {
			var chart = GetDiscSong(0);
			audiosystem.DestroyPlayback(activeTrack);
			var clip = chart?.GetDemoAudio();

			if (!IValidatable.IsValid(clip)) {
				doNotTryToGetTrackAgain = chart == null || !chart.IsAsynchronouslyLoading();
				return;
			}

			clip.BindVolumeToConVar(AudioSettings.snd_musicvolume);
			activeTrack = audiosystem.CreatePlayback(clip, AudioPlaybackSettings.Unaltered with {
				Looping = true,
				ManuallyUpdate = true,
				Stream = true
			});
			audiosystem.PlaySound(activeTrack);
			doNotTryToGetTrackAgain = true;
		}
	}

	public bool InSheetSelection { get; private set; }
	public float TargetRotationPostExit { get; private set; }
	public void EnterSheetSelection() {
		InSheetSelection = true;
		TargetRotationPostExit = 1;
	}
	public void ExitSheetSelection() {
		InSheetSelection = false;
		if (DiscRotateAnimation % 360 > 180) {
			var v = DiscRotateAnimation % 180 - 180;
			DiscRotateSOS.ResetTo((float)v);
			DiscRotateAnimation = 0;
		}
		else
			DiscRotateAnimation = (int)(DiscRotateAnimation / 360) * 360;
		FlyAway = 0;
		DiscVibrate = 0;
		InvalidateLayout();
	}

	public void NavigateToDisc(Button disc) {
		var idx = -1;
		for (int i = 0; i < Discs.Length; i++) {
			if (Discs[i] == disc) {
				idx = i;
				break;
			}
		}

		if (idx == -1)
			throw new Exception("How");

		if (Source == null)
			return;

		var song = Source.At(DiscIndexToSelectIndex(idx));
		Source.Select(song, CommitMove);
	}

	public Label Loading = null!;
	// Constantly running logic
	public void ThinkDiscs() {
		if (Math.Abs(DiscAnimationOffset.Out) > 0.005d) {
			DiscAnimationOffset.Update(0);
			InvalidateLayout(); // loop for next frame
		}
		else if (DiscAnimationOffset.Out != 0) {
			// set it to 0 and don't invalidate again after
			DiscAnimationOffset.ResetTo(0);
			InvalidateLayout();
		}

		FigureOutDisk();

		float width = GetRenderBounds().W, height = GetRenderBounds().H;
		ChildRenderOffset = new(0, (float)NMath.Ease.InCirc(1 - Math.Clamp(Lifetime, 0, 0.5) / 0.5) * (width / 2));

		// Hack... but no better way right now
		if (Math.Abs(DiscAnimationOffset.Value) < 0.05f && this.IsKeyboardFocused()) {
			ref KeyboardState keyboard = ref Level.FrameState.Keyboard;
			if (keyboard.IsKeyDown(ButtonCode.KeyLeft) || keyboard.IsKeyDown(ButtonCode.KeyA) && !keyboard.WasKeyPressed(ButtonCode.KeyA)) {
				MoveLeft();
				InvalidateLayout();
			}
			else if (keyboard.IsKeyDown(ButtonCode.KeyRight) || keyboard.IsKeyDown(ButtonCode.KeyD) && !keyboard.WasKeyPressed(ButtonCode.KeyD)) {
				MoveRight();
				InvalidateLayout();
			}
		}

		if (FlyAwaySOS.Update(FlyAway) > 0.001f || ChildRenderOffset.Y > 0) {
			InvalidateLayout();
		}

		for (int i = 0; i < Discs.Length; i++) {
			var disc = Discs[i];
			var index = disc.GetTag<int>("localDiscIndex");

			if (i == Discs.Length / 2 && (FlyAwaySOS.Out > 0.00001 || Math.Abs(DiscRotateSOS.Out) > 0.00001)) {
				disc.SetImageRotation(DiscRotateSOS.Update((float)(
					Math.Floor(DiscRotateAnimation / 360) * 360
					+ DiscRotateAnimation % 360
				)));

				var discWidth = GetDiscSize(width, disc);
				float size = discWidth * (FlyAwaySOS.Out / 4 + 1) - DiscVibrate;
				CalculateDiscPos(width, height, i, out float x, out float y, out float rot);
				// DON'T do: disc.SetRenderBounds(x - size / 2, y - size / 2, size, size);
				// DO: set Position/Size and let DoOriginAnchor handle center-origin
				disc.SetSize(new(size, size));
				disc.SetPos(new(x, y));
			}


			var song = GetDiscSong(disc);
			if (song == null)
				continue;
			var cover = song.GetCoverTexture();

			disc.SetText("");
			if (cover.Texture != null) {
				disc.SetImageOrientation(ImageOrientation.Stretch);
				disc.SetImagePadding(new(16));
				disc.SetImage(cover.Texture);
				disc.SetImageFlipX(false);
				disc.SetImageFlipY(cover.Flipped);
			}
		}
	}

	public void CalculateDiscPos(float width, float height, int index, out float x, out float y, out float rot) {
		var offsetYParent = ChildRenderOffset.Y / (width / 2);
		float flyAway = FlyAwaySOS.Out - offsetYParent * -0.5f;
		float flyAwayMw = flyAway * width;

		var lrOut = DiscAnimationOffset.Out % 5;

		var widthRatio = MathF.Cos((float)NMath.Remap(index + lrOut, 0, Discs.Length - 1, -1 - flyAway * 2, 1 + flyAway * 2));
		x = (float)NMath.Remap(index + DiscAnimationOffset.Out, 0, Discs.Length - 1, -flyAwayMw, width + flyAwayMw);
		y = height / 2f + (1 - widthRatio) * 250;
		var rR = 150;
		rot = (float)NMath.Remap(index + lrOut, 0, Discs.Length - 1, -25 - flyAway * rR, 25 + flyAway * rR);
	}

	public float GetDiscSize(float width, SongDiscButton b) {
		var mainDiscMult = 0.75f - Math.Clamp(Math.Abs(b.GetTag<int>("localDiscIndex") + DiscAnimationOffset.Out), 0, 1);
		return width / Discs.Length + mainDiscMult * 64;
	}

	private void DisableDiscs(bool disabled) {
		for (int i = 0; i < Discs.Length; i++) {
			Discs[i].SetMouseInputEnabled(!disabled);
			Discs[i].SetVisible(!disabled);
		}
	}

	public void LayoutDiscs(float width, float height) {
		if (Source == null || (Source.GetSongCount() <= 0 && !Source.IsBusy())) {
			Loading.SetText("No songs available.");
			Loading.SetVisible(true);
			DisableDiscs(true);
			return;
		}

		if (Source.IsBusy()) {
			Loading.SetText("LOADING");
			Loading.SetVisible(true);
			DisableDiscs(true);
			return;
		}

		if (Loading.IsVisible()) {
			Loading.SetVisible(false);
			DisableDiscs(false);
		}

		for (int i = 0; i < Discs.Length; i++) {
			var disc = Discs[i];
			disc.SetVisible(true);
			var discWidth = GetDiscSize(width, disc);

			var song = GetDiscSong(DiscIndexToSelectIndex(i));
			disc.SetVisible(song != null);
			if (song == null)
				continue;

			disc.SetSize(new(discWidth, discWidth));

			CalculateDiscPos(width, height, i, out float x, out float y, out float rot);
			disc.SetImageRotation(rot);
			disc.SetPos(new(x, y));
			disc.SetText("");
		}

		var heightDiv2 = height / 2;

		CurrentTrackName.Origin = Anchor.Center;
		CurrentTrackName.Anchor = Anchor.Center;
		CurrentTrackName.SetAutoSize(true);

		CurrentTrackAuthor.Origin = Anchor.Center;
		CurrentTrackAuthor.Anchor = Anchor.Center;
		CurrentTrackAuthor.SetAutoSize(true);

		CurrentTrackName.SetPos(new(0, heightDiv2 / 1.8f));
		CurrentTrackAuthor.SetPos(new(0, heightDiv2 / 1.8f + 42));

		CurrentTrackName.SetTextSize(48);
		CurrentTrackAuthor.SetTextSize(24);

		var mainSong = GetDiscSong(0);
		var info = mainSong?.FetchMetadata(HumanLanguage.GetCurrentLanguage());
		if (info != null) {
			CurrentTrackName.SetText(info.Value.Name ?? "");
			CurrentTrackAuthor.SetText(info.Value.Author ?? "");
		}
	}

	public static int VisibleDiscs => 5;

	public readonly int IntegerMidpoint;

	public SongSelector(Element? parent) : base(parent) {
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);

		Discs = new SongDiscButton[VisibleDiscs];
		IntegerMidpoint = Discs.Length / 2;
		for (int i = 0; i < VisibleDiscs; i++)
			Discs[i] = new(this, i);

		CurrentTrackName = new(this);
		CurrentTrackAuthor = new(this);
		SearchBar = new(this);
		FilterResults = new(this);
		FilterResults.Anchor = Anchor.TopCenter;
		FilterResults.Origin = Anchor.Center;

		SearchBar.OnButtonClick += SearchBar_MouseReleaseEvent;

		Loading = new(this);
		Loading.Anchor = Anchor.Center;
		Loading.Origin = Anchor.Center;
		Loading.SetText("LOADING");
		Loading.SetTextSize(100);
		Loading.SetAutoSize(true);
		Loading.SetVisible(false);

		for (int i = 0; i < Discs.Length; i++) {
			var disc = Discs[i];
			disc.SetVisible(false);
			disc.Origin = Anchor.Center;
			disc.SetTag("localDiscIndex", i - Discs.Length / 2);

			disc.OnButtonClick += (s, _) => {
				NavigateToDisc(s as Button);
				var song = GetDiscSong(0);
				LevelTransitions.LoadSongSelector(this, song);
			};
			disc.BorderSize = 0;
			disc.SetBgColor(new Color(0, 0, 0, 0));
			disc.SetImageColor(i == IntegerMidpoint ? new Color(255) : new Color(155));
		}

		KeyboardFocus();
	}

	private void SearchBar_MouseReleaseEvent(Button self, ButtonCode button) {
		TriggerUserInitializeSearch();
	}

	protected override bool MouseClick(FrameState state, ButtonCode button) {
		base.MouseClick(state, button);
		KeyboardFocus();
		return true;
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		LayoutDiscs(width, height);
		SearchBar.SetPos(new(width / 2, height * .1f));
		SearchBar.SetSize(new(width / 2f, height * 0.06f));
		FilterResults.SetPos(new(0, height * .1f + height * 0.06f + height * 0.00f));
		FilterResults.SetTextSize(height / 30f);
		FilterResults.SetAutoSize(true);
	}

	protected override bool KeyPressed(in KeyboardState keyboardState, ButtonCode key) {
		base.KeyPressed(in keyboardState, key);
		if (key == ButtonCode.KeyLeft || key == ButtonCode.KeyA) {
			MoveLeft();
			InvalidateLayout();
		}
		else if (key == ButtonCode.KeyRight || key == ButtonCode.KeyD) {
			MoveRight();
			InvalidateLayout();
		}
		return true;
	}

	public bool InterceptEscape() {
		Panel? selectedSong = ((IMainMenuLevel)Level).GetSelectedSongPanel();
		if (IValidatable.IsValid(selectedSong)) {
			selectedSong.Remove();
			return false;
		}

		return true;
	}

	public override void Paint(float width, float height) {
		base.Paint(width, height);

		CurrentTrackName.SetTextColor(new(255, 255, 255, (int)(255 * (1 - FlyAway))));
		CurrentTrackAuthor.SetTextColor(new(255, 255, 255, (int)(255 * (1 - FlyAway))));
	}
}
