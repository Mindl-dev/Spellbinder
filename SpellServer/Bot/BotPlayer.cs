using System;
using System.Runtime.Serialization;
using Helper;

namespace SpellServer.Bot
{
    /// <summary>
    /// Factory for creating server-side bot players.
    /// Bots are real ArenaPlayers that go through the normal game loop.
    /// </summary>
    public static class BotPlayer
    {
        private static int _nextBotAccountId = -1000;

        /// <summary>
        /// Create a bot and add it to the arena.
        /// Returns the ArenaPlayer, or null if creation failed.
        /// </summary>
        public static ArenaPlayer Create(Arena arena, Team team, string name, Character.PlayerClass botClass, byte level)
        {
            // Create Player without calling the TcpClient constructor
            var player = (Player)FormatterServices.GetUninitializedObject(typeof(Player));
            player.IsBot = true;
            player.TcpClient = null;
            player.PlayerId = (short)(900 + arena.ArenaPlayers.Count);
            player.AccountId = _nextBotAccountId--;
            player.Username = name;
            player.Serial = "BOT";
            player.Disconnect = false;
            player.DisconnectReason = "";
            player.Flags = PlayerFlag.None;
            player.Admin = AdminLevel.None;
            player.ActiveTeam = team;
            player.Ping = 0;
            player.PingInitialized = true;
            player.LastArenaId = arena.ArenaId;

            // Create Character without DB
            var character = (Character)FormatterServices.GetUninitializedObject(typeof(Character));
            // Set readonly CharacterId via reflection
            var charIdField = typeof(Character).GetField("CharacterId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            charIdField?.SetValue(character, _nextBotAccountId);
            character.Name = name;
            character.Class = botClass;
            character.Level = level;
            character.OpLevel = 0;
            character.CabalId = 0;
            character.Constitution = 60;

            // Initialize spell trees (empty — cheat check is disabled)
            var treesField = typeof(Character).GetField("SpellTrees", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            treesField?.SetValue(character, new SpellTreeCollection());

            // Initialize statistics (use uninitialized object — all Int64 fields default to 0)
            var stats = (Statistics.StatisticSheet)FormatterServices.GetUninitializedObject(typeof(Statistics.StatisticSheet));
            character.Statistics = stats;

            player.ActiveCharacter = character;

            // Create ArenaPlayer through normal constructor
            // IsBot guard prevents all Network.Send calls inside
            var arenaPlayer = new ArenaPlayer(player, arena);

            if (arenaPlayer.ArenaPlayerId == 0)
            {
                Program.Log($"[Bot] Failed to create bot {name} — no arena slot", System.Drawing.Color.Red);
                return null;
            }

            // Broadcast PlayerJoin to real clients so they see the bot
            Network.SendTo(arena, GamePacket.Outgoing.Arena.ArenaPlayerEnterLarge(arenaPlayer, null), Network.SendToType.Arena);

            // Send initial position so the client renders the bot model
            BroadcastPosition(arenaPlayer);

            // Register with BotManager for AI ticking
            BotManager.RegisterBot(arena, arenaPlayer);

            Program.Log($"[Bot] Created {name} ({botClass} Lv{level}) on {team}, ArenaPlayerId={arenaPlayer.ArenaPlayerId}",
                System.Drawing.Color.Green);

            return arenaPlayer;
        }

        /// <summary>
        /// Encode the bot's current position into a 12-byte PlayerMoveState payload
        /// and broadcast it to all real clients.
        /// </summary>
        public static void BroadcastPosition(ArenaPlayer bot)
        {
            byte[] payload = EncodePosition(bot.Location, 0, 0);
            Network.SendToArena(bot,
                GamePacket.Outgoing.Arena.PlayerMoveState(bot, payload), true);
        }

        /// <summary>
        /// Encode a world position + heading into the 12-byte PlayerMoveState wire format.
        /// Matches the Python bot's _send_position() encoding.
        /// </summary>
        public static byte[] EncodePosition(SharpDX.Vector3 location, int heading4096, byte speedScalar)
        {
            byte[] payload = new byte[12];
            int x = (int)location.X & 0x1FFF;
            int y = (int)location.Y & 0x1FFF;
            int z = (int)location.Z;

            // [0-1] direction (12-bit angle 0-4095)
            int dir = heading4096 & 0xFFF;
            payload[0] = (byte)(dir >> 8);
            payload[1] = (byte)(dir & 0xFF);

            // [2-3] Z (11-bit + sign) | speed scalar (upper 4 bits)
            int zVal = Math.Abs(z) & 0x7FF;
            if (z < 0) zVal |= 0x800;
            int zEncoded = (speedScalar << 12) | zVal;
            payload[2] = (byte)(zEncoded >> 8);
            payload[3] = (byte)(zEncoded & 0xFF);

            // [4-5] X (13-bit)
            payload[4] = (byte)(x >> 8);
            payload[5] = (byte)(x & 0xFF);

            // [6-7] Y (13-bit) | flags in upper 3 bits
            payload[6] = (byte)(y >> 8);
            payload[7] = (byte)(y & 0xFF);

            // [8-9] heading (16-bit), [10-11] zero
            int heading16 = (heading4096 * 65536 / 4096) & 0xFFFF;
            payload[8] = (byte)(heading16 >> 8);
            payload[9] = (byte)(heading16 & 0xFF);
            payload[10] = 0;
            payload[11] = 0;

            return payload;
        }
    }
}
