using System.Collections.Generic;

namespace JevWrapper
{
    /// <summary>
    /// Automatic per-group latest-value store, owned by one JevClient. No config, no history window.
    /// </summary>
    public class JevContext
    {
        private readonly Dictionary<string, Dictionary<string, float>> _groups =
            new Dictionary<string, Dictionary<string, float>>();

        public void Update(string name, string group, float value)
        {
            if (string.IsNullOrEmpty(group)) return;
            if (!_groups.TryGetValue(group, out var signals))
            {
                signals = new Dictionary<string, float>();
                _groups[group] = signals;
            }
            signals[name] = value;
        }

        public Dictionary<string, float> Snapshot(string group)
        {
            if (_groups.TryGetValue(group, out var signals))
                return new Dictionary<string, float>(signals);
            return new Dictionary<string, float>();
        }

        public void Clear(string group = null)
        {
            if (group == null) _groups.Clear();
            else _groups.Remove(group);
        }
    }
}
