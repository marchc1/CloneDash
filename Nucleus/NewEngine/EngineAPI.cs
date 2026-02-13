using Microsoft.Extensions.DependencyInjection;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Common.Engine;
using Nucleus.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Nucleus.NewEngine;

public class EngineAPI(IServiceProvider services) : IEngineAPI, IDisposable
{
	public StartupInfo StartupInfo;
	internal List<MemberInfo>? filledDependencies = null;
	public object? GetService(Type serviceType) => services.GetService(serviceType);

	public void Dispose() {
		throw new NotImplementedException();
	}

	public IEngineAPI.Result Run() {
		// TODO: Get rid of EngineCore eventually, this is just bootstrapping from here for the sake of testing in slices...
		EngineCore.Initialize(1600, 900, in StartupInfo, gameThreadInit: BootstrapGameThreadTemp);
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
}