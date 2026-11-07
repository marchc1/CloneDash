using AssetStudio;
using CloneDash.Compatibility.Unity;
using Nucleus.Common.Graphics;
using Nucleus.Particles;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Transform = AssetStudio.Transform;

namespace CloneDash.Game;

public sealed record MuseDash1ParticleEffect(ParticleSystemDefinition Definition, Matrix4x4 Transform)
{
	public static MuseDash1ParticleEffect? Load(GameObject? gameObject, Func<Texture2D, ITexture> loadTexture, Func<string, bool>? loadMissingFile = null) {
		if (gameObject == null)
			return null;

		var transform = gameObject.GetFirstComponent<Transform>();
		return new(UnityParticleConverter.Convert(gameObject, loadTexture, loadMissingFile), transform == null ? Matrix4x4.Identity : UnityParticleConverter.GetLocalTransform(transform));
	}

	public Matrix4x4 At(float y) {
		var transform = Transform;
		transform.M42 = y;
		return transform;
	}

	public Matrix4x4 At(float x, float y) {
		var transform = Transform;
		transform.M41 = x;
		transform.M42 = y;
		return transform;
	}
}
