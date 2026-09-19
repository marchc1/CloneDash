using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AssetStudio
{
	public interface IPPtr
	{
		bool IsNull();
		int GetFileID();
		long GetPathID();
		SerializedFile GetAssetsFile();
		bool TryGet<T2>([NotNullWhen(true)] out T2? value, SerializedFile assetsFile = null) where T2 : AssetStudio.Object;
	}
	public sealed class PPtr<T> : IPPtr where T : Object
	{
		public int m_FileID;
		public long m_PathID;

		public SerializedFile AssetsFile;

		// Might be a misunderstanding - march
		public PPtr() { }
		public PPtr(int fileID, long pathID, SerializedFile assets) {
			AssetsFile = assets;
			m_FileID = fileID;
			m_PathID = pathID;
		}

		public PPtr(ObjectReader reader) {
			m_FileID = reader.ReadInt32();
			m_PathID = reader.m_Version < SerializedFileFormatVersion.Unknown_14 ? reader.ReadInt32() : reader.ReadInt64();
			AssetsFile = reader.assetsFile;
		}

		private bool TryGetAssetsFile([NotNullWhen(true)] out SerializedFile? result) {
			result = null;
			if (m_FileID == 0) {
				result = AssetsFile;
				return true;
			}

			if (m_FileID > 0 && m_FileID - 1 < AssetsFile.m_Externals.Count) {
				var name = AssetsFile.m_Externals[m_FileID - 1].fileName;
				if (AssetsFile.assetsManager.assetsFileByName.TryGetValue(name, out var file)) {
					result = file;
					return true;
				}
			}

			return false;
		}

		public bool TryGet([NotNullWhen(true)] out T? result, SerializedFile assetsFile = null) {
			AssetsFile = AssetsFile ?? assetsFile;
			if (TryGetAssetsFile(out var sourceFile)) {
				if (sourceFile.ObjectsDic.TryGetValue(m_PathID, out var obj)) {
					if (obj is T variable) {
						result = variable;
						return true;
					}
				}
			}

			result = null;
			return false;
		}

		public bool TryGet<T2>([NotNullWhen(true)] out T2? result, SerializedFile assetsFile = null) where T2 : Object {
			AssetsFile = AssetsFile ?? assetsFile;
			if (TryGetAssetsFile(out var sourceFile)) {
				if (sourceFile.ObjectsDic.TryGetValue(m_PathID, out var obj)) {
					if (obj is T2 variable) {
						result = variable;
						return true;
					}
				}
			}

			result = null;
			return false;
		}

		public void Set(T m_Object) {
			var name = m_Object.assetsFile.fileName;
			if (string.Equals(AssetsFile.fileName, name, StringComparison.OrdinalIgnoreCase)) {
				m_FileID = 0;
			}
			else {
				m_FileID = AssetsFile.m_Externals.FindIndex(x => string.Equals(x.fileName, name, StringComparison.OrdinalIgnoreCase));
				if (m_FileID == -1) {
					AssetsFile.m_Externals.Add(new FileIdentifier {
						fileName = m_Object.assetsFile.fileName
					});
					m_FileID = AssetsFile.m_Externals.Count;
				}
				else {
					m_FileID += 1;
				}
			}

			m_PathID = m_Object.m_PathID;
		}

		public bool IsNull() => m_PathID == 0 || m_FileID < 0;
		public int GetFileID() => m_FileID;
		public long GetPathID() => m_PathID;
		public SerializedFile GetAssetsFile() => AssetsFile;
	}
}
