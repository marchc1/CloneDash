// This *might* get separated into separate files per type soon, but for now, it's simpler
// to just have it all be in one place.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Nucleus.Commands;
using Nucleus.Common.Graphics;
using Nucleus.Common.Models;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Types;
using Nucleus.Util;

using Raylib_cs;

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Nucleus.Models.Runtime;

public enum M4S_StencilMode : byte
{
	Off = 0,
	On = 1,
	RenderMask = 2,
	Count
}

/// <summary>
/// Runtime for the 4th (and hopefully, last) major iteration of Nucleus's 2D model system.
/// </summary>
[Nucleus.MarkForStaticConstruction]
public static class Model4System
{
	/// <summary>
	/// Should be YYYY_MM_DD_MR (minor revision)
	/// <br/>
	/// Editor can use this too
	/// </summary>
	public const string MODEL_FORMAT_VERSION = "Nucleus Model4 2026.10.27.01";
	public const string MODEL_FORMAT_VERSION_NORMALIZED_HANDLES = "Nucleus Model4 2025.04.28.01";

	public static ConVar m4s_wireframe = new(nameof(m4s_wireframe), "0", FCvar.Saved, "Model4 instance wireframe overlay.", 0, 1);
	public static ConVar m4s_stencilmode = new(nameof(m4s_stencilmode), "1", FCvar.Saved, "Controls stencil rendering.", 0, ((int)M4S_StencilMode.Count) - 1);
	public const double REFERENCE_FPS = 30;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T? SearchReturnItem<T>(List<T> list, ReadOnlySpan<char> name) where T : class, IModel4Nameable {
		Span<T> items = list.AsSpan();
		T? item = null;
		for (int i = 0, c = items.Length; i < c; i++)
			if ((item = items[i]) != null && item.Name.Equals(name, StringComparison.InvariantCulture))
				return item;

		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int SearchReturnIndex<T>(List<T> list, ReadOnlySpan<char> name) where T : class, IModel4Nameable {
		Span<T> items = list.AsSpan();
		for (int i = 0, c = items.Length; i < c; i++)
			if (items[i] != null && items[i].Name.Equals(name, StringComparison.InvariantCulture))
				return i;

		return -1;
	}

	internal static Color RenderBlend = new(255, 255, 255, 255);

	public static ref readonly Color GetRenderBlend() => ref RenderBlend;

	private static readonly Stack<Color> RenderBlendQueue = new();

	public static void PushRenderBlend(in Color c) {
		RenderBlendQueue.Push(RenderBlend);
		RenderBlend = RenderBlend * c;
	}

	public static void PopRenderBlend() {
		RenderBlend = RenderBlendQueue.Count > 0
			? RenderBlendQueue.Pop()
			: Color.White;
	}

	private static uint activeTexture;

	internal static void NotifyNewModelRender() {
		activeTexture = 0;
	}

	internal static void CheckTextureUpdate(ITexture tex) {
		var textureIdx = tex.GetTextureHandle();
		if (textureIdx != activeTexture) {
			Rlgl.DrawRenderBatchActive();
			activeTexture = textureIdx;
		}
	}

	internal static void SetTexture(ITexture tex) {
		Rlgl.SetTexture(tex.GetTextureHandle());
	}

	/// <summary>
	/// False while a model renders under someone else's shader, tint black needs its own
	/// </summary>
	internal static bool DefaultShader = true;

	// Tint black: rgb = (tex.a - tex.rgb) * dark + tex.rgb * light, premultiplied
	const string TINT_BLACK_FS = """
		#version 330
		in vec2 fragTexCoord;
		in vec4 fragColor;
		uniform sampler2D texture0;
		uniform vec4 colDiffuse;
		uniform vec3 darkColor;
		uniform int straightTexture;
		out vec4 finalColor;
		void main() {
			vec4 tex = texture(texture0, fragTexCoord);
			vec4 light = fragColor * colDiffuse;
			if (straightTexture == 1) {
				tex.rgb *= tex.a;
				light.rgb *= light.a;
			}
			float a = tex.a * light.a;
			vec3 rgb = (tex.a - tex.rgb) * darkColor * light.a + tex.rgb * light.rgb;
			if (straightTexture == 1) rgb = a > 0.0 ? rgb / a : vec3(0.0);
			finalColor = vec4(rgb, a);
		}
		""";
	private static Shader tintBlackShader;
	private static bool tintBlackLoaded;
	private static int tintDarkLoc;
	private static int tintStraightTextureLoc;

	/// <summary>
	/// Switches to the tint black shader if the slot has a dark color.
	/// </summary>
	/// <returns>true if it did, call <see cref="EndTintBlack"/> after</returns>
	internal static bool BeginTintBlack(SlotInstance slot, bool premultipliedAlpha) {
		if (slot.DarkColor is not { } dark || !DefaultShader || !slot.Bone.Model.Data.RenderTintBlack) return false;
		if (!tintBlackLoaded) {
			tintBlackShader = Raylib.LoadShaderFromMemory(null, TINT_BLACK_FS);
			tintDarkLoc = Raylib.GetShaderLocation(tintBlackShader, "darkColor");
			tintStraightTextureLoc = Raylib.GetShaderLocation(tintBlackShader, "straightTexture");
			tintBlackLoaded = true;
		}

		Rlgl.DrawRenderBatchActive();
		Raylib.BeginShaderMode(tintBlackShader);
		Raylib.SetShaderValue(tintBlackShader, tintDarkLoc, new System.Numerics.Vector3(dark.R / 255f, dark.G / 255f, dark.B / 255f), ShaderUniformDataType.SHADER_UNIFORM_VEC3);
		Raylib.SetShaderValue(tintBlackShader, tintStraightTextureLoc, premultipliedAlpha ? 0 : 1, ShaderUniformDataType.SHADER_UNIFORM_INT);
		return true;
	}

	internal static void EndTintBlack() {
		Rlgl.DrawRenderBatchActive();
		Raylib.EndShaderMode();
	}

	internal static void PrepareWireframeRendering() {
		Rlgl.DrawRenderBatchActive();
		Rlgl.SetLineWidth(1);
		Rlgl.SetTexture(0);
		Rlgl.Begin(DrawMode.LINES);
	}

	internal static void EndWireframeRendering() {
		Rlgl.End();
		Rlgl.DrawRenderBatchActive();
		Rlgl.SetTexture(activeTexture);
	}
}

public interface IModel4Nameable
{
	public string Name { get; }
}

public interface IContainsSetupPose
{
	public void SetToSetupPose();
}

public interface IModelInstanceObject
{
	public ModelInstance GetModel();
}

public class ModelData : IDisposable, IModelInterface<BoneData, SlotData>, IModel4Nameable
{
	private bool disposedValue;

	/// <summary>
	/// Model format (matches the model format that the editor compiled)
	/// </summary>
	public string FormatVersion { get; set; }

	/// <summary>
	/// Model name
	/// </summary>
	public string? Name { get; set; } = null;

	public List<BoneData> BoneDatas { get; set; } = [];
	public List<SlotData> SlotDatas { get; set; } = [];
	public List<Skin> Skins { get; set; } = [];
	public Skin DefaultSkin { get; set; }
	public List<Animation> Animations { get; set; } = [];
	public List<LinkedMesh> LinkedMeshes { get; set; } = [];
	public List<IkConstraintData> IkConstraints { get; set; } = [];
	public List<TransformConstraintData> TransformConstraints { get; set; } = [];
	public List<PathConstraintData> PathConstraints { get; set; } = [];
	public List<EventData> Events { get; set; } = [];

	[JsonIgnore] public IRuntimeTextureAtlas TextureAtlas { get; set; }

	/// <summary>
	/// Draw slot dark colors (tint black), off when the source drew without them. Not saved in .nm4b
	/// </summary>
	[JsonIgnore] public bool RenderTintBlack { get; set; } = true;

	// Internal lookup tables (holding off for now)
	// [JsonIgnore] readonly UtlSymbolTableMT Symbols = new();
	// [JsonIgnore] readonly Dictionary<UtlSymId_t, BoneData> SymbolToBoneData = [];
	// [JsonIgnore] readonly Dictionary<UtlSymId_t, SlotData> SymbolToSlotData = [];
	// [JsonIgnore] readonly Dictionary<UtlSymId_t, Skin> SymbolToSkin = [];
	// [JsonIgnore] readonly Dictionary<UtlSymId_t, Animation> SymbolToAnimation = [];

	/// <summary>
	/// Creates a runtime model instance based off the ModelData.
	/// </summary>
	/// <returns></returns>
	public ModelInstance Instantiate() {
		ModelInstance instance = new ModelInstance();
		instance.Data = this;

		Dictionary<BoneData, BoneInstance> bone_data2instance = [];
		foreach (var boneData in BoneDatas) {
			var bone = new BoneInstance();
			bone.Data = boneData;
			bone.Model = instance;
			bone_data2instance[boneData] = bone;

			// Parent setup. If boneData.Parent is not null, try get from bone_data2instance table
			bone.Parent = boneData.Parent == null ? null : bone_data2instance.TryGetValue(boneData.Parent, out var instanceParent) ? instanceParent : null;
			bone.Parent?.Children.Add(bone);

			instance.Bones.Add(bone);
		}

		foreach (var slotData in SlotDatas) {
			var slot = new SlotInstance();
			slot.Data = slotData;
			slot.Model = instance;
			slot.Bone = bone_data2instance[slotData.BoneData];

			instance.Slots.Add(slot);
		}

		foreach (var data in IkConstraints)
			instance.IkConstraints.Add(new() {
				Data = data,
				Bones = BoneInstances(data.Bones, bone_data2instance),
				Target = bone_data2instance[data.Target]
			});
		foreach (var data in TransformConstraints)
			instance.TransformConstraints.Add(new() {
				Data = data,
				Bones = BoneInstances(data.Bones, bone_data2instance),
				Target = bone_data2instance[data.Target]
			});
		foreach (var data in PathConstraints)
			instance.PathConstraints.Add(new() {
				Data = data,
				Bones = BoneInstances(data.Bones, bone_data2instance),
				Target = instance.Slots[data.Target.Index]
			});

		instance.SetToSetupPose();
		instance.UpdateCache();

		return instance;
	}

	private static List<BoneInstance> BoneInstances(IEnumerable<BoneData> bones, Dictionary<BoneData, BoneInstance> instances) {
		var result = new List<BoneInstance>();
		foreach (var boneData in bones)
			result.Add(instances[boneData]);
		return result;
	}

	public EventData? FindEvent(ReadOnlySpan<char> name) => Model4System.SearchReturnItem(Events, name);
	public IkConstraintData? FindIkConstraint(ReadOnlySpan<char> name) =>
		Model4System.SearchReturnItem(IkConstraints, name);
	public TransformConstraintData? FindTransformConstraint(ReadOnlySpan<char> name) =>
		Model4System.SearchReturnItem(TransformConstraints, name);
	public PathConstraintData? FindPathConstraint(ReadOnlySpan<char> name) =>
		Model4System.SearchReturnItem(PathConstraints, name);

	public void ResolveLinkedMeshes() {
		foreach (var skin in Skins) {
			foreach (var entry in skin.Attachments) {
				if (entry.Value is not MeshAttachment { ParentKey: string parentKey } mesh) continue;
				var parentSkin = mesh.ParentSkin == null ? DefaultSkin : FindSkin(mesh.ParentSkin);
				mesh.ParentMesh = parentSkin?.GetAttachment(entry.Key.SlotIndex, parentKey) as MeshAttachment;
			}
		}
	}

	// May use this later
	// T? FindFn<T>(ReadOnlySpan<char> name, Dictionary<UtlSymId_t, T> lookups, List<T> contiguous) where T : IModel4Nameable {
	// 	UtlSymId_t symbol = Symbols.Find(name);
	// 	if (symbol == UtlSymbol.UTL_INVAL_SYMBOL || !lookups.TryGetValue(symbol, out T? value)) {
	// 		symbol = Symbols.AddString(name);
	// 		string str = Symbols.String(symbol)!;
	// 		value = contiguous.FirstOrDefault(x => str.Equals(x.Name));
	// 		if (value != null)
	// 			lookups[symbol] = value;
	// 	}
	//
	// 	return value;
	// }

	public Animation? FindAnimation(ReadOnlySpan<char> name) => Model4System.SearchReturnItem(Animations, name);

	public BoneData? FindBone(ReadOnlySpan<char> name) => Model4System.SearchReturnItem(BoneDatas, name);

	public Skin? FindSkin(ReadOnlySpan<char> name) => Model4System.SearchReturnItem(Skins, name);

	public SlotData? FindSlot(ReadOnlySpan<char> name) => Model4System.SearchReturnItem(SlotDatas, name);

	public int FindBoneIndex(ReadOnlySpan<char> name) => Model4System.SearchReturnIndex(BoneDatas, name);

	public int FindSlotIndex(ReadOnlySpan<char> name) => Model4System.SearchReturnIndex(SlotDatas, name);

	protected virtual void Dispose(bool usercall) {
		if (disposedValue) return;

		BoneDatas.Clear();
		Animations.Clear();
		Skins.Clear();
		SlotDatas.Clear();
#nullable disable
		BoneDatas = null;
		Animations = null;
		Skins = null;
		SlotDatas = null;
#nullable enable

		if (usercall)
			TextureAtlas?.Dispose();

		disposedValue = true;
	}

	public void SetupAttachments() {
		ResolveLinkedMeshes();
		foreach (var skin in Skins) {
			foreach (var attachment in skin.Attachments) {
				attachment.Value.Setup(this);
			}
		}
	}

	// TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	~ModelData() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(usercall: false);
	}

	public void Dispose() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(usercall: true);
		GC.SuppressFinalize(this);
	}
}

public class RuntimeClipper(ModelInstance model) : ModelClipper<ModelInstance, BoneInstance, SlotInstance, ClippingAttachment>(model, true);

public class ModelInstance : IContainsSetupPose, IModelInterface<BoneInstance, SlotInstance>
{
	public ModelData Data { get; set; }
	public List<BoneInstance> Bones { get; set; } = [];
	public List<SlotInstance> Slots { get; set; } = [];
	public List<SlotInstance> DrawOrder { get; set; } = [];
	public List<IkConstraintInstance> IkConstraints { get; set; } = [];
	public List<TransformConstraintInstance> TransformConstraints { get; set; } = [];
	public List<PathConstraintInstance> PathConstraints { get; set; } = [];

	public RuntimeClipper Clipping;

	public BoneInstance? RootBone => Bones.Count > 0 ? Bones[0] : null;

	public Skin Skin { get; set; }
	public Vector2F Position { get; set; }
	public Vector2F Scale { get; set; } = new(1, 1);
	public IRuntimeTextureAtlas TextureAtlas => Data.TextureAtlas;

	private Transformation worldTransform;

	public Transformation WorldTransform {
		get => worldTransform;
	}

	public ModelInstance() {
		Clipping = new(this);
	}

	public void Render(bool useDefaultShader = true) {
		var offset = Graphics2D.Offset;
		Model4System.DefaultShader = useDefaultShader;
		Model4System.NotifyNewModelRender();
		Graphics2D.ResetDrawingOffset();
		Rlgl.DrawRenderBatchActive();
		Rlgl.DisableBackfaceCulling();

		Rlgl.PushMatrix();
		try {
			Rlgl.Translatef(Position.X, Position.Y, 0);
			Rlgl.Scalef(Scale.X, Scale.Y, 1);

			UpdateWorldTransform();

			foreach (var slot in DrawOrder) {
				var attachment = slot.Bone.Active ? slot.Attachment : null;
				if (attachment == null) {
					Clipping.NextSlot(slot);
					continue;
				}
				try {
					attachment.Render(slot);
				}
				finally {
					Clipping.NextSlot(slot);
					slot.EndBlendMode();
				}
			}

			/*int index = 0;
			foreach (var bone in Bones) {
				var test = bone.LocalToWorld(0, 0);
				Raylib.DrawCircleV(new(test.X, -test.Y), 4, Color.Red);
				Graphics2D.DrawText(new(test.X, -test.Y), $"[{index}] {bone.Name}", "Consolas", 48);
				index++;
			}*/

			Clipping.End();
		}
		finally {
			Rlgl.PopMatrix();
		}
		Graphics2D.OffsetDrawing(offset);

		if (Model4System.m4s_wireframe.GetInt() >= 2) {
			foreach (var bone in Bones) {
				Raylib.DrawCircleV(bone.WorldTransform.LocalToWorld(0, 0).ToNumerics() * new System.Numerics.Vector2(1, -1), 4, Color.Red);
			}
		}

		Rlgl.DrawRenderBatchActive();
	}

