using System;

namespace SpellServer.Commands
{
    public interface IChatCommand
    {
        /// <summary>Command name (what comes after !)</summary>
        string Name { get; }

        /// <summary>Alternative names for the same command</summary>
        string[] Aliases { get; }

        /// <summary>Minimum admin level required (0 = everyone)</summary>
        int MinAdminLevel { get; }

        /// <summary>Must be in an arena to use this command</summary>
        bool RequiresArena { get; }

        /// <summary>Execute the command</summary>
        void Execute(Player player, ChatCommand cmd);
    }
}
