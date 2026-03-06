namespace Nucleus.Commands
{
	public enum FCvar : ulong
	{
		None = 0,

		/// <summary> The convar is not registered in the ICvar's internal list. </summary>
		Unregistered = 1 << 0,
		/// <summary> The convars value will be loaded to and saved from the config.cfg file. </summary>
		Saved = 1 << 7,
		/// <summary> The convar may contain invalid printable characters, and therefore must never be printed as a string </summary>
		NeverAsString = 1 << 12,
		/// <summary> The convar will only be active when running in a developers environment. </summary>
		DevelopmentOnly = 1 << 60,
		/// <summary> This flag is applied at runtime to force a convar to always report its default value. 
		/// <br/>
		/// <br/>
		/// <b>NOTE: </b> Setting this flag may trigger a change event. 
		/// <br/>
		/// <b>NOTE: </b> The convar will also not be saved, even if it has <see cref="Saved"/> in its flags.
		/// <br/>
		/// <b>NOTE: </b> Reverting the flag will revert back to normal behavior, and may trigger a change event. </summary>
		AlwaysDefault = 1 << 61
	}
}