	public void SetToSetupPose() {
		SetBonesToSetupPose();
		SetSlotsToSetupPose();
	}

	readonly List<IModelUpdatable> updateCache = [];
	readonly List<BoneInstance> updateCacheReset = [];
	bool cacheDirty = true;

	/// <summary>
	/// Rebuilds the bone/constraint update order
	/// <br/><b>NOTE:</b> Call after changing constraints, timelines skip whatever this marks inactive.
	/// </summary>
	public void InvalidateUpdateCache() => UpdateCache();

	/// <summary>
	/// Poses every bone and runs the constraints
	/// </summary>
	public void UpdateWorldTransform() {
		if (cacheDirty)
			UpdateCache();

		foreach (var bone in updateCacheReset) {
			bone.Applied = new() {
				X = bone.Position.X, Y = bone.Position.Y,
				Rotation = bone.Rotation,
				ScaleX = bone.Scale.X, ScaleY = bone.Scale.Y,
				ShearX = bone.Shear.X, ShearY = bone.Shear.Y,
				Valid = true
			};
		}
		foreach (var updatable in updateCache)
			updatable.Update();
	}

	public void UpdateCache() {
		cacheDirty = false;
		updateCache.Clear();
		updateCacheReset.Clear();

		foreach (var bone in Bones) {
			bone.Sorted = bone.Data.SkinRequired;
			bone.Active = !bone.Sorted;
		}
		// Merged default skins (Muse Dash) can hold bones and constraints
		var skin = Skin ?? Data.DefaultSkin;
		if (skin != null) {
			foreach (var boneData in skin.Bones) {
				BoneInstance? bone = Bones[boneData.Index];
				do {
					bone.Sorted = false;
					bone.Active = true;
					bone = bone.Parent;
				} while (bone != null);
			}
		}

		int constraintCount = IkConstraints.Count + TransformConstraints.Count + PathConstraints.Count;
		for (int order = 0; order < constraintCount; order++) {
			foreach (var ik in IkConstraints) {
				if (ik.Data.Order == order) {
					SortIk(ik);
					goto next;
				}
			}
			foreach (var transform in TransformConstraints) {
				if (transform.Data.Order == order) {
					SortTransform(transform);
					goto next;
				}
			}
			foreach (var path in PathConstraints) {
				if (path.Data.Order == order) {
					SortPath(path);
					goto next;
				}
			}
			next:;
		}

		foreach (var bone in Bones)
			SortBone(bone);
	}

	private bool SkinHas(ConstraintData data) =>
		!data.SkinRequired || (Skin ?? Data.DefaultSkin)?.Constraints.Contains(data) == true;

	private void SortIk(IkConstraintInstance constraint) {
		constraint.Active = constraint.Target.Active && SkinHas(constraint.Data);
		if (!constraint.Active) return;

		SortBone(constraint.Target);
		var constrained = constraint.Bones;
		var parent = constrained[0];
		SortBone(parent);
		if (constrained.Count > 1) {
			var child = constrained[constrained.Count - 1];
			if (!updateCache.Contains(child))
				updateCacheReset.Add(child);
		}
		updateCache.Add(constraint);
		SortReset(parent.Children);
		constrained[constrained.Count - 1].Sorted = true;
	}

	private void SortTransform(TransformConstraintInstance constraint) {
		constraint.Active = constraint.Target.Active && SkinHas(constraint.Data);
		if (!constraint.Active) return;

		SortBone(constraint.Target);
		var constrained = constraint.Bones;
		if (constraint.Data.Local) {
			foreach (var child in constrained) {
				if (child.Parent != null)
					SortBone(child.Parent);
				if (!updateCache.Contains(child))
					updateCacheReset.Add(child);
			}
		}
		else {
			foreach (var bone in constrained)
				SortBone(bone);
		}
		updateCache.Add(constraint);
		foreach (var bone in constrained)
			SortReset(bone.Children);
		foreach (var bone in constrained)
			bone.Sorted = true;
	}

	private void SortPath(PathConstraintInstance constraint) {
		constraint.Active = constraint.Target.Bone.Active && SkinHas(constraint.Data);
		if (!constraint.Active) return;

		var slot = constraint.Target;
		int slotIndex = slot.Index;
		var slotBone = slot.Bone;
		if (Skin != null)
			SortPathAttachments(Skin, slotIndex, slotBone);
		if (Data.DefaultSkin != null && Data.DefaultSkin != Skin)
			SortPathAttachments(Data.DefaultSkin, slotIndex, slotBone);
		foreach (var skin in Data.Skins)
			SortPathAttachments(skin, slotIndex, slotBone);
		SortPathAttachment(slot.Attachment, slotBone);

		var constrained = constraint.Bones;
		foreach (var bone in constrained)
			SortBone(bone);
		updateCache.Add(constraint);
		foreach (var bone in constrained)
			SortReset(bone.Children);
		foreach (var bone in constrained)
			bone.Sorted = true;
	}

	private void SortPathAttachments(Skin skin, int slotIndex, BoneInstance slotBone) {
		foreach (var entry in skin.Attachments) {
			if (entry.Key.SlotIndex == slotIndex)
				SortPathAttachment(entry.Value, slotBone);
		}
	}

	private void SortPathAttachment(Attachment? attachment, BoneInstance slotBone) {
		if (attachment is not PathAttachment path) return;
		bool weighted = false;
		foreach (var vertex in path.GetVertices()) {
			if (vertex.Weights == null || vertex.Weights.Length == 0) continue;
			weighted = true;
			foreach (var weight in vertex.Weights)
				SortBone(Bones[weight.Bone]);
		}
		if (!weighted)
			SortBone(slotBone);
	}

	private void SortBone(BoneInstance bone) {
		if (bone.Sorted) return;
		if (bone.Parent != null)
			SortBone(bone.Parent);
		bone.Sorted = true;
		updateCache.Add(bone);
	}

	private static void SortReset(List<BoneInstance> bones) {
		foreach (var bone in bones) {
			if (!bone.Active) continue;
			if (bone.Sorted)
				SortReset(bone.Children);
			bone.Sorted = false;
		}
	}

	public void SetConstraintsToSetupPose() {
		foreach (var ik in IkConstraints)
			ik.SetToSetupPose();
		foreach (var transform in TransformConstraints)
			transform.SetToSetupPose();
		foreach (var path in PathConstraints)
			path.SetToSetupPose();
	}

	public IkConstraintInstance? FindIkConstraint(ReadOnlySpan<char> name) =>
		Model4System.SearchReturnItem(IkConstraints, name);
	public TransformConstraintInstance? FindTransformConstraint(ReadOnlySpan<char> name) =>
		Model4System.SearchReturnItem(TransformConstraints, name);
	public PathConstraintInstance? FindPathConstraint(ReadOnlySpan<char> name) =>
		Model4System.SearchReturnItem(PathConstraints, name);

	public BoneInstance? FindBone(ReadOnlySpan<char> name) => Model4System.SearchReturnItem(Bones, name);

	public SlotInstance? FindSlot(ReadOnlySpan<char> name) => Model4System.SearchReturnItem(Slots, name);

	public int FindBoneIndex(ReadOnlySpan<char> name) => Model4System.SearchReturnIndex(Bones, name);

	public int FindSlotIndex(ReadOnlySpan<char> name) => Model4System.SearchReturnIndex(Slots, name);

	public Attachment? GetAttachment(int slot, ReadOnlySpan<char> name) {
		Attachment? attachment;
		if (Skin?.TryGetAttachment(slot, name, out attachment) ?? false)
			return attachment;

		if (Data.DefaultSkin?.TryGetAttachment(slot, name, out attachment) ?? false)
			return attachment;

		return null;
	}

	public Attachment? GetAttachment(ReadOnlySpan<char> slotName, ReadOnlySpan<char> attachmentName) => GetAttachment(Data.FindSlotIndex(slotName), attachmentName);

	public void SetBonesToSetupPose() {
		foreach (var bone in Bones)
			bone.SetToSetupPose();
		SetConstraintsToSetupPose();
	}

	public void SetSlotsToSetupPose() {
		ResetDrawOrderToSetup();

		foreach (var slot in Slots)
			slot.SetToSetupPose();
	}

	public void ResetDrawOrderToSetup() {
		DrawOrder.Clear();

		foreach (var slot in Slots)
			DrawOrder.Add(slot);
	}

	public void SetSkin(string skinName) => SetSkin(Data.FindSkin(skinName));

	public void SetSkin(Skin? skin) {
		Skin = skin;
		SetSlotsToSetupPose();
		UpdateCache();
	}
}

public enum MixBlendMode
{
	Setup,
	First,
	Replace,
	Add
}

public enum MixDirection
{
	In,
	Out
}

public class BoneData : IModel4Nameable
{
	public string Name { get; set; } = "";
	public int Index { get; set; }
	public BoneData? Parent { get; set; }
	public float Length { get; set; }

	public TransformMode TransformMode { get; set; }
	public Vector2F Position { get; set; }
	public float Rotation { get; set; }
	public Vector2F Scale { get; set; }
	public Vector2F Shear { get; set; }
	/// <summary>
	/// Only active when the skin lists it (or a child of it)
	/// </summary>
	public bool SkinRequired { get; set; }
}

public class BoneInstance : IContainsSetupPose, IModelInstanceObject, IModel4Nameable, IConstraintBone, IModelUpdatable
{
	public ModelInstance GetModel() => Model;

	public ModelInstance Model;

	public string Name => Data.Name;
	public BoneData Data;

	public BoneInstance? Parent;
	public List<BoneInstance> Children = [];
	public Transformation WorldTransform;
	public TransformMode TransformMode;

	public Vector2F Position;
	public float Rotation;
	public Vector2F Scale;
	public Vector2F Shear;

	/// <summary>
	/// The local transform the world transform was last built from, constraints change it
	/// </summary>
	public BoneApplied Applied;
	/// <summary>
	/// False when a skin-required bone isn't in the current skin
	/// </summary>
	public bool Active = true;
	internal bool Sorted;

	IConstraintBone? IConstraintBone.ParentBone => Parent;
	float IConstraintBone.BoneLength => Data.Length;
	TransformMode IConstraintBone.BoneTransformMode => TransformMode;
	ref BoneApplied IConstraintBone.Applied => ref Applied;
	ref Transformation IConstraintBone.World => ref WorldTransform;

	public void Update() => UpdateWorldTransform();

	public void UpdateWorldTransform(float x, float y, float rotation, float scaleX, float scaleY, float shearX, float shearY)
		=> UpdateWorldTransform(new Vector2F(x, y), rotation, new Vector2F(scaleX, scaleY), new Vector2F(shearX, shearY));

	public void SetToSetupPose() {
		Position = Data.Position;
		Rotation = Data.Rotation;
		Scale = Data.Scale;
		Shear = Data.Shear;

		TransformMode = Data.TransformMode;

		UpdateWorldTransform();
	}

	public Vector2F WorldToLocal(float x, float y) => WorldTransform.WorldToLocal(x, y);

	public Vector2F WorldToLocal(in Vector2F xy) => WorldTransform.WorldToLocal(in xy);

	public float WorldToLocalRotation(float rot) => WorldTransform.WorldToLocalRotation(rot);

	public Vector2F LocalToWorld(float x, float y) => WorldTransform.LocalToWorld(x, y);

	public Vector2F LocalToWorld(in Vector2F xy) => WorldTransform.LocalToWorld(in xy);

	public float LocalToWorldRotation(float rot) => WorldTransform.LocalToWorldRotation(rot);

	public void UpdateWorldTransform() => UpdateWorldTransform(in Position, Rotation, in Scale, in Shear);

	public void UpdateWorldTransform(in Vector2F pos, float rot, in Vector2F scale, in Vector2F shear) {
		Applied = new() {
			X = pos.X, Y = pos.Y,
			Rotation = rot,
			ScaleX = scale.X, ScaleY = scale.Y,
			ShearX = shear.X, ShearY = shear.Y,
			Valid = true
		};
		WorldTransform = Transformation.CalculateWorldTransformation(in pos, rot, in scale, in shear, TransformMode, Parent?.WorldTransform ?? null);
	}
}

public class SlotData : IModel4Nameable
{
	public int Index { get; set; }
	public string Name { get; set; } = "";
	public BoneData BoneData;
	public Color Color;

	// not yet implemented
	public Color? DarkColor;

	public string? Attachment;
	public BlendMode BlendMode;
}

public class SlotInstance : IContainsSetupPose, IModel4Nameable
{
	public ModelInstance GetModel() => Model;

	public ModelInstance Model { get; set; }

	public string Name => Data.Name;
	public int Index => Data.Index;
	public SlotData Data;

	Attachment? attachment;
	/// <summary>
	/// Setting a different attachment clears the deform, unless both deform the same vertices
	/// </summary>
	public Attachment? Attachment {
		get => attachment;
		set {
			if (attachment == value) return;
			if (value is not VertexAttachment next || attachment is not VertexAttachment current || next.DeformAttachment != current.DeformAttachment)
				DeformCount = 0;
			attachment = value;
		}
	}
	public BoneInstance Bone;
	public Color Color;
	public Color? DarkColor;

	/// <summary>
	/// Vertex offsets from deform timelines, only the first <see cref="DeformCount"/> are used
	/// </summary>
	public float[] Deform = [];
	public int DeformCount;

	public float[] EnsureDeform(int count) {
		if (Deform.Length < count)
			Array.Resize(ref Deform, count);
		if (DeformCount < count)
			Array.Clear(Deform, DeformCount, count - DeformCount);
		DeformCount = count;
		return Deform;
	}

	public byte R => Color.R;
	public byte G => Color.G;
	public byte B => Color.B;
	public byte A => Color.A;

	public BlendMode BlendMode { get; set; }

	public void SetToSetupPose() {
		Color = Data.Color;
		DarkColor = Data.DarkColor;
		BlendMode = Data.BlendMode;
		Attachment = Data.Attachment == null ? null : Bone.Model.GetAttachment(Index, Data.Attachment);
		DeformCount = 0;
	}

	public void SetAttachment(string? value) => Attachment = value == null ? null : Model.GetAttachment(Index, value);

	/// <summary>
	/// Sets the blend factors for an attachment.
	/// Premultiplied pages need <see cref="VertexColor"/> too.
	/// </summary>
	public void StartBlendModeFor(Attachment attachment, bool premultipliedAlpha) {
		Raylib.BeginBlendMode(BlendMode switch {
			BlendMode.Normal => premultipliedAlpha ? Raylib_cs.BlendMode.AlphaPremultiply : Raylib_cs.BlendMode.Alpha,
			BlendMode.Additive => premultipliedAlpha ? Raylib_cs.BlendMode.AddColors : Raylib_cs.BlendMode.Additive,
			BlendMode.Multiply => Raylib_cs.BlendMode.Multiplied,
			// todo: screen should be ONE, ONE_MINUS_SRC_COLOR, drawn like normal for now
			BlendMode.Screen => premultipliedAlpha ? Raylib_cs.BlendMode.AlphaPremultiply : Raylib_cs.BlendMode.Alpha,
			_ => throw new Exception($"Unsupported blend mode! (got {BlendMode})")
		});
	}

	/// <summary>
	/// Slot color times the attachment's, premultiplied for premultiplied pages
	/// </summary>
	public (float R, float G, float B, float A) VertexColor(Color attachmentColor, bool premultipliedAlpha) {
		var color = Color * Model4System.GetRenderBlend();
		float r = color.R / 255f * attachmentColor.R / 255f;
		float g = color.G / 255f * attachmentColor.G / 255f;
		float b = color.B / 255f * attachmentColor.B / 255f;
		float a = color.A / 255f * attachmentColor.A / 255f;
		return premultipliedAlpha ? (r * a, g * a, b * a, a) : (r, g, b, a);
	}

	public void EndBlendMode() {
		Raylib.EndBlendMode();
	}
}

