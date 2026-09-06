using System;
using System.Collections.Generic;
using System.Linq;
using Helper;

namespace SpellServer
{
    /// <summary>
    /// Graph of earthblood nodes and shrines connected by ley lines.
    /// Built from WORLD.DAT link data at arena load time.
    ///
    /// Node IDs: 0-99 = earthblood pools, 100+ = shrines (100=dragon, 101=phoenix, 102=griffin)
    /// </summary>
    public class LeyGraph
    {
        public const int ShrineIdOffset = 100;

        private readonly Dictionary<int, LeyNode> _nodes = new Dictionary<int, LeyNode>();

        public IReadOnlyDictionary<int, LeyNode> Nodes => _nodes;

        /// <summary>Build the graph from loaded pools and shrines.</summary>
        public static LeyGraph Build(PoolCollection pools, Shrine dragonShrine, Shrine phoenixShrine, Shrine griffinShrine)
        {
            var graph = new LeyGraph();

            // Add pool nodes
            foreach (Pool pool in pools)
            {
                if (pool.Power <= 0) continue; // skip commented-out nodes (earthblood04)

                var node = new LeyNode
                {
                    Id = pool.PoolId,
                    Type = LeyNodeType.Earthblood,
                    Power = pool.Power,
                    Team = pool.Team,
                    CurrentBias = pool.CurrentBias,
                    X = pool.X,
                    Y = pool.Y,
                    Z = pool.Z,
                    Pool = pool
                };
                graph._nodes[pool.PoolId] = node;
            }

            // Add shrine nodes
            if (dragonShrine != null)
                graph.AddShrine(dragonShrine, Team.Dragon);
            if (phoenixShrine != null)
                graph.AddShrine(phoenixShrine, Team.Pheonix);
            if (griffinShrine != null)
                graph.AddShrine(griffinShrine, Team.Gryphon);

            // Wire up edges from pool links
            foreach (Pool pool in pools)
            {
                if (pool.Power <= 0) continue;
                if (!graph._nodes.ContainsKey(pool.PoolId)) continue;

                var node = graph._nodes[pool.PoolId];
                foreach (short linkId in pool.Links)
                {
                    if (graph._nodes.ContainsKey(linkId))
                        node.AddNeighbor(graph._nodes[linkId]);
                }
            }

            // Wire up edges from shrine links
            foreach (var shrine in new[] { dragonShrine, phoenixShrine, griffinShrine })
            {
                if (shrine == null) continue;
                int shrineNodeId = shrine.ShrineId + ShrineIdOffset;
                if (!graph._nodes.ContainsKey(shrineNodeId)) continue;

                var shrineNode = graph._nodes[shrineNodeId];
                foreach (short linkId in shrine.Links)
                {
                    if (graph._nodes.ContainsKey(linkId))
                        shrineNode.AddNeighbor(graph._nodes[linkId]);
                }
            }

            return graph;
        }

        private void AddShrine(Shrine shrine, Team team)
        {
            int nodeId = shrine.ShrineId + ShrineIdOffset;
            var node = new LeyNode
            {
                Id = nodeId,
                Type = LeyNodeType.Shrine,
                Power = shrine.Power,
                Team = team,
                CurrentBias = shrine.CurrentBias,
                X = shrine.X,
                Y = shrine.Y,
                Z = shrine.Z,
                Shrine = shrine
            };
            _nodes[nodeId] = node;
        }

        /// <summary>Get all earthblood nodes connected to a team's shrine via owned nodes.</summary>
        public List<LeyNode> GetTeamNetwork(Team team)
        {
            // Find the shrine for this team
            var shrine = _nodes.Values.FirstOrDefault(n => n.Type == LeyNodeType.Shrine && n.Team == team);
            if (shrine == null) return new List<LeyNode>();

            // BFS from shrine through nodes owned by this team (or neutral)
            var visited = new HashSet<int>();
            var queue = new Queue<LeyNode>();
            var network = new List<LeyNode>();

            visited.Add(shrine.Id);
            queue.Enqueue(shrine);
            network.Add(shrine);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in current.Neighbors)
                {
                    if (visited.Contains(neighbor.Id)) continue;
                    visited.Add(neighbor.Id);

                    // Only traverse through team-owned nodes — network must be contiguous
                    if (neighbor.Team == team)
                    {
                        queue.Enqueue(neighbor);
                        network.Add(neighbor);
                    }
                }
            }

