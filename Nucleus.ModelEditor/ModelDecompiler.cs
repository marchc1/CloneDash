using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Nucleus.Common.Models;
using Nucleus.Common.Types;
using Nucleus.Models;
using Nucleus.Core;
using Nucleus.Rendering;
using Nucleus.Types;

using Raylib_cs;

using RT = Nucleus.Models.Runtime;

namespace Nucleus.ModelEditor;

public class ModelDecompiler
{
	public EditorModel Decompile(RT.ModelData data, string imagesDirectory) {
		EditorModel model = new EditorModel();
		model.Name = data.Name ?? "model";
		data.ResolveLinkedMeshes();

		List<EditorBone> bones = [];
		foreach (var boneData in data.BoneDatas) {
			var bone = new EditorBone();
			bone.Model = model;
			bone.Name = boneData.Name;
			bone.Length = boneData.Length;
			bone.SetupTransformMode = boneData.TransformMode;
			bone.SetupPosition = boneData.Position;
			bone.SetupRotation = boneData.Rotation;
			bone.SetupScale = boneData.Scale;
			bone.SetupShear = boneData.Shear;
			bone.SkinRequired = boneData.SkinRequired;

			if (boneData.Parent == null)
				model.Root = bone;
			else {
				bone.Parent = bones[boneData.Parent.Index];
				bone.Parent.Children.Add(bone);
			}

			bones.Add(bone);
		}
		model.InvalidateBonesList();
		model.ResetToSetupPose();

		List<EditorSlot> slots = [];
		foreach (var slotData in data.SlotDatas) {
			var slot = new EditorSlot();
			slot.Name = slotData.Name;
			slot.Bone = bones[slotData.BoneData.Index];
			slot.SetupColor = slotData.Color;
			slot.TintBlack = slotData.DarkColor.HasValue;
			slot.SetupDarkColor = slotData.DarkColor ?? Color.Black;
			slot.Blending = slotData.BlendMode;

			model.Slots.Add(slot);
			slot.Bone.Slots.Add(slot);
			slots.Add(slot);
		}

		Dictionary<RT.EventData, EditorEvent> events = [];
		foreach (var eventData in data.Events) {
			var editorEvent = new EditorEvent() {
				Model = model,
				Name = eventData.Name,
				Int = eventData.Int,
				Float = eventData.Float,
				String = eventData.String,
				AudioPath = eventData.AudioPath,
				Volume = eventData.Volume,
				Balance = eventData.Balance
			};
			model.Events.Add(editorEvent);
			events[eventData] = editorEvent;
		}

		Dictionary<RT.ConstraintData, EditorConstraint> constraints = [];
		foreach (var ik in data.IkConstraints)
			model.IkConstraints.Add(DecompileConstraint(ik, model, bones, constraints, new EditorIkConstraint() {
				Target = bones[ik.Target.Index],
				BendDirection = ik.BendDirection,
				Compress = ik.Compress,
				Stretch = ik.Stretch,
				Uniform = ik.Uniform,
				Mix = ik.Mix,
				Softness = ik.Softness
			}));
		foreach (var transform in data.TransformConstraints)
			model.TransformConstraints.Add(DecompileConstraint(transform, model, bones, constraints,
				new EditorTransformConstraint() {
					Target = bones[transform.Target.Index],
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
				}));
		foreach (var path in data.PathConstraints)
			model.PathConstraints.Add(DecompileConstraint(path, model, bones, constraints, new EditorPathConstraint() {
				Target = slots[path.Target.Index],
				PositionMode = path.PositionMode,
				SpacingMode = path.SpacingMode,
				RotateMode = path.RotateMode,
				OffsetRotation = path.OffsetRotation,
				Position = path.Position,
				Spacing = path.Spacing,
				RotateMix = path.RotateMix,
				TranslateMix = path.TranslateMix
			}));
		foreach (var constraint in constraints.Values)
			constraint.ResetToSetupPose();

		HashSet<string> paths = [];
		List<(EditorClippingAttachment Clip, string EndSlot)> clipEnds = [];
		Dictionary<RT.VertexAttachment, DecompiledVertices> decompiled = [];
		List<(RT.MeshAttachment Mesh, EditorMeshAttachment Editor)> linked = [];
		HashSet<RT.Attachment> seenAttachments = [];
		var defaultSkin = data.DefaultSkin ?? data.Skins.FirstOrDefault();
		// Named skins first, the default skin may hold merged copies of their attachments
		IEnumerable<RT.Skin> skinOrder = defaultSkin == null ? data.Skins : [.. data.Skins.Where(x => x != defaultSkin), defaultSkin];
		foreach (var skin in skinOrder) {
			EditorSkin? editorSkin = null;
			if (skin != defaultSkin) {
				editorSkin = new EditorSkin() { Name = skin.Name, Model = model };
				List<EditorBone> skinBones = [];
				foreach (var skinBone in skin.Bones)
					skinBones.Add(bones[skinBone.Index]);
				editorSkin.Bones = skinBones;
				List<EditorConstraint> skinConstraints = [];
				foreach (var skinConstraint in skin.Constraints) {
					if (constraints.TryGetValue(skinConstraint, out var editorConstraint))
						skinConstraints.Add(editorConstraint);
				}
				editorSkin.Constraints = skinConstraints;
				model.Skins.Add(editorSkin);
			}
			else if (skin.Bones.Count > 0 || skin.Constraints.Count > 0)
				Logs.Warn($"Decompiler: default skin lists bones/constraints, the editor can't hold those on the default skin");

			foreach (var pair in skin.Attachments) {
				if (!seenAttachments.Add(pair.Value)) continue;
				var slot = slots[pair.Key.SlotIndex];
				EditorAttachment? attachment = pair.Value switch {
					RT.RegionAttachment region => DecompileRegion(region, paths),
					RT.MeshAttachment mesh => DecompileMesh(mesh, slot, bones, paths, decompiled),
					RT.ClippingAttachment clip => DecompileClipping(clip, slot, bones, clipEnds, decompiled),
					RT.PathAttachment path => DecompilePath(path, slot, bones, decompiled),
					_ => null
				};

				if (pair.Value is RT.MeshAttachment { IsLinked: true } linkedMesh && attachment is EditorMeshAttachment linkedEditor)
					linked.Add((linkedMesh, linkedEditor));

				if (attachment == null) {
					Logs.Warn($"Decompiler: skipping unsupported attachment '{pair.Key.Name}' ({pair.Value.GetType().Name})");
					continue;
				}

				var key = pair.Key.Name.ToString();
				var name = string.IsNullOrEmpty(pair.Value.Name) ? key : pair.Value.Name;
				if (slot.IsAttachmentNameTaken(name, editorSkin)) {
					int suffix = 2;
					while (slot.IsAttachmentNameTaken($"{name}{suffix}", editorSkin))
						suffix++;
					name = $"{name}{suffix}";
				}
				attachment.Name = name;
				attachment.Skin = editorSkin;
				attachment.Placeholder = name == key ? null : key;
				attachment.Slot = slot;
				slot.Attachments.Add(attachment);
			}
		}

		for (int i = 0; i < slots.Count; i++) {
			var slotData = data.SlotDatas[i];
			if (slotData.Attachment != null)
				slots[i].SetupActiveAttachment = FindByKey(slots[i], slotData.Attachment);
		}

		foreach (var item in clipEnds)
			item.Clip.EndSlot = model.FindSlot(item.EndSlot);

		foreach (var item in linked) {
			if (item.Mesh.ParentMesh == null || !decompiled.TryGetValue(item.Mesh.ParentMesh, out var parent) || parent.Attachment is not EditorMeshAttachment parentMesh) {
				Logs.Warn($"Decompiler: linked mesh '{item.Editor.Name}' has no parent '{item.Mesh.ParentKey}', keeping it unlinked");
				continue;
			}
			item.Editor.LinkedParent = parentMesh;
			item.Editor.InheritDeform = item.Mesh.InheritDeform;
		}

		List<EditorIkConstraint> ikConstraints = [];
		foreach (var ik in data.IkConstraints)
			ikConstraints.Add((EditorIkConstraint)constraints[ik]);
		List<EditorTransformConstraint> transformConstraints = [];
		foreach (var transform in data.TransformConstraints)
			transformConstraints.Add((EditorTransformConstraint)constraints[transform]);
		List<EditorPathConstraint> pathConstraints = [];
		foreach (var path in data.PathConstraints)
			pathConstraints.Add((EditorPathConstraint)constraints[path]);
		var timelineContext = new TimelineContext(data, bones, slots, ikConstraints, transformConstraints, pathConstraints, events, decompiled);
		foreach (var animation in data.Animations)
			model.Animations.Add(DecompileAnimation(animation, model, timelineContext));

		if (data.TextureAtlas != null)
			ExtractImages(data.TextureAtlas, paths, imagesDirectory);

		model.Images.Model = model;
		model.Images.Filepath = imagesDirectory;
		model.ResetToSetupPose();
		return model;
	}

