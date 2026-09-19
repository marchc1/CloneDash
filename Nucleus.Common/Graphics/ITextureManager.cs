using Nucleus.Common.Images;
using Nucleus.ManagedMemory;
using System.Collections;

namespace Nucleus.Common.Graphics;

public ref struct TextureEnumerator(ITextureManager textures) : IEnumerator<ITexture>
{
	WeakCollection<ITexture>? textureCollection = textures.GetTextureCollectionForIteration();
	int ptr = -1;
	ITexture? currentTex;

	public ITexture Current => currentTex!;
	object IEnumerator.Current => Current;

	public void Dispose() {
	}

	public bool MoveNext() {
		if (textureCollection is null)
			return false;

		while (++ptr < textureCollection.Count) {
			if (textureCollection[ptr] is ITexture tex) {
				currentTex = tex;
				return true;
			}
		}

		currentTex = null;
		return false;
	}

	public void Reset() {
		ptr = -1;
		currentTex = null;
	}
}

public interface ITextureManager : IMemoryManager<ITexture>
{
	WeakCollection<ITexture> GetTextureCollectionForIteration();

	int GetTextureCount();
	TextureEnumerator GetTextures();

	ITexture CreateProcedural(int width, int height, ImageFormat format, ITextureRegenerator generator);
	IRenderTexture CreateRenderTexture(in RenderTextureDesc desc);
	IRenderTexture CreateRenderTexture(int width, int height, int samples = 1, bool depth = true, ImageFormat format = ImageFormat.R8G8B8A8);
	ITexture CreateTexture(Image image, bool retainCPUImage = false);
	ITexture CreateTexture(int width, int height, ImageFormat format);
	ITexture CreateTexture(ReadOnlySpan<byte> encoded, bool retainCPUImage = false);
	ITexture CreateTexture(Stream encoded, bool retainCPUImage = false);
	ITexture GetErrorTexture();
	ITexture LoadTextureFromFile(ReadOnlySpan<char> pathID, ReadOnlySpan<char> path);
	ITexture LoadTextureFromFile(ReadOnlySpan<char> filepath, bool localToImages = true);
}