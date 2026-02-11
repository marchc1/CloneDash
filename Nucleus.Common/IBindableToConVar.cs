using Nucleus.Common.Commands;

namespace Nucleus.Interfaces;

public interface IBindableToConVar
{
	public void BindToConVar(string convar);
	public void BindToConVar(IConVar convar);
}