public class SkinEntryTypeConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
		return sourceType == typeof(string);
	}

	// Expect format: "SkinEntry { Name = hand_L_1, SlotIndex = 0 }"
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
		if (value is string s) {
			var match = Regex.Match(s, @"SkinEntry \{ Name = (.*?), SlotIndex = (\d+) \}");
			if (match.Success) {
				return new SkinEntry(match.Groups[1].Value.Trim(), int.Parse(match.Groups[2].Value));
			}
		}
		throw new NotSupportedException($"Cannot convert '{value}' to SkinEntry.");
	}
}

/// <summary>
/// A record where <see cref="SlotIndex"/> refers to <see cref="SlotData.Index"/> and <see cref="Name"/> refers to the attachment name in that slot.
/// This allows for specific attachments to be swapped out by a skin. It is not currently implemented in the editor, and needs to be considered before
/// engine-restructure merge
/// </summary>
/// <param name="Name"></param>
/// <param name="SlotIndex"></param>
[TypeConverter(typeof(SkinEntryTypeConverter))] public record struct SkinEntry(UtlSymbol Name, int SlotIndex);
// iirc, records handle this automatically; saving in case they don't
// public override int GetHashCode() => HashCode.Combine(Name, SlotIndex);

public class Skin : IModel4Nameable
{
	public string Name { get; set; }
	public Dictionary<SkinEntry, Attachment> Attachments { get; set; } = [];
	public List<BoneData> Bones { get; set; } = [];
	public List<ConstraintData> Constraints { get; set; } = [];

	public void AddSkin(Skin skin) {
		foreach (var attachment in skin.Attachments) Attachments.TryAdd(attachment.Key, attachment.Value);
		foreach (var bone in skin.Bones) if (!Bones.Contains(bone)) Bones.Add(bone);
		foreach (var constraint in skin.Constraints) {
			if (!Constraints.Contains(constraint))
				Constraints.Add(constraint);
		}
	}

	public void Clear() {
		Attachments.Clear();
		Bones.Clear();
		Constraints.Clear();
	}

	public Attachment? GetAttachment(int slot, ReadOnlySpan<char> name) {
		if (Attachments.TryGetValue(new(name, slot), out Attachment? attachment))
			return attachment;
		return null;
	}

	public bool TryGetAttachment(int slot, ReadOnlySpan<char> name, [NotNullWhen(true)] out Attachment? attachment) {
		return Attachments.TryGetValue(new(name, slot), out attachment);
	}

	public bool GetAttachments(int slot, List<SkinEntry> attachments) {
		var lastCount = attachments.Count;

		foreach (var attachment in Attachments)
			if (attachment.Key.SlotIndex == slot)
				attachments.Add(attachment.Key);

		return attachments.Count > lastCount;
	}

	public void SetAttachment(int slot, ReadOnlySpan<char> name, Attachment attachment) {
		Attachments[new(name, slot)] = attachment;
	}
}

public class Animation : IModel4Nameable
{
	public double Duration { get; set; }
	public string Name { get; set; }
	public List<Timeline> Timelines { get; set; } = [];

	public void Apply(ModelInstance? model, double time, float mix = 1, MixBlendMode mixBlend = MixBlendMode.Setup)
		=> Apply(model, -1, time, false, null, mix, mixBlend);

	/// <summary>
	/// Collects events between lastTime and time into events
	/// </summary>
	public void Apply(ModelInstance? model, double lastTime, double time, bool loop, List<ModelEvent>? events, float mix = 1, MixBlendMode mixBlend = MixBlendMode.Setup) {
		if (model == null)
			return;

		if (loop && Duration != 0) {
			time %= Duration;
			if (lastTime > 0)
				lastTime %= Duration;
		}

		foreach (var tl in Timelines) {
			tl.Apply(model, lastTime, time, mix, mixBlend, MixDirection.In);
			if (events != null)
				tl.CollectEvents(lastTime, time, events);
		}
	}
}

public abstract class Attachment : IModel4Nameable
{
	public string Name { get; set; }

	public virtual void Render(SlotInstance slot) {
	}

	public virtual void Setup(ModelData data) {
	}

	public virtual byte Alpha => 255;
}

public class RegionAttachment : Attachment
{
	public Vector2F Position;
	public Vector2F Scale;
	public float Rotation;
	public Color Color = Color.White;

	public string Path;
	[JsonIgnore] public IModelAtlasRegion? Region;
	[JsonIgnore] public bool InitializedRegion;

	public override byte Alpha => Color.A;

	public override void Setup(ModelData data) {
		Region = data.TextureAtlas.GetRegion(Path);
		Debug.Assert(Region != null, "TextureAtlas couldn't find the texture region!");
	}

	public override void Render(SlotInstance slot) {
		var bone = slot.Bone;
		var worldTransform = Transformation.CalculateWorldTransformation(Position, Rotation, Scale, Vector2F.Zero, TransformMode.Normal, slot.Bone.WorldTransform);

		var region = Region;
		if (region == null) {
			Setup(slot.Bone.Model.Data);
			if (Region == null) return;
			region = Region;
		}

		ITexture tex = region.GetTexture();
		slot.StartBlendModeFor(this, region.GetPage().GetPreMultipliedAlpha());
		region.GetBounds(out int rx, out int ry, out int rw, out int rh);
		region.GetOffsets(out int offsetX, out int offsetY, out int origW, out int origH);
		float rotation = region.GetRotation();

		int deg = (int)rotation;
		float cropW = (deg % 180 == 90) ? rh : rw;
		float cropH = (deg % 180 == 90) ? rw : rh;
		float width = cropH, height = cropW;
		float widthDiv2 = width / 2, heightDiv2 = height / 2;

		float localOffX = (offsetX + cropW * 0.5f) - origW * 0.5f;
		float localOffY = origH * 0.5f - offsetY - cropH * 0.5f;

		Vector2F TL, TR, BR, BL;
		{
			float wA = worldTransform.A, wB = worldTransform.B, wC = worldTransform.C, wD = worldTransform.D;
			float wX = worldTransform.X, wY = worldTransform.Y;

			float x0 = -heightDiv2 + localOffX;
			float x1 = heightDiv2 + localOffX;
			float y0 = widthDiv2 + localOffY;
			float y1 = -widthDiv2 + localOffY;

			// precompute shared
			float ax0 = x0 * wA, ax1 = x1 * wA;
			float by0 = y0 * wB, by1 = y1 * wB;
			float cx0 = x0 * wC, cx1 = x1 * wC;
			float dy0 = y0 * wD, dy1 = y1 * wD;

			TL = new(ax0 + by0 + wX, -(cx0 + dy0 + wY));
			TR = new(ax1 + by0 + wX, -(cx1 + dy0 + wY));
			BR = new(ax1 + by1 + wX, -(cx1 + dy1 + wY));
			BL = new(ax0 + by1 + wX, -(cx0 + dy1 + wY));
		}

		Model4System.CheckTextureUpdate(tex);
		bool tintBlack = Model4System.BeginTintBlack(slot, region.GetPage().GetPreMultipliedAlpha());
		Rlgl.Begin(DrawMode.TRIANGLES);
		Model4System.SetTexture(tex);

		var vertexColor = slot.VertexColor(Color, region.GetPage().GetPreMultipliedAlpha());
		Rlgl.Color4f(vertexColor.R, vertexColor.G, vertexColor.B, vertexColor.A);

		AtlasUV.ComputeRegionUVs(rx, ry, rw, rh, rotation, tex.GetWidth(), tex.GetHeight(), out var uvTL, out var uvTR, out var uvBR, out var uvBL);
		if (tex.HasPublicFlags(PublicTextureFlags.RequiresFlippedV)) {
			uvTL.Y = 1f - uvTL.Y;
			uvTR.Y = 1f - uvTR.Y;
			uvBR.Y = 1f - uvBR.Y;
			uvBL.Y = 1f - uvBL.Y;
		}
		Rlgl.TexCoord2f(uvBL.X, uvBL.Y); Rlgl.Vertex2f(BL.X, BL.Y);
		Rlgl.TexCoord2f(uvTR.X, uvTR.Y); Rlgl.Vertex2f(TR.X, TR.Y);
		Rlgl.TexCoord2f(uvTL.X, uvTL.Y); Rlgl.Vertex2f(TL.X, TL.Y);

		Rlgl.TexCoord2f(uvBR.X, uvBR.Y); Rlgl.Vertex2f(BR.X, BR.Y);
		Rlgl.TexCoord2f(uvTR.X, uvTR.Y); Rlgl.Vertex2f(TR.X, TR.Y);
		Rlgl.TexCoord2f(uvBL.X, uvBL.Y); Rlgl.Vertex2f(BL.X, BL.Y);

		Rlgl.End();
		if (tintBlack)
			Model4System.EndTintBlack();

		if (Model4System.m4s_wireframe.GetBool()) {
			Model4System.PrepareWireframeRendering();

			Rlgl.Vertex2f(BL.X, BL.Y); Rlgl.Vertex2f(TR.X, TR.Y);
			Rlgl.Vertex2f(TR.X, TR.Y); Rlgl.Vertex2f(TL.X, TL.Y);
			Rlgl.Vertex2f(TL.X, TL.Y); Rlgl.Vertex2f(BL.X, BL.Y);
			Rlgl.Vertex2f(BR.X, BR.Y); Rlgl.Vertex2f(TR.X, TR.Y);
			Rlgl.Vertex2f(TR.X, TR.Y); Rlgl.Vertex2f(BL.X, BL.Y);
			Rlgl.Vertex2f(BL.X, BL.Y); Rlgl.Vertex2f(BR.X, BR.Y);

			Model4System.EndWireframeRendering();
		}
	}
}

public record AttachmentWeight(int Bone, float Weight, Vector2F Position)
{
	public bool IsEmpty => Weight == 0;
	public override string ToString() => $"Weight [{Bone} @ {Position}] * {Weight}";
}

public class AttachmentVertex
{
	public float X;
	public float Y;
	public float U;
	public float V;
	public AttachmentWeight[]? Weights;

	public override string ToString() => $"Vertex [coords: {X}, {Y}] [texcoords: {U}, {V}] [weights: {Weights?.Length ?? 0}]";
}

public class AttachmentTriangle
{
	public int V1;
	public int V2;
	public int V3;
}

public class VertexAttachment : Attachment
{
	public AttachmentVertex[] Vertices;

	public Vector2F Position;
	public float Rotation;
	public Vector2F Scale;

	[JsonIgnore] public IModelAtlasRegion Region;
	[JsonIgnore] public bool InitializedRegion;

	/// <summary>
	/// The vertices this attachment uses, the parent's for linked meshes
	/// </summary>
	public virtual AttachmentVertex[] GetVertices() => Vertices;

	/// <summary>
	/// The attachment deform timelines are keyed on, can be the parent for linked meshes
	/// </summary>
	[JsonIgnore] public virtual VertexAttachment DeformAttachment => this;

	[JsonIgnore] int[]? weightStarts;
	[JsonIgnore] AttachmentVertex[]? weightStartsFor;

	/// <summary>
	/// Index of each vertex's first weight across the whole attachment
	/// </summary>
	public int[] GetWeightStarts() {
		var vertices = GetVertices();
		if (weightStartsFor != vertices || weightStarts == null) {
			weightStartsFor = vertices;
			weightStarts = new int[vertices.Length];
			int k = 0;
			for (int i = 0; i < vertices.Length; i++) {
				weightStarts[i] = k;
				k += vertices[i].Weights?.Length ?? 0;
			}
		}
		return weightStarts;
	}

	protected Vector2F CalculateVertexWorldPosition(SlotInstance slot, int vertexI) {
		var vertices = GetVertices();
		if (vertices == null)
			return default;

		var vertex = vertices[vertexI];
		float[]? deform = slot.DeformCount > 0 ? slot.Deform : null;

		if (vertex.Weights == null || vertex.Weights.Length <= 0) {
			ref var transform = ref slot.Bone.WorldTransform;
			float vx = vertex.X, vy = vertex.Y;
			if (deform != null && vertexI * 2 + 1 < slot.DeformCount) {
				vx += deform[vertexI * 2];
				vy += deform[vertexI * 2 + 1];
			}
			return new(
				vx * transform.A + vy * transform.B + transform.X,
				vx * transform.C + vy * transform.D + transform.Y
			);
		}

		var bones = slot.GetModel().Bones;
		float px = 0, py = 0;

		var weights = vertex.Weights;
		int k = deform != null ? GetWeightStarts()[vertexI] : 0;
		for (int i = 0; i < weights.Length; i++, k++) {
			var wd = weights[i];
			float w = wd.Weight;
			if (w == 0) continue;
			ref var t = ref bones[wd.Bone].WorldTransform;
			float lx = wd.Position.X, ly = wd.Position.Y;
			if (deform != null && k * 2 + 1 < slot.DeformCount) {
				lx += deform[k * 2];
				ly += deform[k * 2 + 1];
			}
			px += (lx * t.A + ly * t.B + t.X) * w;
			py += (lx * t.C + ly * t.D + t.Y) * w;
		}

		return new(px, py);
	}

	/// <summary>
	/// World x/y floats, start and count are in floats too
	/// </summary>
	public void ComputeWorldFloats(SlotInstance slot, int start, int count, float[] world, int offset) {
		for (int f = start, o = offset; f < start + count; f += 2, o += 2) {
			var pos = CalculateVertexWorldPosition(slot, f / 2);
			world[o] = pos.X;
			world[o + 1] = pos.Y;
		}
	}

	public int ComputeWorldVertices(SlotInstance slot, int startAt, int length, Vector2F[] worldVertices, int offset) {
		for (int i = startAt; i < length; i++) {
			worldVertices[i + offset] = CalculateVertexWorldPosition(slot, i);
		}

		return length - startAt;
	}

	public int ComputeWorldVerticesInto(SlotInstance slot, Vector2F[] worldVertices)
		=> ComputeWorldVertices(slot, 0, GetVertices().Length, worldVertices, 0);
}

public class LinkedMesh
{
	public string? Path;
	public Color Color;
	public string? Skin;
	public int SlotIndex;
	public string? Parent;
	public MeshAttachment Mesh = null!;
	public bool Deform;
	public int Width;
	public int Height;
}

public class MeshAttachment : VertexAttachment
{
	public AttachmentTriangle[] Triangles;
	public string Path;
	[JsonIgnore] public MeshAttachment? ParentMesh;

	/// <summary>
	/// Set on linked meshes: skin key of the parent mesh in the same slot
	/// </summary>
	public string? ParentKey;
	/// <summary>
	/// Skin the parent lives in, null for the default skin
	/// </summary>
	public string? ParentSkin;
	/// <summary>
	/// Linked mesh uses the parent's deform keys
	/// </summary>
	public bool InheritDeform = true;
	/// <summary>
	/// How many vertices at the start make up the hull, 0 = unknown. Not saved in .nm4b
	/// </summary>
	public int HullCount;

	[JsonIgnore] public bool IsLinked => ParentKey != null;
	public override VertexAttachment DeformAttachment => ParentMesh != null && InheritDeform ? ParentMesh : this;

	public string ToDesmosPointArray() {
		string[] triangles = new string[Triangles.Length];
		for (int i = 0, c = triangles.Length; i < c; i++) {
			var tri = Triangles[i];
			var v1 = Vertices[tri.V1];
			var v2 = Vertices[tri.V2];
			var v3 = Vertices[tri.V3];
			triangles[i] = $"polygon(({v1.X}, {v1.Y}), ({v2.X}, {v2.Y}), ({v3.X}, {v3.Y}))";
		}

		return string.Join('\n', triangles);
	}

	public Color Color = Color.White;
	public override byte Alpha => Color.A;

	public override void Setup(ModelData data) {
		Region = data.TextureAtlas.GetRegion(Path)!;
	}

	public override AttachmentVertex[] GetVertices() => ParentMesh?.Vertices ?? Vertices;

	public AttachmentTriangle[] GetTriangles() => ParentMesh?.Triangles ?? Triangles;
	public int GetHullCount() => ParentMesh?.HullCount ?? HullCount;

