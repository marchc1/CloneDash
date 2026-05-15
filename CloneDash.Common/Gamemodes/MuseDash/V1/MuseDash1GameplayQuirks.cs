using Nucleus.Util;

namespace CloneDash.Common.Gamemodes.MuseDash.V1;


public struct MuseDash1GameplayQuirks()
{
	public enum Judgement : byte
	{
		Miss = 1,
		Pass = 2,
		Great = 4,
		Perfect = 8
	}
	/// <summary>
	/// Various pieces of current game/polling state are passed to numerical modifiers in quirks, more can be exposed if needed
	/// </summary>
	public struct GameSnapshot
	{
		public int Difficulty;

		public int CurrentCombo;
		public double CurrentScore;
		public bool InFever;

		public MuseDash1EntityType EntityType;
		public double EntityBlood;
		public bool EntityWarning;
		public Judgement Judgement;

		public double Health;
		public double MaxHealth;

		public readonly bool IsHealthFull() => Health >= MaxHealth;
	}

	public delegate void ModifyDoubleFn(in GameSnapshot gameState, ref double number);
	public delegate Judgement JudgementUpgradeFn(in GameSnapshot gameState);

	static ModifyDoubleFn One() => static (in state, ref value) => value = 1;

	public const int DEFAULT_MAX_HP = 250;
	public const int DEFAULT_MAX_FEVER = 120;

	public int MaxHP = DEFAULT_MAX_HP;
	public int MaxFever = DEFAULT_MAX_FEVER;

	public bool Autoplay;

	/// <summary>
	/// Will damage be done in fever?
	/// </summary>
	public bool InvincibleInFever;

	/// <summary>
	/// If true, the character will not fall back to the ground
	/// </summary>
	public bool AbleToFly;

	/// <summary>
	/// If true, the character will automatically hold down sustains, allowing you to only have to tap them
	/// </summary>
	public bool AutoHoldsSustains;

	/// <summary>
	/// Enables Divine Gear Buro's in-game quiz mode (TODO: research)
	/// </summary>
	public bool Quiz;

	/// <summary>
	/// When the character would normally die, HP is instead set to 1, and the character will become invincible for this many seconds.
	/// This can only be activated again if the character gains HP (through heart notes) after the invincibility ends.
	/// </summary>
	public double DyingSeconds;

	/// <summary>
	/// How long an invincibility frame lasts
	/// </summary>
	public double IFrameLength = 1.25;

	public EntityTypeLookupArray<double> ScoreMultipliers = EntityTypeLookupArray.Create<double>(1);
	/// <summary>
	/// Not actually used right now, but provided for completions sake
	/// </summary>
	public double XpMultiplier = 1;

	/// <summary>
	/// Used to modify the score being granted in some fashion
	/// </summary>
	public ModifyDoubleFn? ScoreModifier;

	/// <summary>
	/// Used to modify the fever points being granted in some fashion
	/// </summary>
	public ModifyDoubleFn? FeverModifier;

	/// <summary>
	/// Used to modify the fever points being granted in some fashion
	/// </summary>
	public ModifyDoubleFn? DamageModifier;

	/// <summary>
	/// Returns how much fever gain should be granted,  per second 
	/// </summary>
	public ModifyDoubleFn? FeverGainPerSecond;

	/// <summary>
	/// Returns how much health should be drained, per second
	/// </summary>
	public ModifyDoubleFn? HealthLossPerSecond;

	/// <summary>
	/// If combo is less than this number, a miss won't interrupt the combo.
	/// </summary>
	public int MissComboForgiveness;

	/// <summary>
	/// Threshold for forgiveness. If accuracy is less than this number, <see cref="ForgivenAccuracy"/> is granted.
	/// </summary>
	public double MaximumApplicableResultAccuracy;
	public double ForgivenAccuracy;

	/// <summary>
	/// Can forgive up to this many greats (except for sustain beams and mashers)
	/// </summary>
	public int GreatToPerfect;

	/// <summary>
	/// How many times a health bonus can be granted from heart notes
	/// </summary>
	public int HealthBonusTimes = 0;
	/// <summary>
	/// How low does the current HP have to be to trigger this health bonus
	/// </summary>
	public double HealthBonusThreshold;
	/// <summary>
	/// How much health bonus should be granted from heart notes (given <see cref="HealthBonusTimes"/> and <see cref="HealthBonusThreshold"/>)
	/// </summary>
	public double HealthBonusGranted;

