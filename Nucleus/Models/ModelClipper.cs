using Nucleus.Models.Runtime;
using Nucleus.Rendering;
using Nucleus.Types;
using Poly2Tri;
using Raylib_cs;
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
		if (Active)
			Stencils.End();
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
			var vertex = clipPolygon[i];
			shape.Points.Add(new(vertex.X, vertex.Y, vertex));
		}

		triangles.Clear();
		shape.Triangulate(triangles);

		// Draw stencil mask
		Stencils.Begin();

		Stencils.BeginMask();

		Rlgl.Begin(DrawMode.TRIANGLES);
		Rlgl.Color4ub(255, 255, 255, 255);

		foreach (var triangle in triangles) {
			TriPoint a = triangle.Points[0], b = triangle.Points[1], c = triangle.Points[2];
			Rlgl.Vertex2f((float)a.X, -(float)a.Y);
			Rlgl.Vertex2f((float)b.X, -(float)b.Y);
			Rlgl.Vertex2f((float)c.X, -(float)c.Y);
		}

		Rlgl.End();
		Stencils.EndMask();
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