	public override void Render(SlotInstance slot) {
		var vertices = GetVertices();
		var triangles = GetTriangles();

		Debug.Assert(vertices != null);
		if (vertices == null)
			return;

		Debug.Assert(triangles != null);
		if (triangles == null)
			return;

		var region = Region;
		if (region == null) {
			Setup(slot.Bone.Model.Data);
			if (Region == null) return;
			region = Region;
		}
		slot.StartBlendModeFor(this, region.GetPage().GetPreMultipliedAlpha());

		var worldTransform = Transformation.CalculateWorldTransformation(Position, Rotation, Scale, Vector2F.Zero, TransformMode.Normal, slot.Bone.WorldTransform);
		region.GetBounds(out int rx, out int ry, out int rw, out int rh);
		region.GetOffsets(out int offsetX, out int offsetY, out int origW, out int origH);
		float rotation = region.GetRotation();
		ITexture tex = region.GetTexture();

		Model4System.CheckTextureUpdate(tex);
		bool tintBlack = Model4System.BeginTintBlack(slot, region.GetPage().GetPreMultipliedAlpha());
		Rlgl.Begin(DrawMode.TRIANGLES);
		Model4System.SetTexture(tex);

		var vertexColor = slot.VertexColor(Color, region.GetPage().GetPreMultipliedAlpha());
		Rlgl.Color4f(vertexColor.R, vertexColor.G, vertexColor.B, vertexColor.A);
		bool wireframe = Model4System.m4s_wireframe.GetBool();

		const int HEAP_ALLOCATING_VERTEXCOUNT_THRESHOLD = 512;
		int vertCount = vertices.Length;
		Span<Vector2F> worldVerts = vertCount <= HEAP_ALLOCATING_VERTEXCOUNT_THRESHOLD ? stackalloc Vector2F[vertCount] : new Vector2F[vertCount];
		for (int vi = 0; vi < vertCount; vi++)
			worldVerts[vi] = CalculateVertexWorldPosition(slot, vi);

		bool flipV = tex.HasPublicFlags(PublicTextureFlags.RequiresFlippedV);
		Span<float> uvU = vertCount <= HEAP_ALLOCATING_VERTEXCOUNT_THRESHOLD ? stackalloc float[vertCount] : new float[vertCount];
		Span<float> uvV = vertCount <= HEAP_ALLOCATING_VERTEXCOUNT_THRESHOLD ? stackalloc float[vertCount] : new float[vertCount];
		for (int vi = 0; vi < vertCount; vi++) {
			AtlasUV.RemapMeshUV(rx, ry, rw, rh, rotation, offsetX, offsetY, origW, origH, tex.GetWidth(), tex.GetHeight(), vertices[vi].U, vertices[vi].V, out uvU[vi], out uvV[vi]);
			if (flipV) uvV[vi] = 1f - uvV[vi];
		}

		if (triangles.Length > 0) {
			foreach (var tri in triangles) {
				Vector2F p1 = worldVerts[tri.V1];
				Vector2F p2 = worldVerts[tri.V2];
				Vector2F p3 = worldVerts[tri.V3];

				Rlgl.TexCoord2f(uvU[tri.V1], uvV[tri.V1]); Rlgl.Vertex3f(p1.X, -p1.Y, 0);
				Rlgl.TexCoord2f(uvU[tri.V2], uvV[tri.V2]); Rlgl.Vertex3f(p2.X, -p2.Y, 0);
				Rlgl.TexCoord2f(uvU[tri.V3], uvV[tri.V3]); Rlgl.Vertex3f(p3.X, -p3.Y, 0);
			}
		}

		Rlgl.End();
		if (tintBlack)
			Model4System.EndTintBlack();

		if (wireframe) {
			Model4System.PrepareWireframeRendering();
			foreach (var tri in triangles) {
				Vector2F p1 = worldVerts[tri.V1];
				Vector2F p2 = worldVerts[tri.V2];
				Vector2F p3 = worldVerts[tri.V3];

				Rlgl.Vertex3f(p1.X, -p1.Y, 0); Rlgl.Vertex3f(p2.X, -p2.Y, 0);
				Rlgl.Vertex3f(p2.X, -p2.Y, 0); Rlgl.Vertex3f(p3.X, -p3.Y, 0);
				Rlgl.Vertex3f(p3.X, -p3.Y, 0); Rlgl.Vertex3f(p1.X, -p1.Y, 0);
			}
			Model4System.EndWireframeRendering();
		}
	}
}

public class ClippingAttachment : VertexAttachment, IClipPolygon<SlotInstance>
{
	public string? EndSlot = null;

	public int GetVerticesCount() => Vertices.Length;

	public override void Render(SlotInstance slot) {
		base.Render(slot);
		slot.Model.Clipping.Start(this, slot, EndSlot);
	}
}

public static class AtlasUV
{
	public static void ComputeRegionUVs(int rx, int ry, int rw, int rh, float rotation, int texW, int texH, out Vector2F uvTL, out Vector2F uvTR, out Vector2F uvBR, out Vector2F uvBL) {
		float x0 = (float)rx / texW;
		float y0 = (float)ry / texH;
		float x1 = (float)(rx + rw) / texW;
		float y1 = (float)(ry + rh) / texH;

		Vector2F a = new(x0, y0);
		Vector2F b = new(x1, y0);
		Vector2F c = new(x1, y1);
		Vector2F d = new(x0, y1);

		int deg = (int)rotation;
		switch (deg) {
			case 90:
				uvTL = d; uvTR = a; uvBR = b; uvBL = c;
				break;

			case 180:
				uvTL = c; uvTR = d; uvBR = a; uvBL = b;
				break;

			case 270:
				uvTL = b; uvTR = c; uvBR = d; uvBL = a;
				break;

			default:
				uvTL = a; uvTR = b; uvBR = c; uvBL = d;
				break;
		}
	}

	public static void RemapMeshUV(int rx, int ry, int rw, int rh, float rotation,
		int offsetX, int offsetY, int origW, int origH,
		int texW, int texH, float u, float v, out float outU, out float outV) {
		v = 1 - v;

		int deg = (int)rotation;
		int cropW = (deg % 180 == 90) ? rh : rw;
		int cropH = (deg % 180 == 90) ? rw : rh;

		float cu = (u * origW - offsetX) / cropW;
		float cv = (v * origH - offsetY) / cropH;

		float x0 = (float)rx / texW;
		float y0 = (float)ry / texH;
		float fw = (float)rw / texW;
		float fh = (float)rh / texH;

		switch (deg) {
			case 90:
				outU = x0 + cv * fw;
				outV = y0 + (1 - cu) * fh;
				break;

			case 180:
				outU = x0 + (1 - cu) * fw;
				outV = y0 + (1 - cv) * fh;
				break;

			case 270:
				outU = x0 + (1 - cv) * fw;
				outV = y0 + cu * fh;
				break;

			default:
				outU = x0 + cu * fw;
				outV = y0 + cv * fh;
				break;
		}
	}
}

// Get ready for interface hell here; but most of it is for good reason...

public abstract class Timeline
{
	public abstract void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir = MixDirection.Out);

	/// <summary>
	/// Adds events keyed after lastTime, up to and including time.
	/// </summary>
	public virtual void CollectEvents(double lastTime, double time, List<ModelEvent> into) { }
}

public interface IBoneTimeline
{
	public int BoneIndex { get; set; }
}

public interface ISlotTimeline
{
	public int SlotIndex { get; set; }
}

public static class TimelineInterfaceExtensions
{
	public static BoneInstance Bone(this IBoneTimeline tl, ModelInstance model) => model.Bones[tl.BoneIndex];

	public static SlotInstance Slot(this ISlotTimeline tl, ModelInstance model) => model.Slots[tl.SlotIndex];
}

/// <summary>
/// We assume each curve in <see cref="Curves"/> contains the same amount of keyframes.
/// <br/>
/// If this is not true, things like BeforeFirstFrame and DetermineValue will likely not work correctly.
/// <br/>
/// So don't mess with these unless you know what you're doing.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class CurveTimeline<T> : Timeline
{
	public FCurve<T>[] Curves;
	private T?[] outputBuffer;

	public CurveTimeline(int curves) {
		Curves = new FCurve<T>[curves];
		outputBuffer = new T[curves];
	}

	public void NewCurves() {
		for (int i = 0; i < Curves.Length; i++) {
			Curves[i] = new();
		}
	}

	/// <summary>
	/// Returns a shared <typeparamref name="T"/>[] instance; keep this in mind if you store this value somewhere.
	/// <br/>
	/// You aren't supposed to though, you're supposed to use it to convert it into a struct/etc of your choosing
	/// </summary>
	/// <param name="time"></param>
	/// <returns></returns>
	public T?[] DetermineValue(double time) {
		for (int i = 0; i < Curves.Length; i++)
			outputBuffer[i] = Curves[i].DetermineValueAtTime(time);

		return outputBuffer;
	}

	public bool BeforeFirstFrame(double time, out T? value) {
		var first = Curves[0].First;
		value = default;
		if (first == null) return true;

		value = first.Value;
		return time < first.Time;
	}

	public bool AfterLastFrame(double time, out T? value) {
		var last = Curves[0].Last;
		value = default;
		if (last == null) return true;

		value = last.Value;
		return time > last.Time;
	}

	public bool BeforeFirstFrame(double time) => BeforeFirstFrame(time, out var _);

	public bool AfterLastFrame(double time) => AfterLastFrame(time, out var _);

	public FCurve<T> Curve(int index) => Curves[index];
}

public abstract class MonoBoneFloatPropertyTimeline() : CurveTimeline<float>(1), IBoneTimeline
{
	public int BoneIndex { get; set; }

	public abstract float Get(BoneInstance bone);

	public abstract float GetSetup(BoneInstance bone);

	public abstract void Set(BoneInstance bone, float value);
}

public abstract class MonoBoneTranslationPropertyTimeline() : MonoBoneFloatPropertyTimeline
{
	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir) {
		float r;

		var bone = this.Bone(model);
		if (BeforeFirstFrame(time))
			switch (blend) {
				case MixBlendMode.Setup:
					Set(bone, GetSetup(bone));
					return;

				case MixBlendMode.First:
					Set(bone, Get(bone) + (GetSetup(bone) - Get(bone)) * (float)mix);
					return;

				default: return;
			}

		if (!AfterLastFrame(time, out r))
			r = Curve(0).DetermineValueAtTime(time);

		switch (blend) {
			case MixBlendMode.Setup:
				Set(bone, GetSetup(bone) + r * (float)mix);
				break;

			case MixBlendMode.First:
			case MixBlendMode.Replace:
				Set(bone, Get(bone) + (GetSetup(bone) + r - Get(bone)) * (float)mix);
				break;

			case MixBlendMode.Add:
				Set(bone, Get(bone) + (r * (float)mix));
				break;
		}
	}
}

public abstract class MonoBoneMultiplicativePropertyTimeline() : MonoBoneFloatPropertyTimeline
{
	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir) {
		float r;

		var bone = this.Bone(model);
		if (BeforeFirstFrame(time))
			switch (blend) {
				case MixBlendMode.Setup:
					Set(bone, GetSetup(bone));
					return;

				case MixBlendMode.First:
					Set(bone, Get(bone) + ((GetSetup(bone) - Get(bone)) * (float)mix));
					return;

				default: return;
			}

		if (!AfterLastFrame(time, out r))
			r = Curve(0).DetermineValueAtTime(time) * GetSetup(bone);

		if (mix == 1) {
			if (blend == MixBlendMode.Add)
				Set(bone, Get(bone) + (r - GetSetup(bone)));
			else
				Set(bone, r);
		}
		else {
			float v;
			switch (blend) {
				case MixBlendMode.Setup:
					v = MathF.Abs(GetSetup(bone)) * MathF.Sign(r);
					Set(bone, v + (r - v) * (float)mix);
					break;

				case MixBlendMode.First:
				case MixBlendMode.Replace:
					v = MathF.Abs(Get(bone)) * MathF.Sign(r);
					Set(bone, v + (r - v) * (float)mix);
					break;

				case MixBlendMode.Add:
					v = MathF.Sign(r);
					Set(bone, MathF.Abs(Get(bone)) * v + (r - MathF.Abs(GetSetup(bone)) * v) * (float)mix);
					break;
			}
		}
	}
}

public abstract class MonoBoneRotationPropertyTimeline() : MonoBoneFloatPropertyTimeline
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float PERFORM_ROT_WRAP(float r) {
		r %= 360f;
		if (r >= 180f)
			r -= 360f;
		else if (r < -180f)
			r += 360f;
		return r;
	}

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir) {
		float r;

		var bone = this.Bone(model);
		if (BeforeFirstFrame(time))
			switch (blend) {
				case MixBlendMode.Setup:
					Set(bone, GetSetup(bone));
					return;

				case MixBlendMode.First:
					r = GetSetup(bone) - Get(bone);
					Set(bone, Get(bone) + (PERFORM_ROT_WRAP(r)) * (float)mix);
					return;

				default: return;
			}

		if (AfterLastFrame(time, out r))
			switch (blend) {
				case MixBlendMode.Setup:
					Set(bone, GetSetup(bone) + r * (float)mix);
					return;

				case MixBlendMode.First:
				case MixBlendMode.Replace:
					r += GetSetup(bone) - Get(bone);
					r = PERFORM_ROT_WRAP(r);
					goto case MixBlendMode.Add;
				case MixBlendMode.Add:
					Set(bone, Get(bone) + (r * (float)mix));
					return;

				default: return;
			}

		r = Curve(0).DetermineValueAtTime(time);

		switch (blend) {
			case MixBlendMode.Setup:
				Set(bone, GetSetup(bone) + (PERFORM_ROT_WRAP(r)) * (float)mix);
				break;

			case MixBlendMode.First:
			case MixBlendMode.Replace:
				r += GetSetup(bone) - Get(bone);
				goto case MixBlendMode.Add;
			case MixBlendMode.Add:
				Set(bone, Get(bone) + (PERFORM_ROT_WRAP(r)) * (float)mix);
				break;
		}
	}
}

// translation == shearing, but this exists for simplicity if that has to change for some reason
public abstract class MonoBoneShearingPropertyTimeline() : MonoBoneTranslationPropertyTimeline();

public abstract class DuoBoneFloatPropertyTimeline(bool multiplicative) : CurveTimeline<float>(2), IBoneTimeline
{
	public int BoneIndex { get; set; }

	public abstract Vector2F Get(BoneInstance bone);

	public abstract Vector2F GetSetup(BoneInstance bone);

	public abstract void Set(BoneInstance bone, Vector2F value);

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir) {
		float x, y;
		if (multiplicative) {
			var bone = this.Bone(model);

			if (BeforeFirstFrame(time))
				switch (blend) {
					case MixBlendMode.Setup:
						Set(bone, GetSetup(bone));
						return;

					case MixBlendMode.First:
						Set(bone, Get(bone) + ((GetSetup(bone) - Get(bone)) * (float)mix));
						return;

					default: return;
				}

			if (AfterLastFrame(time)) {
				x = Curve(0).Last!.Value * GetSetup(bone).X;
				y = Curve(1).Last!.Value * GetSetup(bone).Y;
			}
			else {
				int index = Curve(0).DetermineIndexAtTime(time);
				x = Curve(0).DetermineValueAtTime(time, index) * GetSetup(bone).X;
				y = Curve(1).DetermineValueAtTime(time, index) * GetSetup(bone).Y;
			}

			if (mix == 1) {
				if (blend == MixBlendMode.Add)
					Set(bone, Get(bone) + (new Vector2F(x, y) - GetSetup(bone)));
				else
					Set(bone, new Vector2F(x, y));
			}
			else {
				Vector2F v;
				switch (blend) {
					case MixBlendMode.Setup:
						v = Vector2F.Abs(GetSetup(bone)) * Vector2F.Sign(new(x, y));
						Set(bone, v + (new Vector2F(x, y) - v) * (float)mix);
						break;

					case MixBlendMode.First:
					case MixBlendMode.Replace:
						v = Vector2F.Abs(Get(bone)) * Vector2F.Sign(new(x, y));
						Set(bone, v + (new Vector2F(x, y) - v) * (float)mix);
						break;

					case MixBlendMode.Add:
						v = Vector2F.Sign(new Vector2F(x, y));
						Set(bone, Vector2F.Abs(Get(bone)) * v + (new Vector2F(x, y) - Vector2F.Abs(GetSetup(bone)) * v) * (float)mix);
						break;
				}
			}
		}
		else {
			var bone = this.Bone(model);
			if (BeforeFirstFrame(time))
				switch (blend) {
					case MixBlendMode.Setup:
						Set(bone, GetSetup(bone));
						return;

					case MixBlendMode.First:
						Set(bone, Get(bone) + (GetSetup(bone) - Get(bone)) * (float)mix);
						return;

					default: return;
				}

			if (AfterLastFrame(time)) {
				x = Curve(0).Last!.Value;
				y = Curve(1).Last!.Value;
			}
			else {
				int index = Curve(0).DetermineIndexAtTime(time);

				x = Curve(0).DetermineValueAtTime(time, index);
				y = Curve(1).DetermineValueAtTime(time, index);
			}

			switch (blend) {
				case MixBlendMode.Setup:
					Set(bone, GetSetup(bone) + new Vector2F(x, y) * (float)mix);
					break;

				case MixBlendMode.First:
				case MixBlendMode.Replace:
					Set(bone, Get(bone) + (GetSetup(bone) + new Vector2F(x, y) - Get(bone)) * (float)mix);
					break;

				case MixBlendMode.Add:
					Set(bone, Get(bone) + (new Vector2F(x, y) * (float)mix));
					break;
			}
		}
	}
}

