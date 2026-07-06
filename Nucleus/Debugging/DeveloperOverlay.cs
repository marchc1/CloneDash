using Nucleus.Commands;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ManagedMemory;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.UI.Elements.Visual;
using Nucleus.Util;
using System.Diagnostics;

namespace Nucleus.Debugging;

[MarkForStaticConstruction]
public class DeveloperOverlay(Level level) : ICanDraw
{
	private const float LineHeight = 12;
	private readonly string _header = $"Nucleus Level / {engineAPI.GetStartupInfo().AppName} - DebugContext";

	private static GCMemoryInfo GcMemoryInfo => GC.GetGCMemoryInfo();

	private DebugRecordState _cachedState;

	// Update only every 250ms because this whole class is a LOT of string allocations
	public static readonly ConVar developer_overlay_update_interval = new ConVar(nameof(developer_overlay_update_interval), "250", FCvar.Saved, "Minimum milliseconds between developer overlay string updates");
	private static double _throttledUpdater = 0;

	public Level Level { get; } = level;
	public readonly DebugRecordList DebugRecords = new DebugRecordList();
	public readonly DebugRecordList UserDefinedDebugRecords = new DebugRecordList();

	public PerformanceGraph UpdateGraph = null!, RenderGraph = null!, MemGraph = null!;

	private void Update() {
		if (IValidatable.IsValid(InGameConsole.Instance)) return;
		if (!ThrottledUpdater.TryUpdate(Level.RendertimeDelta, ref _throttledUpdater, developer_overlay_update_interval.GetDouble() / 1000d)) return;

		DebugRecords.Reset();

		DebugRecords.Write(_header);
		DebugRecords.Write();

		DebugRecords.Write("Engine / Sound");
		DebugRecords.EnterScope();
		{
			DebugRecords.Write("Instances", audiosystem.GetAudioClipCount());
			DebugRecords.Write("Channels", audiosystem.GetActiveChannelCount());
			DebugRecords.Write("Allocated", audiosystem.GetMemoryAllocated().NiceBytes());
		}
		DebugRecords.ExitScope();

		DebugRecords.Write("Engine / Graphics");
		DebugRecords.EnterScope();
		{
			DebugRecords.Write("Window Size", Level.FrameState.WindowSize);
			DebugRecords.Write("Textures", Level.Textures.Count);
			DebugRecords.Write("Texture Memory (CPU)", (Level.Textures.UsedBits_CPU >> 3).NiceBytes());
			DebugRecords.Write("Texture Memory (GPU)", (Level.Textures.UsedBits >> 3).NiceBytes());
			DebugRecords.Write("Font Memory (GPU)", Graphics2D.FontManager.GetUsedGPUBits().NiceBytes());
		}
		DebugRecords.ExitScope();

		DebugRecords.Write("Garbage Collection");
		DebugRecords.EnterScope();
		{
			DebugRecords.Write("Collections Gen1", GC.CollectionCount(0));
			DebugRecords.Write("Collections Gen2", GC.CollectionCount(1));
			DebugRecords.Write("Collections Gen3", GC.CollectionCount(2));
			DebugRecords.Write("Size Gen1", GcMemoryInfo.GenerationInfo[0].SizeAfterBytes);
			DebugRecords.Write("Size Gen2", GcMemoryInfo.GenerationInfo[1].SizeAfterBytes);
			DebugRecords.Write("Size Gen3", GcMemoryInfo.GenerationInfo[2].SizeAfterBytes);
			DebugRecords.Write("Finalization Queue", GcMemoryInfo.FinalizationPendingCount);
		}
		DebugRecords.ExitScope();

		DebugRecords.Write($"Level ({Level.GetType().Name})");
		DebugRecords.EnterScope();
		{
			DebugRecords.Write("Entities", Level.Entities.Count);
			DebugRecords.Write("UI Elements", Level.RootPanel.GetAllElements().Length);
			DebugRecords.Write("UI State:", $"hovered {Level.RootPanel.GetHoveredElement()?.ToString() ?? "<null>"}, depressed {Level.RootPanel.GetDepressedElement()?.ToString() ?? "<null>"}, focused {Level.RootPanel.GetKeyboardFocusedElement()?.ToString() ?? "<null>"}, kb-focused {Level.RootPanel.GetKeyboardFocusedElement()?.ToString() ?? "<null>"}");
		}
		DebugRecords.ExitScope();


		// Only prints the class name right now anyway, so I commented it out

		// DebugRecords.Write("Input");
		// DebugRecords.EnterScope();
		// {
		// 	DebugRecords.Write("Mouse State", $"{Level.FrameState.Mouse}");
		// 	DebugRecords.Write("Keyboard State", $"{Level.FrameState.Keyboard}");
		// }
		// DebugRecords.ExitScope();
	}

	internal void SetUpDebugOverlays() {
		UpdateGraph = new PerformanceGraph(Level.RootPanel);
		UpdateGraph.SetAnchor(Anchor.BottomRight);
		UpdateGraph.SetOrigin(Anchor.BottomRight);
		UpdateGraph.SetPos(new Vector2F(-8, -8 + -52 + -16));
		UpdateGraph.SetSize(new Vector2F(400, 26));
		UpdateGraph.Mode = (PerformanceGraph.GraphMode.CpuUpdateTime);

		RenderGraph = new PerformanceGraph(Level.RootPanel);
		RenderGraph.SetAnchor(Anchor.BottomRight);
		RenderGraph.SetOrigin(Anchor.BottomRight);
		RenderGraph.SetPos(new Vector2F(-8, -8 + -26 + -8));
		RenderGraph.SetSize(new Vector2F(400, 26));
		RenderGraph.Mode = (PerformanceGraph.GraphMode.CpuRenderTime);

		MemGraph = new PerformanceGraph(Level.RootPanel);
		MemGraph.SetAnchor(Anchor.BottomRight);
		MemGraph.SetOrigin(Anchor.BottomRight);
		MemGraph.SetPos(new Vector2F(-8, -8));
		MemGraph.SetSize(new Vector2F(400, 26));
		MemGraph.Mode = (PerformanceGraph.GraphMode.RamUsage);

		EvaluatePerfGraphVisibility();
	}

	public void EvaluatePerfGraphVisibility() {
		Debug.Assert(UpdateGraph != null);
		Debug.Assert(RenderGraph != null);
		Debug.Assert(MemGraph != null);

		bool vis = EngineCore.ShouldShowDeveloperOverlays();
		UpdateGraph.SetVisible(vis);
		RenderGraph.SetVisible(vis);
		MemGraph.SetVisible(vis);
	}

	public void Draw(FrameState frameState) {
		if (!EngineCore.ShouldShowDeveloperOverlays()) return;
		Update();

		_cachedState = DebugRecordState.Max(DebugRecords.CompileState(), UserDefinedDebugRecords.CompileState());

		Graphics2D.ResetDrawingOffset();

		int totalFields = UserDefinedDebugRecords.NumRecords + DebugRecords.NumRecords;
		float textY = (Level.FrameState.WindowHeight - 8) - (totalFields * LineHeight);

		DebugRecords.DrawRecords(in _cachedState, LineHeight, ref textY);
		UserDefinedDebugRecords.DrawRecords(in _cachedState, LineHeight, ref textY);

		ConsoleSystem.Draw(Level.GetConsoleOverlaySettings());
	}
}