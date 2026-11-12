using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.ModelEditor;

public class RuntimeConverter
{
	/// <summary>
	/// A written vertex attachment and where each editor vertex ended up in it
	/// </summary>
	private record ExportedVertices(VertexAttachment Runtime, Dictionary<EditorVertex, int> Indices);

	/// <summary>
	/// Linear part of <paramref name="transform"/> applied to <paramref name="vector"/>
	/// </summary>
	public static Vector2F LinearToWorld(in Transformation transform, Vector2F vector)
		=> new(vector.X * transform.A + vector.Y * transform.B, vector.X * transform.C + vector.Y * transform.D);

	/// <summary>
	/// Inverse of <see cref="LinearToWorld"/>, zero for a degenerate matrix
	/// </summary>
	public static Vector2F LinearToLocal(in Transformation transform, Vector2F vector) {
		float det = transform.A * transform.D - transform.B * transform.C;
		if (det == 0) return Vector2F.Zero;
		float inv = 1 / det;
		return new((vector.X * transform.D - vector.Y * transform.B) * inv, (vector.Y * transform.A - vector.X * transform.C) * inv);
	}

	/// <summary>
	/// Editor deform key to the runtime layout: one pair per weight for weighted attachments, one per vertex otherwise
	/// </summary>
	private static float[] ExportDeformOffsets(float[] offsets, DeformTimeline timeline, ExportedVertices exported, List<EditorBone> boneByIndex) {
		var vertices = exported.Runtime.GetVertices() ?? [];
		bool weighted = vertices.Any(x => x.Weights is { Length: > 0 });
		var starts = exported.Runtime.GetWeightStarts();
		int count;
		if (!weighted)
			count = vertices.Length;
		else if (vertices.Length == 0)
			count = 0;
		else
			count = starts[starts.Length - 1] + (vertices[vertices.Length - 1].Weights?.Length ?? 0);
		float[] result = new float[count * 2];

		for (int t = 0; t < timeline.Vertices.Count && t * 2 + 1 < offsets.Length; t++) {
			if (!exported.Indices.TryGetValue(timeline.Vertices[t], out int i)) continue;
			var bone = t < timeline.Bones.Count ? timeline.Bones[t] : null;

			int k = -1;
			if (!weighted) {
				if (bone == null)
					k = i;
			}
			else if (bone != null && vertices[i].Weights is { } weights) {
				for (int j = 0; j < weights.Length; j++) {
					if (boneByIndex[weights[j].Bone] == bone) {
						k = starts[i] + j;
						break;
					}
				}
			}
			if (k < 0) continue;

			result[k * 2] = offsets[t * 2];
			result[k * 2 + 1] = offsets[t * 2 + 1];
		}

		return result;
	}

	private static void SetupVertexExport(
											Dictionary<EditorBone, BoneData> boneDataLookup,
											EditorVertexAttachment vertexAttachment,
											List<AttachmentVertex> verticesInst,
											Dictionary<EditorVertex, int> verticesArrPtr,
											Dictionary<EditorVertex, AttachmentVertex> editorToRealVertex,
											EditorVertex eVertex,
											out int vIndex
										) {
		if (!verticesArrPtr.TryGetValue(eVertex, out vIndex)) {
			vIndex = verticesInst.Count;
			var newVertex = new AttachmentVertex() {
				X = eVertex.X,
				Y = eVertex.Y,
				U = eVertex.U,
				V = eVertex.V
			};
			verticesInst.Add(newVertex);
			editorToRealVertex.Add(eVertex, newVertex);
			verticesArrPtr.Add(eVertex, vIndex);

			if (vertexAttachment.GetVertexWeightInformation(eVertex, out var bones, out var weights, out var positions)) {
				newVertex.Weights = new AttachmentWeight[bones.Length];

				for (int i = 0; i < bones.Length; i++) {
					newVertex.Weights[i] = new(boneDataLookup[bones[i]].Index, weights[i], positions[i]);
				}
			}
		}
	}
	private static void SetupVertexExport(
											Dictionary<EditorBone, BoneData> boneDataLookup,
											EditorVertexAttachment vertexAttachment,
											List<AttachmentTriangle> trianglesInst,
											List<AttachmentVertex> verticesInst,
											Dictionary<EditorVertex, int> verticesArrPtr,
											Dictionary<EditorVertex, AttachmentVertex> editorToRealVertex,
											EditorVertex eVertex,
											out int vIndex
										) {
		if (!verticesArrPtr.TryGetValue(eVertex, out vIndex)) {
			vIndex = verticesInst.Count;
			var newVertex = new AttachmentVertex() {
				X = eVertex.X,
				Y = eVertex.Y,
				U = eVertex.U,
				V = eVertex.V
			};
			verticesInst.Add(newVertex);
			editorToRealVertex.Add(eVertex, newVertex);
			verticesArrPtr.Add(eVertex, vIndex);

			if (vertexAttachment.GetVertexWeightInformation(eVertex, out var bones, out var weights, out var positions)) {
				newVertex.Weights = new AttachmentWeight[bones.Length];

				for (int i = 0; i < bones.Length; i++) {
					newVertex.Weights[i] = new(boneDataLookup[bones[i]].Index, weights[i], positions[i]);
										}
			}
		}
	}

