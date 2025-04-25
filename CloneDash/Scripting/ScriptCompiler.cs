using CSScriptLib;
using Nucleus;
namespace CloneDash.Scripting;

public static class ScriptAPI {
	static IEvaluator eval;
	static ScriptAPI() {
		eval = SetupEvaluator();
	}

	public static IEvaluator SetupEvaluator() {
		if (eval != null) return eval;

		var ev = CSScript.Evaluator
				 .ReferenceAssemblyOf<Nucleus.ConsoleFlags>();

		ev.Eval("1 + 2");

		eval = ev;

		return ev;
	}

	public static C CompileClassInstance<C>(string code, params object[] args) where C : class {
		C script = SetupEvaluator().LoadCode<C>(code, args);
		return script;
	}

	public static I CompileInterface<I>(string code) where I : class {
		I script = SetupEvaluator().LoadMethod<I>(code);
		return script;
	}
}