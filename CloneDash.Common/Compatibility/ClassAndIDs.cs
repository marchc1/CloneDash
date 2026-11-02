using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace CloneDash.Common.Compatibility;

public delegate string ProduceClassIDFn<T>(T instance) where T : notnull;
public delegate string? ProduceClassUniqueIDFn<T>(T instance) where T : notnull;

public sealed class ClassAndIDs<T>(ProduceClassIDFn<T> produceID, ProduceClassUniqueIDFn<T>? produceUniqueID = null) where T : notnull
{
	sealed class DataSnapshot(T[] instances, string[] ids, string?[] uniqueIDs)
	{
		public static readonly DataSnapshot Empty = new([], [], []);
		public readonly T[] Instances = instances;
		public readonly string[] IDs = ids;
		public readonly string?[] UniqueIDs = uniqueIDs;
	}

	readonly Lock Lock = new();

	readonly List<T> Instances = [];
	readonly List<string> IDs = [];
	readonly List<string?> UniqueIDs = [];
	readonly Dictionary<string, T> IDToInstance = new(StringComparer.Ordinal);
	readonly Dictionary<T, string> InstanceToID = [];
	readonly Dictionary<string, T> UniqueIDToInstance = new(StringComparer.Ordinal);
	readonly Dictionary<T, string> InstanceToUniqueID = [];

	volatile DataSnapshot? Snapshot = DataSnapshot.Empty;


	public bool Add(T instance) {
		if (instance is null) return false;

		string id = produceID(instance);
		string? uniqueID = produceUniqueID?.Invoke(instance);

		lock (Lock) {
			if (InstanceToID.ContainsKey(instance)) return false;
			if (IDToInstance.ContainsKey(id)) return false;
			if (uniqueID is not null && UniqueIDToInstance.ContainsKey(uniqueID)) return false;

			IDToInstance.Add(id, instance);
			InstanceToID.Add(instance, id);
			if (uniqueID is not null) {
				UniqueIDToInstance.Add(uniqueID, instance);
				InstanceToUniqueID.Add(instance, uniqueID);
			}

			Instances.Add(instance);
			IDs.Add(id);
			UniqueIDs.Add(uniqueID);
			Snapshot = null;
			return true;
		}
	}

	public bool Remove(T instance) {
		if (instance is null) return false;

		lock (Lock) {
			if (!InstanceToID.ContainsKey(instance)) return false;
			RemoveLocked(instance);
			return true;
		}
	}

	public bool RemoveByID(string id) {
		lock (Lock) {
			if (!IDToInstance.TryGetValue(id, out T? instance)) return false;
			RemoveLocked(instance);
			return true;
		}
	}

	public bool RemoveByUniqueID(string uniqueID) {
		lock (Lock) {
			if (!UniqueIDToInstance.TryGetValue(uniqueID, out T? instance)) return false;
			RemoveLocked(instance);
			return true;
		}
	}

	void RemoveLocked(T instance) {
		InstanceToID.Remove(instance, out string? id);
		IDToInstance.Remove(id!);
		if (InstanceToUniqueID.Remove(instance, out string? uniqueID))
			UniqueIDToInstance.Remove(uniqueID);

		int index = Instances.IndexOf(instance);
		Instances.RemoveAt(index);
		IDs.RemoveAt(index);
		UniqueIDs.RemoveAt(index);
		Snapshot = null;
	}

	public void Clear() {
		lock (Lock) {
			Instances.Clear();
			IDs.Clear();
			UniqueIDs.Clear();
			IDToInstance.Clear();
			InstanceToID.Clear();
			UniqueIDToInstance.Clear();
			InstanceToUniqueID.Clear();
			Snapshot = DataSnapshot.Empty;
		}
	}

	public bool TryGetID(T instance, [NotNullWhen(true)] out string? id) {
		lock (Lock) return InstanceToID.TryGetValue(instance, out id);
	}

	public bool TryGetUniqueID(T instance, [NotNullWhen(true)] out string? uniqueID) {
		lock (Lock) return InstanceToUniqueID.TryGetValue(instance, out uniqueID);
	}

	public bool TryGetInstance(ReadOnlySpan<char> id, [MaybeNullWhen(false)] out T instance) {
		lock (Lock) return IDToInstance.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(id, out instance);
	}

	public bool TryGetInstanceByUniqueID(ReadOnlySpan<char> uniqueID, [MaybeNullWhen(false)] out T instance) {
		lock (Lock) return UniqueIDToInstance.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(uniqueID, out instance);
	}

	public bool Contains(T instance) {
		lock (Lock) return InstanceToID.ContainsKey(instance);
	}

	public bool ContainsID(string id) {
		lock (Lock) return IDToInstance.ContainsKey(id);
	}

	public bool ContainsUniqueID(string uniqueID) {
		lock (Lock) return UniqueIDToInstance.ContainsKey(uniqueID);
	}

	public int Count => GetSnapshot().Instances.Length;

	DataSnapshot GetSnapshot() {
		DataSnapshot? s = Snapshot;
		if (s is not null) return s;

		lock (Lock) {
			return Snapshot ??= new DataSnapshot(Instances.ToArray(), IDs.ToArray(), UniqueIDs.ToArray());
		}
	}

	public ReadOnlySpan<T> GetInstances() => GetSnapshot().Instances;
	public ReadOnlySpan<string> GetIDs() => GetSnapshot().IDs;
	public ReadOnlySpan<string?> GetUniqueIDs() => GetSnapshot().UniqueIDs;

	public View GetView() {
		DataSnapshot s = GetSnapshot();
		return new View(s.Instances, s.IDs, s.UniqueIDs);
	}
	public Enumerator GetEnumerator() => GetView().GetEnumerator();

	public readonly ref struct View
	{
		public readonly ReadOnlySpan<T> Instances;
		public readonly ReadOnlySpan<string> IDs;
		public readonly ReadOnlySpan<string?> UniqueIDs;

		internal View(ReadOnlySpan<T> instances, ReadOnlySpan<string> ids, ReadOnlySpan<string?> uniqueIDs) {
			Instances = instances;
			IDs = ids;
			UniqueIDs = uniqueIDs;
		}

		public int Count => Instances.Length;
		public (T Instance, string ID, string? UniqueID) this[int index] => (Instances[index], IDs[index], UniqueIDs[index]);
		public Enumerator GetEnumerator() => new(Instances, IDs, UniqueIDs);
	}

	public ref struct Enumerator
	{
		readonly ReadOnlySpan<T> _instances;
		readonly ReadOnlySpan<string> _ids;
		readonly ReadOnlySpan<string?> _uniqueIDs;
		int _index;

		internal Enumerator(ReadOnlySpan<T> instances, ReadOnlySpan<string> ids, ReadOnlySpan<string?> uniqueIDs) {
			_instances = instances;
			_ids = ids;
			_uniqueIDs = uniqueIDs;
			_index = -1;
		}

		public bool MoveNext() => ++_index < _instances.Length;
		public readonly (T Instance, string ID, string? UniqueID) Current => (_instances[_index], _ids[_index], _uniqueIDs[_index]);
	}
}