	/// <summary>
	/// Named skins to leave out, along with the bones, slots and constraints only they use
	/// </summary>
	public HashSet<EditorSkin> ExcludedSkins { get; } = [];

	/// <summary>
	/// Its Export checkbox is on and its skin isn't left out
	/// Linked meshes go too when their parent's Export is off
	/// </summary>
	private bool IsExported(EditorAttachment attachment) {
		if (attachment.Skin != null && ExcludedSkins.Contains(attachment.Skin)) return false;
		for (EditorAttachment? current = attachment; current != null; current = (current as EditorMeshAttachment)?.LinkedParent) {
			if (!current.Export) return false;
		}
		return true;
	}

	readonly HashSet<EditorBone> droppedBones = [];
	readonly HashSet<EditorSlot> droppedSlots = [];
	readonly HashSet<EditorConstraint> droppedConstraints = [];

	private void FindDropped(EditorModel model) {
		droppedBones.Clear();
		droppedSlots.Clear();
		droppedConstraints.Clear();
		if (ExcludedSkins.Count == 0) return;

		List<EditorSkin> kept = [];
		foreach (var skin in model.Skins) {
			if (!ExcludedSkins.Contains(skin))
				kept.Add(skin);
		}
		foreach (var bone in model.GetAllBones()) {
			if ((bone.Parent != null && droppedBones.Contains(bone.Parent)) || (bone.SkinRequired && !kept.Any(x => x.Bones.Contains(bone))))
				droppedBones.Add(bone);
		}

		// kept weighted attachments keep their bones
		foreach (var slot in model.Slots) {
			foreach (var attachment in slot.Attachments) {
				if (attachment is not EditorVertexAttachment vertices || !IsExported(attachment)) continue;
				foreach (var weights in vertices.Weights) {
					for (var bone = weights.Bone; bone != null && droppedBones.Remove(bone); bone = bone.Parent)
						;
				}
			}
		}

		foreach (var slot in model.Slots) {
			if (droppedBones.Contains(slot.Bone))
				droppedSlots.Add(slot);
		}

		List<EditorConstraint> allConstraints = [.. model.IkConstraints, .. model.TransformConstraints,.. model.PathConstraints];
		foreach (var constraint in allConstraints) {
			bool dropped = (constraint.SkinRequired && !kept.Any(x => x.Constraints.Contains(constraint))) || constraint.Bones.Any(droppedBones.Contains);
			dropped |= constraint switch {
				EditorIkConstraint ik => droppedBones.Contains(ik.Target),
				EditorTransformConstraint transform => droppedBones.Contains(transform.Target),
				EditorPathConstraint path => droppedSlots.Contains(path.Target),
				_ => false
			};
			if (dropped)
				droppedConstraints.Add(constraint);
		}
	}

