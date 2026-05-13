using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Common.Engine;

/// <summary>
/// Functions the client exposes to the engine.
/// </summary>
public interface IGameDLL
{
	void Init();
	public void PreStaticInitialize() { }
}
