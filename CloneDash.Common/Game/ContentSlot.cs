using Microsoft.Extensions.DependencyInjection;
using Nucleus;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Common.Game;

public sealed class ContentSlot<T>(RhythmGameLevel level) where T : class
{
	readonly List<Func<IContentSource?>> sources = [];
	Func<T>? fallback;
	bool required;

	public event Action<T?, T?>? Changed;

	public T? Value { get; private set; }

	public string? Check() {
		IContentSource? rejected = null;
		foreach (var source in sources) {
			var content = source();
			if (content == null)
				continue;
			if (Plan(content, out _))
				return null;
			rejected ??= content;
		}

		if (fallback != null || !required)
			return null;
		if (rejected == null)
			return $"Nothing could fill the {typeof(T).Name} slot.";
		return $"{rejected.GetName()} is from {rejected.GetGamemode().GetName()}, and there's no adapter that lets it be used here.";
	}

	public ContentSlot<T> Default(Func<T> factory) {
		fallback = factory;
		return this;
	}

	public ContentSlot<T> From(Func<IContentSource?> source) {
		sources.Add(source);
		return this;
	}

	public ContentSlot<T> Required() {
		required = true;
		return this;
	}

	public void Resolve() {
		foreach (var source in sources) {
			if (TryCreate(source(), out var value)) {
				Set(value);
				return;
			}
		}

		if (fallback != null)
			Set(fallback());
		else if (required)
			throw new ContentSlotException($"Nothing could fill the {typeof(T).Name} slot.");
	}

	public void Set(T? value) {
		var old = Value;
		Value = value;
		Changed?.Invoke(old, value);
	}

	public bool TryCreate(IContentSource? source, [NotNullWhen(true)] out T? value) {
		value = null;
		if (source == null || !Plan(source, out var adapter))
			return false;

		var runtime = Create(source.GetRuntimeType(), source);
		if (adapter == null) {
			value = (T)runtime;
			return true;
		}

		Logs.Info($"Using {adapter.Name} to fit {source.GetName()} into the {typeof(T).Name} slot.");
		value = (T)Create(adapter, source, runtime);
		return true;
	}

	object Create(Type type, params object[] candidates) {
		var used = candidates.Where(candidate => type.GetConstructors().Any(ctor => ctor.GetParameters().Any(p => p.ParameterType.IsInstanceOfType(candidate)))).ToArray();
		return ActivatorUtilities.CreateInstance(level, type, used);
	}

	bool CanConstruct(Type type, IContentSource source, Type? runtimeType = null) {
		var services = level.GetRequiredService<IServiceProviderIsService>();
		foreach (var ctor in type.GetConstructors())
			if (ctor.GetParameters().All(p => p.ParameterType.IsInstanceOfType(source)
				|| (runtimeType != null && p.ParameterType.IsAssignableFrom(runtimeType))
				|| p.HasDefaultValue
				|| services.IsService(p.ParameterType)))
				return true;
		return false;
	}

	bool Plan(IContentSource source, out Type? adapter) {
		adapter = null;
		var runtimeType = source.GetRuntimeType();
		if (!CanConstruct(runtimeType, source))
			return false;
		if (typeof(T).IsAssignableFrom(runtimeType))
			return true;

		adapter = ContentAdapters.Find(runtimeType, typeof(T));
		return adapter != null && CanConstruct(adapter, source, runtimeType);
	}
}

public sealed record ContentSlotCheck(Func<RhythmGameLevel, string?> Run);

public static class ContentSlotExts
{
	public static IServiceCollection AddSlot<T>(this IServiceCollection parts, Action<ContentSlot<T>> configure) where T : class {
		parts.AddSingleton(new ContentSlotCheck(level => {
			var slot = new ContentSlot<T>(level);
			configure(slot);
			return slot.Check();
		}));
		return parts.AddPart(sp => {
			var slot = new ContentSlot<T>(sp.GetRequiredService<RhythmGameLevel>());
			configure(slot);
			slot.Resolve();
			return slot;
		});
	}
}
