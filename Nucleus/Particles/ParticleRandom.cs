using System.Numerics;

namespace Nucleus.Particles;

public static class ParticleRandom
{
	public const int TABLE_SIZE = 4096;
	public const int TABLE_MASK = TABLE_SIZE - 1;

	static readonly float[] table = CreateTable();

	static float[] CreateTable() {
		var random = new System.Random(0x50415254);
		var floats = new float[TABLE_SIZE];
		for (int i = 0; i < floats.Length; i++)
			floats[i] = random.NextSingle();
		return floats;
	}

	public static float Sample(int index) => table[index & TABLE_MASK];

	public static float Sample(int seed, int particleId, int offset) => table[(seed + offset + particleId * 97) & TABLE_MASK];

	public static Vector3 UnitVector(int seed, int particleId, int offset) {
		float z = Sample(seed, particleId, offset) * 2 - 1;
		float angle = Sample(seed, particleId, offset + 1) * MathF.Tau;
		float r = MathF.Sqrt(MathF.Max(0, 1 - z * z));
		return new(r * MathF.Cos(angle), r * MathF.Sin(angle), z);
	}
}
