namespace CloneDash.Compatibility.MuseDash;

public class FailBarrageConfigData
{
	public int index;
	public bool silencer;
	public string[] barrages = [];

	public static string[] GetBarrages(int roleIndex) {
		var config = MuseDash1Compatibility.FailBarrages.Find(barrage => barrage.index == roleIndex);
		if (config == null || config.silencer)
			roleIndex = -1;

		var local = MuseDash1Compatibility.FailBarragesEN.Find(barrage => barrage.index == roleIndex) ?? MuseDash1Compatibility.FailBarragesEN.FirstOrDefault();
		return local?.barrages ?? [];
	}
}
