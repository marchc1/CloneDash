using Nucleus.Engine;
using Nucleus.Core;
using CloneDash.Game.Input;

using System.Numerics;
using Nucleus.Types;
using CloneDash.Game.Entities;
using Nucleus;
using Raylib_cs;
using System.ComponentModel;
using Nucleus.UI.Elements;
using Nucleus.UI;
using MouseButton = Nucleus.Types.MouseButton;
using CloneDash.Game.Logic;
using CloneDash.Levels;
using CloneDash.Data;
using Nucleus.Audio;
using CloneDash.Modding.Descriptors;
using CloneDash.Modding.Settings;
using System.Diagnostics;
using Nucleus.ManagedMemory;
using static CloneDash.MuseDashCompatibility;
using CloneDash.Animation;

namespace CloneDash.Game
{
	[Nucleus.MarkForStaticConstruction]
	public partial class CD_GameLevel(ChartSheet Sheet) : Level
	{
		public const double REFERENCE_FPS = 30;
		public static ConCommand clonedash_seek = ConCommand.Register("clonedash_seek", (_, args) => {
			var level = EngineCore.Level.AsNullable<CD_GameLevel>();
			if (level == null) {
				Logs.Warn("Not in game context!");
				return;
			}

			double? d = args.GetDouble(0);
			if (!d.HasValue) Logs.Warn("Did not specify a time!");
			else level.SeekTo(d.Value);
		});

		public static ConCommand clonedash_openmdlevel = ConCommand.Register(nameof(clonedash_openmdlevel), (_, args) => {
			var md_level = args.GetString(0);
			if (md_level == null) {
				Logs.Warn("Provide a name.");
				return;
			}

			var map = args.GetInt(1);
			if (map == null) {
				Logs.Warn("Provide a difficulty.");
				return;
			}

			MuseDashSong? song = MuseDashCompatibility.Songs.FirstOrDefault(x => x.BaseName == md_level);
			if (song == null) {
				Logs.Warn("Can't find that song.");
				Logs.Print("Here are some similar names:");
				foreach (var s in MuseDashCompatibility.Songs.Where(x => x.BaseName.ToLower().Contains(md_level.ToLower())))
					Logs.Print($"    {s.Name} ({s.BaseName})");
				return;
			}

			CD_GameLevel.LoadLevel(song, map.Value, (args.GetInt(2) ?? 0) == 1);
		});

		public static ConCommand clonedash_restest = ConCommand.Register("clonedash_restest", (_, args) => {
			Vector2 winSize;
			switch (args.GetString(0)) {
				case "16:9": winSize = new(1600, 900); break;
				case "19.5:9": winSize = new(1950, 900); break;
				case "4:3": winSize = new(1600, 1200); break;
				default:
					Logs.Warn($"Expected 16:9, 19.5:9, or 4:3.");
					return;

			}

			Vector2 monPos = Raylib.GetMonitorPosition(0);
			Vector2 monSize = new Vector2(Raylib.GetMonitorWidth(0), Raylib.GetMonitorHeight(0));
			Vector2 winPos = (monPos + (monSize / 2)) - (winSize / 2);
			Raylib.SetWindowPosition((int)winPos.X, (int)winPos.Y);
			Raylib.SetWindowSize((int)winSize.X, (int)winSize.Y);
		});

		public static ConVar clonedash_profilegameload = ConVar.Register("clonedash_profilegameload", "0", ConsoleFlags.None, "Profiles the game during loading, then triggers an engine interrupt afterwards to tell you how long each individual component took.");

		public static CD_GameLevel LoadLevel(ChartSong song, int mapID, bool autoplay) {
			Interlude.Begin($"Loading '{song.Name}'...");
			if (clonedash_profilegameload.GetBool()) {
				Logs.Info("Starting the sequential profiler.");
				CD_StaticSequentialProfiler.Start();
			}

			var sheet = song.GetSheet(mapID);
			var workingLevel = new CD_GameLevel(sheet);
			if (workingLevel == null) return workingLevel;
			EngineCore.LoadLevel(workingLevel, autoplay);
			MainThread.RunASAP(Interlude.End, ThreadExecutionTime.AfterFrame);
			return workingLevel;
		}


		public void SeekTo(double time) {
			Music.Playhead = (float)time;
			foreach (var entity in Entities) {
				if (entity is not CD_BaseEnemy entCD)
					continue;

				entCD.Reset();
			}

			Combo = 0;
			Health = 250;
			InFever = false;
			lastNoteHit = false;
			Score = 0;
			Fever = 0;

			AutoPlayer.Reset();

			foreach (var entity in Entities) {
				if (entity is CD_BaseMEntity mEnt && mEnt.HitTime < time) {
					mEnt.RewardPlayer(true);
					mEnt.Kill();
				}
			}

		}

