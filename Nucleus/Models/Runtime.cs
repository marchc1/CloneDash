// This *might* get separated into separate files per type soon, but for now, it's simpler
// to just have it all be in one place.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Nucleus.Commands;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.ManagedMemory;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.Util;

using Raylib_cs;

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Nucleus.Models.Runtime;

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
	public const string MODEL_FORMAT_VERSION = "Nucleus Model4 2025.04.28.01";

	public static ConVar m4s_wireframe = new(nameof(m4s_wireframe), "0", FCvar.Saved, "Model4 instance wireframe overlay.", 0, 1);
	public const double REFERENCE_FPS = 30;


	public static T? SearchName<T>(List<T> list, ReadOnlySpan<char> name) where T : class, IModel4Nameable {
		Span<T> items = list.AsSpan();
		T? item = null;
		for (int i = 0, c = items.Length; i < c; i++)
			if ((item = items[i]).Name.Equals(name, StringComparison.InvariantCulture))
				return item;

		return null;
	}
	public static int SearchSlot<T>(List<T> list, ReadOnlySpan<char> name) where T : class, IModel4Nameable {
		Span<T> items = list.AsSpan();
		T? item = null;
		for (int i = 0, c = items.Length; i < c; i++)
			if ((item = items[i]).Name.Equals(name, StringComparison.InvariantCulture))
				return i;

		return -1;
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

	[JsonIgnore] public TextureAtlasSystem TextureAtlas { get; set; }

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

		instance.SetToSetupPose();

		return instance;
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


	public Animation? FindAnimation(ReadOnlySpan<char> name) => Model4System.SearchName(Animations, name);
	public BoneData? FindBone(ReadOnlySpan<char> name) => Model4System.SearchName(BoneDatas, name);
	public Skin? FindSkin(ReadOnlySpan<char> name) => Model4System.SearchName(Skins, name);
	public SlotData? FindSlot(ReadOnlySpan<char> name) => Model4System.SearchName(SlotDatas, name);

	public int FindBoneIndex(ReadOnlySpan<char> name) => Model4System.SearchSlot(BoneDatas, name);
	public int FindSlotIndex(ReadOnlySpan<char> name) => Model4System.SearchSlot(BoneDatas, name);

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

	public RuntimeClipper Clipping;

	public BoneInstance? RootBone => Bones.Count > 0 ? Bones[0] : null;

	public Skin Skin { get; set; }
	public Vector2F Position { get; set; }
	public Vector2F Scale { get; set; } = new(1, 1);
	public bool FlipX { get; set; }
	public bool FlipY { get; set; }
	public TextureAtlasSystem TextureAtlas => Data.TextureAtlas;

	private Transformation worldTransform;
	public Transformation WorldTransform {
		get => worldTransform;
	}

	public ModelInstance() {
		Clipping = new(this);
	}

	public void Render(bool useDefaultShader = true) {
		var offset = Graphics2D.Offset;
		Graphics2D.ResetDrawingOffset();
		Rlgl.PushMatrix();
		Rlgl.Translatef(Position.X, Position.Y, 0);
		Rlgl.Scalef(Scale.X, Scale.Y, 0);

		foreach (var bone in Bones) {
			bone.UpdateWorldTransform();
		}

		foreach (var slot in DrawOrder) {
			var attachment = slot.Attachment;
			if (attachment == null) {
				Clipping.NextSlot(slot);
				continue;
			}

			attachment.Render(slot);
			Clipping.NextSlot(slot);
			slot.EndBlendMode();
		}

		/*int index = 0;
		foreach (var bone in Bones) {
			var test = bone.LocalToWorld(0, 0);
			Raylib.DrawCircleV(new(test.X, -test.Y), 4, Color.Red);
			Graphics2D.DrawText(new(test.X, -test.Y), $"[{index}] {bone.Name}", "Consolas", 48);
			index++;
		}*/

		Clipping.End();
		Rlgl.PopMatrix();
		Graphics2D.OffsetDrawing(offset);

		if (Model4System.m4s_wireframe.GetBool()) {
			foreach (var bone in Bones) {
				Raylib.DrawCircleV(bone.WorldTransform.LocalToWorld(0, 0).ToNumerics() * new System.Numerics.Vector2(1, -1), 4, Color.Red);
			}
		}
	}

	public void SetToSetupPose() {
		SetBonesToSetupPose();
		SetSlotsToSetupPose();
	}

	public void SetAttachment(SlotInstance slot, string name) {

	}

	public BoneInstance? FindBone(ReadOnlySpan<char> name) => Model4System.SearchName(Bones, name);
	public SlotInstance? FindSlot(ReadOnlySpan<char> name) => Model4System.SearchName(Slots, name);
	public int FindBoneIndex(ReadOnlySpan<char> name) => Model4System.SearchSlot(Bones, name);
	public int FindSlotIndex(ReadOnlySpan<char> name) => Model4System.SearchSlot(Slots, name);


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
	}

	public void SetSlotsToSetupPose() {
		DrawOrder.Clear();

		foreach (var slot in Slots)
			DrawOrder.Add(slot);

		foreach (var slot in Slots)
			slot.SetToSetupPose();
	}

	public void SetSkin(string skinName) => throw new NotImplementedException();
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
}
public class BoneInstance : IContainsSetupPose, IModelInstanceObject, IModel4Nameable
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
	public void UpdateWorldTransform(in Vector2F pos, float rot, in Vector2F scale, in Vector2F shear)
		=> WorldTransform = Transformation.CalculateWorldTransformation(in pos, rot, in scale, in shear, TransformMode, Parent?.WorldTransform ?? null);
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

	public Attachment? Attachment;
	public BoneInstance Bone;
	public Color Color;
	public Color? DarkColor;

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
	}

	public void SetAttachment(string? value) => Attachment = value == null ? null : Model.GetAttachment(Index, value);

	public void StartBlendModeFor(Attachment attachment) {
		Raylib.BeginBlendMode(BlendMode switch {
			BlendMode.Normal => (A < 255 || attachment.Alpha < 255) ? Raylib_cs.BlendMode.BLEND_ALPHA : Raylib_cs.BlendMode.BLEND_ALPHA_PREMULTIPLY,
			BlendMode.Additive => Raylib_cs.BlendMode.BLEND_ADDITIVE,
			BlendMode.Multiply => Raylib_cs.BlendMode.BLEND_MULTIPLIED,
			BlendMode.Screen => Raylib_cs.BlendMode.BLEND_ALPHA, // need to implement this in a shader I believe
			_ => throw new Exception($"Unsupported blend mode! (got {BlendMode})")
		});
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
[TypeConverter(typeof(SkinEntryTypeConverter))] public record SkinEntry(UtlSymbol Name, int SlotIndex);
// iirc, records handle this automatically; saving in case they don't
// public override int GetHashCode() => HashCode.Combine(Name, SlotIndex);

public class Skin : IModel4Nameable
{
	public string Name { get; set; }
	public Dictionary<SkinEntry, Attachment> Attachments { get; set; } = [];
	public List<BoneData> Bones { get; set; } = [];

	public void AddSkin(Skin skin) {
		foreach (var attachment in skin.Attachments) Attachments.Add(attachment.Key, attachment.Value);
		foreach (var bone in skin.Bones) Bones.Add(bone);
	}

	public void Clear() {
		Attachments.Clear();
		Bones.Clear();
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

	public void Apply(ModelInstance? model, double time, float mix = 1, MixBlendMode mixBlend = MixBlendMode.Setup) {
		if (model == null)
			return;

		foreach (var tl in Timelines) {
			// todo: lasttime
			tl.Apply(model, 0, time, mix, mixBlend);
		}
	}
}

public abstract class Attachment : IModel4Nameable
{
	public string Name { get; set; }
	public virtual void Render(SlotInstance slot) {

	}
	public virtual void Setup(ModelData data) { }

	public virtual byte Alpha => 255;
}

public class RegionAttachment : Attachment
{
	public Vector2F Position;
	public Vector2F Scale;
	public float Rotation;
	public Color Color = Color.White;

	public string Path;
	[JsonIgnore] public AtlasRegion Region;
	[JsonIgnore] public bool InitializedRegion;

	public override byte Alpha => Color.A;

	public override void Setup(ModelData data) {
		bool OK = data.TextureAtlas.TryGetTextureRegion(Path, out Region);
		Debug.Assert(OK, "TextureAtlas couldn't find the texture region!");
	}

	public override void Render(SlotInstance slot) {
		slot.StartBlendModeFor(this);
		var bone = slot.Bone;
		var worldTransform = Transformation.CalculateWorldTransformation(Position, Rotation, Scale, Vector2F.Zero, TransformMode.Normal, slot.Bone.WorldTransform);

		var region = Region;
		if (!region.IsValid()) {
			Setup(slot.Bone.Model.Data);
			if (!Region.IsValid()) return;
			region = Region;
		}

		var tex = slot.Bone.Model.TextureAtlas.Texture;
		float width = region.H, height = region.W;
		float widthDiv2 = width / 2, heightDiv2 = height / 2;

		Vector2F TL = worldTransform.LocalToWorld(-heightDiv2, widthDiv2);
		Vector2F TR = worldTransform.LocalToWorld(heightDiv2, widthDiv2);
		Vector2F BR = worldTransform.LocalToWorld(heightDiv2, -widthDiv2);
		Vector2F BL = worldTransform.LocalToWorld(-heightDiv2, -widthDiv2);

		TL.Y *= -1;
		TR.Y *= -1;
		BL.Y *= -1;
		BR.Y *= -1;

		Rlgl.DisableBackfaceCulling();

		Rlgl.Begin(DrawMode.TRIANGLES);
		Rlgl.SetTexture(tex.HardwareID);

		var color = slot.Color;
		float srM = slot.Color.R / 255f, sgM = slot.Color.G / 255f, sbM = slot.Color.B / 255f, saM = slot.Color.A / 255f;
		float arM = Color.R / 255f, agM = Color.G / 255f, abM = Color.B / 255f, aaM = Color.A / 255f;

		Rlgl.Color4f(srM * arM, sgM * agM, sbM * abM, saM * aaM);

		float uStart, uEnd, vStart, vEnd;
		uStart = (float)region.X / tex.Width;
		uEnd = uStart + ((float)region.W / tex.Width);

		vStart = (float)region.Y / tex.Height;
		vEnd = vStart + ((float)region.H / tex.Height);

		var clipping = slot.Model.Clipping;

		Vector2F t1v1 = BL, t1v2 = TR, t1v3 = TL;
		Vector2F uv1v1 = new(uStart, vEnd), uv1v2 = new(uEnd, vStart), uv1v3 = new(uStart, vStart);
		Rlgl.TexCoord2f(uv1v1.X, uv1v1.Y); Rlgl.Vertex2f(t1v1.X, t1v1.Y);
		Rlgl.TexCoord2f(uv1v2.X, uv1v2.Y); Rlgl.Vertex2f(t1v2.X, t1v2.Y);
		Rlgl.TexCoord2f(uv1v3.X, uv1v3.Y); Rlgl.Vertex2f(t1v3.X, t1v3.Y);

		Vector2F t2v1 = BR, t2v2 = TR, t2v3 = BL;
		Vector2F uv2v1 = new(uEnd, vEnd), uv2v2 = new(uEnd, vStart), uv2v3 = new(uStart, vEnd);
		Rlgl.TexCoord2f(uv2v1.X, uv2v1.Y); Rlgl.Vertex2f(t2v1.X, t2v1.Y);
		Rlgl.TexCoord2f(uv2v2.X, uv2v2.Y); Rlgl.Vertex2f(t2v2.X, t2v2.Y);
		Rlgl.TexCoord2f(uv2v3.X, uv2v3.Y); Rlgl.Vertex2f(t2v3.X, t2v3.Y);

		Rlgl.End();

		if (Model4System.m4s_wireframe.GetBool()) {
			Rlgl.DrawRenderBatchActive();
			Rlgl.Begin(DrawMode.LINES);
			Rlgl.SetTexture(0);

			Rlgl.Vertex2f(BL.X, BL.Y); Rlgl.Vertex2f(TR.X, TR.Y);
			Rlgl.Vertex2f(TR.X, TR.Y); Rlgl.Vertex2f(TL.X, TL.Y);
			Rlgl.Vertex2f(TL.X, TL.Y); Rlgl.Vertex2f(BL.X, BL.Y);
			Rlgl.Vertex2f(BR.X, BR.Y); Rlgl.Vertex2f(TR.X, TR.Y);
			Rlgl.Vertex2f(TR.X, TR.Y); Rlgl.Vertex2f(BL.X, BL.Y);
			Rlgl.Vertex2f(BL.X, BL.Y); Rlgl.Vertex2f(BR.X, BR.Y);

			Rlgl.End();
			Rlgl.DrawRenderBatchActive();
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

	[JsonIgnore] public AtlasRegion Region;
	[JsonIgnore] public bool InitializedRegion;

	protected Vector2F CalculateVertexWorldPosition(SlotInstance slot, int vertexI) {
		var vertex = Vertices![vertexI];
		var bone = slot.Bone;
		var transform = slot.Bone.WorldTransform;

		if (vertex.Weights == null || vertex.Weights.Length <= 0)
			return transform.LocalToWorld(vertex.X, vertex.Y);

		var model = slot.GetModel();

		Vector2F pos = Vector2F.Zero;

		foreach (var weightData in vertex.Weights) {
			if (weightData.IsEmpty) continue;
			var vertLocalPos = weightData.Position;
			var weight = weightData.Weight;
			pos += model.Bones[weightData.Bone].WorldTransform.LocalToWorld(vertLocalPos) * weight;
		}

		return pos;
	}

	public int ComputeWorldVertices(SlotInstance slot, int startAt, int length, Vector2F[] worldVertices, int offset) {
		for (int i = startAt; i < length; i++) {
			worldVertices[i + offset] = CalculateVertexWorldPosition(slot, i);
		}

		return length - startAt;
	}

	public int ComputeWorldVerticesInto(SlotInstance slot, Vector2F[] worldVertices)
		=> ComputeWorldVertices(slot, 0, Vertices.Length, worldVertices, 0);
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
	public MeshAttachment? ParentMesh;

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
		data.TextureAtlas.TryGetTextureRegion(Path, out Region);
	}

	public AttachmentVertex[] GetVertices() => ParentMesh?.Vertices ?? Vertices;
	public AttachmentTriangle[] GetTriangles() => ParentMesh?.Triangles ?? Triangles;

	public override void Render(SlotInstance slot) {
		var vertices = GetVertices();
		var triangles = GetTriangles();

		Debug.Assert(vertices != null);
		if (vertices == null)
			return;

		Debug.Assert(triangles != null);
		if (triangles == null)
			return;

		slot.StartBlendModeFor(this);

		var region = Region;

		//Debug.Assert(region.IsValid());
		if (!region.IsValid()) {
			Setup(slot.Bone.Model.Data);
			if (!Region.IsValid()) return;
			region = Region;
		}

		var worldTransform = Transformation.CalculateWorldTransformation(Position, Rotation, Scale, Vector2F.Zero, TransformMode.Normal, slot.Bone.WorldTransform);

		float width = region.H, height = region.W;
		float widthDiv2 = width / 2, heightDiv2 = height / 2;
		ManagedMemory.Texture tex = slot.Bone.Model.TextureAtlas.Texture;

		Rlgl.Begin(DrawMode.TRIANGLES);
		Rlgl.SetTexture(tex.HardwareID);

		var color = slot.Color;
		float srM = slot.Color.R / 255f, sgM = slot.Color.G / 255f, sbM = slot.Color.B / 255f, saM = slot.Color.A / 255f;
		float arM = Color.R / 255f, agM = Color.G / 255f, abM = Color.B / 255f, aaM = Color.A / 255f;

		Rlgl.DisableBackfaceCulling();
		Rlgl.Color4f(srM * arM, sgM * agM, sbM * abM, saM * aaM);
		if (triangles.Length > 0) {
			float uStart, uEnd, vStart, vEnd;
			uStart = (float)region.X / (float)tex.Width;
			uEnd = uStart + ((float)region.W / (float)tex.Width);

			vStart = ((float)region.Y / (float)tex.Height);
			vEnd = vStart + ((float)region.H / (float)tex.Height);

			bool block = false;
			foreach (var tri in triangles) {
				var av1 = vertices[tri.V1];
				var av2 = vertices[tri.V2];
				var av3 = vertices[tri.V3];

				float u1 = (float)NMath.Remap(av1.U, 0, 1, region.X, region.X + region.W), v1 = (float)NMath.Remap(av1.V, 1, 0, region.Y, region.Y + region.H);
				float u2 = (float)NMath.Remap(av2.U, 0, 1, region.X, region.X + region.W), v2 = (float)NMath.Remap(av2.V, 1, 0, region.Y, region.Y + region.H);
				float u3 = (float)NMath.Remap(av3.U, 0, 1, region.X, region.X + region.W), v3 = (float)NMath.Remap(av3.V, 1, 0, region.Y, region.Y + region.H);

				u1 /= tex.Width; u2 /= tex.Width; u3 /= tex.Width;
				v1 /= tex.Height; v2 /= tex.Height; v3 /= tex.Height;

				Vector2F p1 = CalculateVertexWorldPosition(slot, tri.V1);
				Vector2F p2 = CalculateVertexWorldPosition(slot, tri.V2);
				Vector2F p3 = CalculateVertexWorldPosition(slot, tri.V3);

				Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(p1.X, -p1.Y, 0);
				Rlgl.TexCoord2f(u2, v2); Rlgl.Vertex3f(p2.X, -p2.Y, 0);
				Rlgl.TexCoord2f(u3, v3); Rlgl.Vertex3f(p3.X, -p3.Y, 0);
			}
		}

		Rlgl.End();

		if (Model4System.m4s_wireframe.GetBool()) {
			Rlgl.DrawRenderBatchActive();
			Rlgl.Begin(DrawMode.LINES);
			Rlgl.SetTexture(0);

			foreach (var tri in triangles) {
				var av1 = vertices[tri.V1];
				var av2 = vertices[tri.V2];
				var av3 = vertices[tri.V3];

				float u1 = (float)NMath.Remap(av1.U, 0, 1, region.X, region.X + region.W), v1 = (float)NMath.Remap(av1.V, 1, 0, region.Y, region.Y + region.H);
				float u2 = (float)NMath.Remap(av2.U, 0, 1, region.X, region.X + region.W), v2 = (float)NMath.Remap(av2.V, 1, 0, region.Y, region.Y + region.H);
				float u3 = (float)NMath.Remap(av3.U, 0, 1, region.X, region.X + region.W), v3 = (float)NMath.Remap(av3.V, 1, 0, region.Y, region.Y + region.H);

				u1 /= tex.Width; u2 /= tex.Width; u3 /= tex.Width;
				v1 /= tex.Height; v2 /= tex.Height; v3 /= tex.Height;

				Vector2F p1 = CalculateVertexWorldPosition(slot, tri.V1);
				Vector2F p2 = CalculateVertexWorldPosition(slot, tri.V2);
				Vector2F p3 = CalculateVertexWorldPosition(slot, tri.V3);

				Rlgl.Vertex3f(p1.X, -p1.Y, 0); Rlgl.Vertex3f(p2.X, -p2.Y, 0);
				Rlgl.Vertex3f(p2.X, -p2.Y, 0); Rlgl.Vertex3f(p3.X, -p3.Y, 0);
				Rlgl.Vertex3f(p3.X, -p3.Y, 0); Rlgl.Vertex3f(p1.X, -p1.Y, 0);
			}

			Rlgl.End();
			Rlgl.DrawRenderBatchActive();
		}
	}
}

public class ClippingAttachment : VertexAttachment, IClipPolygon<SlotInstance>
{
	public string? EndSlot = null;

	public int GetVerticesCount() => Vertices.Length;
	public override void Render(SlotInstance slot) {
		base.Render(slot);
		// Start the model clipper
		slot.Model.Clipping.Start(this, slot, EndSlot);
	}
}


// Get ready for interface hell here; but most of it is for good reason...

public abstract class Timeline
{
	public abstract void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend, MixDirection dir = MixDirection.Out);
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
	public const float ROT_WRAP = 16384f;
	public const float ROT_WRAP_MIDWAY = 16384.498046875f; // ^141 +255
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float PERFORM_ROT_WRAP(float r) => (ROT_WRAP - (int)(ROT_WRAP_MIDWAY - r / 360)) * 360;

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
					Set(bone, Get(bone) + (r - PERFORM_ROT_WRAP(r)) * (float)mix);
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
					r -= PERFORM_ROT_WRAP(r);
					goto case MixBlendMode.Add;
				case MixBlendMode.Add:
					Set(bone, Get(bone) + (r * (float)mix));
					return;
				default: return;
			}

		// Some more manual work has to be done for rotation delta..
		var curve = Curve(0);
		int frame = curve.BinarySearchKeyframe(time);
		float p = curve[frame].Value;
		float percentage = (float)curve.GetPercentage(frame, time);

		float delta = (curve.Count == 1 ? curve[frame] : curve[frame + 1]).Value - p;
		delta -= PERFORM_ROT_WRAP(delta);
		r = p + delta * percentage;

		switch (blend) {
			case MixBlendMode.Setup:
				Set(bone, GetSetup(bone) + (r - PERFORM_ROT_WRAP(r)) * (float)mix);
				break;
			case MixBlendMode.First:
			case MixBlendMode.Replace:
				r += GetSetup(bone) - Get(bone);
				goto case MixBlendMode.Add;
			case MixBlendMode.Add:
				Set(bone, Get(bone) + (r - PERFORM_ROT_WRAP(r)) * (float)mix);
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
				x = Curve(0).DetermineValueAtTime(time) * GetSetup(bone).X;
				y = Curve(1).DetermineValueAtTime(time) * GetSetup(bone).Y;
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
				x = Curve(0).DetermineValueAtTime(time);
				y = Curve(1).DetermineValueAtTime(time);
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
			r = Curve(0).DetermineValueAtTime(time);
			g = Curve(1).DetermineValueAtTime(time);
			b = Curve(2).DetermineValueAtTime(time);
			a = Curve(3).DetermineValueAtTime(time);
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


public class AnimationChannelEntry
{
	public Animation Animation;
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

	public void ElapseTime(double deltaTime) {
		Time += deltaTime;
		ElapsedTime += deltaTime;
	}

	public void ResetTime() {
		Time = 0;
		ElapsedTime = 0;
	}

	public void EnqueueNext() {
		// Enqueue the next animation
		if (QueuedEntries.TryDequeue(out AnimationChannelEntry? newAnim)) {
			CurrentEntry = newAnim;
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
	ModelData? model;
	public AnimationHandler() {
		for (int i = 0; i < Channels.Length; i++) {
			Channels[i] = new();
		}
	}

	public void SetModel(ModelData? data) {
		if (model == data)
			return;
		model = data;
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

			channel.ElapseTime(time);
			var anim = channel.CurrentEntry;
			if (anim == null) {
				if (channel.QueuedEntries.TryDequeue(out AnimationChannelEntry? newAnim)) {
					channel.CurrentEntry = newAnim;
					channel.ResetTime();
					anim = newAnim;
				}
				else continue;
			}

			if (channel.Time >= anim.Animation.Duration) {
				if (anim.Looping)
					channel.Time = channel.Time % anim.Animation.Duration;
				else
					channel.EnqueueNext();
			}

			if (anim.LimitedLoop && channel.ElapsedTime >= anim.LoopDuration)
				channel.EnqueueNext();
		}
	}

	public void AddAnimation(int channel, string? animation, bool loops = false, double loopDuration = -1) {
		if (animation == null)
			return;

		var channelObj = Channels[channel];

		var anim = model?.FindAnimation(animation);
		if (anim == null) return;

		channelObj.QueuedEntries.Enqueue(new() {
			Animation = anim,
			Looping = loops,
			LoopDuration = loopDuration
		});
	}

	public void SetAnimation(int channel, string? animation, bool loops = false, double loopDuration = -1) {
		if (animation == null)
			return;

		var channelObj = Channels[channel];
		StopAnimation(channel);

		var anim = model?.FindAnimation(animation);
		if (anim == null) return;

		channelObj.QueuedEntries.Clear();
		channelObj.QueuedEntries.Enqueue(new() {
			Animation = anim,
			Looping = loops,
			LoopDuration = loopDuration
		});
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

	public void Apply(ModelInstance model) {
		bool first = true;
		foreach (var channel in Channels) {
			if (channel.CurrentEntry == null) continue;

			MixBlendMode blending = first ? MixBlendMode.First : channel.Blending;
			float mix = channel.Mix;
			channel.CurrentEntry.Animation.Apply(model, channel.Time, mix, blending);
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

	private static BoneData readBone(BinaryReader reader, List<BoneData> workingArray) {
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

		return bone;
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
	private static Timeline readTimeline(BinaryReader reader) {
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
			default: throw new NotSupportedException($"Unknown timeline type '{type}'");
		}
	}
	private static Animation readAnimation(BinaryReader reader) {
		Animation anim = new Animation();

		anim.Duration = reader.ReadDouble();
		anim.Name = reader.ReadString();
		anim.Timelines = reader.ReadList(readTimeline);

		return anim;
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

				data.BoneDatas = reader.ReadList<BoneData>(readBone);
				data.SlotDatas = reader.ReadList(data, readSlot);
				data.Skins = reader.ReadList(data, readSkin);
				data.DefaultSkin = reader.ReadIndexThenFetch(data.Skins);
				data.Animations = reader.ReadList(readAnimation);

				// Try to load the texture atlas
				string? texatlas = filesystem.ReadAllText(pathID, Path.ChangeExtension(path, ".texatlas"));
				byte[]? imagedata = filesystem.ReadAllBytes(pathID, Path.ChangeExtension(path, ".png"));

				if (texatlas == null && imagedata == null)
					return data;

				if (texatlas == null)
					throw new NullReferenceException("Got image data, expected texture atlas as well");
				if (imagedata == null)
					throw new NullReferenceException("Got texture atlas, expected image data as well");

				data.TextureAtlas = new();
				data.TextureAtlas.Load(texatlas, imagedata);

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
		}
	}

	private static void writeSkin(ModelData modelData, BinaryWriter writer, Skin skin) {
		writer.Write(skin.Name);
		writer.WriteDictionary(skin.Attachments, writeSkinEntry, writeAttachment);
		writer.Write(skin.Bones.Count);
		foreach (var bone in skin.Bones) writer.WriteIndexOf(modelData.BoneDatas, bone);
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

	private static void writeTimeline(BinaryWriter writer, Timeline timeline) {
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
			case ActiveAttachmentTimeline duo:
				writer.Write(duo.SlotIndex);
				writeFCurveStringN(writer, duo.Curves[0]);
				break;
			default:
				throw new NotImplementedException();
		}
	}
	private static void writeAnimation(BinaryWriter writer, Animation anim) {
		writer.Write(anim.Duration);
		writer.Write(anim.Name);
		writer.WriteList(anim.Timelines, writeTimeline);
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
			writer.WriteList(modelData.Skins, (writer, slot) => writeSkin(modelData, writer, slot));
			writer.WriteIndexOf(modelData.Skins, modelData.DefaultSkin);
			writer.WriteList(modelData.Animations, writeAnimation);
		}

		modelData.TextureAtlas.SaveTo(absoluteFilePath);
	}
}

// Basic JSON model loader. Uses Newtonsoft's references to deserialize properly.
// The editor does something similar but with EditorModel instead.
public class ModelRefJSON : IModelFormat
{
	public const string EXTENSION = "nm4rj";
	public const string FULL_EXTENSION = $".{EXTENSION}";
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

		// Try to load the texture atlas
		string? texatlas = filesystem.ReadAllText(pathID, Path.ChangeExtension(path, ".texatlas"));
		byte[]? imagedata = filesystem.ReadAllBytes(pathID, Path.ChangeExtension(path, ".png"));

		if (texatlas == null && imagedata == null)
			return data;

		if (texatlas == null)
			throw new NullReferenceException("Got image data, expected texture atlas as well");
		if (imagedata == null)
			throw new NullReferenceException("Got texture atlas, expected image data as well");

		data.TextureAtlas = new();
		data.TextureAtlas.Load(texatlas, imagedata);

		data.SetupAttachments();

		return data;
	}

	public void SaveModelToFile(string filepath, ModelData data) {
		var serialized = JsonConvert.SerializeObject(data, Settings);
		File.WriteAllText(filepath, serialized);
		data.TextureAtlas.SaveTo(filepath);
	}
}