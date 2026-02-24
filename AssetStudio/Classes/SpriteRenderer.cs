namespace AssetStudio
{
	public sealed class SpriteRenderer : Renderer
	{
		public PPtr<Sprite> m_Sprite;
		public Color m_Color;
		public bool m_FlipX;
		public bool m_FlipY;
		public int m_DrawMode;
		public Vector2 m_Size;
		public float m_AdaptiveModeThreshold;
		public int m_SpriteTileMode;
		public bool m_WasSpriteAssigned;
		public int m_MaskInteraction;
		public int m_SpriteSortPoint;
		public SpriteRenderer(ObjectReader reader) : base(reader) {
			m_Sprite = new(reader);
			m_Color = reader.ReadColor4();
			m_FlipX = reader.ReadBoolean();
			m_FlipY = reader.ReadBoolean();
			m_DrawMode = reader.ReadInt32();
			m_Size = reader.ReadVector2();
			m_AdaptiveModeThreshold = reader.ReadSingle();
			m_SpriteTileMode = reader.ReadInt32();
			m_WasSpriteAssigned = reader.ReadBoolean();
			m_MaskInteraction = reader.ReadInt32();
			m_SpriteSortPoint = reader.ReadInt32();
		}
	}
}
