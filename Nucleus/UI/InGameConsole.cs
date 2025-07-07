using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI;

using System.Diagnostics.CodeAnalysis;

namespace Nucleus
{
	public class ConsoleAutocomplete : Panel
	{
		public string[]? PotentialMatches;
		public string?[]? HelpStrings;
		public void SetPotentialMatches(string[] potentialMatches, string?[]? helpStrings = null) {
			PotentialMatches = potentialMatches;
			HelpStrings = helpStrings;
		}
		public void SetNoMatches() {
			PotentialMatches = null;
			HelpStrings = null;
		}
		//public ConCommand? ActiveConCommand;
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
				string s = PotentialMatches[i];
				if (s.Length > maxI) {
					maxS = s;
					maxI = s.Length;
				}

				if (HelpStrings != null) {
					string? d = HelpStrings[i];
					if (d != null && d.Length > maxID) {
						maxSD = d;
						maxID = d.Length;
					}
				}
			}

			var textSize = 16;
			var textRect = 4;

			float maxX = Graphics2D.GetTextSize(maxS, "Consolas", textSize).X;
			float maxXD = Graphics2D.GetTextSize(maxSD, "Consolas", textSize).X;

			for (int i = 0; i < PotentialMatches.Length; i++) {
				if (hasTabbed && tabPointer == i)
					Graphics2D.SetDrawColor(60, 60, 75, 255);
				else
					Graphics2D.SetDrawColor(5, 5, 5, 155);
				var yRect = -4 + (i * (textSize + textRect));

				Graphics2D.DrawRectangle(new(0, yRect), new(maxX + 8, textSize + textRect));
				if (HelpStrings != null)
					Graphics2D.DrawRectangle(new(maxX + 20, yRect), new(maxXD + 8, textSize + textRect));

				Graphics2D.SetDrawColor(245, 245, 245);
				Graphics2D.DrawText(new(4, (textSize + textRect) * i), PotentialMatches[i], "Consolas", 16);

				if (HelpStrings != null && i < HelpStrings.Length && i < PotentialMatches.Length) {
					string? d = HelpStrings[i];
					if (d != null) {
						Graphics2D.SetDrawColor(190, 190, 190);
						Graphics2D.DrawText(new(4 + 16 + maxX, ((textSize + textRect) * i) + 1f), ": " + d, "Consolas", 13);
					}
				}
			}
		}


		int tabPointer = 0;
		bool hasTabbed = false;
		internal void Reset() {
			PotentialMatches = null;
			tabPointer = 0;
			hasTabbed = false;
		}
		internal string? Tab() {
			if ((PotentialMatches?.Length ?? 0) == 0) return null;

			if (hasTabbed == false) {
				hasTabbed = true;
				if (PotentialMatches == null) return "";

				return PotentialMatches[0];
			}
			else {
				tabPointer = tabPointer + 1;
				if (PotentialMatches == null || tabPointer >= PotentialMatches.Length)
					tabPointer = 0;
				return PotentialMatches?[tabPointer] ?? "";
			}
		}

		public bool TryGetTabSelection([NotNullWhen(true)] out string? tabSelection) {
			tabSelection = PotentialMatches == null ? null : PotentialMatches.Length == 0 ? null : (tabPointer < 0 | tabPointer > PotentialMatches.Length) ? null : PotentialMatches[tabPointer];
			return hasTabbed;
		}
	}
	public class ConsoleWindow : Panel
	{
		public static string[] UserHistory = new string[256];
		public static int UserHistoryPos = 0;
		public static void PushUserHistory(string str) {
			var last = UserHistory[NMath.Modulo(UserHistoryPos - 1, UserHistory.Length)];
			if (str == last && last != null) return;

			UserHistory[UserHistoryPos] = str;
			UserHistoryPos = NMath.Modulo(UserHistoryPos + 1, UserHistory.Length);
		}
		public static string? GetUserHistory(int localIndex) {
			string? at = UserHistory[NMath.Modulo(UserHistoryPos - localIndex, UserHistory.Length)];
			return at;
		}

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
			consoleInput.Editor.OnTextInput += ConsoleInput_OnTextInput;
			consoleInput.Editor.Keybinds.AddKeybind([KeyboardLayout.USA.Tilda], () => InGameConsole.CloseConsole());
			consoleInput.PreRenderEditorLines += ConsoleInput_PreRenderEditorLines;
			consoleInput.OnTab += ConsoleInput_OnTab;

			consoleLogs = Add<TextEditor>();
			consoleLogs.Dock = Dock.Fill;
			consoleLogs.TextSize = 12;
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
			consoleInput.Editor.MouseReleaseEvent += (_, _, _) => SetupAutocomplete();

			foreach (var msg in ConsoleSystem.GetMessages()) {
				SetupRow(msg);
			}
			ConsoleSystem.ConsoleMessageWrittenEvent += ConsoleSystem_ConsoleMessageWrittenEvent;

			this.InvalidateChildren(recursive: true);
		}

		private void ConsoleInput_PreRenderEditorLines(TextEditor self, float w, float h) {
			if (autoCompleteStr == null) return;

			self.RenderRowPiece(0, 0, autoCompleteStr, new Raylib_cs.Color(255, 255, 255, 150));
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

		private string? autoCompleteStr;
		private void ConsoleInput_OnTab(TextEditor self) {
			// Autocomplete
			if (!IValidatable.IsValid(autoComplete)) return;
			autoCompleteStr = autoComplete.Tab();
			//ConCommandBase? selected = autoComplete.GetSelected();
			//if (selected == null) return;

			//consoleInput.SetText(selected.Name);
			//consoleInput.SetCaret(selected.Name.Length, 0);
		}

		private void SetupAutocomplete() {
			var args = ConCommandArguments.FromString(consoleInput.GetText());
			if (!IValidatable.IsValid(autoComplete))
				autoComplete = UI.Add<ConsoleAutocomplete>();

			int startX = 0;
			for (int i = 0; i < args.Length - 1; i++) {
				startX += (args.GetString(i)?.Length ?? 0) + 1;
			}
			autoComplete.Position = consoleInput.GetGlobalPosition() + new Vector2F(startX * consoleInput.FontWidth, 40);

			string? basename = args.GetString(0);
			if (basename == null) {
				autoComplete.SetNoMatches();
				return;
			}

			if (args.Length <= 1) {
				var matches = ConCommandBase.FindMatchesThatStartWith(basename, 0, 20);
				string[] names = new string[matches.Length];
				string[] descs = new string[matches.Length];
				for (int i = 0, n = matches.Length; i < n; i++) {
					var match = matches[i];
					names[i] = match.Name;
					descs[i] = match.HelpString;
				}

				autoComplete.SetPotentialMatches(names, descs);
			}
			else {
				ConCommandBase? match = ConCommandBase.Get(basename);
				if (match != null)
					autoComplete.SetPotentialMatches(ConCommandBase.Autocomplete(match, args.Raw, consoleInput.Caret.Column));
				else
					autoComplete.SetNoMatches();
			}
		}

		private int userHistoryPos = 0;
		private void ConsoleInput_OnKeyPressed(Element self, in KeyboardState state, KeyboardKey key) {
			if (IValidatable.IsValid(autoComplete)) {
				if (key == KeyboardLayout.USA.Space && autoComplete.TryGetTabSelection(out string? tabSelection)) {
					// Remove one character because this is a post-hook
					var text = consoleInput.GetText();
					var userLen = text.Length;
					var currentCaret = consoleInput.Caret.StartCol;

					var args = ConCommandArguments.FromString(text);
					int startX = 0;
					for (int i = 0; i < args.Length - 1; i++) {
						startX += (args.GetString(i)?.Length ?? 0) + 1;
					}

					consoleInput.SetSelection(startX, 0, userLen, 0);
					consoleInput.InsertText(tabSelection + " ");
					autoComplete.Reset();
					autoCompleteStr = null;
				}
				else if (key == KeyboardLayout.USA.Up) {
					int newPos = userHistoryPos + 1;
					string? txt;
					if (newPos == 0)
						txt = "";
					else
						txt = GetUserHistory(newPos);
					if (txt != null) {
						userHistoryPos++;
						consoleInput.SetText(txt);
						consoleInput.SetCaret(txt.Length, 0);
					}
					else {
						consoleInput.SetCaret(consoleInput.GetText().Length, 0);
					}
				}
				else if (key == KeyboardLayout.USA.Down) {
					int newPos = userHistoryPos - 1;
					string? txt;
					if (newPos == 0)
						txt = "";
					else
						txt = GetUserHistory(newPos);
					if (txt != null) {
						userHistoryPos--;
						consoleInput.SetText(txt);
						consoleInput.SetCaret(txt.Length, 0);
					}
					else {
						consoleInput.SetCaret(consoleInput.GetText().Length, 0);
					}
				}
				else if (key != KeyboardLayout.USA.Tab) {
					autoComplete.Reset();
					autoCompleteStr = null;
				}
			}
			SetupAutocomplete();
		}
		private void ConsoleInput_OnTextInput(Element self, in KeyboardState state, string inText) {
			SetupAutocomplete();
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
			var txt = self.GetText();
			Logs.Print("> " + txt);
			ConsoleSystem.ParseOneCommand(txt);
			autoComplete?.Remove();
			MainThread.RunASAP(() => {
				consoleLogs.SetScroll(1);
			});

			PushUserHistory(txt);
			userHistoryPos = 0;

		}

		public override void Paint(float width, float height) {
			base.Paint(width, height);
		}
	}
	public static class InGameConsole
	{
		private static ConsoleWindow? inputPanel = null;
		public static ConsoleWindow? Instance => inputPanel;
		public static void OpenConsole(Element parent) {
			if (IValidatable.IsValid(inputPanel)) {
				CloseConsole();
				return;
			}

			inputPanel = parent.Add<ConsoleWindow>();

			ConsoleSystem.AddScreenBlocker(inputPanel);
			inputPanel.Removed += (self) => OnConsoleClosed();
		}
		public static void CloseConsole() {
			inputPanel?.Remove();
			OnConsoleClosed();
		}
		private static void OnConsoleClosed() {
			ConsoleSystem.RemoveScreenBlocker(inputPanel);
		}
		public static void HookToLevel(this Level level) => level.Keybinds.AddKeybind([KeyboardLayout.USA.Tilda], () => OpenConsole(level.UI));
	}
}
