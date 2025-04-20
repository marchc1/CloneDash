// This *might* get separated into separate files per type soon, but for now, it's simpler
// to just have it all be in one place.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

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
	public const string MODEL_FORMAT_VERSION = "Nucleus Model4 2025.04.13.01";
	public const string MODEL_FORMAT_REFJSON_EXT = ".nm4rj";

	public static ConVar m4s_wireframe = ConVar.Register("m4s_wireframe", "0", ConsoleFlags.Saved, "Model4 instance wireframe overlay.", 0, 1);
}


public interface IContainsSetupPose
{
	public void SetToSetupPose();
}
public interface IModelInstanceObject
{
	public ModelInstance GetModel();
}



public class ModelData : IDisposable
{
	public void Dispose() {
		MainThread.RunASAP(() => { TextureAtlas?.ClearTextures(); });
	}
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
	[JsonIgnore] public TextureAtlasSystem TextureAtlas { get; set; }


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

	public Animation? FindAnimation(string name) => Animations.FirstOrDefault(x => x.Name == name);
	public BoneData? FindBone(string name) => BoneDatas.FirstOrDefault(x => x.Name == name);
	public Skin? FindSkin(string name) => Skins.FirstOrDefault(x => x.Name == name);
	public SlotData? FindSlot(string name) => SlotDatas.FirstOrDefault(x => x.Name == name);

	public int FindBoneIndex(string slot) => BoneDatas.FirstOrDefaultIndex(x => x.Name == slot);
	public int FindSlotIndex(string slot) => SlotDatas.FirstOrDefaultIndex(x => x.Name == slot);
}

public class ModelInstance : IContainsSetupPose
{
	public ModelData Data { get; set; }
	public List<BoneInstance> Bones { get; set; } = [];
	public List<SlotInstance> Slots { get; set; } = [];
	public List<SlotInstance> DrawOrder { get; set; } = [];

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

	public void Render() {
		var offset = Graphics2D.Offset;
		Graphics2D.ResetDrawingOffset();
		Rlgl.PushMatrix();
		Rlgl.Translatef(Position.X, Position.Y, 0);
		Rlgl.Scalef(Scale.X, Scale.Y, 0);

		foreach (var bone in Bones)
			bone.UpdateWorldTransform();

		foreach (var slot in DrawOrder) {
			var attachment = slot.Attachment;
			if (attachment == null) continue;

			attachment.Render(slot);
		}
		foreach (var bone in Bones) {
			var test = bone.LocalToWorld(0, 0);
			//Raylib.DrawCircleV(new(test.X, -test.Y), 4, Color.Red);
			//Graphics2D.DrawText(new(test.X, -test.Y), $"rot: {bone.Rotation}", "Consolas", 7);
		}

		Rlgl.PopMatrix();
		Graphics2D.OffsetDrawing(offset);
	}

	public void SetToSetupPose() {
		SetBonesToSetupPose();
		SetSlotsToSetupPose();
	}

	public void SetAttachment(SlotInstance slot, string name) {

	}

	public BoneInstance? FindBone(string name) => Bones.FirstOrDefault(x => x.Name == name);
	public SlotInstance? FindSlot(string name) => Slots.FirstOrDefault(x => x.Name == name);
	public int FindBoneIndex(string name) => Bones.FirstOrDefaultIndex(x => x.Name == name);
	public int FindSlotIndex(string name) => Slots.FirstOrDefaultIndex(x => x.Name == name);


	public Attachment? GetAttachment(int slot, string name) {
		Attachment? attachment;
		if (Skin?.TryGetAttachment(slot, name, out attachment) ?? false)
			return attachment;

		if (Data.DefaultSkin?.TryGetAttachment(slot, name, out attachment) ?? false)
			return attachment;

		return null;
	}
	public Attachment? GetAttachment(string slotName, string attachmentName) => GetAttachment(Data.FindSlotIndex(slotName), attachmentName);

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
	Setup
}

