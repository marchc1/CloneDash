namespace CloneDash.Characters
{
	public interface ICharacterRetriever
	{
		public ICharacterDescriptor? GetDescriptorFromName(string name);
		public int Priority { get; }

		public IEnumerable<string> GetAvailableCharacters();
	}
}