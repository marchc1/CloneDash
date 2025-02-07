namespace Nucleus.ModelEditor
{
	/// <summary>
	/// The current editor mode when no operators are active.
	/// </summary>
	public enum Editor_DefaultOperator {
		/// <summary>
		/// Aim a bone towards your mouse position
		/// </summary>
		PoseBoneToTarget,
		/// <summary>
		/// Change the weights of mesh vertices
		/// </summary>
		ChangeMeshWeights,
		/// <summary>
		/// Create new bones in the viewport
		/// </summary>
		CreateNewBones,
		/// <summary>
		/// Rotate selection
		/// </summary>
		RotateSelection,
		/// <summary>
		/// Translate selection
		/// </summary>
		TranslateSelection,
		/// <summary>
		/// Scale selection
		/// </summary>
		ScaleSelection,
		/// <summary>
		/// Shear selection
		/// </summary>
		ShearSelection
	}
}
