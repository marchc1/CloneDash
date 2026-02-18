namespace Nucleus.Common.Graphics;

public interface IGraphicsHardwareConfig
{
	bool SupportsBPTC { get; }


	void ConfirmCapabilities();
}