public class BoneData
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
public class BoneInstance : IContainsSetupPose, IModelInstanceObject
{
	public ModelInstance GetModel() => Model;
	public ModelInstance Model { get; set; }

	public string Name => Data.Name;
	public BoneData Data { get; set; }

	public BoneInstance? Parent { get; set; }
	public List<BoneInstance> Children { get; set; } = [];
	public Transformation WorldTransform;
	public TransformMode TransformMode { get; set; }

	public Vector2F Position { get; set; }
	public float Rotation { get; set; }
	public Vector2F Scale { get; set; }
	public Vector2F Shear { get; set; }


	public void SetToSetupPose() {
		Position = Data.Position;
		Rotation = Data.Rotation;
		Scale = Data.Scale;
		Shear = Data.Shear;

		TransformMode = Data.TransformMode;

		UpdateWorldTransform();
	}

	public Vector2F WorldToLocal(float x, float y) => WorldTransform.WorldToLocal(x, y);
	public Vector2F WorldToLocal(Vector2F xy) => WorldTransform.WorldToLocal(xy);
	public float WorldToLocalRotation(float rot) => WorldTransform.WorldToLocalRotation(rot);

	public Vector2F LocalToWorld(float x, float y) => WorldTransform.LocalToWorld(x, y);
	public Vector2F LocalToWorld(Vector2F xy) => WorldTransform.LocalToWorld(xy);
	public float LocalToWorldRotation(float rot) => WorldTransform.LocalToWorldRotation(rot);

	public void UpdateWorldTransform()
		=> UpdateWorldTransform(Position, Rotation, Scale, Shear);
	public void UpdateWorldTransform(Vector2F pos, float rot, Vector2F scale, Vector2F shear)
		=> WorldTransform = Transformation.CalculateWorldTransformation(pos, rot, scale, shear, TransformMode, Parent?.WorldTransform ?? null);
}
public class SlotData
{
	public int Index { get; set; }
	public string Name { get; set; } = "";
	public BoneData BoneData { get; set; }
	public Color Color { get; set; }
	// not yet implemented
	public Color? DarkColor { get; set; }
	public string? Attachment { get; set; }
	public BlendMode BlendMode { get; set; }
}
public class SlotInstance : IContainsSetupPose
{
	public ModelInstance GetModel() => Model;
	public ModelInstance Model { get; set; }

	public string Name => Data.Name;
	public int Index => Data.Index;
	public SlotData Data { get; set; }

	public Attachment? Attachment { get; set; }
	public BoneInstance Bone { get; set; }
	public Color Color { get; set; }
	public Color? DarkColor { get; set; }

	public byte R => Color.R;
	public byte G => Color.G;
	public byte B => Color.B;
	public byte A => Color.A;

	public void SetToSetupPose() {
		Color = Data.Color;
		DarkColor = Data.DarkColor;

		Attachment = Data.Attachment == null ? null : Bone.Model.GetAttachment(Index, Data.Attachment);
	}

	public void SetAttachment(string? value) => Attachment = value == null ? null : Model.GetAttachment(Index, value);
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
[TypeConverter(typeof(SkinEntryTypeConverter))] public record SkinEntry(string Name, int SlotIndex);
// iirc, records handle this automatically; saving in case they don't
// public override int GetHashCode() => HashCode.Combine(Name, SlotIndex);

public class Skin
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

	public Attachment? GetAttachment(int slot, string name) {
		if (Attachments.TryGetValue(new(name, slot), out Attachment? attachment))
			return attachment;
		return null;
	}

	public bool TryGetAttachment(int slot, string name, [NotNullWhen(true)] out Attachment? attachment) {
		return Attachments.TryGetValue(new(name, slot), out attachment);
	}

	public bool GetAttachments(int slot, List<SkinEntry> attachments) {
		var lastCount = attachments.Count;

		foreach (var attachment in Attachments)
			if (attachment.Key.SlotIndex == slot)
				attachments.Add(attachment.Key);

		return attachments.Count > lastCount;
	}

