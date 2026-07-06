using Nucleus.Models.Runtime;
using Nucleus.Rendering;
using Nucleus.Types;
using Poly2Tri;
using Raylib_cs;
using System.Buffers;

namespace Nucleus.Models;

public interface IClipPolygon<in SlotType>
{
	int GetVerticesCount();
	int ComputeWorldVerticesInto(SlotType slot, Vector2F[] into);
}

// Since clipping is done via GPU stencils, all that's really needed here is some generics for slot logic.
// This lets EditorModel and ModelInstance use pretty much the exact same logic with no changes needed
public abstract class ModelClipper<ModelType, BoneType, SlotType, ClipAttachmentType>(ModelType model, bool flipY)
	where ModelType : IModelInterface<BoneType, SlotType>
	where BoneType : class
	where SlotType : class
	where ClipAttachmentType : class, IClipPolygon<SlotType>
{
	public bool Active { get; protected set; }
	public ModelType Model = model;

	private Vector2F[]? _clipPolygon;

	public bool FlipY { get; set; } = flipY;

	public void End() {
		bool renderMask = false;
		switch ((M4S_StencilMode)Model4System.m4s_stencilmode.GetInt()) {
			case M4S_StencilMode.On:
			default:
				break;
			case M4S_StencilMode.Off:
				return;
			case M4S_StencilMode.RenderMask:
				renderMask = true;
				break;
		}

		if (Active && !renderMask)
			Stencils.End();

		Active = false;
		_endAt = null;
		_workingAttachment = null;

		if (_clipPolygon == null)
			return;

		ArrayPool<Vector2F>.Shared.Return(_clipPolygon, true);
		_clipPolygon = null;

		// Do not clear triangles or Points because it now intentionally persists between frames for caching
		
		// triangles.Clear();
		// shape.Points.Clear();
	}

	private SlotType? _endAt;
	private ClipAttachmentType? _workingAttachment;
	private int _verticesLength;
	private readonly Shape _shape = new Shape();
	private readonly List<Triangle> _triangles = [];

	private Vector2F[]? _cachedVertices;
	private int _cachedVerticesLength;
	private ClipAttachmentType? _cachedAttachment;
	private bool _hasCachedTriangulation;

	public void Start(ClipAttachmentType attachment, SlotType slot, string? endAt = null) {
		if (_workingAttachment != null) return;

		bool renderMask = false;
		switch ((M4S_StencilMode)Model4System.m4s_stencilmode.GetInt()) {
			case M4S_StencilMode.On:
			default:
				break;
			case M4S_StencilMode.Off:
				return;
			case M4S_StencilMode.RenderMask:
				renderMask = true;
				break;
		}

		Active = true;
		_workingAttachment = attachment;
		_endAt = endAt == null ? null : Model.FindSlot(endAt);

		_clipPolygon = ArrayPool<Vector2F>.Shared.Rent(attachment.GetVerticesCount());
		_verticesLength = attachment.ComputeWorldVerticesInto(slot, _clipPolygon);

		bool verteciesChanged =
			_hasCachedTriangulation &&
			ReferenceEquals(_cachedAttachment, attachment) &&
			_cachedVerticesLength == _verticesLength &&
			_cachedVertices != null &&
			_clipPolygon.AsSpan(0, _verticesLength).SequenceEqual(_cachedVertices.AsSpan(0, _verticesLength));

		if (!verteciesChanged) {
			_shape.Points.Clear();
			_shape.Points.EnsureCapacity(_verticesLength);
			for (int i = 0; i < _verticesLength; i++) {
				var vertex = _clipPolygon[i];
				_shape.Points.Add(new(vertex.X, vertex.Y));
			}

			_triangles.Clear();
			_shape.Triangulate(_triangles);

			// Update the cache
			if (_cachedVertices == null || _cachedVertices.Length < _verticesLength) {
				if (_cachedVertices != null)
					ArrayPool<Vector2F>.Shared.Return(_cachedVertices);
				_cachedVertices = ArrayPool<Vector2F>.Shared.Rent(_verticesLength);
			}

			_clipPolygon.AsSpan(0, _verticesLength).CopyTo(_cachedVertices);
			_cachedVerticesLength = _verticesLength;
			_cachedAttachment = attachment;
			_hasCachedTriangulation = true;
		}

		// Draw stencil mask
		if (!renderMask) {
			Stencils.Begin();
			Stencils.BeginMask();
		}

		Rlgl.Begin(DrawMode.TRIANGLES);
		Rlgl.Color4ub(255, 255, 255, 255);

		foreach (var triangle in _triangles) {
			TriPoint a = triangle.Points[0], b = triangle.Points[1], c = triangle.Points[2];
			if (FlipY) {
				Rlgl.Vertex2f((float)a.X, -(float)a.Y);
				Rlgl.Vertex2f((float)b.X, -(float)b.Y);
				Rlgl.Vertex2f((float)c.X, -(float)c.Y);
			}
			else {
				Rlgl.Vertex2f((float)a.X, (float)a.Y);
				Rlgl.Vertex2f((float)b.X, (float)b.Y);
				Rlgl.Vertex2f((float)c.X, (float)c.Y);
			}
		}

		Rlgl.End();
		if (!renderMask)
			Stencils.EndMask();
	}

	public void NextSlot(SlotType slot) {
		if (!Active) return;
		if (_endAt == null) return;

		if (_endAt == slot) {
			End();
		}
	}
}