	private EditorRegionAttachment DecompileRegion(RT.RegionAttachment region, HashSet<string> paths) {
		paths.Add(region.Path);
		return new EditorRegionAttachment() {
			Path = region.Path,
			Position = region.Position,
			Rotation = region.Rotation,
			Scale = region.Scale,
			Color = region.Color
		};
	}

	/// <summary>
	/// Editor vertices in runtime vertex order
	/// </summary>
	private record DecompiledVertices(EditorVertexAttachment Attachment, EditorVertex[] Vertices);

	private record TimelineContext(
		RT.ModelData Data,
		List<EditorBone> Bones,
		List<EditorSlot> Slots,
		List<EditorIkConstraint> IkConstraints,
		List<EditorTransformConstraint> TransformConstraints,
		List<EditorPathConstraint> PathConstraints,
		Dictionary<RT.EventData, EditorEvent> Events,
		Dictionary<RT.VertexAttachment, DecompiledVertices> Decompiled
	);

	private static T DecompileConstraint<T>(RT.ConstraintData data, EditorModel model, List<EditorBone> bones,
		Dictionary<RT.ConstraintData, EditorConstraint> constraints, T constraint) where T : EditorConstraint {
		constraint.Model = model;
		constraint.Name = data.Name;
		constraint.Order = data.Order;
		constraint.SkinRequired = data.SkinRequired;
		List<EditorBone> constraintBones = [];
		foreach (var bone in data.Bones)
			constraintBones.Add(bones[bone.Index]);
		constraint.Bones = constraintBones;
		constraints[data] = constraint;
		return constraint;
	}

