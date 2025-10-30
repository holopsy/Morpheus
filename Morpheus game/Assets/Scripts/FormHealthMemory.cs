using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FormHealthMemory : MonoBehaviour
{
    [System.Serializable]
    public struct HealthState
    {
        public int current;
        public int max;
        public HealthState(int c, int m) { current = c; max = m; }
    }

    // Key = form id ("Default", "Agile", "Power", "Flying")
    private readonly Dictionary<string, HealthState> memory = new Dictionary<string, HealthState>();

    /// <summary>Save or overwrite health for a form.</summary>
    public void Save(string formId, int current, int max)
    {
        if (string.IsNullOrEmpty(formId)) return;
        memory[formId] = new HealthState(current, max);
    }

    /// <summary>Try to load saved health for a form.</summary>
    public bool TryLoad(string formId, out HealthState state)
    {
        if (string.IsNullOrEmpty(formId))
        {
            state = default;
            return false;
        }
        return memory.TryGetValue(formId, out state);
    }

    /// <summary>Clear ALL stored forms (used on death/respawn so everyone resets).</summary>
    public void ClearAll()
    {
        memory.Clear();
    }

    /// <summary>Remove a single form’s entry (not required, but handy).</summary>
    public void Remove(string formId)
    {
        if (string.IsNullOrEmpty(formId)) return;
        memory.Remove(formId);
    }
}