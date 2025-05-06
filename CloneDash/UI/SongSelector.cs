using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Raylib_cs;
using MouseButton = Nucleus.Input.MouseButton;
using CloneDash.Data;
using CloneDash.Animation;
using Nucleus.Audio;
using static CloneDash.CustomAlbumsCompatibility;
using CloneDash.Game;
using System.Collections.Concurrent;
using static CloneDash.MuseDashCompatibility;
using Nucleus.UI.Elements;
using CloneDash.Systems.CustomAlbums;
using static CloneDash.Systems.CustomAlbums.MDMCWebAPI;
using System.ComponentModel.DataAnnotations;
using Nucleus.Input;

namespace CloneDash.UI;

public abstract class SearchFilter
{
	public abstract void Populate(SongSearchDialog dialog);
	public abstract Predicate<ChartSong> BuildPredicate(SongSearchDialog dialog);

	public void TextInput(SongSearchDialog dialog, string field, string helperText, bool enterReturns = false) {
		var type = this.GetType();
		var fieldInfo = type.GetField(field);
		if (fieldInfo == null) throw new NotImplementedException();

		dialog.Add(out Textbox textbox);
		textbox.Dock = Dock.Top;
		textbox.Size = new(0.12f);
		if (enterReturns)
			textbox.OnUserPressedEnter += (_, _, _) => dialog.Submit();

		textbox.DynamicallySized = true;

		textbox.TextSize = 10;
		textbox.Text = fieldInfo.GetValue(this)?.ToString() ?? "";
		textbox.HelperText = helperText;
		textbox.TextSize = 20;
		textbox.BorderSize = 0;
		textbox.OnTextChanged += (_, _, nt) => fieldInfo.SetValue(this, nt);
		dialog.SetTag(field, textbox);
	}

	public void EnumInput<T>(SongSearchDialog dialog, string field, T curVal) where T : Enum {
		var type = this.GetType();
		var fieldInfo = type.GetField(field);
		if (fieldInfo == null) throw new NotImplementedException();

		var selector = dialog.Add(DropdownSelector<T>.FromEnum(curVal));
		selector.Dock = Dock.Top;
		selector.Size = new(48);
		selector.Text = fieldInfo.GetValue(this)?.ToString() ?? "";
		selector.TextSize = 20;
		selector.BorderSize = 0;
		selector.OnSelectionChanged += (_, _, nt) => fieldInfo.SetValue(this, nt);
		dialog.SetTag(field, selector);
	}

	public void CheckboxInput(SongSearchDialog dialog, string field, string label) {
		var type = this.GetType();
		var fieldInfo = type.GetField(field);
		if (fieldInfo == null) throw new NotImplementedException();

		var selector = dialog.Add<CheckboxButton>();
		selector.Dock = Dock.Top;
		selector.Size = new(48);
		selector.Text = label;
		selector.TextSize = 20;
		selector.BorderSize = 0;
		selector.Checked = (bool?)fieldInfo.GetValue(this) ?? false;
		selector.OnCheckedChanged += (s) => fieldInfo.SetValue(this, s.Checked);
		dialog.SetTag(field, selector);
	}
}
public class MuseDashSearchFilter : SearchFilter
{
	public string FilterText;

	public override void Populate(SongSearchDialog dialog) {
		TextInput(dialog, nameof(FilterText), "Filter by name, song author, etc..", true);
	}

	public override Predicate<ChartSong> BuildPredicate(SongSearchDialog dialog) {
		dialog.SetBarText(FilterText);
		return x =>
			x is MuseDashSong mds && (
				FilterText == null ? true :
				mds.Name.ToLower().Contains(FilterText.ToLower()) ||
				mds.BaseName.ToLower().Contains(FilterText.ToLower()) ||
				mds.Author.ToLower().Contains(FilterText.ToLower())
			);
	}
}

public class MDMCSearchFilter : SearchFilter
{
	public int Page = 0;
	public string Query = "";
	public MDMCWebAPI.Sort Sort;
	public bool OnlyRanked = false;


	public override void Populate(SongSearchDialog dialog) {
		TextInput(dialog, nameof(Query), "Search Query", true);
		EnumInput(dialog, nameof(Sort), Sort);
		CheckboxInput(dialog, nameof(OnlyRanked), "Only ranked charts?");
	}