	private EditorPathAttachment DecompilePath(RT.PathAttachment path, EditorSlot slot, List<EditorBone> bones, Dictionary<RT.VertexAttachment, DecompiledVertices> decompiled) {
		var editorPath = new EditorPathAttachment() {
			Color = path.Color,
			Closed = path.Closed,
			ConstantSpeed = path.ConstantSpeed,
			Lengths = [.. path.Lengths]
		};
		editorPath.Slot = slot;

		var vertices = CreateVertices(editorPath, path.GetVertices() ?? [], bones, false);
		foreach (var vertex in vertices)
			editorPath.ShapeEdges.Add(vertex);
		decompiled[path] = new(editorPath, vertices);

		editorPath.Invalidate();
		return editorPath;
	}

	private EditorMeshAttachment DecompileMesh(RT.MeshAttachment mesh, EditorSlot slot, List<EditorBone> bones, HashSet<string> paths, Dictionary<RT.VertexAttachment, DecompiledVertices> decompiled) {
		paths.Add(mesh.Path);
		var editorMesh = new EditorMeshAttachment() {
			Path = mesh.Path,
			Color = mesh.Color
		};
		editorMesh.Slot = slot;

		// Linked meshes get their own copy of the parent's geometry.
		var vertices = CreateVertices(editorMesh, mesh.GetVertices() ?? [], bones, true);
		decompiled[mesh] = new(editorMesh, vertices);
		var triangles = mesh.GetTriangles() ?? [];

		Dictionary<(int, int), int> edgeUse = [];
		foreach (var tri in triangles) {
			CountEdge(edgeUse, tri.V1, tri.V2);
			CountEdge(edgeUse, tri.V2, tri.V3);
			CountEdge(edgeUse, tri.V3, tri.V1);
		}

		int hullCount = mesh.GetHullCount();
		bool knownHull = hullCount > 2 && hullCount <= vertices.Length;
		List<int> hull = knownHull ? [.. Enumerable.Range(0, hullCount)] : FindHull(edgeUse);
		// hull comes first in order, start at the lowest index
		if (!knownHull && hull.Count > 2) {
			int start = hull.IndexOf(hull.Min());
			List<int> rotated = [];
			for (int i = start; i < hull.Count; i++)
				rotated.Add(hull[i]);
			for (int i = 0; i < start; i++)
				rotated.Add(hull[i]);
			hull = rotated;
			if (hull[1] > hull[hull.Count - 1]) {
				List<int> reversed = [hull[0]];
				for (int i = hull.Count - 1; i >= 1; i--)
					reversed.Add(hull[i]);
				hull = reversed;
			}
		}
		HashSet<int> onHull = [.. hull];
		foreach (var i in hull)
			editorMesh.ShapeEdges.Add(vertices[i]);
		for (int i = 0; i < vertices.Length; i++) {
			if (!onHull.Contains(i))
				editorMesh.SteinerPoints.Add(vertices[i]);
		}

		// No manual edges, they're only in nonessential data.
		editorMesh.Invalidate();
		editorMesh.SourceTriangles = [];
		foreach (var tri in triangles) {
			if (tri.V1 < vertices.Length && tri.V2 < vertices.Length && tri.V3 < vertices.Length)
				editorMesh.SourceTriangles.AddRange([vertices[tri.V1], vertices[tri.V2], vertices[tri.V3]]);
		}
		editorMesh.OrderWeightsFromTriangles();
		return editorMesh;
	}