	public void SetAttachment(int slot, string name, Attachment attachment) {
		Attachments[new(name, slot)] = attachment;
	}
}

public class Animation
{
	public double Duration { get; set; }
	public string Name { get; set; }
	public List<Timeline> Timelines { get; set; } = [];

	public void Apply(ModelInstance model, double time, float mix = 1, MixBlendMode mixBlend = MixBlendMode.Setup) {
		foreach (var tl in Timelines) {
			// todo: lasttime
			tl.Apply(model, 0, time, mix, mixBlend);
		}
	}
}

public abstract class Attachment
{
	public string Name { get; set; }
	public virtual void Render(SlotInstance slot) {

	}
}

public class RegionAttachment : Attachment
{
	public Vector2F Position;
	public Vector2F Scale;
	public float Rotation;
	public Color Color = Color.White;
	public AtlasRegion Region;

	public override void Render(SlotInstance slot) {
		var bone = slot.Bone;
		var worldTransform = Transformation.CalculateWorldTransformation(Position, Rotation, Scale, Vector2F.Zero, TransformMode.Normal, slot.Bone.WorldTransform);

		var region = Region;

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

		Rlgl.Color4ub(slot.R, slot.G, slot.B, slot.A);

		float uStart, uEnd, vStart, vEnd;
		uStart = (float)region.X / tex.Width;
		uEnd = uStart + ((float)region.W / tex.Width);

		vStart = (float)region.Y / tex.Height;
		vEnd = vStart + ((float)region.H / tex.Height);

		Rlgl.TexCoord2f(uStart, vEnd); Rlgl.Vertex2f(BL.X, BL.Y);
		Rlgl.TexCoord2f(uEnd, vStart); Rlgl.Vertex2f(TR.X, TR.Y);
		Rlgl.TexCoord2f(uStart, vStart); Rlgl.Vertex2f(TL.X, TL.Y);

		Rlgl.TexCoord2f(uEnd, vEnd);   Rlgl.Vertex2f(BR.X, BR.Y);
		Rlgl.TexCoord2f(uEnd, vStart); Rlgl.Vertex2f(TR.X, TR.Y);
		Rlgl.TexCoord2f(uStart, vEnd); Rlgl.Vertex2f(BL.X, BL.Y);

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


public record MeshAttachmentWeight(int Bone, float Weight, Vector2F Position)
{
	public bool IsEmpty => Weight == 0;
}
public class MeshVertex
{
	public float X;
	public float Y;
	public float U;
	public float V;
	public MeshAttachmentWeight[]? Weights;
}
public class MeshTriangle
{
	public int V1;
	public int V2;
	public int V3;
}

public class MeshAttachment : Attachment
{
	public MeshVertex[] Vertices;
	public MeshTriangle[] Triangles;
	public AtlasRegion Region;

	public Vector2F Position;
	public float Rotation;
	public Vector2F Scale;

	private Vector2F CalculateVertexWorldPosition(ModelInstance model, Transformation transform, MeshVertex vertex) {
		if (vertex.Weights == null || vertex.Weights.Length <= 0)
			return transform.LocalToWorld(vertex.X, vertex.Y);

		Vector2F pos = Vector2F.Zero;
		foreach (var weightData in vertex.Weights) {
			if (weightData.IsEmpty) continue;
			var vertLocalPos = weightData.Position;
			var weight = weightData.Weight;
			pos += model.Bones[weightData.Bone].WorldTransform.LocalToWorld(vertLocalPos) * weight;
		}

		return pos;
	}


	public override void Render(SlotInstance slot) {
		Debug.Assert(Vertices != null); if (Vertices == null) return;
		Debug.Assert(Triangles != null); if (Triangles == null) return;

		var region = Region;

		Debug.Assert(region.IsValid());
		if (!region.IsValid()) return;

		var worldTransform = Transformation.CalculateWorldTransformation(Position, Rotation, Scale, Vector2F.Zero, TransformMode.Normal, slot.Bone.WorldTransform);

		float width = region.H, height = region.W;
		float widthDiv2 = width / 2, heightDiv2 = height / 2;
		ManagedMemory.Texture tex = slot.Bone.Model.TextureAtlas.Texture;

		Rlgl.Begin(DrawMode.TRIANGLES);
		Rlgl.SetTexture(tex.HardwareID);

		var color = slot.Color;

		Rlgl.DisableBackfaceCulling();
		Rlgl.Color4ub(color.R, color.G, color.B, color.A);
		if (Triangles.Length > 0) {
			float uStart, uEnd, vStart, vEnd;
			uStart = (float)region.X / (float)tex.Width;
			uEnd = uStart + ((float)region.W / (float)tex.Width);

			vStart = ((float)region.Y / (float)tex.Height);
			vEnd = vStart + ((float)region.H / (float)tex.Height);

			bool block = false;
			foreach (var tri in Triangles) {
				var av1 = Vertices[tri.V1];
				var av2 = Vertices[tri.V2];
				var av3 = Vertices[tri.V3];

				float u1 = (float)NMath.Remap(av1.U, 0, 1, region.X, region.X + region.W), v1 = (float)NMath.Remap(av1.V, 1, 0, region.Y, region.Y + region.H);
				float u2 = (float)NMath.Remap(av2.U, 0, 1, region.X, region.X + region.W), v2 = (float)NMath.Remap(av2.V, 1, 0, region.Y, region.Y + region.H);
				float u3 = (float)NMath.Remap(av3.U, 0, 1, region.X, region.X + region.W), v3 = (float)NMath.Remap(av3.V, 1, 0, region.Y, region.Y + region.H);

				u1 /= tex.Width; u2 /= tex.Width; u3 /= tex.Width;
				v1 /= tex.Height; v2 /= tex.Height; v3 /= tex.Height;

				Vector2F p1 = CalculateVertexWorldPosition(slot.Model, worldTransform, av1);
				Vector2F p2 = CalculateVertexWorldPosition(slot.Model, worldTransform, av2);
				Vector2F p3 = CalculateVertexWorldPosition(slot.Model, worldTransform, av3);

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

			foreach (var tri in Triangles) {
				var av1 = Vertices[tri.V1];
				var av2 = Vertices[tri.V2];
				var av3 = Vertices[tri.V3];

				float u1 = (float)NMath.Remap(av1.U, 0, 1, region.X, region.X + region.W), v1 = (float)NMath.Remap(av1.V, 1, 0, region.Y, region.Y + region.H);
				float u2 = (float)NMath.Remap(av2.U, 0, 1, region.X, region.X + region.W), v2 = (float)NMath.Remap(av2.V, 1, 0, region.Y, region.Y + region.H);
				float u3 = (float)NMath.Remap(av3.U, 0, 1, region.X, region.X + region.W), v3 = (float)NMath.Remap(av3.V, 1, 0, region.Y, region.Y + region.H);

				u1 /= tex.Width; u2 /= tex.Width; u3 /= tex.Width;
				v1 /= tex.Height; v2 /= tex.Height; v3 /= tex.Height;

				Vector2F p1 = CalculateVertexWorldPosition(slot.Model, worldTransform, av1);
				Vector2F p2 = CalculateVertexWorldPosition(slot.Model, worldTransform, av2);
				Vector2F p3 = CalculateVertexWorldPosition(slot.Model, worldTransform, av3);

				Rlgl.Vertex3f(p1.X, -p1.Y, 0); Rlgl.Vertex3f(p2.X, -p2.Y, 0);
				Rlgl.Vertex3f(p2.X, -p2.Y, 0); Rlgl.Vertex3f(p3.X, -p3.Y, 0);
				Rlgl.Vertex3f(p3.X, -p3.Y, 0); Rlgl.Vertex3f(p1.X, -p1.Y, 0);
			}

			Rlgl.End();
			Rlgl.DrawRenderBatchActive();
		}
	}
}

// Get ready for interface hell here; but most of it is for good reason...

public abstract class Timeline
{
	public abstract void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend);
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

public abstract class MonoBoneFloatPropertyTimeline(bool multiplicative, bool rotation) : CurveTimeline<float>(1), IBoneTimeline
{
	public int BoneIndex { get; set; }

	public abstract float Get(BoneInstance bone);
	public abstract float GetSetup(BoneInstance bone);
	public abstract void Set(BoneInstance bone, float value);

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend) {
		var bone = this.Bone(model);
		if (BeforeFirstFrame(time))
			switch (blend) {
				case MixBlendMode.Setup: Set(bone, GetSetup(bone)); return;
				default: return;
			}

		if (AfterLastFrame(time, out float r))
			switch (blend) {
				case MixBlendMode.Setup: Set(bone, multiplicative ? (GetSetup(bone) * r) : (GetSetup(bone) + r)); return;
				default: return;
			}

		r = Curve(0).DetermineValueAtTime(time);
		switch (blend) {
			case MixBlendMode.Setup:
				if (multiplicative) r = (GetSetup(bone) * r);
				else if (rotation) {
					r = (GetSetup(bone) + r);
				}
				else r = (GetSetup(bone) + r);

				Set(bone, r);
				break;
		}
	}
}
public abstract class DuoBoneFloatPropertyTimeline(bool multiplicative) : CurveTimeline<float>(2), IBoneTimeline
{
	public int BoneIndex { get; set; }

	public abstract Vector2F Get(BoneInstance bone);
	public abstract Vector2F GetSetup(BoneInstance bone);
	public abstract void Set(BoneInstance bone, Vector2F value);

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend) {
		var bone = this.Bone(model);
		if (BeforeFirstFrame(time))
			switch (blend) {
				case MixBlendMode.Setup: Set(bone, GetSetup(bone)); return;
				default: return;
			}

		float x, y;
		Vector2F xy;

		if (AfterLastFrame(time)) {
			x = Curve(0).Last?.Value ?? (multiplicative ? 1 : 0);
			y = Curve(1).Last?.Value ?? (multiplicative ? 1 : 0);
			xy = new(x, y);
			switch (blend) {
				case MixBlendMode.Setup: Set(bone, multiplicative ? (GetSetup(bone) * xy) : (GetSetup(bone) + xy)); return;
				default: return;
			}
		}

		x = Curve(0).DetermineValueAtTime(time);
		y = Curve(1).DetermineValueAtTime(time);
		xy = new(x, y);

		switch (blend) {
			case MixBlendMode.Setup: Set(bone, multiplicative ? (GetSetup(bone) * xy) : (GetSetup(bone) + xy)); break;
		}
	}
}

public class SlotColor4Timeline() : CurveTimeline<float>(4), ISlotTimeline
{
	public int SlotIndex { get; set; }

