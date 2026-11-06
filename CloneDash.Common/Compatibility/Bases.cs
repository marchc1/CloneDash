using Nucleus;
using Nucleus.Commands;
using Nucleus.Common.Commands;
using Nucleus.Util;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Common.Compatibility;

// A Unique ID (UID) is a combination of three identifiers in the chain, of which all these systems have:
//		e.g. scenes/musedash1/scene_01
//			   ^	   ^	     ^------------- Descriptor
//			   ^	   ^----------------------- Provider
//			   ^------------------------------- Store

public delegate bool PreDescriptorChangedFn<TDescriptor>(TDescriptor? previous, TDescriptor? current);
public delegate void PostDescriptorChangedFn<TDescriptor>(TDescriptor? previous, TDescriptor? current);

public interface ICloneDashModNameID
{
	string GetName();
	string GetID();
}

public interface ICloneDashModUniqueID
{
	string GetUniqueID();
}

public static class UniqueIDFactory
{
	public const char Separator = '/';

	public static string BuildUniqueID(ReadOnlySpan<char> storeID, ReadOnlySpan<char> providerID)
		=> $"{storeID}{Separator}{providerID}";
	public static string BuildUniqueID(ReadOnlySpan<char> storeID, ReadOnlySpan<char> providerID, ReadOnlySpan<char> descriptorID)
		=> $"{storeID}{Separator}{providerID}{Separator}{descriptorID}";

	public static bool TryParse(ReadOnlySpan<char> uid, out Range store, out Range provider, out Range descriptor) {
		store = provider = descriptor = default;

		int first = uid.IndexOf(Separator);
		if (first <= 0) return false;
		int second = uid[(first + 1)..].IndexOf(Separator);
		if (second <= 0) return false;
		second += first + 1;
		if (second + 1 >= uid.Length) return false;

		store = ..first;
		provider = (first + 1)..second;
		descriptor = (second + 1)..;
		return true;
	}

	public static bool TryParse(ReadOnlySpan<char> uid, out Range store, out Range provider) {
		store = provider = default;

		int first = uid.IndexOf(Separator);
		if (first <= 0 || first + 1 >= uid.Length) return false;

		store = ..first;
		provider = (first + 1)..;
		return true;
	}
}

public interface ICloneDashModStore : ICloneDashModNameID
{
	int GetProviderCount();
	ICloneDashModProvider? GetProviderByID(ReadOnlySpan<char> id);
	ICloneDashModProvider? GetProviderByName(ReadOnlySpan<char> name);
	bool AddProvider(ICloneDashModProvider provider);
	bool RemoveProvider(ICloneDashModProvider provider);

	IEnumerable<string> GetAllDescriptorUIDs();
	ICloneDashModDescriptor? GetDescriptorByUID(ReadOnlySpan<char> uid);
	void ClearDescriptorCache();
}

public interface ICloneDashModProvider : ICloneDashModNameID, ICloneDashModUniqueID
{
	ICloneDashModStore? GetStore();
	void OnAddedToStore(ICloneDashModStore store);
	void OnRemovedFromStore(ICloneDashModStore store);

	int GetSortIndex() => 0;
	bool IsEnabled() => true;

	IEnumerable<string> GetAvailableIDs();

	string ICloneDashModUniqueID.GetUniqueID() {
		var store = GetStore();
		return store == null ? GetID() : UniqueIDFactory.BuildUniqueID(store.GetID(), GetID());
	}
}

public interface ICloneDashModProvider<TDescriptor> : ICloneDashModProvider
	where TDescriptor : class, ICloneDashModDescriptor
{
	TDescriptor? FindByID(ReadOnlySpan<char> id);
}

public interface ICloneDashModDescriptor : ICloneDashModNameID, ICloneDashModUniqueID, IUniquelyIdentifiableObject
{
	ICloneDashModProvider GetProvider();
	ICloneDashModStore? GetStore() => GetProvider().GetStore();

