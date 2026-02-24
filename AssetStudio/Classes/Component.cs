using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#nullable enable

namespace AssetStudio
{
    public abstract class Component : EditorExtension
    {
        public PPtr<GameObject> m_GameObject;

        public GameObject? GetGameObject(){
            if (m_GameObject.TryGet(out GameObject? go))
                return go;
            return null;
        }

        protected Component(ObjectReader reader) : base(reader)
        {
            m_GameObject = new PPtr<GameObject>(reader);
        }
    }
}
