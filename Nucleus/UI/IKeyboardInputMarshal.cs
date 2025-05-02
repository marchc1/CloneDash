using Nucleus.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI
{
	public interface IKeyboardInputMarshal
	{
		public KeyboardState State(ref KeyboardState original);
	}
	public class DefaultKeyboardInputMarshal : IKeyboardInputMarshal
	{
		public static DefaultKeyboardInputMarshal Instance { get; } = new();
		public KeyboardState State(ref KeyboardState original) {
			return original;
		}
	}
	public class HoldingKeyboardInputMarshal : IKeyboardInputMarshal
	{
		private List<int> Presses = [];
		private DateTime PressTime = DateTime.Now;
		private DateTime MultiTime = DateTime.Now;

		public KeyboardState State(ref KeyboardState original) {
			DateTime now = DateTime.Now;

			bool invalidated = false;

			foreach (var p in original.KeysHeld) {
				if (!Presses.Contains(p)) {
					invalidated = true;
					break;
				}
			}
			if (!invalidated) {
				foreach (var p in Presses) {
					if (!original.KeysHeld.Contains(p)) {
						invalidated = true;
						break;
					}
				}
			}

			if (invalidated) {
				PressTime = now;
				MultiTime = now;
				Presses = original.KeysHeld.ToList();
				Presses.Reverse();
			}

			if ((now - PressTime).TotalSeconds > 0.45 && Presses.Count > 0) {
				if ((now - MultiTime).TotalSeconds > 0.025) {
					MultiTime = now;
					foreach (var x in Presses) {
						original.KeyOrder.Add(x);
						original.KeysPressed.Add(x);
						if (!original.KeyPressCounts.TryGetValue(x, out var dict)) {
							dict = 1;
							original.KeyPressCounts[x] = dict;
						}
						else {
							original.KeyPressCounts[x] += 1;
						}
					}
				}
			}

			return original;
		}
	}
}
