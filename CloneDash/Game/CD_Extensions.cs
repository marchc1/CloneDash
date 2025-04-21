namespace CloneDash.Game;

public static class CD_Extensions
{
	public static bool IsBoss(this EntityVariant variant) => variant switch { EntityVariant.Boss1 or EntityVariant.Boss2 or EntityVariant.Boss3 => true, _ => false };
}
