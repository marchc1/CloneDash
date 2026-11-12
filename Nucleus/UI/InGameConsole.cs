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

		private const string FONT = "Consolas";
		private const float TEXT_SIZE = 14;
		private const float ROW_HEIGHT = 17;
		private const float HORIZ_PAD = 6;
		private const float HELP_GAP = 12;
		private const float FOOTER_HEIGHT = 14;

		public void SetPotentialMatches(string[] potentialMatches, string?[]? helpStrings = null) {
			PotentialMatches = potentialMatches;
			HelpStrings = helpStrings;
		}
		public void SetNoMatches() {
			PotentialMatches = null;
			HelpStrings = null;
		}

		public ConsoleAutocomplete(Element? parent) : base(parent) {
			SetPaintBackgroundEnabled(false);
			SetPaintBorderEnabled(false);
			SetPassthru(true);
			MakePopup();
		}

		int selectionIndex = -1;

		/// <summary>
		/// Highlighted row, -1 for none
		/// </summary>
		public int SelectionIndex {
			get => selectionIndex;
			set => selectionIndex = value;
		}

		int VisibleCount => Math.Min(PotentialMatches?.Length ?? 0, MaxVisibleRows);

		int ScrollStart {
			get {
				int start = 0;
				int visible = VisibleCount;
				if (selectionIndex >= visible)
					start = selectionIndex - visible + 1;
				return start;
			}
		}

		float nameWidth;

		/// <summary>
		/// Sizes to the rows that are showing right now.
		/// </summary>
		public void FitToContents() {
			int visible = VisibleCount;
			if (visible == 0) {
				SetVisible(false);
				return;
			}
			SetVisible(true);

			float helpWidth = 0;
			nameWidth = 0;
			int start = ScrollStart;
			for (int i = start; i < start + visible; i++) {
				nameWidth = MathF.Max(nameWidth, Graphics2D.GetTextSize(PotentialMatches![i], FONT, TEXT_SIZE).X);
				if (HelpStrings != null && i < HelpStrings.Length && HelpStrings[i] is string help)
					helpWidth = MathF.Max(helpWidth, Graphics2D.GetTextSize(help, FONT, TEXT_SIZE).X);
			}

			float contentWidth = nameWidth + HORIZ_PAD * 2;
			if (helpWidth > 0)
				contentWidth += HELP_GAP + helpWidth + HORIZ_PAD;
			float footer = PotentialMatches!.Length > MaxVisibleRows ? FOOTER_HEIGHT : 0;
			float contentHeight = visible * ROW_HEIGHT + footer;
			Size = new(MathF.Ceiling(contentWidth), MathF.Ceiling(contentHeight));
		}

		public override void Paint(float width, float height) {
			int visible = VisibleCount;
			if (visible == 0) return;
			int start = ScrollStart;

			Graphics2D.SetDrawColor(2, 2, 2, 200);
			Graphics2D.DrawRectangle(0, 0, width, height);

			for (int row = 0; row < visible; row++) {
				int i = start + row;
				float y = row * ROW_HEIGHT;

				if (selectionIndex == i)
					Graphics2D.SetDrawColor(60, 60, 80, 240);
				else if (row % 2 == 0)
					Graphics2D.SetDrawColor(15, 15, 18, 210);
				else
					Graphics2D.SetDrawColor(10, 10, 12, 210);
				Graphics2D.DrawRectangle(0, y, width, ROW_HEIGHT);

				Graphics2D.SetDrawColor(selectionIndex == i ? new Color(255, 255, 255) : new Color(230, 230, 230));
				Graphics2D.DrawText(new(HORIZ_PAD, y + 1), PotentialMatches![i], FONT, TEXT_SIZE);

				if (HelpStrings != null && i < HelpStrings.Length && HelpStrings[i] is string help) {
					Graphics2D.SetDrawColor(selectionIndex == i ? new Color(180, 180, 200) : new Color(140, 140, 150));
					Graphics2D.DrawText(new(nameWidth + HORIZ_PAD + HELP_GAP, y + 1), help, FONT, TEXT_SIZE);
				}
			}

			if (PotentialMatches!.Length > MaxVisibleRows) {
				Graphics2D.SetDrawColor(100, 100, 120, 180);
				Graphics2D.DrawText(new(HORIZ_PAD, visible * ROW_HEIGHT),
					$" [{start + 1}-{start + visible} of {PotentialMatches.Length}] ", FONT, 11);
			}
		}
	}

	public class ConsoleWindow : Panel
	{
		public const int MAX_HISTORY = 256;

		/// <summary>
		/// Commands typed into the console, oldest first
		/// </summary>
		public static List<string> History { get; } = [];

		public static void AddToHistory(string command) {
			command = command.Trim();
			if (command.Length == 0)
				return;
			History.RemoveAll(h => string.Equals(h, command, StringComparison.OrdinalIgnoreCase));
			History.Add(command);
			while (History.Count > MAX_HISTORY)
				History.RemoveAt(0);
		}

		ConsoleLogs consoleLogs;
		ConsoleInput consoleInput;
		ConsoleAutocomplete? autoComplete;

		/// <param name="Fill">What goes in the input when this is picked</param>
		readonly record struct Completion(string Fill, string Name, string? Help);

		readonly List<Completion> completions = [];
		int nextCompletion;
		bool cycling;
		bool settingText;
		float listOffsetX;


		protected override void OnThink() {
			Position = new(8, Level.GetConsoleOverlaySettings().Position.Y);
			Size = new(GetParent()!.Size.W - 16, 384);
			PlaceList();
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

			this.
			DockMargin = RectangleF.TLRB(8);
			this.BorderSize = 0;

			consoleInput = new ConsoleInput(this);
			consoleInput.			Size = new(0, 32);
			consoleInput.			Dock = Dock.Bottom;
			consoleInput.Multiline = false;
			consoleInput.ShowDetails = false;
			consoleInput.ShowGutter = false;
			consoleInput.TriggerExecuteOnEnter = true;
			consoleInput.OnExecute += ConsoleInput_OnExecute;

			consoleInput.OnTextEdited += ConsoleInput_OnTextEdited;
			consoleInput.InterceptKey = ConsoleInput_InterceptKey;
			consoleInput.Editor.Keybinds.AddKeybind([ButtonCode.KeyBackquote], () => InGameConsole.CloseConsole());

			consoleLogs = new ConsoleLogs(this);
			consoleLogs.			Dock = Dock.Fill;
			consoleLogs.			TextSize = 12;
			consoleLogs.			DockMargin = new(0, 0, 0, 0);
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

		private void SetupRow(ref readonly LiveConsoleMessage message) {
			consoleLogs.AppendLine($"[{message.Header.Time.ToString(Logs.TimeFormat)}] [{Logs.LevelToConsoleString(message.Header.Level)}] {message.Text}");
			if (consoleLogs.Rows.Count > ConsoleSystem.MaxConsoleMessages)
				consoleLogs.RemoveLine(0);
			consoleLogs.ScrollToLine(consoleLogs.Rows.Count, 1f);
		}
		private void ConsoleSystem_ConsoleMessageWrittenEvent(ref readonly LiveConsoleMessage message) {
			SetupRow(in message);
		}

		private static string QuoteIfNeeded(string arg) {
			if (arg.Contains(' '))
				return $"\"{arg}\"";
			return arg;
		}

		private void SetInput(string text) {
			settingText = true;
			consoleInput.SetText(text);
			consoleInput.SetCaret(text.Length, 0);
			settingText = false;
		}

		private void ConsoleInput_OnTextEdited(TextEditor self) {
			if (settingText)
				return;
			SetupAutocomplete();
		}

		// Tab/Down go forward through the list, Shift+Tab/Up go back. Like Source.
		private bool ConsoleInput_InterceptKey(TextEditor self, in KeyboardState state, ButtonCode key) {
			switch (key) {
				case ButtonCode.KeyTab: Complete(state.ShiftDown); return true;
				case ButtonCode.KeyDown: Complete(false); return true;
				case ButtonCode.KeyUp: Complete(true); return true;
			}
			return false;
		}

		private void Complete(bool reverse) {
			if (!cycling) {
				nextCompletion = 0;
				cycling = true;
			}

			if (reverse) {
				nextCompletion -= 2;
				if (nextCompletion < 0)
					nextCompletion = completions.Count - 1;
			}

			if (nextCompletion < 0 || nextCompletion >= completions.Count)
				nextCompletion = 0;
			if (completions.Count == 0)
				return;

			SetInput(completions[nextCompletion].Fill);
			ShowList(nextCompletion);
			nextCompletion++;
		}

		[MemberNotNull(nameof(autoComplete))]
		private void EnsureAutocompletePanel() {
			if (!IValidatable.IsValid(autoComplete))
				autoComplete = new ConsoleAutocomplete(UI);
		}

		private void ShowList(int selected) {
			EnsureAutocompletePanel();
			if (completions.Count == 0)
				autoComplete.SetNoMatches();
			else {
				autoComplete.SetPotentialMatches([.. completions.Select(c => c.Name)], [.. completions.Select(c => c.Help)]);
				autoComplete.SelectionIndex = selected;
			}
			autoComplete.FitToContents();
			PlaceList();
		}

		private void PlaceList() {
			if (!IValidatable.IsValid(autoComplete))
				return;
			var pos = consoleInput.GetGlobalPosition();
			autoComplete.Position = new(MathF.Round(pos.X + listOffsetX), MathF.Round(pos.Y + consoleInput.GetRenderBounds().H + 2));
			autoComplete.MoveToFront();
		}

		/// <summary>
		/// Rebuilds the suggestions from what's typed, or the history when it's empty.
		/// </summary>
		private void SetupAutocomplete() {
			cycling = false;
			completions.Clear();
			listOffsetX = 0;

			var inputText = consoleInput.GetText();
			if (string.IsNullOrEmpty(inputText)) {
				foreach (var command in History)
					completions.Add(new(command.Contains(' ') ? command : command + " ", command, null));
				// History only shows once you start going through it
				EnsureAutocompletePanel();
				autoComplete.SetNoMatches();
				autoComplete.FitToContents();
				return;
			}

			TokenizedCommand tokenized = new();
			tokenized.Tokenize(inputText);
			string firstToken = tokenized.ArgC() > 0 ? tokenized.Arg(0).ToString() : inputText.Trim();

			if (inputText.Contains(' ')) {
				ConCommandBase? cmd = cvar.FindCommandBase(firstToken);
				if (cmd != null)
					AddArgumentCompletions(cmd, inputText, tokenized);
			}
			else
				AddNameCompletions(inputText.Trim());

			ShowList(-1);
		}

		private void AddNameCompletions(string partial) {
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

					completions.Add(new(current.Name + " ", current.Name, helpText));
				}
				current = current.Next;
			}

			completions.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		}

		private void AddArgumentCompletions(ConCommandBase cmd, string fullInput, TokenizedCommand tokenized) {
			int currentArgIndex = tokenized.ArgC();
			bool endsWithSpace = fullInput.EndsWith(' ');
			bool hasUnclosedQuote = tokenized.HasUnclosedQuote();

			if (hasUnclosedQuote || !endsWithSpace)
				currentArgIndex = tokenized.ArgC() - 1;
			if (currentArgIndex < 1)
				currentArgIndex = 1;

			int prefixEnd = hasUnclosedQuote || !endsWithSpace ? tokenized.GetArgStartPosition(currentArgIndex) : fullInput.Length;
			string argumentPrefix = fullInput[..prefixEnd];

			if (cmd.OnAutocomplete != null) {
				string[] returns = [];
				string[]? helpReturns = null;
				cmd.OnAutocomplete(cmd, fullInput, tokenized, currentArgIndex, ref returns, ref helpReturns);

				for (int i = 0; i < returns.Length; i++)
					completions.Add(new(argumentPrefix + QuoteIfNeeded(returns[i]) + " ", returns[i],
						helpReturns != null && i < helpReturns.Length ? helpReturns[i] : null));
				listOffsetX = Graphics2D.GetTextSize(argumentPrefix, "Consolas", consoleInput.TextSize).X + 4;
			}
			else if (!string.IsNullOrEmpty(cmd.HelpString))
				completions.Add(new(fullInput, fullInput.TrimEnd(), cmd.HelpString));
		}

		protected override void OnRemoval() {
			base.OnRemoval();
			ConsoleSystem.ConsoleMessageWrittenEvent -= ConsoleSystem_ConsoleMessageWrittenEvent;
			autoComplete?.Remove();
		}

		private void ConsoleInput_OnExecute(TextEditor self) {
			var txt = self.GetText();
			if (string.IsNullOrWhiteSpace(txt)) return;

			Logs.Print("> " + txt);
			Cbuf.AddText(txt);
			MainThread.RunASAP(() => {
				consoleLogs.SetScroll(1);
			});

			AddToHistory(txt);
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