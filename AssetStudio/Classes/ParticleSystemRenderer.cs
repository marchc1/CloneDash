using System.Collections.Generic;
using System.Numerics;

namespace AssetStudio
{
	public sealed class ParticleSystemRenderer : Renderer
	{
		public ushort m_RenderMode;
		public ushort m_SortMode;
		public float m_MinParticleSze;
		public float m_MaxParticleSize;
		public float m_CameraVelocityScale;
		public float m_VelocityScale;
		public float m_LengthScale;
		public float m_SortingFudge;
		public float m_NormalDirection;
		public float m_ShadowBias;
		public int m_RenderAlignment;
		public Vector3 m_Pivot;
		public Vector3 m_Flip;
		public bool m_UseCustomVertexStreams;
		public bool m_EnableGPUInstancing;
		public bool m_ApplyActiveColorSpace;
		public bool m_AllowRoll;
		public byte[] m_VertexStreams;
		public PPtr<Mesh> m_Mesh;
		public PPtr<Mesh> m_Mesh1;
		public PPtr<Mesh> m_Mesh2;
		public PPtr<Mesh> m_Mesh3;
		public int m_MaskInteraction = 0;
		public ParticleSystemRenderer(ObjectReader reader) : base(reader) {
			m_RenderMode = reader.ReadUInt16();
			m_SortMode = reader.ReadUInt16();
			m_MinParticleSze = reader.ReadSingle();
			m_MaxParticleSize = reader.ReadSingle();
			m_CameraVelocityScale = reader.ReadSingle();
			m_VelocityScale = reader.ReadSingle();
			m_LengthScale = reader.ReadSingle();
			m_SortingFudge = reader.ReadSingle();
			m_NormalDirection = reader.ReadSingle();
			m_ShadowBias = reader.ReadSingle();
			m_RenderAlignment = reader.ReadInt32();
			m_Pivot = reader.ReadVector3();
			m_Flip = reader.ReadVector3();
			m_UseCustomVertexStreams = reader.ReadBoolean();
			m_EnableGPUInstancing = reader.ReadBoolean();
			m_ApplyActiveColorSpace = reader.ReadBoolean();
			m_AllowRoll = reader.ReadBoolean();
			m_VertexStreams = reader.ReadUInt8Array();
			m_Mesh = new(reader);
			m_Mesh1 = new(reader);
			m_Mesh2 = new(reader);
			m_Mesh3 = new(reader);
			m_MaskInteraction = reader.ReadInt32();
		}
	}
}
