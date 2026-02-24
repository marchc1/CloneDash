namespace AssetStudio
{
	public sealed class ParticleSystem : Component{
		public float lengthInSec;
		public float simulationSpeed;
		public int stopAction;
		public int cullingMode;
		public int ringBufferMode;
		public Vector2 ringBufferLoopRange;
		public bool looping;
		public bool prewarm;
		public bool playOnWake;
		public bool useUnscaledTime;
		public bool autoRandomSeed;
		public bool useRigidbodyForVelocity;
		public ParticleSystem(ObjectReader reader) : base(reader) {
			lengthInSec = reader.ReadSingle();
			simulationSpeed = reader.ReadSingle();
			stopAction = reader.ReadInt32();
			cullingMode = reader.ReadInt32();
			ringBufferMode = reader.ReadInt32();
			ringBufferLoopRange = reader.ReadVector2();
			looping = reader.ReadBoolean();
			prewarm = reader.ReadBoolean();
			playOnWake = reader.ReadBoolean();
			useUnscaledTime = reader.ReadBoolean();
			autoRandomSeed = reader.ReadBoolean();
			useRigidbodyForVelocity = reader.ReadBoolean();
		}
	}
}
