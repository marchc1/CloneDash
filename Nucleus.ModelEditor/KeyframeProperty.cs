namespace Nucleus.ModelEditor;

/// <summary>
/// Keyframe property types
/// </summary>
public enum KeyframeProperty {
	None,

	Bone_Rotation,
	Bone_Translation,
	Bone_Scale,
	Bone_Shear,

	Bone_Inherit,

	Slot_Attachment,
	Slot_Color,

	Model_DrawOrder
}
