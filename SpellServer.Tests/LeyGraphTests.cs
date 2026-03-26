using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Helper;
using SpellServer;

namespace SpellServer.Tests
{
    [TestFixture]
    public class LeyGraphTests
    {
        private static string FindWorldDat(string gridId)
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            for (int i = 0; i < 6; i++)
            {
                string candidate = Path.Combine(dir, "Content", "Grids", gridId, "WORLD.DAT");
                if (File.Exists(candidate)) return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        private static LeyGraph BuildGrid00Graph()
        {
            string world = FindWorldDat("grid00");
            if (world == null) return null;

            string nifsDir = Path.Combine(Path.GetDirectoryName(world), "NIFS", "NIFS.DAT");
            string nifs = File.Exists(nifsDir) ? nifsDir : null;
            var pools = Grid.LoadPools(world, nifs);

            // Load shrines the same way Grid does
            var links = new ListCollection<Int16>();

            short link1, link2, link3;

            link1 = NativeMethods.GetPrivateProfileInt16("shrine00", "link1", world);
            link2 = NativeMethods.GetPrivateProfileInt16("shrine00", "link2", world);
            link3 = NativeMethods.GetPrivateProfileInt16("shrine00", "link3", world);
            links.Clear(); links.Add(link1); links.Add(link2); links.Add(link3);
            string align0 = NativeMethods.GetPrivateProfileString("shrine00", "alignment", world);
            var shrine0 = new Shrine(
                align0 == "chaos" ? Team.Dragon : align0 == "balance" ? Team.Pheonix : Team.Gryphon,
                0,
                NativeMethods.GetPrivateProfileInt16("shrine00", "power", world),
                NativeMethods.GetPrivateProfileInt16("shrine00", "bias", world),
                new ListCollection<Int16>(links));

            link1 = NativeMethods.GetPrivateProfileInt16("shrine01", "link1", world);
            link2 = NativeMethods.GetPrivateProfileInt16("shrine01", "link2", world);
            link3 = NativeMethods.GetPrivateProfileInt16("shrine01", "link3", world);
            links.Clear(); links.Add(link1); links.Add(link2); links.Add(link3);
            string align1 = NativeMethods.GetPrivateProfileString("shrine01", "alignment", world);
            var shrine1 = new Shrine(
                align1 == "chaos" ? Team.Dragon : align1 == "balance" ? Team.Pheonix : Team.Gryphon,
                1,
                NativeMethods.GetPrivateProfileInt16("shrine01", "power", world),
                NativeMethods.GetPrivateProfileInt16("shrine01", "bias", world),
                new ListCollection<Int16>(links));

            link1 = NativeMethods.GetPrivateProfileInt16("shrine02", "link1", world);
            link2 = NativeMethods.GetPrivateProfileInt16("shrine02", "link2", world);
            link3 = NativeMethods.GetPrivateProfileInt16("shrine02", "link3", world);
            links.Clear(); links.Add(link1); links.Add(link2); links.Add(link3);
            string align2 = NativeMethods.GetPrivateProfileString("shrine02", "alignment", world);
            var shrine2 = new Shrine(
                align2 == "chaos" ? Team.Dragon : align2 == "balance" ? Team.Pheonix : Team.Gryphon,
                2,
                NativeMethods.GetPrivateProfileInt16("shrine02", "power", world),
                NativeMethods.GetPrivateProfileInt16("shrine02", "bias", world),
                new ListCollection<Int16>(links));

            // Match shrine to team
            Shrine dragon = null, phoenix = null, griffin = null;
            foreach (var s in new[] { shrine0, shrine1, shrine2 })
            {
                if (s.Team == Team.Dragon) dragon = s;
                else if (s.Team == Team.Pheonix) phoenix = s;
                else if (s.Team == Team.Gryphon) griffin = s;
            }

            return LeyGraph.Build(pools, dragon, phoenix, griffin);
        }

        // ================================================================
        // Graph structure
        // ================================================================

        [Test]
        public void Build_CreatesNodesForActivePools()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null, "WORLD.DAT not found");
            // 9 active pools (pool 4 skipped) + 3 shrines = 12 nodes
            Assert.AreEqual(12, graph.Nodes.Count);
        }

