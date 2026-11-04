namespace CloneDash.Common.Game;

public class LayerRemapScrollModifier(IReadOnlyDictionary<int, TimeRemap> remaps) : ScrollModifier
{
	public override TimeRemap? GetRemap(IScrollable note) => remaps.GetValueOrDefault(note.Layer);
}