            return network;
        }

        /// <summary>Is an earthblood node connected to its team's shrine through team-owned nodes only?</summary>
        public bool IsConnectedToShrine(int poolId, Team team)
        {
            var shrine = _nodes.Values.FirstOrDefault(n => n.Type == LeyNodeType.Shrine && n.Team == team);
            if (shrine == null) return false;

            var visited = new HashSet<int>();
            var queue = new Queue<LeyNode>();

            visited.Add(shrine.Id);
            queue.Enqueue(shrine);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Id == poolId) return true;

                foreach (var neighbor in current.Neighbors)
                {
                    if (visited.Contains(neighbor.Id)) continue;
                    visited.Add(neighbor.Id);

                    // Only traverse through team-owned nodes
                    if (neighbor.Team == team)
                        queue.Enqueue(neighbor);
                }
            }

            return false;
        }

        /// <summary>
        /// Is a node adjacent (one hop) to a team-owned node or the team's shrine?
        /// Used for front-line biasing — you can bias a node if it neighbors your network.
        /// </summary>
        public bool IsAdjacentToTeamNetwork(int nodeId, Team team)
        {
            if (!_nodes.ContainsKey(nodeId)) return false;
            var node = _nodes[nodeId];

            foreach (var neighbor in node.Neighbors)
            {
                if (neighbor.Team == team) return true;
                if (neighbor.Type == LeyNodeType.Shrine && neighbor.Team == team) return true;
            }

            return false;
        }

        /// <summary>
        /// Classify how a team can bias a target node.
        ///   Connected: node is adjacent to team's owned network → normal speed
        ///   Disconnected: node exists but no adjacent team nodes → slow (back-hack)
        ///   Blocked: target is a shrine and team has no connected path → cannot bias
        /// </summary>
        public BiasEligibility GetBiasEligibility(int nodeId, Team team)
        {
            if (!_nodes.ContainsKey(nodeId))
                return BiasEligibility.Blocked;

            var node = _nodes[nodeId];

            // Nexus: own team can always repair, enemy requires adjacent owned node
            if (node.Type == LeyNodeType.Shrine)
            {
                if (node.Team == team)
                    return BiasEligibility.Connected; // repairing own nexus
                if (IsAdjacentToTeamNetwork(nodeId, team))
                    return BiasEligibility.Connected;
                return BiasEligibility.Blocked;
            }

            // Earthblood node: check if adjacent to team network
            if (IsAdjacentToTeamNetwork(nodeId, team))
                return BiasEligibility.Connected;

            // Not adjacent — back-hack (much slower)
            return BiasEligibility.Disconnected;
        }

        /// <summary>World distance from a position to a team's shrine. Returns float.MaxValue if shrine doesn't exist.</summary>
        public float DistanceToShrine(int x, int y, int z, Team team)
        {
            var shrine = _nodes.Values.FirstOrDefault(n => n.Type == LeyNodeType.Shrine && n.Team == team);
            if (shrine == null) return float.MaxValue;
            float dx = shrine.X - x;
            float dy = shrine.Y - y;
            float dz = shrine.Z - z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>Total power of all earthblood nodes in a team's connected network.</summary>
        public int GetTeamPower(Team team)
        {
            return GetTeamNetwork(team)
                .Where(n => n.Type == LeyNodeType.Earthblood && n.Team == team)
                .Sum(n => n.Power);
        }

        /// <summary>Find the nearest earthblood node to a world position.</summary>
        public LeyNode GetNearestNode(int x, int y, int z)
        {
            LeyNode nearest = null;
            float minDist = float.MaxValue;

            foreach (var node in _nodes.Values)
            {
                if (node.Type != LeyNodeType.Earthblood) continue;
                float dx = node.X - x;
                float dy = node.Y - y;
                float dz = node.Z - z;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = node;
                }
            }

            return nearest;
        }

        /// <summary>Find the nearest node owned by a specific team.</summary>
        public LeyNode GetNearestTeamNode(int x, int y, int z, Team team)
        {
            LeyNode nearest = null;
            float minDist = float.MaxValue;

            foreach (var node in _nodes.Values)
            {
                if (node.Type != LeyNodeType.Earthblood) continue;
                if (node.Team != team) continue;
                float dx = node.X - x;
                float dy = node.Y - y;
                float dz = node.Z - z;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = node;
                }
            }

            return nearest;
        }

        /// <summary>Distance from a point to the nearest node of a team.</summary>
        public float DistanceToNearestTeamNode(int x, int y, int z, Team team)
        {
            var node = GetNearestTeamNode(x, y, z, team);
            if (node == null) return float.MaxValue;
            float dx = node.X - x;
            float dy = node.Y - y;
            float dz = node.Z - z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>Sync node team/bias state from the live Pool/Shrine objects.</summary>
        public void SyncFromGameState()
        {
            foreach (var node in _nodes.Values)
            {
                if (node.Pool != null)
                {
                    node.Team = node.Pool.Team;
                    node.CurrentBias = node.Pool.CurrentBias;
                }
                if (node.Shrine != null)
                {
                    node.CurrentBias = node.Shrine.CurrentBias;
                }
            }
        }
    }

    public enum LeyNodeType
    {
        Earthblood,
        Shrine
    }

    public enum BiasEligibility
    {
        Connected,    // Adjacent to team network — normal bias speed
        Disconnected, // No adjacent team nodes — back-hack (slow)
        Blocked       // Cannot bias (nexus requires connected path)
    }

    public class LeyNode
    {
        public int Id;
        public LeyNodeType Type;
        public Int16 Power;
        public Team Team;
        public Int16 CurrentBias;
        public int X;
        public int Y;
        public int Z;

        /// <summary>Reference to the live Pool object (null for shrines).</summary>
        public Pool Pool;
        /// <summary>Reference to the live Shrine object (null for earthblood).</summary>
        public Shrine Shrine;

        private readonly List<LeyNode> _neighbors = new List<LeyNode>();
        public IReadOnlyList<LeyNode> Neighbors => _neighbors;

        public void AddNeighbor(LeyNode other)
        {
            if (!_neighbors.Contains(other))
                _neighbors.Add(other);
            if (!other._neighbors.Contains(this))
                other._neighbors.Add(this);
        }
    }
}
