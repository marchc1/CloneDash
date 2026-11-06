using CloneDash.Compatibility.Unity;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public sealed class UnbeatableWhiteLabelBeatModel : UnitySimScene
{
	const string ASSIST = "Rhythm Assist";
	const string PLAYER = "Rhythm Player";
	const string STAGE_SCENE = "level4";

	readonly SceneObject? assist;
	readonly SceneObject? player;

	public UnbeatableWhiteLabelBeatModel() {
		RenderSpriteMeshes = true;
		player = Import(PLAYER);
		assist = Import(ASSIST);
		Beat = Find(player, "Beat");
		Quaver = Find(assist, "Quaver");
		BeatAnimator = Beat?.GetComponent<SceneAnimator>();
		QuaverAnimator = Quaver?.GetComponent<SceneAnimator>();
		Place(player, 0, 0);
		Place(assist, 0, 0);
	}

	public SceneObject? Beat { get; }
	public SceneAnimator? BeatAnimator { get; }
	public SceneObject? Quaver { get; }
	public SceneAnimator? QuaverAnimator { get; }

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

	public static double TimeLeft(SceneAnimator? animator) {
		if (animator?.CurrentState is not string state)
			return 0;
		return Math.Max(0, animator.GetStateLength(state) - animator.Time);
	}

	public void PlayBeat(string state) => BeatAnimator?.Play(state);

	public void PlayQuaver(string state) => QuaverAnimator?.Play(state);

	public void RenderBeat(Vector2F position, float scale, bool flipX = false, float alpha = 1) => Render(player, Beat, position, scale, flipX, alpha);

	public void RenderQuaver(Vector2F position, float scale, bool flipX = false, float alpha = 1) => Render(assist, Quaver, position, scale, flipX, alpha);

	public void Update(double deltaTime) => RunThinkFuncs(deltaTime);

	static void Place(SceneObject? obj, float x, float y) {
		if (obj == null)
			return;

		obj.Transform.LocalX = x;
		obj.Transform.LocalY = y;
		obj.Transform.LocalZ = 0;
	}

	SceneObject? Import(string name) {
		var obj = ImportGameObject(UnbeatableWhiteLabelCompatibility.FindRootGameObject(STAGE_SCENE, name), null);
		if (obj != null)
			Activate(obj);
		return obj;
	}

	void Activate(SceneObject obj) {
		obj.Active = true;
		obj.Awake();
		foreach (var animator in obj.GetComponents<SceneAnimator>())
			animators.Add(animator);
		foreach (var child in obj.Transform.Children)
			Activate(child.Object);
	}

	void Render(SceneObject? root, SceneObject? character, Vector2F position, float scale, bool flipX, float alpha) {
		var renderer = character?.GetComponent<SceneSpriteRenderer>();
		if (root == null || renderer == null)
			return;

		renderer.FlipX = flipX;
		renderer.Color = new(1, 1, 1, alpha);

		Rlgl.DrawRenderBatchActive();
		Rlgl.PushMatrix();
		Rlgl.Translatef(position.X, position.Y, 0);
		Rlgl.Scalef(scale, scale, 1);
		renderer.Render(this);
		Rlgl.DrawRenderBatchActive();
		Rlgl.PopMatrix();
	}
}
