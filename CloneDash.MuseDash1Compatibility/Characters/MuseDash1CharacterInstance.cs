using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
using CloneDash.Game.Logic;
using Nucleus;
using Nucleus.Entities;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using System.Numerics;

namespace CloneDash.Characters;

public class MuseDash1CharacterIndividual(MuseDash1CharacterInstance instance, RhythmGameLevel game, bool isSecondary) : IMuseDash1CharacterIndividual
{
	public readonly MuseDash1CharacterInstance instance = instance;
	public readonly RhythmGameLevel game = game;
	public readonly bool isSecondary = isSecondary;
	public ModelEntity Player = null!;
	public MD1_SpineActionController PlayerController = null!;
	bool LastAnimationWasAir;

	double LastAnimationTime = -20000000;
	double LastAnimationDuration = 0;

	public void PlayAnimation(CharacterAnimationType type) {
		instance.descriptor.PlayCharacterAnimation(type, PlayerController);
		instance.NotifyAnimation(this, isSecondary, type);

		switch(type){
			case CharacterAnimationType.Jump:
			case CharacterAnimationType.JumpHit:
			case CharacterAnimationType.JumpHitGreat:
			case CharacterAnimationType.UpHit:
				LastAnimationWasAir = true;
				break;
			default:
				LastAnimationWasAir = false;
				break;
		}

		// Set last animation fields
		LastAnimationTime = game.Curtime;
		var entry = (Player.Animations.Channels[0].CurrentEntry ?? (Player.Animations.Channels[0].QueuedEntries.TryPeek(out var e) ? e : null));
		if (entry != null)
			LastAnimationDuration = entry.Animation.Duration;
	}

	public void SetPos(Vector2F pos) => Player.Position = pos;
	public void SetScale(Vector2F scale) => Player.Scale = scale;
	public double GetAnimationDuration() {
		var anims = Player.Animations;
		if (!anims.IsPlayingAnimation())
			return 0;

		var entry = anims.Channels[0].CurrentEntry;
		if (entry == null)
			return 0;

		return entry.Animation.Duration;
	}
	public double GetTimeToAnimationEnd() {
		var anims = Player.Animations;
		if (!anims.IsPlayingAnimation())
			return 0;

		var entry = anims.Channels[0].CurrentEntry;
		if (entry == null)
			return 0;

		return entry.Animation.Duration - anims.Channels[0].Time;
	}

	public bool IsInAir(){
		return LastAnimationWasAir && ((game.Curtime - LastAnimationTime) < LastAnimationDuration);
	}

	internal void Reset() {
		LastAnimationWasAir = false;
		LastAnimationTime = -20000000;
		LastAnimationDuration = 0;
		PlayAnimation(CharacterAnimationType.Run);
	}
}

public class MuseDash1CharacterInstance : IMuseDash1CharacterInstance
{
	internal MuseDash1CharacterDescriptor descriptor;
	private RhythmGameLevel game;
	private readonly Conductor conductor;
	private readonly bool hasSupport;
	private readonly ISustainManager sustains;
	private bool swapped;

	private readonly MuseDash1CharacterIndividual Player;
	private readonly MuseDash1CharacterIndividual HologramPlayer;

	public MuseDash1CharacterInstance(MuseDash1CharacterDescriptor descriptor, RhythmGameLevel game, Conductor conductor, GameEventManager events, ISustainManager sustains) {
		this.descriptor = descriptor;
		this.game = game;
		this.conductor = conductor;
		this.sustains = sustains;
		hasSupport = descriptor.ConfigData.BattleShow.Contains(',');
		if (hasSupport)
			events.Listen<CharacterAttackGameEvent>(SwapRoles);

		Player = new(this, game, false);
		HologramPlayer = new(this, game, true);
	}

	public ICharacterDescriptor GetCharacter() => descriptor;
	public IMuseDash1CharacterIndividual GetPrimary() => swapped ? HologramPlayer : Player;
	public IMuseDash1CharacterIndividual GetSecondary() => swapped ? Player : HologramPlayer;

	IShader hologramShader = null!;

	public void Initialize() {
		var level = game;
		// hologram shader
		hologramShader = level.Shaders.LoadFragmentShaderFromFile("shaders", "hologram.fs");

		// set up hologram player before player so it has correct renderorder
		HologramPlayer.Player = level.Add(ModelEntity.Create(descriptor.GetPlayGhostModel(level)));
		Player.Player = level.Add(ModelEntity.Create(descriptor.GetPlayModel(level)));
		// set hologram shader (todo: we really need models to have materials here instead...)
		if (!hasSupport)
			HologramPlayer.Player.Shader = hologramShader;

		// setup animations
		Player.Player.SetToSetupPose();
		HologramPlayer.Player.SetToSetupPose();
		Player.PlayerController = new(descriptor.GetPlayAnimationData(), Player.Player.Animations);
		HologramPlayer.PlayerController = new(descriptor.GetPlayGhostAnimationData(), HologramPlayer.Player.Animations);
	}

	public void Think() {
		if (hasSupport)
			return;

		if (HologramPlayer.Player.PlayingAnimation || HologramPlayer.Player.AnimationQueued) {
			HologramPlayer.Player.Visible = true;
			HologramPlayer.Player.SetShaderUniform("time", NMath.Ease.InQuint((float)(conductor.Time - lastHologramAnimationTime) * 3));
		}
		else {
			HologramPlayer.Player.Visible = false;
		}
	}

	public double GetJumpDuration() {
		return 0.5;
	}

	double lastHologramAnimationTime = -20000000;

	internal void NotifyAnimation(MuseDash1CharacterIndividual museDash1CharacterIndividual, bool isSecondary, CharacterAnimationType type) {
		if (hasSupport) {
			if (museDash1CharacterIndividual != GetPrimary())
				return;

			var support = GetSecondary();
			switch (type) {
				case CharacterAnimationType.In:
				case CharacterAnimationType.Die:
					support.PlayAnimation(type);
					break;
				case CharacterAnimationType.Hurt:
				case CharacterAnimationType.JumpHurt:
					support.PlayAnimation(support.IsInAir() ? CharacterAnimationType.JumpHurt : CharacterAnimationType.Hurt);
					break;
			}
		}
		else if (isSecondary)
			lastHologramAnimationTime = conductor.Time;
	}

	public void Reset(){
		lastHologramAnimationTime = -200000;
		swapped = false;
		Player.Reset();
		HologramPlayer.Reset();

		if (hasSupport)
			return;

		HologramPlayer.Player.Visible = false;
		HologramPlayer.Player.Animations.ClearAllAnimation();
	}

	void SwapRoles(ref CharacterAttackGameEvent ev) {
		if (!ev.Result.Hit || sustains.IsSustaining() || ev.Result.HitEntity.SameTimeNotes.Count != 0)
			return;

		swapped = Random.Shared.Next(2) == 0;
	}
}