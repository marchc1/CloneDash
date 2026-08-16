using Nucleus.Commands;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.ExceptionServices;

namespace Nucleus.Engine;

[MarkForStaticConstruction]
public static class PanicSystem
{
	// ReSharper disable once InconsistentNaming
	public static readonly ConCommand panic = new("panic", (_, in _) => {
		if (!Debugger.IsAttached)
			throw new Exception("panic concommand called");

		try {
			throw new Exception("force-panic (despite panic system deactivated due to presence of debugger)");
		}
		catch (Exception ex) {
			ExceptionDispatchInfo edi = ExceptionDispatchInfo.Capture(ex);
			if (!Panic(edi)) {
				edi.Throw();
			}
		}

		throw new Exception("panic concommand called");
	}, FCvar.DevelopmentOnly, "Tests the PanicSystem.Panic method (NOTE: this *will* crash the engine!).");

	// ReSharper disable once InconsistentNaming
	public static ConCommand interrupt = new("interrupt", (_, in a) => {
		Interrupt(() => {
			Graphics2D.SetDrawColor(255, 0, 0);
			Graphics2D.DrawRectangle(64, 64, 256, 256);
		}, a.Arg(1, 0) > 0, "You should see a red square in the top-left!");
	}, FCvar.DevelopmentOnly, "Tests the PanicSystem.Interrupt method");

	// ReSharper disable InconsistentNaming
	private const string PANIC_FONT = "Noto Sans";
	private const string PANIC_FONT_ARABIC = "Noto Sans Arabic";

	private static readonly string PANIC_FONT_TC = CultureInfo.CurrentCulture.Name switch {
		"zh-HK" => "Noto Sans HK",
		"zh-MO" => "Noto Sans HK",
		_ => "Noto Sans TC",
	};

	private const string PANIC_FONT_SC = "Noto Sans SC";
	private const string PANIC_FONT_KR = "Noto Sans KR";
	private const string PANIC_FONT_JP = "Noto Sans JP";
	private const string PANIC_FONT_CONSOLE = "Noto Sans Mono";
	private const float PANIC_SIZE = 18;
	private const float PANIC_SIZE_CONSOLE = 16;
	// ReSharper restore InconsistentNaming

	private static void RenderLine(ref int textY) => RenderLine(null, ref textY);

	private static void RenderLine(string? line, ref int textY) {
		if (line == null) {
			textY++;
			return;
		}

		Graphics2D.SetDrawColor(0, 0, 0, 220);
		var textSize = Graphics2D.GetTextSize(line, PANIC_FONT_CONSOLE, PANIC_SIZE_CONSOLE);
		Graphics2D.DrawRectangle(0, textY * PANIC_SIZE_CONSOLE, textSize.W + 8, PANIC_SIZE_CONSOLE);

		Graphics2D.SetDrawColor(255, 255, 255);
		Graphics2D.DrawText(new(4, textY * PANIC_SIZE_CONSOLE), line, PANIC_FONT_CONSOLE, PANIC_SIZE_CONSOLE);

		textY++;
	}

