namespace CloneDash.Game;

public static class CD_Extensions
{
	public static bool IsBoss(this EntityVariant variant) => variant switch { 
		EntityVariant.Boss1 or EntityVariant.Boss2 or EntityVariant.Boss3 
		or EntityVariant.BossHitFast or EntityVariant.BossHitSlow
		=> true, _ => false };
}
