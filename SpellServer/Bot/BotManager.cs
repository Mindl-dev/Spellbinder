using System.Collections.Generic;

namespace SpellServer.Bot
{
    /// <summary>
    /// Manages all bot brains per arena. Called from the arena tick loop.
    /// </summary>
    public static class BotManager
    {
        private static readonly Dictionary<byte, List<BotBrain>> _bots = new Dictionary<byte, List<BotBrain>>();

        public static void RegisterBot(Arena arena, ArenaPlayer botPlayer)
        {
            if (!_bots.ContainsKey(arena.ArenaId))
                _bots[arena.ArenaId] = new List<BotBrain>();

            _bots[arena.ArenaId].Add(new BotBrain(botPlayer, arena));
        }

        public static void ProcessBots(Arena arena)
        {
            List<BotBrain> brains;
            if (!_bots.TryGetValue(arena.ArenaId, out brains)) return;

            for (int i = 0; i < brains.Count; i++)
            {
                brains[i].Think();
            }
        }

        public static void RemoveBots(Arena arena)
        {
            if (_bots.ContainsKey(arena.ArenaId))
            {
                _bots[arena.ArenaId].Clear();
                _bots.Remove(arena.ArenaId);
            }
        }
    }
}