	public int C4Bombs; // This ONLY exists for Wisadel, I just don't like having 'public bool WisadelMode', idk

	/// <summary>
	/// If true, avoidance interactivity will break instead of doing damage
	/// (TODO: is this ALWAYS the case... Touhou mode?)
	/// </summary>
	public bool BreaksAvoids;

	/// <summary>
	/// Completes mashers instantly
	/// </summary>
	public bool CompleteMashersInstantly;

	/// <summary>
	/// If true, the game will still be playable after death. Score would not be uploaded (if we were actually uploading scores)
	/// </summary>
	public bool PlayableAfterDeath;

	/// <summary>
	/// Mostly for Rebirth Girl: "automatically catch all the missed Blue Notes, Red Hearts, ghosts, and enemies with Heart. The automatically caught ghosts and enemies with Heart is 'Perfect' judgment."
	/// </summary>
	public bool CatchExtras;

	public JudgementUpgradeFn? JudgementUpgrade;

	// todo: a better name for this
	/// <summary>
	/// How much time it takes, post-fever-completion, to be able to gain fever points again
	/// </summary>
	public double PostFeverDismissalTime;

	public bool ConsumeFeverBeforeHealth;

	/// <summary>
	/// Swap locations physically on the entities of this type.
	/// </summary>
	public EntityTypeLookupArray<bool> SwapLocations;
	/// <summary>
	/// Merge locations in input polling for hit entities
	/// </summary>
	public bool MergeHitLocations;

