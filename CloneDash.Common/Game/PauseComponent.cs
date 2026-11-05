using CloneDash.Menu;
using Nucleus;
using Nucleus.Common.Input;
using Nucleus.Engine;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace CloneDash.Common.Game;

public class PauseComponent(RhythmGameLevel level, MusicComponent music, GameEventManager events, InputComponent input) : IRhythmGameThinker
{
	public double DeltaUnpauseTime => level.Realtime - UnpauseTime;

	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Input;

	public Panel? PauseWindow { get; private set; }

	/// <summary>
	/// Is the game currently paused
	/// </summary>
	public double UnpauseTime { get; private set; } = 0;

	public void ForcePause() {
		music.Pause();
		SetPauseGuarded(true);
	}

	public void ForceUnpause() {
		music.Resume();
		SetPauseGuarded(false);
	}

	public void FullUnpause() {
		music.Resume();
		SetPauseGuarded(false);
		UnpauseTime = 0;
	}

	public bool SetPauseGuarded(bool paused) {
		if (!RequestPause(paused, false))
			return false;

		level.Paused = paused;
		return true;
	}

	// WIP pausing
	// return false to not spawn the pause menu
	public bool StartPause() {
		if (!RequestPause(true, true))
			return false;

		level.Paused = true;
		music.Pause();
		UnpauseTime = 0;
		return true;
	}

	public void StartUnpause() {
		events.Fire(new UnpauseStartGameEvent());
		UnpauseTime = level.Realtime;
		level.Timers.Simple(3, () => {
			FullUnpause();
		});
	}

	public void Think(FrameState frameState) {
		if (input.State.PauseButton) {
			TogglePauseMenu();
			level.EndFrame();
		}
	}

	public void TogglePauseMenu() {
		if (music.IsValid() && music.IsPaused) {
			StartUnpause();
			if (IValidatable.IsValid(PauseWindow))
				PauseWindow.Remove();
		}
		else {
			if (StartPause()) {
				PauseWindow = new Panel(level.RootPanel);
				PauseWindow.Size = new(300, 400);
				PauseWindow.Center();

				var flex = new FlexPanel(PauseWindow);
				flex.Dock = Dock.Fill;
				flex.Direction = Axis.Vertical;
				flex.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
				flex.DockPadding = RectangleF.TLRB(4);

				var play = new PauseMenuButton(flex, "ui/pause_play.png");
				play.BorderSize = 0;
				play.Text = "Return to Game";
				play.TextSize = 24;
				play.OnButtonClick += delegate (Button self, ButtonCode clickedButton) {
					PauseWindow.Remove();
					StartUnpause();
				};

				var restart = new PauseMenuButton(flex, "ui/pause_restart.png");
				restart.BorderSize = 0;
				restart.Text = "Restart Level";
				restart.TextSize = 24;
				restart.OnButtonClick += delegate (Button self, ButtonCode clickedButton) {
					// Interlude.Begin($"Reloading '{gameParameters.Chart?.Song?.Name ?? "<NULL>"}'...");
					// 
					// if (profilegameload.GetBool())
					// 	StaticSequentialProfiler.Start();
					// 
					// EngineCore.LoadLevel(new MuseDash1Game(gameParameters));

					// Maybe seeking is stable enough now to justify this though?
					level.Restart();
					PauseWindow.Remove();
					FullUnpause();
				};

				var settings = new PauseMenuButton(flex, "ui/pause_settings.png");
				settings.BorderSize = 0;
				settings.Text = "Open Preferences...";
				settings.TextSize = 24;
				settings.OnButtonClick += delegate (Button self, ButtonCode clickedButton) {
					var panel = new Panel(level.RootPanel);
					panel.SetPaintBackgroundEnabled(false);
					panel.Anchor = Anchor.Center;
					panel.Origin = Anchor.Center;
					panel.DynamicallySized = true;
					panel.Size = new(0.9f);

					var titlebar = new Titlebar(panel);
					titlebar.Dock = Dock.Top;
					titlebar.MinimizeButton.SetVisible(false);
					titlebar.MaximizeButton.SetVisible(false);
					titlebar.CloseButton.OnButtonClick += (_, _) => {
						panel.Remove();
					};
					titlebar.SetText("Settings");

					var settings = new SettingsEditor(panel);
					settings.Dock = Dock.Fill;
					settings.DockMargin = RectangleF.TLRB(0, 8, 8, 0);

					panel.MakePopup();
				};

				var back2menu = new PauseMenuButton(flex, "ui/pause_exit.png");
				back2menu.BorderSize = 0;
				back2menu.Text = "Exit to Menu";
				back2menu.TextSize = 24;
				back2menu.OnButtonClick += delegate (Button self, ButtonCode clickedButton) {
					LevelTransitions.LoadMainMenu();
				};
			}
		}
	}

	bool RequestPause(bool paused, bool fromMenu) {
		var ev = new PauseRequestGameEvent(paused, fromMenu, false);
		events.Fire(ref ev);
		return !ev.Cancel;
	}
}
