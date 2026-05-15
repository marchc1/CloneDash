using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;
using Nucleus;
using Nucleus.Entities;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using System.Numerics;

namespace CloneDash.Characters;


public class MuseDash1CharacterIndividual(MuseDash1CharacterInstance instance, bool isSecondary) : IMuseDash1CharacterIndividual
{
	public readonly MuseDash1CharacterInstance instance = instance;
	public ModelEntity Player;
	public MD1_SpineActionController PlayerController;

	public void PlayAnimation(CharacterAnimationType type) {
		instance.descriptor.PlayCharacterAnimation(type, PlayerController);
		instance.NotifyAnimation(this, isSecondary, type);
	}

	public void SetPos(Vector2F pos) => Player.Position = pos;
	public void SetScale(Vector2F scale) => Player.Scale = scale;
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

		Player = new(this, false);
		HologramPlayer = new(this, true);
	}

	public ICharacterDescriptor GetCharacter() => descriptor;
	public double GetDefaultHP() => defaultHP;
	public IMuseDash1CharacterIndividual GetPrimary() => Player;
	public IMuseDash1CharacterIndividual GetSecondary() => HologramPlayer;

	IShader hologramShader = null!;

	public void Initialize() {
		var level = game;
		hologramShader = level.Shaders.LoadFragmentShaderFromFile("shaders", "hologram.fs");
		Player.Player = level.Add(ModelEntity.Create(descriptor.GetPlayModel(level)));
		HologramPlayer.Player = level.Add(ModelEntity.Create(descriptor.GetPlayGhostModel(level)));
		HologramPlayer.Player.Shader = hologramShader;

		Player.Player.SetToSetupPose();
		Player.PlayerController = new(descriptor.GetPlayAnimationData(), Player.Player.Animations);
		HologramPlayer.PlayerController = new(descriptor.GetPlayGhostAnimationData(), HologramPlayer.Player.Animations);
	}

	// this hack REALLY sucks, todo fix this
	public bool IsInAir() => Player.PlayerController.Animation.Channels[0].CurrentEntry?.Animation?.Name?.Contains("double") ?? false;
	private double lastHologramAnimationTime = -20000;

	public void Think(){
		if (HologramPlayer.Player.PlayingAnimation || HologramPlayer.Player.AnimationQueued) {
			HologramPlayer.Player.Visible = true;
			HologramPlayer.Player.SetShaderUniform("time", NMath.Ease.InQuint((float)(game.Conductor.Time - lastHologramAnimationTime) * 3));
		}
		else {
			HologramPlayer.Player.Visible = false;
		}
	}

	public void Reset(){
		HologramPlayer.Player.Visible = false;
		HologramPlayer.Player.Animations.ClearAllAnimation();
	}

	internal void NotifyAnimation(MuseDash1CharacterIndividual museDash1CharacterIndividual, bool isSecondary, CharacterAnimationType type) {
		if(isSecondary)
			lastHologramAnimationTime = game.Conductor.Time;
	}
}