using System;
using System.IO;
using Helper;
using Helper.Network;

namespace SpellServer
{
    public enum PacketOutFunction : byte
    {
        PlayerMoveState = 0x01,
        PlayerJoin = 0x03,
        PlayerLeave = 0x04,
        CabalIDUpdate = 0x05,
        PlayerDeath = 0x06,
        Chat = 0x07,
        ObjectDeath = 0x0A,
        PlayerMoveStateShort = 0x12,
        UpdateHealth = 0x13,
        PlayerHit = 0x14,
        PlayerState = 0x18,
        ArenaCreated = 0x19,
        ArenaState = 0x1A,
        PlayerResurrect = 0x21,
        ArenaDeleted = 0x24,
        CastBolt = 0x25,
        BiasedPool = 0x29,
        BiasedShrine = 0x2B,
        CastTargeted = 0x2D,
        ThinDamage = 0x2F,
        CalledGhost = 0x30,
        ActivatedTrigger = 0x31,
        PlayerEnterLarge = 0x34,
        WorldEnterLarge = 0x36,
        UpdateShrinePoolState = 0x37,
        TableCreated = 0x42,
        TableDeleted = 0x43,
        TableEnterLarge = 0x44,
        SendAdminStatus = 0x51, // Was PlayWebMusic
        InviteToTable = 0x52,
        SwitchedToTable = 0x53,
        SendCharacterInSlot = 0x55,
        SaveSuccess = 0x58,
        SaveError = 0x59,
        FireTeamEnterLarge = 0x5C,
        PlaySound = 0x61,
        IsNameTaken = 0x64,
        IsNameValid = 0x6B,
        ClaimAreana = 0x73,
        SendPlayerId = 0x80,
        HeartbeatReply = 0x81,
        LoginConnected = 0x82,
        SuccessfulArenaEntry = 0x83,
        LoginError = 0x84,
        HasEnteredWorld = 0x85,
        HighScores = 0xA2,
        PlayerYank = 0xA4,
        KillPlayer = 0xA9,
        UnBan = 0xA9,
        PlayerJump = 0xAC,
        PlayerGod = 0xAD,
        UpdateExperience = 0xAE,
        CastProjectile = 0xB0,
        CastRune = 0xB2,
        CastEffect = 0xB3,
        CastWall = 0xB4,
        CabalInvite = 0xB5,
        CabalLeave = 0xB6,
        CabalJoin = 0xB7,
        SendCabalList = 0xBA,
        ResetPlayer = 0xBB,
        SpawnPlayer = 0x02,
        EstablishDatagram = 0x87,
    }

    public class Packet
    {
        public Byte[] PacketData;
        public PacketOutFunction Function;

        /// <summary>Build an outgoing server→client packet.
        /// Layout: [2:length] [2:source_id] [1:func_id] [N:data] [2:trailing]
        /// The inStream starts with [0x00, func_id, data...] — the leading 0x00
        /// is the low byte of source_id (originally hardcoded to 0).</summary>
        /// <param name="inStream">Stream starting with 0x00 + func_id + payload</param>
        /// <param name="sourceId">Player ID for the client to route this packet (0 = server/system)</param>
        public Packet(MemoryStream inStream, UInt16 sourceId = 0)
        {
            // Outgoing packet layout: [2:length] [2:source_id] [1:func_id] [N:data] [2:trailing]
            // inStream arrives as: [0x00(padding), func_id, data...]
            // We replace the padding byte with source_id, keeping the total size identical.
            // Length field = inStream.Length = source_id(1 byte used) + func_id + data
            Byte[] streamData = inStream.ToArray();
            Int16 payloadLength = (Int16)inStream.Length;
            MemoryStream outStream = new MemoryStream(streamData.Length + 5);
            outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(payloadLength)), 0, 2);
            // Source ID as big-endian uint16 — replaces the [padding, inStream[0]] pair
            outStream.WriteByte((Byte)(sourceId >> 8));
            outStream.WriteByte((Byte)(sourceId & 0xFF));
            // Write func_id + payload (skip the leading 0x00 padding byte from inStream)
            outStream.Write(streamData, 1, streamData.Length - 1);
            outStream.WriteByte(0x00);
            outStream.WriteByte(0x00);
            PacketData = outStream.GetBuffer();
            Function = (PacketOutFunction)PacketData[4];
        }
    }
}
