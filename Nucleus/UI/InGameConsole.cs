using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Common.Input;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI;

using SDL;

using System.Diagnostics.CodeAnalysis;

namespace Nucleus
{
	public class ConsoleAutocomplete : Panel
	{
		public string[]? PotentialMatches;
		public string?[]? HelpStrings;
		public int MaxVisibleRows = 12;

		public void SetPotentialMatches(string[] potentialMatches, string?[]? helpStrings = null) {
			PotentialMatches = potentialMatches;
			HelpStrings = helpStrings;
		}
		public void SetNoMatches() {
			PotentialMatches = null;
			HelpStrings = null;
		}

		public ConsoleAutocomplete(Element? parent) : base(parent) {
			Clipping = false;
		}

		public override void Paint(float width, float height) {
			if (PotentialMatches == null || PotentialMatches.Length == 0) return;

			int visibleCount = Math.Min(PotentialMatches.Length, MaxVisibleRows);

			int scrollStart = 0;
			if (selectionIndex >= 0) {
				if (selectionIndex >= scrollStart + visibleCount)
					scrollStart = selectionIndex - visibleCount + 1;
				if (selectionIndex < scrollStart)
					scrollStart = selectionIndex;
			}
			int scrollEnd = scrollStart + visibleCount;

			int maxNameLen = 0;
			string maxNameStr = "";
			int maxHelpLen = 0;
			string maxHelpStr = "";

			for (int i = scrollStart; i < scrollEnd && i < PotentialMatches.Length; i++) {
				string s = PotentialMatches[i];
				if (s.Length > maxNameLen) {
					maxNameStr = s;
					maxNameLen = s.Length;
				}

				if (HelpStrings != null && i < HelpStrings.Length) {
					string? d = HelpStrings[i];
					if (d != null && d.Length > maxHelpLen) {
						maxHelpStr = d;
						maxHelpLen = d.Length;
					}
				}
			}

			var textSize = 14;
			var rowPadding = 3;
			var rowHeight = textSize + rowPadding;
			var horizPad = 6;

			float maxNameX = Graphics2D.GetTextSize(maxNameStr, "Consolas", textSize).X;
			float maxHelpX = maxHelpStr.Length > 0 ? Graphics2D.GetTextSize(maxHelpStr, "Consolas", textSize).X : 0;

			float totalWidth = maxNameX + horizPad * 2;
			if (maxHelpX > 0)
				totalWidth += 12 + maxHelpX + horizPad;

			float totalHeight = visibleCount * rowHeight;

			Graphics2D.SetDrawColor(2, 2, 2, 200);
			Graphics2D.DrawRectangle(new(-1, -4), new(totalWidth + 2, totalHeight + 2));

			for (int vi = 0; vi < visibleCount; vi++) {
				int i = scrollStart + vi;
				if (i >= PotentialMatches.Length) break;

				var yPos = -4 + (vi * rowHeight);

				if (selectionIndex == i)
					Graphics2D.SetDrawColor(60, 60, 80, 240);
				else if (vi % 2 == 0)
					Graphics2D.SetDrawColor(15, 15, 18, 210);
				else
					Graphics2D.SetDrawColor(10, 10, 12, 210);

				Graphics2D.DrawRectangle(new(0, yPos), new(totalWidth, rowHeight));

				if (selectionIndex == i)
					Graphics2D.SetDrawColor(255, 255, 255);
				else
					Graphics2D.SetDrawColor(230, 230, 230);

				Graphics2D.DrawText(new(horizPad, yPos + 1), PotentialMatches[i], "Consolas", textSize);

				if (HelpStrings != null && i < HelpStrings.Length) {
					string? d = HelpStrings[i];
					if (d != null) {
						if (selectionIndex == i)
							Graphics2D.SetDrawColor(180, 180, 200);
						else
							Graphics2D.SetDrawColor(140, 140, 150);

						Graphics2D.DrawText(new(maxNameX + horizPad + 12, yPos + 1), d, "Consolas", textSize);
					}
				}
			}

			if (PotentialMatches.Length > MaxVisibleRows) {
				float indicatorY = -4 + totalHeight + 2;
				Graphics2D.SetDrawColor(100, 100, 120, 180);
				string scrollInfo = $" [{scrollStart + 1}-{Math.Min(scrollEnd, PotentialMatches.Length)} of {PotentialMatches.Length}] ";
				Graphics2D.DrawText(new(horizPad, indicatorY), scrollInfo, "Consolas", 11);
			}
		}