	private static void CountEdge(Dictionary<(int, int), int> edges, int a, int b) {
		var key = EdgeKey(a, b);
		edges[key] = edges.TryGetValue(key, out int count) ? count + 1 : 1;
	}

	private static (int, int) EdgeKey(int a, int b) => a < b ? (a, b) : (b, a);

	/// <summary>
	/// Walks the edges only used by one triangle and picks the longest loop.
	/// </summary>
	private static List<int> FindHull(Dictionary<(int, int), int> edgeUse) {
		Dictionary<int, List<int>> neighbors = [];
		foreach (var pair in edgeUse) {
			if (pair.Value != 1) continue;
			Add(pair.Key.Item1, pair.Key.Item2, neighbors);
			Add(pair.Key.Item2, pair.Key.Item1, neighbors);
		}

		List<int> best = [];
		HashSet<int> visited = [];
		foreach (var start in neighbors.Keys) {
			if (visited.Contains(start)) continue;

			List<int> loop = [start];
			visited.Add(start);
			int prev = -1;
			int current = start;
			while (true) {
				int next = -1;
				foreach (var neighbor in neighbors[current]) {
					if (neighbor != prev && !visited.Contains(neighbor)) {
						next = neighbor;
						break;
					}
				}
				if (next == -1) break;

				loop.Add(next);
				visited.Add(next);
				prev = current;
				current = next;
			}

			if (loop.Count > best.Count)
				best = loop;
		}

		return best;
	}

	private static void Add(int a, int b, Dictionary<int, List<int>> neighbors) {
		if (!neighbors.TryGetValue(a, out var list))
			neighbors[a] = list = [];
		list.Add(b);
	}

	private EditorClippingAttachment DecompileClipping(RT.ClippingAttachment clip, EditorSlot slot, List<EditorBone> bones,
		List<(EditorClippingAttachment, string)> clipEnds, Dictionary<RT.VertexAttachment, DecompiledVertices> decompiled) {
		var editorClip = new EditorClippingAttachment();
		editorClip.Slot = slot;
		if (clip.EndSlot != null)
			clipEnds.Add((editorClip, clip.EndSlot));

		var vertices = CreateVertices(editorClip, clip.Vertices, bones, false);
		foreach (var vertex in vertices)
			editorClip.ShapeEdges.Add(vertex);
		decompiled[clip] = new(editorClip, vertices);

		editorClip.Invalidate();
		return editorClip;
	}