		public const string STRING_HP = "HP: {0}";
		public const string STRING_FEVERY = "FEVER! {0}s";
		public const string STRING_FEVERN = "FEVER: {0}/{1}";
		public const string STRING_COMBO = "COMBO";
		public const string STRING_SCORE = "SCORE";
		public const string FONT = "Noto Sans";

		public const float PLAYER_OFFSET_X = 0.25f;
		public const float PLAYER_OFFSET_Y = 0.775f;
		public const float PLAYER_OFFSET_HIT_Y = -0.267f;

		public bool InHit { get; private set; } = false;

		public bool SuppressHitMessages { get; set; }
		public void EnterHitState() {
			InHit = true;
			SuppressHitMessages = false;
		}
		public void ExitHitState() {
			InHit = false;
		}

		public float XPos { get; private set; }

		public bool InMashState { get; private set; }
		public CD_BaseMEntity? MashingEntity;
		private SecondOrderSystem MashZoomSOS = new(1.1f, 0.9f, 2f, 0);
		private TextEffect mashTextEffect;

		/// <summary>
		/// Enters the mash state, which causes all attacks to be redirected into this entity.
		/// </summary>
		/// <param name="ent"></param>
		public void EnterMashState(CD_BaseMEntity ent) {
			if (IValidatable.IsValid(mashTextEffect))
				mashTextEffect.Remove();

			mashTextEffect = SpawnTextEffect("HITS: 1", new(0), TextEffectTransitionOut.SlideUp, Game.Pathway.PATHWAY_DUAL_COLOR);
			mashTextEffect.SuppressAutoDeath = true;
			UpdateMashTextEffect();

			InMashState = true;
			MashingEntity = ent;
		}
		public void UpdateMashTextEffect() {
			if (!IValidatable.IsValid(mashTextEffect)) return;
			if (!IValidatable.IsValid(MashingEntity)) return;

			mashTextEffect.Position = GetPathway(PathwaySide.Top).Position;
			mashTextEffect.Text = $"HITS: {MashingEntity.Hits}";
		}
		/// <summary>
		/// Exits the mash state.
		/// </summary>
		public void ExitMashState() {
			if (IValidatable.IsValid(mashTextEffect))
				mashTextEffect.Remove();

			InMashState = false;
			MashingEntity = null;
		}

		/// <summary>
		/// Is an entity on-screen and/or event currently warning the player? Used to draw the "!" warning on the side (and, if the entity wants to, on the entity itself)
		/// </summary>
		public bool IsWarning { get; set; } = false;


		// Player input system
		public InputState InputState { get; private set; }
		public List<ICloneDashInputSystem> InputReceivers { get; } = [];

		public AutoPlayer AutoPlayer { get; private set; }
		/// <summary>
		/// Timing system.
		/// </summary>
		public Conductor Conductor { get; private set; }
		public MusicTrack Music { get; private set; }
		public ModelEntity Player { get; set; }
		public ModelEntity HologramPlayer { get; set; }
		public Boss Boss { get; set; }
		public Pathway TopPathway { get; set; }
		public Pathway BottomPathway { get; set; }

		/// <summary>
		/// Is the game currently paused
		/// </summary>
		public double UnpauseTime { get; private set; } = 0;
		public double DeltaUnpauseTime => Realtime - UnpauseTime;

		/// <summary>
		/// How many ticks have passed, meant for debugging
		/// </summary>
		public int Ticks { get; private set; } = 0;

		// WIP pausing
		// return false to not spawn the pause menu
		private bool startPause() {
			if (lastNoteHit)
				return false;
			if (Conductor.Time < 0)
				return false;

			Music.Paused = true;
			Paused = true;
			UnpauseTime = 0;

			return true;
		}
		private void startUnpause() {
			Scene.PlayUnpause();
			UnpauseTime = Realtime;
			Timers.Simple(3, () => {
				fullUnpause();
			});
		}
		private void fullUnpause() {
			Music.Paused = false;
			Paused = false;
			UnpauseTime = 0;
		}

		int attackP = 0;
		int failP = 0;

		public enum CharacterAnimation
		{
			Walk,
			AirFail,
			GroundFail,
			AirHit,
			GroundHit,
			Hold
		}

		private int entI = 0;
		private bool __deferringAsync = false;
		public bool LoadMapFrame(int ms) {
			__deferringAsync = true;
			Stopwatch watch = new Stopwatch();
			watch.Start();
			while (entI < Sheet.Entities.Count) {
				if (watch.ElapsedMilliseconds >= ms)
					return false;

				LoadEntity(Sheet.Entities[entI]);
				entI++;
			}

			return entI >= Sheet.Entities.Count;
		}

		private StatisticsData Stats { get; } = new();
		public CharacterDescriptor Character;
		public SceneDescriptor Scene;

		public enum CDDAnimationType
		{
			Run,
			Die,
			Standby,

			AirGreat,
			AirPerfect,
			AirHurt,

			RoadGreat,
			RoadPerfect,
			RoadHurt,
			RoadMiss,

			Double,

			AirToGround,

