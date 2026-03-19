using System;
using System.Reflection;
using System.Runtime.Serialization;
using Helper;
using Helper.Network;

namespace SpellServer.Tests
{
    /// <summary>Test utilities for creating stub game objects without heavy constructors.</summary>
    internal static class TestHelpers
    {
        /// <summary>Create an ArenaPlayer without calling its constructor (which requires Arena + lock).
        /// Sets public fields directly.</summary>
        internal static ArenaPlayer MakeArenaPlayer(
            byte id = 1,
            string name = "TestMage",
            byte level = 5,
            Character.PlayerClass playerClass = Character.PlayerClass.Magician,
            Team team = Team.Dragon,
            byte opLevel = 0,
            int cabalId = 0,
            string cabalTag = null,
            short hp = 100,
            short kills = 0,
            short deaths = 0)
        {
            var ap = (ArenaPlayer)FormatterServices.GetUninitializedObject(typeof(ArenaPlayer));
            ap.ArenaPlayerId = id;
            ap.ActiveTeam = team;
            // CurrentHp setter clamps to MaxHp — set private fields directly
            var maxHpField = typeof(ArenaPlayer).GetField("_maxHp", BindingFlags.NonPublic | BindingFlags.Instance);
            var currentHpField = typeof(ArenaPlayer).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance);
            maxHpField.SetValue(ap, (short)32767);
            currentHpField.SetValue(ap, hp);
            ap.KillCount = kills;
            ap.DeathCount = deaths;
            ap.RaiseCount = 0;
            ap.ActiveCharacter = MakeCharacter(name, level, playerClass, opLevel, cabalId, cabalTag);
            return ap;
        }

        /// <summary>Create a Player without calling its constructor (which starts TCP threads).</summary>
        internal static Player MakePlayer(string username = "TestPlayer", short playerId = 1)
        {
            var p = (Player)FormatterServices.GetUninitializedObject(typeof(Player));
            p.Username = username;
            p.PlayerId = playerId;
            return p;
        }

        internal static Character MakeCharacter(
            string name = "TestMage",
            byte level = 5,
            Character.PlayerClass playerClass = Character.PlayerClass.Magician,
            byte opLevel = 0,
            int cabalId = 0,
            string cabalTag = null)
        {
            var c = (Character)FormatterServices.GetUninitializedObject(typeof(Character));
            c.Name = name;
            c.Level = level;
            c.Class = playerClass;
            c.OpLevel = opLevel;
            c.CabalId = cabalId;
            // CabalTag is a computed property (reads from CabalManager singleton)
            return c;
        }

        internal static Shrine MakeShrine(byte id = 1, Team team = Team.Dragon, short currentBias = 50, short power = 100)
        {
            var s = (Shrine)FormatterServices.GetUninitializedObject(typeof(Shrine));
            s.ShrineId = id;
            s.Team = team;
            s.Power = power;
            s.MaxBias = 100;
            // CurrentBias is a property with private backing field
            var biasField = typeof(Shrine).GetField("_currentBias", BindingFlags.NonPublic | BindingFlags.Instance);
            biasField.SetValue(s, currentBias);
            return s;
        }

        internal static Pool MakePool(byte id = 1, Team team = Team.Neutral, short currentBias = 0, short power = 50)
        {
            return new Pool(id, power, 100) { Team = team, CurrentBias = currentBias };
        }

        internal static Trigger MakeTrigger(short id = 1, TriggerState state = TriggerState.Inactive)
        {
            return new Trigger { TriggerId = id, CurrentState = state };
        }

        internal static Table MakeTable(short id = 1, string name = "Test Table", string founder = "TestPlayer")
        {
            var t = (Table)FormatterServices.GetUninitializedObject(typeof(Table));
            // Table has readonly fields — use reflection
            SetReadonly(t, "TableId", id);
            SetReadonly(t, "Name", name);
            SetReadonly(t, "Founder", founder);
            SetReadonly(t, "Type", TableType.Public);
            return t;
        }

        internal static Arena MakeArena(byte id = 1, string gameName = "Test Arena", string founder = "TestPlayer",
            string gridName = "Grid00", string shortName = "Test")
        {
            var a = (Arena)FormatterServices.GetUninitializedObject(typeof(Arena));
            a.ArenaId = id;
            a.GameName = gameName;
            a.Founder = founder;
            a.ShortGameName = shortName;
            // Grid needs to exist for Grid.Name
            var g = (Grid)FormatterServices.GetUninitializedObject(typeof(Grid));
            g.Name = gridName;
            a.Grid = g;
            return a;
        }

        private static void SetReadonly(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(obj, value);
        }

        internal static Spell MakeSpell(short id = 1, short range = 500)
        {
            var s = (Spell)FormatterServices.GetUninitializedObject(typeof(Spell));
            s.Id = id;
            s.Range = range;
            return s;
        }

        internal static SpellDamage MakeSpellDamage(short damage = 50, short power = 10)
        {
            var sd = (SpellDamage)FormatterServices.GetUninitializedObject(typeof(SpellDamage));
            var dmgField = typeof(SpellDamage).GetField("_damage", BindingFlags.NonPublic | BindingFlags.Instance);
            var pwrField = typeof(SpellDamage).GetField("_power", BindingFlags.NonPublic | BindingFlags.Instance);
            dmgField.SetValue(sd, damage);
            pwrField.SetValue(sd, power);
            return sd;
        }

        /// <summary>Assert packet starts with 0x00 + expected function ID.</summary>
        internal static void AssertPacketHeader(byte[] data, PacketOutFunction func)
        {
            if (data.Length < 2)
                throw new Exception($"Packet too short ({data.Length} bytes)");
            if (data[0] != 0x00)
                throw new Exception($"Expected leading 0x00, got 0x{data[0]:X2}");
            if (data[1] != (byte)func)
                throw new Exception($"Expected func 0x{(byte)func:X2} ({func}), got 0x{data[1]:X2}");
        }

        /// <summary>Read a big-endian Int16 from byte array.</summary>
        internal static short ReadBE16(byte[] data, int offset)
        {
            return NetHelper.FlipBytes(BitConverter.ToInt16(data, offset));
        }

        /// <summary>Read a big-endian UInt16 from byte array.</summary>
        internal static ushort ReadBE16U(byte[] data, int offset)
        {
            return NetHelper.FlipBytes(BitConverter.ToUInt16(data, offset));
        }

        /// <summary>Read a big-endian Int32 from byte array.</summary>
        internal static int ReadBE32(byte[] data, int offset)
        {
            return NetHelper.FlipBytes(BitConverter.ToInt32(data, offset));
        }
    }
}
