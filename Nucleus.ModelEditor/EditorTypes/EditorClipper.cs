using Nucleus.Models;
namespace Nucleus.ModelEditor;

public class EditorClipper(EditorModel model) : ModelClipper<EditorModel, EditorBone, EditorSlot, EditorClippingAttachment>(model, false);