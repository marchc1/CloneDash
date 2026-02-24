using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
	public sealed class GameObject : EditorExtension
	{
		public PPtr<Component>[] m_Components;
		public string m_Name;

		public Transform m_Transform;
		public MeshRenderer m_MeshRenderer;
		public MeshFilter m_MeshFilter;
		public SkinnedMeshRenderer m_SkinnedMeshRenderer;
		public Animator m_Animator;
		public Animation m_Animation;

		public GameObject(ObjectReader reader) : base(reader) {
			int m_Component_size = reader.ReadInt32();
			m_Components = new PPtr<Component>[m_Component_size];
			for (int i = 0; i < m_Component_size; i++) {
				if ((version[0] == 5 && version[1] < 5) || version[0] < 5) //5.5 down
				{
					int first = reader.ReadInt32();
				}
				m_Components[i] = new PPtr<Component>(reader);
			}

			var m_Layer = reader.ReadInt32();
			m_Name = reader.ReadAlignedString();
		}

#nullable enable
		public T? GetFirstComponent<T>() where T : Component {
			foreach (var compPtr in m_Components) {
				if (!compPtr.TryGet(out var comp)) continue;

				if (comp is not T castComp) continue;
				return castComp;
			}

			return null;
		}
		public T? GetComponentByName<T>(ReadOnlySpan<char> name) where T : Component {
			foreach (var compPtr in m_Components) {
				if (!compPtr.TryGet(out var comp)) continue;

				if (comp is not T castComp) continue;
				switch (comp) {
					case MonoBehaviour mb:
						return name.Equals(mb.m_Name, StringComparison.InvariantCulture) ? castComp
								: mb.m_Script.TryGet(out var script) ? name.Equals(script.m_Name, StringComparison.InvariantCulture) ? castComp : null : null;
				}
			}

			return null;
		}

		public IEnumerable<Component> Components {
			get {
				foreach (var compPtr in m_Components) {
					if (compPtr.TryGet<Object>(out var result))
						if (result is Component c)
							yield return c;
						else
							throw new Exception($"A Unity type needs a deserializer: {result.serializedType}");
				}
			}
		}

		public MonoBehaviour? GetMonoBehaviorByScriptName(string? name = null) {
			foreach (var compPtr in m_Components) {
				if (!compPtr.TryGet(out var comp)) continue;

				if (comp is not MonoBehaviour mb) continue;
				var scriptPtr = mb.m_Script;
				if (!scriptPtr.TryGet(out var script)) continue;

				if (script.m_Name == name)
					return mb;
			}

			return null;
		}
	}
#nullable disable
}