			Jump,
			JumpHurt,

			Press,
			AirPressEnd,
			AirPressHurt,
			DownPressHit,
			UpPressHit
		}

		int seq = 0;
		public string AnimationCDD(CDDAnimationType type) {
			var playData = Character.Play;
			if (type != CDDAnimationType.Run) seq += 1;
			switch (type) {
				case CDDAnimationType.Run: return playData.RunAnimation.GetAnimation(seq);
				case CDDAnimationType.Die: return playData.DieAnimation.GetAnimation(seq);

				case CDDAnimationType.AirGreat: return playData.AirAnimations.Great.GetAnimation(seq);
				case CDDAnimationType.AirPerfect: return playData.AirAnimations.Perfect.GetAnimation(seq);
				case CDDAnimationType.AirHurt: return playData.AirAnimations.Hurt.GetAnimation(seq);

				case CDDAnimationType.Double: return playData.DoubleAnimation.GetAnimation(seq);

				case CDDAnimationType.Jump: return playData.JumpAnimations.Jump.GetAnimation(seq);
				case CDDAnimationType.JumpHurt: return playData.JumpAnimations.Hurt.GetAnimation(seq);

				case CDDAnimationType.RoadGreat: return playData.RoadAnimations.Great.GetAnimation(seq);
				case CDDAnimationType.RoadPerfect: return playData.RoadAnimations.Perfect.GetAnimation(seq);
				case CDDAnimationType.RoadMiss: return playData.RoadAnimations.Miss.GetAnimation(seq);
				case CDDAnimationType.RoadHurt: return playData.RoadAnimations.Hurt.GetAnimation(seq);

				case CDDAnimationType.Press: return playData.PressAnimations.Press.GetAnimation(seq);
				case CDDAnimationType.AirPressEnd: return playData.PressAnimations.AirPressEnd.GetAnimation(seq);

				default: throw new Exception("Can't do anything here");
			}

			throw new Exception("Can't do anything here");
		}

		public void PlayerAnim_EnqueueRun(ModelEntity model) {
			model.Model.SetToSetupPose();
			model.Animations.AddAnimation(0, AnimationCDD(CDDAnimationType.Run), true);
		}

		public void PlayerAnim_ForceJump(ModelEntity model) {
			model.Animations.SetAnimation(0, AnimationCDD(CDDAnimationType.Jump), false);
			PlayerAnim_EnqueueRun(model);
		}
		public void PlayerAnim_ForceMiss(ModelEntity model) {
			model.Animations.SetAnimation(0, AnimationCDD(CDDAnimationType.RoadMiss), false);
			PlayerAnim_EnqueueRun(model);
		}
		public void PlayerAnim_ForceAttackAir(ModelEntity model, bool perfect) {
			if (perfect)
				model.Animations.SetAnimation(0, AnimationCDD(CDDAnimationType.AirPerfect), false);
			else
				model.Animations.SetAnimation(0, AnimationCDD(CDDAnimationType.AirGreat), false);
		}
		public void PlayerAnim_ForceAttackGround(ModelEntity model, bool perfect) {
			if (perfect)
				model.Animations.SetAnimation(0, AnimationCDD(CDDAnimationType.RoadPerfect), false);
			else
				model.Animations.SetAnimation(0, AnimationCDD(CDDAnimationType.RoadGreat), false);
		}

		public void PlayerAnim_ForceAttackDouble(ModelEntity model) {
			model.Animations.SetAnimation(0, AnimationCDD(CDDAnimationType.Double), false);
		}

		public void PlayerAnim_EnterSustain() {
			Player.Animations.SetAnimation(0, AnimationCDD(CDDAnimationType.Press), true);
		}
		public void PlayerAnim_ExitSustain() {
			Player.Animations.ClearAnimation(0);
			if (InAir) {
				Player.Animations.SetAnimation(0, AnimationCDD(CDDAnimationType.AirPressEnd), false);
			}
			// We'll also kill the hologram player here
			HologramPlayer.Animations.StopAnimation(0);
			PlayerAnim_EnqueueRun(Player);
		}

		ShaderInstance hologramShader;

