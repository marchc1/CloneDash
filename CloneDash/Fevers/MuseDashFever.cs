using AssetStudio;
using CloneDash.Compatibility.MuseDash;
using CloneDash.Compatibility.Unity;
using CloneDash.Game;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloneDash.Fevers;

public class MuseDashFever : BaseMuseDashUnitySimScene, IFeverDescriptor
{
	public static MuseDashFever? GetFever(ReadOnlySpan<char> name) {
		if (name != "battle_fever")
			return null;
		// ^^ todo
		return new();
	}

	SceneObject root = null!;
	SceneObject background = null!;
	SceneSpriteRenderer whiteboard = null!;
	readonly List<SceneObject> particles = [];

	public void Initialize(DashGameLevel game) {
		var feverObj = MuseDashCompatibility.StreamingAssets.FindAssetByName<GameObject>("battle_fever")!;
		var feverEffectManager = new MonoBehaviourReader(feverObj.GetComponentByName<MonoBehaviour>("FeverEffectManager")!);

		var transform = feverObj.GetFirstComponent<AssetStudio.Transform>();
		root = new SceneObject();
		root.Transform.ReadFrom(transform!);

		background = ImportGameObject(feverEffectManager.Get<GameObject>("m_Background")!, root.Transform);
		whiteboard = new SceneSpriteRenderer(feverEffectManager.Get<SpriteRenderer>("m_WhitBoardRender")!);
		Vector3 outScenePosition = feverEffectManager.GetAny<Vector3>("outScenePosition");
		List<GameObject?> particles = feverEffectManager.GetList<GameObject>("m_Particles");
		foreach (var particle in particles)
			if (particle != null)
				this.particles.Add(ImportGameObject(particle, root.Transform));

		BuildRenderOrder();
	}

	public void Render(DashGameLevel game) {
		Rlgl.PushMatrix();
		foreach (var renderer in sortedRenderers) renderer.Render(this);
		Rlgl.PopMatrix();
	}

	public void Start(DashGameLevel game) {
		throw new NotImplementedException();
	}

	public void Think(DashGameLevel game) {
		throw new NotImplementedException();
	}
}
