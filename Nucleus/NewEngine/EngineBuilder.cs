using Microsoft.Extensions.DependencyInjection;
using Nucleus.Commands;
using Nucleus.Common;
using Nucleus.Common.Commands;
using Nucleus.Common.Engine;
using Nucleus.Common.FileSystem;
using Nucleus.Files;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Nucleus.NewEngine;

public class EngineBuilder(ICommandLine cmdLine) : ServiceCollection
{
	public EngineBuilder MarkInterface<I, T>() where T : class, I where I : class {
		this.AddSingleton<I>(x => x.GetRequiredService<T>());
		return this;
	}

	/// <summary>
	/// Force loads an assembly.
	/// </summary>
	/// <param name="assemblyName"></param>
	/// <returns></returns>
	public EngineBuilder WithAssembly(string assemblyName) {
		if (!assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
			assemblyName += ".dll";

		if (!Path.IsPathFullyQualified(assemblyName))
			assemblyName = Path.Combine(AppContext.BaseDirectory, assemblyName);

		Assembly.LoadFrom(assemblyName);

		return this;
	}
	public EngineBuilder WithComponent<I, T>() where T : class, I where I : class {
		PreInject<T>(this);
		this.AddSingleton<I, T>();
		return this;
	}

	public EngineBuilder WithResolvedComponent<I, T>(Func<IServiceProvider, T> resolver) where T : class, I where I : class {
		this.AddSingleton<I, T>(resolver);
		return this;
	}

	public EngineBuilder WithComponent<T>() where T : class {
		PreInject<T>(this);
		this.AddSingleton<T>();
		return this;
	}

	HashSet<Type> injectedTypelist = [];
	void PreInject<T>(IServiceCollection services) {
		if (injectedTypelist.Add(typeof(T))) {
			Type t = typeof(T);
			var preInject = t.GetMethod("DLLInit", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<PreInject>();
			if (preInject != null)
				preInject(services);
		}
	}

	readonly List<MemberInfo> filledDependencies = [];

	/// <summary>
	/// Nulls out all automatic references the EngineBuilder previously created for the EngineAPI.
	/// </summary>
	/// <param name="members"></param>
	public static void InvalidateEngineDeps(List<MemberInfo>? members) {
		if (members == null) return;
		foreach (var member in members) {
			switch (member) {
				case FieldInfo field: field.SetValue(null, null); break;
				case PropertyInfo prop: prop.SetValue(null, null); break;
			}
		}
	}

	/// <summary>
	/// Finalizes the dependency injection setup and returns a finalized <see cref="IServiceProvider"/> (as an <see cref="EngineAPI"/>).
	/// </summary>
	/// <param name="dedicated"></param>
	/// <returns></returns>
	public EngineAPI Build() {
		this.AddSingleton(cmdLine);
		this.AddSingleton<GlobalVariablesBase>();
		this.AddSingleton<ICvar, Cvar>();
		this.AddSingleton<IEngineAPI, EngineAPI>();
		this.AddSingleton<IEngine, GameEngine>();

		List<Type> wantsInjection = [];
		object?[]? linkInput = [this];
		List<MemberInfo> populateLater = [];
		List<MemberInfo> populateLaterKeyed = [];
		void populateLookups(Type? type) {
			if (type == null)
				return;
			foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)) {
				if (field.GetCustomAttribute<DependencyAttribute>() != null)
					populateLater.Add(field);
				if (field.GetCustomAttribute<KeyedDependencyAttribute>() != null)
					populateLater.Add(field);
			}
			foreach (var property in type.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)) {
				if (property.GetCustomAttribute<DependencyAttribute>() != null)
					populateLater.Add(property);
				if (property.GetCustomAttribute<KeyedDependencyAttribute>() != null)
					populateLater.Add(property);
			}
		}
		foreach (var assembly in ReflectionUtils.GetAssemblies()) {
			// This allows a type to define a class named NucleusDllMain, with a static void Link(IServiceCollection),
			// which allows a loaded assembly to insert whatever it wants into the DI system before the provider is
			// fully built.
			Type? sourceDLL = assembly.GetTypes().FirstOrDefault(x => x.Name == "NucleusDllMain");
			sourceDLL
				?.GetMethod("Link", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				?.Invoke(null, linkInput);
			populateLookups(sourceDLL);

			// This checks for any classes with the MarkForDependencyInjection attribute.
			// They are then injected into the service collection.
			foreach (var typeKVP in assembly.GetTypesWithAttribute<EngineComponentAttribute>()) {
				populateLookups(typeKVP.Key);
				if (typeKVP.Key.IsAbstract && typeKVP.Key.IsSealed) 
					continue; // Just wants to get dependencies. Do not add to the singleton list. Usually this is done for globals.

				this.AddSingleton(typeKVP.Key);
			}
		}

		// Everything else should be provided by the launcher!
		ServiceProvider provider = this.BuildServiceProvider();

		// Start using this provider for the engine
		using ServiceLocatorScope locatorScope = new(provider);

		EngineAPI api = (EngineAPI)provider.GetRequiredService<IEngineAPI>();
		api.filledDependencies = filledDependencies;

		object? getService(Type service, DependencyAttribute depAttr) {
			if (depAttr is KeyedDependencyAttribute keyed)
				return depAttr.Required ? provider.GetRequiredKeyedService(service, keyed.Key) : provider.GetKeyedService(service, keyed.Key);
			else
				return depAttr.Required ? provider.GetRequiredService(service) : provider.GetService(service);
		}

		void handleSet(MemberInfo member, DependencyAttribute? depAttr) {
			if (depAttr == null)
				return;

			switch (member) {
				case FieldInfo field:
					field.SetValue(null, getService(depAttr.GetUnderlyingType() ?? field.FieldType, depAttr));
					break;
				case PropertyInfo prop:
					prop.SetValue(null, getService(depAttr.GetUnderlyingType() ?? prop.PropertyType, depAttr));
					break;
				default: // don't add a dependency for junk
					return;
			}

			filledDependencies.Add(member);
		}

		foreach (var member in populateLater) {
			handleSet(member, member.GetCustomAttribute<DependencyAttribute>());
			handleSet(member, member.GetCustomAttribute<KeyedDependencyAttribute>());
		}

		return api;
	}
}