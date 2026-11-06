using CloneDash.Game;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Engine;

namespace CloneDash.Common.Game;

public static class StandardRhythmParts
{
	public static IServiceCollection AddStandardParts(this IServiceCollection parts, RhythmGameLevel level, KeyBindSet? binds = null)
		=> parts
			.AddPart<ResultComponent>()
			.AddPart<InputComponent>(input => {
				input.Binds = binds;
				input.Initialize();
			})
			.AddPart<PauseComponent>(pause => level.Component<Conductor>().Pause = pause);
}
