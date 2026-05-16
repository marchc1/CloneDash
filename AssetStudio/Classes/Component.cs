namespace AssetStudio
{
#nullable enable
	public abstract class Component : EditorExtension
    {
        public PPtr<GameObject> m_GameObject;
		public GameObject? GetGameObject() {
			if (m_GameObject.TryGet(out GameObject? go))
				return go;
			return null;
		}
		public Component() { }

        protected Component(ObjectReader reader) : base(reader)
        {
            m_GameObject = new PPtr<GameObject>(reader);
        }
    }
}