public class SlotColor4Timeline() : CurveTimeline<float>(4), ISlotTimeline
{
	public int SlotIndex { get; set; }

	public Color Get(SlotInstance slot) => slot.Color;

	public Color GetSetup(SlotInstance slot) => slot.Data.Color;

	public void Set(SlotInstance slot, Color value) => slot.Color = value;

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir) {
		var slot = this.Slot(model);
		if (BeforeFirstFrame(time))
			switch (blend) {
				case MixBlendMode.Setup: Set(slot, GetSetup(slot)); return;
				case MixBlendMode.First: Set(slot, Get(slot) + ((GetSetup(slot) - Get(slot)) * (float)mix)); return;
				default: return;
			}

		float r, g, b, a;
		Color rgba;

		if (AfterLastFrame(time)) {
			r = Curve(0).Last?.Value ?? 1;
			g = Curve(1).Last?.Value ?? 1;
			b = Curve(2).Last?.Value ?? 1;
			a = Curve(3).Last?.Value ?? 1;
		}
		else {
			int index = Curve(0).DetermineIndexAtTime(time);

			r = Curve(0).DetermineValueAtTime(time, index);
			g = Curve(1).DetermineValueAtTime(time, index);
			b = Curve(2).DetermineValueAtTime(time, index);
			a = Curve(3).DetermineValueAtTime(time, index);
		}

		if (mix != 1) {
			Color basecolor = blend == MixBlendMode.Setup ? GetSetup(slot) : Get(slot);
			float baser = basecolor.R / 255f, baseg = basecolor.G / 255f, baseb = basecolor.B / 255f, basea = basecolor.A / 255f;
			r = (basecolor.R / 255f) + ((r - baser) * (float)mix);
			g = (basecolor.G / 255f) + ((g - baseg) * (float)mix);
			b = (basecolor.B / 255f) + ((b - baseb) * (float)mix);
			a = (basecolor.A / 255f) + ((a - basea) * (float)mix);
		}

		r = Math.Clamp(r * 255f, 0, 255);
		g = Math.Clamp(g * 255f, 0, 255);
		b = Math.Clamp(b * 255f, 0, 255);
		a = Math.Clamp(a * 255f, 0, 255);
		rgba = new((byte)r, (byte)g, (byte)b, (byte)a);
		Set(slot, rgba);
	}
}

/// <summary>
/// Slot color without alpha, for slots keyed with color and alpha apart
/// </summary>
public class SlotRGBTimeline() : CurveTimeline<float>(3), ISlotTimeline
{
	public int SlotIndex { get; set; }

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir) {
		var slot = this.Slot(model);
		var setup = slot.Data.Color;
		if (BeforeFirstFrame(time)) {
			switch (blend) {
				case MixBlendMode.Setup: slot.Color = new(setup.R, setup.G, setup.B, slot.Color.A); return;
				case MixBlendMode.First: {
						var now = slot.Color;
						slot.Color = new(
							(byte)(now.R + (setup.R - now.R) * mix),
							(byte)(now.G + (setup.G - now.G) * mix),
							(byte)(now.B + (setup.B - now.B) * mix),
							now.A);
						return;
					}
				default: return;
			}
		}

		float r, g, b;
		if (AfterLastFrame(time)) {
			r = Curve(0).Last?.Value ?? 1;
			g = Curve(1).Last?.Value ?? 1;
			b = Curve(2).Last?.Value ?? 1;
		}
		else {
			int index = Curve(0).DetermineIndexAtTime(time);
			r = Curve(0).DetermineValueAtTime(time, index);
			g = Curve(1).DetermineValueAtTime(time, index);
			b = Curve(2).DetermineValueAtTime(time, index);
		}

		var current = slot.Color;
		if (mix != 1) {
			var basecolor = blend == MixBlendMode.Setup ? setup : current;
			r = basecolor.R / 255f + (r - basecolor.R / 255f) * (float)mix;
			g = basecolor.G / 255f + (g - basecolor.G / 255f) * (float)mix;
			b = basecolor.B / 255f + (b - basecolor.B / 255f) * (float)mix;
		}
		slot.Color = new(
			(byte)Math.Clamp(r * 255f, 0, 255),
			(byte)Math.Clamp(g * 255f, 0, 255),
			(byte)Math.Clamp(b * 255f, 0, 255),
			current.A);
	}
}

/// <summary>
/// Slot alpha on its own, for slots keyed with color and alpha apart
/// </summary>
public class SlotAlphaTimeline() : CurveTimeline<float>(1), ISlotTimeline
{
	public int SlotIndex { get; set; }

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir) {
		var slot = this.Slot(model);
		var now = slot.Color;
		byte setup = slot.Data.Color.A;
		if (BeforeFirstFrame(time)) {
			switch (blend) {
				case MixBlendMode.Setup: slot.Color = new(now.R, now.G, now.B, setup); return;
				case MixBlendMode.First:
					slot.Color = new(now.R, now.G, now.B, (byte)(now.A + (setup - now.A) * mix));
					return;
				default: return;
			}
		}

		float a = AfterLastFrame(time) ? Curve(0).Last?.Value ?? 1 : Curve(0).DetermineValueAtTime(time);
		if (mix != 1) {
			float baseA = (blend == MixBlendMode.Setup ? setup : now.A) / 255f;
			a = baseA + (a - baseA) * (float)mix;
		}
		slot.Color = new(now.R, now.G, now.B, (byte)Math.Clamp(a * 255f, 0, 255));
	}
}

/// <summary>
/// Slot color plus the tint black (dark) color, curves R, G, B, A, dark R, G, B
/// </summary>
public class SlotRGBA2Timeline() : CurveTimeline<float>(7), ISlotTimeline
{
	public int SlotIndex { get; set; }

	private static byte ToByte(float value) => (byte)Math.Clamp(value * 255f, 0, 255);

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir) {
		var slot = this.Slot(model);
		Color setupLight = slot.Data.Color, setupDark = slot.Data.DarkColor ?? Color.Black;
		Color light = slot.Color, dark = slot.DarkColor ?? Color.Black;

		if (BeforeFirstFrame(time)) {
			switch (blend) {
				case MixBlendMode.Setup:
					slot.Color = setupLight;
					slot.DarkColor = setupDark;
					return;
				case MixBlendMode.First:
					slot.Color = light + (setupLight - light) * (float)mix;
					slot.DarkColor = dark + (setupDark - dark) * (float)mix;
					return;
				default: return;
			}
		}

		Span<float> values = stackalloc float[7];
		if (AfterLastFrame(time)) {
			for (int i = 0; i < 7; i++)
				values[i] = Curve(i).Last?.Value ?? 1;
		}
		else {
			int index = Curve(0).DetermineIndexAtTime(time);
			for (int i = 0; i < 7; i++)
				values[i] = Curve(i).DetermineValueAtTime(time, index);
		}

		if (mix != 1) {
			Color baseLight = blend == MixBlendMode.Setup ? setupLight : light;
			Color baseDark = blend == MixBlendMode.Setup ? setupDark : dark;
			ReadOnlySpan<float> baseValues = [
				baseLight.R / 255f, baseLight.G / 255f, baseLight.B / 255f, baseLight.A / 255f,
				baseDark.R / 255f, baseDark.G / 255f, baseDark.B / 255f
			];
			for (int i = 0; i < 7; i++)
				values[i] = baseValues[i] + (values[i] - baseValues[i]) * (float)mix;
		}

		slot.Color = new(ToByte(values[0]), ToByte(values[1]), ToByte(values[2]), ToByte(values[3]));
		slot.DarkColor = new(ToByte(values[4]), ToByte(values[5]), ToByte(values[6]), (byte)255);
	}
}

public class ActiveAttachmentTimeline() : CurveTimeline<string?>(1), ISlotTimeline
{
	public int SlotIndex { get; set; }

	public string? Get(SlotInstance slot) => slot.Attachment?.Name;

	public string? GetSetup(SlotInstance slot) => slot.Data.Attachment;

	public void Set(SlotInstance slot, string? value) => slot.SetAttachment(value);

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir) {
		var slot = this.Slot(model);

		if (BeforeFirstFrame(time))
			switch (blend) {
				case MixBlendMode.Setup:
				case MixBlendMode.First:
					Set(slot, GetSetup(slot));
					return;

				default: return;
			}

		string? attachmentID;
		Color rgba;

		var curve = Curve(0);
		if (AfterLastFrame(time)) {
			attachmentID = curve.Last?.Value ?? null;
			switch (blend) {
				case MixBlendMode.Setup:
				case MixBlendMode.First:
					Set(slot, attachmentID);
					return;

				default: return;
			}
		}

		attachmentID = Curve(0).DetermineValueAtTime(time);

		Set(slot, attachmentID);
	}
}

public class DrawOrderTimeline() : CurveTimeline<int[]?>(1)
{
	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir) {
		if (BeforeFirstFrame(time))
			return;

		int[]? order = Curve(0).DetermineValueAtTime(time);
		if (order == null)
			return;

		var drawOrder = model.DrawOrder;
		var slots = model.Slots;
		drawOrder.Clear();
		for (int i = 0; i < order.Length; i++)
			drawOrder.Add(slots[order[i]]);
	}
}

public class TranslateTimeline() : DuoBoneFloatPropertyTimeline(false)
{
	public override Vector2F Get(BoneInstance bone) => bone.Position;

	public override Vector2F GetSetup(BoneInstance bone) => bone.Data.Position;

	public override void Set(BoneInstance bone, Vector2F value) => bone.Position = value;
}

public class ScaleTimeline() : DuoBoneFloatPropertyTimeline(true)
{
	public override Vector2F Get(BoneInstance bone) => bone.Scale;

	public override Vector2F GetSetup(BoneInstance bone) => bone.Data.Scale;

	public override void Set(BoneInstance bone, Vector2F value) => bone.Scale = value;
}

public class ShearTimeline() : DuoBoneFloatPropertyTimeline(false)
{
	public override Vector2F Get(BoneInstance bone) => bone.Shear;

	public override Vector2F GetSetup(BoneInstance bone) => bone.Data.Shear;

	public override void Set(BoneInstance bone, Vector2F value) => bone.Shear = value;
}

public class RotationTimeline() : MonoBoneRotationPropertyTimeline()
{
	public override float Get(BoneInstance bone) => bone.Rotation;

	public override float GetSetup(BoneInstance bone) => bone.Data.Rotation;

	public override void Set(BoneInstance bone, float value) => bone.Rotation = value;
}

public class TranslateXTimeline() : MonoBoneTranslationPropertyTimeline()
{
	public override float Get(BoneInstance bone) => bone.Position.X;

	public override float GetSetup(BoneInstance bone) => bone.Data.Position.X;

	public override void Set(BoneInstance bone, float value) => bone.Position = new(value, bone.Position.Y);
}

public class TranslateYTimeline() : MonoBoneTranslationPropertyTimeline()
{
	public override float Get(BoneInstance bone) => bone.Position.Y;

	public override float GetSetup(BoneInstance bone) => bone.Data.Position.Y;

	public override void Set(BoneInstance bone, float value) => bone.Position = new(bone.Position.X, value);
}

public class ScaleXTimeline() : MonoBoneMultiplicativePropertyTimeline()
{
	public override float Get(BoneInstance bone) => bone.Scale.X;

	public override float GetSetup(BoneInstance bone) => bone.Data.Scale.X;

	public override void Set(BoneInstance bone, float value) => bone.Scale = new(value, bone.Scale.Y);
}

public class ScaleYTimeline() : MonoBoneMultiplicativePropertyTimeline()
{
	public override float Get(BoneInstance bone) => bone.Scale.Y;

	public override float GetSetup(BoneInstance bone) => bone.Data.Scale.Y;

	public override void Set(BoneInstance bone, float value) => bone.Scale = new(bone.Scale.X, value);
}

public class ShearXTimeline() : MonoBoneShearingPropertyTimeline()
{
	public override float Get(BoneInstance bone) => bone.Shear.X;

	public override float GetSetup(BoneInstance bone) => bone.Data.Shear.X;

	public override void Set(BoneInstance bone, float value) => bone.Shear = new(value, bone.Shear.Y);
}

public class ShearYTimeline() : MonoBoneShearingPropertyTimeline()
{
	public override float Get(BoneInstance bone) => bone.Shear.Y;

	public override float GetSetup(BoneInstance bone) => bone.Data.Shear.Y;

	public override void Set(BoneInstance bone, float value) => bone.Shear = new(bone.Shear.X, value);
}

public delegate void AnimPlaybackBeginFn(AnimationChannelEntry entry);

public delegate void AnimPlaybackStartFn(AnimationChannelEntry entry);

public delegate void AnimPlaybackEndFn(AnimationChannelEntry entry);

public delegate void AnimPlaybackCompleteFn(AnimationChannelEntry entry);

public delegate void AnimEventFn(AnimationChannelEntry entry, ModelEvent modelEvent);

public class AnimationChannelEntry
{
	public Animation Animation;

	/// <summary> Called when the animation playback has first begun (loop exclusive) </summary>
	public AnimPlaybackBeginFn? OnPlaybackBegin;

	/// <summary> Called when the animation playback has started (loop inclusive)</summary>
	public AnimPlaybackStartFn? OnPlaybackStart;

	/// <summary> Called when the animation playback has completed (loop inclusive). Note that loopback will trigger Start again. </summary>
	public AnimPlaybackEndFn? OnPlaybackEnd;

	/// <summary> Called when the animation playback has fully completed (loop exclusive) </summary>
	public AnimPlaybackCompleteFn? OnPlaybackComplete;

	/// <summary>
	/// Called for each event key playback passes
	/// </summary>
	public AnimEventFn? OnEvent;

	public bool Looping;
	public double LoopDuration = -1;

	public bool LimitedLoop => LoopDuration > 0;
}

public class AnimationChannel
{
	public AnimationChannelEntry? CurrentEntry;
	public Queue<AnimationChannelEntry> QueuedEntries = [];
	public double Time;
	public float Mix = 1;
	public double ElapsedTime;
	public MixBlendMode Blending;
	/// <summary>
	/// Time events were last collected up to, -1 so keys at 0 fire
	/// </summary>
	public double EventTime = -1;

	public void ElapseTime(double deltaTime) {
		Time += deltaTime;
		ElapsedTime += deltaTime;
	}

	public void ResetTime() {
		Time = 0;
		ElapsedTime = 0;
		EventTime = -1;
	}

	public void EnqueueNext() {
		// Enqueue the next animation
		CurrentEntry?.OnPlaybackEnd?.Invoke(CurrentEntry);
		CurrentEntry?.OnPlaybackComplete?.Invoke(CurrentEntry);
		if (QueuedEntries.TryDequeue(out AnimationChannelEntry? newAnim)) {
			CurrentEntry = newAnim;
			newAnim.OnPlaybackBegin?.Invoke(newAnim);
			newAnim.OnPlaybackStart?.Invoke(newAnim);
			ResetTime();
		}
		else {
			CurrentEntry = null;
			ResetTime();
		}
	}
}

public class AnimationHandler
{
	public AnimationChannel[] Channels = new AnimationChannel[5];
	private ModelData? model;

	public AnimationHandler() {
		for (int i = 0; i < Channels.Length; i++) {
			Channels[i] = new();
		}
	}

	public ModelData? GetModelData() => model;

	public void SetModel(ModelData? data) {
		if (model == data)
			return;
		model = data;
		ClearAllAnimation();
	}

	public void SetModel(ModelInstance? instance) => SetModel(instance?.Data);