        [Test]
        public void Build_SkipsCommentedOutPool()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            Assert.IsFalse(graph.Nodes.ContainsKey(4), "Pool 4 (commented out) should not be in graph");
        }

        [Test]
        public void Build_HasThreeShrines()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            Assert.IsTrue(graph.Nodes.ContainsKey(100), "Dragon shrine (100)");
            Assert.IsTrue(graph.Nodes.ContainsKey(101), "Phoenix shrine (101)");
            Assert.IsTrue(graph.Nodes.ContainsKey(102), "Griffin shrine (102)");
        }

        [Test]
        public void Build_ShrinesHaveCorrectTeams()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // grid00: shrine00=balance(Phoenix), shrine01=order(Griffin), shrine02=chaos(Dragon)
            Assert.AreEqual(Team.Pheonix, graph.Nodes[100].Team);
            Assert.AreEqual(Team.Gryphon, graph.Nodes[101].Team);
            Assert.AreEqual(Team.Dragon, graph.Nodes[102].Team);
        }

        [Test]
        public void Build_EdgesAreBidirectional()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            foreach (var node in graph.Nodes.Values)
            {
                foreach (var neighbor in node.Neighbors)
                {
                    Assert.IsTrue(neighbor.Neighbors.Contains(node),
                        $"Edge {node.Id} -> {neighbor.Id} is not bidirectional");
                }
            }
        }

        [Test]
        public void Build_Pool0_ConnectedToGriffinShrine()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            var pool0 = graph.Nodes[0];
            // earthblood00 link1=102 (griffin shrine)
            Assert.IsTrue(pool0.Neighbors.Any(n => n.Id == 102),
                "Pool 0 should be connected to Griffin shrine (102)");
        }

        [Test]
        public void Build_Pool0_HasThreeNeighbors()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            var pool0 = graph.Nodes[0];
            // links: 102, 5, 7
            Assert.AreEqual(3, pool0.Neighbors.Count);
        }

        // ================================================================
        // Team network (BFS)
        // ================================================================

        [Test]
        public void GetTeamNetwork_AllNeutral_ReturnsOnlyShrine()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // All pools start neutral — only the shrine itself is in the team network
            var network = graph.GetTeamNetwork(Team.Dragon);
            Assert.AreEqual(1, network.Count, "Only shrine, neutral nodes don't count as team network");
            Assert.IsTrue(network.Any(n => n.Type == LeyNodeType.Shrine));
        }

        [Test]
        public void GetTeamPower_AllNeutral_ReturnsZero()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Power only counts nodes owned by the team, not neutral
            Assert.AreEqual(0, graph.GetTeamPower(Team.Dragon));
        }

        [Test]
        public void GetTeamPower_WithOwnedNode_ReturnsPower()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Simulate biasing: Dragon owns pool 8 (link1=102=Dragon shrine)
            graph.Nodes[8].Team = Team.Dragon;
            graph.Nodes[8].Pool.Team = Team.Dragon;

            int power = graph.GetTeamPower(Team.Dragon);
            Assert.AreEqual(11, power, "Pool 8 has power=11");
        }

        [Test]
        public void IsConnectedToShrine_OwnedDirectLink_ReturnsTrue()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Pool 8 links directly to shrine 102 (Dragon). Must be owned by Dragon to count.
            graph.Nodes[8].Team = Team.Dragon;
            Assert.IsTrue(graph.IsConnectedToShrine(8, Team.Dragon));
        }

        [Test]
        public void IsConnectedToShrine_EnemyBlocks_ReturnsFalse()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Pool 0 connects to shrine 102 (Dragon) via link.
            // If we make pool 0 owned by Phoenix, Dragon can still reach it since pool 0 is a neighbor of shrine 102.
            // But if we block ALL paths by making intermediate nodes enemy-owned...
            // Pool 8 -> shrine 102 (Dragon). Pool 8 also links to pool 9 and pool 7.
            // Block pool 8 by making it Phoenix-owned
            graph.Nodes[8].Team = Team.Pheonix;
            // Pool 7 also links to shrine 102. Block it too.
            graph.Nodes[7].Team = Team.Pheonix;
            // Pool 0 also links to shrine 102. Block it.
            graph.Nodes[0].Team = Team.Pheonix;

            // Now pool 9 (linked to 100=Phoenix shrine, 8, 6) should NOT be reachable from Dragon shrine
            // because all paths from shrine 102 go through Phoenix-owned nodes
            Assert.IsFalse(graph.IsConnectedToShrine(9, Team.Dragon));
        }

        // ================================================================
        // Bias eligibility
        // ================================================================

        [Test]
        public void BiasEligibility_NodeAdjacentToShrine_Connected()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Pool 8 is directly linked to shrine 102 (Dragon)
            // Dragon shrine counts as team-owned
            var elig = graph.GetBiasEligibility(8, Team.Dragon);
            Assert.AreEqual(BiasEligibility.Connected, elig);
        }

        [Test]
        public void BiasEligibility_NodeAdjacentToOwnedNode_Connected()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Own pool 8 (linked to shrine 102=Dragon)
            graph.Nodes[8].Team = Team.Dragon;
            // Pool 9 links to pool 8 — should be connected
            var elig = graph.GetBiasEligibility(9, Team.Dragon);
            Assert.AreEqual(BiasEligibility.Connected, elig);
        }

        [Test]
        public void BiasEligibility_NodeNotAdjacent_Disconnected()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Pool 5 links to shrine 101 (Griffin) and pools 0, 1
            // None of those are Dragon-owned, and shrine 101 is Griffin
            // Pool 5 is not adjacent to Dragon network
            var elig = graph.GetBiasEligibility(5, Team.Dragon);
            Assert.AreEqual(BiasEligibility.Disconnected, elig);
        }

        [Test]
        public void BiasEligibility_Nexus_RequiresAdjacentNode()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Try to attack Phoenix nexus (100) without any owned nodes adjacent to it
            var elig = graph.GetBiasEligibility(100, Team.Dragon);
            Assert.AreEqual(BiasEligibility.Blocked, elig);
        }

        [Test]
        public void BiasEligibility_Nexus_WithAdjacentNode_Connected()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Phoenix shrine (100) links to pools 3, 6, 9
            // Own pool 3 as Dragon
            graph.Nodes[3].Team = Team.Dragon;
            var elig = graph.GetBiasEligibility(100, Team.Dragon);
            Assert.AreEqual(BiasEligibility.Connected, elig);
        }

        [Test]
        public void BiasEligibility_OwnNexus_AlwaysConnected()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Dragon shrine (102) — Dragon can always repair their own nexus
            var elig = graph.GetBiasEligibility(102, Team.Dragon);
            Assert.AreEqual(BiasEligibility.Connected, elig);
        }

        // ================================================================
        // Proximity
        // ================================================================

        [Test]
        public void GetNearestNode_ReturnsClosest()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Pool 0 is at (2112, 3072) — stand right on it
            var nearest = graph.GetNearestNode(2112, 3072, 260);
            Assert.IsNotNull(nearest);
            Assert.AreEqual(0, nearest.Id);
        }

        [Test]
        public void GetNearestTeamNode_NoOwnedNodes_ReturnsNull()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            var nearest = graph.GetNearestTeamNode(2112, 3072, 260, Team.Dragon);
            Assert.IsNull(nearest, "No Dragon nodes exist yet");
        }

        [Test]
        public void GetNearestTeamNode_WithOwnedNode_ReturnsIt()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            graph.Nodes[0].Team = Team.Dragon;
            var nearest = graph.GetNearestTeamNode(2112, 3072, 260, Team.Dragon);
            Assert.IsNotNull(nearest);
            Assert.AreEqual(0, nearest.Id);
        }

        // ================================================================
        // Sync
        // ================================================================

        [Test]
        public void SyncFromGameState_UpdatesNodeTeam()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Change the Pool object directly (as biasing code does)
            var pool5 = graph.Nodes[5].Pool;
            pool5.Team = Team.Pheonix;
            pool5.CurrentBias = 75;

            // Node doesn't know yet
            Assert.AreEqual(Team.Neutral, graph.Nodes[5].Team);

            // Sync
            graph.SyncFromGameState();

            Assert.AreEqual(Team.Pheonix, graph.Nodes[5].Team);
            Assert.AreEqual(75, graph.Nodes[5].CurrentBias);
        }
    }
}
