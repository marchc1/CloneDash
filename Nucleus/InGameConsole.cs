using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;

namespace Nucleus
{
	public class ConsoleAutocomplete : Panel
	{
		public ConCommandBase[]? PotentialMatches;
		protected override void Initialize() {
			base.Initialize();
			Clipping = false;
		}
		public override void Paint(float width, float height) {
			//base.Paint(width, height);
			if (PotentialMatches == null) return;


			int maxI = 0;
			string maxS = "";

			int maxID = 0;
			string maxSD = "";

			for (int i = 0; i < PotentialMatches.Length; i++) {
				string s = PotentialMatches[i].Name;
				if (s.Length > maxI) {
					maxS = s;
					maxI = s.Length;
				}

				string d = PotentialMatches[i].HelpString;
				if (d.Length > maxID) {
					maxSD = d;
					maxID = d.Length;
				}
			}

			float maxX = Graphics2D.GetTextSize(maxS, "Consolas", 16).X;
			float maxXD = Graphics2D.GetTextSize(maxSD, "Consolas", 16).X;

			Graphics2D.SetDrawColor(25, 25, 25, 220);
			Graphics2D.DrawRectangle(new(0, -4), new(maxX + 8, (PotentialMatches.Length * 18) + 8));
			Graphics2D.DrawRectangle(new(maxX + 20, -4), new(maxXD + 8, (PotentialMatches.Length * 18) + 8));

			for (int i = 0; i < PotentialMatches.Length; i++) {
				Graphics2D.SetDrawColor(245, 245, 245);
				Graphics2D.DrawText(new(4, 18 * i), PotentialMatches[i].Name, "Consolas", 16);

				Graphics2D.SetDrawColor(190, 190, 190);
				Graphics2D.DrawText(new(4 + 16 + maxX, (18 * i) + 1f), ": " + PotentialMatches[i].HelpString, "Consolas", 13);
			}
		}

		internal ConCommandBase? GetSelected() {
			if (PotentialMatches == null) return null;
			if (PotentialMatches.Length <= 0) return null;
			return PotentialMatches[0];
		}
	}
	public class ConsoleWindow : Panel
	{
		TextEditor consoleLogs;
		TextEditor consoleInput;
		ConsoleAutocomplete? autoComplete;
		protected override void Initialize() {
			base.Initialize();

			this.Dock = Dock.Top;
			this.Size = new(0, 384);
			this.DockMargin = RectangleF.TLRB(8);

			this.BorderSize = 0;

			consoleInput = Add<TextEditor>();
			consoleInput.Size = new(0, 32);
			consoleInput.Dock = Dock.Bottom;
			consoleInput.Multiline = false;
			consoleInput.ShowDetails = false;
			consoleInput.ShowGutter = false;
			consoleInput.TriggerExecuteOnEnter = true;
			consoleInput.OnExecute += ConsoleInput_OnExecute;
			consoleInput.Editor.OnKeyPressed += ConsoleInput_OnKeyPressed;
			consoleInput.OnTab += ConsoleInput_OnTab;

			consoleLogs = Add<TextEditor>();
			consoleLogs.Dock = Dock.Fill;
			consoleLogs.TextSize = 14;
			consoleLogs.DockMargin = new(0, 0, 0, 74);
			consoleLogs.Readonly = true;
			consoleLogs.ShowDetails = false;
			consoleLogs.ShowGutter = false;
			consoleLogs.Multiline = true;
			consoleLogs.Highlighter = new ConsoleLogHighlighter();
			consoleLogs.SetScroll(1f);

			consoleLogs.DrawPanelBackground = false;
			consoleInput.DrawPanelBackground = false;

			consoleLogs.Thinking += ConsoleLogs_Thinking;

			consoleInput.DemandKeyboardFocus();

			foreach (var msg in ConsoleSystem.GetMessages()) {
				SetupRow(msg);
			}
			ConsoleSystem.ConsoleMessageWrittenEvent += ConsoleSystem_ConsoleMessageWrittenEvent;

			this.InvalidateChildren(recursive: true);
		}
		private void SetupRow(ConsoleMessage message) {
			consoleLogs.AppendLine($"[{message.Time.ToString(Logs.TimeFormat)}] [{Logs.LevelToConsoleString(message.Level)}] {message.Message}");
			if (consoleLogs.Rows.Count > ConsoleSystem.MaxConsoleMessages)
				consoleLogs.RemoveLine(0);
			consoleLogs.ScrollToLine(consoleLogs.Rows.Count, 1f);
		}
		private void ConsoleSystem_ConsoleMessageWrittenEvent(ref ConsoleMessage message) {
			SetupRow(message);
		}

		private void ConsoleInput_OnTab(TextEditor self) {
			// Autocomplete
			if (!IValidatable.IsValid(autoComplete)) return;

			ConCommandBase? selected = autoComplete.GetSelected();
			if (selected == null) return;

			consoleInput.SetText(selected.Name);
			consoleInput.SetCaret(selected.Name.Length, 0);
		}

		private void ConsoleInput_OnKeyPressed(Element self, KeyboardState state, Nucleus.Types.KeyboardKey key) {
			if (key == KeyboardLayout.USA.Enter || key == KeyboardLayout.USA.NumpadEnter) return;
			if (!IValidatable.IsValid(autoComplete)) {
				autoComplete = UI.Add<ConsoleAutocomplete>();
				autoComplete.Position = self.GetGlobalPosition() + new Vector2F(0, 40);
			}

			autoComplete.PotentialMatches = ConCommandBase.FindMatchesThatStartWith(consoleInput.GetText(), 0, 20);
		}
		public override void OnRemoval() {
			base.OnRemoval();
			autoComplete?.Remove();
		}
		private void ConsoleLogs_Thinking(Element self) {
			if (IValidatable.IsValid(autoComplete) && !consoleInput.Editor.KeyboardFocused) {
				autoComplete.Remove();
			}
		}

		private void ConsoleInput_OnExecute(TextEditor self) {
			Logs.Print("> " + self.GetText());
			ConsoleSystem.ParseOneCommand(self.GetText());
			autoComplete?.Remove();
			MainThread.RunASAP(() => {
				consoleLogs.SetScroll(1);
			});
		}

		public override void Paint(float width, float height) {
			base.Paint(width, height);
		}
	}
	public static class InGameConsole
	{
		private static ConsoleWindow? inputPanel = null;
		private static void OpenConsole(Element parent) {
			if (IValidatable.IsValid(inputPanel)) {
				CloseConsole();
				return;
			}

			inputPanel = parent.Add<ConsoleWindow>();

			ConsoleSystem.AddScreenBlocker(inputPanel);
			inputPanel.Removed += (self) => OnConsoleClosed();
		}
		private static void CloseConsole() {
			inputPanel?.Remove();
			OnConsoleClosed();
		}
		private static void OnConsoleClosed() {
			ConsoleSystem.RemoveScreenBlocker(inputPanel);
		}
		public static void HookToLevel(this Level level) => level.Keybinds.AddKeybind([KeyboardLayout.USA.Tilda], () => OpenConsole(level.UI));
	}
}
