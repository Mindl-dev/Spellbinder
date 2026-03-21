using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace SpellServer
{
    /// <summary>Central registry for all stateful objects.
    /// Enables hot reload (serialize all → restart → deserialize all)
    /// and admin console (inspect/modify any state via API).</summary>
    public class StateManager
    {
        private static readonly Lazy<StateManager> _instance =
            new Lazy<StateManager>(() => new StateManager());

        public static StateManager Instance => _instance.Value;

        private readonly Dictionary<string, HasState> _registry =
            new Dictionary<string, HasState>();

        private readonly object _lock = new object();

        private StateManager() { }

        /// <summary>Register a stateful object. Called automatically by HasState constructor.</summary>
        public void Register(HasState obj)
        {
            lock (_lock)
            {
                _registry[obj.StateKey] = obj;
            }
        }

        /// <summary>Deregister a stateful object. Called automatically by HasState.Dispose.</summary>
        public void Deregister(HasState obj)
        {
            lock (_lock)
            {
                _registry.Remove(obj.StateKey);
            }
        }

        /// <summary>Get all registered state keys.</summary>
        public List<string> GetKeys()
        {
            lock (_lock)
            {
                return _registry.Keys.ToList();
            }
        }

        /// <summary>Get state for a single object by key.</summary>
        public Dictionary<string, object> GetState(string key)
        {
            lock (_lock)
            {
                HasState obj;
                if (!_registry.TryGetValue(key, out obj)) return null;
                return obj.GetState();
            }
        }

        /// <summary>Set state for a single object by key.</summary>
        public bool SetState(string key, Dictionary<string, object> state)
        {
            lock (_lock)
            {
                HasState obj;
                if (!_registry.TryGetValue(key, out obj)) return false;
                obj.SetState(state);
                return true;
            }
        }

        /// <summary>Serialize all registered state to a dictionary of dictionaries.</summary>
        public Dictionary<string, Dictionary<string, object>> SerializeAll()
        {
            lock (_lock)
            {
                var result = new Dictionary<string, Dictionary<string, object>>();
                foreach (var kvp in _registry)
                {
                    try
                    {
                        result[kvp.Key] = kvp.Value.GetState();
                    }
                    catch (Exception ex)
                    {
                        Program.Log($"[StateManager] Failed to serialize {kvp.Key}: {ex.Message}", Color.Red);
                    }
                }
                return result;
            }
        }

        /// <summary>Deserialize state into all registered objects.
        /// Objects must already be constructed and registered — this restores their state.</summary>
        public void DeserializeAll(Dictionary<string, Dictionary<string, object>> allState)
        {
            lock (_lock)
            {
                foreach (var kvp in allState)
                {
                    HasState obj;
                    if (_registry.TryGetValue(kvp.Key, out obj))
                    {
                        try
                        {
                            obj.SetState(kvp.Value);
                        }
                        catch (Exception ex)
                        {
                            Program.Log($"[StateManager] Failed to deserialize {kvp.Key}: {ex.Message}", Color.Red);
                        }
                    }
                    else
                    {
                        Program.Log($"[StateManager] No object registered for key {kvp.Key}, skipping", Color.Orange);
                    }
                }
            }
        }

        /// <summary>Number of registered stateful objects.</summary>
        public int Count
        {
            get { lock (_lock) { return _registry.Count; } }
        }
    }
}
