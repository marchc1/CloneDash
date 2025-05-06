using CloneDash.Game.Entities;
using System.Xml;

namespace CloneDash.Interfaces;

public interface ISustainManager
{
	public void StartSustainBeam(SustainBeam sustain);
	public void FailSustainBeam(SustainBeam sustain);
	public void CompleteSustainBeam(SustainBeam sustain);
	public PathwaySide GetSustainState();
	public bool IsSustaining() => GetSustainState() != PathwaySide.None;
	public bool IsSustaining(PathwaySide pathway) {
		var pathwayNow = GetSustainState();
		return pathwayNow == pathway || pathwayNow == PathwaySide.Both;
	}

	public IEnumerable<SustainBeam> GetSustainsActive(PathwaySide pathway);
	public int GetSustainsActiveCount(PathwaySide pathway);

	public void ThinkSustainBeam(SustainBeam sustain);

	public int ActiveSustains(PathwaySide pathway);
	public int ActiveSustains() => ActiveSustains(PathwaySide.Top) + ActiveSustains(PathwaySide.Bottom);
}
