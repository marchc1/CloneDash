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

namespace CloneDash.Game
{
	[Nucleus.MarkForStaticConstruction]
	public partial class CD_GameLevel(ChartSheet Sheet) : Level
	{
		public static ConCommand clonedash_settime = ConCommand.Register("clonedash_seek", (_, args) => {
			var level = EngineCore.Level.AsNullable<CD_GameLevel>();
			if (level == null) {
				Logs.Warn("Not in game context!");
				return;
			}

			double? d = args.GetDouble(0);
			if (!d.HasValue) Logs.Warn("Did not specify a time!");
			else level.SeekTo(d.Value);
		});

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

		public const float PLAYER_OFFSET_X = 0.225f;
		public const float PLAYER_OFFSET_Y = 0.64f;
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

		/// <summary>
		/// Enters the mash state, which causes all attacks to be redirected into this entity.
		/// </summary>
		/// <param name="ent"></param>
		public void EnterMashState(CD_BaseMEntity ent) {
			InMashState = true;
			MashingEntity = ent;
		}

		/// <summary>
		/// Exits the mash state.
		/// </summary>
		public void ExitMashState() {
			InMashState = false;
			MashingEntity = null;
		}

		/// <summary>
		/// Is an entity on-screen and/or event currently warning the player? Used to draw the "!" warning on the side (and, if the entity wants to, on the entity itself)
		/// </summary>
		public bool IsWarning { get; set; } = false;


		// Player input system
		public InputState InputState { get; private set; }
		public List<IPlayerInput> InputReceivers { get; } = [];

		public AutoPlayer AutoPlayer { get; private set; }
		/// <summary>
		/// Timing system.
		/// </summary>
		public Conductor Conductor { get; private set; }
		public MusicTrack Music { get; private set; }
		public ModelEntity Player { get; set; }
		public ModelEntity HologramPlayer { get; set; }
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
			Model3AnimationChannel.GlobalPause = true;
			UnpauseTime = 0;