	public bool IsPlayingAnimation() {
		foreach (var channel in Channels) {
			if (channel.CurrentEntry != null) return true;
		}

		return false;
	}

	public bool IsAnimationQueued() {
		foreach (var channel in Channels) {
			if (channel.QueuedEntries.Count > 0) return true;
		}

		return false;
	}

	public void AddDeltaTime(double time) {
		for (int i = 0; i < Channels.Length; i++) {
			var channel = Channels[i];

			var anim = channel.CurrentEntry;
			if (anim == null) {
				if (channel.QueuedEntries.TryDequeue(out AnimationChannelEntry? newAnim)) {
					channel.CurrentEntry = newAnim;
					channel.ResetTime();
					anim = newAnim;
				}
				else continue;
			}
			channel.ElapseTime(time);
			if (channel.Time >= anim.Animation.Duration) {
				if (anim.Looping) {
					anim.OnPlaybackEnd?.Invoke(anim);
					channel.Time %= anim.Animation.Duration;
					anim.OnPlaybackStart?.Invoke(anim);
				}
				else
					channel.EnqueueNext();
			}

			if (anim.LimitedLoop && channel.ElapsedTime >= anim.LoopDuration)
				channel.EnqueueNext();
		}
	}

	public AnimationChannelEntry? AddAnimation(int channel, string? animation, bool loops = false, double loopDuration = -1) {
		if (animation == null)
			return null;

		var channelObj = Channels[channel];

		var anim = model?.FindAnimation(animation);
		if (anim == null) return null;

		AnimationChannelEntry entry = new() {
			Animation = anim,
			Looping = loops,
			LoopDuration = loopDuration
		};
		channelObj.QueuedEntries.Enqueue(entry);
		return entry;
	}

	public AnimationChannelEntry? SetAnimation(int channel, string? animation, bool loops = false, double loopDuration = -1) {
		if (animation == null)
			return null;

		var channelObj = Channels[channel];
		StopAnimation(channel);

		var anim = model?.FindAnimation(animation);
		if (anim == null) return null;

		channelObj.QueuedEntries.Clear();
		AnimationChannelEntry entry = new() {
			Animation = anim,
			Looping = loops,
			LoopDuration = loopDuration
		};
		channelObj.QueuedEntries.Enqueue(entry);
		return entry;
	}

	public void StopAllAnimation() {
		for (int channel = 0; channel < Channels.Length; channel++) {
			Channels[channel].CurrentEntry = null;
			Channels[channel].Time = 0;
		}
	}

	public void StopAnimation(int channel) {
		Channels[channel].CurrentEntry = null;
		Channels[channel].Time = 0;
	}

	public void ClearAllAnimation() {
		for (int channel = 0; channel < Channels.Length; channel++) {
			Channels[channel].QueuedEntries.Clear();
			Channels[channel].CurrentEntry = null;
			Channels[channel].Time = 0;
		}
	}

	public void ClearAnimation(int channel) {
		Channels[channel].QueuedEntries.Clear();
		Channels[channel].CurrentEntry = null;
		Channels[channel].Time = 0;
	}

	/// <summary>
	/// Called for each event key any channel passes
	/// </summary>
	public event AnimEventFn? OnEvent;

	readonly List<ModelEvent> firedEvents = [];

	public void Apply(ModelInstance model) {
		model.ResetDrawOrderToSetup();

		bool first = true;
		foreach (var channel in Channels) {
			var entry = channel.CurrentEntry;
			if (entry == null) continue;

			MixBlendMode blending = first ? MixBlendMode.First : channel.Blending;
			float mix = channel.Mix;
			firedEvents.Clear();
			entry.Animation.Apply(model, channel.EventTime, channel.Time, false, firedEvents, mix, blending);
			channel.EventTime = channel.Time;
			foreach (var firedEvent in firedEvents) {
				entry.OnEvent?.Invoke(entry, firedEvent);
				OnEvent?.Invoke(entry, firedEvent);
			}
			first = false;
		}
	}
}

public interface IModelFormat
{
	ModelData LoadModelFromFile(string pathID, string path);

	void SaveModelToFile(string absoluteFilePath, ModelData modelData);
}

public enum NucleusModel_SaveType : ushort
{
	Attachment = 1 << 11,
	Timeline = 1 << 12,

	Attachment_Region = Attachment | 1,
	Attachment_Mesh = Attachment | 2,
	Attachment_Clipping = Attachment | 3,

	Timeline_Translate = Timeline | 1,
	Timeline_TranslateX = Timeline | 2,
	Timeline_TranslateY = Timeline | 3,
	Timeline_Rotation = Timeline | 4,
	Timeline_Scale = Timeline | 5,
	Timeline_ScaleX = Timeline | 6,
	Timeline_ScaleY = Timeline | 7,
	Timeline_Shear = Timeline | 8,
	Timeline_ShearX = Timeline | 9,
	Timeline_ShearY = Timeline | 10,
	Timeline_SlotColor4 = Timeline | 11,
	Timeline_ActiveAttachment = Timeline | 12,

	Attachment_Path = Attachment | 4,
	Attachment_LinkedMesh = Attachment | 5,

	Timeline_DrawOrder = Timeline | 13,
	Timeline_Deform = Timeline | 14,
	Timeline_IkConstraint = Timeline | 15,
	Timeline_TransformConstraint = Timeline | 16,
	Timeline_PathConstraintPosition = Timeline | 17,
	Timeline_PathConstraintSpacing = Timeline | 18,
	Timeline_PathConstraintMix = Timeline | 19,
	Timeline_Event = Timeline | 20,
	Timeline_SlotRGB = Timeline | 21,
	Timeline_SlotAlpha = Timeline | 22,
	Timeline_SlotRGBA2 = Timeline | 23,
}

public static class NucleusModel_SaveType_Ext
{
	public static NucleusModel_SaveType ReadSaveType(this BinaryReader reader) {
		return (NucleusModel_SaveType)reader.ReadUInt16();
	}

	public static void WriteSaveType(this BinaryWriter writer, NucleusModel_SaveType savetype) {
		writer.Write((ushort)savetype);
	}
}

// More efficient binary loader.
public class ModelBinary : IModelFormat
{
	public const string EXTENSION = "nm4b";
	public const string FULL_EXTENSION = $".{EXTENSION}";

	/// <summary>
	/// Pack and write the atlas beside the model when saving
	/// </summary>
	public bool PackAtlas { get; set; } = true;
	public AtlasPackSettings PackSettings { get; set; } = new();
	/// <summary>
	/// The atlas packed by the last save, for its warnings
	/// </summary>
	public PackedAtlas? LastAtlas { get; private set; }

	/// <summary>
	/// First format version with constraints, events, path attachments and linked meshes
	/// </summary>
	public const string VERSION_CONSTRAINTS = "Nucleus Model4 2026.10.27.01";

	private static bool HasConstraints(string? formatVersion) =>
		formatVersion != null && string.CompareOrdinal(formatVersion, VERSION_CONSTRAINTS) >= 0;

	private static BoneData readBone(BinaryReader reader, List<BoneData> workingArray, bool constraints) {
		BoneData bone = new();

		bone.Name = reader.ReadString();
		bone.Index = reader.ReadInt32();
		var parent = reader.ReadInt32();
		bone.Parent = parent == -1 ? null : workingArray[parent];
		bone.Length = reader.ReadSingle();

		bone.TransformMode = (TransformMode)reader.ReadInt32();
		bone.Position = reader.ReadVector2F();
		bone.Rotation = reader.ReadSingle();
		bone.Scale = reader.ReadVector2F();
		bone.Shear = reader.ReadVector2F();
		if (constraints)
			bone.SkinRequired = reader.ReadBoolean();

		return bone;
	}

	private static List<BoneData> readBoneRefs(BinaryReader reader, ModelData modelData) {
		int count = reader.Read7BitEncodedInt();
		List<BoneData> bones = new(count);
		for (int i = 0; i < count; i++)
			bones.Add(reader.ReadIndexThenFetch(modelData.BoneDatas));
		return bones;
	}

	private static void readIntoConstraint(BinaryReader reader, ModelData modelData, ConstraintData constraint) {
		constraint.Name = reader.ReadString();
		constraint.Order = reader.ReadInt32();
		constraint.SkinRequired = reader.ReadBoolean();
		constraint.Bones = readBoneRefs(reader, modelData);
	}

	private static IkConstraintData readIkConstraint(BinaryReader reader, ModelData modelData) {
		IkConstraintData ik = new();
		readIntoConstraint(reader, modelData, ik);
		ik.Target = reader.ReadIndexThenFetch(modelData.BoneDatas);
		ik.BendDirection = reader.ReadInt32();
		ik.Compress = reader.ReadBoolean();
		ik.Stretch = reader.ReadBoolean();
		ik.Uniform = reader.ReadBoolean();
		ik.Mix = reader.ReadSingle();
		ik.Softness = reader.ReadSingle();
		return ik;
	}

	private static TransformConstraintData readTransformConstraint(BinaryReader reader, ModelData modelData) {
		TransformConstraintData transform = new();
		readIntoConstraint(reader, modelData, transform);
		transform.Target = reader.ReadIndexThenFetch(modelData.BoneDatas);
		transform.Local = reader.ReadBoolean();
		transform.Relative = reader.ReadBoolean();
		transform.OffsetRotation = reader.ReadSingle();
		transform.OffsetX = reader.ReadSingle();
		transform.OffsetY = reader.ReadSingle();
		transform.OffsetScaleX = reader.ReadSingle();
		transform.OffsetScaleY = reader.ReadSingle();
		transform.OffsetShearY = reader.ReadSingle();
		transform.RotateMix = reader.ReadSingle();
		transform.TranslateMix = reader.ReadSingle();
		transform.ScaleMix = reader.ReadSingle();
		transform.ShearMix = reader.ReadSingle();
		return transform;
	}

	private static PathConstraintData readPathConstraint(BinaryReader reader, ModelData modelData) {
		PathConstraintData path = new();
		readIntoConstraint(reader, modelData, path);
		path.Target = reader.ReadIndexThenFetch(modelData.SlotDatas);
		path.PositionMode = (PositionMode)reader.ReadInt32();
		path.SpacingMode = (SpacingMode)reader.ReadInt32();
		path.RotateMode = (RotateMode)reader.ReadInt32();
		path.OffsetRotation = reader.ReadSingle();
		path.Position = reader.ReadSingle();
		path.Spacing = reader.ReadSingle();
		path.RotateMix = reader.ReadSingle();
		path.TranslateMix = reader.ReadSingle();
		return path;
	}

	private static EventData readEventData(BinaryReader reader) => new() {
		Name = reader.ReadString(),
		Int = reader.ReadInt32(),
		Float = reader.ReadSingle(),
		String = reader.ReadNullableString(),
		AudioPath = reader.ReadNullableString(),
		Volume = reader.ReadSingle(),
		Balance = reader.ReadSingle()
	};

	// Skin constraint refs: 0 ik, 1 transform, 2 path
	private static ConstraintData readConstraintRef(BinaryReader reader, ModelData modelData) {
		var kind = reader.ReadByte();
		return kind switch {
			0 => (ConstraintData)reader.ReadIndexThenFetch(modelData.IkConstraints),
			1 => reader.ReadIndexThenFetch(modelData.TransformConstraints),
			2 => reader.ReadIndexThenFetch(modelData.PathConstraints),
			_ => throw new NotSupportedException($"Unknown constraint kind '{kind}'")
		};
	}

	private static SlotData readSlot(BinaryReader reader, ModelData workingModelData) {
		SlotData slot = new();

		slot.Index = reader.ReadInt32();
		slot.Name = reader.ReadString();
		slot.BoneData = workingModelData.BoneDatas[reader.ReadInt32()];
		slot.Color = reader.ReadColor();
		slot.DarkColor = reader.ReadNullableColor();
		slot.Attachment = reader.ReadNullableString();
		slot.BlendMode = (BlendMode)reader.ReadInt32();

		return slot;
	}

	private static SkinEntry readSkinEntry(BinaryReader reader) => new(reader.ReadString(), reader.ReadInt32());

	private static AttachmentWeight readMeshAttachmentWeight(BinaryReader reader) => new(reader.ReadInt32(), reader.ReadSingle(), reader.ReadVector2F());

	private static AttachmentVertex readMeshVertex(BinaryReader reader) {
		AttachmentVertex vertex = new() {
			X = reader.ReadSingle(),
			Y = reader.ReadSingle(),
			U = reader.ReadSingle(),
			V = reader.ReadSingle()
		};

		if (reader.ReadBoolean())
			vertex.Weights = reader.ReadArray(readMeshAttachmentWeight);

		return vertex;
	}

	private static AttachmentTriangle readMeshTriangle(BinaryReader reader) => new() {
		V1 = reader.ReadInt32(),
		V2 = reader.ReadInt32(),
		V3 = reader.ReadInt32()
	};

	private static Attachment readAttachment(BinaryReader reader) {
		var name = reader.ReadString();
		var type = reader.ReadSaveType();

		switch (type) {
			case NucleusModel_SaveType.Attachment_Region: {
					RegionAttachment attachment = new() { Name = name };
					attachment.Position = reader.ReadVector2F();
					attachment.Rotation = reader.ReadSingle();
					attachment.Scale = reader.ReadVector2F();
					attachment.Color = reader.ReadColor();
					attachment.Path = reader.ReadString();

					return attachment;
				}
			case NucleusModel_SaveType.Attachment_Mesh: {
					MeshAttachment attachment = new() { Name = name };
					attachment.Vertices = reader.ReadArray(readMeshVertex);
					attachment.Triangles = reader.ReadArray(readMeshTriangle);

					attachment.Position = reader.ReadVector2F();
					attachment.Rotation = reader.ReadSingle();
					attachment.Scale = reader.ReadVector2F();
					attachment.Color = reader.ReadColor();
					attachment.Path = reader.ReadString();

					return attachment;
				}
			case NucleusModel_SaveType.Attachment_Clipping: {
					ClippingAttachment attachment = new() { Name = name };
					attachment.Vertices = reader.ReadArray(readMeshVertex);

					attachment.Position = reader.ReadVector2F();
					attachment.Rotation = reader.ReadSingle();
					attachment.Scale = reader.ReadVector2F();

					attachment.EndSlot = reader.ReadNullableString();

					return attachment;
				}
			case NucleusModel_SaveType.Attachment_Path: {
					PathAttachment attachment = new() { Name = name };
					attachment.Vertices = reader.ReadArray(readMeshVertex);

					attachment.Position = reader.ReadVector2F();
					attachment.Rotation = reader.ReadSingle();
					attachment.Scale = reader.ReadVector2F();

					attachment.Closed = reader.ReadBoolean();
					attachment.ConstantSpeed = reader.ReadBoolean();
					attachment.Lengths = reader.ReadArray(r => r.ReadSingle());
					attachment.Color = reader.ReadColor();

					return attachment;
				}
			case NucleusModel_SaveType.Attachment_LinkedMesh: {
					MeshAttachment attachment = new() { Name = name };
					attachment.ParentKey = reader.ReadString();
					attachment.ParentSkin = reader.ReadNullableString();
					attachment.InheritDeform = reader.ReadBoolean();

					attachment.Position = reader.ReadVector2F();
					attachment.Rotation = reader.ReadSingle();
					attachment.Scale = reader.ReadVector2F();
					attachment.Color = reader.ReadColor();
					attachment.Path = reader.ReadNullableString()!;

					// Own geometry, only when it differs from the parent's
					if (reader.ReadBoolean()) {
						attachment.Vertices = reader.ReadArray(readMeshVertex);
						attachment.Triangles = reader.ReadArray(readMeshTriangle);
					}

					return attachment;
				}
			default: throw new NotImplementedException($"Weird attachment type given '{type}'");
		}
	}

	private static Skin readSkin(BinaryReader reader, ModelData modelData) {
		Skin skin = new();

		skin.Name = reader.ReadString();
		skin.Attachments = reader.ReadDictionary(readSkinEntry, readAttachment);

		var c = reader.ReadInt32();
		skin.Bones.EnsureCapacity(c);
		for (int i = 0; i < c; i++)
			skin.Bones.Add(reader.ReadIndexThenFetch(modelData.BoneDatas));

		if (HasConstraints(modelData.FormatVersion))
			skin.Constraints = reader.ReadList(modelData, readConstraintRef);

		return skin;
	}