	private EditorVertex[] CreateVertices(EditorVertexAttachment attachment, RT.AttachmentVertex[] source, List<EditorBone> bones, bool uvs) {
		attachment.SetupWorldTransform();
		var vertices = new EditorVertex[source.Length];

		for (int i = 0; i < source.Length; i++) {
			var src = source[i];
			var vertex = new EditorVertex() {
				Attachment = attachment,
				X = src.X,
				Y = src.Y,
				U = uvs ? src.U : 0,
				V = uvs ? src.V : 0
			};

			if (src.Weights != null && src.Weights.Length > 0) {
				Vector2F world = Vector2F.Zero;
				foreach (var vertexWeight in src.Weights) {
					var bone = bones[vertexWeight.Bone];
					var weights = attachment.Weights.FirstOrDefault(x => x.Bone == bone);
					if (weights == null) {
						weights = new() { Bone = bone };
						attachment.Weights.Add(weights);
					}

					weights.SetVertexWeight(vertex, vertexWeight.Weight);
					weights.SetVertexPos(vertex, vertexWeight.Position);
					world += bone.WorldTransform.LocalToWorld(vertexWeight.Position) * vertexWeight.Weight;
				}

				var local = attachment.WorldTransform.WorldToLocal(world);
				vertex.X = local.X;
				vertex.Y = local.Y;
			}

			vertices[i] = vertex;
		}

		return vertices;
	}

