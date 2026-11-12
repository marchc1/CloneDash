using AssetStudio;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public static partial class UnbeatableWhiteLabelCompatibility
{
	public static GameObject? FindRootGameObject(string fileName, string name) {
		WaitForInitialization();
		SerializedFile? file = ReadAssetsFile(fileName);
		if (file == null)
			return null;

		foreach (var obj in file.Objects)
			if (obj is GameObject gameObject && gameObject.m_Name == name && !gameObject.m_Transform.m_Father.TryGet(out _))
				return gameObject;

		return null;
	}
}
