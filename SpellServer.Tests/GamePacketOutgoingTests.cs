using System;
using System.IO;
using NUnit.Framework;
using Helper;
using Helper.Network;
using static SpellServer.Tests.TestHelpers;

namespace SpellServer.Tests
{
    [TestFixture]
    public class GamePacketOutgoingTests
    {
        // ================================================================
        // Batch 1: Zero-dependency outgoing builders (pure primitives/relays)
        // ================================================================

        // --- SuccessfulArenaEntry (4 bytes) ---

        [Test]
        public void Arena_SuccessfulArenaEntry_CorrectLayout()
        {
            byte[] data = GamePacket.Outgoing.Arena.SuccessfulArenaEntry().ToArray();
            Assert.AreEqual(4, data.Length);
            AssertPacketHeader(data, PacketOutFunction.SuccessfulArenaEntry);
            Assert.AreEqual(0x00, data[2]);
            Assert.AreEqual(0x00, data[3]);
        }

        // --- HasEnteredWorld (4 bytes) ---

        [Test]
        public void Player_HasEnteredWorld_CorrectLayout()
        {
            byte[] data = GamePacket.Outgoing.Player.HasEnteredWorld().ToArray();
            Assert.AreEqual(4, data.Length);
            AssertPacketHeader(data, PacketOutFunction.HasEnteredWorld);
            Assert.AreEqual(0x00, data[2]);
            Assert.AreEqual(0x00, data[3]);
        }

        // --- Login.Error (7 bytes) ---

        [Test]
        public void Login_Error_CorrectLayout()
        {
            byte[] data = GamePacket.Outgoing.Login.Error(Subscription.ErrorType.InvalidPassword).ToArray();
            AssertPacketHeader(data, PacketOutFunction.LoginError);
            Assert.AreEqual((byte)Subscription.ErrorType.InvalidPassword, data[2]);
        }

        [Test]
        public void Login_Error_DifferentTypes()
        {
            foreach (Subscription.ErrorType errType in Enum.GetValues(typeof(Subscription.ErrorType)))
            {
                byte[] data = GamePacket.Outgoing.Login.Error(errType).ToArray();
                AssertPacketHeader(data, PacketOutFunction.LoginError);
                Assert.AreEqual((byte)errType, data[2], $"Error type {errType} should be at byte 2");
            }
        }

        // --- SendAdminStatus (5 bytes) ---

        [Test]
        public void System_SendAdminStatus_True()
        {
            byte[] data = GamePacket.Outgoing.System.SendAdminStatus(true).ToArray();
            Assert.AreEqual(5, data.Length);
            AssertPacketHeader(data, PacketOutFunction.SendAdminStatus);
            Assert.AreEqual(0x00, data[2], "padding");
            Assert.AreEqual(0x00, data[3], "padding");
            Assert.AreEqual(0x01, data[4], "true = DevLevel5 (0x01)");
        }

        [Test]
        public void System_SendAdminStatus_False()
        {
            byte[] data = GamePacket.Outgoing.System.SendAdminStatus(false).ToArray();
            Assert.AreEqual(5, data.Length);
            AssertPacketHeader(data, PacketOutFunction.SendAdminStatus);
            Assert.AreEqual(0x00, data[4], "false = StaffLevel3 (0x00)");
        }

        // --- ThinDamage (8 bytes) ---

        [Test]
        public void Arena_ThinDamage_CorrectLayout()
        {
            short objectId = 42;
            short damage = 100;
            byte[] data = GamePacket.Outgoing.Arena.ThinDamage(objectId, damage).ToArray();
            AssertPacketHeader(data, PacketOutFunction.ThinDamage);
            // Bytes 2-3: padding
            Assert.AreEqual(0x00, data[2]);
            Assert.AreEqual(0x00, data[3]);
            // Bytes 4-5: objectId BE
            Assert.AreEqual(objectId, ReadBE16(data, 4));
            // Bytes 6-7: damage BE
            Assert.AreEqual(damage, ReadBE16(data, 6));
        }

        // --- ObjectDeath without player (8 bytes) ---