	private EditorAnimation DecompileAnimation(RT.Animation animation, EditorModel model, TimelineContext context) {
		var editorAnimation = new EditorAnimation() {
			Name = animation.Name,
			Model = model,
			Timelines = []
		};

		var bones = context.Bones;
		var slots = context.Slots;
		foreach (var timeline in animation.Timelines) {
			EditorTimeline? editorTimeline = timeline switch {
				RT.TranslateTimeline source => new TranslateTimeline() {
					Bone = bones[source.BoneIndex], CurveX = source.Curves[0], CurveY = source.Curves[1]
				},
				RT.ScaleTimeline source => new ScaleTimeline() {
					Bone = bones[source.BoneIndex], CurveX = source.Curves[0], CurveY = source.Curves[1]
				},
				RT.ShearTimeline source => new ShearTimeline() {
					Bone = bones[source.BoneIndex], CurveX = source.Curves[0], CurveY = source.Curves[1]
				},
				RT.RotationTimeline source => new RotationTimeline() {
					Bone = bones[source.BoneIndex], Curve = source.Curves[0]
				},
				RT.TranslateXTimeline source => new TranslateXTimeline() {
					Bone = bones[source.BoneIndex], Curve = source.Curves[0]
				},
				RT.TranslateYTimeline source => new TranslateYTimeline() {
					Bone = bones[source.BoneIndex], Curve = source.Curves[0]
				},
				RT.ScaleXTimeline source => new ScaleXTimeline() {
					Bone = bones[source.BoneIndex], Curve = source.Curves[0]
				},
				RT.ScaleYTimeline source => new ScaleYTimeline() {
					Bone = bones[source.BoneIndex], Curve = source.Curves[0]
				},
				RT.ShearXTimeline source => new ShearXTimeline() {
					Bone = bones[source.BoneIndex], Curve = source.Curves[0]
				},
				RT.ShearYTimeline source => new ShearYTimeline() {
					Bone = bones[source.BoneIndex], Curve = source.Curves[0]
				},
				RT.SlotColor4Timeline source => new SlotColorTimeline() {
					Slot = slots[source.SlotIndex], CurveR = source.Curves[0], CurveG = source.Curves[1],
					CurveB = source.Curves[2], CurveA = source.Curves[3]
				},
				RT.SlotRGBTimeline source => new SlotColorRGBTimeline() {
					Slot = slots[source.SlotIndex], CurveR = source.Curves[0], CurveG = source.Curves[1],
					CurveB = source.Curves[2]
				},
				RT.SlotAlphaTimeline source => new SlotColorAlphaTimeline() {
					Slot = slots[source.SlotIndex], CurveA = source.Curves[0]
				},
				RT.SlotRGBA2Timeline source => new SlotTwoColorTimeline() {
					Slot = slots[source.SlotIndex], CurveR = source.Curves[0], CurveG = source.Curves[1],
					CurveB = source.Curves[2], CurveA = source.Curves[3], CurveR2 = source.Curves[4],
					CurveG2 = source.Curves[5], CurveB2 = source.Curves[6]
				},
				RT.ActiveAttachmentTimeline source => DecompileAttachmentTimeline(source, slots[source.SlotIndex]),
				RT.IkConstraintTimeline source => new IkConstraintTimeline() {
					Constraint = context.IkConstraints[source.ConstraintIndex], Mix = source.Curves[0],
					Softness = source.Curves[1], BendDirection = source.Curves[2], Compress = source.Curves[3],
					Stretch = source.Curves[4]
				},
				RT.TransformConstraintTimeline source => new TransformConstraintTimeline() {
					Constraint = context.TransformConstraints[source.ConstraintIndex], RotateMix = source.Curves[0],
					TranslateMix = source.Curves[1], ScaleMix = source.Curves[2], ShearMix = source.Curves[3]
				},
				RT.PathConstraintPositionTimeline source => new PathPositionTimeline() {
					Constraint = context.PathConstraints[source.ConstraintIndex], Position = source.Curves[0]
				},
				RT.PathConstraintSpacingTimeline source => new PathSpacingTimeline() {
					Constraint = context.PathConstraints[source.ConstraintIndex], Spacing = source.Curves[0]
				},
				RT.PathConstraintMixTimeline source => new PathMixTimeline() {
					Constraint = context.PathConstraints[source.ConstraintIndex], RotateMix = source.Curves[0],
					TranslateMix = source.Curves[1]
				},
				RT.DeformTimeline source => DecompileDeformTimeline(source, context),
				RT.EventTimeline source => DecompileEventTimeline(source, context),
				RT.DrawOrderTimeline source => DecompileDrawOrderTimeline(source, slots),
				_ => null
			};

			if (editorTimeline == null) {
				Logs.Warn($"Decompiler: skipping unsupported timeline '{timeline.GetType().Name}' in '{animation.Name}'");
				continue;
			}

			editorAnimation.Timelines.Add(editorTimeline);
			if (editorTimeline is SlotColorRGBTimeline or SlotColorAlphaTimeline)
				editorAnimation.SeparatedColors.Add(((ISlotTimeline)editorTimeline).Slot);
		}

		return editorAnimation;
	}

	// Default skin first, then whichever skin has it
	private static EditorAttachment? FindByKey(EditorSlot slot, string key) => slot.Attachments.FirstOrDefault(x => x.Skin == null && x.SkinKey == key)
			?? slot.Attachments.FirstOrDefault(x => x.SkinKey == key);

	private ActiveAttachmentTimeline DecompileAttachmentTimeline(RT.ActiveAttachmentTimeline timeline, EditorSlot slot) {
		var editorTimeline = new ActiveAttachmentTimeline() { Slot = slot };
		foreach (var keyframe in timeline.Curves[0].GetKeyframes())
			editorTimeline.Curve.AddKeyframe(new(keyframe.Time, keyframe.Value == null ? null : FindByKey(slot, keyframe.Value)));
		return editorTimeline;
	}

