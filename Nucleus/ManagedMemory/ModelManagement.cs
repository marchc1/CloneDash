using Nucleus.Core;
using Nucleus.Models;
using Nucleus.Models.Runtime;

namespace Nucleus.ManagedMemory;

public class ModelManagement : IManagedMemory
{
	private Dictionary<string, ModelData> ModelDatas = [];
	private bool disposedValue;
	public ulong UsedBits => 0; // todo

	public bool IsValid() => !disposedValue;

	protected virtual void Dispose(bool disposing) {
		if (!disposedValue) {
			lock (ModelDatas) {
				foreach (var m in ModelDatas) {
					m.Value.Dispose();
				}
				ModelDatas.Clear();
				disposedValue = true;
			}
		}
	}

	// TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	~ModelManagement() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
	}

	public void Dispose() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private Dictionary<string, Texture> LoadedTexturesFromFile = [];
	private Dictionary<Texture, string> LoadedFilesFromTexture = [];

	public ModelRefJSON ModelLoader = new();

	public ModelData LoadModelFromFile(string pathID, string path) {
		var savePath = Path.Combine(pathID, path);
		if (ModelDatas.TryGetValue(savePath, out var data)) {
			return data;
		}

		ModelDatas[savePath] = ModelLoader.LoadModelFromFile(pathID, Path.ChangeExtension(path, ".nm4rj"));
		return ModelDatas[savePath];
	}

	public ModelInstance CreateInstanceFromFile(string pathID, string filepath) => LoadModelFromFile(pathID, filepath).Instantiate();
}