		int selectionIndex = -1;

		internal void Reset() {
			selectionIndex = -1;
		}

		internal void SelectNext() {
			if ((PotentialMatches?.Length ?? 0) == 0) return;
			selectionIndex++;
			if (selectionIndex >= PotentialMatches!.Length)
				selectionIndex = 0;
		}

		internal void SelectPrev() {
			if ((PotentialMatches?.Length ?? 0) == 0) return;
			if (selectionIndex <= 0)
				selectionIndex = PotentialMatches!.Length - 1;
			else
				selectionIndex--;
		}

		internal string? GetSelected() {
			if (PotentialMatches == null || selectionIndex < 0 || selectionIndex >= PotentialMatches.Length)
				return null;
			return PotentialMatches[selectionIndex];
		}

		internal string? SelectAndGet() {
			if ((PotentialMatches?.Length ?? 0) == 0) return null;
			if (selectionIndex < 0)
				selectionIndex = 0;
			return PotentialMatches![selectionIndex];
		}

		public bool HasSelection => selectionIndex >= 0 && PotentialMatches != null && selectionIndex < PotentialMatches.Length;
		public int SelectionIndex => selectionIndex;
	}

	public class ConsoleWindow : Panel
	{
		public static string[] UserHistory = new string[256];
		public static int UserHistoryPos = 0;
		public static int UserHistoryCount = 0;
		public static void PushUserHistory(string str) {
			var last = UserHistory[NMath.Modulo(UserHistoryPos - 1, UserHistory.Length)];
			if (str == last && last != null) return;

			UserHistory[UserHistoryPos] = str;
			UserHistoryPos = NMath.Modulo(UserHistoryPos + 1, UserHistory.Length);
			if (UserHistoryCount < UserHistory.Length)
				UserHistoryCount++;
		}
		public static string? GetUserHistory(int localIndex) {
			if (localIndex <= 0 || localIndex > UserHistoryCount) return null;
			string? at = UserHistory[NMath.Modulo(UserHistoryPos - localIndex, UserHistory.Length)];
			return at;
		}

		ConsoleLogs consoleLogs;
		ConsoleInput consoleInput;
		ConsoleAutocomplete? autoComplete;

		bool isArgumentAutocomplete = false;
		string argumentPrefix = "";


		protected override void OnThink() {
			Position = new(8, Level.GetConsoleOverlaySettings().Position.Y);
			Size = new(GetParent()!.Size.W - 16, 384);
		}

		internal class ConsoleLogs : TextEditor
		{
			ConsoleWindow parent;
			public ConsoleLogs(ConsoleWindow parent) : base(parent) {
				this.parent = parent;
				SetPaintBorderEnabled(false);
				HScrollbar?.SetPaintBorderEnabled(false);
				VScrollbar?.SetPaintBorderEnabled(false);
				Readonly = true;
			}
			protected override void OnThink() {
				if (IValidatable.IsValid(parent.autoComplete) && !parent.consoleInput.Editor.IsKeyboardFocused()) {
					parent.autoComplete.Remove();
				}
				base.OnThink();
			}
		}

		internal class ConsoleInput : TextEditor
		{
			ConsoleWindow parent;
			public ConsoleInput(ConsoleWindow parent) : base(parent) {
				this.parent = parent;
				SetPaintBorderEnabled(false);
			}
			protected override void OnThink() {

			}
			protected override bool MouseRelease(Element self, FrameState state, ButtonCode button) {
				if (!IsHovered()) return true;
				base.MouseRelease(self, state, button);
				parent.SetupAutocomplete();
				return true;
			}
		}

