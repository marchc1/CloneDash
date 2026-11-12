using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;

namespace CloneDash.Scenes;

public class MuseDash1MultiHitTip : UnitySimScene
{
	const string NAME = "MultiHitTip";

	readonly HashSet<SerializedFile> pinnedFiles = [];

	SceneAnimator? animator;

	public void Hide() {
		if (root != null)
			root.Active = false;
	}

	public void Initialize() {
		root = ImportGameObject(MuseDash1Compatibility.StreamingAssets.FindAssetByName<GameObject>(NAME), null);
		foreach (var obj in allObjects) obj.Awake();

		animator = root?.GetComponent<SceneAnimator>();
		if (animator != null)
			animators.Add(animator);

		foreach (var file in pinnedFiles)
			MuseDash1Compatibility.StreamingAssets.Release(file);
		pinnedFiles.Clear();
	}

	public void Play() {
		if (root == null)
			return;

		root.Active = true;
		animator?.Play(NAME);
		BuildRenderOrder();
	}

	public void Render() {
		foreach (var renderer in sortedRenderers) renderer.Render(this);
	}

	public void Think(double dt) => RunThinkFuncs(dt);

	protected override bool LoadMissingFile(string fileName) => MuseDash1Compatibility.StreamingAssets.LoadDependenciesContaining(NAME, new HashSet<string> { fileName }, pinnedFiles);
}
