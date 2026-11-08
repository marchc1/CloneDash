using CloneDash.Common.Compatibility;
using Nucleus.Common.Graphics;
using Nucleus.Common.Images;

namespace CloneDash.Common.Interludes;

/// <summary>
/// A loading screen image.
/// </summary>
public interface IInterludeDescriptor : ICloneDashModDescriptor
{
	/// <summary>
	/// Decodes the image, or returns null if it can't be used.
	/// <br/><b>NOTE:</b> Runs off the main thread.
	/// </summary>
	Image? LoadImage();
	/// <summary>
	/// Creates the texture for an image from <see cref="LoadImage"/>. Does not unload the image.
	/// </summary>
	ITexture CreateTexture(Image image);
}
