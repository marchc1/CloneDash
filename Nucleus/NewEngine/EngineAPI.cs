using Nucleus.Common.Engine;
using Nucleus.Types;
using System.Reflection;

namespace Nucleus.NewEngine;

public class EngineAPI(IServiceProvider services) : IEngineAPI, IDisposable
{
	public StartupInfo StartupInfo;

	public WindowInitialState WindowInitialState = new() {
		Width = 1600,
		Height = 900
	};

	internal List<MemberInfo>? filledDependencies = null;

	public object? GetService(Type serviceType) => services.GetService(serviceType);

	public void Dispose() {
		throw new NotImplementedException();
	}

	public IEngineAPI.Result Run() {
		// TODO: Get rid of EngineCore eventually, this is just bootstrapping from here for the sake of testing in slices...
		EngineCore.Initialize(WindowInitialState.Width, WindowInitialState.Height, in StartupInfo, gameThreadInit: BootstrapGameThreadTemp, flags: WindowInitialState.Flags);
		EngineCore.StartMainThread();
		return IEngineAPI.Result.RunOK;
	}

	private void BootstrapGameThreadTemp() {
		gameDLL.Init();
	}

	public ref readonly StartupInfo GetStartupInfo() => ref StartupInfo;

	public void SetStartupInfo(in StartupInfo info) {
		StartupInfo = info; // copy off
	}

	public void SetWindowInitialState(in WindowInitialState info) {
		WindowInitialState = info;
	}
}