			return true;
		}
		private void startUnpause() {
			Sounds.PlaySound("321.wav", true, 0.8f, 1f);
			UnpauseTime = Realtime;
			Timers.Simple(3, () => {
				fullUnpause();
			});
		}
		private void fullUnpause() {
			Music.Paused = false;
			Paused = false;
			Model3AnimationChannel.GlobalPause = false;
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

		public string GetCharacterAnimation(CharacterAnimation animation) {
			switch (animation) {
				case CharacterAnimation.Walk:
					return CharacterDescriptor.Animation_WalkCycle;

				case CharacterAnimation.AirFail:
					return CharacterDescriptor.Animation_AirAttacks_Failed[failP++ % CharacterDescriptor.Animation_AirAttacks_Failed.Length];
				case CharacterAnimation.GroundFail:
					return CharacterDescriptor.Animation_GroundAttacks_Failed[failP++ % CharacterDescriptor.Animation_GroundAttacks_Failed.Length];

				case CharacterAnimation.AirHit:
					var airs = CharacterDescriptor.GetAirAttacks();
					return airs[attackP++ % airs.Length];
				case CharacterAnimation.GroundHit:
					var grounds = CharacterDescriptor.GetAirAttacks();
					return grounds[attackP++ % grounds.Length];

				case CharacterAnimation.Hold:
					return CharacterDescriptor.Animation_Holding;

				default:
					throw new Exception("Unknown CharacterAnimation value.");
			}
		}

		private StatisticsData Stats { get; } = new();
		private CharacterDescriptor CharacterDescriptor;
		public override void Initialize(params object[] args) {
			Health = MaxHealth;
			Draw3DCoordinateStart = Draw3DCoordinateStart.TopLeft0_0;

			// build the input system
			var inputInterface = typeof(IPlayerInput);
			var inputs = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(x => x.GetTypes())
				.Where(x => inputInterface.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
				.Select(x => Activator.CreateInstance(x)).ToList();

			foreach (object input in inputs)
				InputReceivers.Add((IPlayerInput)input);

			var info = CharacterMod.GetCharacterData();
			CharacterDescriptor = info.Descriptor;
			var filename = Filesystem.Resolve($"{info.Name}.glb", "chars");

			Player = Add(ModelEntity.Create(filename, resolvePath: false));
			HologramPlayer = Add(ModelEntity.Create(filename, resolvePath: false));
			Player.PlayAnimation(GetCharacterAnimation(CharacterAnimation.Walk), loop: true);

			HologramPlayer.Visible = false;
			AutoPlayer = Add<AutoPlayer>();
			AutoPlayer.Enabled = (bool)args[0];
			TopPathway = Add<Pathway>(PathwaySide.Top);
			BottomPathway = Add<Pathway>(PathwaySide.Bottom);

			foreach (ChartEntity ChartEntity in Sheet.Entities)
				LoadEntity(ChartEntity);

			Conductor = Add<Conductor>();

			//foreach (var tempoChange in Sheet)
			Conductor.TempoChanges.Add(new TempoChange(0, (double)Sheet.Song.BPM));

			Music = Sheet.Song.GetAudioTrack();
			Music.Volume = 0.25f;

			Music.Loops = false;
			Music.Playing = true;

			UIBar = this.UI.Add<CD_Player_UIBar>();
			UIBar.Level = this;
			UIBar.Size = new(0, 64);

			Scorebar = this.UI.Add<CD_Player_Scorebar>();
			Scorebar.Level = this;
			Scorebar.Size = new(0, 128);

			Sounds.PlaySound("readygo.wav", true, 0.8f, 1.0f);
		}
		public bool Debug { get; set; } = true;
		public Panel PauseWindow { get; private set; }
		private bool lastNoteHit = false;
		public override void PreThink(ref FrameState frameState) {
			XPos = frameState.WindowWidth * PLAYER_OFFSET_X;

			if (lastNoteHit && Music.Paused) {
				EngineCore.LoadLevel(new CD_Statistics(), Sheet, Stats);
				return;
			}

			if (ShouldExitFever && InFever)
				ExitFever();

			if (InputState.TopClicked > 0 && CanJump)
				__whenjump = Conductor.Time;

			InputState inputState = new InputState();
			foreach (IPlayerInput playerInput in InputReceivers)
				playerInput.Poll(ref frameState, ref inputState);

			if (AutoPlayer.Enabled) {
				AutoPlayer.Play(ref inputState);
			}

			InputState = inputState;

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
							EngineCore.LoadLevel(new CD_GameLevel(Sheet), AutoPlayer.Enabled);
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
			if (holdingTop && holdingBottom) yoff = Game.Pathway.GetPathwayY(PathwaySide.Both);
			else if (holdingTop) yoff = Game.Pathway.GetPathwayY(PathwaySide.Top);
			else if (holdingBottom) yoff = Game.Pathway.GetPathwayY(PathwaySide.Bottom);


			Player.Position = new Vector2F(frameState.WindowHeight * PLAYER_OFFSET_X, yoff ?? ((frameState.WindowHeight * PLAYER_OFFSET_Y) + (frameState.WindowHeight * PLAYER_OFFSET_HIT_Y * CharacterYRatio)));
			Player.Scale = new(0.9f);
			HologramPlayer.Position = new Vector2F(frameState.WindowHeight * PLAYER_OFFSET_X, ((frameState.WindowHeight * PLAYER_OFFSET_Y) + (frameState.WindowHeight * PLAYER_OFFSET_HIT_Y * HologramCharacterYRatio)));

			if (HologramPlayer.PlayingAnimation) {
				var animator = HologramPlayer.Model;
				var animation = animator.Animations.FirstOrDefault(x => x.Value.Playing).Value ?? null;

				float a = (float)(animation.AnimationPlayhead / animation.AnimationData.AnimationLength);
				HologramPlayer.Visible = true;
				var alpha = Math.Clamp(NMath.Ease.InQuad(1 - a) * 255, 0, 255);
				HologramPlayer.Color = new(150, 206, 255, (int)alpha);
				FrameDebuggingStrings.Add("PlayerAlpha = " + alpha);
			}
			else {
				HologramPlayer.Visible = false;
			}

			VisibleEntities.Clear();

			foreach (var entity in Entities) {
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
					Sounds.PlaySound("fullcombo.wav", true, 0.8f, 1f);
				}
			}

			// Sort the visible entities by their hit time
			VisibleEntities.Sort((x, y) => x.HitTime.CompareTo(y.HitTime));

			//LockEntityBuffer();

			// Removes entities marked for removal safely
			foreach (var entity in Entities)
				if (entity is CD_BaseMEntity && ((CD_BaseMEntity)entity).MarkedForRemoval)
					Remove(entity);

			//UnlockEntityBuffer(); LockEntityBuffer();

			//foreach (var e in Events)
			//e.TryCall();

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
		public void SpawnTextEffect(string text, Vector2F position, Color? color = null) {
			if (color == null)
				color = new Color(255, 255, 255, 255);

			Add(new TextEffect(text, position, color.Value));
		}

		/// <summary>
		/// Loads an event from a <see cref="ChartEvent"/> representation, builds a <see cref="MapEvent"/> out of it, and adds it to  <see cref="GameplayManager.Events"/>.
		/// </summary>
		/// <param name="ChartEvent"></param>
		public void LoadEvent(ChartEvent ChartEvent) {
			/*var ev = MapEvent.CreateFromType(this.Game, ChartEvent.Type);

            ev.Time = ChartEvent.Time;
            ev.Length = ChartEvent.Length;

            ev.Score = ChartEvent.Score;
            ev.Fever = ChartEvent.Fever;
            ev.Damage = ChartEvent.Damage;

            ev.Build();

            Events.Add(ev);*/
		}

		/// <summary>
		/// Loads an entity from a <see cref="ChartEntity"/> representation, builds a <see cref="MapEntity"/> out of it, and adds it to <see cref="GameplayManager.Entities"/>.
		/// </summary>
		/// <param name="ChartEntity"></param>
		public void LoadEntity(ChartEntity ChartEntity) {
			if (!CD_BaseEnemy.TypeConvert.ContainsKey(ChartEntity.Type)) {
				Console.WriteLine("No load entity handler for type " + ChartEntity.Type);
				return;
			}
			var ent = CD_BaseEnemy.CreateFromType(this, ChartEntity.Type);

			ent.Pathway = ChartEntity.Pathway;
			ent.EnterDirection = ChartEntity.EnterDirection;

			ent.HitTime = ChartEntity.HitTime;
			ent.ShowTime = ChartEntity.ShowTime;
			ent.Length = ChartEntity.Length;

			ent.FeverGiven = ChartEntity.Fever;
			ent.DamageTaken = ChartEntity.Damage;
			ent.ScoreGiven = ChartEntity.Score;

			ent.RelatedToBoss = ChartEntity.RelatedToBoss;

			ent.RendersItself = false;
			ent.DebuggingInfo = ChartEntity.DebuggingInfo;
			ent.Build();
		}

		public override void PreRenderBackground(FrameState frameState) {
			Graphics2D.SetTexture(Textures.LoadTextureFromFile("backgroundscroll.png"));
			var offset = ((float)Curtime * -600f) % frameState.WindowWidth;
			Graphics2D.SetDrawColor(255, 255, 255, 127);
			var offset2 = frameState.WindowHeight * 0.1f;
			Graphics2D.DrawTexture(new(offset, offset2), new(frameState.WindowWidth, frameState.WindowHeight));
			Graphics2D.DrawTexture(new(offset + frameState.WindowWidth, offset2), new(frameState.WindowWidth, frameState.WindowHeight)); ;
		}
		public override void CalcView(FrameState frameState, ref Camera3D cam) {
			base.CalcView(frameState, ref cam);
		}
		public override void Render(FrameState frameState) {
			if (CommandLineArguments.IsParamTrue("debug")) {
				Rlgl.DrawRenderBatchActive();
				Rlgl.SetLineWidth(5);
				Raylib.DrawLine3D(new(0, 2.5f, 0), new(10000, 2.5f, 0), Color.Red);
				Raylib.DrawLine3D(new(2.5f, 0, 0), new(2.5f, 10000, 0), Color.Green);
				Rlgl.DrawRenderBatchActive();
				Rlgl.SetLineWidth(1);
			}
			
			foreach (Entity ent in VisibleEntities) {
				if (ent is not CD_BaseEnemy entCD)
					continue;

				float yPosition = Game.Pathway.GetPathwayY(entCD.Pathway);

				Graphics2D.SetDrawColor(255, 255, 255);
				var p = new Vector2F((float)entCD.XPos, yPosition); // Calculate the final beat position on the track
				entCD.ChangePosition(ref p); // Allow the entity to modify the position before it goes to the renderer
				ent.Position = p;
				ent.Render(frameState);
			}
			
			FrameDebuggingStrings.Add("Visible Entities: " + VisibleEntities.Count);
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
