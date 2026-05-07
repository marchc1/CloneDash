using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AssetStudio
{
    public sealed class GameObject : EditorExtension
    {
        public List<PPtr<Component>> m_Components;
        public string m_Name;
		public bool m_IsActive;

		public Transform m_Transform;
        public MeshRenderer m_MeshRenderer;
        public MeshFilter m_MeshFilter;
        public SkinnedMeshRenderer m_SkinnedMeshRenderer;
        public Animator m_Animator;
        public Animation m_Animation;
        [JsonIgnore]
        public CubismModel CubismModel;

        public GameObject(ObjectReader reader) : base(reader)
        {
            var m_ComponentSize = reader.ReadInt32();
            m_Components = new List<PPtr<Component>>();
            for (var i = 0; i < m_ComponentSize; i++)
            {
                if (version < (5, 5)) //5.5 down
                {
                    var first = reader.ReadInt32();
                }
                m_Components.Add(new PPtr<Component>(reader));
            }

            var m_Layer = reader.ReadInt32();
            if (version.IsTuanjie && (version > (2022, 3, 2) || (version == (2022, 3, 2) && version.Build >= 11))) //2022.3.2t11(1.1.3) and up
            {
                var m_HasEditorInfo = reader.ReadBoolean();
                reader.AlignStream();
            }
            m_Name = reader.ReadAlignedString();
			var m_Tag = reader.ReadUInt16();
			m_IsActive = reader.ReadBoolean();
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
						var test = name.Equals(mb.m_Name, StringComparison.InvariantCulture) ? castComp
								: mb.m_Script.TryGet(out var script) ? name.Equals(script.m_Name, StringComparison.InvariantCulture) ? castComp : null : null;
						if (test != null)
							return test;
						break;
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
							throw new Exception($"A Unity type needs a deserializer: {result.type}");
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