	private static Keyframe<string?> readKeyframeStringN(BinaryReader reader) {
		return new() {
			Time = reader.ReadDouble(),
			Value = reader.ReadNullableString()
		};
	}

	private static void readIntoFCurveStringN(BinaryReader reader, FCurve<string?> fcs) {
		fcs.Keyframes = reader.ReadList(readKeyframeStringN);
	}

	private static void readIntoKeyframeFloatHandle(BinaryReader reader, ref KeyframeHandle<float> handle) {
		handle.Time = reader.ReadDouble();
		handle.Value = reader.ReadSingle();
		handle.HandleType = (KeyframeHandleType)reader.ReadInt32();
	}

	private static Keyframe<float> readKeyframeFloat(BinaryReader reader) {
		Keyframe<float> kf = new() {
			Time = reader.ReadDouble(),
			Value = reader.ReadSingle()
		};

		if (reader.ReadBoolean()) {
			KeyframeHandle<float> lf = new();
			readIntoKeyframeFloatHandle(reader, ref lf);
			kf.LeftHandle = lf;
		}

		if (reader.ReadBoolean()) {
			KeyframeHandle<float> rf = new();
			readIntoKeyframeFloatHandle(reader, ref rf);
			kf.RightHandle = rf;
		}

		kf.Easing = (KeyframeEasing)reader.ReadInt32();
		kf.Interpolation = (KeyframeInterpolation)reader.ReadInt32();
		return kf;
	}

	private static void readIntoMonoBoneFloatPropertyTimeline(BinaryReader reader, MonoBoneFloatPropertyTimeline mono) {
		mono.BoneIndex = reader.ReadInt32();
		readIntoFCurveFloat(reader, mono.Curves[0]);
	}

	private static void readIntoDuoBoneFloatPropertyTimeline(BinaryReader reader, DuoBoneFloatPropertyTimeline duo) {
		duo.BoneIndex = reader.ReadInt32();
		readIntoFCurveFloat(reader, duo.Curves[0]);
		readIntoFCurveFloat(reader, duo.Curves[1]);
	}

	private static void readIntoFCurveFloat(BinaryReader reader, FCurve<float> fcf) {
		fcf.Keyframes = reader.ReadList(readKeyframeFloat);
	}

	private static Keyframe<int[]?> readKeyframeDrawOrder(BinaryReader reader) {
		Keyframe<int[]?> keyframe = new() { Time = reader.ReadDouble() };
		keyframe.Value = reader.ReadBoolean() ? reader.ReadArray(r => r.ReadInt32()) : null;
		keyframe.Interpolation = (KeyframeInterpolation)reader.ReadInt32();
		return keyframe;
	}

	private static ModelEvent readEvent(BinaryReader reader, ModelData modelData) => new() {
		Time = reader.ReadDouble(),
		Data = reader.ReadIndexThenFetch(modelData.Events),
		Int = reader.ReadInt32(),
		Float = reader.ReadSingle(),
		String = reader.ReadNullableString(),
		Volume = reader.ReadSingle(),
		Balance = reader.ReadSingle()
	};

	private static void readIntoCurves(BinaryReader reader, CurveTimeline<float> timeline) {
		timeline.NewCurves();
		for (int i = 0; i < timeline.Curves.Length; i++)
			readIntoFCurveFloat(reader, timeline.Curves[i]);
	}

