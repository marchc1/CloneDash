using CloneDash.Common.Game;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;

namespace CloneDash.Compatibility.UnbeatableWhiteLabel;

public class UnbeatableWhiteLabelHudComponent(ComboComponent combo, HealthComponent health, UnbeatableWhiteLabelJudgeComponent judge, RhythmGameLevel level, ResultComponent results, UnbeatableWhiteLabelNoteRules rules, UnbeatableWhiteLabelScoreComponent score) : IRhythmGamePostRenderer
{
	const string FONT = "Noto Sans";
	const string FONT_BOLD = "Noto Sans Bold";
	const double JUDGEMENT_DISPLAY_TIME = 0.6;

	static readonly Color BackColor = new(0, 0, 0, 170);
	static readonly Color HealthColor = new(255, 73, 114, 255);
	static readonly Color TextColor = new(245, 245, 245, 255);

	public void PostRender(FrameState frameState) {
		float scale = frameState.WindowHeight / 900;

		if (results.Cleared || results.Failed) {
			RenderResults(frameState, scale);
			return;
		}

		RenderScore(scale);
		RenderCombo(frameState, scale);
		RenderHealth(frameState, scale);
		RenderJudgement(frameState, scale);
	}

	static string GetJudgementText(UnbeatableWhiteLabelJudgement judgement) {
		string name = UnbeatableWhiteLabelJudgement.TierNames[judgement.Tier];
		if (!judgement.HasBonusScore)
			return name;

		int offset = (int)Math.Abs(judgement.Offset * 1000);
		return $"{name} {(judgement.Offset < 0 ? "-" : "+")} {offset:D3}";
	}

	void RenderCombo(FrameState frameState, float scale) {
		if (combo.Value <= 0)
			return;

		Graphics2D.SetDrawColor(TextColor);
		Graphics2D.DrawText(frameState.WindowWidth / 2, 40 * scale, $"{combo.Value}", FONT_BOLD, 56 * scale, Anchor.TopCenter);
		Graphics2D.DrawText(frameState.WindowWidth / 2, 100 * scale, "COMBO", FONT, 20 * scale, Anchor.TopCenter);
	}

	void RenderHealth(FrameState frameState, float scale) {
		float width = 420 * scale, height = 18 * scale;
		float x = (frameState.WindowWidth - width) / 2, y = frameState.WindowHeight - 50 * scale;
		float fraction = health.Max <= 0 ? 0 : health.Value / health.Max;

		Graphics2D.SetDrawColor(BackColor);
		Graphics2D.DrawRectangle(new Vector2F(x, y), new Vector2F(width, height));
		Graphics2D.SetDrawColor(HealthColor);
		Graphics2D.DrawRectangle(new Vector2F(x, y), new Vector2F(width * fraction, height));
		Graphics2D.SetDrawColor(TextColor);
		Graphics2D.DrawText(x + width / 2, y - 6 * scale, $"{(int)health.Value} / {(int)health.Max}", FONT, 18 * scale, Anchor.BottomCenter);
	}

	void RenderJudgement(FrameState frameState, float scale) {
		if (judge.LastJudgement is not UnbeatableWhiteLabelJudgement judgement)
			return;

		double age = level.Realtime - judge.LastJudgementTime;
		if (age >= JUDGEMENT_DISPLAY_TIME)
			return;

		int alpha = (int)(255 * (1 - age / JUDGEMENT_DISPLAY_TIME));
		Graphics2D.SetDrawColor(judgement.Miss ? HealthColor : TextColor, alpha);
		Graphics2D.DrawText(frameState.WindowWidth / 2, frameState.WindowHeight * 0.3f, GetJudgementText(judgement), FONT_BOLD, 34 * scale, Anchor.Center);
	}

	void RenderResults(FrameState frameState, float scale) {
		float width = 520 * scale, height = 560 * scale;
		float x = (frameState.WindowWidth - width) / 2, y = (frameState.WindowHeight - height) / 2;

		Graphics2D.SetDrawColor(BackColor);
		Graphics2D.DrawRectangle(new Vector2F(0, 0), new Vector2F(frameState.WindowWidth, frameState.WindowHeight));
		Graphics2D.DrawRectangle(new Vector2F(x, y), new Vector2F(width, height));

		float centerX = x + width / 2;
		float lineY = y + 30 * scale;
		Graphics2D.SetDrawColor(results.Failed ? HealthColor : TextColor);
		Graphics2D.DrawText(centerX, lineY, results.Failed ? "FAILED" : "CLEARED", FONT_BOLD, 44 * scale, Anchor.TopCenter);

		lineY += 80 * scale;
		Graphics2D.SetDrawColor(TextColor);
		RenderResultLine(x, width, ref lineY, scale, "TOTAL SCORE", $"{(int)score.TotalScore}");
		RenderResultLine(x, width, ref lineY, scale, "BASE SCORE", $"{(int)score.BaseScore}");
		RenderResultLine(x, width, ref lineY, scale, "BONUS", $"{(int)score.BonusScore}");
		RenderResultLine(x, width, ref lineY, scale, "ACCURACY", $"{score.Accuracy * 100:F2}%");
		RenderResultLine(x, width, ref lineY, scale, "MAX COMBO", $"{rules.MaxCombo}");

		lineY += 16 * scale;
		for (int tier = UnbeatableWhiteLabelJudgement.TIER_COUNT; tier >= 0; tier--)
			RenderResultLine(x, width, ref lineY, scale, UnbeatableWhiteLabelJudgement.TierNames[tier], $"{score.GetTierCount(tier)}");
	}

	static void RenderResultLine(float x, float width, ref float lineY, float scale, string label, string value) {
		Graphics2D.DrawText(x + 40 * scale, lineY, label, FONT, 22 * scale, Anchor.TopLeft);
		Graphics2D.DrawText(x + width - 40 * scale, lineY, value, FONT_BOLD, 22 * scale, Anchor.TopRight);
		lineY += 32 * scale;
	}

	void RenderScore(float scale) {
		Graphics2D.SetDrawColor(TextColor);
		Graphics2D.DrawText(30 * scale, 24 * scale, $"{(int)score.TotalScore}", FONT_BOLD, 40 * scale, Anchor.TopLeft);
		Graphics2D.DrawText(30 * scale, 74 * scale, $"ACCURACY {score.Accuracy * 100:F2}%", FONT, 20 * scale, Anchor.TopLeft);
	}
}
