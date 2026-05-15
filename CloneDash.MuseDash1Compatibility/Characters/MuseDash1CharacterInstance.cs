using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
using Nucleus;
using Nucleus.Entities;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using System.Numerics;

namespace CloneDash.Characters;

public class MuseDash1CharacterIndividual(MuseDash1CharacterInstance instance, MuseDash1Game game, bool isSecondary) : IMuseDash1CharacterIndividual
{
	public readonly MuseDash1CharacterInstance instance = instance;
	public readonly MuseDash1Game game = game;
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
	private MuseDash1Game game;

	private readonly MuseDash1CharacterIndividual Player;
	private readonly MuseDash1CharacterIndividual HologramPlayer;
	int defaultHP = 250;

	public MuseDash1CharacterInstance(MuseDash1CharacterDescriptor descriptor, MuseDash1Game game) {
		this.descriptor = descriptor;
		this.game = game;

		if (!int.TryParse(descriptor.ConfigData.DefaultHP, out defaultHP))
			defaultHP = 250;

		Player = new(this, game, false);
		HologramPlayer = new(this, game, true);
	}

	public ICharacterDescriptor GetCharacter() => descriptor;
	public double GetDefaultHP() => defaultHP;
	public IMuseDash1CharacterIndividual GetPrimary() => Player;
	public IMuseDash1CharacterIndividual GetSecondary() => HologramPlayer;

	IShader hologramShader = null!;

	public void Initialize() {
		var level = game;
		// hologram shader
		hologramShader = level.Shaders.LoadFragmentShaderFromFile("shaders", "hologram.fs");

		// set up hologram player before player so it has correct renderorder
		HologramPlayer.Player = level.Add(ModelEntity.Create(descriptor.GetPlayGhostModel(level)));
		Player.Player = level.Add(ModelEntity.Create(descriptor.GetPlayModel(level)));
		// set hologram shader (todo: we really need models to have materials here instead...)
		HologramPlayer.Player.Shader = hologramShader;

		// setup animations
		Player.Player.SetToSetupPose();
		HologramPlayer.Player.SetToSetupPose();
		Player.PlayerController = new(descriptor.GetPlayAnimationData(), Player.Player.Animations);
		HologramPlayer.PlayerController = new(descriptor.GetPlayGhostAnimationData(), HologramPlayer.Player.Animations);
	}

	public void Think() {
		if (HologramPlayer.Player.PlayingAnimation || HologramPlayer.Player.AnimationQueued) {
			HologramPlayer.Player.Visible = true;
			HologramPlayer.Player.SetShaderUniform("time", NMath.Ease.InQuint((float)(game.Conductor.Time - lastHologramAnimationTime) * 3));
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
		if (isSecondary)
			lastHologramAnimationTime = game.Conductor.Time;
	}

	public void Reset(){
		lastHologramAnimationTime = -200000;
		Player.Reset();
		HologramPlayer.Reset();

		HologramPlayer.Player.Visible = false;
		HologramPlayer.Player.Animations.ClearAllAnimation();
	}
}