	public ModelData LoadModelFromEditor(EditorModel model) {
		FindDropped(model);
		ModelData data = new ModelData();
		data.Name = model.Name;
		data.TextureAtlas = model.Images.TextureAtlas;
		data.FormatVersion = Model4System.MODEL_FORMAT_VERSION;

		Dictionary<EditorBone, BoneData> lookupBone = [];
		Dictionary<EditorSlot, SlotData> lookupSlot = [];
		List<EditorBone> boneByIndex = [];
		// Setup pose world transforms, the model might be posed right now
		Dictionary<EditorBone, Transformation> setupWorld = [];
		{
			var boneIndex = 0;
			foreach (var bone in model.GetAllBones()) {
				if (droppedBones.Contains(bone)) continue;
				BoneData boneData = new BoneData();

				boneData.Name = bone.Name;
				boneData.Index = boneIndex;
				boneData.TransformMode = bone.TransformMode;
				boneData.Length = bone.Length;
				boneData.Position = bone.SetupPosition;
				boneData.Rotation = bone.SetupRotation;
				boneData.Scale = bone.SetupScale;
				boneData.Shear = bone.SetupShear;
				boneData.Parent = bone.Parent == null ? null : lookupBone.TryGetValue(bone.Parent, out BoneData? boneDataParent) ? boneDataParent : null;
				boneData.SkinRequired = bone.SkinRequired;

				data.BoneDatas.Add(boneData);
				lookupBone.Add(bone, boneData);
				boneByIndex.Add(bone);
				setupWorld[bone] = Transformation.CalculateWorldTransformation(
					bone.SetupPosition, bone.SetupRotation, bone.SetupScale, bone.SetupShear, bone.SetupTransformMode,
					bone.Parent != null && setupWorld.TryGetValue(bone.Parent, out var parentWorld) ? parentWorld : (Transformation?)null
				);
				boneIndex += 1;
			}
		}
		{
			var slotIndex = 0;

			foreach (var slot in model.Slots) {
				if (droppedSlots.Contains(slot)) continue;
				var slotData = new SlotData();

				slotData.Name = slot.Name;
				slotData.Index = slotIndex;
				slotData.BoneData = lookupBone.TryGetValue(slot.Bone, out var boneData) ? boneData : throw new KeyNotFoundException("Can't find the Slot's bone! This should never happen!");
				slotData.Color = slot.SetupColor;
				slotData.DarkColor = slot.TintBlack ? slot.SetupDarkColor : null;
				slotData.BlendMode = slot.Blending;
				slotData.Attachment = slot.SetupActiveAttachment is { } setupAttachment && IsExported(setupAttachment) ? setupAttachment.SkinKey : null;

				data.SlotDatas.Add(slotData);
				lookupSlot[slot] = slotData;
				slotIndex++;
			}
		}

		Dictionary<EditorConstraint, ConstraintData> lookupConstraint = [];
		// Index into the list for that constraint type
		Dictionary<EditorConstraint, int> constraintIndex = [];
		{

			foreach (var ik in model.IkConstraints.Where(x => !droppedConstraints.Contains(x)))
				ExportConstraint(ik, new IkConstraintData() {
					Target = lookupBone[ik.Target],
					BendDirection = ik.BendDirection,
					Compress = ik.Compress,
					Stretch = ik.Stretch,
					Uniform = ik.Uniform,
					Mix = ik.Mix,
					Softness = ik.Softness
				}, data.IkConstraints, lookupBone, lookupConstraint, constraintIndex);

			foreach (var transform in model.TransformConstraints.Where(x => !droppedConstraints.Contains(x)))
				ExportConstraint(transform, new TransformConstraintData() {
					Target = lookupBone[transform.Target],
					Local = transform.Local,
					Relative = transform.Relative,
					OffsetRotation = transform.OffsetRotation,
					OffsetX = transform.OffsetX,
					OffsetY = transform.OffsetY,
					OffsetScaleX = transform.OffsetScaleX,
					OffsetScaleY = transform.OffsetScaleY,
					OffsetShearY = transform.OffsetShearY,
					RotateMix = transform.RotateMix,
					TranslateMix = transform.TranslateMix,
					ScaleMix = transform.ScaleMix,
					ShearMix = transform.ShearMix
				}, data.TransformConstraints, lookupBone, lookupConstraint, constraintIndex);

			foreach (var path in model.PathConstraints.Where(x => !droppedConstraints.Contains(x)))
				ExportConstraint(path, new PathConstraintData() {
					Target = lookupSlot[path.Target],
					PositionMode = path.PositionMode,
					SpacingMode = path.SpacingMode,
					RotateMode = path.RotateMode,
					OffsetRotation = path.OffsetRotation,
					Position = path.Position,
					Spacing = path.Spacing,
					RotateMix = path.RotateMix,
					TranslateMix = path.TranslateMix
				}, data.PathConstraints, lookupBone, lookupConstraint, constraintIndex);
		}

		Dictionary<EditorEvent, EventData> lookupEvent = [];
		foreach (var editorEvent in model.Events) {
			var eventData = new EventData() {
				Name = editorEvent.Name,
				Int = editorEvent.Int,
				Float = editorEvent.Float,
				String = editorEvent.String,
				AudioPath = editorEvent.AudioPath,
				Volume = editorEvent.Volume,
				Balance = editorEvent.Balance
			};
			data.Events.Add(eventData);
			lookupEvent[editorEvent] = eventData;
		}

		Dictionary<EditorVertexAttachment, ExportedVertices> exportedVertices = [];
		{
			// TODO: what the hell was I thinking with the whole <path> stuff? Need to go back and 
			// figure out a better solution here
			var skin = new Skin();
			skin.Name = "default";
			data.DefaultSkin = skin;
			data.Skins.Add(skin);
			Dictionary<EditorSkin, Skin> skinFor = [];
			foreach (var editorSkin in model.Skins) {
				if (ExcludedSkins.Contains(editorSkin)) continue;
				var named = new Skin() { Name = editorSkin.Name };
				List<BoneData> namedBones = [];
				foreach (var skinBone in editorSkin.Bones) {
					if (lookupBone.TryGetValue(skinBone, out var skinBoneData))
						namedBones.Add(skinBoneData);
				}
				named.Bones = namedBones;
				List<ConstraintData> namedConstraints = [];
				foreach (var skinConstraint in editorSkin.Constraints) {
					if (lookupConstraint.TryGetValue(skinConstraint, out var skinConstraintData))
						namedConstraints.Add(skinConstraintData);
				}
				named.Constraints = namedConstraints;
				skinFor[editorSkin] = named;
				data.Skins.Add(named);
			}

			foreach (var slot in model.Slots) {
				if (droppedSlots.Contains(slot)) continue;
				foreach (var attachment in slot.Attachments) {
					if (!IsExported(attachment)) continue;
					Attachment? realAttachment;

					switch (attachment) {
						case EditorRegionAttachment region:
							var newRegion = new RegionAttachment();

							newRegion.Position = region.Position;
							newRegion.Rotation = region.Rotation;
							newRegion.Scale = region.Scale;
							newRegion.Color = region.Color;
							newRegion.Path = region.GetPath().TrimStart('<').TrimEnd('>');

							realAttachment = newRegion;
							break;
						case EditorMeshAttachment mesh: {
								var newMesh = new MeshAttachment();

								newMesh.Position = mesh.Position;
								newMesh.Rotation = mesh.Rotation;
								newMesh.Scale = mesh.Scale;
								newMesh.Color = mesh.Color;

								List<AttachmentTriangle> trianglesInst = [];
								List<AttachmentVertex> verticesInst = [];
								Dictionary<EditorVertex, int> verticesArrPtr = [];
								Dictionary<EditorVertex, AttachmentVertex> editorToRealVertex = [];

								// Linked meshes write the parent's geometry, the runtime swaps in the parent's anyway.
								var source = mesh;
								HashSet<EditorMeshAttachment> seen = [mesh];
								while (source.LinkedParent != null && seen.Add(source.LinkedParent))
									source = source.LinkedParent;

								if (mesh.LinkedParent != null && (mesh.LinkedParent.Skin == null || !ExcludedSkins.Contains(mesh.LinkedParent.Skin))) {
									newMesh.ParentKey = mesh.LinkedParent.SkinKey;
									newMesh.ParentSkin = mesh.LinkedParent.Skin?.Name;
									newMesh.InheritDeform = mesh.InheritDeform;
								}

								// Keep the editor's vertex order, timelines index vertices
								foreach (var vertex in source.GetVertices())
									SetupVertexExport(lookupBone, source, trianglesInst, verticesInst, verticesArrPtr, editorToRealVertex, vertex, out _);

								source.RefreshDelaunator();
								var triangles = source.Triangles;
								for (int t = 0; t < triangles.Count; t += 3) {
									EditorVertex ev1 = triangles[t], ev2 = triangles[t + 1], ev3 = triangles[t + 2];
									SetupVertexExport(lookupBone, source, trianglesInst, verticesInst, verticesArrPtr, editorToRealVertex, ev1, out int v1);
									SetupVertexExport(lookupBone, source, trianglesInst, verticesInst, verticesArrPtr, editorToRealVertex, ev2, out int v2);
									SetupVertexExport(lookupBone, source, trianglesInst, verticesInst, verticesArrPtr, editorToRealVertex, ev3, out int v3);

									trianglesInst.Add(new() {
										V1 = v1,
										V2 = v2,
										V3 = v3,
									});
								}

								newMesh.Triangles = trianglesInst.ToArray();
								newMesh.Vertices = verticesInst.ToArray();

								if (source != mesh) {
									// synced copy, match by position
									var own = mesh.GetVertices().ToList();
									var shared = source.GetVertices().ToList();
									if (own.Count == shared.Count) {
										for (int i = 0; i < own.Count; i++) {
											if (verticesArrPtr.TryGetValue(shared[i], out int index))
												verticesArrPtr.TryAdd(own[i], index);
										}
									}
								}

								newMesh.Path = mesh.GetPath().TrimStart('<').TrimEnd('>');
								realAttachment = newMesh;
								exportedVertices[mesh] = new(newMesh, verticesArrPtr);
							}
							break;
						case EditorClippingAttachment clip: {
								var newClip = new ClippingAttachment();

								newClip.Position = clip.Position;
								newClip.Rotation = clip.Rotation;
								newClip.Scale = clip.Scale;
								newClip.EndSlot = clip.EndSlot?.Name;

								List<AttachmentVertex> verticesInst = []; 
								Dictionary<EditorVertex, int> verticesArrPtr = [];
								Dictionary<EditorVertex, AttachmentVertex> editorToRealVertex = [];

								foreach (var vertex in clip.GetVertices()) {
									SetupVertexExport(lookupBone, clip, verticesInst, verticesArrPtr, editorToRealVertex, vertex, out _);
								}

								newClip.Vertices = verticesInst.ToArray();
								realAttachment = newClip;
								exportedVertices[clip] = new(newClip, verticesArrPtr);
							}
							break;
						case EditorPathAttachment path: {
								var newPath = new PathAttachment();

								newPath.Position = path.Position;
								newPath.Rotation = path.Rotation;
								newPath.Scale = path.Scale;
								newPath.Color = path.Color;
								newPath.Closed = path.Closed;
								newPath.ConstantSpeed = path.ConstantSpeed;
								newPath.Lengths = path.Lengths.ToArray();

								List<AttachmentVertex> verticesInst = [];
								Dictionary<EditorVertex, int> verticesArrPtr = [];
								Dictionary<EditorVertex, AttachmentVertex> editorToRealVertex = [];

								foreach (var vertex in path.GetVertices())
									SetupVertexExport(lookupBone, path, verticesInst, verticesArrPtr, editorToRealVertex, vertex, out _);

								newPath.Vertices = verticesInst.ToArray();
								realAttachment = newPath;
								exportedVertices[path] = new(newPath, verticesArrPtr);
							}
							break;
						default: continue;
					}

					realAttachment.Name = attachment.Name;
					var owner = attachment.Skin != null && skinFor.TryGetValue(attachment.Skin, out var namedSkin) ? namedSkin : skin;
					owner.SetAttachment(lookupSlot[slot].Index, attachment.SkinKey, realAttachment);
				}
			}
		}

		{
			foreach(var animation in model.Animations) {
				var animationData = new Animation();
				animationData.Name = animation.Name;
				animationData.Duration = animation.CalculateMaxTime();

				foreach(var editTimeline in animation.Timelines) {
					if (Dropped(editTimeline)) continue;
					Timeline runtimeTimeline;
					switch (editTimeline) {
						// duo bone properties

						case TranslateTimeline editTimelineCast: {
								Models.Runtime.TranslateTimeline tl = new();
								tl.Curves[0] = editTimelineCast.CurveX;
								tl.Curves[1] = editTimelineCast.CurveY;
								tl.BoneIndex = lookupBone[editTimelineCast.Bone].Index;
								runtimeTimeline = tl;
								break;
							}
						case ScaleTimeline editTimelineCast: {
								Models.Runtime.ScaleTimeline tl = new();
								tl.Curves[0] = editTimelineCast.CurveX;
								tl.Curves[1] = editTimelineCast.CurveY;
								tl.BoneIndex = lookupBone[editTimelineCast.Bone].Index;
								runtimeTimeline = tl;
								break;
							}
						case ShearTimeline editTimelineCast: {
								Models.Runtime.ShearTimeline tl = new();
								tl.Curves[0] = editTimelineCast.CurveX;
								tl.Curves[1] = editTimelineCast.CurveY;
								tl.BoneIndex = lookupBone[editTimelineCast.Bone].Index;
								runtimeTimeline = tl;
								break;
							}

						// mono bone properties
						case RotationTimeline editTimelineCast: {
								Models.Runtime.RotationTimeline tl = new();
								tl.Curves[0] = editTimelineCast.Curve;
								tl.BoneIndex = lookupBone[editTimelineCast.Bone].Index;
								runtimeTimeline = tl;
								break;
							}
						case TranslateXTimeline editTimelineCast: {
								Models.Runtime.TranslateXTimeline tl = new();
								tl.Curves[0] = editTimelineCast.Curve;
								tl.BoneIndex = lookupBone[editTimelineCast.Bone].Index;
								runtimeTimeline = tl;
								break;
							}
						case TranslateYTimeline editTimelineCast: {
								Models.Runtime.TranslateYTimeline tl = new();
								tl.Curves[0] = editTimelineCast.Curve;
								tl.BoneIndex = lookupBone[editTimelineCast.Bone].Index;
								runtimeTimeline = tl;
								break;
							}
						case ScaleXTimeline editTimelineCast: {
								Models.Runtime.ScaleXTimeline tl = new();
								tl.Curves[0] = editTimelineCast.Curve;
								tl.BoneIndex = lookupBone[editTimelineCast.Bone].Index;
								runtimeTimeline = tl;
								break;
							}
						case ScaleYTimeline editTimelineCast: {
								Models.Runtime.ScaleYTimeline tl = new();
								tl.Curves[0] = editTimelineCast.Curve;
								tl.BoneIndex = lookupBone[editTimelineCast.Bone].Index;
								runtimeTimeline = tl;
								break;
							}
						case ShearXTimeline editTimelineCast: {
								Models.Runtime.ShearXTimeline tl = new();
								tl.Curves[0] = editTimelineCast.Curve;
								tl.BoneIndex = lookupBone[editTimelineCast.Bone].Index;
								runtimeTimeline = tl;
								break;
							}
						case ShearYTimeline editTimelineCast: {
								Models.Runtime.ShearYTimeline tl = new();
								tl.Curves[0] = editTimelineCast.Curve;
								tl.BoneIndex = lookupBone[editTimelineCast.Bone].Index;
								runtimeTimeline = tl;
								break;
							}

						case SlotColorTimeline editTimelineCast: {
								Models.Runtime.SlotColor4Timeline tl = new();
								tl.Curves[0] = editTimelineCast.CurveR;
								tl.Curves[1] = editTimelineCast.CurveG;
								tl.Curves[2] = editTimelineCast.CurveB;
								tl.Curves[3] = editTimelineCast.CurveA;
								tl.SlotIndex = lookupSlot[editTimelineCast.Slot].Index;
								runtimeTimeline = tl;
								break;
							}

						case SlotColorRGBTimeline editTimelineCast: {
								Models.Runtime.SlotRGBTimeline tl = new();
								tl.Curves[0] = editTimelineCast.CurveR;
								tl.Curves[1] = editTimelineCast.CurveG;
								tl.Curves[2] = editTimelineCast.CurveB;
								tl.SlotIndex = lookupSlot[editTimelineCast.Slot].Index;
								runtimeTimeline = tl;
								break;
							}

						case SlotColorAlphaTimeline editTimelineCast: {
								Models.Runtime.SlotAlphaTimeline tl = new();
								tl.Curves[0] = editTimelineCast.CurveA;
								tl.SlotIndex = lookupSlot[editTimelineCast.Slot].Index;
								runtimeTimeline = tl;
								break;
							}

						case SlotTwoColorTimeline editTimelineCast: {
								Models.Runtime.SlotRGBA2Timeline tl = new();
								tl.Curves[0] = editTimelineCast.CurveR;
								tl.Curves[1] = editTimelineCast.CurveG;
								tl.Curves[2] = editTimelineCast.CurveB;
								tl.Curves[3] = editTimelineCast.CurveA;
								tl.Curves[4] = editTimelineCast.CurveR2;
								tl.Curves[5] = editTimelineCast.CurveG2;
								tl.Curves[6] = editTimelineCast.CurveB2;
								tl.SlotIndex = lookupSlot[editTimelineCast.Slot].Index;
								runtimeTimeline = tl;
								break;
							}

						case ActiveAttachmentTimeline editTimelineCast: {
								Models.Runtime.ActiveAttachmentTimeline tl = new();

								var oldFCurve = editTimelineCast.Curve;
								Models.FCurve<string?> newFCurve = new();
								foreach(var oldKF in oldFCurve.GetKeyframes()) {
									// Keys for an attachment that isn't exported aren't exported either.
									if (oldKF.Value != null && !IsExported(oldKF.Value)) continue;
									newFCurve.AddKeyframe(new(oldKF.Time, oldKF.Value?.SkinKey));
								}

								tl.Curves[0] = newFCurve;
								tl.SlotIndex = lookupSlot[editTimelineCast.Slot].Index;
								runtimeTimeline = tl;
								break;
							}

						// constraints
						case IkConstraintTimeline editTimelineCast: {
								Models.Runtime.IkConstraintTimeline tl = new();
								tl.Curves[0] = editTimelineCast.Mix;
								tl.Curves[1] = editTimelineCast.Softness;
								tl.Curves[2] = editTimelineCast.BendDirection;
								tl.Curves[3] = editTimelineCast.Compress;
								tl.Curves[4] = editTimelineCast.Stretch;
								tl.ConstraintIndex = constraintIndex[editTimelineCast.Constraint];
								runtimeTimeline = tl;
								break;
							}
						case TransformConstraintTimeline editTimelineCast: {
								Models.Runtime.TransformConstraintTimeline tl = new();
								tl.Curves[0] = editTimelineCast.RotateMix;
								tl.Curves[1] = editTimelineCast.TranslateMix;
								tl.Curves[2] = editTimelineCast.ScaleMix;
								tl.Curves[3] = editTimelineCast.ShearMix;
								tl.ConstraintIndex = constraintIndex[editTimelineCast.Constraint];
								runtimeTimeline = tl;
								break;
							}
						case PathPositionTimeline editTimelineCast: {
								PathConstraintPositionTimeline tl = new();
								tl.Curves[0] = editTimelineCast.Position;
								tl.ConstraintIndex = constraintIndex[editTimelineCast.Constraint];
								runtimeTimeline = tl;
								break;
							}
						case PathSpacingTimeline editTimelineCast: {
								PathConstraintSpacingTimeline tl = new();
								tl.Curves[0] = editTimelineCast.Spacing;
								tl.ConstraintIndex = constraintIndex[editTimelineCast.Constraint];
								runtimeTimeline = tl;
								break;
							}
						case PathMixTimeline editTimelineCast: {
								PathConstraintMixTimeline tl = new();
								tl.Curves[0] = editTimelineCast.RotateMix;
								tl.Curves[1] = editTimelineCast.TranslateMix;
								tl.ConstraintIndex = constraintIndex[editTimelineCast.Constraint];
								runtimeTimeline = tl;
								break;
							}

						case DeformTimeline editTimelineCast: {
								if (editTimelineCast.Attachment == null || !exportedVertices.TryGetValue(editTimelineCast.Attachment, out var exported)) {
									if (editTimelineCast.Attachment == null || IsExported(editTimelineCast.Attachment))
										Logs.Warn($"Deform timeline in '{animation.Name}' points at an attachment that wasn't exported, skipping");
									continue;
								}

								Models.Runtime.DeformTimeline tl = new();
								tl.SlotIndex = lookupSlot[editTimelineCast.Slot].Index;
								tl.Skin = editTimelineCast.Attachment.Skin?.Name;
								tl.Attachment = editTimelineCast.Attachment.SkinKey;
								tl.Curve = editTimelineCast.Curve;

								foreach (var offsets in editTimelineCast.Offsets)
									tl.Offsets.Add(ExportDeformOffsets(offsets, editTimelineCast, exported, boneByIndex));

								runtimeTimeline = tl;
								break;
							}

						case DrawOrderTimeline editTimelineCast: {
								Models.Runtime.DrawOrderTimeline tl = new();
								var curve = new FCurve<int[]?>();
								foreach (var keyframe in editTimelineCast.Keys.GetKeyframes()) {
									int[]? order = null;
									if (keyframe.Value != null) {
										List<int> slotIndices = [];
										foreach (var orderSlot in keyframe.Value) {
											if (lookupSlot.TryGetValue(orderSlot, out var orderSlotData))
												slotIndices.Add(orderSlotData.Index);
										}
										order = slotIndices.ToArray();
									}
									curve.AddKeyframe(new(keyframe.Time, order) { Interpolation = KeyframeInterpolation.Constant });
								}
								tl.Curves[0] = curve;
								runtimeTimeline = tl;
								break;
							}

						case EventTimeline editTimelineCast: {
								Models.Runtime.EventTimeline tl = new();
								foreach (var keyframe in editTimelineCast.Keys.GetKeyframes().OrderBy(x => x.Time)) {
									if (keyframe.Value == null) continue;
									foreach (var key in keyframe.Value) {
										if (!lookupEvent.TryGetValue(key.Event, out var eventData)) continue;

										tl.Events.Add(new ModelEvent() {
											Time = keyframe.Time,
											Data = eventData,
											Int = key.Int,
											Float = key.Float,
											String = key.String,
											Volume = key.Volume,
											Balance = key.Balance
										});
									}
								}
								runtimeTimeline = tl;
								break;
							}


						default: Logs.Warn($"Please implement Timeline converter for edit-type '{editTimeline.GetType().Name}'"); continue;
					}

					animationData.Timelines.Add(runtimeTimeline);
				}

				data.Animations.Add(animationData);
			}
		}

		data.ResolveLinkedMeshes();
		return data;
	}

