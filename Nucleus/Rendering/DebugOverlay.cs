using Nucleus.Core;
using Nucleus.Types;
using Raylib_cs;
using System.Collections.Concurrent;

namespace Nucleus.Rendering;

public interface IDebugOverlayItem {
	public void Render();
}

public record DebugOverlayText(string text, Vector2F position, float size, Color color) : IDebugOverlayItem {
	public void Render() {
		Graphics2D.SetDrawColor()
	}
}

/// <summary>
/// Thread-safe debug overlay library. Render calls truly happen right after <see cref="Nucleus.Engine.Level.PostRender(FrameState)"/>.
/// </summary>
public static class DebugOverlay
{
	private static ConcurrentQueue<IDebugOverlayItem> items = [];

	public static void Text(string text, Vector2F position, float size = 16, Color? color = null)
		=> items.Enqueue(new DebugOverlayText(text, position, size, color ?? Color.White));

	/// <summary>
	/// Renders and flushes the overlay item queue.
	/// </summary>
	public static void Render() {
		if (items.Count <= 0) return;

		while (items.TryDequeue(out IDebugOverlayItem? item)) {
			item.Render();
		}
	}
}
