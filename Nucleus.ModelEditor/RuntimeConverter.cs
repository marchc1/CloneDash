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

	public ModelData LoadModelFromEditor(EditorModel model) {
		ModelData data = new ModelData();
		data.Name = model.Name;
		data.TextureAtlas = model.Images.TextureAtlas;
		data.FormatVersion = Model4System.MODEL_FORMAT_VERSION;

		Dictionary<EditorBone, BoneData> lookupBone = [];
		Dictionary<EditorSlot, SlotData> lookupSlot = [];
		{
			var boneIndex = 0;
			foreach (var bone in model.GetAllBones()) {
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

				data.BoneDatas.Add(boneData);
				lookupBone.Add(bone, boneData);
				boneIndex += 1;
			}
		}
		{
			var slotIndex = 0;

			foreach (var slot in model.Slots) {
				var slotData = new SlotData();

				slotData.Name = slot.Name;
				slotData.Index = slotIndex;
				slotData.BoneData = lookupBone.TryGetValue(slot.Bone, out var boneData) ? boneData : throw new KeyNotFoundException("Can't find the Slot's bone! This should never happen!");
				slotData.Color = slot.SetupColor;
				slotData.BlendMode = slot.Blending;
				slotData.Attachment = slot.SetupActiveAttachment?.Name ?? null;

				data.SlotDatas.Add(slotData);
				lookupSlot[slot] = slotData;
				slotIndex++;
			}
		}
		{
			// Placeholder; write a single skin pair.
			// TODO: implement skins
			// TODO: what the hell was I thinking with the whole <path> stuff? Need to go back and 
			// figure out a better solution here
			var skin = new Skin();
			skin.Name = "default";
			data.DefaultSkin = skin;
			foreach (var slot in model.Slots) {
				foreach (var attachment in slot.Attachments) {
					Attachment? realAttachment;

					switch (attachment) {
						case EditorRegionAttachment region:
							var newRegion = new RegionAttachment();

							newRegion.Position = region.Position;
							newRegion.Rotation = region.Rotation;
							newRegion.Scale = region.Scale;
							newRegion.Path = region.GetPath().TrimStart('<').TrimEnd('>');

							realAttachment = newRegion;
							break;
						case EditorMeshAttachment mesh: {
								var newMesh = new MeshAttachment();

								newMesh.Position = mesh.Position;
								newMesh.Rotation = mesh.Rotation;
								newMesh.Scale = mesh.Scale;

								List<AttachmentTriangle> trianglesInst = [];
								List<AttachmentVertex> verticesInst = [];
								Dictionary<EditorVertex, int> verticesArrPtr = [];
								Dictionary<EditorVertex, AttachmentVertex> editorToRealVertex = [];

								mesh.RefreshDelaunator();
								foreach (var triangle in mesh.Triangles) {
									EditorVertex? ev1 = triangle.Points[0].AssociatedObject as EditorVertex;
									EditorVertex? ev2 = triangle.Points[1].AssociatedObject as EditorVertex;
									EditorVertex? ev3 = triangle.Points[2].AssociatedObject as EditorVertex;

									if (ev1 == null) continue;
									if (ev2 == null) continue;
									if (ev3 == null) continue;

									SetupVertexExport(lookupBone, mesh, trianglesInst, verticesInst, verticesArrPtr, editorToRealVertex, ev1, out int v1);
									SetupVertexExport(lookupBone, mesh, trianglesInst, verticesInst, verticesArrPtr, editorToRealVertex, ev2, out int v2);
									SetupVertexExport(lookupBone, mesh, trianglesInst, verticesInst, verticesArrPtr, editorToRealVertex, ev3, out int v3);

									trianglesInst.Add(new() {
										V1 = v1,
										V2 = v2,
										V3 = v3,
									});
								}

								newMesh.Triangles = trianglesInst.ToArray();
								newMesh.Vertices = verticesInst.ToArray();

								newMesh.Path = mesh.GetPath().TrimStart('<').TrimEnd('>');
								realAttachment = newMesh;
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

								clip.RefreshDelaunator();
								foreach (var vertex in clip.GetVertices()) {
									SetupVertexExport(lookupBone, clip, verticesInst, verticesArrPtr, editorToRealVertex, vertex, out _);
								}

								newClip.Vertices = verticesInst.ToArray();
								realAttachment = newClip;
							}
							break;
						default: continue;
					}

					realAttachment.Name = attachment.Name;
					skin.SetAttachment(lookupSlot[slot].Index, attachment.Name, realAttachment);
				}
			}
			data.Skins.Add(skin);
		}

		{
			foreach(var animation in model.Animations) {
				var animationData = new Animation();
				animationData.Name = animation.Name;
				animationData.Duration = animation.CalculateMaxTime();

				foreach(var editTimeline in animation.Timelines) {
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

						case ActiveAttachmentTimeline editTimelineCast: {
								Models.Runtime.ActiveAttachmentTimeline tl = new();

								var oldFCurve = editTimelineCast.Curve;
								Models.FCurve<string?> newFCurve = new();
								foreach(var oldKF in oldFCurve.GetKeyframes()) 
									newFCurve.AddKeyframe(new(oldKF.Time, oldKF.Value?.Name));

								tl.Curves[0] = newFCurve;
								tl.SlotIndex = lookupSlot[editTimelineCast.Slot].Index;
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

		return data;
	}
}