		public override void Initialize(params object[] args) {
			using (CD_StaticSequentialProfiler.StartStackFrame("CD_GameLevel.Initialize")) {
				using (CD_StaticSequentialProfiler.StartStackFrame("Get Descriptors")) {
					var charData = CharacterMod.GetCharacterData();
					var sceneData = SceneMod.GetSceneData();
					if (charData == null) throw new ArgumentNullException(nameof(charData));
					if (sceneData == null) throw new ArgumentNullException(nameof(sceneData));

					Character = charData;
					Scene = sceneData;
				}
				using (CD_StaticSequentialProfiler.StartStackFrame("Initialize Scene")) {
					Scene.Initialize(this);
				}
				Interlude.Spin();

				MaxHealth = (float)(Character.MaxHP ?? MaxHealth);

				Render3D = false;
				Health = MaxHealth;

				using (CD_StaticSequentialProfiler.StartStackFrame("Build Inputs")) {
					// build the input system
					var inputInterface = typeof(ICloneDashInputSystem);
					var inputs = AppDomain.CurrentDomain.GetAssemblies()
						.SelectMany(x => x.GetTypes())
						.Where(x => inputInterface.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
						.Select(x => Activator.CreateInstance(x)).ToList();

					foreach (object input in inputs)
						InputReceivers.Add((ICloneDashInputSystem)input);
				}
				using (CD_StaticSequentialProfiler.StartStackFrame("Initialize Character")) {
					hologramShader = Shaders.LoadFragmentShaderFromFile("shaders", "hologram.fs");
					Interlude.Spin();
					Player = Add(ModelEntity.Create("character", Character.GetPlayModel()));
					Interlude.Spin();

					HologramPlayer = Add(ModelEntity.Create("character", Character.GetPlayModel()));
					Player.Scale = new(1.25f);

					HologramPlayer.Scale = Player.Scale;
					HologramPlayer.Shader = hologramShader;

					Player.Model.SetToSetupPose();
					Player.Animations.AddAnimation(0, AnimationCDD(CDDAnimationType.Run), true);
				}
				using (CD_StaticSequentialProfiler.StartStackFrame("Initialize Boss")) {
					Boss = Add(new Boss(Scene));
					Boss.Model.SetToSetupPose();
				}

				Interlude.Spin();

				using (CD_StaticSequentialProfiler.StartStackFrame("Setup Internal Ents")) {
					HologramPlayer.Visible = false;
					AutoPlayer = Add<AutoPlayer>();
					AutoPlayer.Enabled = (bool)args[0];
					TopPathway = Add<Pathway>(PathwaySide.Top);
					BottomPathway = Add<Pathway>(PathwaySide.Bottom);
					Interlude.Spin();

					Conductor = Add<Conductor>();
					Interlude.Spin();
				}
				using (CD_StaticSequentialProfiler.StartStackFrame("Load Enemies")) {
					if (!__deferringAsync) {
						foreach (var ent in Sheet.Entities)
							LoadEntity(ent);

						foreach (var ev in Sheet.Events)
							LoadEvent(ev);
					}
				}
				Interlude.Spin();

				//foreach (var tempoChange in Sheet)
				Conductor.TempoChanges.Add(new TempoChange(0, (double)Sheet.Song.BPM));
				using (CD_StaticSequentialProfiler.StartStackFrame("Sheet.Song.GetAudioTrack()")) {
					Music = Sheet.Song.GetAudioTrack();
				}
				Music.Volume = 0.25f;
				Interlude.Spin();

				Music.Loops = false;
				Music.Playing = true;

				UIBar = this.UI.Add<CD_Player_UIBar>();
				UIBar.Level = this;
				UIBar.Size = new(0, 64);

				Scorebar = this.UI.Add<CD_Player_Scorebar>();
				Scorebar.Level = this;
				Scorebar.Size = new(0, 128);

				Scene.PlayBegin();
			}

			if (CD_StaticSequentialProfiler.Profiling) {
				var results = CD_StaticSequentialProfiler.End();
				EngineCore.Interrupt(() => {
					Graphics2D.SetDrawColor(255, 255, 255);
					var lines = results.ToStringArray();
					for (int i = 0; i < lines.Length; i++) {
						Graphics2D.DrawText(8, 8 + (i * 16), lines[i], "Consolas", 15);
					}
				}, false);
			}
		}
		public bool Debug { get; set; } = true;
		public Panel PauseWindow { get; private set; }
		private bool lastNoteHit = false;

		public float GetPlayerY(double jumpRatio) {
			var height = EngineCore.GetWindowHeight();

			return (float)(NMath.Remap(jumpRatio, 0, 1, Game.Pathway.GetPathwayBottom(), Game.Pathway.GetPathwayTop())) - (height * -.28f);
		}

		public override void PreThink(ref FrameState frameState) {
			Ticks++;
			XPos = frameState.WindowHeight * Game.Pathway.PATHWAY_LEFT_PERCENTAGE;

			if (lastNoteHit && Music.Paused) {
				EngineCore.LoadLevel(new CD_Statistics(), Sheet, Stats);
				return;
			}

			if (ShouldExitFever && InFever)
				ExitFever();

			if (InputState.TopClicked > 0 && CanJump)
				__whenjump = Conductor.Time;

			InputState inputState = new InputState();
			foreach (ICloneDashInputSystem playerInput in InputReceivers)
				playerInput.Poll(ref frameState, ref inputState);

			if (AutoPlayer.Enabled) {
				AutoPlayer.Play(ref inputState);
			}

			InputState = inputState;

			if (InMashState)
				UpdateMashTextEffect();

			if (inputState.PauseButton) {
				if (Music.Paused) {
					startUnpause();
					if (IValidatable.IsValid(PauseWindow))
						PauseWindow.Remove();
				}
				else {
					if (startPause()) {

						PauseWindow = this.UI.Add<Panel>();
						PauseWindow.Size = new(300, 400);
						PauseWindow.Center();

						var flex = PauseWindow.Add<FlexPanel>();
						flex.Dock = Dock.Fill;
						flex.Direction = Directional180.Vertical;
						flex.ChildrenResizingMode = FlexChildrenResizingMode.StretchToFit;
						flex.DockPadding = RectangleF.TLRB(4);

						var play = flex.Add<Button>();
						play.BorderSize = 0;
						play.Text = "Return to Game";
						play.TextSize = 24;
						play.Image = Textures.LoadTextureFromFile("ui/pause_play.png");
						play.ImageOrientation = ImageOrientation.Fit;
						play.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton clickedButton) {
							PauseWindow.Remove();
							startUnpause();
						};
						play.PaintOverride += Button_PaintOverride;

						var restart = flex.Add<Button>();
						restart.BorderSize = 0;
						restart.Text = "Restart Level";
						restart.TextSize = 24;
						restart.Image = Textures.LoadTextureFromFile("ui/pause_restart.png");
						restart.ImageOrientation = ImageOrientation.Fit;
						restart.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton clickedButton) {
							Interlude.Begin($"Reloading '{Sheet.Song.Name}'...");

							if (clonedash_profilegameload.GetBool())
								CD_StaticSequentialProfiler.Start();

							EngineCore.LoadLevel(new CD_GameLevel(Sheet), AutoPlayer.Enabled);
							Interlude.End();
						};
						restart.PaintOverride += Button_PaintOverride;

						var settings = flex.Add<Button>();
						settings.BorderSize = 0;
						settings.Text = "Open Preferences...";
						settings.TextSize = 24;
						settings.Image = Textures.LoadTextureFromFile("ui/pause_settings.png");
						settings.ImageOrientation = ImageOrientation.Fit;
						settings.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton clickedButton) {

						};
						settings.PaintOverride += Button_PaintOverride;

						var back2menu = flex.Add<Button>();
						back2menu.BorderSize = 0;
						back2menu.Text = "Exit to Menu";
						back2menu.TextSize = 24;
						back2menu.Image = Textures.LoadTextureFromFile("ui/pause_exit.png");
						back2menu.ImageOrientation = ImageOrientation.Fit;
						back2menu.MouseReleaseEvent += delegate (Element self, FrameState state, MouseButton clickedButton) {
							EngineCore.LoadLevel(new CD_MainMenu());
						};
						back2menu.PaintOverride += Button_PaintOverride;
					}
				}
				return;
			}