        [Test]
        public void Arena_ObjectDeath_NoPlayer_CorrectLayout()
        {
            short objectId = 55;
            byte[] data = GamePacket.Outgoing.Arena.ObjectDeath(objectId).ToArray();
            AssertPacketHeader(data, PacketOutFunction.ObjectDeath);
            Assert.AreEqual(objectId, ReadBE16(data, 2));
            // Bytes 4-7: padding
            Assert.AreEqual(0x00, data[4]);
            Assert.AreEqual(0x00, data[5]);
            Assert.AreEqual(0x00, data[6]);
            Assert.AreEqual(0x00, data[7]);
        }

        // --- CastEffect (6 bytes) ---

        [Test]
        public void Arena_CastEffect_CorrectLayout()
        {
            short spellId = 42;
            var ap = MakeArenaPlayer();
            byte[] data = GamePacket.Outgoing.Arena.CastEffect(ap, spellId).ToArray();
            AssertPacketHeader(data, PacketOutFunction.CastEffect);
            Assert.AreEqual(spellId, ReadBE16(data, 2));
            Assert.AreEqual(0x00, data[4]);
            Assert.AreEqual(0x00, data[5]);
        }

        [Test]
        public void Arena_CastEffect_HighSpellId()
        {
            short spellId = 399;
            var ap = MakeArenaPlayer();
            byte[] data = GamePacket.Outgoing.Arena.CastEffect(ap, spellId).ToArray();
            Assert.AreEqual(spellId, ReadBE16(data, 2));
        }

        // --- PlayerJump (6 bytes) ---

        [Test]
        public void Arena_PlayerJump_CorrectLayout()
        {
            short targetId = 7;
            var ap = MakeArenaPlayer();
            byte[] data = GamePacket.Outgoing.Arena.PlayerJump(ap, targetId).ToArray();
            AssertPacketHeader(data, PacketOutFunction.PlayerJump);
            Assert.AreEqual(targetId, ReadBE16(data, 2));
            Assert.AreEqual(0x00, data[4]);
            Assert.AreEqual(0x00, data[5]);
        }

        // --- PlayerGod (4 bytes) ---

        [Test]
        public void Arena_PlayerGod_True()
        {
            var ap = MakeArenaPlayer();
            byte[] data = GamePacket.Outgoing.Arena.PlayerGod(ap, true).ToArray();
            AssertPacketHeader(data, PacketOutFunction.PlayerGod);
        }

        [Test]
        public void Arena_PlayerGod_False()
        {
            var ap = MakeArenaPlayer();
            byte[] data = GamePacket.Outgoing.Arena.PlayerGod(ap, false).ToArray();
            AssertPacketHeader(data, PacketOutFunction.PlayerGod);
        }

        // ================================================================
        // Relay methods — verify raw bytes are preserved unchanged
        // ================================================================

        // --- PlayerMoveState relay (14 bytes) ---

        [Test]
        public void Arena_PlayerMoveState_RelayPreserved()
        {
            byte[] relay = new byte[] { 0x68, 0x6F, 0xF1, 0x40, 0x07, 0x7C, 0x0E, 0x72, 0x24, 0xC8, 0x00, 0x01 };
            var ap = MakeArenaPlayer();
            byte[] data = GamePacket.Outgoing.Arena.PlayerMoveState(ap, relay).ToArray();
            Assert.AreEqual(14, data.Length);
            AssertPacketHeader(data, PacketOutFunction.PlayerMoveState);
            for (int i = 0; i < 12; i++)
                Assert.AreEqual(relay[i], data[i + 2], $"Relay byte {i} mismatch");
        }

        // --- PlayerMoveStateShort relay (10 bytes) ---

        [Test]
        public void Arena_PlayerMoveStateShort_RelayPreserved()
        {
            byte[] relay = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22 };
            var ap = MakeArenaPlayer();
            byte[] data = GamePacket.Outgoing.Arena.PlayerMoveStateShort(ap, relay).ToArray();
            Assert.AreEqual(10, data.Length);
            AssertPacketHeader(data, PacketOutFunction.PlayerMoveStateShort);
            for (int i = 0; i < 8; i++)
                Assert.AreEqual(relay[i], data[i + 2], $"Relay byte {i} mismatch");
        }

