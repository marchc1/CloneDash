namespace CloneDash.Game.Events;

public class BossVisibilityEvent(MuseDash1Game game, bool visible) : DashEvent(game)
{
	public readonly bool Visible = visible;
	public override void Activate() {
		if (Visible)
			Game.Boss.Show();
		else
			Game.Boss.Hide();
	}
}
