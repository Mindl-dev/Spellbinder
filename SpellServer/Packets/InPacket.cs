using System;

namespace SpellServer.Packets
{
    /// <summary>
    /// Base class for incoming (client->server) packets.
    ///
    /// Parse bytes in the constructor, hold typed fields,
    /// Apply() contains the legacy game logic (temporary — remove once
    /// the corresponding subsystem handles it).
    /// </summary>
    public abstract class InPacket
    {
        /// <summary>The player who sent this packet.</summary>
        public Player Source { get; set; }

        /// <summary>Raw opcode byte for this packet type.</summary>
        public abstract byte Opcode { get; }

        /// <summary>
        /// Temporary bridge — runs the old inline handler logic.
        /// Delete once the proper subsystem processes this packet type.
        /// </summary>
        public virtual void Apply(Arena arena) { }
    }
}
