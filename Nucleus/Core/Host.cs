using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Core
{
	public class HostConfig
	{
		public Dictionary<string, string> CVars { get; set; } = [];
		public Dictionary<string, string> DataStore { get; set; } = [];

	}
	[Nucleus.MarkForStaticConstruction]
	public static class Host
	{
		public static HostConfig Config { get; private set; }
		public static bool Initialized { get; private set; } = false;

		public static T? GetDataStore<T>(string key) => Config.DataStore.TryGetValue(key, out var str) ? JsonConvert.DeserializeObject<T>(str) : default;
		public static void SetDataStore<T>(string key, T? value) {
			if (value == null) {
				Config.DataStore.Remove(key);
				return;
			}

			Config.DataStore[key] = JsonConvert.SerializeObject(value);
		}

		public static void ReadConfig(bool forced = false) {
			if (Initialized && !forced)
				return;

			if (!File.Exists(Filesystem.Resolve("config.cfg", "cfg", false))) {
				Config = new();
				return;
			}

			Config = JsonConvert.DeserializeObject<HostConfig>(Filesystem.ReadAllText("config.cfg", "cfg")) ?? throw new Exception("Could not parse cfg/config.cfg");
			/*foreach (var cfg in Config.CVars) {
				ConsoleSystem.ParseOneCommand($"{cfg.Key} \"{cfg.Value}\"");
			}*/
			Initialized = true;
		}

		public static string? GetConfigCVar(string cvar) {
			ReadConfig();
			return Config.CVars.TryGetValue(cvar, out var value) ? value : null;
		}
		public static void SetConfigCVar(string cvar, string? val) {
			ReadConfig();
			if (val == null) {
				Config.CVars.Remove(cvar);
				return;
			}

			Config.CVars[cvar] = val;
		}

		public static void WriteConfig() {
			Filesystem.WriteAllText("config.cfg", "cfg", JsonConvert.SerializeObject(Config));
			Logs.Info("Host: Wrote config to config.cfg");
		}

		public static ConCommand host_writeconfig = ConCommand.Register("host_writeconfig", (_, args) => WriteConfig(), "Writes the current configuration to config.cfg");
	}
}
