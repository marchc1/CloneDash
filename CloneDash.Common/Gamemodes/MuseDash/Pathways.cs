using CloneDash.Game.Input;
using Nucleus.Common.Types;
using System.Numerics;
using System.Text.Json.Serialization;

namespace CloneDash.Common.Gamemodes.MuseDash;

public readonly record struct PathwayId(byte Index)
{
	public PathwaySet AsSet => PathwaySet.Of(this);
}

public readonly struct PathwaySet : IEquatable<PathwaySet>
{
	public readonly uint Bits;

	PathwaySet(uint bits) => Bits = bits;

	public static readonly PathwaySet None = new(0);
	public static readonly PathwaySet Any = new(uint.MaxValue);

	public static PathwaySet Of(PathwayId pathway) => new(1u << pathway.Index);
	public static PathwaySet FromBits(uint bits) => new(bits);

	public bool IsEmpty => Bits == 0;
	public bool IsAny => Bits == uint.MaxValue;
	public int Count => BitOperations.PopCount(Bits);
	public PathwayId First => new((byte)BitOperations.TrailingZeroCount(Bits));

	public bool Contains(PathwayId pathway) => (Bits & (1u << pathway.Index)) != 0;
	public bool Overlaps(PathwaySet other) => (Bits & other.Bits) != 0;
	public PathwaySet With(PathwayId pathway) => new(Bits | (1u << pathway.Index));
	public PathwaySet Without(PathwayId pathway) => new(Bits & ~(1u << pathway.Index));

	public static PathwaySet operator |(PathwaySet a, PathwaySet b) => new(a.Bits | b.Bits);
	public static PathwaySet operator &(PathwaySet a, PathwaySet b) => new(a.Bits & b.Bits);
	public static bool operator ==(PathwaySet a, PathwaySet b) => a.Bits == b.Bits;
	public static bool operator !=(PathwaySet a, PathwaySet b) => a.Bits != b.Bits;

	public bool Equals(PathwaySet other) => Bits == other.Bits;
	public override bool Equals(object? obj) => obj is PathwaySet other && Equals(other);
	public override int GetHashCode() => (int)Bits;
}

public readonly record struct PathwayAssetPreference(string AssetSet, float HueShift = 0);

public sealed record PathwayDefinition(string Name, InputAction Action, float NormalizedY, PathwayAssetPreference[] Assets, Color Color);

public sealed class PathwayLayout
{
	public const int MAX_PATHWAYS = 16;

	public const string ASSETS_AIR = "air";
	public const string ASSETS_MIDDLE = "middle";
	public const string ASSETS_ROAD = "road";

	public static readonly Color MUSEDASH_TOP_COLOR = new(178, 255, 252, 120);
	public static readonly Color MUSEDASH_BOTTOM_COLOR = new(248, 178, 255, 120);
	public static readonly Color CENTER_COLOR = new(220, 160, 140, 255);

	public static readonly PathwayId MuseDashTop = new(0);
	public static readonly PathwayId MuseDashBottom = new(1);

	public static readonly PathwayLayout MuseDash = new(
		new PathwayDefinition("Top", MuseDashActions.Air, 1, [new(ASSETS_AIR)], MUSEDASH_TOP_COLOR),
		new PathwayDefinition("Bottom", MuseDashActions.Ground, 0, [new(ASSETS_ROAD)], MUSEDASH_BOTTOM_COLOR)
	);

	static readonly List<PathwayLayout> registered = [MuseDash];

	public static IReadOnlyList<PathwayLayout> Registered => registered;

	public static void Register(PathwayLayout layout) {
		if (!registered.Contains(layout))
			registered.Add(layout);
	}

	readonly PathwayDefinition[] definitions;

	public int Count => definitions.Length;
	public PathwaySet All { get; }
	public float CenterNormalizedY { get; }
	[JsonIgnore] public ReadOnlySpan<PathwayDefinition> Definitions => definitions;
	public PathwayDefinition this[PathwayId pathway] => definitions[pathway.Index];

	public PathwayLayout(params PathwayDefinition[] definitions) {
		if (definitions.Length == 0 || definitions.Length > MAX_PATHWAYS)
			throw new ArgumentException($"A pathway layout needs 1 to {MAX_PATHWAYS} pathways, got {definitions.Length}.");
		for (int i = 1; i < definitions.Length; i++)
			if (definitions[i].NormalizedY > definitions[i - 1].NormalizedY)
				throw new ArgumentException("Pathway layout pathways must be ordered top to bottom.");

		this.definitions = definitions;
		All = PathwaySet.FromBits((1u << definitions.Length) - 1);
		CenterNormalizedY = (definitions[0].NormalizedY + definitions[^1].NormalizedY) / 2;
	}

	public PathwayId PathwayAt(int index) => new((byte)index);
	public bool IsAirSide(PathwayId pathway) => definitions[pathway.Index].NormalizedY >= CenterNormalizedY;

	public PathwayId? Find(InputAction action) {
		for (int i = 0; i < definitions.Length; i++)
			if (definitions[i].Action == action)
				return new((byte)i);
		return null;
	}

	public PathwayId? Find(string name) {
		for (int i = 0; i < definitions.Length; i++)
			if (definitions[i].Name == name)
				return new((byte)i);
		return null;
	}

	public PathwaySet Clamp(PathwaySet set) => set & All;

	public static float LerpY(float normalizedY, float roadY, float airY) => roadY + (airY - roadY) * normalizedY;
}
