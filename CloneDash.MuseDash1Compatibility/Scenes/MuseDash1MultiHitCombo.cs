using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using Nucleus.Common.Types;
using Nucleus.ManagedMemory;

namespace CloneDash.Scenes;

public class MuseDash1MultiHitCombo : UnitySimScene, IDisposable
{
	const string NAME = "MultiHitCombo";
	const string TEXT_NAME = "TxtMultiHitCombo";
	const float TEXT_SIZE = 0.85f;

	readonly TextImageRenderItem number = new(0, 0, new(0, 0), 0, new(1, 1), "0", "Snaps Taste", new Color(255, 0, 162), null) {
		Worldspace = true,
		borderColor = new Color(118, 0, 96),
		BorderSize = 6,
		DarkenAmount = 0,
		DesaturateAmount = 0
	};
	readonly HashSet<AssetStudio.SerializedFile> pinnedFiles = [];

	SceneAnimator? dialogAnimator;
	bool showing;
	SceneObject? text;
	SceneAnimator? textAnimator;

	public void Dispose() => number.Dispose();

	public void Hide() {
		showing = false;
		if (root != null)
			root.Active = false;
	}

	public void Initialize() {
		root = ImportGameObject(MuseDash1Compatibility.StreamingAssets.FindAssetByName<AssetStudio.GameObject>(NAME), null);
		foreach (var obj in allObjects) obj.Awake();

		dialogAnimator = root?.GetComponent<SceneAnimator>();
		if (dialogAnimator != null)
			animators.Add(dialogAnimator);

		text = allObjects.FirstOrDefault(obj => obj.Name == TEXT_NAME);
		textAnimator = text?.GetComponent<SceneAnimator>();
		if (textAnimator != null)
			animators.Add(textAnimator);

		foreach (var file in pinnedFiles)
			MuseDash1Compatibility.StreamingAssets.Release(file);
		pinnedFiles.Clear();
	}

	public void Render(IShader? shader) {
		foreach (var renderer in sortedRenderers) renderer.Render(this);

		if (text == null || !IsActiveInHierarchy(text))
			return;

		var color = text.GetColor();
		text.Transform.GetWorldPosition(out float x, out float y, out _);
		text.Transform.GetWorldScale(out float scaleX, out float scaleY);
		if (color.W <= 0 || scaleX == 0 || scaleY == 0)
			return;

		number.StartPosition = new(x, y);
		number.StartScale = new(scaleX * TEXT_SIZE, scaleY * TEXT_SIZE);
		number.Color = new Color(255, 255, 255, (int)(color.W * 255));
		number.Render(0, shader);
	}

	public void SetCount(int count) {
		if (root == null || text == null)
			return;

		if (count >= 1) {
			bool entering = !showing;
			showing = true;
			number.Text = $"{count}";
			root.Active = true;
			text.Active = true;
			Play(dialogAnimator, entering ? "MultiHitDialogStart" : "MultiHitDialogStandby");
			Play(textAnimator, entering ? "MultiHitComboCounterIn" : "MultiHitComboCounterStandby");
			if (entering)
				BuildRenderOrder();
		}
		else if (showing) {
			showing = false;
			Play(dialogAnimator, "MultiHitDialogEnd");
			Play(textAnimator, "MultiHitComboCounterOut");
		}
		else {
			text.Active = false;
			if (dialogAnimator?.CurrentState != "MultiHitDialogEnd")
				root.Active = false;
		}
	}

	public void Think(double dt) {
		RunThinkFuncs(dt);
		number.DetermineRenderTexture();
	}

	protected override bool LoadMissingFile(string fileName) => MuseDash1Compatibility.StreamingAssets.LoadDependenciesContaining(NAME, new HashSet<string> { fileName }, pinnedFiles);

	static void Play(SceneAnimator? animator, string state) {
		animator?.Play(state);
		animator?.Evaluate(0);
	}
}