	internal static readonly Dictionary<string, string> ErrorMessages = new() {
		{ "A fatal error has occured. Press any key to exit.", PANIC_FONT },
		{ "حدث خطأ فادح. اضغط على أي مفتاح للخروج.", PANIC_FONT },
		{ "Възникнала е фатална грешка. Натиснете който и да е клавиш, за да излезете.", PANIC_FONT_ARABIC },
		{ "出现致命错误。按任意键退出。", PANIC_FONT_SC },
		{ "發生致命錯誤。按任意鍵退出。", PANIC_FONT_TC },
		{ "Došlo k fatální chybě. Stiskněte libovolnou klávesu pro ukončení.", PANIC_FONT },
		{ "Der er opstået en fatal fejl. Tryk på en vilkårlig tast for at afslutte.", PANIC_FONT },
		{ "Er is een fatale fout opgetreden. Druk op een willekeurige toets om af te sluiten.", PANIC_FONT },
		{ "On ilmnenud fataalne viga. Väljumiseks vajutage suvalist klahvi.", PANIC_FONT },
		{ "On tapahtunut kohtalokas virhe. Poistu painamalla mitä tahansa näppäintä.", PANIC_FONT },
		{ "Une erreur fatale s'est produite. Appuyez sur n'importe quelle touche pour quitter.", PANIC_FONT },
		{ "Es ist ein schwerwiegender Fehler aufgetreten. Drücken Sie eine beliebige Taste zum Beenden.", PANIC_FONT },
		{ "Προέκυψε ένα μοιραίο σφάλμα. Πατήστε οποιοδήποτε πλήκτρο για έξοδο.", PANIC_FONT },
		{ "Végzetes hiba történt. Nyomja meg bármelyik billentyűt a kilépéshez.", PANIC_FONT },
		{ "Telah terjadi kesalahan fatal. Tekan sembarang tombol untuk keluar.", PANIC_FONT },
		{ "Si è verificato un errore fatale. Premere un tasto qualsiasi per uscire.", PANIC_FONT },
		{ "致命的なエラーが発生しました。いずれかのキーを押して終了してください。", PANIC_FONT_JP },
		{ "치명적인 오류가 발생했습니다. 종료하려면 아무 키나 누르세요.", PANIC_FONT_KR },
		{ "Ir notikusi fatāla kļūda. Nospiediet jebkuru taustiņu, lai izietu.", PANIC_FONT },
		{ "Įvyko lemtinga klaida. Paspauskite bet kurį klavišą, kad išeitumėte.", PANIC_FONT },
		{ "Det har oppstått en alvorlig feil. Trykk på en hvilken som helst tast for å avslutte.", PANIC_FONT },
		{ "Wystąpił błąd krytyczny. Naciśnij dowolny przycisk, aby wyjść.", PANIC_FONT },
		{ "Ocorreu um erro fatal. Prima qualquer tecla para sair.", PANIC_FONT },
		{ "Ocorreu um erro fatal. Pressione qualquer tecla para sair.", PANIC_FONT },
		{ "A apărut o eroare fatală. Apăsați orice tastă pentru a ieși.", PANIC_FONT },
		{ "Произошла фатальная ошибка. Нажмите любую клавишу, чтобы выйти.", PANIC_FONT },
		{ "Vyskytla sa fatálna chyba. Stlačte ľubovoľné tlačidlo, aby ste ukončili prácu.", PANIC_FONT },
		{ "Zgodila se je usodna napaka. Za izhod pritisnite katero koli tipko.", PANIC_FONT },
		{ "Se ha producido un error fatal. Pulse cualquier tecla para salir.", PANIC_FONT },
		{ "Ett allvarligt fel har inträffat. Tryck på valfri tangent för att avsluta.", PANIC_FONT },
		{ "Ölümcül bir hata oluştu. Çıkmak için herhangi bir tuşa basın.", PANIC_FONT },
		{ "Виникла фатальна помилка. Натисніть будь-яку клавішу для виходу.", PANIC_FONT },
	};

	public static bool Panic(ExceptionDispatchInfo ex) {
		if (EngineCore.ShouldThrowExceptions)
			ex.Throw();

		OSWindow window = EngineCore.Window;

		float oldMaster = Raylib.GetMasterVolume();
		Raylib.SetMasterVolume(0);
		window.Title = "Nucleus Engine - Panicked!";
		window.MinSize = new Vector2F((int)window.Size.W, (int)window.Size.H);
		window.MaxSize = new Vector2F((int)window.Size.W, (int)window.Size.H);
		EngineCore.ShouldThrowExceptions = true;

		// Rudimentary frame loop for crashed state. Kinda emulates an older Mac kernel panic
		Stopwatch time = new();
		Graphics2D.ResetDrawingOffset();
		time.Start();
		int y = 0;
		double lastTime = 0d;

		string[] exLines = ex.SourceException.Message.Split('\n');
		string[] exStkLines = ex.SourceException.StackTrace?.Split('\n') ?? ["<No stack trace available>"];

		Exception? innerEx = ex.SourceException.InnerException;
		string[] innerExLines = innerEx == null ? ["<No inner exception>"] : innerEx.Message.Split('\n');
		string[] innerExStkLines = innerEx?.StackTrace?.Split('\n') ?? ["<No stack trace available>"];
		bool hasRenderedOverlay = false;

		while (true) {
			double now = time.Elapsed.TotalSeconds;
			double elapsed = now - lastTime;
			lastTime = now;
			Rlgl.LoadIdentity();

			if (y < window.Size.H) {
				int elapsedY = (int)((float)elapsed * 1150);
				for (int i = 0; i < 2; i++) {
					// Need to draw on both buffers
					Raylib.DrawRectangle(0, y, (int)window.Size.W, elapsedY, new Color(90, 100, 120, 170));
					Rlgl.DrawRenderBatchActive();
					window.SwapScreenBuffer();
				}

				y += elapsedY;
			}
			else if (!hasRenderedOverlay) {
				// Hopefully it wasn't the font manager that broke!
				Graphics2D.SetDrawColor(255, 255, 255);

				Vector2 box = new System.Numerics.Vector2(0, PANIC_SIZE * ErrorMessages.Count);
				foreach ((string languageLine, string languageFont) in ErrorMessages) {
					Vector2F size = Graphics2D.GetTextSize(languageLine, languageFont, PANIC_SIZE);
					if (size.X > box.X)
						box.X = size.X;
				}

				const int padding = 32;
				const int paddingDiv2 = padding / 2;
				Vector2 center = new System.Numerics.Vector2((window.Size.W / 2) - (box.X / 2),
					(window.Size.H / 2) - (box.Y / 2));
				Raylib.DrawRectangle((int)center.X - paddingDiv2, (int)center.Y - paddingDiv2, (int)box.X + padding,
					(int)box.Y + padding, new Color(10, 220));
				int langLineY = 0;

				foreach ((string line, string font) in ErrorMessages) {
					Graphics2D.DrawText(center.X + (box.X / 2), center.Y + (langLineY * PANIC_SIZE), line, font,
						PANIC_SIZE, Anchor.TopCenter);
					langLineY++;
				}

				int textY = 0;
				RenderLine("A fatal error has occured. Please restart the application.", ref textY);
				RenderLine("Details:", ref textY);
				RenderLine(null, ref textY);

				foreach (string line in exLines)
					RenderLine(line, ref textY);

				RenderLine(ref textY);

				foreach (string line in exStkLines)
					RenderLine($"    {line}", ref textY);

				if (innerEx != null) {
					RenderLine(ref textY);
					RenderLine("Inner exception:", ref textY);

					foreach (string line in innerExLines)
						RenderLine($"    {line}", ref textY);

					RenderLine(ref textY);

					foreach (string line in innerExStkLines)
						RenderLine($"        {line}", ref textY);
				}

				hasRenderedOverlay = true;
				Rlgl.DrawRenderBatchActive();
				window.SwapScreenBuffer();
			}
			else {
				while (true) {
					OSWindow.PropagateEventBuffer();
					if (!window.KeyAvailable(out _, out _) && !window.UserClosed())
						continue;

					Raylib.SetMasterVolume(oldMaster);
					return false;
				}
			}

			Rlgl.DrawRenderBatchActive();
			OS.Wait(hasRenderedOverlay ? 0.2 : 0.005);
		}
	}