        // --- CastTargeted relay (30 bytes) ---

        [Test]
        public void Arena_CastTargeted_RelayPreserved()
        {
            byte[] relay = new byte[28];
            for (int i = 0; i < 28; i++) relay[i] = (byte)(i + 1);
            var ap = MakeArenaPlayer();
            byte[] data = GamePacket.Outgoing.Arena.CastTargeted(ap, relay).ToArray();
            Assert.AreEqual(30, data.Length);
            AssertPacketHeader(data, PacketOutFunction.CastTargeted);
            for (int i = 0; i < 28; i++)
                Assert.AreEqual(relay[i], data[i + 2], $"Relay byte {i} mismatch");
        }

        // --- CastRune relay (22 bytes) ---

        [Test]
        public void Arena_CastRune_RelayPreserved()
        {
            byte[] relay = new byte[20];
            for (int i = 0; i < 20; i++) relay[i] = (byte)(0xA0 + i);
            var ap = MakeArenaPlayer();
            byte[] data = GamePacket.Outgoing.Arena.CastRune(ap, relay).ToArray();
            Assert.AreEqual(22, data.Length);
            AssertPacketHeader(data, PacketOutFunction.CastRune);
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(relay[i], data[i + 2], $"Relay byte {i} mismatch");
        }

        // --- CastBolt relay (36 bytes) ---

        [Test]
        public void Arena_CastBolt_RelayPreserved()
        {
            byte[] relay = new byte[34];
            for (int i = 0; i < 34; i++) relay[i] = (byte)(0x50 + i);
            var ap = MakeArenaPlayer();
            byte[] data = GamePacket.Outgoing.Arena.CastBolt(ap, relay).ToArray();
            Assert.AreEqual(36, data.Length);
            AssertPacketHeader(data, PacketOutFunction.CastBolt);
            for (int i = 0; i < 34; i++)
                Assert.AreEqual(relay[i], data[i + 2], $"Relay byte {i} mismatch");
        }

        // --- CastProjectile relay (18 bytes) ---

        [Test]
        public void Arena_CastProjectile_RelayPreserved()
        {
            byte[] relay = new byte[16];
            for (int i = 0; i < 16; i++) relay[i] = (byte)(0x30 + i);
            var ap = MakeArenaPlayer();
            byte[] data = GamePacket.Outgoing.Arena.CastProjectile(ap, relay).ToArray();
            Assert.AreEqual(18, data.Length);
            AssertPacketHeader(data, PacketOutFunction.CastProjectile);
            for (int i = 0; i < 16; i++)
                Assert.AreEqual(relay[i], data[i + 2], $"Relay byte {i} mismatch");
        }

        // --- CastWall relay (20 bytes) ---

        [Test]
        public void Arena_CastWall_RelayPreserved()
        {
            byte[] relay = new byte[18];
            for (int i = 0; i < 18; i++) relay[i] = (byte)(0x10 + i);
            byte[] data = GamePacket.Outgoing.Arena.CastWall(relay).ToArray();
            Assert.AreEqual(20, data.Length);
            AssertPacketHeader(data, PacketOutFunction.CastWall);
            for (int i = 0; i < 18; i++)
                Assert.AreEqual(relay[i], data[i + 2], $"Relay byte {i} mismatch");
        }

        // ================================================================
        // Batch 2: ArenaPlayer stub tests
        // ================================================================

        // --- PlayerLeave (4 bytes) ---

        [Test]
        public void Arena_PlayerLeave_CorrectLayout()
        {
            var ap = MakeArenaPlayer(id: 3);
            byte[] data = GamePacket.Outgoing.Arena.PlayerLeave(ap).ToArray();
            AssertPacketHeader(data, PacketOutFunction.PlayerLeave);
            Assert.AreEqual(3, ReadBE16(data, 2));
        }

        // --- PlayerState (10 bytes) ---

