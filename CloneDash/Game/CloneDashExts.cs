namespace CloneDash.Game;

public static class CloneDashExts
{
	public static bool IsBoss(this EntityVariant variant) => variant switch {
		EntityVariant.Boss1 or EntityVariant.Boss2 or EntityVariant.Boss3
		or EntityVariant.BossHitFast or EntityVariant.BossHitSlow
		or EntityVariant.BossMash1 or EntityVariant.BossMash2
		=> true,
		_ => false
	};
}
