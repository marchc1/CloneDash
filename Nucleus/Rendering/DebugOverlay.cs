using Nucleus.Core;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Raylib_cs;
using System.Collections.Concurrent;

namespace Nucleus.Rendering;

public interface IDebugOverlayItem {
	public void Render();
}

public record DebugOverlayLine(Vector2F from, Vector2F to, float size, Color color) : IDebugOverlayItem
{
	public void Render() {
		Graphics2D.SetDrawColor(color);
		Graphics2D.DrawLine(from, to, size);
	}
}

public record DebugOverlayCircle(Vector2F pos, float size, Color color, bool lines) : IDebugOverlayItem
{
	public void Render() {
		Graphics2D.SetDrawColor(color);
		if (lines)
			Graphics2D.DrawCircle(pos, size);
		else
			Graphics2D.DrawCircleLines(pos, new(size));
	}
}

public record DebugOverlayText(string text, Vector2F position, float size, Color color, Anchor anchor) : IDebugOverlayItem
{
	public void Render() {
		Graphics2D.SetDrawColor(color);
		Graphics2D.DrawText(position, text, "Consolas", size, anchor);
	}
}

public record DebugOverlayTexture(Texture texture, Vector2F pos, Vector2F size, Color color, Anchor anchor, Vector2F? tl, Vector2F? tr, Vector2F? bl, Vector2F? br) : IDebugOverlayItem
{
	public void Render() {
		Graphics2D.SetDrawColor(color);
		Graphics2D.SetTexture(texture);
		Graphics2D.DrawTexture(anchor.CalculatePosition(pos, size, true), size, tl, tr, bl, br);
	}
}


/// <summary>
/// Thread-safe debug overlay library. Render calls truly happen right after <see cref="Nucleus.Engine.Level.PostRender(FrameState)"/>.
/// </summary>
[Nucleus.MarkForStaticConstruction]
public static class DebugOverlay
{
	static DebugOverlay() {
		ClearState();
	}
	public static ConVar debugoverlay = ConVar.Register("debugoverlay", "0", ConsoleFlags.Saved, "Enables the debugging overlay.");
	private static ConcurrentQueue<IDebugOverlayItem> items = [];
	public static bool Enabled => debugoverlay.GetBool();

	// State
	public static bool UseGraphics2DOffset { get; set; }

	public static void ClearState() {
		UseGraphics2DOffset = true;
	}

	private static Vector2F GetOffset() => UseGraphics2DOffset ? Graphics2D.Offset : Vector2F.Zero;

	public static void Circle(Vector2F pos, float radius, Color? color = null, bool lines = false)
		=> items.Enqueue(new DebugOverlayCircle(pos, radius, color ?? Color.White, lines));
	public static void Line(
						Vector2F from,
						Vector2F to,
						float width = 1, Color? color = null)
	=> items.Enqueue(new DebugOverlayLine(from + GetOffset(), to + GetOffset(), width, color ?? Color.White));

	public static void Text(
							string text, 
							Vector2F position, 
							float size = 16, Color? color = null, Anchor? anchor = null)
		=> items.Enqueue(new DebugOverlayText(text, position + GetOffset(), size, color ?? Color.White, anchor ?? Anchor.TopLeft));

	public static void Texture(
								Texture texture, 
								Vector2F position, 	Vector2F? size = null, 
								Vector2F? tl = null, Vector2F? tr = null, Vector2F? bl = null, Vector2F? br = null, 
								Color? color = null, Anchor? anchor = null)
		=> items.Enqueue(new DebugOverlayTexture(
			texture, position + GetOffset(), size ?? new(texture.Width, texture.Height), color ?? Color.White, anchor ?? Anchor.TopLeft, tl, tr, bl, br
		));

	/// <summary>
	/// Renders and flushes the overlay item queue.
	/// </summary>
	internal static void Render() {
		if (items.Count <= 0) return;

		if (!debugoverlay.GetBool()) {
			items.Clear();
			return;
		}

		while (items.TryDequeue(out IDebugOverlayItem? item)) {
			item.Render();
		}

		ClearState();
	}
}
