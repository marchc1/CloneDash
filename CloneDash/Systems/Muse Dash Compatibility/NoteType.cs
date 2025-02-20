namespace CloneDash.Systems.Muse_Dash_Compatibility
{
	/// <summary>
	/// Muse Dash enemy type enumeration.
	/// </summary>
	public enum NoteType : uint
	{
		None,
		/// <summary>
		/// One-hit entity
		/// </summary>
		Monster,
		/// <summary>
		/// One-avoid entity
		/// </summary>
		Block,
		/// <summary>
		/// Hold-to-end entity
		/// </summary>
		Press,
		/// <summary>
		/// Undocumented
		/// </summary>
		Hide,
		/// <summary>
		/// Boss-related entity
		/// </summary>
		Boss,
		/// <summary>
		/// Extra HP entity
		/// </summary>
		Hp,
		/// <summary>
		/// Extra score entity
		/// </summary>
		Music,
		/// <summary>
		/// Multi-hit entity
		/// </summary>
		Mul,
		/// <summary>
		/// Trigger a scene change
		/// </summary>
		SceneChange,

		// need further documentation for the rest

		AutoOn,
		AutoOff,
		DisappearOn,
		DisappearOff,
		DisappearBossOn,
		DisappearBossOff,
		SceneHideOn,
		SceneHideOff
	}
}
