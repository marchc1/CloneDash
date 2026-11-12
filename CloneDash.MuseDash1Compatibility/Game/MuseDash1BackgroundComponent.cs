using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash.V1.Data;
using CloneDash.MD1_Compat.Game.Events;
using CloneDash.Scenes;
using Nucleus;
using Nucleus.Common.Graphics;
using Nucleus.Core;
using Nucleus.Types;

namespace CloneDash.Game;

public class MuseDash1BackgroundComponent(MD1_GamemodeData data, ScreenEffectsComponent effects, MuseDash1ScenesComponent scenes) : IRhythmGamePreRenderer, IDisposable
{
	readonly ITexture? backgroundOverride = data.BackgroundTextureOverride;
	readonly float backgroundOverrideOpacity = data.BackgroundTextureOpacity;
	readonly ScreenEffectsComponent Effects = effects;

	public void Dispose() {
		if (backgroundOverride != null) {
			backgroundOverride.Dispose();
		}
	}

	public void PreRender(FrameState frameState) {
		float width = EngineCore.GetWindowWidth(), height = EngineCore.GetWindowHeight();
		//Stopwatch test = Stopwatch.StartNew();
		if (Effects.Get<BackgroundVisibilityScreenEffect>().Visible) {
			if (backgroundOverride != null) {
				backgroundOverride.Download(); // call regenerator?
				Graphics2D.SetTexture(backgroundOverride);
				Graphics2D.SetDrawColor(255, 255, 255, (int)(255 * backgroundOverrideOpacity));
				float texWidth = width * MuseDash1Gamemode.GlobalScale * 1.5f;
				float texHeight = height * MuseDash1Gamemode.GlobalScale * 1.5f;
				Graphics2D.DrawTexturedRectangle(texWidth / -2, texHeight / -2, texWidth, texHeight);
			}
			else if (scenes.HasActiveScene(out var scene))
				scene.RenderBackground();
		}
		//Logs.Info(test.Elapsed.TotalMilliseconds);
	}
}