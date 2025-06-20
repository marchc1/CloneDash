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

	protected virtual void Dispose(bool usercall) {
		if (disposedValue) return;

		lock (ModelDatas) {
			foreach (var m in ModelDatas) {
				m.Value.Dispose();
			}
			ModelDatas.Clear();
		}
		disposedValue = true;
	}

	// TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	~ModelManagement() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(usercall: false);
	}

	public void Dispose() {
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(usercall: true);
		GC.SuppressFinalize(this);
	}

	public ModelRefJSON JSONModelLoader = new();
	public ModelBinary BinaryModelLoader = new();

	public string CombinePath(string pathID, string path) => IManagedMemory.MergePath(pathID, path);
	public bool IsCached(string pathID, string path) {
		var savePath = CombinePath(pathID, path);
		return ModelDatas.TryGetValue(savePath, out _);
	}

	public void Cache(ModelData data, string pathID, string path) {
		ModelDatas[CombinePath(pathID, path)] = data;
	}

	public ModelData LoadModelFromFile(string pathID, string path) {
		var savePath = CombinePath(pathID, path);
		if (ModelDatas.TryGetValue(savePath, out var data) && data != null) {
			return data;
		}

		if (string.IsNullOrEmpty(Path.GetExtension(path)))
			path = Path.ChangeExtension(path, ".nm4rj"); // Assume ref'd json, but allow binary/etc formats

		switch (Path.GetExtension(path)) {
			case ModelRefJSON.FULL_EXTENSION: ModelDatas[savePath] = JSONModelLoader.LoadModelFromFile(pathID, path); break;
			case ModelBinary.FULL_EXTENSION: ModelDatas[savePath] = BinaryModelLoader.LoadModelFromFile(pathID, path); break;
			default: throw new Exception("Unknown extension.");
		}
		return ModelDatas[savePath];
	}

	public ModelInstance CreateInstanceFromFile(string pathID, string filepath) => LoadModelFromFile(pathID, filepath).Instantiate();
}