using Nucleus.Audio;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.Util;

using Raylib_cs;
using System.Numerics;

namespace Nucleus.ManagedMemory;

public interface IShader : IManagedMemoryUnit
{
	int HardwareID { get; }
	int GetUniformLocation(ReadOnlySpan<char> location);
	void SetUniform<T>(int location, T value, ShaderUniformDataType type) where T : unmanaged;
	void SetUniform<T>(string location, T value, ShaderUniformDataType type) where T : unmanaged;
	void SetUniform<T>(int location, T value, bool iVal = false) where T : unmanaged;
	void SetUniform<T>(ReadOnlySpan<char> location, T value) where T : unmanaged;
	void SetUniform(int location, in Matrix4x4 matrix);
	void SetUniform(ReadOnlySpan<char> location, in Matrix4x4 matrix);
	void Activate();
	void Deactivate();
}
public class ShaderInstance : IShader
{
	public ShaderManagement? parent;
	internal Raylib_cs.Shader underlying;
	public bool selfDisposing;
	private bool disposedValue;

	public ShaderInstance(ShaderManagement? parent, Shader underlying, bool selfDispose = true) {
		this.parent = parent;
		this.underlying = underlying;
		this.selfDisposing = selfDispose;
	}

	private Dictionary<UtlSymId_t, int> shaderLocs { get; } = [];
	private int getShaderLocation(ReadOnlySpan<char> loc) {
		var key = loc.Hash(false);
		if (shaderLocs.TryGetValue(key, out int realLoc))
			return realLoc;

		shaderLocs[key] = realLoc = Raylib.GetShaderLocation(underlying, loc);
		return realLoc;
	}

	public int GetUniformLocation(ReadOnlySpan<char> location) => getShaderLocation(location);

	public void SetUniform<T>(int location, T value, ShaderUniformDataType type) where T : unmanaged => Raylib.SetShaderValue(underlying, location, value, type);
	public void SetUniform<T>(string location, T value, ShaderUniformDataType type) where T : unmanaged => Raylib.SetShaderValue(underlying, GetUniformLocation(location), value, type);

	public void SetUniform<T>(int location, T value, bool iVal = false) where T : unmanaged {
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
				uniformType = ShaderUniformDataType.SHADER_UNIFORM_INT;
				break;
			default:
				throw new Exception("Uniform type for T is not explicitly defined by the ShaderExtensions class");
		}

		Raylib.SetShaderValue(underlying, location, value, uniformType);
	}
	public void SetUniform<T>(ReadOnlySpan<char> location, T value) where T : unmanaged => SetUniform(GetUniformLocation(location), value);
	public void SetUniform(int location, in Matrix4x4 matrix) => Raylib.SetShaderValueMatrix(underlying, location, matrix);
	public void SetUniform(ReadOnlySpan<char> location, in Matrix4x4 matrix) => Raylib.SetShaderValueMatrix(underlying, GetUniformLocation(location), matrix);

	public int HardwareID => (int)underlying.Id;

	public ulong UsedBits => 0; // not applicable

	public void Activate() {
		parent?.Activate(this);
	}

	public void Deactivate() {
		parent?.Deactivate(this);
	}

	public bool IsValid() => Raylib_cs.Raylib.IsShaderValid(underlying);

	protected virtual void Dispose(bool disposing) {
		if (!disposedValue && selfDisposing) {
			MainThread.RunASAP(() => {
				Raylib_cs.Raylib.UnloadShader(underlying);
				parent?.EnsureIShaderRemoved(this);
			}, ThreadExecutionTime.BeforeFrame);
			disposedValue = true;
		}
	}
	~ShaderInstance() { if (selfDisposing) Dispose(false); }
	public void Dispose() {
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
public class ShaderManagement
{
	private List<IShader> shaders = [];
	public IEnumerable<IShader> Shaders {
		get {
			foreach (var shader in shaders)
				yield return shader;
		}
	}

	private bool disposedValue;
	public ulong UsedBits => 0; // todo

	public bool IsValid() => !disposedValue;

	protected virtual void Dispose(bool usercall) {
		if (!disposedValue) {
			lock (Shaders) {
				foreach (var m in shaders) {
					m.Dispose();
				}
				shaders.Clear();
				disposedValue = true;
			}
		}
	}

	// TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	~ShaderManagement() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(usercall: false);
	}

	public void Dispose() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(usercall: true);
		GC.SuppressFinalize(this);
	}

	private Dictionary<UtlSymId_t, ShaderInstance> LoadedShadersFromFile = [];
	private Dictionary<ShaderInstance, UtlSymId_t> LoadedFilesFromShader = [];
	public void EnsureIShaderRemoved(IShader isnd) {
		switch (isnd) {
			case ShaderInstance shader:
				if (LoadedFilesFromShader.TryGetValue(shader, out var shaderFilepath)) {
					LoadedShadersFromFile.Remove(shaderFilepath);
					LoadedFilesFromShader.Remove(shader);
					shaders.Remove(shader);

					shader.Dispose();
				}
				break;
		}
	}

	public IShader LoadFragmentShaderFromFile(string pathID, string path) {
		Span<char> finalPath = stackalloc char[IManagedMemoryUnit.MergePathSize(pathID, path)];
		IManagedMemoryUnit.MergePath(pathID, path, finalPath);
		UtlSymbol searchName = new(finalPath);

		if (LoadedShadersFromFile.TryGetValue(searchName, out ShaderInstance? shader))
			return shader;

		Shader shaderRL = Filesystem.ReadFragmentShader(pathID, path);
		shader = new(this, shaderRL, true);

		LoadedShadersFromFile.Add(searchName, shader);
		LoadedFilesFromShader.Add(shader, searchName);
		shaders.Add(shader);
		return shader;
	}

	public IShader LoadVertexShaderFromFile(string pathID, string path) {
		Span<char> finalPath = stackalloc char[IManagedMemoryUnit.MergePathSize(pathID, path)];
		IManagedMemoryUnit.MergePath(pathID, path, finalPath);
		UtlSymbol searchName = new(finalPath);

		if (LoadedShadersFromFile.TryGetValue(searchName, out ShaderInstance? shader))
			return shader;

		Shader shaderRL = Filesystem.ReadVertexShader(pathID, path);
		shader = new(this, shaderRL, true);

		LoadedShadersFromFile.Add(searchName, shader);
		LoadedFilesFromShader.Add(shader, searchName);
		shaders.Add(shader);
		return shader;
	}

	public IShader LoadShaderFromFile(string pathID, string path) {
		Span<char> finalPath = stackalloc char[IManagedMemoryUnit.MergePathSize(pathID, path)];
		IManagedMemoryUnit.MergePath(pathID, path, finalPath);
		UtlSymbol searchName = new(finalPath);

		if (LoadedShadersFromFile.TryGetValue(searchName, out ShaderInstance? shader))
			return shader;

		Shader shaderRL = Filesystem.ReadShader(pathID, Path.ChangeExtension(path, ".vs"), Path.ChangeExtension(path, ".fs"));
		shader = new(this, shaderRL, true);

		LoadedShadersFromFile.Add(searchName, shader);
		LoadedFilesFromShader.Add(shader, searchName);
		shaders.Add(shader);
		return shader;
	}

	internal void Activate(ShaderInstance shaderInstance) {
		Raylib_cs.Raylib.BeginShaderMode(shaderInstance.underlying);
	}

	internal void Deactivate(ShaderInstance shaderInstance) {
		Raylib_cs.Raylib.EndShaderMode();
	}
}