	private static T ExportConstraint<T>(EditorConstraint constraint, T constraintData, List<T> into,
		Dictionary<EditorBone, BoneData> lookupBone, Dictionary<EditorConstraint, ConstraintData> lookupConstraint,
		Dictionary<EditorConstraint, int> constraintIndex) where T : ConstraintData {
		constraintData.Name = constraint.Name;
		constraintData.Order = constraint.Order;
		constraintData.SkinRequired = constraint.SkinRequired;
		List<BoneData> constraintBones = [];
		foreach (var bone in constraint.Bones)
			constraintBones.Add(lookupBone[bone]);
		constraintData.Bones = constraintBones;

		constraintIndex[constraint] = into.Count;
		lookupConstraint[constraint] = constraintData;
		into.Add(constraintData);
		return constraintData;
	}

	private bool Dropped(EditorTimeline timeline) => timeline switch {
		IBoneTimeline bone => droppedBones.Contains(bone.Bone),
		ISlotTimeline slot => droppedSlots.Contains(slot.Slot),
		ConstraintCurveTimeline<EditorIkConstraint> ik => droppedConstraints.Contains(ik.Constraint),
		ConstraintCurveTimeline<EditorTransformConstraint> transform => droppedConstraints.Contains(transform.Constraint),
		ConstraintCurveTimeline<EditorPathConstraint> path => droppedConstraints.Contains(path.Constraint),
		_ => false
	};
}