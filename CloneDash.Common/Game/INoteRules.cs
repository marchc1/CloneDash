namespace CloneDash.Common.Game;

public interface INoteRules<in T> where T : RhythmNote
{
	bool CanAffectPlayer(T note);
	void Damage(T note);
	void Kill(T note);
	void Punish(T note);
	void Reward(T note);
}
