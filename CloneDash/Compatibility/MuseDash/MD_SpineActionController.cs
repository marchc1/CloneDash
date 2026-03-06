using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Entities;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Compatibility.MuseDash;

public class MD_ActionData
{
	public bool Collapsed;
	public bool IsEndLoop;
	public bool IsRandomSequence;
	public bool IsSelfProtect;
	public string Name = "";
	public int ProtectLevel;
	public int SpineActionKeyIndex;
	public string[] ActionIdx = [];
	public int[] ActionEventIdx = [];
}

public ref struct SacPlaySetting
{
	public ReadOnlySpan<char> ActionName;
	public Action? CustomCompleteEvent;

	public SacPlaySetting(ReadOnlySpan<char> name, Action? customCompleteEvent = null) {
		ActionName = name;
		CustomCompleteEvent = customCompleteEvent;
	}
}

public readonly ref struct DoNothingCtx(AnimationChannelEntry entry, MD_SpineActionController controller)
{
	public readonly AnimationChannelEntry Entry = entry;
	public readonly MD_SpineActionController Controller = controller;
}

public class MD_DoNothing
{
	public virtual void Do(DoNothingCtx ctx) { }

	public static readonly MD_DoNothing?[] Lookup = [
		/* 00 */ new MD_DoNothing(),
		/* 01 */ null, // DestroySelf
		/* 02 */ null, // UnBindFromNode
		/* 03 */ null, // NormalNodeOnAttacked
		/* 04 */ new MD_AttackToNormalRun(),
		/* 05 */ null, // UnActiveObject
		/* 06 */ null, // OnNormalAttack
		/* 07 */ null, // FrontRenderObject
		/* 08 */ new MD_UnLockActionProtect(),
		/* 09 */ null, // OnGainEnergyBottle
		/* 10 */ new MD_OnJumpEnd(),
	];

	public static bool TryGetValue(int idx, out MD_DoNothing? nothing){
		if(idx < 0 || idx >= Lookup.Length) {
			nothing = null;
			return false;
		}

		nothing = Lookup[idx];
		return true;
	}
}

public class MD_AttackToNormalRun : MD_DoNothing
{
	public override void Do(DoNothingCtx ctx) {
		if (EngineCore.Level is not DashGameLevel dashLvl)
			return;

		ctx.Controller.PlaySkeletonAction(new(ActionKeys.RUN), true);
	}
}

public class MD_OnJumpEnd : MD_DoNothing
{
	public override void Do(DoNothingCtx ctx) {
		if (EngineCore.Level is not DashGameLevel dashLvl)
			return;

		ctx.Controller.PlaySkeletonAction(new(ActionKeys.RUN), true);
	}
}

public class MD_UnLockActionProtect : MD_DoNothing
{
	public override void Do(DoNothingCtx ctx) {
		ctx.Controller.CurrentProtectionLevel = 0;
		ctx.Controller.CurrentActionName = null;
	}
}

public class MD_SpineActionController(MD_SpineActionControllerData data, AnimationHandler animation)
{
	public readonly MD_SpineActionControllerData Data = data;
	public readonly AnimationHandler Animation = animation;
	readonly char[] currentActionName = new char[256];
	public int CurrentProtectionLevel;
	public ReadOnlySpan<char> CurrentActionName {
		get => currentActionName.SliceNullTerminatedString();
		set {
			value.CopyTo(currentActionName);
			currentActionName[value.Length] = '\0';
		}
	}

	public void PlaySkeletonAction(SacPlaySetting settings, bool isOverride) {
		var action = Data.Get(settings.ActionName);

		if (action == null)
			return;

		if (CheckActionProtect(action))
			return;

		Animation.ClearAllAnimation();
		CurrentActionName = action.Name;
		CurrentProtectionLevel = action.ProtectLevel;

		var del = settings.CustomCompleteEvent;
		if (action.IsRandomSequence) {
			var randIdx = Random.Shared.Next(0, action.ActionIdx.Length);
			var randAnim = action.ActionIdx[randIdx];
			var randEv = action.ActionEventIdx[randIdx];

			var entry = isOverride ? Animation.SetAnimation(0, randAnim, action.IsEndLoop) : Animation.AddAnimation(0, randAnim, action.IsEndLoop);
			if (entry != null && MD_DoNothing.TryGetValue(randEv, out MD_DoNothing? nothing)) {
				entry.OnPlaybackEnd += (e) => nothing?.Do(new(e, this));
				return;
			}
		}
		else {
			int lastIdx = action.ActionIdx.Length - 1;
			for (int i = 0; i < action.ActionIdx.Length; i++) {
				string animName = action.ActionIdx[i];
				bool loop = action.IsEndLoop && i >= lastIdx;
				AnimationChannelEntry? entry = isOverride ? Animation.SetAnimation(0, animName, loop) : Animation.AddAnimation(0, animName, loop);
				if (entry != null && MD_DoNothing.TryGetValue(action.ActionEventIdx[i], out MD_DoNothing? nothing))
					entry.OnPlaybackEnd += (e) => nothing?.Do(new(e, this));
			}
		}
	}

	private bool CheckActionProtect(MD_ActionData action) {
		return Animation == null
			|| (action.IsSelfProtect && CurrentActionName.Equals(action.Name, StringComparison.InvariantCulture))
			|| CurrentProtectionLevel > action.ProtectLevel;
	}
}

public class MD_SpineActionControllerData
{
	public readonly MD_ActionData?[] ActionData;

	public MD_ActionData? Get(ReadOnlySpan<char> name) {
		for (int i = 0, c = ActionData.Length; i < c; i++) {
			var data = ActionData[i];
			if (data == null) continue;
			if (data.Name.Equals(name, StringComparison.InvariantCulture))
				return data;
		}

		if (parent != null)
			return parent.Get(name);

		return null;
	}

	MD_SpineActionControllerData? parent;

	public MD_SpineActionControllerData(MonoBehaviourReader reader, MD_SpineActionControllerData? parent = null) {
		this.parent = parent;
		var animationData = reader.GetAny<List<object>>("actionData")!;
		ActionData = new MD_ActionData?[animationData.Count];

		for (int i = 0; i < animationData.Count; i++) {
			if (animationData[i] is not OrderedDictionary dict) continue;
			MD_ActionData data;
			data = ActionData[i] = new MD_ActionData();

			data.Collapsed = (((byte?)dict["collapsed"]) ?? 0) != 0;
			data.IsEndLoop = (((byte?)dict["isEndLoop"]) ?? 0) != 0;
			data.IsRandomSequence = (((byte?)dict["isRandomSequence"]) ?? 0) != 0;
			data.IsSelfProtect = (((byte?)dict["isSelfProtect"]) ?? 0) != 0;
			data.Name = ((string?)dict["name"]) ?? throw new Exception();
			data.ProtectLevel = (((int?)dict["protectLevel"]) ?? 0);
			data.SpineActionKeyIndex = (((int?)dict["spineActionKeyIndex"]) ?? 0);
			data.ActionIdx = ((List<object>)dict["actionIdx"]!).Cast<string>().ToArray();
			data.ActionEventIdx = ((List<object>)dict["actionEventIdx"]!).Cast<int>().ToArray();
		}
	}
}