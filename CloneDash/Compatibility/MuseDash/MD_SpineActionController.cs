using CloneDash.Compatibility.Unity;
using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.ManagedMemory;
using Nucleus.Models.Runtime;
using System.Collections.Specialized;

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

public class MD_SpineActionController(MD_SpineActionControllerData data, AnimationHandler animation)
{
	public readonly MD_SpineActionControllerData Data = data;
	public readonly AnimationHandler Animation = animation;

	public void PlaySkeletonAction(ReadOnlySpan<char> name, bool isOverride) {
		var action = Data.Get(name);
		if (action == null)
			return;

		if (action.IsRandomSequence) {
			Animation.SetAnimation(0, action.ActionIdx[Random.Shared.Next(0, action.ActionIdx.Length)], action.IsEndLoop);
			if (!action.IsEndLoop)
				Animation.AddAnimation(0, Data.Get("char_run")!.ActionIdx[0], true);
		}
		else {
			Animation.ClearAllAnimation();
			for (int i = 0; i < action.ActionIdx.Length; i++) {
				bool loop = i == action.ActionIdx.Length - 1 && action.IsEndLoop;
				if (i == 0)
					Animation.SetAnimation(0, action.ActionIdx[i], loop);
				else
					Animation.AddAnimation(0, action.ActionIdx[i], loop);
			}
			if(!action.IsEndLoop)
			Animation.AddAnimation(0, Data.Get("char_run")!.ActionIdx[0], true);
		}
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