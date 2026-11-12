using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

using System.Security.Cryptography;
using System.Text;

namespace Nucleus.ModelEditor;

/// <summary>
/// Autosaves to modelsrc/autosave and offers it back after a crash
/// </summary>
public class Autosave(ModelEditor editor)
{
	/// <summary>
	/// Idle time after an edit before autosaving
	/// </summary>
	public const double IDLE_SECONDS = 2;
	/// <summary>
	/// Minimum time between autosaves
	/// </summary>
	public const double MIN_INTERVAL_SECONDS = 30;

	public static string Folder => Path.Combine(AppContext.BaseDirectory, "modelsrc", "autosave");
	private const string SOURCE_EXT = ".source";

	int seenVersion = -1;
	int writtenVersion = -1;
	double lastChange;
	double lastWrite = double.MinValue;
	Task? writing;

	public static string PathFor(string? diskPath) {
		if (diskPath == null) return Path.Combine(Folder, "untitled." + ModelEditor.NUCLEUS_MODEL4_SOURCE_EXT);
		var fullPath = Path.GetFullPath(diskPath).ToLowerInvariant();
		var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(fullPath))).Substring(0, 8);
		return Path.Combine(Folder,
			$"{Path.GetFileNameWithoutExtension(diskPath)}_{hash}.{ModelEditor.NUCLEUS_MODEL4_SOURCE_EXT}");
	}

	public void Think(double curtime, bool mouseHeld) {
		int version = editor.Actions.Version;
		if (version != seenVersion) {
			seenVersion = version;
			lastChange = curtime;
		}

		if (!editor.HasUnsavedChanges || version == writtenVersion || mouseHeld) return;
		if (curtime - lastChange < IDLE_SECONDS || curtime - lastWrite < MIN_INTERVAL_SECONDS) return;
		if (writing != null && !writing.IsCompleted) return;

		Write();
		lastWrite = curtime;
	}

	public void Write() {
		var path = PathFor(editor.File.DiskPath);
		var source = editor.File.DiskPath ?? "";
		var json = editor.File.Serialize();
		writtenVersion = editor.Actions.Version;

		writing = Task.Run(() => {
			try {
				Directory.CreateDirectory(Folder);
				var temp = path + ".tmp";
				System.IO.File.WriteAllText(temp, json);
				System.IO.File.Move(temp, path, true);
				System.IO.File.WriteAllText(path + SOURCE_EXT, source);
			}
			catch (Exception ex) {
				Logs.Warn($"Autosave failed: {ex.Message}");
			}
		});
	}

	public void Delete(string? diskPath) {
		writing?.Wait();
		writtenVersion = -1;
		var path = PathFor(diskPath);
		try {
			if (System.IO.File.Exists(path))
				System.IO.File.Delete(path);
			if (System.IO.File.Exists(path + SOURCE_EXT))
				System.IO.File.Delete(path + SOURCE_EXT);
		}
		catch (Exception ex) {
			Logs.Warn($"Couldn't remove autosave: {ex.Message}");
		}
	}

	public record Leftover(string AutosavePath, string? SourcePath, DateTime Time);

	public static List<Leftover> FindLeftovers() {
		if (!Directory.Exists(Folder)) return [];
		List<Leftover> found = [];
		foreach (var path in Directory.GetFiles(Folder, "*." + ModelEditor.NUCLEUS_MODEL4_SOURCE_EXT)) {
			string? source = null;
			if (System.IO.File.Exists(path + SOURCE_EXT)) {
				source = System.IO.File.ReadAllText(path + SOURCE_EXT).Trim();
				if (source.Length == 0)
					source = null;
			}
			found.Add(new(path, source, System.IO.File.GetLastWriteTime(path)));
		}
		var order = new List<int>(found.Count);
		for (int i = 0; i < found.Count; i++)
			order.Add(i);
		order.Sort((first, second) => {
			int compare = found[second].Time.CompareTo(found[first].Time);
			return compare != 0 ? compare : first.CompareTo(second);
		});
		var sorted = new List<Leftover>(found.Count);
		foreach (int index in order)
			sorted.Add(found[index]);
		return sorted;
	}

	public Window? OfferRecovery(Element root) {
		var leftovers = FindLeftovers();
		if (leftovers.Count == 0) return null;

		var window = new Window(root);
		window.Title = "Recover unsaved work";
		window.Size = new(640, 100 + 44 * leftovers.Count);
		window.Center();
		window.MakePopup();

		var info = new Label(window);
		info.Dock = Dock.Top;
		info.Size = new(48);
		info.TextSize = 20;
		info.Text = "The editor closed with unsaved changes. Recover them?";

		foreach (var leftover in leftovers) {
			var row = new Panel(window);
			row.Dock = Dock.Top;
			row.Size = new(40);
			row.DockMargin = RectangleF.TLRB(4, 8, 8, 0);
			row.DockPadding = RectangleF.TLRB(4);

			var discard = new Button(row, "Discard");
			discard.Dock = Dock.Right;
			discard.Size = new(110);
			discard.DockMargin = RectangleF.TLRB(0, 4, 0, 0);
			discard.OnButtonClick += (_, _) => {
				DeleteFiles(leftover.AutosavePath);
				row.Remove();
				if (FindLeftovers().Count == 0)
					window.Remove();
			};

			var recover = new Button(row, "Recover");
			recover.Dock = Dock.Right;
			recover.Size = new(110);
			recover.DockMargin = RectangleF.TLRB(0, 4, 0, 0);
			recover.OnButtonClick += (_, _) => {
				editor.Recover(leftover);
				window.Remove();
			};

			var name = new Label(row);
			name.Dock = Dock.Fill;
			name.TextSize = 18;
			var title = leftover.SourcePath == null ? "Untitled" : Path.GetFileName(leftover.SourcePath);
			name.Text = $"{title}  ({leftover.Time:g})";
			name.TooltipText = leftover.SourcePath ?? "Never saved";
		}

		return window;
	}

	private static void DeleteFiles(string autosavePath) {
		try {
			System.IO.File.Delete(autosavePath);
			if (System.IO.File.Exists(autosavePath + SOURCE_EXT))
				System.IO.File.Delete(autosavePath + SOURCE_EXT);
		}
		catch (Exception ex) {
			Logs.Warn($"Couldn't remove autosave: {ex.Message}");
		}
	}
}