	private static bool Interrupting;
	public static bool InInterrupt => Interrupting;

	public static void Interrupt(Action draw, bool problematic, params string?[] messages) {
		if (Interrupting)
			return;

		OSWindow window = EngineCore.Window;

		Interrupting = true;

		float oldMaster = Raylib.GetMasterVolume();
		Raylib.SetMasterVolume(0);

		window.MinSize = new Vector2F((int)window.Size.W, (int)window.Size.H);
		window.MaxSize = new Vector2F((int)window.Size.W, (int)window.Size.H);

		// Rudimentary frame loop for crashed state. Kinda emulates an older Mac kernel panic
		Stopwatch time = new();
		Graphics2D.ResetDrawingOffset();
		time.Start();
		int y = 0;
		double lastTime = 0d;

		bool hasRenderedOverlay = false;

		while (true) {
			double now = time.Elapsed.TotalSeconds;
			double elapsed = now - lastTime;
			lastTime = now;
			Rlgl.LoadIdentity();

			if (y < window.Size.H) {
				int elapsedY = (int)((float)elapsed * 5000);
				for (int i = 0; i < 2; i++) {
					// Need to draw on both buffers
					Raylib.DrawRectangle(0, y, (int)window.Size.W, elapsedY, new(90, 100, 120, 170));
					Rlgl.DrawRenderBatchActive();
					window.SwapScreenBuffer();
				}

				y += elapsedY;
			}
			else if (!hasRenderedOverlay) {
				Graphics2D.SetDrawColor(255, 255, 255);

				// don't feel like making it static right now
				string[] lines = new string[messages.Length + 5];

				if (problematic)
					lines[0] = "An interrupt has occured due to an issue, and the application has temporarily halted.";
				else
					lines[0] = "A debugging interrupt has occured and the application has temporarily halted.";

				lines[1] = "";
				for (int i = 0; i < messages.Length; i++)
					lines[i + 2] = messages[i] ?? "<NULL STRING>";

				lines[^3] = "";
				lines[^2] = "";
				lines[^1] = "Press any key to continue.";

				Vector2 box = new System.Numerics.Vector2(0, PANIC_SIZE * lines.Length);
				foreach (string languageLine in lines) {
					Vector2F size = Graphics2D.GetTextSize(languageLine, PANIC_FONT, PANIC_SIZE);
					if (size.X > box.X)
						box.X = size.X;
				}

				const int padding = 32;
				const int paddingDiv2 = padding / 2;
				Vector2 center = new System.Numerics.Vector2((window.Size.W / 2) - (box.X / 2), padding);
				Raylib.DrawRectangle((int)center.X - paddingDiv2, (int)center.Y - paddingDiv2, (int)box.X + padding,
					(int)box.Y + padding, new Color(10, 220));
				int langLineY = 0;
				foreach (string line in lines) {
					Graphics2D.DrawText(center.X + (box.X / 2), center.Y + (langLineY * PANIC_SIZE), line, PANIC_FONT,
						PANIC_SIZE, Anchor.TopCenter);

					langLineY++;
				}

				draw();
				hasRenderedOverlay = true;
				Rlgl.DrawRenderBatchActive();
				window.SwapScreenBuffer();
			}
			else {
				OSWindow.PropagateEventBuffer();
				if (window.KeyAvailable(out _, out _)) {
					Raylib.SetMasterVolume(oldMaster);
					Interrupting = false;
					return;
				}
			}

			OS.Wait(hasRenderedOverlay ? 0.2 : 1 / 60f);
		}
	}
}