using AssetStudio;
using CloneDash.Characters;
using CloneDash.Common.Game;
using CloneDash.Compatibility.MuseDash;
using Nucleus.Entities;
using Nucleus.Models.Runtime;
using Nucleus.Types;

namespace CloneDash.Elfins;

public class MuseDash1ElfinInstance(MuseDash1ElfinDescriptor descriptor, RhythmGameLevel game)
{
	public const double APPEAR_DELAY = 2.5;
	public const string IN_ANIMATION = "in";
	public const string NORMAL_STANDBY_ANIMATION = "normal_standby";
	public const string STANDBY_ANIMATION = "standby";

	public static readonly Vector2F ElfinPoint = new(-5.5f, -1.3f);

	public readonly MuseDash1ElfinDescriptor Descriptor = descriptor;

	public ModelEntity? Model { get; private set; }

	ModelData? preparedModel;
	ModelInstance? originalModel;

	string Standby => HasAnimation(STANDBY_ANIMATION) ? STANDBY_ANIMATION : NORMAL_STANDBY_ANIMATION;

	public bool HasAnimation(string animation) => Model?.Model?.Data.FindAnimation(animation) != null;

	public void Prepare() => preparedModel = Descriptor.GetBattleModel(game, uploadTextures: false);

	public void Initialize() {
		var data = preparedModel;
		preparedModel = null;
		if (data != null)
			MuseDash1Compatibility.UploadModelDataTextures(data);

		Model = game.Add(ModelEntity.Create(data ?? Descriptor.GetBattleModel(game)));
		Model.Position = ElfinPoint;
		Model.Scale = new(Descriptor.GetBattleScale());
		Hide();
	}

	public void Hide() {
		if (Model == null) return;

		if (originalModel != null) {
			Model.Model = originalModel;
			originalModel = null;
		}

		Model.Visible = false;
		Model.Animations.ClearAllAnimation();
		Model.SetToSetupPose();
	}

	public void Appear() {
		if (Model == null || Model.Visible) return;

		Model.Visible = true;
		PlayIn();
	}

	public bool SwapModel(string prefab) {
		if (Model == null || MuseDash1Compatibility.StreamingAssets.FindAssetByName<GameObject>(prefab) == null)
			return false;

		originalModel ??= Model.Model;
		Model.Model = MuseDash1CharacterDescriptor.PullModelDataFromGameObject(game, prefab).Instantiate();
		PlayIn();
		return true;
	}

	public void PlayAnimation(string animation, string? then = null) {
		if (Model == null || !HasAnimation(animation)) return;

		Model.Animations.SetAnimation(0, animation, false);
		Model.Animations.AddAnimation(0, then ?? Standby, true);
	}

	public void PlayOverlay(string animation) {
		if (Model == null || !HasAnimation(animation)) return;

		Model.Animations.SetAnimation(1, animation, false);
	}

	void PlayIn() {
		Model!.SetToSetupPose();
		if (HasAnimation(IN_ANIMATION))
			PlayAnimation(IN_ANIMATION);
		else
			Model.Animations.SetAnimation(0, Standby, true);
	}
}