        [Test]
        public void Arena_PlayerState_CorrectLayout()
        {
            var ap = MakeArenaPlayer(id: 5, level: 10, team: Team.Gryphon, kills: 3, deaths: 1, hp: 100);
            byte[] data = GamePacket.Outgoing.Arena.PlayerState(ap).ToArray();
            AssertPacketHeader(data, PacketOutFunction.PlayerState);
            Assert.AreEqual(5, ReadBE16(data, 2), "playerId");
            Assert.AreEqual(3, data[4], "kills");
            Assert.AreEqual(1, data[5], "deaths");
            Assert.AreEqual(0, data[6], "isAlive=true writes 0 (inverted: 0=alive, 1=dead)");
            Assert.AreEqual((byte)Team.Gryphon, data[7], "team");
            Assert.AreEqual(10, data[8], "level");
        }

        // --- UpdateHealth (8 bytes) ---

        [Test]
        public void Arena_UpdateHealth_CorrectLayout()
        {
            var ap = MakeArenaPlayer(hp: 500);
            byte[] data = GamePacket.Outgoing.Arena.UpdateHealth(ap).ToArray();
            Assert.AreEqual(8, data.Length);
            AssertPacketHeader(data, PacketOutFunction.UpdateHealth);
            // Bytes 2-5: padding
            // Bytes 6-7: hp in LITTLE-ENDIAN (no FlipBytes in source!)
            Assert.AreEqual(500, BitConverter.ToInt16(data, 6), "hp (LE)");
        }

        // --- UpdateExperience (8 bytes) ---

        [Test]
        public void Arena_UpdateExperience_CorrectLayout()
        {
            var ap = MakeArenaPlayer(kills: 7, deaths: 2);
            byte[] data = GamePacket.Outgoing.Arena.UpdateExperience(ap).ToArray();
            Assert.AreEqual(8, data.Length);
            AssertPacketHeader(data, PacketOutFunction.UpdateExperience);
            Assert.AreEqual(7, ReadBE16(data, 2), "kills");
            // SessionKillExp defaults to 0 (auto-property on uninitialized object)
            Assert.AreEqual(0, ReadBE16(data, 4), "killExp (default 0)");
            Assert.AreEqual(2, ReadBE16(data, 6), "deaths");
        }

        // --- PlayerHit (4 bytes) ---

        [Test]
        public void Arena_PlayerHit_CorrectLayout()
        {
            var ap = MakeArenaPlayer(id: 9);
            byte[] data = GamePacket.Outgoing.Arena.PlayerHit(ap).ToArray();
            AssertPacketHeader(data, PacketOutFunction.PlayerHit);
            Assert.AreEqual(0x00, data[2], "padding");
            Assert.AreEqual(9, data[3], "victimId");
        }

        // --- PlayerDeath (6 bytes) ---

        [Test]
        public void Arena_PlayerDeath_CorrectLayout()
        {
            var victim = MakeArenaPlayer(id: 4);
            var attacker = MakeArenaPlayer(id: 7);
            byte[] data = GamePacket.Outgoing.Arena.PlayerDeath(victim, attacker).ToArray();
            AssertPacketHeader(data, PacketOutFunction.PlayerDeath);
            Assert.AreEqual(0x00, data[2], "padding");
            Assert.AreEqual(4, data[3], "victimId");
            Assert.AreEqual(0x00, data[4], "padding");
            Assert.AreEqual(7, data[5], "attackerId");
        }

        // --- PlayerResurrect (6 bytes) ---

        [Test]
        public void Arena_PlayerResurrect_CorrectLayout()
        {
            var caster = MakeArenaPlayer(id: 2);
            var target = MakeArenaPlayer(id: 8);
            byte[] data = GamePacket.Outgoing.Arena.PlayerResurrect(caster, target).ToArray();
            AssertPacketHeader(data, PacketOutFunction.PlayerResurrect);
            Assert.AreEqual(0x00, data[2], "padding");
            Assert.AreEqual(8, data[3], "targetId");
            Assert.AreEqual(0x00, data[4], "padding");
            Assert.AreEqual(2, data[5], "reviverId");
        }

