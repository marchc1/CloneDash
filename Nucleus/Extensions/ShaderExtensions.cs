using Nucleus.Types;
using Raylib_cs;
using System.Numerics;

namespace Nucleus.Extensions
{
	public static class ShaderExtensions
	{
		private static Dictionary<string, int> shaderLocs { get; } = [];
		private static int getShaderLocation(Shader shader, string loc) {
			var key = string.Format("{0}_{1}", shader.Id, loc);
			if (shaderLocs.ContainsKey(key))
				return shaderLocs[key];

			int location = Raylib.GetShaderLocation(shader, loc);
			shaderLocs[key] = location;
			return location;
		}

		public static int GetShaderLocation(this Shader shader, string location) => getShaderLocation(shader, location);
		public static void Begin(this Shader shader) => Raylib.BeginShaderMode(shader);
		public static void End(this Shader shader) => Raylib.EndShaderMode();

		public static void SetShaderValue<T>(this Shader shader, int location, T value, ShaderUniformDataType type) where T : unmanaged => Raylib.SetShaderValue(shader, location, value, type);
		public static void SetShaderValue<T>(this Shader shader, string location, T value, ShaderUniformDataType type) where T : unmanaged => Raylib.SetShaderValue(shader, shader.GetShaderLocation(location), value, type);

		public static void SetShaderValue<T>(this Shader shader, int location, T value, bool iVal = false) where T : unmanaged {
			ShaderUniformDataType uniformType;
			switch (value) {
				case float:
					uniformType = ShaderUniformDataType.SHADER_UNIFORM_FLOAT;
					break;
				case Vector2:
				case Vector2F:
					uniformType = iVal ? ShaderUniformDataType.SHADER_UNIFORM_IVEC2 : ShaderUniformDataType.SHADER_UNIFORM_VEC2;
					break;
				case Vector3:
					uniformType = iVal ? ShaderUniformDataType.SHADER_UNIFORM_IVEC3 : ShaderUniformDataType.SHADER_UNIFORM_VEC3;
					break;
				case Vector4:
					uniformType = iVal ? ShaderUniformDataType.SHADER_UNIFORM_IVEC4 : ShaderUniformDataType.SHADER_UNIFORM_VEC4;
					break;
				case int:
					uniformType = ShaderUniformDataType.SHADER_UNIFORM_FLOAT;
					break;
				default:
					throw new Exception("Uniform type for T is not explicitly defined by the ShaderExtensions class");
			}

			shader.SetShaderValue(location, value, uniformType);
		}
		public static void SetShaderValue<T>(this Shader shader, string location, T value) where T : unmanaged => shader.SetShaderValue(shader.GetShaderLocation(location), value);

		public static void SetShaderValueMatrix(this Shader shader, int location, Matrix4x4 matrix) => Raylib.SetShaderValueMatrix(shader, location, matrix);
		public static void SetShaderValueMatrix(this Shader shader, string location, Matrix4x4 matrix) => Raylib.SetShaderValueMatrix(shader, shader.GetShaderLocation(location), matrix);
	}
}