	public override Predicate<ChartSong> BuildPredicate(SongSearchDialog dialog) {
		var menu = dialog.Level.As<CD_MainMenu>();
		Page = 0;
		dialog.Selector.ClearSongs();
		//PopulateMDMCCharts(dialog.Selector);
		return x => true;
	}

	private CustomChartsSong AddChartSelector(MDMCChart chart) {
		CustomChartsSong song = new CustomChartsSong(chart);
		return song;
	}

	internal void PopulateMDMCCharts(SongSelector selector) {
		Page++;

		MDMCWebAPI.SearchCharts(string.IsNullOrWhiteSpace(Query) ? null : Query, Sort, Page, OnlyRanked).Then((resp) => {
			MDMCChart[] charts = resp.FromJSON<MDMCChart[]>() ?? throw new Exception("Parsing failure");
			if (charts.Length == 0) {
				selector.MarkNoMoreSongsLeft();
				return;
			}
			var songs = new List<CustomChartsSong>();

			foreach (MDMCChart chart in charts) {
				songs.Add(AddChartSelector(chart));
			}

			selector?.AddSongs(songs);
			selector?.AcceptMoreSongs();
		});
	}
}


public class SongSelector : Panel, IMainMenuPanel
{
	public string GetName() => "Song Selector";
	public void OnHidden() { }
	public void OnShown() { }
	public List<ChartSong> Songs { get; set; } = [];
	public List<ChartSong>? SongsPostFilter { get; set; }

	public Predicate<ChartSong>? CompiledFilter { get; private set; }

	public List<ChartSong> GetSongsList() => SongsPostFilter ?? Songs;

	public SongSearchBar SearchBar;
	public Label FilterResults;
	public SearchFilter? SearchFilter;
	public SongSearchDialog? ActiveDialog;

	public void TriggerUserInitializeSearch() {
		if (SearchFilter == null) return;
		UI.Add(out ActiveDialog);
		ActiveDialog.Selector = this;
		ActiveDialog.Bar = SearchBar;
		ActiveDialog.OnUserSubmit += () => TriggerUserSubmittedSearch();

		SearchFilter.Populate(ActiveDialog);
	}

	public void TriggerUserSubmittedSearch() {
		if (SearchFilter == null) return;
		if (!IValidatable.IsValid(ActiveDialog)) return;

		ApplyPredicate(SearchFilter.BuildPredicate(ActiveDialog));
	}

	public void AddSongs(IEnumerable<ChartSong> songs) {
		Songs.AddRange(songs);

		ApplyPredicate(CompiledFilter);
		InvalidateLayout();
	}

	public void ClearSongs() {
		Songs.Clear();
		DiscIndex = 0;
		InvalidateLayout();

		CanAcceptMoreSongs = true;
		NoMoreSongsLeft = false;
	}

	protected override void OnThink(FrameState frameState) {
		base.OnThink(frameState);
		ThinkDiscs();
	}

	public void ApplyPredicate(Predicate<ChartSong>? filter) {
		if (filter == null) {
			ClearFilter();
			return;
		}

		ConcurrentBag<ChartSong> multicoreSearchBag = [];

		Parallel.ForEach(Songs, (song) => {
			if (filter(song))
				multicoreSearchBag.Add(song);
		});

		SongsPostFilter = multicoreSearchBag.ToList();
		SongsPostFilter.Sort((x, y) => x.Name.CompareTo(y.Name));
		CompiledFilter = filter;

		SelectionUpdated(false);
		InvalidateLayout();
		DiscIndex = 0;
		ResetDiskTrack();
		FilterResults.Text = $"{SongsPostFilter.Count}/{Songs.Count} songs available";
	}

	public void ClearFilter() {
		SongsPostFilter?.Clear();
		SongsPostFilter = null;
		SelectionUpdated(true);
		FilterResults.Text = $"{Songs.Count} songs available";
	}

	public delegate void UserWantsMore();
	public event UserWantsMore? UserWantsMoreSongs;
	public bool CanAcceptMoreSongs { get; set; } = true;
	public bool NoMoreSongsLeft { get; set; } = false;
	public bool InfiniteList { get; set; } = true;

	public float DiscRotateAnimation { get; set; } = 0;

	public SecondOrderSystem DiscRotateSOS = new(2f, 0.94f, 1.1f, 0);
	public SecondOrderSystem FlyAwaySOS = new(1.5f, 0.94f, 1.1f, 0);

