namespace CloneDash.Scripting;

public interface ILuaWrappedObject<Around>
{
	public Around Unwrap();
}
