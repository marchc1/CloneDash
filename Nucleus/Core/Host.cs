using Newtonsoft.Json;
using Nucleus.Files;
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
		public static bool IsDirty { get; private set; } = false;
		public static DateTime LastWriteTime { get; private set; } = DateTime.MinValue;

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

			if (!Filesystem.ReadAllText("cfg", "config.cfg", out string? cfgText)) 
				throw new FileNotFoundException("Cannot read the config.cfg file. Is the filesystem initialized properly?");

			Config = JsonConvert.DeserializeObject<HostConfig>(cfgText) ?? throw new Exception("Could not parse cfg/config.cfg");
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

		/// <summary>
		/// Marks the configuration data as outdated and needing to be re-written to disk.
		/// </summary>
		public static void MarkDirty() {
			IsDirty = true;
		}
		public static void CheckDirty() {
			if (!IsDirty)
				return;

			if ((DateTime.Now - LastWriteTime).TotalMilliseconds < 1000)
				return;

			WriteConfig();
			IsDirty = false;
		}

		public static void WriteConfig() {
			Filesystem.WriteAllText("cfg", "config.cfg", JsonConvert.SerializeObject(Config));
			Logs.Debug("Host: Wrote config to config.cfg");
			LastWriteTime = DateTime.Now;
		}

		public static ConCommand host_writeconfig = ConCommand.Register("host_writeconfig", (_, args) => WriteConfig(), "Writes the current configuration to config.cfg");
	}
}
