using System;
using System.Linq;
using System.Text;
using Helper;

namespace SpellServer.Commands
{
    public class LeyCommand : IChatCommand
    {
        public string Name { get { return "ley"; } }
        public string[] Aliases { get { return new[] { "graph", "nodes" }; } }
        public int MinAdminLevel { get { return 0; } }
        public bool RequiresArena { get { return true; } }

        public void Execute(Player player, ChatCommand cmd)
        {
            var arena = player.ActiveArena;
            if (arena.LeyGraph == null)
            {
                World.SendSystemMessage(player, "[Ley] No ley graph loaded.");
                return;
            }

            var graph = arena.LeyGraph;
            var myTeam = player.ActiveArenaPlayer.ActiveTeam;

            // If argument is "near", show nearest node to player
            if (cmd.Arguments.Count > 0 && cmd.Arguments[0].Equals("near", StringComparison.OrdinalIgnoreCase))
            {
                var pos = player.ActiveArenaPlayer.Location;
                int px = (int)pos.X, py = (int)pos.Y, pz = (int)pos.Z;
                var nearest = graph.GetNearestNode(px, py, pz);
                if (nearest == null)
                {
                    World.SendSystemMessage(player, "[Ley] No nodes found.");
                    return;
                }
                float dx = nearest.X - px, dy = nearest.Y - py, dz = nearest.Z - pz;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                var neighbors = string.Join(", ", nearest.Neighbors.Select(n =>
                    n.Type == LeyNodeType.Shrine ? $"S{n.Id - LeyGraph.ShrineIdOffset}({n.Team})" : $"{n.Id}({n.Team})"));
                var eligibility = graph.GetBiasEligibility(nearest.Id, myTeam);
                World.SendSystemMessage(player,
                    String.Format("[Ley] Nearest: pool {0} team={1} bias={2} power={3} dist={4:F0} elig={5}",
                        nearest.Id, nearest.Team, nearest.CurrentBias, nearest.Power, dist, eligibility));
                World.SendSystemMessage(player,
                    String.Format("[Ley] Links: [{0}]", neighbors));
                World.SendSystemMessage(player,
                    String.Format("[Ley] Node pos: {0},{1},{2}", nearest.X, nearest.Y, nearest.Z));
                return;
            }

            // Default: dump summary per team
            foreach (var team in new[] { Team.Dragon, Team.Pheonix, Team.Gryphon })
            {
                var network = graph.GetTeamNetwork(team);
                int nodeCount = network.Count(n => n.Type == LeyNodeType.Earthblood);
                int totalPower = network.Where(n => n.Type == LeyNodeType.Earthblood && n.Team == team).Sum(n => n.Power);
                var shrine = graph.Nodes.Values.FirstOrDefault(n => n.Type == LeyNodeType.Shrine && n.Team == team);
                string shrineLinks = shrine != null
                    ? string.Join(",", shrine.Neighbors.Select(n => n.Id.ToString()))
                    : "none";
                World.SendSystemMessage(player,
                    String.Format("[Ley] {0}: {1} nodes, power={2}, shrine links=[{3}]",
                        team, nodeCount, totalPower, shrineLinks));
            }

            // Show total node count
            int totalNodes = graph.Nodes.Values.Count(n => n.Type == LeyNodeType.Earthblood);
            World.SendSystemMessage(player,
                String.Format("[Ley] Total: {0} earthblood nodes, {1} graph nodes",
                    totalNodes, graph.Nodes.Count));
        }
    }
}
