using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Common.Gamemodes.MuseDash.V1;
using CloneDash.MD1_Compat.Game.Events;
using CloneDash.Settings;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Models;
using Nucleus.Models.Runtime;
using Nucleus.Types;
using Raylib_cs;

namespace CloneDash.Game;

public class MuseDash1PlayfieldComponent(MuseDash1BattleFXComponent battleFX, MuseDash1ChartComponent chart, ScreenEffectsComponent effects, MuseDash1HudComponent hud, RhythmGameLevel level, PathwaysComponent pathways) : IRhythmGameComponent, IRhythmGameBackgroundPreRenderer, IRhythmGameRenderer
{
	readonly MuseDash1BattleFXComponent BattleFX = battleFX;
	readonly MuseDash1ChartComponent Chart = chart;
	readonly ScreenEffectsComponent Effects = effects;

	public Pathway[] Pathways { get; private set; } = [];

	PathwayLayout Layout => pathways.Layout;

	public void ConditionallyRenderVisibleEntities(FrameState frameState, Predicate<DashEnemy> enemyPredicate, Span<DashEnemy> visibleEnemies) {
		DeadEntityVisibility deadVis = GameSettings.DeadEntityVisibility;
		foreach (DashEnemy ent in visibleEnemies) {
			if (!enemyPredicate(ent)) continue;
			RenderVisibleEntity(frameState, ent, deadVis);
		}
	}

	public void ConditionallyRenderVisibleEntities(FrameState frameState, PathwaySet exactPathways, Span<DashEnemy> visibleEnemies) {
		DeadEntityVisibility deadVis = GameSettings.DeadEntityVisibility;
		foreach (DashEnemy ent in visibleEnemies) {
			if (ent.Type == MuseDash1EntityType.SustainBeam || ent.Pathway != exactPathways) continue;
			RenderVisibleEntity(frameState, ent, deadVis);
		}
	}

	public Pathway GetPathway(PathwayId pathway) => Pathways[pathway.Index];

	public Pathway GetPathway(DashEnemy ent) => GetPathway(pathways.PrimaryPathway(ent.Pathway));

	public void Load() {
		Pathways = new Pathway[Layout.Count];
		for (int i = 0; i < Layout.Count; i++)
			Pathways[i] = level.Add(ActivatorUtilities.CreateInstance<Pathway>(level, Layout.PathwayAt(i)));
	}

	public void PreRenderBackground(FrameState frameState) {
		Chart.Boss.Position = new(0, 2.25f);
	}

	public void Render(FrameState frameState) {
		Rlgl.DrawRenderBatchActive();

		Rlgl.DisableDepthTest();
		Rlgl.DisableDepthMask();
		Rlgl.DisableBackfaceCulling();

		//Raylib.DrawLineV(new(-100000, 0), new(100000, 0), Color.Red);
		//Raylib.DrawLineV(new(0, -100000), new(0, 100000), Color.Green);

		hud.UI?.PreRenderWorldspace();

		// Pathways
		foreach (var pathway in Pathways)
			pathway.Render();

		bool notesVisible = Effects.Get<NoteVisibilityScreenEffect>().Visible;
		Span<DashEnemy> visibleEnemies = Chart.EnemyManager.GetLastVisible();

		// Hold notes
		if (notesVisible)
			ConditionallyRenderVisibleEntities(frameState, static x => x.Type == MuseDash1EntityType.SustainBeam, visibleEnemies);

		BattleFX.RenderBelowEntities();

		if (notesVisible) {
			// Boss
			Chart.Boss.Render();

			// The other entities, that aren't sustain beams, in order of top -> bottom pathway
			for (int i = 0; i < Layout.Count; i++)
				ConditionallyRenderVisibleEntities(frameState, Layout.PathwayAt(i).AsSet, visibleEnemies);
		}

		// The fail screen's fade-in covers the stage and notes; character entities render after this, so they stay on top of it
		hud.UI?.RenderFailureUnderlay();

		level.AddDebugString("Visible Entities", Chart.EnemyManager.GetLastVisible().Length);
		level.AddDebugString("Active Effects", Effects.GetActiveEffects());

		Rlgl.DrawRenderBatchActive();
	}

	public void Reset() { }

	public bool ShouldFreezeNoteAnimations() => Effects.Get<NoteFreezeScreenEffect>().Value >= 1;

	private void RenderVisibleEntity(FrameState frameState, DashEnemy ent, DeadEntityVisibility deadVis) {
		if (ent.Dead) {
			switch (deadVis) {
				case DeadEntityVisibility.UseGamemodeDefaults:
				case DeadEntityVisibility.FullyVisible:
					Model4System.PushRenderBlend(new(255, 255, 255));
					break;
				case DeadEntityVisibility.Dimmed:
					Model4System.PushRenderBlend(new(70, 70, 70));
					break;
				case DeadEntityVisibility.Invisible:
					return;
			}
		}

		ent.Render(frameState);
		Rlgl.DrawRenderBatchActive();
		Model4System.PopRenderBlend();
	}
}