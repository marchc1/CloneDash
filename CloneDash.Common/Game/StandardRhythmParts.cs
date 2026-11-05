using CloneDash.Game;
using Microsoft.Extensions.DependencyInjection;

namespace CloneDash.Common.Game;

public static class StandardRhythmParts
{
	public static IServiceCollection AddStandardParts(this IServiceCollection parts, RhythmGameLevel level)
		=> parts
			.AddPart<ResultComponent>()
			.AddPart<InputComponent>(input => input.Initialize())
			.AddPart<PauseComponent>(pause => level.Component<Conductor>().Pause = pause);
}