		public ConsoleWindow(Element? parent) : base(parent) {
			Position = new(8, Level.GetConsoleOverlaySettings().Position.Y);
			Size = new(GetParent()!.Size.W - 16, 384);

			this.DockMargin = RectangleF.TLRB(8);
			this.BorderSize = 0;

			consoleInput = new ConsoleInput(this);
			consoleInput.Size = new(0, 32);
			consoleInput.Dock = Dock.Bottom;
			consoleInput.Multiline = false;
			consoleInput.ShowDetails = false;
			consoleInput.ShowGutter = false;
			consoleInput.TriggerExecuteOnEnter = true;
			consoleInput.OnExecute += ConsoleInput_OnExecute;

			consoleInput.OnKeyPressed += ConsoleInput_OnKeyPressed;
			consoleInput.OnTextInput += ConsoleInput_OnTextInput;
			consoleInput.Editor.Keybinds.AddKeybind([ButtonCode.KeyBackquote], () => InGameConsole.CloseConsole());
			consoleInput.Editor.Keybinds.AddKeybind([ButtonCode.KeyUp], HandleArrowUp);
			consoleInput.Editor.Keybinds.AddKeybind([ButtonCode.KeyDown], HandleArrowDown);
			consoleInput.PreRenderEditorLines += ConsoleInput_PreRenderEditorLines;
			consoleInput.OnTab += ConsoleInput_OnTab;

			consoleLogs = new ConsoleLogs(this);
			consoleLogs.Dock = Dock.Fill;
			consoleLogs.SetTextSize(12);
			consoleLogs.DockMargin = new(0, 0, 0, 0);
			consoleLogs.Readonly = true;
			consoleLogs.ShowDetails = false;
			consoleLogs.ShowGutter = false;
			consoleLogs.Multiline = true;
			consoleLogs.Highlighter = new ConsoleLogHighlighter();
			consoleLogs.SetScroll(1f);

			consoleLogs.SetPaintBackgroundEnabled(false);
			consoleInput.SetPaintBackgroundEnabled(false);

			consoleInput.KeyboardFocus();
			var msgList = ConsoleSystem.GetAllMessagesList();
			msgList.BeginRead();
			int msgCount = msgList.ComputeCount();
			Span<int> offsets = stackalloc int[msgCount];
			int found = msgList.GetMessages(offsets, out _);
			for (int i = 0; i < found; i++) {
				if (msgList.GetMessageAt(offsets, i, out var header, out var text)) {
					LiveConsoleMessage live = new() {
						Header = header,
						Text = text
					};
					SetupRow(in live);
				}
			}
			msgList.EndRead();
			ConsoleSystem.ConsoleMessageWrittenEvent += ConsoleSystem_ConsoleMessageWrittenEvent;

			this.InvalidateChildren(recursive: true);
			SetupAutocomplete();
		}

		private void ConsoleInput_PreRenderEditorLines(TextEditor self, float w, float h) {
			if (autoCompleteStr == null) return;
			self.RenderRowPiece(0, 0, autoCompleteStr, new Color(255, 255, 255, 150));
		}

		private void SetupRow(ref readonly LiveConsoleMessage message) {
			consoleLogs.AppendLine($"[{message.Header.Time.ToString(Logs.TimeFormat)}] [{Logs.LevelToConsoleString(message.Header.Level)}] {message.Text}");
			if (consoleLogs.Rows.Count > ConsoleSystem.MaxConsoleMessages)
				consoleLogs.RemoveLine(0);
			consoleLogs.ScrollToLine(consoleLogs.Rows.Count, 1f);
		}
		private void ConsoleSystem_ConsoleMessageWrittenEvent(ref readonly LiveConsoleMessage message) {
			SetupRow(in message);
		}

		private string? autoCompleteStr;
		private string savedInputBeforeHistory = "";
		private bool browsingHistory = false;

		private static string QuoteIfNeeded(string arg) {
			if (arg.Contains(' '))
				return $"\"{arg}\"";
			return arg;
		}

		private string BuildCommandWithArg(string selected) {
			return argumentPrefix + QuoteIfNeeded(selected) + " ";
		}

		private void ConsoleInput_OnTab(TextEditor self) {
			if (!IValidatable.IsValid(autoComplete)) return;

			string? match = autoComplete.SelectAndGet();
			if (match == null) return;

			if (isArgumentAutocomplete) {
				string built = BuildCommandWithArg(match);
				consoleInput.SetText(built);
				consoleInput.SetCaret(built.Length, 0);
				autoCompleteStr = built;
			}
			else {
				consoleInput.SetText(match);
				consoleInput.SetCaret(match.Length, 0);
				autoCompleteStr = match;
			}
		}