	/// <summary>
	/// Runtime deform keys to per vertex offsets in the attachment's local setup space.
	/// The model has to be in setup pose.
	/// </summary>
	private DeformTimeline? DecompileDeformTimeline(RT.DeformTimeline timeline, TimelineContext context) {
		if (timeline.Resolve(context.Data) is not RT.VertexAttachment attachment || !context.Decompiled.TryGetValue(attachment, out var found)) {
			Logs.Warn($"Decompiler: deform timeline points at missing attachment '{timeline.Attachment}' (skin {timeline.Skin ?? "default"})");
			return null;
		}

		var vertices = attachment.GetVertices() ?? [];
		bool weighted = vertices.Any(x => x.Weights is { Length: > 0 });
		int count = Math.Min(vertices.Length, found.Vertices.Length);

		var editorTimeline = new DeformTimeline() {
			Slot = context.Slots[timeline.SlotIndex],
			Attachment = found.Attachment,
			Curve = timeline.Curve
		};

		// same pair order as the runtime
		for (int i = 0; i < count; i++) {
			if (!weighted) {
				editorTimeline.Vertices.Add(found.Vertices[i]);
				editorTimeline.Bones.Add(null);
				continue;
			}
			foreach (var weight in vertices[i].Weights ?? []) {
				editorTimeline.Vertices.Add(found.Vertices[i]);
				editorTimeline.Bones.Add(context.Bones[weight.Bone]);
			}
		}

		int pairs = editorTimeline.Vertices.Count;
		foreach (var offsets in timeline.Offsets) {
			float[] result = new float[pairs * 2];
			Array.Copy(offsets, result, Math.Min(offsets.Length, result.Length));
			editorTimeline.Offsets.Add(result);
		}

		return editorTimeline;
	}

	private DrawOrderTimeline DecompileDrawOrderTimeline(RT.DrawOrderTimeline timeline, List<EditorSlot> slots) {
		var editorTimeline = new DrawOrderTimeline();
		foreach (var keyframe in timeline.Curves[0].GetKeyframes()) {
			List<EditorSlot>? order = null;
			if (keyframe.Value != null) {
				order = [];
				foreach (var index in keyframe.Value)
					order.Add(slots[index]);
			}
			var key = editorTimeline.Keys.AddKeyframe(new(keyframe.Time, order));
			key.Interpolation = KeyframeInterpolation.Constant;
		}
		return editorTimeline;
	}

	private EventTimeline DecompileEventTimeline(RT.EventTimeline timeline, TimelineContext context) {
		var editorTimeline = new EventTimeline();
		foreach (var runtimeEvent in timeline.Events) {
			if (runtimeEvent.Data == null || !context.Events.TryGetValue(runtimeEvent.Data, out var editorEvent)) {
				Logs.Warn($"Decompiler: event key at {runtimeEvent.Time} has no event data, skipping");
				continue;
			}

			if (!editorTimeline.Keys.TryFindKeyframe(runtimeEvent.Time, out var key)) {
				key = editorTimeline.Keys.AddKeyframe(new(runtimeEvent.Time, []));
				key.Interpolation = KeyframeInterpolation.Constant;
			}
			key.Value!.Add(new EditorEventKey() {
				Event = editorEvent,
				Int = runtimeEvent.Int,
				Float = runtimeEvent.Float,
				String = runtimeEvent.String,
				Volume = runtimeEvent.Volume,
				Balance = runtimeEvent.Balance
			});
		}
		return editorTimeline;
	}

