using CloneDash.Characters;
using Nucleus.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloneDash.Common.Gamemodes.MuseDash.V1;

/// <summary>
/// A single character, capable of its own animations and rendering changes
/// </summary>
public interface IMuseDash1CharacterIndividual
{
	double GetTimeToAnimationEnd();
	double GetAnimationDuration();
	void PlayAnimation(CharacterAnimationType type);
	void SetPos(Vector2F vector2F);
	void SetScale(Vector2F value);
	bool IsInAir();
}


/// <summary>
/// This is an instance of the character selected
/// </summary>
public interface IMuseDash1CharacterInstance : ICharacterInGameInstance
{
	IMuseDash1CharacterIndividual GetPrimary();
	IMuseDash1CharacterIndividual GetSecondary();
	void Initialize();
	void Think();
	void Reset();
	double GetJumpDuration();
}