	public Color Get(SlotInstance slot) => slot.Color;
	public Color GetSetup(SlotInstance slot) => slot.Data.Color;
	public void Set(SlotInstance slot, Color value) => slot.Color = value;

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend) {
		var slot = this.Slot(model);
		if (BeforeFirstFrame(time))
			switch (blend) {
				case MixBlendMode.Setup: Set(slot, GetSetup(slot)); return;
				default: return;
			}

		float r, g, b, a;
		Color rgba;

		if (AfterLastFrame(time)) {
			r = Curve(0).Last?.Value ?? 100;
			g = Curve(1).Last?.Value ?? 100;
			b = Curve(2).Last?.Value ?? 100;
			a = Curve(3).Last?.Value ?? 100;

			r = Math.Clamp(r * 2.55f, 0, 255);
			g = Math.Clamp(g * 2.55f, 0, 255);
			b = Math.Clamp(b * 2.55f, 0, 255);
			a = Math.Clamp(a * 2.55f, 0, 255);

			rgba = new((byte)r, (byte)g, (byte)b, (byte)a);
			switch (blend) {
				case MixBlendMode.Setup: Set(slot, rgba); return;
				default: return;
			}
		}

		r = Curve(0).DetermineValueAtTime(time);
		g = Curve(1).DetermineValueAtTime(time);
		b = Curve(2).DetermineValueAtTime(time);
		a = Curve(3).DetermineValueAtTime(time);

		r = Math.Clamp(r * 2.55f, 0, 255);
		g = Math.Clamp(g * 2.55f, 0, 255);
		b = Math.Clamp(b * 2.55f, 0, 255);
		a = Math.Clamp(a * 2.55f, 0, 255);

		rgba = new((byte)r, (byte)g, (byte)b, (byte)a);

		switch (blend) {
			case MixBlendMode.Setup: Set(slot, rgba); break;
		}
	}
}