			float? yoff = null;

			bool holdingTop = HoldingTopPathwaySustain != null, holdingBottom = HoldingBottomPathwaySustain != null;
			bool holding = holdingTop || holdingBottom;
			if (holdingTop && holdingBottom)
				yoff = Game.Pathway.GetPathwayY(PathwaySide.Both);
			else if (holdingTop)
				yoff = Game.Pathway.GetPathwayY(PathwaySide.Top);
			else if (holdingBottom)
				yoff = Game.Pathway.GetPathwayY(PathwaySide.Bottom);

			if (yoff.HasValue) {
				yoff = yoff.Value - (frameState.WindowHeight * -0.15f);
			}

			Player.Position = new Vector2F(
				((frameState.WindowHeight) * Game.Pathway.PATHWAY_LEFT_PERCENTAGE) - 245,
				yoff ?? GetPlayerY(CharacterYRatio)
			);

			HologramPlayer.Position = new Vector2F(
				((frameState.WindowHeight) * Game.Pathway.PATHWAY_LEFT_PERCENTAGE) - 245,
				yoff ?? GetPlayerY(HologramCharacterYRatio)
			);

			if (HologramPlayer.PlayingAnimation || HologramPlayer.AnimationQueued) {
				HologramPlayer.Visible = true;
				HologramPlayer.SetShaderUniform("time", (float)(Conductor.Time - lastHologramHitTime) * 2);
			}
			else {
				HologramPlayer.Visible = false;
			}

			VisibleEntities.Clear();

			foreach (var entity in Entities) {
				if (entity is Boss) continue;
				if (entity is not CD_BaseEnemy)
					continue;

				var entCD = entity as CD_BaseMEntity;
				// Visibility testing
				// ShouldDraw overrides ForceDraw here, which is intentional, although the naming convention is confusing and should be adjusted (maybe the names swapped?)
				if ((entCD.CheckVisTest(frameState) || entCD.ForceDraw) && entCD.ShouldDraw) {
					VisibleEntities.Add(entCD);

					if (entCD.Warns && !entCD.Dead && !InMashState)
						IsWarning = true;
				}
			}

			var lastEntity = (CD_BaseMEntity)Entities.Last(x => x is CD_BaseEnemy);