        // --- ObjectDeath with player (8 bytes) ---

        [Test]
        public void Arena_ObjectDeath_WithPlayer_CorrectLayout()
        {
            short objectId = 12;
            var ap = MakeArenaPlayer(id: 3);
            byte[] data = GamePacket.Outgoing.Arena.ObjectDeath(ap, objectId).ToArray();
            AssertPacketHeader(data, PacketOutFunction.ObjectDeath);
            Assert.AreEqual(objectId, ReadBE16(data, 2), "objectId");
            Assert.AreEqual(0x00, data[4], "padding");
            Assert.AreEqual(3, data[5], "attackerId");
        }
        // --- PlayerJoin arena (25 bytes) ---

        [Test]
        public void Arena_PlayerJoin_CorrectLayout()
        {
            var ap = MakeArenaPlayer(id: 3, name: "Frostbane", level: 12,
                playerClass: Character.PlayerClass.Mystic, team: Team.Gryphon, opLevel: 0, cabalId: 0);
            byte[] data = GamePacket.Outgoing.Arena.PlayerJoin(ap).ToArray();
            // 2 header + 2 playerId + 1 padding + 1 team + 12 name + 1 class + 1 level
            // + 1 opLevel + 1 cabalId + 4 cabalTag + 1 footer = 27
            // (CabalTag returns "" not null when cabalId=0, taking the <4 branch + padding)
            Assert.AreEqual(27, data.Length);
            AssertPacketHeader(data, PacketOutFunction.PlayerJoin);
            // Bytes 2-3: playerId BE
            Assert.AreEqual(3, ReadBE16(data, 2), "playerId");
            // Byte 4: padding
            Assert.AreEqual(0x00, data[4], "padding");
            // Byte 5: team
            Assert.AreEqual((byte)Team.Gryphon, data[5], "team");
            // Bytes 6-17: name (12 bytes, null padded)
            Assert.AreEqual((byte)'F', data[6]);
            Assert.AreEqual((byte)'r', data[7]);
            Assert.AreEqual((byte)'o', data[8]);
            Assert.AreEqual(0x00, data[15], "name null padding");
            // Byte 18: class
            Assert.AreEqual((byte)Character.PlayerClass.Mystic, data[18], "class");
            // Byte 19: level
            Assert.AreEqual(12, data[19], "level");
            // Byte 20: opLevel
            Assert.AreEqual(0, data[20], "opLevel");
            // Byte 21: cabalId
            Assert.AreEqual(0, data[21], "cabalId");
            // Bytes 22-25: cabalTag (4 bytes zero when cabalId=0)
            // Byte 26: footer padding
            Assert.AreEqual(0x00, data[26], "footer");
        }

        // --- CalledGhost relay (12 bytes) ---

