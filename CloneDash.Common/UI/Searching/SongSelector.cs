using CloneDash.Charts;
using CloneDash.Common.Songs;
using CloneDash.Common.Systems.Discord;
using CloneDash.Game;
using CloneDash.Menu.Searching;
using CloneDash.Settings;
using Nucleus;
using Nucleus.Common.Audio;
using Nucleus.Common.Graphics;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using System.Numerics;

namespace CloneDash.Common.UI.Searching;

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
	#region IMainMenuPanel

	string IMainMenuPanel.Name => "Song Select";

	void IMainMenuPanel.SetRichPresence() {
		RichPresenceSystem.SetPresence(new RichPresenceState {
			Details = "Main Menu",
			State = "Picking a chart"
		});
	}


	private IMainMenuLevel _mainMenu = null!;
	public IMainMenuLevel GetMainMenu() => _mainMenu;
	public void SetMainMenu(IMainMenuLevel level) => _mainMenu = level;

	#endregion

	private readonly SongSearchBar _searchBar;
	private readonly SongLabel _filterResults;
	private SongSearchDialog? _activeDialog;
	private IChartSongFilter? _searchFilter;
	private bool _isFirst = true;

	/// <summary>
	/// Triggers IsFirst to false, which causes previous convars storing state to be
	/// completely ignored; used for drag n drop
	/// </summary>
	public void IgnorePreviousState() {
		_isFirst = false;
	}

	private readonly IChartSongProvider _provider;
	public IChartSongProvider GetProvider() => _provider;
	private ISongSourceState? _source;
	public void SetSource(ISongSourceState source) {
		_source = source;
		ClearSongs();
	}

	public void TriggerUserInitializeSearch() {
		if (_source == null) return;
		_searchFilter = _source.NewFilter();

		_activeDialog = new SongSearchDialog(UI);
		_activeDialog.MakeModal();
		_activeDialog.Selector = this;
		_activeDialog.Bar = _searchBar;
		_activeDialog.OnUserSubmit += () => TriggerUserSubmittedSearch();

		_searchFilter.PopulateFields(_activeDialog);
	}

	public void TriggerUserSubmittedSearch() {
		if (_source == null) return;
		if (!IValidatable.IsValid(_activeDialog)) return;

		if (_searchFilter == null) {
			ClearFilter();
			return;
		}

		_source = _activeDialog.Apply(_source, _searchFilter);
		_provider.UpdateSavedFilter(_source.NewFilter());
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
	public bool IsFiltered => _source?.GetParentSource() != null;
	public int SongCountFiltered => _source?.GetSongCount() ?? 0;
	public int SongCountTotal => _source?.GetRootSource().GetSongCount() ?? 0;

	public string GetFilterText() {
		if (_source == null)
			return "Source == null?";

		string text = !IsFiltered ? $"{SongCountTotal} songs available" : $"{SongCountFiltered}/{SongCountTotal} songs filtered";

		return text;
	}

	public void UpdateFilterText() => _filterResults.Text = GetFilterText();

	public void ClearFilter() {
		_source = _source?.GetRootSource();
		_provider.UpdateSavedFilter(null);
		UpdateFilterText();
	}

	public delegate void UserWantsMore();
	public event UserWantsMore? UserWantsMoreSongs;

	public double DiscRotateAnimation { get; set; }

	public SecondOrderSystem DiscRotateSos = new(2f, 0.94f, 1.1f, 0);
	public SecondOrderSystem FlyAwaySos = new(1.5f, 0.94f, 1.1f, 0);

	protected void GetMoreSongs() {
		Loading.SetVisible(true);
		Loading.MoveToFront();
		UserWantsMoreSongs?.Invoke();
	}

	public class SongDiscButton : Button
	{
		private readonly SongSelector _selector;
		private readonly int _i;
		private readonly Image _imageRenderer;
		public SongDiscButton(SongSelector selector, int i) : base(selector) {
			_selector = selector;
			_i = i;
			_imageRenderer = new Image(this) { Dock = Dock.Fill };
		}
		public override void Paint(float w, float h) {
			float a;
			if (_selector.InSheetSelection)
				a = _i == _selector.IntegerMidpoint ? _selector.FlyAway : 1;
			else
				a = 1;


			Vector2F center = new Vector2F(w / 2, h / 2);
			const float borderThickness = 2f;
			
			float outerRadius = w / 2 - borderThickness;
			float innerRadius = outerRadius - borderThickness;

			Color outlineColor = GetScheme()?.GetColor("Menu.Accent.Primary") ?? Color.White;
			Color backgroundBase = GetScheme()?.GetColor("Menu.Accent.Background") ?? Color.Black;
			Color backgroundColor = MixColorBasedOnMouseState(this, backgroundBase, new Vector4(0, 1, 2, 1), new Vector4(0, 1, 0.5f, 1));
			
			Graphics2D.SetDrawColor(outlineColor);
			Graphics2D.DrawCircle(center, outerRadius);

			Graphics2D.SetDrawColor(backgroundColor);
			Graphics2D.DrawCircle(center, innerRadius);
			Opacity = a;
		}

		internal void SetImageRotation(float value) => _imageRenderer.ImageRotation = value;
		internal void SetImageOrientation(ImageOrientation value) => _imageRenderer.ImageOrientation = value;
		internal void SetImagePadding(Vector2F value) => _imageRenderer.ImagePadding = value;
		internal void SetImage(ITexture value) => _imageRenderer.Texture = value;
		internal void SetImageOffset(Vector2F value) => _imageRenderer.ImageRotationOffset = value;
		internal void SetImageFlipX(bool value) => _imageRenderer.ImageFlipX = value;
		internal void SetImageFlipY(bool value) => _imageRenderer.ImageFlipY = value;
		internal void SetImageColor(Color value) => _imageRenderer.ImageColor = value;
	}

	public SongLabel CurrentTrackName;
	public SongLabel CurrentTrackAuthor;
	public SongDiscButton[] Discs;
	public readonly SecondOrderSystem DiscAnimationOffset = new(4.5f, 1, 1, 0);

	public void MoveLeft() => _source?.MoveLeft(CommitMove);

	public void MoveRight() => _source?.MoveRight(CommitMove);

	private void CommitMove(in ChartSongSourceMoveFinish finished) {
		if (!finished.OperationExecuted)
			return;
		if (finished.Movement == 0)
			return;

		DiscAnimationOffset.ResetTo(finished.Movement);

		ResetDiskTrack();
		InvalidateLayout();
		UpdateFilterText();

		// Commit updates to the source
		if (!_isFirst)
			_provider.UpdateSavedSong(GetDiscSong(GetActiveDisc()));
	}

	public static int GetButtonLocalIndex(SongDiscButton discButton) => discButton.GetTag<int>("localDiscIndex");

	public ISong? GetDiscSong(SongDiscButton discButton) => _source?.At(GetButtonLocalIndex(discButton));
	public ISong? GetDiscSong(int idx) => _source?.At(idx);

	public int DiscIndexToSelectIndex(int idx) => idx - (VisibleDiscs / 2);
	public int SelectIndexToDiscIndex(int idx) => idx + (VisibleDiscs / 2);


	public float DiscVibrate;
	public float FlyAway;

	public SongDiscButton GetActiveDisc() => Discs[Discs.Length / 2];

	private AudioPlaybackHandle _activeTrack;
	private bool _doNotTryToGetTrackAgain;
	public AudioPlaybackHandle ActiveTrack => _activeTrack;

	public void ResetDiskTrack() {
		if (IValidatable.IsValid(_activeTrack)) {
			audiosystem.DestroyPlayback(_activeTrack);
			_activeTrack = AudioPlaybackHandle.Null;
		}
		_doNotTryToGetTrackAgain = false;
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

	private bool _wasBusy;
	public void FigureOutDisk() {
		if (IValidatable.IsValid(_activeTrack))
			audiosystem.UpdatePlayback(_activeTrack);

		if (_source == null || _source.IsBusy()) {
			_wasBusy = true;
			return;
		}
		if (_source.GetSongCount() <= 0) return;

		if (!_source.IsBusy() && _wasBusy) {
			_wasBusy = false;

			InvalidateLayout();
			UpdateFilterText();
		}

		if (_doNotTryToGetTrackAgain)
			return;

		// The source has loaded, so we can prepare our filters...
		LoadLastStateIfApplicable();

		// Should play track?
		if (Math.Abs(DiscAnimationOffset.Out) < 0.3) {
			ISong? chart = GetDiscSong(0);
			audiosystem.DestroyPlayback(_activeTrack);
			IAudioClip? clip = chart?.GetDemoAudio();

			if (!IValidatable.IsValid(clip)) {
				_doNotTryToGetTrackAgain = chart == null || !chart.IsAsynchronouslyLoading();
				return;
			}

			clip.BindVolumeToConVar(AudioSettings.snd_musicvolume);
			_activeTrack = audiosystem.CreatePlayback(clip, AudioPlaybackSettings.Unaltered with {
				Looping = true,
				ManuallyUpdate = true,
				Stream = true
			});
			audiosystem.PlaySound(_activeTrack);
			_doNotTryToGetTrackAgain = true;
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
			double v = DiscRotateAnimation % 180 - 180;
			DiscRotateSos.ResetTo((float)v);
			DiscRotateAnimation = 0;
		}
		else
			DiscRotateAnimation = (int)(DiscRotateAnimation / 360) * 360;
		FlyAway = 0;
		DiscVibrate = 0;
		InvalidateLayout();
	}

	public void NavigateToSong(ISong? song, bool animated = true) {
		if (_source == null)
			return;

		// Get the song index
		int index = _source.Index(song);
		if (index == -1)
			return;

		_source.Select(song, CommitMove);
		if (!animated)
			DiscAnimationOffset.ResetTo(0);

	}

	public void NavigateToDisc(Button disc) {
		int idx = -1;
		for (int i = 0; i < Discs.Length; i++) {
			if (Discs[i] == disc) {
				idx = i;
				break;
			}
		}

		if (idx == -1)
			throw new Exception("How");

		if (_source == null)
			return;

		ISong? song = _source.At(DiscIndexToSelectIndex(idx));
		_source.Select(song, CommitMove);
	}

	public Label Loading;
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
		RenderOffset = new Vector2F(0, (float)NMath.Ease.InCirc(1 - Math.Clamp(Lifetime, 0, 0.5) / 0.5) * (width / 2));

		// Hack... but no better way right now
		if (Math.Abs(DiscAnimationOffset.Value) < 0.05f && GetMainMenu().IsHoldingSelectorKeys()) {
			// ref KeyboardState keyboard = ref Level.FrameState.Keyboard;
			if (GetMainMenu().IsHoldingLeftSelector()) {
				MoveLeft();
				InvalidateLayout();
			}
			else if (GetMainMenu().IsHoldingRightSelector()) {
				MoveRight();
				InvalidateLayout();
			}
		}

		for (int i = 0; i < GetMainMenu().MoveLeftsThisFrame(); i++) {
			MoveLeft();
			InvalidateLayout();
		}

		for (int i = 0; i < GetMainMenu().MoveRightsThisFrame(); i++) {
			MoveRight();
			InvalidateLayout();
		}


		if (FlyAwaySos.Update(FlyAway) > 0.001f || RenderOffset.Y > 0) {
			InvalidateLayout();
		}

		for (int i = 0; i < Discs.Length; i++) {
			SongDiscButton disc = Discs[i];
			// int index = disc.GetTag<int>("localDiscIndex");

			if (i == Discs.Length / 2 && (FlyAwaySos.Out > 0.00001 || Math.Abs(DiscRotateSos.Out) > 0.00001)) {
				disc.SetImageRotation(DiscRotateSos.Update((float)(
					Math.Floor(DiscRotateAnimation / 360) * 360
					+ DiscRotateAnimation % 360
				)));

				float discWidth = GetDiscSize(width, disc);
				float size = discWidth * (FlyAwaySos.Out / 4 + 1) - DiscVibrate;
				CalculateDiscPos(width, height, i, out float x, out float y, out float _);
				// DON'T do: disc.SetRenderBounds(x - size / 2, y - size / 2, size, size);
				// DO: set Position/Size and let DoOriginAnchor handle center-origin
				disc.               // DON'T do: disc.SetRenderBounds(x - size / 2, y - size / 2, size, size);
									// DO: set Position/Size and let DoOriginAnchor handle center-origin
				Size = new Vector2F(size, size);
				disc.Position = new Vector2F(x, y);
			}


			ISong? song = GetDiscSong(disc);
			if (song == null)
				continue;
			SongCoverInfo cover = song.GetCoverTexture();

			disc.
			Text = "";
			if (cover.Texture != null) {
				disc.SetImageOrientation(ImageOrientation.Stretch);
				disc.SetImagePadding(new Vector2F(32));
				disc.SetImage(cover.Texture);
				disc.SetImageOffset(new Vector2F(0.5f));
				disc.SetImageFlipX(false);
				disc.SetImageFlipY(cover.Flipped);
			}
		}
	}

	public void CalculateDiscPos(float width, float height, int index, out float x, out float y, out float rot) {
		float offsetYParent = RenderOffset.Y / (width / 2);
		float flyAway = FlyAwaySos.Out - offsetYParent * -0.5f;
		float flyAwayMw = flyAway * width;

		float lrOut = DiscAnimationOffset.Out % 5;

		float widthRatio = MathF.Cos(NMath.Remap(index + lrOut, 0, Discs.Length - 1, -1 - flyAway * 2, 1 + flyAway * 2));
		x = NMath.Remap(index + DiscAnimationOffset.Out, 0, Discs.Length - 1, -flyAwayMw, width + flyAwayMw);
		y = height / 2f + (1 - widthRatio) * 250;
		int rR = 150;
		rot = NMath.Remap(index + lrOut, 0, Discs.Length - 1, -25 - flyAway * rR, 25 + flyAway * rR);
	}

	public float GetDiscSize(float width, SongDiscButton b) {
		float mainDiscMult = 0.75f - Math.Clamp(Math.Abs(b.GetTag<int>("localDiscIndex") + DiscAnimationOffset.Out), 0, 1);
		return width / Discs.Length + mainDiscMult * 64;
	}

	private void DisableDiscs(bool disabled) {
		for (int i = 0; i < Discs.Length; i++) {
			Discs[i].SetMouseInputEnabled(!disabled);
			Discs[i].SetVisible(!disabled);
		}
	}

	public void LayoutDiscs(float width, float height) {
		if (_source == null || (_source.GetSongCount() <= 0 && !_source.IsBusy())) {
			Loading.Text = "No songs available.";
			Loading.SetVisible(true);
			DisableDiscs(true);
			return;
		}

		if (_source.IsBusy()) {
			Loading.Text = "LOADING";
			Loading.SetVisible(true);
			DisableDiscs(true);
			return;
		}

		if (Loading.IsVisible()) {
			Loading.SetVisible(false);
			DisableDiscs(false);
		}

		for (int i = 0; i < Discs.Length; i++) {
			SongDiscButton disc = Discs[i];
			disc.SetVisible(true);
			float discWidth = GetDiscSize(width, disc);

			ISong? song = GetDiscSong(DiscIndexToSelectIndex(i));
			disc.SetVisible(song != null);
			if (song == null)
				continue;

			disc.Size = new Vector2F(discWidth, discWidth);

			CalculateDiscPos(width, height, i, out float x, out float y, out float rot);
			disc.SetImageRotation(rot);
			disc.Position = new Vector2F(x, y);
		}

		float heightDiv2 = height / 2;

		CurrentTrackName.
		Origin = Anchor.Center;
		CurrentTrackName.Anchor = Anchor.Center;
		CurrentTrackName.SetAutoSize(true);

		CurrentTrackAuthor.
		Origin = Anchor.Center;
		CurrentTrackAuthor.Anchor = Anchor.Center;
		CurrentTrackAuthor.SetAutoSize(true);

		CurrentTrackName.
		Position = new Vector2F(0, heightDiv2 / 1.8f);
		CurrentTrackAuthor.Position = new Vector2F(0, heightDiv2 / 1.8f + 42);

		CurrentTrackName.
		TextSize = 48;
		CurrentTrackAuthor.TextSize = 24;

		ISong? mainSong = GetDiscSong(0);
		if (mainSong != null) {
			SongMetadata info = mainSong.FetchMetadata();
			CurrentTrackName.Text = info.Name;
			CurrentTrackAuthor.Text = info.Author;
		}
	}

	private void LoadLastStateIfApplicable() {
		if (!_isFirst)
			return;
		_isFirst = false;

		ISong? lastSong = _provider.GetSavedSong();
		IChartSongFilter? lastFilter = _provider.GetSavedFilter();

		if (lastFilter != null) {
			if (_source == null) return;

			_searchFilter = lastFilter;
			_source = _source.GetRootSource().ProduceNewSource(_searchFilter);
			ClearSongs();
		}

		if (lastSong != null)
			NavigateToSong(lastSong, false);
	}

	public static int VisibleDiscs => 5;

	public readonly int IntegerMidpoint;

	public SongSelector(Element? parent, IChartSongProvider provider) : base(parent) {
		_provider = provider;
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);

		Discs = new SongDiscButton[VisibleDiscs];
		IntegerMidpoint = Discs.Length / 2;
		for (int i = 0; i < VisibleDiscs; i++) {
			Discs[i] = new SongDiscButton(this, i);
			Discs[i].Text = "";
		}

		CurrentTrackName = new SongLabel(this);
		CurrentTrackAuthor = new SongLabel(this);
		_searchBar = new SongSearchBar(this);
		_filterResults = new SongLabel(this);
		_filterResults.Anchor = Anchor.TopCenter;
		_filterResults.Origin = Anchor.Center;

		_searchBar.OnButtonClick += SearchBar_MouseReleaseEvent;

		Loading = new Label(this);
		Loading.Anchor = Anchor.Center;
		Loading.Origin = Anchor.Center;
		Loading.Text = "LOADING";
		Loading.TextSize = 100;
		Loading.SetAutoSize(true);
		Loading.SetVisible(false);

		for (int i = 0; i < Discs.Length; i++) {
			SongDiscButton disc = Discs[i];
			disc.SetVisible(false);
			disc.Origin = Anchor.Center;
			disc.SetTag("localDiscIndex", i - Discs.Length / 2);

			disc.OnButtonClick += (s, _) => {
				NavigateToDisc(s);
				ISong? song = GetDiscSong(0);
				LevelTransitions.LoadSongSelector(this, song);
			};
			disc.BorderSize = 0;
			disc.SetBgColor(new Color(0, 0, 0, 0));
			disc.SetImageColor(i == IntegerMidpoint ? new Color(255) : new Color(155));
		}
	}

	private void SearchBar_MouseReleaseEvent(Button self, ButtonCode button) {
		TriggerUserInitializeSearch();
	}

	protected override bool MouseClick(FrameState state, ButtonCode button) {
		base.MouseClick(state, button);
		return true;
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		LayoutDiscs(width, height);
		_searchBar.Position = new Vector2F(width / 2, height * .1f);
		_searchBar.Size = new Vector2F(width / 2f, height * 0.06f);
		_filterResults.Position = new Vector2F(0, height * .1f + height * 0.06f + height * 0.00f);
		_filterResults.TextSize = height / 30f;
		_filterResults.SetAutoSize(true);
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

		CurrentTrackName.SetTextColor(new Color(255, 255, 255, (int)(255 * (1 - FlyAway))));
		CurrentTrackAuthor.SetTextColor(new Color(255, 255, 255, (int)(255 * (1 - FlyAway))));
	}
}
