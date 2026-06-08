using System.Collections.Generic;

namespace AssetStudio
{
#nullable enable
	public class Transform : Component
	{
		public Quaternion m_LocalRotation;
		public Vector3 m_LocalPosition;
		public Vector3 m_LocalScale;
		public PPtr<Transform>[] m_Children;
		public PPtr<Transform> m_Father;

		public Transform? GetFather() {
			if (m_Father.TryGet(out var t))
				return t;

			return null;
		}

		public Transform? GetTransform(int index) {
			if (m_Children[index].TryGet(out var t))
				return t;

			return null;
		}

		public void ComputeGlobalTransform(out Vector3 position, out Quaternion rotation) {
			position = m_LocalPosition;
			rotation = m_LocalRotation;

			var parent = GetFather();
			while (parent != null) {
				position = new Vector3(
					position.X * parent.m_LocalScale.X,
					position.Y * parent.m_LocalScale.Y,
					position.Z * parent.m_LocalScale.Z
				);

				position = RotateVector(parent.m_LocalRotation, position);
				position += parent.m_LocalPosition;
				rotation = MultiplyQuaternion(parent.m_LocalRotation, rotation);
				parent = parent.GetFather();
			}
		}

		private static Vector3 RotateVector(Quaternion q, Vector3 v) {
			float ux = q.X, uy = q.Y, uz = q.Z, s = q.W;

			float dotUV = ux * v.X + uy * v.Y + uz * v.Z;
			float dotUU = ux * ux + uy * uy + uz * uz;

			float cx = uy * v.Z - uz * v.Y;
			float cy = uz * v.X - ux * v.Z;
			float cz = ux * v.Y - uy * v.X;

			return new Vector3(
				2f * dotUV * ux + (s * s - dotUU) * v.X + 2f * s * cx,
				2f * dotUV * uy + (s * s - dotUU) * v.Y + 2f * s * cy,
				2f * dotUV * uz + (s * s - dotUU) * v.Z + 2f * s * cz
			);
		}

		private static Quaternion MultiplyQuaternion(Quaternion a, Quaternion b) {
			return new Quaternion(
				a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
				a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
				a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
				a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
			);
		}

		public IEnumerable<Transform> GetChildren() {
			foreach (var child in m_Children) {
				if (child.TryGet(out var t))
					yield return t;
			}
		}

		public Transform(ObjectReader reader) : base(reader) {
			m_LocalRotation = reader.ReadQuaternion();
			m_LocalPosition = reader.ReadVector3();
			m_LocalScale = reader.ReadVector3();

			int m_ChildrenCount = reader.ReadInt32();
			m_Children = new PPtr<Transform>[m_ChildrenCount];
			for (int i = 0; i < m_ChildrenCount; i++) {
				m_Children[i] = new PPtr<Transform>(reader);
			}
			m_Father = new PPtr<Transform>(reader);
		}
	}
}