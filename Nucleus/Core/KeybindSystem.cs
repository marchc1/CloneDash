using Nucleus.Common.Input;
using Nucleus.Input;
using System.Diagnostics.CodeAnalysis;

namespace Nucleus.Core
{
	public class KeybindSystem
	{
		internal Dictionary<ButtonCode, List<Keybind>> FinalKeybindAssociation { get; } = [];

		public Keybind AddKeybind(List<ButtonCode> requiredKeys, Action bind, bool mustBePure = false) {
			Keybind ret = Keybind.Make(requiredKeys, bind, mustBePure);

			if (!FinalKeybindAssociation.ContainsKey(ret.FinalKey))
				FinalKeybindAssociation[ret.FinalKey] = [];
			FinalKeybindAssociation[ret.FinalKey].Add(ret);

			return ret;
		}

		public bool RemoveKeybind(Keybind bind) {
			return FinalKeybindAssociation.TryGetValue(bind.FinalKey, out List<Keybind>? list) && list.Remove(bind);
		}

		public bool TestKeybinds(ref KeyboardState state, [NotNullWhen(true)] out Keybind? ranKeybind) {
			bool ranKeybinds = false;

			foreach (var keybindFinal in FinalKeybindAssociation) {
				if (!state.WasKeyPressed(keybindFinal.Key))
					continue;

				keybindFinal.Value.Sort((x, y) => y.Complexity.CompareTo(x.Complexity));
				foreach (var keybindTest in keybindFinal.Value) {
					if (keybindTest.Test(state)) {
						ranKeybinds = true;
						keybindTest.Bind?.Invoke();
						ranKeybind = keybindTest;
						return true;
					}
				}
			}

			ranKeybind = null;
			return ranKeybinds;
		}
	}

	public class Keybind
	{
		public List<ButtonCode> RequiredKeys;
		public ButtonCode FinalKey;
		public Action Bind;
		public string NiceKeybindString;
		public bool MustBePure = false;
		public int Complexity => RequiredKeys.Count;
		internal Keybind() { }

		public bool Test(KeyboardState state) {
			foreach (var key in RequiredKeys) {
				if (!state.IsKeyDown(key))
					return false;
			}

			if (MustBePure) {
				foreach (var key in state.GetKeysHeld()) {
					ButtonCode k = key.ToButtonCode();
					if (!RequiredKeys.Contains(k) && k != FinalKey) {
						return false;
					}
				}
			}

			return state.WasKeyPressed(FinalKey);
		}

		public static Keybind Make(List<ButtonCode> requiredKeys, Action bind, bool mustBePure) => new() {
			RequiredKeys = requiredKeys,
			FinalKey = requiredKeys.Last(),
			Bind = bind,
			MustBePure = mustBePure,
			NiceKeybindString = string.Join(" + ", requiredKeys.Select(x => x.ToString()))
		};

		internal void WipeState(ref KeyboardState state) {
			state.ConsumeFirstKeyPress(FinalKey);
		}
	}
}