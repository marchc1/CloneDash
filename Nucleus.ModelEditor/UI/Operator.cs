namespace Nucleus.ModelEditor.UI
{
	/// <summary>
	/// A user operator. 
	/// <br></br>
	/// <br></br>
	/// Features:
	/// - Override the main in-editor operations with your own operations
	/// - Override the rendering process of the editor
	/// <br></br>
	/// <br></br>
	/// Self-note: operator naming convention should be {TYPE}{OPERATION}Operator
	/// <br></br>
	/// ex. Image (the type), Set Parent (the operation) == ImageSetParentOperator
	/// </summary>
	public abstract class Operator {
		/// <summary>
		/// During the lifetime of the operator, the UI determinations (what's selected) shouldn't change.
		/// <br></br>
		/// The operator is automatically killed when these determinations change.
		/// </summary>
		public PreUIDeterminations UIDeterminations { get; set; }
		/// <summary>
		/// Called after UI determinations are set.
		/// </summary>
		public virtual void Activate() { }
		/// <summary>
		/// Called in a few ways:
		/// <br></br>
		/// - The user completed the operation
		/// <br></br>
		/// - The operation was cancelled due to a UI determinations change
		/// </summary>
		public virtual void Deactivate() { }

		public virtual void ChangeEditorProperties() { }
	}
}
