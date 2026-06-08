using System.Numerics;
using System.Runtime.CompilerServices;

namespace CloneDash.Common.Gamemodes.MuseDash.V1;

/// <summary>
/// The default entity types.<br></br>
/// Custom entities should use the Custom EntityType.
/// </summary>
public enum MuseDash1EntityType
{
	/// <summary>
	/// Basic, single-hit enemy, which damages you when you fail to hit it
	/// </summary>
	Single = (1 << 0),
	/// <summary>
	/// Single-hit enemy that requires the key to be held (sustained) until the note is over, does not do any damage for failing to hit but still ruins your combo
	/// </summary>
	SustainBeam = (1 << 1),
	/// <summary>
	/// Single-hit enemy that is placed on both pathways, requires both entities to be hit or you will be damaged
	/// </summary>
	Double = (1 << 2),
	/// <summary>
	/// Multi-hit enemy, requires an initial hit and (not yet implemented) gives more score for each further hit until a maximum number of hits is achieved
	/// </summary>
	Masher = (1 << 3),
	/// <summary>
	/// Enemy which needs to be avoided, otherwise it will damage the player
	/// </summary>
	Gear = (1 << 4),
	/// <summary>
	/// Single-hit enemy that doesn't do any damage or combo reset when missed
	/// </summary>
	Ghost = (1 << 5),
	/// <summary>
	/// Boss enemy. Only spawned once and responds moreso to map events
	/// </summary>
	Boss = (1 << 6),
	/// <summary>
	/// Health pickup
	/// </summary>
	Heart = (1 << 7),
	/// <summary>
	/// Score pickup
	/// </summary>
	Score = (1 << 8),
	/// <summary>
	/// Single-hit enemy that swings in from the top
	/// </summary>
	Hammer = (1 << 9),
	/// <summary>
	/// Single-hit enemy that comes in from the bottom-up
	/// </summary>
	Raider = (1 << 10)
	// MAKE SURE TO INCREASE MAX_BITS to THE ABOVE + 1 IF THIS CHANGES !!!!!!!!!!
}
public static class MuseDash1EntityTypeExt
{
	public const int MAX_BITS = 11;
	extension(MuseDash1EntityType type)
	{
		public int GetBit() => BitOperations.Log2((uint)type);
	}
}
public static class EntityTypeLookupArray
{
	public static EntityTypeLookupArray<T> Create<T>(T? def, params ReadOnlySpan<(MuseDash1EntityType type, T? value)> values) {
		EntityTypeLookupArray<T> ret = new();
		for (int i = 0; i < MuseDash1EntityTypeExt.MAX_BITS; i++)
			ret[i] = def;

		foreach (var value in values)
			ret[value.type.GetBit()] = value.value;

		return ret;
	}

	public static EntityTypeLookupArray<T> Create<T>(params ReadOnlySpan<(MuseDash1EntityType type, T? value)> values) {
		EntityTypeLookupArray<T> ret = new();
		foreach (var value in values)
			ret[value.type.GetBit()] = value.value;
		return ret;
	}

	/// <summary>
	/// Creates a lookup array where the values provided are set to true.
	/// </summary>
	public static EntityTypeLookupArray<bool> CreateWhitelist(params ReadOnlySpan<MuseDash1EntityType> values) {
		EntityTypeLookupArray<bool> ret = new();
		for (int i = 0; i < MuseDash1EntityTypeExt.MAX_BITS; i++)
			ret[i] = false;

		foreach (var value in values)
			ret[value.GetBit()] = true;

		return ret;
	}

	/// <summary>
	/// Creates a lookup array where the values provided are set to false.
	/// </summary>
	public static EntityTypeLookupArray<bool> CreateBlacklist(params ReadOnlySpan<MuseDash1EntityType> values) {
		EntityTypeLookupArray<bool> ret = new();
		for (int i = 0; i < MuseDash1EntityTypeExt.MAX_BITS; i++)
			ret[i] = true;

		foreach (var value in values)
			ret[value.GetBit()] = false;

		return ret;
	}
}

[InlineArray(MuseDash1EntityTypeExt.MAX_BITS)] public struct EntityTypeLookupArray<T> { T? f; }