        [Test]
        public void Arena_CalledGhost_RelayPreserved()
        {
            byte[] relay = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A };
            var caster = MakeArenaPlayer(id: 1);
            var target = MakeArenaPlayer(id: 2);
            byte[] data = GamePacket.Outgoing.Arena.CalledGhost(caster, target, relay).ToArray();
            Assert.AreEqual(12, data.Length);
            AssertPacketHeader(data, PacketOutFunction.CalledGhost);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(relay[i], data[i + 2], $"Relay byte {i} mismatch");
        }

        // --- TappedAtShrine (6 bytes) ---

        [Test]
        public void Arena_TappedAtShrine_CanRes()
        {
            var ap = MakeArenaPlayer(id: 5);
            byte[] data = GamePacket.Outgoing.Arena.TappedAtShrine(ap, true).ToArray();
            Assert.AreEqual(6, data.Length);
            // NOTE: uses PlayerResurrect opcode, not a dedicated TappedAtShrine opcode
            AssertPacketHeader(data, PacketOutFunction.PlayerResurrect);
            Assert.AreEqual(0x00, data[2], "padding");
            Assert.AreEqual(5, data[3], "playerId");
            Assert.AreEqual(0xFF, data[4]);
            Assert.AreEqual(0xFE, data[5], "canRes=true -> 0xFE");
        }

        [Test]
        public void Arena_TappedAtShrine_CannotRes()
        {
            var ap = MakeArenaPlayer(id: 5);
            byte[] data = GamePacket.Outgoing.Arena.TappedAtShrine(ap, false).ToArray();
            Assert.AreEqual(0xFF, data[5], "canRes=false -> 0xFF");
        }

        // --- SendPlayerId (Player overload, 4 bytes) ---

        [Test]
        public void Player_SendPlayerId_Player()
        {
            var p = MakePlayer(playerId: 42);
            byte[] data = GamePacket.Outgoing.Player.SendPlayerId(p).ToArray();
            Assert.AreEqual(4, data.Length);
            AssertPacketHeader(data, PacketOutFunction.SendPlayerId);
            // NOTE: PlayerId written in LITTLE-ENDIAN (no FlipBytes!)
            Assert.AreEqual(42, BitConverter.ToInt16(data, 2), "playerId (LE)");
        }

        // --- SendPlayerId (ArenaPlayer overload, 4 bytes) ---

        [Test]
        public void Player_SendPlayerId_ArenaPlayer()
        {
            var ap = MakeArenaPlayer(id: 7);
            byte[] data = GamePacket.Outgoing.Player.SendPlayerId(ap).ToArray();
            Assert.AreEqual(4, data.Length);
            AssertPacketHeader(data, PacketOutFunction.SendPlayerId);
            Assert.AreEqual(7, data[2], "arenaPlayerId");
            Assert.AreEqual(0x00, data[3], "padding");
        }

        // --- HeartbeatReply (6 bytes) ---

        [Test]
        public void Player_HeartbeatReply_CorrectLayout()
        {
            var p = MakePlayer();
            // LastHeartbeat setter is private + calls Network.Send — set backing field directly
            typeof(Player).GetField("_lastHeartbeat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(p, (uint)123456789);
            byte[] data = GamePacket.Outgoing.Player.HeartbeatReply(p).ToArray();
            Assert.AreEqual(6, data.Length);
            AssertPacketHeader(data, PacketOutFunction.HeartbeatReply);
            Assert.AreEqual(123456789, (uint)ReadBE32(data, 2), "heartbeat BE");
        }

        // --- SaveSuccess (23 bytes) ---

        [Test]
        public void Player_SaveSuccess_CorrectLayout()
        {
            var p = MakePlayer(username: "Frostbane");
            byte[] data = GamePacket.Outgoing.Player.SaveSuccess(p, 2).ToArray();
            Assert.AreEqual(23, data.Length);
            AssertPacketHeader(data, PacketOutFunction.SaveSuccess);
            // Bytes 2-21: username (20 bytes null padded)
            Assert.AreEqual((byte)'F', data[2]);
            Assert.AreEqual((byte)'r', data[3]);
            Assert.AreEqual(0x00, data[11], "name null padding");
            // Byte 22: slot
            Assert.AreEqual(2, data[22], "slot");
        }

        // --- SwitchedToTable (6 bytes) ---

        [Test]
        public void Player_SwitchedToTable_CorrectLayout()
        {
            var p = MakePlayer(playerId: 10);
            typeof(Player).GetField("_tableId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(p, (byte)3);
            byte[] data = GamePacket.Outgoing.Player.SwitchedToTable(p).ToArray();
            Assert.AreEqual(6, data.Length);
            AssertPacketHeader(data, PacketOutFunction.SwitchedToTable);
            Assert.AreEqual(10, ReadBE16(data, 2), "playerId BE");
            Assert.AreEqual(0x00, data[4], "padding");
            Assert.AreEqual(3, data[5], "tableId");
        }

        // --- PlayerDamage (8 bytes, reuses UpdateHealth opcode) ---

        [Test]
        public void Arena_PlayerDamage_CorrectLayout()
        {
            var victim = MakeArenaPlayer(id: 4, hp: 250);
            var attacker = MakeArenaPlayer(id: 7);
            var dmg = MakeSpellDamage(damage: 30, power: 5);
            byte[] data = GamePacket.Outgoing.Arena.PlayerDamage(victim, attacker, dmg).ToArray();
            Assert.AreEqual(8, data.Length);
            // NOTE: reuses UpdateHealth opcode, not a dedicated PlayerDamage opcode
            AssertPacketHeader(data, PacketOutFunction.UpdateHealth);
            Assert.AreEqual(0x00, data[2], "padding");
            Assert.AreEqual(7, data[3], "attackerId");
            Assert.AreEqual(30, data[4], "damage");
            Assert.AreEqual(5, data[5], "power");
            // HP in LE (same quirk as UpdateHealth)
            Assert.AreEqual(250, BitConverter.ToInt16(data, 6), "hp (LE)");
        }

        [Test]
        public void Arena_PlayerDamage_NullAttacker()
        {
            var victim = MakeArenaPlayer(id: 4, hp: 100);
            var dmg = MakeSpellDamage(damage: 10, power: 2);
            byte[] data = GamePacket.Outgoing.Arena.PlayerDamage(victim, null, dmg).ToArray();
            Assert.AreEqual(0, data[3], "null attacker -> 0");
        }

        // --- CastTargetedEx (30 bytes) ---

        [Test]
        public void Arena_CastTargetedEx_CorrectLayout()
        {
            var target = MakeArenaPlayer(id: 5);
            var source = MakeArenaPlayer(id: 2);
            var spell = MakeSpell(id: 42, range: 800);
            byte[] data = GamePacket.Outgoing.Arena.CastTargetedEx(target, source, spell).ToArray();
            Assert.AreEqual(30, data.Length);
            AssertPacketHeader(data, PacketOutFunction.CastTargeted);
            Assert.AreEqual(42, ReadBE16(data, 2), "spellId");
            Assert.AreEqual(800, ReadBE16(data, 4), "range");
            Assert.AreEqual(2, ReadBE16(data, 6), "sourceId");
            Assert.AreEqual(5, ReadBE16(data, 8), "targetId");
            // Bytes 10-29: 20 bytes padding
            for (int i = 10; i < 30; i++)
                Assert.AreEqual(0x00, data[i], $"padding byte {i}");
        }

        [Test]
        public void Arena_CastTargetedEx_NullSource()
        {
            var target = MakeArenaPlayer(id: 5);
            var spell = MakeSpell(id: 10, range: 300);
            byte[] data = GamePacket.Outgoing.Arena.CastTargetedEx(target, null, spell).ToArray();
            Assert.AreEqual(0, data[6], "null source hi");
            Assert.AreEqual(0, data[7], "null source lo");
        }

        // --- SaveError (44 bytes) ---

        [Test]
        public void Player_SaveError_CorrectLayout()
        {
            var p = MakePlayer(username: "TestPlayer");
            p.ActiveCharacter = MakeCharacter(name: "Frostbane");
            byte[] data = GamePacket.Outgoing.Player.SaveError(p, 1).ToArray();
            AssertPacketHeader(data, PacketOutFunction.SaveError);
            // Bytes 2-21: username (20 bytes)
            Assert.AreEqual((byte)'T', data[2]);
            // Byte 22: slot
            Assert.AreEqual(1, data[22], "slot");
            // Bytes 23-25: padding
            // Bytes 26-45: charName (20 bytes)
            Assert.AreEqual((byte)'F', data[26]);
        }

        // --- World.PlayerLeave (4 bytes) ---

        [Test]
        public void World_PlayerLeave_CorrectLayout()
        {
            var p = MakePlayer(playerId: 15);
            byte[] data = GamePacket.Outgoing.World.PlayerLeave(p).ToArray();
            Assert.AreEqual(4, data.Length);
            AssertPacketHeader(data, PacketOutFunction.PlayerLeave);
            Assert.AreEqual(15, ReadBE16(data, 2), "playerId BE");
        }

        // ================================================================
        // Batch 3: Shrine/Pool/Trigger/Table/Arena stubs
        // ================================================================

        // --- BiasedShrine (10 bytes) ---

        [Test]
        public void Arena_BiasedShrine_CorrectLayout()
        {
            var ap = MakeArenaPlayer(id: 2);
            var shrine = MakeShrine(id: 1, team: Team.Dragon, currentBias: 75, power: 100);
            byte biasAmount = 25;
            byte[] data = GamePacket.Outgoing.Arena.BiasedShrine(ap, shrine, biasAmount).ToArray();
            Assert.AreEqual(10, data.Length);
            AssertPacketHeader(data, PacketOutFunction.BiasedShrine);
            Assert.AreEqual(1, data[2], "shrineId");
            Assert.AreEqual((byte)Team.Dragon, data[3], "team");
            Assert.AreEqual(75, data[4], "currentBias");
            Assert.AreEqual(0x00, data[5], "padding");
            Assert.AreEqual(25, data[6], "biasAmount");
            Assert.AreEqual(0x00, data[7], "padding");
            // Bytes 8-9: ArenaPlayerId BE
            Assert.AreEqual(2, ReadBE16(data, 8), "playerId");
        }

        // --- BiasedPool (10 bytes) ---

        [Test]
        public void Arena_BiasedPool_CorrectLayout()
        {
            var ap = MakeArenaPlayer(id: 4);
            var pool = MakePool(id: 3, team: Team.Gryphon, currentBias: 40, power: 50);
            byte biasAmount = 10;
            byte[] data = GamePacket.Outgoing.Arena.BiasedPool(ap, pool, biasAmount).ToArray();
            Assert.AreEqual(10, data.Length);
            AssertPacketHeader(data, PacketOutFunction.BiasedPool);
            Assert.AreEqual(3, data[2], "poolId");
            Assert.AreEqual((byte)Team.Gryphon, data[3], "team");
            Assert.AreEqual(40, data[4], "currentBias");
            Assert.AreEqual(0x00, data[5], "padding");
            Assert.AreEqual(10, data[6], "biasAmount");
            Assert.AreEqual(0x00, data[7], "padding");
            Assert.AreEqual(4, ReadBE16(data, 8), "playerId");
        }

        // --- ActivatedTrigger (7 bytes) ---

        [Test]
        public void Arena_ActivatedTrigger_Inactive()
        {
            var trigger = MakeTrigger(id: 5, state: TriggerState.Inactive);
            byte[] data = GamePacket.Outgoing.Arena.ActivatedTrigger(trigger).ToArray();
            Assert.AreEqual(7, data.Length);
            AssertPacketHeader(data, PacketOutFunction.ActivatedTrigger);
            Assert.AreEqual(0x00, data[2], "padding");
            Assert.AreEqual(0x00, data[3], "padding");
            Assert.AreEqual(0x00, data[4], "padding");
            Assert.AreEqual(5, data[5], "triggerId");
            Assert.AreEqual((byte)TriggerState.Inactive, data[6], "state");
        }

        [Test]
        public void Arena_ActivatedTrigger_Active()
        {
            var trigger = MakeTrigger(id: 12, state: TriggerState.Active);
            byte[] data = GamePacket.Outgoing.Arena.ActivatedTrigger(trigger).ToArray();
            Assert.AreEqual(12, data[5], "triggerId");
            Assert.AreEqual((byte)TriggerState.Active, data[6], "state");
        }

        // --- TableDeleted (3 bytes) ---

        [Test]
        public void World_TableDeleted_CorrectLayout()
        {
            var table = MakeTable(id: 7);
            byte[] data = GamePacket.Outgoing.World.TableDeleted(table).ToArray();
            Assert.AreEqual(3, data.Length);
            AssertPacketHeader(data, PacketOutFunction.TableDeleted);
            Assert.AreEqual(7, data[2], "tableId");
        }

        // --- ArenaDeleted (3 bytes) ---

        [Test]
        public void World_ArenaDeleted_CorrectLayout()
        {
            var arena = MakeArena(id: 3);
            byte[] data = GamePacket.Outgoing.World.ArenaDeleted(arena).ToArray();
            Assert.AreEqual(3, data.Length);
            AssertPacketHeader(data, PacketOutFunction.ArenaDeleted);
            Assert.AreEqual(3, data[2], "arenaId");
        }
    }
}