	string ICloneDashModUniqueID.GetUniqueID() {
		var provider = GetProvider();
		var store = provider.GetStore();
		return store == null
			? UniqueIDFactory.BuildUniqueID(provider.GetID(), GetID())
			: UniqueIDFactory.BuildUniqueID(store.GetID(), provider.GetID(), GetID());
	}

	ReadOnlySpan<char> IUniquelyIdentifiableObject.GetUUID() => GetUniqueID();
}

public abstract class ModProvider<TDescriptor>(string name, string id) : ICloneDashModProvider<TDescriptor>
	where TDescriptor : class, ICloneDashModDescriptor
{
	protected readonly string name = name;
	protected readonly string id = id;
	ICloneDashModStore? store;
	string? uniqueID;

	public string GetName() => name;
	public string GetID() => id;
	public string GetUniqueID() => uniqueID ??= store == null ? id : UniqueIDFactory.BuildUniqueID(store.GetID(), id);
	public ICloneDashModStore? GetStore() => store;

	public virtual void OnAddedToStore(ICloneDashModStore store) {
		this.store = store;
		uniqueID = null;
	}
	public virtual void OnRemovedFromStore(ICloneDashModStore store) {
		if (this.store != store) return;
		this.store = null;
		uniqueID = null;
	}

	public virtual int GetSortIndex() => 0;
	public virtual bool IsEnabled() => true;

	public abstract IEnumerable<string> GetAvailableIDs();
	public abstract TDescriptor? FindByID(ReadOnlySpan<char> id);
}

public abstract class ModDescriptor : ICloneDashModDescriptor
{
	string? uniqueID;

	public abstract string GetName();
	public abstract string GetID();
	public abstract ICloneDashModProvider GetProvider();
	public ICloneDashModStore? GetStore() => GetProvider().GetStore();

	protected void InvalidateUniqueID() => uniqueID = null;

	public string GetUniqueID() {
		if (uniqueID != null) return uniqueID;
		var provider = GetProvider();
		var store = provider.GetStore();
		return uniqueID = store == null
			? UniqueIDFactory.BuildUniqueID(provider.GetID(), GetID())
			: UniqueIDFactory.BuildUniqueID(store.GetID(), provider.GetID(), GetID());
	}

	public ReadOnlySpan<char> GetUUID() => GetUniqueID();
}

public abstract class SimpleModDescriptor(string name, string id, ICloneDashModProvider provider) : ModDescriptor
{
	protected readonly string name = name;
	protected readonly string id = id;
	protected readonly ICloneDashModProvider provider = provider;

	public override string GetName() => name;
	public override string GetID() => id;
	public override ICloneDashModProvider GetProvider() => provider;
}

public static class ModStores
{
	static class Holder<T> where T : class, ICloneDashModStore, new()
	{
		public static readonly T Instance = new();
	}

	static readonly ClassAndIDs<ICloneDashModStore> stores = new(static s => s.GetID());

	public static T Get<T>() where T : class, ICloneDashModStore, new() => Holder<T>.Instance;

	internal static void Register(ICloneDashModStore store) {
		if (!stores.Add(store))
			Logs.Warn($"A mod store with the ID '{store.GetID()}' was already registered.");
	}

	public static ReadOnlySpan<ICloneDashModStore> GetAll() => stores.GetInstances();
	public static ICloneDashModStore? GetByID(ReadOnlySpan<char> id) => stores.TryGetInstance(id, out var store) ? store : null;

	public static ICloneDashModDescriptor? GetDescriptorByUID(ReadOnlySpan<char> uid) {
		if (!UniqueIDFactory.TryParse(uid, out Range storeRange, out _, out _)) return null;
		return GetByID(uid[storeRange])?.GetDescriptorByUID(uid);
	}
}

