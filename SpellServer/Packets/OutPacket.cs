using System;
using System.IO;

namespace SpellServer.Packets
{
    /// <summary>
    /// Base class for outgoing (server->client) packets.
    ///
    /// Each subclass takes whatever game objects it needs in its constructor
    /// (Player, Arena, Table, etc.) and serializes them in ToBytes().
    /// Side effects belong in the system that sends the packet, not here.
    /// </summary>
    public abstract class OutPacket
    {
        /// <summary>Raw opcode byte for this packet type.</summary>
        public abstract byte Opcode { get; }

        /// <summary>Serialize this packet to wire format.</summary>
        public abstract MemoryStream ToBytes();
    }
}
