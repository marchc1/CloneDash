using Nucleus.Models.Runtime;
using Nucleus.Types;
using System.Buffers;

namespace Nucleus.Models;

// Since clipping is done via GPU stencils, all that's really needed here is some generics for slot logic.
// This lets EditorModel and ModelInstance use pretty much the exact same logic with no changes needed
public abstract class ModelClipper<ModelType, BoneType, SlotType> where ModelType : IModelInterface<BoneType, SlotType> where BoneType : class where SlotType : class
{
	public bool Active { get; protected set; }
	public ModelType Model;

	Vector2F[]? clipPolygon;

	public ModelClipper(ModelType model) {
		Model = model;
	}

	public void End() {
		Active = false;
		endAt = null;
		workingAttachment = null;
		if (clipPolygon != null) {
			ArrayPool<Vector2F>.Shared.Return(clipPolygon, true);
			clipPolygon = null;
		}
		triangles.Clear();
		shape.Points.Clear();
	}

	private SlotType? endAt;
	private ClippingAttachment? workingAttachment;
	private int verticesLength;
	private Poly2Tri.Shape shape = new Poly2Tri.Shape();
	private List<Poly2Tri.Triangle> triangles = [];

	public void Start(ClippingAttachment attachment, SlotInstance slot, string? endAt = null) {
		if (workingAttachment != null) return;

		Active = true;
		workingAttachment = attachment;
		this.endAt = endAt == null ? null : Model.FindSlot(endAt);

		clipPolygon = ArrayPool<Vector2F>.Shared.Rent(attachment.Vertices.Length);
		verticesLength = attachment.ComputeWorldVerticesInto(slot, clipPolygon);

		shape.Points.Clear();
		shape.Points.EnsureCapacity(verticesLength);
		for (int i = 0; i < verticesLength; i++) {
			var vertex = attachment.Vertices[i];
			shape.Points.Add(new(vertex.X, vertex.Y, vertex));
		}

		triangles.Clear();
		shape.Triangulate(triangles);
	}

	public void NextSlot(SlotInstance slot) {
		if (!Active) return;
		if (endAt == null) return;

		if (endAt == slot) {
			End();
			return;
		}
	}
}