using CloneDash.Game;
using Nucleus;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using static AssetStudio.BundleFile;

namespace CloneDash.Levels;

public class CD_SceneEdit() : CD_GameLevel(null)
{
	public static ConCommand clonedash_sceneedit = ConCommand.Register(nameof(clonedash_sceneedit), (_, _) => {
		Interlude.Begin("Loading the Scene Editor...");
		Interlude.Spin();
		EngineCore.LoadLevel(new CD_SceneEdit());
	}, "Opens the scene editor");

	Menubar menubar;

	public override void Initialize(params object[] args) {
		base.Initialize(args);

		var bgrEditWindow = UI.Add<Window>();
		bgrEditWindow.Title = "Lua - Background Renderer";
		bgrEditWindow.Size = new(1200, 820);
		bgrEditWindow.Center();

		bgrEditWindow.Add(out Button bgrEditRecompile);
		bgrEditRecompile.Text = "Recompile";
		bgrEditRecompile.Dock = Dock.Bottom;
		bgrEditRecompile.Size = new(48);
		bgrEditRecompile.DockMargin = RectangleF.TLRB(4);

		bgrEditWindow.Add(out TextEditor bgrEdit);
		bgrEdit.Dock = Dock.Fill;
		bgrEdit.Highlighter = new LuaSyntaxHighlighter();
		bgrEdit.SetText(Filesystem.ReadAllText("scene", "scripts/background.lua"));
		bgrEdit.TextSize = 14;
		bgrEdit.DockMargin = RectangleF.TLRB(4);

		bgrEditRecompile.MouseReleaseEvent += (_, _, _) => {
			Lua.DoString(bgrEdit.GetText());
			SetupLua(false);
		};

		menubar = UI.Add<Menubar>();
		menubar.Dock = Dock.Top;
		menubar.BackgroundColor = new(10, 15, 20, 155);
		menubar.MoveToBack();

		var events = menubar.AddButton("Game Events");
		events.AddButton("Enter Fever", null, () => AddFever(MaxFever));

		var options = menubar.AddButton("Scene");
		options.AddButton("Refresh Scene", null, () => ConCommand.Execute(clonedash_sceneedit));
		options.AddButton("PlayScale = .6", null, () => PlayScale = .6f);
		options.AddButton("PlayScale = 1.2", null,  () => PlayScale = 1.2f);

		ConsoleSystem.AddScreenBlocker(this.UI);
	}

	public override void PreRender(FrameState frameState) {
		base.PreRender(frameState);
	}

	public override void PostRender(FrameState frameState) {
		base.PostRender(frameState);
		ConsoleSystem.TextSize = 11;
		ConsoleSystem.RenderToScreen(4 + 6, (int)(menubar.RenderBounds.H + 10));
	}
}
