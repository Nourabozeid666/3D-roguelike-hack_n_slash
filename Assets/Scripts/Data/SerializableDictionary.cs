using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

    public Dictionary<TKey, TValue> Dict => dictionary;
    public TValue this[TKey key] => dictionary[key];
    public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);

    public void OnBeforeSerialize()
    {
        // keys.Clear();
        // values.Clear();
        // foreach (var kvp in dictionary)   // <-- rebuilds keys/values FROM the dictionary
        // {
        //     keys.Add(kvp.Key);
        //     values.Add(kvp.Value);
        // }
    }

    public void OnAfterDeserialize()
    {
        dictionary = new Dictionary<TKey, TValue>();
        for (int i = 0; i < keys.Count && i < values.Count; i++)
            dictionary[keys[i]] = values[i];
    }
}