	private unsafe void ExtractImages(IRuntimeTextureAtlas atlas, HashSet<string> paths, string directory) {
		Dictionary<ITexture, (byte[] Pixels, int W, int H)> pages = [];

		foreach (var path in paths) {
			var region = atlas.GetRegion(path);
			if (region == null) {
				Logs.Warn($"Decompiler: atlas has no region '{path}'");
				continue;
			}

			var texture = region.GetTexture();
			if (!pages.TryGetValue(texture, out var page)) {
				if (!TryReadPixels(texture, out page.Pixels, out page.W, out page.H)) {
					Logs.Warn($"Decompiler: can't read pixels for region '{path}' (format {texture.GetFormat()}, cpu image {texture.HasCPUImage()})");
					continue;
				}
				pages[texture] = page;
			}

			region.GetBounds(out int regionX, out int regionY, out int regionW, out int regionH);
			region.GetOffsets(out int offsetX, out int offsetY, out int origW, out int origH);
			float rotation = region.GetRotation();
			bool flipped = texture.HasPublicFlags(PublicTextureFlags.RequiresFlippedV);
			bool premultiplied = region.GetPage().GetPreMultipliedAlpha();

			Image image = Image.GenImageColor(origW, origH, Color.Blank);
			byte* dst = (byte*)image.Data;
			int cropW = ((int)rotation % 180 == 90) ? regionH : regionW;
			int cropH = ((int)rotation % 180 == 90) ? regionW : regionH;

			// Rescaled atlases round offsets and sizes separately, keep the writes inside the image.
			int endY = Math.Min(offsetY + cropH, origH), endX = Math.Min(offsetX + cropW, origW);
			for (int py = Math.Max(offsetY, 0); py < endY; py++) {
				for (int px = Math.Max(offsetX, 0); px < endX; px++) {
					float u = (px + 0.5f) / origW;
					float v = 1 - (py + 0.5f) / origH;
					RT.AtlasUV.RemapMeshUV(regionX, regionY, regionW, regionH, rotation, offsetX, offsetY, origW, origH, page.W, page.H, u, v, out float atlasU, out float atlasV);

					int sourceX = Math.Clamp((int)(atlasU * page.W), 0, page.W - 1);
					int sourceY = Math.Clamp((int)(atlasV * page.H), 0, page.H - 1);
					if (flipped)
						sourceY = page.H - 1 - sourceY;

					int sourceIndex = (sourceY * page.W + sourceX) * 4;
					int destIndex = (py * origW + px) * 4;
					byte alpha = page.Pixels[sourceIndex + 3];
					for (int channel = 0; channel < 3; channel++) {
						byte value = page.Pixels[sourceIndex + channel];
						dst[destIndex + channel] = premultiplied && alpha > 0 ? (byte)Math.Min(255, value * 255 / alpha) : value;
					}
					dst[destIndex + 3] = alpha;
				}
			}

			var file = Path.Combine(directory, path.Replace('/', Path.DirectorySeparatorChar) + ".png");
			Directory.CreateDirectory(Path.GetDirectoryName(file)!);
			Raylib.ExportImage(image, file);
			Image.UnloadImage(ref image);
		}
	}

	private static unsafe bool TryReadPixels(ITexture texture, out byte[] pixels, out int w, out int h) {
		w = texture.GetWidth();
		h = texture.GetHeight();
		pixels = [];

		if (texture.HasCPUImage()) {
			var copy = Image.Copy(texture.GetCPUImage());
			if (copy.Format != ImageFormat.R8G8B8A8)
				Image.Reformat(ref copy, ImageFormat.R8G8B8A8);
			pixels = new Span<byte>(copy.Data, w * h * 4).ToArray();
			Image.UnloadImage(ref copy);
			return true;
		}

		uint handle = texture.GetTextureHandle();
		IRenderTexture? renderTexture = null;
		if (texture.GetFormat() != ImageFormat.R8G8B8A8) {
			// Compressed, draw it out to something we can read
			renderTexture = textures.CreateRenderTexture(w, h, 1, false);
			Graphics2D.BeginRenderTarget(renderTexture);
			Rlgl.SetBlendFactorsSeparate(GLEnum.ONE, GLEnum.ZERO, GLEnum.ONE, GLEnum.ZERO, GLEnum.FUNC_ADD, GLEnum.FUNC_ADD);
			Graphics2D.SetBlendMode(Raylib_cs.BlendMode.CustomSeparate);
			Graphics2D.SetDrawColor(255, 255, 255, 255);
			Graphics2D.SetTexture(texture);
			Graphics2D.DrawTexturedRectangle(0, 0, w, h, flipY: !texture.HasPublicFlags(PublicTextureFlags.RequiresFlippedV));
			Graphics2D.SetTexture(null);
			Graphics2D.EndRenderTarget(renderTexture);
			handle = renderTexture.GetTextureHandle();
		}

		byte* data = (byte*)Rlgl.ReadTexturePixels(handle, w, h, ImageFormat.R8G8B8A8);
		renderTexture?.Dispose();
		if (data == null)
			return false;

		pixels = new Span<byte>(data, w * h * 4).ToArray();
		Raylib.MemFree(data);
		return true;
	}
}