public class ActiveAttachmentTimeline() : CurveTimeline<string?>(1), ISlotTimeline
{
	public int SlotIndex { get; set; }

	public string? Get(SlotInstance slot) => slot.Attachment?.Name;
	public string? GetSetup(SlotInstance slot) => slot.Data.Attachment;
	public void Set(SlotInstance slot, string? value) => slot.SetAttachment(value);

	public override void Apply(ModelInstance model, double lastTime, double time, double mix, MixBlendMode blend) {
		var slot = this.Slot(model);
		if (BeforeFirstFrame(time))
			switch (blend) {
				case MixBlendMode.Setup: Set(slot, GetSetup(slot)); return;
				default: return;
			}

		string? attachmentID;
		Color rgba;

		if (AfterLastFrame(time)) {
			attachmentID = Curve(0).Last?.Value ?? null;
			switch (blend) {
				case MixBlendMode.Setup: Set(slot, attachmentID); return;
				default: return;
			}
		}

		attachmentID = Curve(0).DetermineValueAtTime(time);

		switch (blend) {
			case MixBlendMode.Setup: Set(slot, attachmentID); break;
		}
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


public class RotationTimeline() : MonoBoneFloatPropertyTimeline(false, true)
{
	public override float Get(BoneInstance bone) => bone.Rotation;
	public override float GetSetup(BoneInstance bone) => bone.Data.Rotation;
	public override void Set(BoneInstance bone, float value) => bone.Rotation = value;
}
public class TranslateXTimeline() : MonoBoneFloatPropertyTimeline(false, false)
{
	public override float Get(BoneInstance bone) => bone.Position.X;
	public override float GetSetup(BoneInstance bone) => bone.Data.Position.X;
	public override void Set(BoneInstance bone, float value) => bone.Position = new(value, bone.Position.Y);
}
public class TranslateYTimeline() : MonoBoneFloatPropertyTimeline(false, false)
{
	public override float Get(BoneInstance bone) => bone.Position.Y;
	public override float GetSetup(BoneInstance bone) => bone.Data.Position.Y;
	public override void Set(BoneInstance bone, float value) => bone.Position = new(bone.Position.X, value);
}
public class ScaleXTimeline() : MonoBoneFloatPropertyTimeline(true, false)
{
	public override float Get(BoneInstance bone) => bone.Scale.X;
	public override float GetSetup(BoneInstance bone) => bone.Data.Scale.X;
	public override void Set(BoneInstance bone, float value) => bone.Scale = new(value, bone.Scale.Y);
}
public class ScaleYTimeline() : MonoBoneFloatPropertyTimeline(true, false)
{
	public override float Get(BoneInstance bone) => bone.Scale.Y;
	public override float GetSetup(BoneInstance bone) => bone.Data.Scale.Y;
	public override void Set(BoneInstance bone, float value) => bone.Scale = new(bone.Scale.X, value);
}
public class ShearXTimeline() : MonoBoneFloatPropertyTimeline(false, false)
{
	public override float Get(BoneInstance bone) => bone.Shear.X;
	public override float GetSetup(BoneInstance bone) => bone.Data.Shear.X;
	public override void Set(BoneInstance bone, float value) => bone.Shear = new(value, bone.Shear.Y);
}
public class ShearYTimeline() : MonoBoneFloatPropertyTimeline(false, false)
{
	public override float Get(BoneInstance bone) => bone.Shear.Y;
	public override float GetSetup(BoneInstance bone) => bone.Data.Shear.Y;
	public override void Set(BoneInstance bone, float value) => bone.Shear = new(bone.Shear.X, value);
}


public class AnimationChannelEntry
{
	public Animation Animation;
	public bool Looping;
}
public class AnimationChannel
{
	public AnimationChannelEntry? CurrentEntry;
	public Queue<AnimationChannelEntry> QueuedEntries = [];
	public double Time;
}
public class AnimationHandler
{
	public AnimationChannel[] Channels = new AnimationChannel[5];
	ModelData model;
	public AnimationHandler(ModelInstance model) : this(model.Data) { }
	public AnimationHandler(ModelData model) {
		this.model = model;
		for (int i = 0; i < Channels.Length; i++) {
			Channels[i] = new();
		}
	}
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

			channel.Time += time;
			var anim = channel.CurrentEntry;
			if (anim == null) {
				if (channel.QueuedEntries.TryDequeue(out AnimationChannelEntry? newAnim)) {
					channel.CurrentEntry = newAnim;
					anim = newAnim;
				}
				else continue;
			}

			if (channel.Time > anim.Animation.Duration) {
				if (anim.Looping)
					channel.Time = channel.Time % anim.Animation.Duration;
				else {
					// Enqueue the next animation
					if (channel.QueuedEntries.TryDequeue(out AnimationChannelEntry? newAnim)) {
						channel.CurrentEntry = newAnim;
					}
					else {
						channel.CurrentEntry = null;
					}
				}
			}
		}
	}