	protected virtual void SelectionUpdated(bool cleared) {

	}

	protected void GetMoreSongs() {
		if (!CanAcceptMoreSongs) return;
		if (NoMoreSongsLeft) return;

		Loading.Visible = true;
		Loading.MoveToFront();
		CanAcceptMoreSongs = false;
		UserWantsMoreSongs?.Invoke();
	}

	public Label CurrentTrackName;
	public Label CurrentTrackAuthor;

	public int DiscIndex = 0;
	public Button[] Discs;

	public SecondOrderSystem DiscAnimationOffset = new SecondOrderSystem(1.5f, 1, 1, 0);

	public void MoveLeft() {
		if (!InfiniteList && DiscIndex <= 0)
			return;

		DiscIndex--;
		DiscAnimationOffset.ResetTo(-1);
		ResetDiskTrack();
	}

	public void MoveRight() {
		if (!InfiniteList && DiscIndex >= GetSongsList().Count - 1)
			return;

		DiscIndex++;
		DiscAnimationOffset.ResetTo(1);
		ResetDiskTrack();
	}

	public int GetSongIndex(int localIndex) => GetSongsList().Count == 0 ? localIndex : NMath.Modulo(DiscIndex + localIndex, GetSongsList().Count);
	public ChartSong GetDiscSong(int localIndex) {
		var songIndex = GetSongIndex(localIndex);
		return GetSongsList()[songIndex];
	}
	public ChartSong GetDiscSong(Button discButton) {
		int localIndex = discButton.GetTagSafely<int>("localDiscIndex");
		var songIndex = GetSongIndex(localIndex);

		return GetSongsList()[songIndex];
	}

	public void NavigateToDisc(Button discButton) {
		var index = discButton.GetTagSafely<int>("localDiscIndex");
		DiscIndex += index;
		DiscAnimationOffset.ResetTo(DiscAnimationOffset.Out + index);
		if (index != 0)
			ResetDiskTrack();
	}

	public bool IsDiscOverflowed(int localIndex) {
		var songIndex = DiscIndex + localIndex;
		return songIndex >= GetSongsList().Count || songIndex < 0;
	}

	public bool WillDiscOverflow() {
		return IsDiscOverflowed(DiscIndex);
	}

	public float DiscVibrate = 0;
	public float FlyAway = 0;

	public Button GetActiveDisc() => Discs[Discs.Length / 2];

	MusicTrack? activeTrack;
	bool doNotTryToGetTrackAgain;
	public MusicTrack? ActiveTrack => activeTrack;
	public void ResetDiskTrack() {
		if (IValidatable.IsValid(activeTrack)) {
			activeTrack.Playing = false;
			activeTrack = null;
		}
		doNotTryToGetTrackAgain = false;
	}

	public override void MouseScroll(Element self, FrameState state, Vector2F delta) {
		if (delta.Y == 0) return;

		for (int i = 0; i < Math.Abs(delta.Y); i++) {
			if (delta.Y > 0)
				MoveLeft();
			else
				MoveRight();
		}

		InvalidateLayout();
	}

	public void FigureOutDisk() {
		if (GetSongsList().Count <= 0) return;
		activeTrack?.Update();
		if (IValidatable.IsValid(activeTrack)) return;
		if (doNotTryToGetTrackAgain) return;

		// Should play track?
		if (Math.Abs(DiscAnimationOffset.Out) < 0.3) {
			var chart = GetDiscSong(0);
			activeTrack = chart.GetDemoTrack();

			if (activeTrack == null) {
				doNotTryToGetTrackAgain = !chart.IsLoadingDemoAsync;
				return;
			}

			activeTrack.Restart();
			activeTrack.Playing = true;
			activeTrack.Volume = 0.5f;
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
			DiscRotateSOS.ResetTo(DiscRotateAnimation % 180 - 180);
		}
		DiscRotateAnimation = 0;
		FlyAway = 0;
		DiscVibrate = 0;
		InvalidateLayout();
	}

