using CloneDash.Common.Game;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Nucleus.Types;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelStageComponent(UnbeatableWhiteLabelCameraRig camera, Conductor conductor) : UnitySimScene, IDisposable, IRhythmGameBackgroundPreRenderer, IRhythmGameComponent, IRhythmGameThinker
{
	const float FAR_PLANE = 5000f;
	const float FIELD_OF_VIEW = 60f;
	const float NEAR_PLANE = 0.3f;
	const string NOTE_ASSETS = "sharedassets2.assets";
	const float REFERENCE_ASPECT = 1.778f;
	const string STAGE_SCENE = "level4";

	static readonly string[] BackgroundRoots = ["Clouds", "Skybox", "TrainStation"];
	static readonly string[] NotePrefabs = ["Default Note", "Dodge Note", "Double Note", "Freestyle Note", "Hold Note", "Spam Note"];
	static readonly string[] TransparentMaterials = ["CelVertexShaderTransparent", "PuffyClouds", "TransparentWindow"];

	readonly Dictionary<string, SceneObject> prefabs = [];
	double lastTime = double.NaN;

	public SceneObject? Assist { get; private set; }
	public SceneObject? Clef { get; private set; }
	public SceneObject? LeftGuide { get; private set; }
	public SceneObject? Master { get; private set; }
	public RhythmGameThinkPhase Phase => RhythmGameThinkPhase.Present;
	public SceneObject? Player { get; private set; }
	public SceneObject? RightGuide { get; private set; }
	public SceneObject? Treble { get; private set; }

	public static SceneObject? Find(SceneObject? root, string name) {
		if (root == null)
			return null;
		if (root.Name == name)
			return root;

		foreach (var child in root.Transform.Children)
			if (Find(child.Object, name) is SceneObject found)
				return found;
		return null;
	}

	public void Dispose() {
		UnloadMeshes();
		GC.SuppressFinalize(this);
	}

	public SceneObject? Instantiate(string prefab) {
		if (!prefabs.TryGetValue(prefab, out var template))
			return null;

		var instance = SceneObject.Instantiate(template);
		instance.Active = true;
		return instance;
	}

	public void Load() {
		RenderSpriteMeshes = true;
		Assist = ImportStageObject("Rhythm Assist");
		Clef = ImportStageObject("Clef");
		LeftGuide = ImportStageObject("Left Guide");
		Master = ImportStageObject("Rhythm Master");
		Player = ImportStageObject("Rhythm Player");
		RightGuide = ImportStageObject("Right Guide");
		Treble = ImportStageObject("Treble");

		foreach (var name in NotePrefabs)
			if (ImportGameObject(UnbeatableWhiteLabelCompatibility.FindRootGameObject(NOTE_ASSETS, name), null) is SceneObject template)
				prefabs[name] = template;

		BuildMeshes([.. BackgroundRoots.Select(ImportStageObject)]);
	}

	public void PreRenderBackground(FrameState frameState) {
		float aspect = frameState.WindowWidth / frameState.WindowHeight;
		float fieldOfView = FIELD_OF_VIEW;
		if (aspect < 1.7f) {
			float horizontal = MathF.Atan(MathF.Tan(FIELD_OF_VIEW * MathF.PI / 360) * REFERENCE_ASPECT);
			fieldOfView = MathF.Atan(MathF.Tan(horizontal) / aspect) * 360 / MathF.PI;
		}
		RenderMeshes(new(camera.X, camera.Y, -camera.Distance), System.Numerics.Quaternion.Identity, fieldOfView, aspect, NEAR_PLANE, FAR_PLANE);
	}

	public void Render(SceneObject? root) {
		if (root == null || !IsActiveInHierarchy(root))
			return;

		List<(float Z, SceneSpriteRenderer Renderer)> renderers = [];
		CollectRenderers(root, renderers);
		renderers.Sort(static (a, b) => b.Z.CompareTo(a.Z));
		foreach (var (_, renderer) in renderers)
			renderer.Render(this);
	}

	public void Reset() {
		lastTime = double.NaN;
	}

	public void Think(FrameState frameState) {
		double time = conductor.Time;
		double deltaTime = double.IsNaN(lastTime) ? 0 : Math.Max(0, time - lastTime);
		lastTime = time;
		RunThinkFuncs(deltaTime);
	}

	protected override SceneMeshMaterial CreateMeshMaterial(AssetStudio.Material material) {
		var result = base.CreateMeshMaterial(material);
		result.Hidden = material.m_Name.Contains("SDF");
		result.Transparent = TransparentMaterials.Contains(material.m_Name);
		foreach (var (key, value) in material.m_SavedProperties.m_Floats)
			if (key == "_OpacityMultiplier")
				result.Color.W *= value;
		return result;
	}

	static void CollectRenderers(SceneObject obj, List<(float Z, SceneSpriteRenderer Renderer)> renderers) {
		if (!obj.Active)
			return;

		obj.Transform.GetWorldPosition(out _, out _, out float z);
		foreach (var renderer in obj.GetComponents<SceneSpriteRenderer>())
			renderers.Add((z, renderer));
		foreach (var child in obj.Transform.Children)
			CollectRenderers(child.Object, renderers);
	}

	void Activate(SceneObject obj) {
		obj.Awake();
		foreach (var animator in obj.GetComponents<SceneAnimator>())
			animators.Add(animator);
		foreach (var child in obj.Transform.Children)
			Activate(child.Object);
	}

	SceneObject? ImportStageObject(string name) {
		var obj = ImportGameObject(UnbeatableWhiteLabelCompatibility.FindRootGameObject(STAGE_SCENE, name), null);
		if (obj != null)
			Activate(obj);
		return obj;
	}
}
