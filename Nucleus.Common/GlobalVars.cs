using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Common;

public static class s_GlobalVariablesBase{
	public static readonly GlobalVariablesBase globals = new(); // TODO: Ideally we pass this through dependency injection in the future.
}

public class GlobalVariablesBase {
	public double CurTime;
	public double CurTimeDelta;
}