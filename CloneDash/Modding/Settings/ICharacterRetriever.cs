using CloneDash.Modding.Descriptors;

namespace CloneDash.Modding.Settings
{
	public interface ICharacterRetriever
	{
		public ICharacterDescriptor? GetDescriptorFromName(string name);
		public int Priority { get; }

		public IEnumerable<string> GetAvailableCharacters();
	}
}