public abstract class ModStore<TProvider, TDescriptor> : ICloneDashModStore
	where TProvider : class, ICloneDashModProvider<TDescriptor>
	where TDescriptor : class, ICloneDashModDescriptor
{
	readonly string name;
	readonly string id;
	readonly ClassAndIDs<TProvider> providers = new(static p => p.GetID());
	readonly ClassAndIDs<TDescriptor> descriptors = new(static d => d.GetUniqueID());
	readonly Lock discoveryLock = new();
	volatile bool discovered;

	protected ModStore(string name, string id) {
		this.name = name;
		this.id = id;
		ModStores.Register(this);
	}

	public string GetName() => name;
	public string GetID() => id;

	protected virtual TProvider[] DiscoverProviders() => ReflectionTools.InstantiateAllInheritorsOfInterface<TProvider>();

	void EnsureDiscovered() {
		if (discovered) return;
		lock (discoveryLock) {
			if (discovered) return;
			TProvider[] found = DiscoverProviders();
			found.Sort(static (a, b) => a.GetSortIndex().CompareTo(b.GetSortIndex()));
			foreach (var provider in found)
				AddProviderInternal(provider);
			discovered = found.Length > 0;
		}
	}

	bool AddProviderInternal(TProvider provider) {
		if (!providers.Add(provider)) {
			Logs.Warn($"[{id}] Provider '{provider.GetID()}' ({provider.GetType().FullName}) could not be added; ID already in use?");
			return false;
		}
		provider.OnAddedToStore(this);
		return true;
	}

	public bool AddProvider(TProvider provider) {
		EnsureDiscovered();
		return AddProviderInternal(provider);
	}

	public bool RemoveProvider(TProvider provider) {
		EnsureDiscovered();
		if (!providers.Remove(provider)) return false;

		foreach (var descriptor in descriptors.GetInstances())
			if (descriptor.GetProvider() == provider)
				descriptors.Remove(descriptor);

		provider.OnRemovedFromStore(this);
		return true;
	}

	public int GetProviderCount() {
		EnsureDiscovered();
		return providers.Count;
	}

	public ReadOnlySpan<TProvider> GetAllProviders() {
		EnsureDiscovered();
		return providers.GetInstances();
	}

	public TProvider? GetProviderByID(ReadOnlySpan<char> id) {
		EnsureDiscovered();
		return providers.TryGetInstance(id, out var provider) ? provider : null;
	}

	public TProvider? GetProviderByName(ReadOnlySpan<char> name) {
		if (name.IsWhiteSpace()) return null;
		foreach (var provider in GetAllProviders())
			if (name.Equals(provider.GetName(), StringComparison.InvariantCultureIgnoreCase))
				return provider;
		return null;
	}

	public string BuildUID(TProvider provider, ReadOnlySpan<char> descriptorID)
		=> UniqueIDFactory.BuildUniqueID(id, provider.GetID(), descriptorID);

	public IEnumerable<string> GetAllDescriptorUIDs() {
		EnsureDiscovered();
		foreach (var provider in providers.GetInstances().ToArray()) {
			if (!provider.IsEnabled()) continue;
			foreach (var descriptorID in provider.GetAvailableIDs())
				yield return BuildUID(provider, descriptorID);
		}
	}

	public IEnumerable<TDescriptor> GetAllDescriptors() {
		foreach (var uid in GetAllDescriptorUIDs()) {
			var descriptor = GetDescriptorByUID(uid);
			if (descriptor != null)
				yield return descriptor;
		}
	}

	public TDescriptor? GetDescriptor(TProvider provider, ReadOnlySpan<char> descriptorID) {
		EnsureDiscovered();
		if (!provider.IsEnabled()) return null;

		string uid = BuildUID(provider, descriptorID);
		if (descriptors.TryGetInstance(uid, out var cached)) return cached;

		TDescriptor? descriptor = provider.FindByID(descriptorID);
		if (descriptor == null) return null;

		if (!descriptors.Add(descriptor) && descriptors.TryGetInstance(uid, out cached))
			return cached;

		return descriptor;
	}

	public TDescriptor? GetDescriptorByUID(ReadOnlySpan<char> uid) {
		EnsureDiscovered();
		if (uid.IsWhiteSpace()) return null;
		if (descriptors.TryGetInstance(uid, out var cached)) return cached;

		if (!UniqueIDFactory.TryParse(uid, out Range storeRange, out Range providerRange, out Range descriptorRange)) return null;
		if (!uid[storeRange].Equals(id, StringComparison.Ordinal)) return null;

		TProvider? provider = GetProviderByID(uid[providerRange]);
		if (provider == null) return null;

		return GetDescriptor(provider, uid[descriptorRange]);
	}

	public bool TryGetDescriptorByUID(ReadOnlySpan<char> uid, [NotNullWhen(true)] out TDescriptor? descriptor) {
		descriptor = GetDescriptorByUID(uid);
		return descriptor != null;
	}

	public bool UncacheDescriptor(TDescriptor descriptor) => descriptors.Remove(descriptor);
	public void ClearDescriptorCache() => descriptors.Clear();

	public void AutocompleteUIDs(ConCommandBase cmd, string argsStr, TokenizedCommand args, int curArgPos, ref string[] returns, ref string[]? returnHelp) {
		returns = GetAllDescriptorUIDs().Where(x => x.StartsWith(args.ArgS(curArgPos), StringComparison.OrdinalIgnoreCase)).ToArray();
	}

	public void PrintAll() {
		foreach (var uid in GetAllDescriptorUIDs())
			Logs.Print($"    {uid}");
	}

	ICloneDashModProvider? ICloneDashModStore.GetProviderByID(ReadOnlySpan<char> id) => GetProviderByID(id);
	ICloneDashModProvider? ICloneDashModStore.GetProviderByName(ReadOnlySpan<char> name) => GetProviderByName(name);
	bool ICloneDashModStore.AddProvider(ICloneDashModProvider provider) => provider is TProvider typed && AddProvider(typed);
	bool ICloneDashModStore.RemoveProvider(ICloneDashModProvider provider) => provider is TProvider typed && RemoveProvider(typed);
	ICloneDashModDescriptor? ICloneDashModStore.GetDescriptorByUID(ReadOnlySpan<char> uid) => GetDescriptorByUID(uid);
}

