using Nucleus.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Core
{
	public class KeybindSystem
	{
		internal Dictionary<KeyboardKey, List<Keybind>> FinalKeybindAssociation { get; } = [];

		public Keybind AddKeybind(List<KeyboardKey> requiredKeys, Action bind, bool mustBePure = false) {
			Keybind ret = Keybind.Make(requiredKeys, bind, mustBePure);

			if (!FinalKeybindAssociation.ContainsKey(ret.FinalKey))
				FinalKeybindAssociation[ret.FinalKey] = [];
			FinalKeybindAssociation[ret.FinalKey].Add(ret);

			return ret;
		}

		public bool TestKeybinds(KeyboardState state) {
			bool ranKeybinds = false;

			foreach (var keybindFinal in FinalKeybindAssociation) {
				if (!state.KeyPressed(keybindFinal.Key))
					continue;

				keybindFinal.Value.Sort((x, y) => y.Complexity.CompareTo(x.Complexity));
				foreach (var keybindTest in keybindFinal.Value) {
					if (keybindTest.Test(state)) {
						ranKeybinds = true;
						keybindTest.Bind?.Invoke();
						return true;
					}
				}
			}

			return ranKeybinds;
		}
	}

	public class Keybind
	{
		public List<KeyboardKey> RequiredKeys;
		public KeyboardKey FinalKey;
		public Action Bind;
		public string NiceKeybindString;
		public bool MustBePure = false;
		public int Complexity => RequiredKeys.Count;
		internal Keybind() { }

		public bool Test(KeyboardState state) {
			foreach (var key in RequiredKeys) {
				if (!state.KeyDown(key))
					return false;
			}

			if (MustBePure) {
				foreach (var key in state.KeysHeld) {
					KeyboardKey k = KeyboardLayout.USA.FromInt(key);
					if (!RequiredKeys.Contains(k) && k != FinalKey) {
						return false;
					}
				}
			}

			return state.KeyPressed(FinalKey);
		}

		public static Keybind Make(List<KeyboardKey> requiredKeys, Action bind, bool mustBePure) {
			Keybind ret = new Keybind();

			ret.RequiredKeys = requiredKeys;
			ret.FinalKey = requiredKeys.Last();
			ret.Bind = bind;
			ret.MustBePure = mustBePure;

			List<string> keyNames = [];
			foreach (KeyboardKey key in requiredKeys) {
				keyNames.Add(KeyboardLayout.USA.FromInt(key.Key).Name);
			}
			ret.NiceKeybindString = string.Join(" + ", keyNames);

			return ret;
		}
	}
}
