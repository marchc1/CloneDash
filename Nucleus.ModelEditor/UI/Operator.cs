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
		public event EditorFile.OnOperatorActivated? OnActivated;
		public event EditorFile.OnOperatorDeactivated? OnDeactivated;
		public virtual string Name => "Unknown Operator Name";
		/// <summary>
		/// During the lifetime of the operator, the UI determinations (what's selected) shouldn't change.
		/// <br></br>
		/// The operator is automatically killed when these determinations change.
		/// </summary>
		public PreUIDeterminations UIDeterminations { get; set; }
		/// <summary>
		/// Called after UI determinations are set.
		/// </summary>
		protected virtual void Activated() { }
		/// <summary>
		/// Called in a few ways:
		/// <br></br>
		/// - The user completed the operation
		/// <br></br>
		/// - The operation was cancelled due to a UI determinations change
		/// </summary>
		protected virtual void Deactivated() { }

		public virtual void ModifyEditor(ModelEditor editor) { }
		public virtual void RestoreEditor(ModelEditor editor) { }

		public virtual bool OverrideSelection => false;
		/// <summary>
		/// If null; all types are selectable. If empty, no types are selectable.
		/// </summary>
		public virtual Type[]? SelectableTypes => null;
		public virtual void Selected(ModelEditor editor, IEditorType type) { }

		public virtual void ChangeEditorProperties(CenteredObjectsPanel panel) { }

		public void CallActivateSubscriptions(EditorFile file) {
			Activated();
			OnActivated?.Invoke(file, this);
		}
		public void CallDeactivateSubscriptions(EditorFile file) {
			Deactivated();
			OnDeactivated?.Invoke(file, this);
		}

		/// <summary>
		/// Deactivate the operator. Should only be called internally; its just a macro.
		/// </summary>
		protected void Deactivate() => ModelEditor.Active.File.DeactivateOperator();
	}
}