	public void AddAnimation(int channel, string animation, bool loops = false) {
		var channelObj = Channels[channel];

		var anim = model.FindAnimation(animation);
		if (anim == null) return;

		channelObj.QueuedEntries.Enqueue(new() {
			Animation = anim,
			Looping = loops
		});
	}

	public void SetAnimation(int channel, string animation, bool loops = false) {
		var channelObj = Channels[channel];
		StopAnimation(channel);

		var anim = model.FindAnimation(animation);
		if (anim == null) return;

		channelObj.QueuedEntries.Clear();
		channelObj.QueuedEntries.Enqueue(new() {
			Animation = anim,
			Looping = loops
		});
	}

	public void StopAnimation(int channel) {
		Channels[channel].CurrentEntry = null;
		Channels[channel].Time = 0;
	}

	public void ClearAnimation(int channel) {
		Channels[channel].QueuedEntries.Clear();
		Channels[channel].CurrentEntry = null;
		Channels[channel].Time = 0;
	}

	public void Apply(ModelInstance model) {
		foreach (var channel in Channels) {
			if (channel.CurrentEntry == null) continue;

			channel.CurrentEntry.Animation.Apply(model, channel.Time);
		}
	}
}

public interface IModelLoader
{
	ModelData LoadModelFromFile(string pathID, string path);
}

// Basic JSON model loader. Uses Newtonsoft's references to deserialize properly.
// The editor does something similar but with EditorModel instead.
public class ModelRefJSON : IModelLoader
{
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

	public ModelData LoadModelFromFile(string pathID, string path){
		var text = Filesystem.ReadAllText(pathID, path);
		if (text == null) throw new FileNotFoundException();

		var data = JsonConvert.DeserializeObject<ModelData>(text, Settings);
		if (data == null)
			throw new FormatException("Issue occured during deserialization of a Model4-refjson");

		// Load the texture atlas now
		data.TextureAtlas = new();
		data.TextureAtlas.Load(Filesystem.ReadAllText(pathID, Path.ChangeExtension(path, ".texatlas")), Filesystem.ReadAllBytes(pathID, Path.ChangeExtension(path, ".png")));
		return data;
	}
	public void SaveModelToFile(string filepath, ModelData data) {
		var serialized = JsonConvert.SerializeObject(data, Settings);
		File.WriteAllText(filepath, serialized);
		data.TextureAtlas.SaveTo(filepath);
	}
}