	private static Timeline readTimeline(BinaryReader reader, ModelData modelData) {
		var type = reader.ReadSaveType();
		switch (type) {
			case NucleusModel_SaveType.Timeline_Translate: {
					TranslateTimeline tl = new();
					tl.NewCurves();
					readIntoDuoBoneFloatPropertyTimeline(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_TranslateX: {
					TranslateXTimeline tl = new();
					tl.NewCurves();
					readIntoMonoBoneFloatPropertyTimeline(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_TranslateY: {
					TranslateYTimeline tl = new();
					tl.NewCurves();
					readIntoMonoBoneFloatPropertyTimeline(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_Rotation: {
					RotationTimeline tl = new();
					tl.NewCurves();
					readIntoMonoBoneFloatPropertyTimeline(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_Scale: {
					ScaleTimeline tl = new();
					tl.NewCurves();
					readIntoDuoBoneFloatPropertyTimeline(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_ScaleX: {
					ScaleXTimeline tl = new();
					tl.NewCurves();
					readIntoMonoBoneFloatPropertyTimeline(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_ScaleY: {
					ScaleYTimeline tl = new();
					tl.NewCurves();
					readIntoMonoBoneFloatPropertyTimeline(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_Shear: {
					ShearTimeline tl = new();
					tl.NewCurves();
					readIntoDuoBoneFloatPropertyTimeline(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_ShearX: {
					ShearXTimeline tl = new();
					tl.NewCurves();
					readIntoMonoBoneFloatPropertyTimeline(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_ShearY: {
					ShearYTimeline tl = new();
					tl.NewCurves();
					readIntoMonoBoneFloatPropertyTimeline(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_ActiveAttachment: {
					ActiveAttachmentTimeline tl = new();
					tl.NewCurves();

					tl.SlotIndex = reader.ReadInt32();
					readIntoFCurveStringN(reader, tl.Curves[0]);

					return tl;
				}
			case NucleusModel_SaveType.Timeline_SlotColor4: {
					SlotColor4Timeline tl = new();
					tl.NewCurves();

					tl.SlotIndex = reader.ReadInt32();
					readIntoFCurveFloat(reader, tl.Curves[0]);
					readIntoFCurveFloat(reader, tl.Curves[1]);
					readIntoFCurveFloat(reader, tl.Curves[2]);
					readIntoFCurveFloat(reader, tl.Curves[3]);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_SlotRGB: {
					SlotRGBTimeline tl = new();
					tl.NewCurves();
					tl.SlotIndex = reader.ReadInt32();
					readIntoCurves(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_SlotAlpha: {
					SlotAlphaTimeline tl = new();
					tl.NewCurves();
					tl.SlotIndex = reader.ReadInt32();
					readIntoCurves(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_SlotRGBA2: {
					SlotRGBA2Timeline tl = new();
					tl.NewCurves();
					tl.SlotIndex = reader.ReadInt32();
					readIntoCurves(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_DrawOrder: {
					DrawOrderTimeline tl = new();
					tl.NewCurves();
					tl.Curves[0].Keyframes = reader.ReadList(readKeyframeDrawOrder);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_Deform: {
					DeformTimeline tl = new();
					tl.SlotIndex = reader.ReadInt32();
					tl.Skin = reader.ReadNullableString();
					tl.Attachment = reader.ReadString();
					readIntoFCurveFloat(reader, tl.Curve);
					tl.Offsets = reader.ReadList(r => r.ReadArray(r2 => r2.ReadSingle()));
					return tl;
				}
			case NucleusModel_SaveType.Timeline_IkConstraint: {
					IkConstraintTimeline tl = new();
					tl.ConstraintIndex = reader.ReadInt32();
					readIntoCurves(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_TransformConstraint: {
					TransformConstraintTimeline tl = new();
					tl.ConstraintIndex = reader.ReadInt32();
					readIntoCurves(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_PathConstraintPosition: {
					PathConstraintPositionTimeline tl = new();
					tl.ConstraintIndex = reader.ReadInt32();
					readIntoCurves(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_PathConstraintSpacing: {
					PathConstraintSpacingTimeline tl = new();
					tl.ConstraintIndex = reader.ReadInt32();
					readIntoCurves(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_PathConstraintMix: {
					PathConstraintMixTimeline tl = new();
					tl.ConstraintIndex = reader.ReadInt32();
					readIntoCurves(reader, tl);
					return tl;
				}
			case NucleusModel_SaveType.Timeline_Event: {
					EventTimeline tl = new();
					tl.Events = reader.ReadList(modelData, readEvent);
					return tl;
				}
			default: throw new NotSupportedException($"Unknown timeline type '{type}'");
		}
	}

	private static Animation readAnimation(BinaryReader reader, ModelData modelData) {
		Animation anim = new Animation();

		anim.Duration = reader.ReadDouble();
		anim.Name = reader.ReadString();
		anim.Timelines = reader.ReadList(modelData, readTimeline);

		return anim;
	}

	// Linked meshes without their own geometry share the parent's arrays
	private static void ShareLinkedGeometry(ModelData data) {
		foreach (var skin in data.Skins) {
			foreach (var entry in skin.Attachments) {
				if (entry.Value is not MeshAttachment { IsLinked: true, ParentMesh: MeshAttachment parent } mesh)
					continue;
				mesh.Vertices ??= parent.Vertices;
				mesh.Triangles ??= parent.Triangles;
			}
		}
	}

	private static void MigrateNormalizedHandles(ModelData data) {
		foreach (var animation in data.Animations) {
			foreach (var timeline in animation.Timelines) {
				if (timeline is CurveTimeline<float> curves) {
					foreach (var curve in curves.Curves)
						LegacyCurves.ConvertNormalizedHandles(curve, timeline is MonoBoneRotationPropertyTimeline);
				}
			}
		}
	}

	public ModelData LoadModelFromFile(string pathID, string path) {
		using (Stream? stream = filesystem.Open(pathID, path, FileAccess.Read, FileMode.Open)) {
			if (stream == null) throw new NullReferenceException();
			using (BinaryReader reader = new BinaryReader(stream)) {
				ModelData data = new ModelData();

				data.FormatVersion = reader.ReadString();
				bool compressed = reader.ReadBoolean();
				if (compressed) throw new NotImplementedException("Compression not yet implemented!");
				for (int i = 0; i < 8; i++) reader.ReadInt32(); // placeholder padding bytes, if they're ever needed

				data.Name = reader.ReadNullableString();

				bool constraints = HasConstraints(data.FormatVersion);
				data.BoneDatas = reader.ReadList<BoneData>((r, bones) => readBone(r, bones, constraints));
				data.SlotDatas = reader.ReadList(data, readSlot);
				if (constraints) {
					data.IkConstraints = reader.ReadList(data, readIkConstraint);
					data.TransformConstraints = reader.ReadList(data, readTransformConstraint);
					data.PathConstraints = reader.ReadList(data, readPathConstraint);
					data.Events = reader.ReadList(readEventData);
				}
				data.Skins = reader.ReadList(data, readSkin);
				data.DefaultSkin = reader.ReadIndexThenFetch(data.Skins);
				data.Animations = reader.ReadList(data, readAnimation);
				if (data.FormatVersion == Model4System.MODEL_FORMAT_VERSION_NORMALIZED_HANDLES)
					MigrateNormalizedHandles(data);

				data.ResolveLinkedMeshes();
				ShareLinkedGeometry(data);

				var atlas = ModelAtlasFiles.Load(
					filesystem.ReadAllText(pathID, Path.ChangeExtension(path, ".texatlas")),
					page => filesystem.ReadAllBytes(pathID, Path.Combine(Path.GetDirectoryName(path) ?? "", page))
				);
				if (atlas == null)
					return data;

				data.TextureAtlas = atlas;
				data.SetupAttachments();
				return data;
			}
		}
	}

	private static void writeBone(BinaryWriter writer, BoneData bone) {
		writer.Write(bone.Name);
		writer.Write(bone.Index);
		writer.Write(bone.Parent?.Index ?? -1);
		writer.Write(bone.Length);

		writer.Write((int)bone.TransformMode);
		writer.Write(bone.Position);
		writer.Write(bone.Rotation);
		writer.Write(bone.Scale);
		writer.Write(bone.Shear);
		writer.Write(bone.SkinRequired);
	}

	private static void writeConstraint(ModelData modelData, BinaryWriter writer, ConstraintData constraint) {
		writer.Write(constraint.Name);
		writer.Write(constraint.Order);
		writer.Write(constraint.SkinRequired);
		writer.Write7BitEncodedInt(constraint.Bones.Count);
		foreach (var bone in constraint.Bones)
			writer.WriteIndexOf(modelData.BoneDatas, bone);
	}

	private static void writeIkConstraint(ModelData modelData, BinaryWriter writer, IkConstraintData ik) {
		writeConstraint(modelData, writer, ik);
		writer.WriteIndexOf(modelData.BoneDatas, ik.Target);
		writer.Write(ik.BendDirection);
		writer.Write(ik.Compress);
		writer.Write(ik.Stretch);
		writer.Write(ik.Uniform);
		writer.Write(ik.Mix);
		writer.Write(ik.Softness);
	}

	private static void writeTransformConstraint(ModelData modelData, BinaryWriter writer, TransformConstraintData transform) {
		writeConstraint(modelData, writer, transform);
		writer.WriteIndexOf(modelData.BoneDatas, transform.Target);
		writer.Write(transform.Local);
		writer.Write(transform.Relative);
		writer.Write(transform.OffsetRotation);
		writer.Write(transform.OffsetX);
		writer.Write(transform.OffsetY);
		writer.Write(transform.OffsetScaleX);
		writer.Write(transform.OffsetScaleY);
		writer.Write(transform.OffsetShearY);
		writer.Write(transform.RotateMix);
		writer.Write(transform.TranslateMix);
		writer.Write(transform.ScaleMix);
		writer.Write(transform.ShearMix);
	}

	private static void writePathConstraint(ModelData modelData, BinaryWriter writer, PathConstraintData path) {
		writeConstraint(modelData, writer, path);
		writer.WriteIndexOf(modelData.SlotDatas, path.Target);
		writer.Write((int)path.PositionMode);
		writer.Write((int)path.SpacingMode);
		writer.Write((int)path.RotateMode);
		writer.Write(path.OffsetRotation);
		writer.Write(path.Position);
		writer.Write(path.Spacing);
		writer.Write(path.RotateMix);
		writer.Write(path.TranslateMix);
	}

	private static void writeEventData(BinaryWriter writer, EventData eventData) {
		writer.Write(eventData.Name);
		writer.Write(eventData.Int);
		writer.Write(eventData.Float);
		writer.WriteNullableString(eventData.String);
		writer.WriteNullableString(eventData.AudioPath);
		writer.Write(eventData.Volume);
		writer.Write(eventData.Balance);
	}

	private static void writeConstraintRef(ModelData modelData, BinaryWriter writer, ConstraintData constraint) {
		switch (constraint) {
			case IkConstraintData ik:
				writer.Write((byte)0);
				writer.WriteIndexOf(modelData.IkConstraints, ik);
				break;
			case TransformConstraintData transform:
				writer.Write((byte)1);
				writer.WriteIndexOf(modelData.TransformConstraints, transform);
				break;
			case PathConstraintData path:
				writer.Write((byte)2);
				writer.WriteIndexOf(modelData.PathConstraints, path);
				break;
			default:
				throw new NotSupportedException($"Unknown constraint type '{constraint.GetType()}'");
		}
	}

	private static void writeSlot(ModelData modelData, BinaryWriter writer, SlotData slot) {
		writer.Write(slot.Index);
		writer.Write(slot.Name);
		writer.WriteIndexOf(modelData.BoneDatas, slot.BoneData);
		writer.Write(slot.Color);
		writer.Write(slot.DarkColor);
		writer.WriteNullableString(slot.Attachment);
		writer.Write((int)slot.BlendMode);
	}

	private static void writeSkinEntry(BinaryWriter writer, SkinEntry skinEntry) {
		writer.Write(skinEntry.Name.String()!);
		writer.Write(skinEntry.SlotIndex);
	}

	private static void writeMeshAttachmentWeight(BinaryWriter writer, AttachmentWeight vweight) {
		writer.Write(vweight.Bone);
		writer.Write(vweight.Weight);
		writer.Write(vweight.Position);
	}

	private static void writeMeshVertex(BinaryWriter writer, AttachmentVertex vertex) {
		writer.Write(vertex.X);
		writer.Write(vertex.Y);
		writer.Write(vertex.U);
		writer.Write(vertex.V);
		if (vertex.Weights == null) {
			writer.Write(false);
		}
		else {
			writer.Write(true);
			writer.WriteArray(vertex.Weights, writeMeshAttachmentWeight);
		}
	}

	private static void writeMeshTriangle(BinaryWriter writer, AttachmentTriangle triangle) {
		writer.Write(triangle.V1);
		writer.Write(triangle.V2);
		writer.Write(triangle.V3);
	}

	private static void writeAttachment(BinaryWriter writer, Attachment attachment) {
		writer.Write(attachment.Name);
		switch (attachment) {
			case RegionAttachment regionAttachment:
				writer.WriteSaveType(NucleusModel_SaveType.Attachment_Region);
				writer.Write(regionAttachment.Position);
				writer.Write(regionAttachment.Rotation);
				writer.Write(regionAttachment.Scale);
				writer.Write(regionAttachment.Color);
				writer.Write(regionAttachment.Path);
				break;

			case MeshAttachment { IsLinked: true } linkedMesh: {
					writer.WriteSaveType(NucleusModel_SaveType.Attachment_LinkedMesh);
					writer.Write(linkedMesh.ParentKey!);
					writer.WriteNullableString(linkedMesh.ParentSkin);
					writer.Write(linkedMesh.InheritDeform);
					writer.Write(linkedMesh.Position);
					writer.Write(linkedMesh.Rotation);
					writer.Write(linkedMesh.Scale);
					writer.Write(linkedMesh.Color);
					writer.WriteNullableString(linkedMesh.Path);

					var parent = linkedMesh.ParentMesh;
					bool ownGeometry = linkedMesh.Vertices != null && linkedMesh.Triangles != null
						&& (parent == null || !ReferenceEquals(linkedMesh.Vertices, parent.Vertices) || !ReferenceEquals(linkedMesh.Triangles, parent.Triangles));
					writer.Write(ownGeometry);
					if (ownGeometry) {
						writer.WriteArray(linkedMesh.Vertices!, writeMeshVertex);
						writer.WriteArray(linkedMesh.Triangles!, writeMeshTriangle);
					}
					break;
				}

			case PathAttachment pathAttachment:
				writer.WriteSaveType(NucleusModel_SaveType.Attachment_Path);
				writer.WriteArray(pathAttachment.Vertices, writeMeshVertex);
				writer.Write(pathAttachment.Position);
				writer.Write(pathAttachment.Rotation);
				writer.Write(pathAttachment.Scale);
				writer.Write(pathAttachment.Closed);
				writer.Write(pathAttachment.ConstantSpeed);
				writer.WriteArray(pathAttachment.Lengths, (w, length) => w.Write(length));
				writer.Write(pathAttachment.Color);
				break;

			case MeshAttachment meshAttachment:
				writer.WriteSaveType(NucleusModel_SaveType.Attachment_Mesh);
				writer.WriteArray(meshAttachment.Vertices, writeMeshVertex);
				writer.WriteArray(meshAttachment.Triangles, writeMeshTriangle);
				writer.Write(meshAttachment.Position);
				writer.Write(meshAttachment.Rotation);
				writer.Write(meshAttachment.Scale);
				writer.Write(meshAttachment.Color);
				writer.Write(meshAttachment.Path);
				break;

			case ClippingAttachment clippingAttachment:
				writer.WriteSaveType(NucleusModel_SaveType.Attachment_Clipping);
				writer.WriteArray(clippingAttachment.Vertices, writeMeshVertex);
				writer.Write(clippingAttachment.Position);
				writer.Write(clippingAttachment.Rotation);
				writer.Write(clippingAttachment.Scale);
				writer.WriteNullableString(clippingAttachment.EndSlot);
				break;

			default:
				throw new NotSupportedException($"Unknown attachment type '{attachment.GetType()}'");
		}
	}

	private static void writeSkin(ModelData modelData, BinaryWriter writer, Skin skin) {
		writer.Write(skin.Name);
		writer.WriteDictionary(skin.Attachments, writeSkinEntry, writeAttachment);
		writer.Write(skin.Bones.Count);
		foreach (var bone in skin.Bones) writer.WriteIndexOf(modelData.BoneDatas, bone);
		writer.WriteList(skin.Constraints, (w, constraint) => writeConstraintRef(modelData, w, constraint));
	}

	private static void writeKeyframeStringN(BinaryWriter writer, Keyframe<string?> kf) {
		writer.Write(kf.Time);
		writer.WriteNullableString(kf.Value);

		// We can skip everything else, since strings are not interpolated ever
	}

	private static void writeFCurveStringN(BinaryWriter writer, FCurve<string?> fcf) {
		writer.WriteList(fcf.Keyframes, writeKeyframeStringN);
	}

	private static void writeKeyframeFloatHandle(BinaryWriter writer, ref KeyframeHandle<float> kfh) {
		writer.Write(kfh.Time);
		writer.Write(kfh.Value);
		writer.Write((int)kfh.HandleType);
	}

	private static void writeKeyframeFloat(BinaryWriter writer, Keyframe<float> kf) {
		writer.Write(kf.Time);
		writer.Write(kf.Value);

		if (!kf.LeftHandle.HasValue)
			writer.Write(false);
		else {
			var handle = kf.LeftHandle.Value;
			writer.Write(true);
			writeKeyframeFloatHandle(writer, ref handle);
		}

		if (!kf.RightHandle.HasValue)
			writer.Write(false);
		else {
			var handle = kf.RightHandle.Value;
			writer.Write(true);
			writeKeyframeFloatHandle(writer, ref handle);
		}

		writer.Write((int)kf.Easing);
		writer.Write((int)kf.Interpolation);
	}

	private static void writeFCurveFloat(BinaryWriter writer, FCurve<float> fcf) {
		writer.WriteList(fcf.Keyframes, writeKeyframeFloat);
	}

	private static void writeKeyframeDrawOrder(BinaryWriter writer, Keyframe<int[]?> keyframe) {
		writer.Write(keyframe.Time);
		writer.Write(keyframe.Value != null);
		if (keyframe.Value != null)
			writer.WriteArray(keyframe.Value, (w, index) => w.Write(index));
		writer.Write((int)keyframe.Interpolation);
	}

	private static void writeEvent(ModelData modelData, BinaryWriter writer, ModelEvent modelEvent) {
		writer.Write(modelEvent.Time);
		writer.WriteIndexOf(modelData.Events, modelEvent.Data);
		writer.Write(modelEvent.Int);
		writer.Write(modelEvent.Float);
		writer.WriteNullableString(modelEvent.String);
		writer.Write(modelEvent.Volume);
		writer.Write(modelEvent.Balance);
	}

	private static void writeCurves(BinaryWriter writer, CurveTimeline<float> timeline) {
		for (int i = 0; i < timeline.Curves.Length; i++)
			writeFCurveFloat(writer, timeline.Curves[i]);
	}

	private static void writeTimeline(ModelData modelData, BinaryWriter writer, Timeline timeline) {
		writer.WriteSaveType(timeline switch {
			TranslateTimeline => NucleusModel_SaveType.Timeline_Translate,
			TranslateXTimeline => NucleusModel_SaveType.Timeline_TranslateX,
			TranslateYTimeline => NucleusModel_SaveType.Timeline_TranslateY,
			RotationTimeline => NucleusModel_SaveType.Timeline_Rotation,
			ScaleTimeline => NucleusModel_SaveType.Timeline_Scale,
			ScaleXTimeline => NucleusModel_SaveType.Timeline_ScaleX,
			ScaleYTimeline => NucleusModel_SaveType.Timeline_ScaleY,
			ShearTimeline => NucleusModel_SaveType.Timeline_Shear,
			ShearXTimeline => NucleusModel_SaveType.Timeline_ShearX,
			ShearYTimeline => NucleusModel_SaveType.Timeline_ShearY,
			ActiveAttachmentTimeline => NucleusModel_SaveType.Timeline_ActiveAttachment,
			SlotColor4Timeline => NucleusModel_SaveType.Timeline_SlotColor4,
			SlotRGBTimeline => NucleusModel_SaveType.Timeline_SlotRGB,
			SlotAlphaTimeline => NucleusModel_SaveType.Timeline_SlotAlpha,
			SlotRGBA2Timeline => NucleusModel_SaveType.Timeline_SlotRGBA2,
			DrawOrderTimeline => NucleusModel_SaveType.Timeline_DrawOrder,
			DeformTimeline => NucleusModel_SaveType.Timeline_Deform,
			IkConstraintTimeline => NucleusModel_SaveType.Timeline_IkConstraint,
			TransformConstraintTimeline => NucleusModel_SaveType.Timeline_TransformConstraint,
			PathConstraintPositionTimeline => NucleusModel_SaveType.Timeline_PathConstraintPosition,
			PathConstraintSpacingTimeline => NucleusModel_SaveType.Timeline_PathConstraintSpacing,
			PathConstraintMixTimeline => NucleusModel_SaveType.Timeline_PathConstraintMix,
			EventTimeline => NucleusModel_SaveType.Timeline_Event,
			_ => throw new NotSupportedException($"Unknown timeline type '{timeline.GetType()}'")
		});

		switch (timeline) {
			case MonoBoneFloatPropertyTimeline duo:
				writer.Write(duo.BoneIndex);
				writeFCurveFloat(writer, duo.Curves[0]);
				break;

			case DuoBoneFloatPropertyTimeline duo:
				writer.Write(duo.BoneIndex);
				writeFCurveFloat(writer, duo.Curves[0]);
				writeFCurveFloat(writer, duo.Curves[1]);
				break;

			case SlotColor4Timeline sc4:
				writer.Write(sc4.SlotIndex);
				writeFCurveFloat(writer, sc4.Curves[0]);
				writeFCurveFloat(writer, sc4.Curves[1]);
				writeFCurveFloat(writer, sc4.Curves[2]);
				writeFCurveFloat(writer, sc4.Curves[3]);
				break;

			case SlotRGBTimeline rgb:
				writer.Write(rgb.SlotIndex);
				writeCurves(writer, rgb);
				break;

			case SlotAlphaTimeline alpha:
				writer.Write(alpha.SlotIndex);
				writeCurves(writer, alpha);
				break;

			case SlotRGBA2Timeline rgba2:
				writer.Write(rgba2.SlotIndex);
				writeCurves(writer, rgba2);
				break;

			case ActiveAttachmentTimeline duo:
				writer.Write(duo.SlotIndex);
				writeFCurveStringN(writer, duo.Curves[0]);
				break;

			case DrawOrderTimeline drawOrder:
				writer.WriteList(drawOrder.Curves[0].Keyframes, writeKeyframeDrawOrder);
				break;

			case DeformTimeline deform:
				writer.Write(deform.SlotIndex);
				writer.WriteNullableString(deform.Skin);
				writer.Write(deform.Attachment);
				writeFCurveFloat(writer, deform.Curve);
				writer.WriteList(deform.Offsets, (w, offsets) => w.WriteArray(offsets, (w2, value) => w2.Write(value)));
				break;

			case IkConstraintTimeline ik:
				writer.Write(ik.ConstraintIndex);
				writeCurves(writer, ik);
				break;

			case TransformConstraintTimeline transform:
				writer.Write(transform.ConstraintIndex);
				writeCurves(writer, transform);
				break;

			case PathConstraintPositionTimeline position:
				writer.Write(position.ConstraintIndex);
				writeCurves(writer, position);
				break;

			case PathConstraintSpacingTimeline spacing:
				writer.Write(spacing.ConstraintIndex);
				writeCurves(writer, spacing);
				break;

			case PathConstraintMixTimeline mix:
				writer.Write(mix.ConstraintIndex);
				writeCurves(writer, mix);
				break;

			case EventTimeline events:
				writer.WriteList(events.Events, (w, ev) => writeEvent(modelData, w, ev));
				break;

			default:
				throw new NotImplementedException();
		}
	}

	private static void writeAnimation(ModelData modelData, BinaryWriter writer, Animation anim) {
		writer.Write(anim.Duration);
		writer.Write(anim.Name);
		writer.WriteList(anim.Timelines, (w, timeline) => writeTimeline(modelData, w, timeline));
	}

	public void SaveModelToFile(string absoluteFilePath, ModelData modelData) {
		using (FileStream stream = File.Open(absoluteFilePath, FileMode.Create, FileAccess.Write))
		using (BinaryWriter writer = new BinaryWriter(stream)) {
			writer.Write(Model4System.MODEL_FORMAT_VERSION);
			writer.Write(false); // todo: compression, this is the flag for that
								 // if compressed; would include a string to specify compression algorithm after
			for (int i = 0; i < 8; i++) writer.Write(0); // placeholder padding bytes, if they're ever needed. 8 * 4 = 32 bytes
			writer.WriteNullableString(modelData.Name);

			writer.WriteList(modelData.BoneDatas, writeBone);
			writer.WriteList(modelData.SlotDatas, (writer, slot) => writeSlot(modelData, writer, slot));
			writer.WriteList(modelData.IkConstraints, (w, ik) => writeIkConstraint(modelData, w, ik));
			writer.WriteList(modelData.TransformConstraints, (w, transform) => writeTransformConstraint(modelData, w, transform));
			writer.WriteList(modelData.PathConstraints, (w, path) => writePathConstraint(modelData, w, path));
			writer.WriteList(modelData.Events, writeEventData);
			writer.WriteList(modelData.Skins, (writer, slot) => writeSkin(modelData, writer, slot));
			writer.WriteIndexOf(modelData.Skins, modelData.DefaultSkin);
			writer.WriteList(modelData.Animations, (w, anim) => writeAnimation(modelData, w, anim));
		}

		LastAtlas = PackAtlas ? ModelAtlasFiles.SaveNextTo(absoluteFilePath, modelData, PackSettings) : null;
	}
}

// Basic JSON model loader. Uses Newtonsoft's references to deserialize properly.
// The editor does something similar but with EditorModel instead.
public class ModelRefJSON : IModelFormat
{
	public const string EXTENSION = "nm4rj";
	public const string FULL_EXTENSION = $".{EXTENSION}";

	/// <summary>
	/// Pack and write the atlas beside the model when saving
	/// </summary>
	public bool PackAtlas { get; set; } = true;
	public AtlasPackSettings PackSettings { get; set; } = new();
	/// <summary>
	/// The atlas packed by the last save, for its warnings
	/// </summary>
	public PackedAtlas? LastAtlas { get; private set; }

	public class ModelRefJsonSerializationBinder : ISerializationBinder
	{
		private static HashSet<Type> ApprovedBindables;

		static ModelRefJsonSerializationBinder() {
			ApprovedBindables = [];

			// Build the type whitelist. Has to be done for security reasons
			// Includes all abstract implementors of attachments and timelines.
			// Those types will then include their typename in the JSON so they can
			// be deserialized properly

			foreach (var attachmentType in typeof(Attachment).GetInheritorsOfAbstractType())
				ApprovedBindables.Add(attachmentType);

			foreach (var timelineType in typeof(Timeline).GetInheritorsOfAbstractType())
				ApprovedBindables.Add(timelineType);
		}

		public Type BindToType(string? assemblyName, string typeName) {
			var resolvedTypeName = $"{typeName}, {assemblyName}";

			var type = Type.GetType(resolvedTypeName, true);
			if (!ApprovedBindables.Contains(type))
				throw new JsonSerializationException($"Type is not approved for serialization. Typename: ${resolvedTypeName}");

			return type;
		}

		public void BindToName(Type serializedType, out string? assemblyName, out string? typeName) {
			assemblyName = null;
			typeName = serializedType.AssemblyQualifiedName;
		}
	}

	public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings() {
		ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
		PreserveReferencesHandling = PreserveReferencesHandling.Objects,
		SerializationBinder = new ModelRefJsonSerializationBinder(),
		NullValueHandling = NullValueHandling.Ignore,
		TypeNameHandling = TypeNameHandling.Auto
	};

	public ModelData LoadModelFromFile(string pathID, string path) {
		var text = filesystem.ReadAllText(pathID, path);
		if (text == null) throw new FileNotFoundException($"Cannot find '{path}' in {pathID}");

		var data = JsonConvert.DeserializeObject<ModelData>(text, Settings);
		if (data == null)
			throw new FormatException("Issue occured during deserialization of a Model4-refjson");

		var atlas = ModelAtlasFiles.Load(
			filesystem.ReadAllText(pathID, Path.ChangeExtension(path, ".texatlas")),
			page => filesystem.ReadAllBytes(pathID, Path.Combine(Path.GetDirectoryName(path) ?? "", page))
		);
		if (atlas == null)
			return data;

		data.TextureAtlas = atlas;
		data.SetupAttachments();
		return data;
	}

	public void SaveModelToFile(string filepath, ModelData data) {
		var serialized = JsonConvert.SerializeObject(data, Settings);
		File.WriteAllText(filepath, serialized);
		LastAtlas = PackAtlas ? ModelAtlasFiles.SaveNextTo(filepath, data, PackSettings) : null;
	}
}