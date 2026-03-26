using System;
using System.Collections.Generic;

namespace SpellServer.Commands
{
    /// <summary>
    /// Registers and dispatches chat commands.
    /// Add new commands by calling Register() or just drop a new IChatCommand class
    /// into the Commands/ folder and register it in Initialize().
    /// </summary>
    public static class CommandRegistry
    {
        private static readonly Dictionary<string, IChatCommand> _commands = new Dictionary<string, IChatCommand>(StringComparer.OrdinalIgnoreCase);

        public static void Register(IChatCommand command)
        {
            _commands[command.Name] = command;
            if (command.Aliases != null)
            {
                foreach (var alias in command.Aliases)
                    _commands[alias] = command;
            }
        }

        /// <summary>Try to dispatch a command. Returns true if handled.</summary>
        public static bool TryExecute(Player player, ChatCommand cmd)
        {
            IChatCommand handler;
            if (!_commands.TryGetValue(cmd.Command, out handler))
                return false;

            // Check admin level
            if ((int)player.Admin < handler.MinAdminLevel)
            {
                World.SendSystemMessage(player, "[System] You don't have permission to use that command.");
                return true; // consumed but denied
            }

            // Check arena requirement
            if (handler.RequiresArena && !player.IsInArena)
            {
                World.SendSystemMessage(player, Resources.Strings_Commands.General_NotInArena);
                return true;
            }

            handler.Execute(player, cmd);
            return true;
        }

        /// <summary>Register all commands. Called once at startup.</summary>
        public static void Initialize()
        {
            Register(new YankCommand());
            Register(new LocCommand());
            Register(new LeyCommand());
            Register(new PerfCommand());
            Register(new TpCommand());
        }
    }
}
