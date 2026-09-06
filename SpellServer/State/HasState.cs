using System;
using System.Collections.Generic;

namespace SpellServer
{
    /// <summary>Base class for any object whose state should survive a server restart.
    /// Subclasses must implement GetState/SetState for serialization, and provide
    /// a unique StateKey for registry lookup.
    ///
    /// Construction auto-registers with StateManager. Disposal auto-deregisters.</summary>
    public abstract class HasState : IDisposable
    {
        /// <summary>Unique key for this instance, e.g. "arena:1" or "player:42".
        /// Must be unique across all registered objects.</summary>
        public abstract string StateKey { get; }

        /// <summary>Serialize this object's state to a dictionary.
        /// Values should be primitives, strings, or nested dictionaries —
        /// anything that serializes cleanly to JSON.</summary>
        public abstract Dictionary<string, object> GetState();

        /// <summary>Restore this object's state from a dictionary.
        /// Called during hot reload after construction.</summary>
        public abstract void SetState(Dictionary<string, object> state);

        protected HasState()
        {
            StateManager.Instance.Register(this);
        }

        public void Dispose()
        {
            StateManager.Instance.Deregister(this);
        }
    }
}