public abstract class ActiveModStore<TProvider, TDescriptor> : ModStore<TProvider, TDescriptor>
	where TProvider : class, ICloneDashModProvider<TDescriptor>
	where TDescriptor : class, ICloneDashModDescriptor
{
	TDescriptor? active;
	bool activeResolved;
	ConVar? convar;

	public event PreDescriptorChangedFn<TDescriptor>? PreChange;
	public event PostDescriptorChangedFn<TDescriptor>? PostChange;

	protected ActiveModStore(string name, string id) : base(name, id) { }

	public ConVar? GetConVar() => convar;

	public void BindConVar(ConVar convar) {
		this.convar = convar;
		activeResolved = false;
		convar.OnAutocomplete ??= AutocompleteUIDs;
		convar.OnChange += ConVarChanged;
	}

	void ConVarChanged(ConVar self, scoped ReadOnlySpan<char> oldStr, double oldDouble) => SetActiveByUID(self.GetString());

	public TDescriptor? GetActive() {
		if (!activeResolved && convar != null)
			SetActiveByUID(convar.GetString());
		return active;
	}

	public TDescriptor? SetActiveByUID(ReadOnlySpan<char> uid) {
		TDescriptor? descriptor = GetDescriptorByUID(uid);
		bool missing = descriptor == null && !uid.IsWhiteSpace();
		if (missing)
			Logs.Warn($"[{GetID()}] '{uid}' could not be found!");
		TDescriptor? previous = SetActive(descriptor);
		if (missing)
			activeResolved = false;
		return previous;
	}

	public TDescriptor? SetActive(TDescriptor? value) {
		activeResolved = true;
		if (value == active)
			return active;

		if (PreChange != null && !PreChange(active, value))
			return active;

		TDescriptor? previous = active;
		active = value;
		PostChange?.Invoke(previous, value);

		return previous;
	}
}
