using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Common.Game;

public sealed class ContentSlot<T>(RhythmGameLevel level) where T : class
{
	readonly List<Func<IContentSource?>> sources = [];
	Func<T>? fallback;
	bool required;

	public event Action<T?, T?>? Changed;

	public T? Value { get; private set; }

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
			throw new InvalidOperationException($"Nothing could fill the {typeof(T).Name} slot.");
	}

	public void Set(T? value) {
		var old = Value;
		Value = value;
		Changed?.Invoke(old, value);
	}

	public bool TryCreate(IContentSource? source, [NotNullWhen(true)] out T? value) {
		value = null;
		if (source == null)
			return false;

		var runtimeType = source.GetRuntimeType();
		if (!typeof(T).IsAssignableFrom(runtimeType) || !CanConstruct(source, runtimeType))
			return false;

		value = (T)ActivatorUtilities.CreateInstance(level, runtimeType, source);
		return true;
	}

	bool CanConstruct(IContentSource source, Type runtimeType) {
		var services = level.GetRequiredService<IServiceProviderIsService>();
		foreach (var ctor in runtimeType.GetConstructors())
			if (ctor.GetParameters().All(p => p.ParameterType.IsInstanceOfType(source) || p.HasDefaultValue || services.IsService(p.ParameterType)))
				return true;
		return false;
	}
}

public static class ContentSlotExts
{
	public static IServiceCollection AddSlot<T>(this IServiceCollection parts, Action<ContentSlot<T>> configure) where T : class
		=> parts.AddPart(sp => {
			var slot = new ContentSlot<T>(sp.GetRequiredService<RhythmGameLevel>());
			configure(slot);
			slot.Resolve();
			return slot;
		});
}