	public Label Loading;
	// Constantly running logic
	public void ThinkDiscs() {
		FigureOutDisk();
		RequestKeyboardFocus();

		float width = RenderBounds.W, height = RenderBounds.H;
		ChildRenderOffset = new(0, (float)NMath.Ease.InCirc(1 - Math.Clamp(Lifetime, 0, 0.5) / 0.5) * (width / 2));

		if (FlyAwaySOS.Update(FlyAway) > 0.001f || ChildRenderOffset.Y > 0) {
			LayoutDiscs(width, height);
		}

		for (int i = 0; i < Discs.Length; i++) {
			var disc = Discs[i];
			var index = DiscIndex + disc.GetTagSafely<int>("localDiscIndex");

			if (i == Discs.Length / 2 && (FlyAwaySOS.Out > 0.00001 || Math.Abs(DiscRotateSOS.Out) > 0.00001)) {
				disc.ImageRotation = DiscRotateSOS.Update(
					MathF.Floor(DiscRotateAnimation / 360) * 360
					+ DiscRotateAnimation % 360
				);

				var discWidth = GetDiscSize(width, disc);
				float size = discWidth * (FlyAwaySOS.Out / 4 + 1) - DiscVibrate;
				CalculateDiscPos(width, height, i, out float x, out float y, out float rot);
				disc.SetRenderBounds(x - size / 2, y - size / 2, size, size);
			}

			if (!InfiniteList && index > GetSongsList().Count) continue;

			if (GetSongsList().Count > 0) {
				var song = GetDiscSong(disc);
				var cover = song.GetCover();

				disc.Text = "";
				if (cover != null) {
					disc.ImageOrientation = ImageOrientation.Stretch;
					disc.ImagePadding = new(16);
					disc.Image = cover.Texture;
					disc.ImageFlipX = false;
					disc.ImageFlipY = cover.Flipped;
				}
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

	public float GetDiscSize(float width, Button b) {
		var mainDiscMult = 0.75f - Math.Clamp(Math.Abs(b.GetTagSafely<int>("localDiscIndex") + DiscAnimationOffset.Out), 0, 1);
		return width / Discs.Length + mainDiscMult * 64;
	}

	private bool discsDisabled;
	private void DisableDiscs(bool disabled) {
		for (int i = 0; i < Discs.Length; i++) {
			Discs[i].InputDisabled = disabled;
		}
		discsDisabled = disabled;
	}

	public void LayoutDiscs(float width, float height) {
		if (GetSongsList().Count() <= 0) {
			Loading.Text = "No songs available.";
			DisableDiscs(true);
			return;
		}

		Loading.Text = "LOADING";

		if (!NoMoreSongsLeft && (!InfiniteList && WillDiscOverflow())) {
			GetMoreSongs();
			DisableDiscs(true);
			return;
		}

		DisableDiscs(false);
		for (int i = 0; i < Discs.Length; i++) {
			var disc = Discs[i];
			disc.Visible = true;
			var discWidth = GetDiscSize(width, disc);

			var willOverflow = !InfiniteList && IsDiscOverflowed(i - Discs.Length / 2);
			if (willOverflow) {
				disc.Visible = false;
				continue;
			}
			else {
				disc.Visible = true;
			}

			disc.Size = new(discWidth, discWidth);

			CalculateDiscPos(width, height, i, out float x, out float y, out float rot);
			disc.ImageRotation = rot;
			disc.Position = new(x, y);
			disc.Text = "";
		}

		var heightDiv2 = height / 2;

		CurrentTrackName.Origin = Anchor.Center;
		CurrentTrackName.Anchor = Anchor.Center;
		CurrentTrackName.AutoSize = true;

		CurrentTrackAuthor.Origin = Anchor.Center;
		CurrentTrackAuthor.Anchor = Anchor.Center;
		CurrentTrackAuthor.AutoSize = true;

		CurrentTrackName.Position = new(0, heightDiv2 / 1.8f);
		CurrentTrackAuthor.Position = new(0, heightDiv2 / 1.8f + 42);

		CurrentTrackName.TextSize = 48;
		CurrentTrackAuthor.TextSize = 24;

		var mainSong = GetDiscSong(0);
		var info = mainSong.GetInfo();
		if (info != null) {
			CurrentTrackName.Text = mainSong.Name;
			CurrentTrackAuthor.Text = mainSong.Author;
		}

		if (Math.Abs(DiscAnimationOffset.Out) > 0.005d) {
			DiscAnimationOffset.Update(0);
			InvalidateLayout(); // loop for next frame
		}
		else if (DiscAnimationOffset.Out != 0) {
			// set it to 0 and don't invalidate again after
			DiscAnimationOffset.ResetTo(0);
			InvalidateLayout();
		}
	}

	public static int VisibleDiscs => 5;

	protected override void Initialize() {
		base.Initialize();
		DrawPanelBackground = false;

		Discs = new Button[VisibleDiscs];
		for (int i = 0; i < VisibleDiscs; i++) 
			Add(out Discs[i]);
		
		Add(out CurrentTrackName);
		Add(out CurrentTrackAuthor);
		Add(out SearchBar);
		Add(out FilterResults);
		FilterResults.Anchor = Anchor.TopCenter;
		FilterResults.Origin = Anchor.Center;

		SearchBar.MouseReleaseEvent += SearchBar_MouseReleaseEvent;

		Add(out Loading);
		Loading.Anchor = Anchor.Center;
		Loading.Origin = Anchor.Center;
		Loading.Text = "LOADING";
		Loading.TextSize = 100;
		Loading.AutoSize = true;
		Loading.Visible = false;

		for (int i = 0; i < Discs.Length; i++) {
			var disc = Discs[i];
			disc.Visible = false;
			disc.Origin = Anchor.Center;
			disc.SetTag("localDiscIndex", i - Discs.Length / 2);

			disc.MouseReleaseEvent += (s, _, _) => {
				NavigateToDisc(s as Button);
				var song = GetDiscSong(0);
				if (song is CustomChartsSong customChartsSong) {
					customChartsSong.DownloadOrPullFromCache((c) => EngineCore.Level.As<CD_MainMenu>().LoadChartSelector(this, c));
				}
				else
					EngineCore.Level.As<CD_MainMenu>().LoadChartSelector(this, song);
			};
			disc.BorderSize = 0;
			var midpoint = Discs.Length / 2;
			disc.BackgroundColor = new(0, 0, 0, 0);
			disc.ImageColor = i == midpoint ? new Color(255) : new Color(155);
			disc.PaintOverride += (s, w, h) => {
				var a = i == midpoint ? 1 - FlyAway : 1;
				var c = MixColorBasedOnMouseState(s, new(35, (int)(255 * a)), new(0, 1, 2, 1), new(0, 1, 0.5f, 1));
				Graphics2D.SetDrawColor(c);
				Graphics2D.DrawCircle(new(w / 2, h / 2), w / 2 - 8);
				ImageColor = new(255, 255, 255, (int)(255 * a));
				s.Paint(w, h);
			};
		}

		DemandKeyboardFocus();
	}

	private void SearchBar_MouseReleaseEvent(Element self, FrameState state, MouseButton button) {
		TriggerUserInitializeSearch();
	}

	public override void MouseClick(FrameState state, MouseButton button) {
		base.MouseClick(state, button);
		DemandKeyboardFocus();
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		LayoutDiscs(width, height);
		SearchBar.Position = new(width / 2, height * .1f);
		SearchBar.Size = new(width / 2f, height * 0.06f);
		FilterResults.Position = new(0, (height * .1f) + (height * 0.06f) + (height * 0.00f));
		FilterResults.TextSize = height / 30f;
		FilterResults.AutoSize = true;
	}

	public override void KeyPressed(KeyboardState keyboardState, Nucleus.Input.KeyboardKey key) {
		base.KeyPressed(keyboardState, key);
		if (key == KeyboardLayout.USA.Left || key == KeyboardLayout.USA.A) {
			MoveLeft();
			InvalidateLayout();
		}
		else if (key == KeyboardLayout.USA.Right || key == KeyboardLayout.USA.D) {
			MoveRight();
			InvalidateLayout();
		}
	}

	public override void Paint(float width, float height) {
		base.Paint(width, height);

		CurrentTrackName.TextColor = new(255, 255, 255, (int)(255 * (1 - FlyAway)));
		CurrentTrackAuthor.TextColor = new(255, 255, 255, (int)(255 * (1 - FlyAway)));
	}

	internal void AcceptMoreSongs() {
		CanAcceptMoreSongs = true;
		Loading.Visible = false;
	}

	internal void MarkNoMoreSongsLeft() {
		NoMoreSongsLeft = true;
		Loading.Visible = false;
		CanAcceptMoreSongs = false;
	}

}