			if (lastEntity.HitTime + lastEntity.Length < Conductor.Time && !lastNoteHit) {
				lastNoteHit = true;
				if (Stats.Misses == 0) {
					Logs.Info("Full combo achieved.");
					Scene.PlayFullCombo();
				}
			}

			// Sort the visible entities by their hit time
			VisibleEntities.Sort((x, y) => x.HitTime.CompareTo(y.HitTime));

			IterateEvents();

			//LockEntityBuffer();

			// Removes entities marked for removal safely
			foreach (var entity in Entities)
				if (entity is CD_BaseMEntity && ((CD_BaseMEntity)entity).MarkedForRemoval)
					Remove(entity);

			//UnlockEntityBuffer(); LockEntityBuffer();

			//foreach (var e in Events)
			//e.TryCall();

			// Resets the player animation state controller.
			// Does not reset any actively playing animations, just the internal state machines
			// used to determine when animations are triggered and on what.
			resetPlayerAnimState();

			// Start input processing.
			// Bottom is executed first, so if two pathway attacks happen on the same frame, it can exit the jump state
			// before jumping again, allowing the attack to work as expected
			HitLogic(PathwaySide.Bottom);
			HitLogic(PathwaySide.Top);

			// This loop is mostly for per-tick polls that need to occur, ie. when entities have been fully missed.
			// It is ran after input processing.
			foreach (var entity in VisibleEntities) {
				switch (entity.Interactivity) {
					case EntityInteractivity.Hit:
						if (!entity.Dead) {
							PathwaySide currentPathway = Pathway;

							// Is it too late for the player to hit this entity anyway?
							if (entity.DistanceToHit < -entity.PreGreatRange && !(entity is SustainBeam && ((SustainBeam)entity).HeldState == true)) {
								entity.Miss();
								Stats.Misses++;
							}
						}
						break;
					case EntityInteractivity.SamePath:
						if (NMath.InRange(entity.DistanceToHit, -entity.PreGreatRange, 0)) {
							PathwaySide pathCurrentCharacter = Pathway;
							if (pathCurrentCharacter == entity.Pathway && entity.Hits == 0) {
								entity.Hit(pathCurrentCharacter);
								Stats.Hits++;
							}
						}
						break;
					case EntityInteractivity.Avoid:
						// Checks if the player has completely failed to avoid the entity, and if so, damages the player.
						if (Pathway == entity.Pathway && entity.DistanceToHit < -entity.PrePerfectRange && !entity.DidRewardPlayer) {
							//entity.Hit(Game.PlayerController.Pathway);
							entity.DamagePlayer();
							Stats.Misses++;
						}

						// If the player is now avoiding the entity, then reward the player for missing it, and make it so they cant be damaged by it)
						if (Pathway != entity.Pathway && entity.DistanceToHit < 0 && !entity.DidDamagePlayer) {
							entity.Pass();
							Stats.Avoids++;
						}

						break;
				}
				//entity.WhenVisible();
			}

