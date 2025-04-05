namespace Nucleus.Types
{
	public interface IBindableToConVar
	{
		public void BindToConVar(string convar);
		public void BindToConVar(ConVar convar);
	}
}