		private void EnsureAutocompletePanel() {
			if (!IValidatable.IsValid(autoComplete)) {
				autoComplete = new ConsoleAutocomplete(consoleInput);
				autoComplete.Dock = Dock.Bottom;
				autoComplete.Size = new(0, 0);
			}
		}

		private void SetupAutocomplete() {
			EnsureAutocompletePanel();
			autoComplete.Reset();
			autoCompleteStr = null;
			isArgumentAutocomplete = false;
			argumentPrefix = "";

			var inputText = consoleInput.GetText();

			if (string.IsNullOrEmpty(inputText)) {
				autoComplete.ChildRenderOffset = new(0, 12);
				SetupHistoryAutocomplete();
				return;
			}

			browsingHistory = false;

			TokenizedCommand tokenized = new();
			tokenized.Tokenize(inputText);

			string firstToken = tokenized.ArgC() > 0 ? tokenized.Arg(0).ToString() : inputText.Trim();
			ConCommandBase? exactMatch = cvar.FindCommandBase(firstToken);

			if (exactMatch != null && inputText.Contains(' '))
				SetupArgumentAutocomplete(exactMatch, inputText, tokenized);
			else
				SetupNameAutocomplete(inputText.Trim());

			float xOffset = 0;
			if (exactMatch != null && exactMatch.OnAutocomplete != null && isArgumentAutocomplete) {
				xOffset = Graphics2D.GetTextSize(argumentPrefix, "Consolas", consoleInput.GetTextSize()).X + 4;
			}
			autoComplete.ChildRenderOffset = new(xOffset, 12);
		}

		private void SetupHistoryAutocomplete() {
			if (UserHistoryCount == 0) {
				autoComplete?.SetNoMatches();
				return;
			}

			var entries = new List<string>();
			for (int i = 1; i <= UserHistoryCount; i++) {
				string? entry = GetUserHistory(i);
				if (entry != null && entry.Length > 0 && !entries.Contains(entry))
					entries.Add(entry);
			}

			if (entries.Count == 0) {
				autoComplete?.SetNoMatches();
				return;
			}

			autoComplete?.SetPotentialMatches(entries.ToArray());
		}

		private void SetupNameAutocomplete(string partial) {
			if (string.IsNullOrEmpty(partial)) {
				autoComplete?.SetNoMatches();
				return;
			}

			var matches = new List<(string name, string? help)>();

			var current = cvar.GetCommands();
			while (current != null) {
				if (current.Registered && current.Name.StartsWith(partial, StringComparison.OrdinalIgnoreCase)) {
					string helpText = current.HelpString;

					if (!current.IsCommand() && current is ConVar cv) {
						string valStr = cv.GetString().ToString();
						helpText = string.IsNullOrEmpty(helpText)
							? $"= \"{valStr}\""
							: $"= \"{valStr}\" - {helpText}";
					}

					matches.Add((current.Name, helpText));
				}
				current = current.Next;
			}

			if (matches.Count == 0) {
				autoComplete?.SetNoMatches();
				return;
			}

			matches.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

			autoComplete?.SetPotentialMatches(
				matches.Select(m => m.name).ToArray(),
				matches.Select(m => m.help).ToArray()
			);
		}

		private void SetupArgumentAutocomplete(ConCommandBase cmd, string fullInput, TokenizedCommand tokenized) {
			isArgumentAutocomplete = true;

			int currentArgIndex = tokenized.ArgC();
			bool endsWithSpace = fullInput.EndsWith(' ');
			bool hasUnclosedQuote = tokenized.HasUnclosedQuote();

			if (hasUnclosedQuote) {
				currentArgIndex = tokenized.ArgC() - 1;
			}
			else if (!endsWithSpace) {
				currentArgIndex = tokenized.ArgC() - 1;
			}

			if (currentArgIndex < 1)
				currentArgIndex = 1;

			int prefixEnd;
			if (hasUnclosedQuote || !endsWithSpace) {
				prefixEnd = tokenized.GetArgStartPosition(currentArgIndex);
			}
			else {
				prefixEnd = fullInput.Length;
			}
			argumentPrefix = fullInput[..prefixEnd];

			if (cmd.OnAutocomplete != null) {
				string[] returns = Array.Empty<string>();
				string[]? helpReturns = null;

				cmd.OnAutocomplete(cmd, fullInput, tokenized, currentArgIndex, ref returns, ref helpReturns);

				if (returns.Length > 0)
					autoComplete?.SetPotentialMatches(returns, helpReturns);
				else
					autoComplete?.SetNoMatches();
			}
			else {
				if (!string.IsNullOrEmpty(cmd.HelpString)) {
					autoComplete?.SetPotentialMatches(
						new[] { fullInput.TrimEnd() },
						new[] { cmd.HelpString }
					);
				}
				else {
					autoComplete?.SetNoMatches();
				}
			}
		}

