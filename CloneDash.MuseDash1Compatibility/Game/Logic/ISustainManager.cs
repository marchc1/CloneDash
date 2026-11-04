using CloneDash.Common.Game;
using CloneDash.Common.Gamemodes.MuseDash;
using CloneDash.Game.Entities;

namespace CloneDash.Game.Logic;

public interface ISustainManager : IRhythmGameComponent
{
	public void StartSustainBeam(SustainBeam sustain);
	public void FailSustainBeam(SustainBeam sustain);
	public void CompleteSustainBeam(SustainBeam sustain);
	public PathwaySet GetSustainState();
	public bool IsSustaining() => !GetSustainState().IsEmpty;
	public bool IsSustaining(PathwayId pathway) => GetSustainState().Contains(pathway);
	public bool IsSustaining(PathwaySet pathways) => !pathways.IsEmpty && (GetSustainState() & pathways) == pathways;

	public int GetSustainsActiveCount(PathwayId pathway);

	public void ThinkSustainBeam(SustainBeam sustain);

	public int ActiveSustains();
}