	static MuseDash1GameplayQuirks() {
		AddQuirks("character/musedash1/char_1_rock", new() {
			MaxHP = 300
		});
		AddQuirks("character/musedash1/char_1_rampage", new() {
			GreatToPerfect = 5
		});
		AddQuirks("character/musedash1/char_1_sleepy", new() {
			MaxHP = 200,
			Autoplay = true
		});
		AddQuirks("character/musedash1/char_1_bunny", new() {
			MaxHP = 200,
			ScoreMultipliers = EntityTypeLookupArray.Create<double>(1,
				(MuseDash1EntityType.Score, 3),
				(MuseDash1EntityType.Ghost, 3),
				(MuseDash1EntityType.Gear, 3)
			),
		});
		AddQuirks("character/musedash1/char_2_pilot", new() {
			InvincibleInFever = true
		});
		AddQuirks("character/musedash1/char_2_robot", new() {
			MaxHP = 200,
			XpMultiplier = 1.5
		});
		AddQuirks("character/musedash1/char_2_zombie", new() {
			DyingSeconds = 15
		});
		AddQuirks("character/musedash1/char_2_joker", new() {
			MaxHP = 200,
			ScoreModifier = static (in GameSnapshot gameState, ref double scoreGranting) => {
				// TODO:
				// Mixed answers on how this works (minimum 50 or 60? outdated info after nerf/buff?)
				// Also unsure how much to increase score by right now.
			}
		});
		AddQuirks("character/musedash1/char_3_violin", new() {
			MissComboForgiveness = 100
		});
		AddQuirks("character/musedash1/char_3_maid", new() {
			HealthBonusTimes = 1,
			HealthBonusThreshold = 100,
			HealthBonusGranted = 2.5
		});
		AddQuirks("character/musedash1/char_3_magic", new() {
			MaxHP = 200,
			MaxFever = 100
		});
		AddQuirks("character/musedash1/char_3_evil", new() {
			MaxHP = 200,
			ScoreModifier = static (in gameState, ref scoreGranting) => {

			},
			HealthLossPerSecond = static (in _, ref loss) => loss = 10
		});
		AddQuirks("character/musedash1/char_3_black", new() {
			// todo
		});
		AddQuirks("character/musedash1/char_1_santa", new() {
			MaximumApplicableResultAccuracy = 90,
			ForgivenAccuracy = 5
		});
		AddQuirks("character/musedash1/char_2_jk", new() {
			// todo
		});
		AddQuirks("character/musedash1/char_4_yume", new() {
			BreaksAvoids = true
		});
		AddQuirks("character/musedash1/char_5_neko", new() {
			PlayableAfterDeath = true
		});
		AddQuirks("character/musedash1/char_1_worker", new() {
			// I think this just has nothing lol but idk
		});
		AddQuirks("character/musedash1/char_6_reimu", new() {
			AbleToFly = true,
			// TODO: "and shrunk the hitbox of obstacles (gears) , making it easier to dodge."
		});
		AddQuirks("character/musedash1/char_7_clear", new() {
			CatchExtras = true
		});
		AddQuirks("character/musedash1/char_3_sister", new() {
			SwapLocations = EntityTypeLookupArray.CreateWhitelist(
				MuseDash1EntityType.Raider,
				MuseDash1EntityType.Hammer,
				MuseDash1EntityType.Gear
			)
		});
		AddQuirks("character/musedash1/char_8_marisa", new() {
			AbleToFly = true,
			ScoreModifier = static (in gameState, ref scoreGranting) => {
				if (gameState.IsHealthFull() && (gameState.EntityType == MuseDash1EntityType.Score || gameState.EntityType == MuseDash1EntityType.Heart || gameState.EntityBlood != 0))
					scoreGranting *= 3;
			},
		});
		AddQuirks("character/musedash1/char_9_amiya", new() {
			// TODO: Earns more fever by repelling enemies (need specifics)
			JudgementUpgrade = (in gameState) => {
				if (!gameState.InFever)
					return gameState.Judgement;

				return gameState.Judgement switch {
					Judgement.Miss => Judgement.Great,
					Judgement.Great => Judgement.Perfect,
					_ => gameState.Judgement
				};
			},
			PostFeverDismissalTime = 3,
		});
		AddQuirks("character/musedash1/char_10_ola", new() {
			InvincibleInFever = true
		});
		AddQuirks("character/musedash1/char_2_exorcist", new() {
			ConsumeFeverBeforeHealth = true,
			FeverModifier = (in state, ref fever) => fever *= 1.3
		});
		AddQuirks("character/musedash1/char_11_miku", new() {
			MergeHitLocations = true
		});
		AddQuirks("character/musedash1/char_12_rin", new() {
			// TODO
			// I'm pretty sure this character crashes the game right now anyway, it needs work before it can be supported
			// the latest restructuring changes should help a lot though with making it work
		});
		AddQuirks("character/musedash1/char_1_racer", new() {
			JudgementUpgrade = (in GameSnapshot gameState) => {
				if (gameState.Health >= 150 && gameState.Judgement == Judgement.Miss)
					return Judgement.Great;

				if (gameState.Health < 150 && gameState.Judgement == Judgement.Great)
					return Judgement.Perfect;

				return gameState.Judgement;
			}
		});
		AddQuirks("character/musedash1/char_3_dancer", new() {
			FeverGainPerSecond = (in state, ref fever) => fever = 10, // TODO: This is definitely not right, its based on difficulty...
			FeverModifier = (in state, ref fever) => fever = 0
		});
		AddQuirks("character/musedash1/char_13_wisadel", new() {
			C4Bombs = 6,
			FeverModifier = (in state, ref fever) => fever = 0
		});
		AddQuirks("character/musedash1/char_2_legendburo", new() {
			CompleteMashersInstantly = true,
			DamageModifier = (in state, ref damage) => damage *= 0.1,
			AbleToFly = true, // Correct? review
			Quiz = true
		});
		AddQuirks("character/musedash1/char_2_bloodheir", new() {
			MaxHP = DEFAULT_MAX_HP + 200,
			// TODO: Quality plasma...?
		});
		AddQuirks("character/musedash1/char_1_pirate", new() {
			AutoHoldsSustains = true
		});
		AddQuirks("character/musedash1/char_2_diver", new() {
			// Idk 
		});
		// I was wondering why this was named "char_3" and now I realize it's just a direct copy of Marija's behavior lol
		AddQuirks("character/musedash1/char_3_horse", new() {
			MissComboForgiveness = 100
		});
	}

	static readonly Dictionary<ulong, MuseDash1GameplayQuirks> lookup = [];
	public static readonly MuseDash1GameplayQuirks Default = new() { };

	public static MuseDash1GameplayQuirks GetQuirks(ReadOnlySpan<char> characterName)
		=> lookup.TryGetValue(characterName.Hash(), out MuseDash1GameplayQuirks quirks) ? quirks : Default;

	public static void AddQuirks(ReadOnlySpan<char> name, MuseDash1GameplayQuirks quirks) {
		lookup[name.Hash()] = quirks;
	}

	public static ModifyQuirksFn? ModifyQuirks;
	/// <summary>
	/// Allows game mods to modify quirks
	/// </summary>
	public static void ApplyMods(ref MuseDash1GameplayQuirks quirks) {
		ModifyQuirks?.Invoke(ref quirks);
	}
}

public delegate void ModifyQuirksFn(ref MuseDash1GameplayQuirks quirks);