		private int userHistoryPos = 0;

		private void NavigateHistory(int direction) {
			var inputText = consoleInput.GetText();

			if (!browsingHistory) {
				savedInputBeforeHistory = inputText;
				browsingHistory = true;
				userHistoryPos = 0;
			}

			int newPos = userHistoryPos + direction;

			if (newPos <= 0) {
				userHistoryPos = 0;
				browsingHistory = false;
				consoleInput.SetText(savedInputBeforeHistory);
				consoleInput.SetCaret(savedInputBeforeHistory.Length, 0);
				SetupAutocomplete();
				return;
			}

			string? txt = GetUserHistory(newPos);
			if (txt != null) {
				userHistoryPos = newPos;
				consoleInput.SetText(txt);
				consoleInput.SetCaret(txt.Length, 0);
				SetupAutocomplete();
			}
		}

		private void HandleArrowUp() {
			if (IValidatable.IsValid(autoComplete) && (autoComplete.PotentialMatches?.Length ?? 0) > 0) {
				autoComplete.SelectPrev();
				return;
			}

			NavigateHistory(1);
		}

		private void HandleArrowDown() {
			if (IValidatable.IsValid(autoComplete) && (autoComplete.PotentialMatches?.Length ?? 0) > 0) {
				autoComplete.SelectNext();
				return;
			}

			NavigateHistory(-1);
		}

		private void ConsoleInput_OnKeyPressed(TextEditor self, in KeyboardState state, ButtonCode key) {
			if (IValidatable.IsValid(autoComplete)) {
				if (key == ButtonCode.KeySpace && autoComplete.HasSelection) {
					string? selected = autoComplete.GetSelected();
					if (selected != null) {
						if (isArgumentAutocomplete) {
							string built = BuildCommandWithArg(selected);
							consoleInput.SetText(built);
							consoleInput.SetCaret(built.Length, 0);
						}
						else {
							string committed = selected + " ";
							consoleInput.SetText(committed);
							consoleInput.SetCaret(committed.Length, 0);
						}

						autoComplete.Reset();
						autoCompleteStr = null;
						browsingHistory = false;
						SetupAutocomplete();
						return;
					}
				}

				if (key != ButtonCode.KeyTab && key != ButtonCode.KeyUp && key != ButtonCode.KeyDown && !state.ControlDown) {
					autoComplete.Reset();
					autoCompleteStr = null;
					browsingHistory = false;
				}
			}

			SetupAutocomplete();
		}

		private void ConsoleInput_OnTextInput(TextEditor self, in KeyboardState state, string inText) {
			browsingHistory = false;
			userHistoryPos = 0;
			SetupAutocomplete();
		}

		protected override void OnRemoval() {
			base.OnRemoval();
			autoComplete?.Remove();
		}

		private void ConsoleInput_OnExecute(TextEditor self) {
			var txt = self.GetText();
			if (string.IsNullOrWhiteSpace(txt)) return;

			Logs.Print("> " + txt);
			Cbuf.AddText(txt);
			autoComplete?.Remove();
			MainThread.RunASAP(() => {
				consoleLogs.SetScroll(1);
			});

			PushUserHistory(txt);
			userHistoryPos = 0;
			browsingHistory = false;
			savedInputBeforeHistory = "";
			self.SetText("");
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

			inputPanel = new ConsoleWindow(parent);

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
		public static void HookToLevel(this Level level) => level.Keybinds.AddKeybind([ButtonCode.KeyBackquote], () => OpenConsole(level.RootPanel));
	}
}