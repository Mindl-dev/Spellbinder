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
        SpawnPlayer = 0x02,
        EstablishDatagram = 0x87,
    }

    public class Packet
    {
        public Byte[] PacketData;
        public PacketOutFunction Function;

        public Packet(MemoryStream inStream)
        {
            MemoryStream outStream = new MemoryStream((Int32)inStream.Length + 5);
            outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)inStream.Length)), 0, 2);
            outStream.WriteByte(0x00);
            inStream.WriteTo(outStream);
            outStream.WriteByte(0x00);
            outStream.WriteByte(0x00);
            PacketData = outStream.GetBuffer();
            Function = (PacketOutFunction)PacketData[4];
        }
    }
}
