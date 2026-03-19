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
    }
}
