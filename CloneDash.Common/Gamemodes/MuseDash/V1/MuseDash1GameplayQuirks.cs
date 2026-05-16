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

		public readonly bool IsEnemyCall() => EntityType != 0;
		public readonly bool IsHealthFull() => Health >= MaxHealth;
	}

	public delegate void ModifyDoubleFn(in GameSnapshot gameState, ref double number);
	public delegate Judgement JudgementUpgradeFn(in GameSnapshot gameState);

	static ModifyDoubleFn One() => static (in state, ref value) => value = 1;

	public const int DEFAULT_MAX_HP = 250;
	public const int DEFAULT_MAX_FEVER = 120;

	public int MaxHP = DEFAULT_MAX_HP;
	public int MaxFever = DEFAULT_MAX_FEVER;
	public double FeverDuration = 6;

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
		AddCharacterQuirks("character/musedash1/char_1_rock", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MaxHP = 300;
		});
		AddCharacterQuirks("character/musedash1/char_1_rampage", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.GreatToPerfect = 5;
		});
		AddCharacterQuirks("character/musedash1/char_1_sleepy", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MaxHP = 200;
			quirks.Autoplay = true;
		});
		AddCharacterQuirks("character/musedash1/char_1_bunny", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MaxHP = 200;
			quirks.ScoreMultipliers = EntityTypeLookupArray.Create<double>(1,
				(MuseDash1EntityType.Score, 3),
				(MuseDash1EntityType.Ghost, 3),
				(MuseDash1EntityType.Gear, 3)
			);
		});
		AddCharacterQuirks("character/musedash1/char_2_pilot", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.InvincibleInFever = true;
		});
		AddCharacterQuirks("character/musedash1/char_2_robot", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MaxHP = 200;
			quirks.XpMultiplier = 1.5;
		});
		AddCharacterQuirks("character/musedash1/char_2_zombie", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.DyingSeconds = 15;
		});
		AddCharacterQuirks("character/musedash1/char_2_joker", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MaxHP = 200;
			quirks.ScoreModifier = static (in GameSnapshot gameState, ref double scoreGranting) => {
				// TODO:
				// Mixed answers on how this works (minimum 50 or 60? outdated info after nerf/buff?)
				// Also unsure how much to increase score by right now.
			};
		});
		AddCharacterQuirks("character/musedash1/char_3_violin", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MissComboForgiveness = 100;
		});
		AddCharacterQuirks("character/musedash1/char_3_maid", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.HealthBonusTimes = 1;
			quirks.HealthBonusThreshold = 100;
			quirks.HealthBonusGranted = 2.5;
		});
		AddCharacterQuirks("character/musedash1/char_3_magic", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MaxHP = 200;
			quirks.MaxFever = 100;
		});
		AddCharacterQuirks("character/musedash1/char_3_evil", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MaxHP = 200;
			quirks.ScoreModifier = static (in gameState, ref scoreGranting) => {

			};
			quirks.HealthLossPerSecond = static (in _, ref loss) => loss = 10;
		});
		AddCharacterQuirks("character/musedash1/char_3_black", (ref MuseDash1GameplayQuirks quirks) => {
			// todo
		});
		AddCharacterQuirks("character/musedash1/char_1_santa", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MaximumApplicableResultAccuracy = 90;
			quirks.ForgivenAccuracy = 5;
		});
		AddCharacterQuirks("character/musedash1/char_2_jk", (ref MuseDash1GameplayQuirks quirks) => {
			// todo
		});
		AddCharacterQuirks("character/musedash1/char_4_yume", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.BreaksAvoids = true;
		});
		AddCharacterQuirks("character/musedash1/char_5_neko", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.PlayableAfterDeath = true;
		});
		AddCharacterQuirks("character/musedash1/char_1_worker", (ref MuseDash1GameplayQuirks quirks) => {
			// I think this just has nothing lol but idk
		});
		AddCharacterQuirks("character/musedash1/char_6_reimu", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.AbleToFly = true;
			// TODO: "and shrunk the hitbox of obstacles (gears) , making it easier to dodge."
		});
		AddCharacterQuirks("character/musedash1/char_7_clear", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.CatchExtras = true;
		});
		AddCharacterQuirks("character/musedash1/char_3_sister", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.SwapLocations = EntityTypeLookupArray.CreateWhitelist(
				MuseDash1EntityType.Raider,
				MuseDash1EntityType.Hammer,
				MuseDash1EntityType.Gear
			);
		});
		AddCharacterQuirks("character/musedash1/char_8_marisa", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.AbleToFly = true;
			quirks.ScoreModifier = static (in gameState, ref scoreGranting) => {
				if (gameState.IsHealthFull() && (gameState.EntityType == MuseDash1EntityType.Score || gameState.EntityType == MuseDash1EntityType.Heart || gameState.EntityBlood != 0))
					scoreGranting *= 3;
			};
		});
		AddCharacterQuirks("character/musedash1/char_9_amiya", (ref MuseDash1GameplayQuirks quirks) => {
			// TODO: Earns more fever by repelling enemies (need specifics)
			quirks.JudgementUpgrade = (in gameState) => {
				if (!gameState.InFever)
					return gameState.Judgement;

				return gameState.Judgement switch {
					Judgement.Miss => Judgement.Great,
					Judgement.Great => Judgement.Perfect,
					_ => gameState.Judgement
				};
			};
			quirks.PostFeverDismissalTime = 3;
		});
		AddCharacterQuirks("character/musedash1/char_10_ola", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.InvincibleInFever = true;
		});
		AddCharacterQuirks("character/musedash1/char_2_exorcist", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.ConsumeFeverBeforeHealth = true;
			quirks.FeverModifier = (in state, ref fever) => fever *= 1.3;
		});
		AddCharacterQuirks("character/musedash1/char_11_miku", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MergeHitLocations = true;
		});
		AddCharacterQuirks("character/musedash1/char_12_rin", (ref MuseDash1GameplayQuirks quirks) => {
			// TODO
			// I'm pretty sure this character crashes the game right now anyway, it needs work before it can be supported
			// the latest restructuring changes should help a lot though with making it work
		});
		AddCharacterQuirks("character/musedash1/char_1_racer", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.JudgementUpgrade = (in GameSnapshot gameState) => {
				if (gameState.Health >= 150 && gameState.Judgement == Judgement.Miss)
					return Judgement.Great;

				if (gameState.Health < 150 && gameState.Judgement == Judgement.Great)
					return Judgement.Perfect;

				return gameState.Judgement;
			};
		});
		AddCharacterQuirks("character/musedash1/char_3_dancer", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.FeverGainPerSecond = (in state, ref fever) => fever = 10; // TODO: This is definitely not right, its based on difficulty...
			quirks.FeverModifier = (in state, ref fever) => fever = 0;
		});
		AddCharacterQuirks("character/musedash1/char_13_wisadel", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.C4Bombs = 6;
			quirks.FeverModifier = (in state, ref fever) => fever = 0;
		});
		AddCharacterQuirks("character/musedash1/char_2_legendburo", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.CompleteMashersInstantly = true;
			quirks.DamageModifier = (in state, ref damage) => damage *= 0.1;
			quirks.AbleToFly = true; // Correct? review
			quirks.Quiz = true;
		});
		AddCharacterQuirks("character/musedash1/char_2_bloodheir", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MaxHP = DEFAULT_MAX_HP + 200;
			// TODO: Quality plasma...?
		});
		AddCharacterQuirks("character/musedash1/char_1_pirate", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.AutoHoldsSustains = true;
		});
		AddCharacterQuirks("character/musedash1/char_2_diver", (ref MuseDash1GameplayQuirks quirks) => {

		});
		// I was wondering why this was named "char_3" and now I realize it's just a direct copy of Marija's behavior lol
		AddCharacterQuirks("character/musedash1/char_3_horse", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.MissComboForgiveness = 100;
		});

		// elfin order in the english json
		// elfin_cat
		// elfin_angel
		// elfin_death_god
		// elfin_carrot_robot
		// elfin_fan_robot
		// elfin_magic_girl
		// elfin_dragon_girl
		// elfin_devil
		// elfin_doctor
		// elfin_ghost
		// elfin_egg
		// elfin_tv_dog
		// elfin_
		// elfin_
		// elfin_

		// Angela
		AddElfinQuirks("elfin/musedash1/elfin_angel", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.DamageModifier += (in state, ref damage) => {
				damage -= 6;
			};
		});
		AddElfinQuirks("elfin/musedash1/elfin_carrot", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
		// Mio Sir
		AddElfinQuirks("elfin/musedash1/elfin_cat", (ref MuseDash1GameplayQuirks quirks) => {
			quirks.FeverDuration += 2;
		});
		// Thanatos
		AddElfinQuirks("elfin/musedash1/elfin_death_god", (ref MuseDash1GameplayQuirks quirks) => {

		});
		AddElfinQuirks("elfin/musedash1/elfin_devil", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
		AddElfinQuirks("elfin/musedash1/elfin_doctor", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
		AddElfinQuirks("elfin/musedash1/elfin_dragon_girl", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
		AddElfinQuirks("elfin/musedash1/elfin_egg", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
		AddElfinQuirks("elfin/musedash1/elfin_fan_robot", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
		AddElfinQuirks("elfin/musedash1/elfin_ghost", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
		AddElfinQuirks("elfin/musedash1/elfin_lin", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
		AddElfinQuirks("elfin/musedash1/elfin_magic_girl", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
		AddElfinQuirks("elfin/musedash1/elfin_r6", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
		AddElfinQuirks("elfin/musedash1/elfin_saya", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
		AddElfinQuirks("elfin/musedash1/elfin_TV_dog", (ref MuseDash1GameplayQuirks quirks) => {
			
		});
	}

	static readonly Dictionary<ulong, ModifyQuirksFn> characterLookup = [];
	static readonly Dictionary<ulong, ModifyQuirksFn> elfinLookup = [];

	public static readonly MuseDash1GameplayQuirks Default = new() { };

	public static void ApplyCharacterQuirks(ReadOnlySpan<char> characterName, ref MuseDash1GameplayQuirks quirks) {
		if (!characterLookup.TryGetValue(characterName.Hash(), out ModifyQuirksFn? quirkFn))
			return;

		quirkFn(ref quirks);
	}

	public static void ApplyElfinQuirks(ReadOnlySpan<char> elfinName, ref MuseDash1GameplayQuirks quirks) {
		if (!elfinLookup.TryGetValue(elfinName.Hash(), out ModifyQuirksFn? quirkFn))
			return;

		quirkFn(ref quirks);
	}

	public static event ModifyQuirksFn? ModifyQuirks;
	/// <summary>
	/// Allows game mods to modify quirks after applying
	/// </summary>
	public static void ApplyMods(ref MuseDash1GameplayQuirks quirks) {
		ModifyQuirks?.Invoke(ref quirks);
	}

	public static void AddCharacterQuirks(ReadOnlySpan<char> name, ModifyQuirksFn quirkFn) {
		characterLookup[name.Hash()] = quirkFn;
	}

	public static void AddElfinQuirks(ReadOnlySpan<char> name, ModifyQuirksFn quirkFn) {
		elfinLookup[name.Hash()] = quirkFn;
	}
}

public delegate void ModifyQuirksFn(ref MuseDash1GameplayQuirks quirks);