			FrameDebuggingStrings.Add($"HoldingTopPathwaySustain {(HoldingTopPathwaySustain == null ? "<null>" : HoldingTopPathwaySustain)}");
			FrameDebuggingStrings.Add($"HoldingBottomPathwaySustain {(HoldingBottomPathwaySustain == null ? "<null>" : HoldingBottomPathwaySustain)}");
		}

		private void Button_PaintOverride(Element self, float width, float height) {
			Button b = self as Button;
			var backpre = self.BackgroundColor;

			var back = Element.MixColorBasedOnMouseState(self, backpre, new(0, 0.8f, 2.4f, 1f), new(0, 1.2f, 0.6f, 1f));
			var fore = Element.MixColorBasedOnMouseState(self, self.ForegroundColor, new(0, 0.8f, 1.8f, 1f), new(0, 1.2f, 0.6f, 1f));

			Graphics2D.SetDrawColor(back);
			Graphics2D.DrawRectangle(0, 0, width, height);
			var text = b.Text;
			var tSize = Graphics2D.GetTextSize(text, b.Font, b.TextSize);
			b.ImageDrawing(new((height / -4) + (tSize.X / -2), 0), new(height, height));
			Graphics2D.SetDrawColor(255, 255, 255);
			Graphics2D.DrawText(new((width / 2) + (height / 4), height / 2), text, b.Font, b.TextSize, Anchor.Center);
		}

		public override void Think(FrameState frameState) {

		}
		public override void PostThink(FrameState frameState) {
			// Perform player animation. Things in HitLogic will trigger certain flags, which will result in 
			// specific animations being played
			determinePlayerAnimationState();
		}
		/// <summary>
		/// Gets the games <see cref="Pathway"/> from a <see cref="PathwaySide"/><br></br>
		/// Note: If strict is off (default), it will return the bottom pathway if PathwaySide.Middle is passed, otherwise it will throw an exception.
		/// </summary>
		/// <param name="pathway"></param>
		/// <param name="strict"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public Pathway GetPathway(PathwaySide pathway, bool strict = false) {
			switch (pathway) {
				case PathwaySide.Top:
					return TopPathway;
				case PathwaySide.Bottom:
					return BottomPathway;
				case PathwaySide.Both:
					if (strict)
						break;
					else
						return BottomPathway;
				case PathwaySide.None:
					break;
			}

			throw new ArgumentException("pathway");
		}

		public Pathway GetPathway(CD_BaseMEntity ent) => GetPathway(ent.Pathway);

		/// <summary>
		/// Creates an entity from a C# type and adds it to <see cref="GameplayManager.Entities"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
		public T CreateEntity<T>() where T : CD_BaseMEntity => (T)Add((T)Activator.CreateInstance(typeof(T)));
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

		/// <summary>
		/// Creates an event from an EventType enumeration, and adds it to <see cref="GameplayManager.Events"/>.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		/*public CD_BaseEvent AddEvent(EventType t) {
            CD_BaseEvent e = CD_BaseEvent.CreateFromType(this.Game, t);
            return e;
        }*/

		public PollResult LastPollResult { get; private set; } = PollResult.Empty;
		/// <summary>
		/// Polling function which figures out the closest, potentially-hit entity and returns the result.
		/// </summary>
		/// <param name="pathway"></param>
		/// <returns>A <see cref="PollResult"/>, if it hit something, Hit is true, and vice versa.</returns>
		public PollResult Poll(PathwaySide pathway) {
			foreach (CD_BaseMEntity entity in VisibleEntities) {
				// If the entity has no interactivity, ignore it in the poll
				if (!entity.Interactive)
					continue;

				// If the entity says its dead, ignore it
				if (entity.Dead)
					continue;

				switch (entity.Interactivity) {
					case EntityInteractivity.Hit:
					case EntityInteractivity.Sustain:
						if (Game.Pathway.ComparePathwayType(entity.Pathway, pathway)) {
							double distance = entity.DistanceToHit;
							double pregreat = -entity.PreGreatRange, postgreat = entity.PostGreatRange;
							double preperfect = -entity.PrePerfectRange, postperfect = entity.PostPerfectRange;
							if (NMath.InRange(distance, pregreat, postgreat)) { // hit occured
								var greatness = (NMath.InRange(distance, preperfect, postperfect) ? "PERFECT" : "GREAT") + " " + Math.Round(distance * 1000, 1) + "ms";
								LastPollResult = PollResult.Create(entity, distance, greatness);
								return LastPollResult;

							}
						}
						break;
				}
			}

			LastPollResult = PollResult.Empty;
			return LastPollResult;
		}

		/// <summary>
		/// Spawns a <see cref="TextEffect"/> into the game and adds it to the game.
		/// </summary>
		/// <param name="text">The text</param>
		/// <param name="position">Where it spawns (it will rise upwards after being spawned)</param>
		/// <param name="color">The color of the text</param>
		public TextEffect SpawnTextEffect(string text, Vector2F position, TextEffectTransitionOut transitionOut = TextEffectTransitionOut.SlideUp, Color? color = null) {
			if (color == null)
				color = new Color(255, 255, 255, 255);

			return Add(new TextEffect(text, position, transitionOut, color.Value));
		}

		public List<CD_BaseEvent> Events = [];
		public HashSet<CD_BaseEvent> ActiveEvents = [];
		public HashSet<CD_BaseEvent> HandledEvents = [];

		private bool shouldActivateEvent(CD_BaseEvent ev) => ev.TriggerType switch {
			EventTriggerType.AtTimeMinusLength => Conductor.Time >= (ev.Time - ev.Length),
			EventTriggerType.AtTime => Conductor.Time >= ev.Time,
			_ => false
		};
		private bool shouldDeactivateEvent(CD_BaseEvent ev) => ev.TriggerType switch {
			EventTriggerType.AtTimeMinusLength => Conductor.Time >= ev.Time,
			EventTriggerType.AtTime => Conductor.Time >= (ev.Time + ev.Length),
			_ => false
		};

		public void IterateEvents() {
			foreach (var ev in Events) {
				if (ActiveEvents.Contains(ev)) {
					// Determine if the event needs to be deactivated
					if (shouldDeactivateEvent(ev)) {
						HandledEvents.Add(ev);
						ActiveEvents.Remove(ev);
						ev.Deactivate();
						Logs.Debug($"Deactivating {ev.GetType().Name}");
					}
				}
				else if (!HandledEvents.Contains(ev)) {
					// Determine if the event needs to be activated
					if (shouldActivateEvent(ev)) {
						ActiveEvents.Add(ev);
						ev.Activate();
						Logs.Debug($"Activating {ev.GetType().Name}");
					}
				}
				// The event has both been activated and deactivated, so its ignored
			}
		}

		/// <summary>
		/// Loads an event from a <see cref="ChartEvent"/> representation, builds a <see cref="MapEvent"/> out of it, and adds it to  <see cref="GameplayManager.Events"/>.
		/// </summary>
		/// <param name="ChartEvent"></param>
		public void LoadEvent(ChartEvent ChartEvent) {
			Interlude.Spin();
			var ev = CD_BaseEvent.CreateFromType(this, ChartEvent.Type);

			ev.Time = ChartEvent.Time;
			ev.Length = ChartEvent.Length;

			ev.Score = ChartEvent.Score;
			ev.Fever = ChartEvent.Fever;
			ev.Damage = ChartEvent.Damage;

			ev.Build();

			Events.Add(ev);
		}

		/// <summary>
		/// Loads an entity from a <see cref="ChartEntity"/> representation, builds a <see cref="MapEntity"/> out of it, and adds it to <see cref="GameplayManager.Entities"/>.
		/// </summary>
		/// <param name="ChartEntity"></param>
		public void LoadEntity(ChartEntity ChartEntity) {
			Interlude.Spin();

			if (!CD_BaseEnemy.TryCreateFromType(this, ChartEntity.Type, out CD_BaseEnemy? ent)) {
				Console.WriteLine("No load entity handler for type " + ChartEntity.Type);
				return;
			}

			ent.Pathway = ChartEntity.Pathway;
			ent.EnterDirection = ChartEntity.EnterDirection;
			ent.Variant = ChartEntity.Variant;

			ent.HitTime = ChartEntity.HitTime;
			ent.ShowTime = ChartEntity.ShowTime;
			ent.Length = ChartEntity.Length;
			ent.Speed = ChartEntity.Speed;

			ent.FeverGiven = ChartEntity.Fever;
			ent.DamageTaken = ChartEntity.Damage;
			ent.ScoreGiven = ChartEntity.Score;

			ent.RelatedToBoss = ChartEntity.RelatedToBoss;

			ent.RendersItself = false;
			ent.DebuggingInfo = ChartEntity.DebuggingInfo;
			ent.Build();
		}
		public float GlobalScale => 1f;
		public override void PreRenderBackground(FrameState frameState) {
			Boss.Scale = new(GlobalScale);
			Boss.Position = new(0, 450);
		}
		public override void CalcView2D(FrameState frameState, ref Camera2D cam) {
			var zoomValue = MashZoomSOS.Update(InMashState ? 1 : 0) * .5f;
			cam.Zoom = (frameState.WindowHeight / 900 / 2) + (zoomValue / 5f);
			cam.Rotation = 0.0f;
			cam.Offset = new(frameState.WindowWidth / 2, frameState.WindowHeight / 2);
			cam.Target = new((frameState.WindowWidth / 1) * zoomValue, 0);
			cam.Offset += cam.Target;

			//cam.Offset = new(frameState.WindowWidth * Game.Pathway.PATHWAY_LEFT_PERCENTAGE * .5f, frameState.WindowHeight * 0.5f);
			//cam.Target = cam.Offset;
		}
		public override void Render(FrameState frameState) {
			Rlgl.DisableDepthTest();
			Rlgl.DisableBackfaceCulling();

			Raylib.DrawLineV(new(-100000, 0), new(100000, 0), Color.Red);
			Raylib.DrawLineV(new(0, -100000), new(0, 100000), Color.Green);
			Rlgl.DrawRenderBatchActive();

			TopPathway.Render();
			BottomPathway.Render();

			foreach (Entity ent in VisibleEntities) {
				if (ent is not CD_BaseEnemy entCD) continue;

				float yPosition = Game.Pathway.GetPathwayY(entCD.Pathway);

				Graphics2D.SetDrawColor(255, 255, 255);
				var p = new Vector2F((float)entCD.XPos, yPosition);
				; // Calculate the final beat position on the track
				entCD.ChangePosition(ref p); // Allow the entity to modify the position before it goes to the renderer
				ent.Position = p;
				ent.Render(frameState);

				Graphics2D.DrawCircle(ent.Position, 16);
			}

			FrameDebuggingStrings.Add("Visible Entities: " + VisibleEntities.Count);
			FrameDebuggingStrings.Add($"Player Animation: {Player.Animations.Channels[0].CurrentEntry?.Animation?.Name ?? "<null>"}");
			FrameDebuggingStrings.Add($"Hologram-Player Animation: {HologramPlayer.Animations.Channels[0].CurrentEntry?.Animation?.Name ?? "<null>"}");
			FrameDebuggingStrings.Add($"Player Y: {CharacterYRatio}");
			FrameDebuggingStrings.Add($"Hologram-Player Y: {HologramCharacterYRatio}");
		}

		public override void Render2D(FrameState frameState) {
			base.Render2D(frameState);

			foreach (Entity ent in VisibleEntities) {
				if (ent is not CD_BaseEnemy)
					continue;

				var entCD = (CD_BaseEnemy)ent;
				//Graphics2D.DrawText(ent.Position, entCD.DebuggingInfo, "Consolas", 20);
			}
		}
	}
}