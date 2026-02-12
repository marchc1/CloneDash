using Newtonsoft.Json;

using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Engine;
using Nucleus.Files;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Core;


[Nucleus.MarkForStaticConstruction]
public static class Host
{
	static Dictionary<string, string> DataStore = [];
	static void SerializeDataStore(Stream stream) {
		using StreamWriter writer = new(stream);
		writer.Write(JSON.Serialize(DataStore));
	}

	static void DeserializeDataStore(Stream stream) {
		using StreamReader reader = new(stream);
		DataStore = JSON.Deserialize<Dictionary<string, string>>(reader.ReadToEnd()) ?? [];
	}

	public static bool IsDirty { get; private set; } = false;
	public static DateTime LastWriteTime { get; private set; } = DateTime.MinValue;

	public static T? GetDataStore<T>(string key) => DataStore.TryGetValue(key, out var str) ? JsonConvert.DeserializeObject<T>(str) : default;
	public static void SetDataStore<T>(string key, T? value) {
		if (value == null) {
			DataStore.Remove(key);
			return;
		}

		DataStore[key] = JsonConvert.SerializeObject(value);
	}

	public static bool ConfigCfgExecuted { get; private set; }
	public static bool Initialized { get; private set; }

	public static void ReadConfiguration() {
		bool userHasConfig = filesystem.Exists("cfg", "config.cfg");

		if (userHasConfig)
			Cbuf.AddText("exec config.cfg");
		else if (filesystem.Exists("cfg", "config_default.cfg"))
			Cbuf.AddText("exec config_default.cfg");
		else
			userHasConfig = false;

		Cbuf.Execute();

		if (filesystem.ReadAllText("cfg", "datastore.cfg", out string? datastoreText))
			DataStore = JSON.Deserialize<Dictionary<string, string>>(datastoreText) ?? [];

		if (!userHasConfig)
			WriteConfiguration(force: true);

		ConfigCfgExecuted = true;
	}

	public static void WriteConfiguration(ReadOnlySpan<char> filename = default, bool allVars = false, bool force = false) {
		if (filename.IsEmpty)
			filename = "config.cfg";

		if (!force) {
			if (!ConfigCfgExecuted)
				return;

			if (!Initialized)
				return;
		}

		using MemoryStream stream = new();
		using StreamWriter writer = new(stream);
		{
			Key.WriteBindings(writer);
			cv.WriteVariables(writer, allVars);
		}

		writer.Flush();
		stream.Seek(0, SeekOrigin.Begin);

		using StreamReader reader = new(stream, leaveOpen: true);
		filesystem.WriteAllText("cfg", "config.cfg", reader.ReadToEnd());
		filesystem.WriteAllText("cfg", "datastore.cfg", JSON.Serialize(DataStore));
	}

	internal static void ReformatOldHostStore(OldHostStore hoststore) {
		using MemoryStream stream = new();
		using StreamWriter writer = new(stream);

		foreach (var cvarKVP in hoststore.CVars) 
			writer.WriteLine($"{cvarKVP.Key} \"{cvarKVP.Value}\"");

		writer.Flush();
		stream.Seek(0, SeekOrigin.Begin);

		using StreamReader reader = new(stream, leaveOpen:true);
		filesystem.WriteAllText("cfg", "config.cfg", reader.ReadToEnd());
		filesystem.WriteAllText("cfg", "datastore.cfg", JSON.Serialize(hoststore.DataStore));
	}

	public static ConCommand host_writeconfig = new("host_writeconfig", (_, in args) => WriteConfiguration(), "Writes the current configuration to config.cfg");
}
