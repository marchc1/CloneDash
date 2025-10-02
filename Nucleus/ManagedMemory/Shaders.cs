using Nucleus.Audio;
using Nucleus.Extensions;
using Nucleus.Files;
using Nucleus.UI;
using Nucleus.Util;

using Raylib_cs;

namespace Nucleus.ManagedMemory;

public interface IShader : IManagedMemory {
	public int HardwareID { get; }
	public void Activate();
	public void Deactivate();

	public void SetUniform<T>(string name, T value) where T : unmanaged;
}
public class ShaderInstance : IShader
{
	public void SetUniform<T>(string name, T value) where T : unmanaged => underlying.SetShaderValue(name, value);

	public ShaderManagement? parent;
	private Raylib_cs.Shader underlying;
	public bool selfDisposing;
	private bool disposedValue;

	public ShaderInstance(ShaderManagement? parent, Shader underlying, bool selfDispose = true) {
		this.parent = parent;
		this.underlying = underlying;
		this.selfDisposing = selfDispose;
	}

	public int HardwareID => (int)underlying.Id;

	public ulong UsedBits => 0; // not applicable

	public void Activate() {
		Raylib_cs.Raylib.BeginShaderMode(underlying);
	}

	public void Deactivate() {
		Raylib_cs.Raylib.EndShaderMode();
	}

	public bool IsValid() => Raylib_cs.Raylib.IsShaderReady(underlying);

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
public class ShaderManagement : IManagedMemory
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

	public ShaderInstance LoadFragmentShaderFromFile(string pathID, string path) {
		Span<char> finalPath = stackalloc char[IManagedMemory.MergePathSize(pathID, path)];
		IManagedMemory.MergePath(pathID, path, finalPath);
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

	public ShaderInstance LoadVerterxShaderFromFile(string pathID, string path) {
		Span<char> finalPath = stackalloc char[IManagedMemory.MergePathSize(pathID, path)];
		IManagedMemory.MergePath(pathID, path, finalPath);
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

	public ShaderInstance LoadShaderFromFile(string pathID, string path) {
		Span<char> finalPath = stackalloc char[IManagedMemory.MergePathSize(pathID, path)];
		IManagedMemory.MergePath(pathID, path, finalPath);
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
}