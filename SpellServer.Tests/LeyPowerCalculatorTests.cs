using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Helper;
using SpellServer;

namespace SpellServer.Tests
{
    [TestFixture]
    public class LeyPowerCalculatorTests
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

            var links = new ListCollection<Int16>();
            Shrine dragon = null, phoenix = null, griffin = null;

            int shrineCount = NativeMethods.GetPrivateProfileInt32("shrinedefs", "numshrines", world);
            for (int x = 0; x < shrineCount; x++)
            {
                string section = string.Format("shrine{0:00}", x);
                short power = NativeMethods.GetPrivateProfileInt16(section, "power", world);
                short bias = NativeMethods.GetPrivateProfileInt16(section, "bias", world);
                byte fixture = NativeMethods.GetPrivateProfileByte(section, "fixture", world);
                links.Clear();
                links.Add(NativeMethods.GetPrivateProfileInt16(section, "link1", world));
                links.Add(NativeMethods.GetPrivateProfileInt16(section, "link2", world));
                links.Add(NativeMethods.GetPrivateProfileInt16(section, "link3", world));

                string align = NativeMethods.GetPrivateProfileString(section, "alignment", world);
                Team team = align == "chaos" ? Team.Dragon : align == "balance" ? Team.Pheonix : Team.Gryphon;
                var shrine = new Shrine(team, (byte)x, power, bias, new ListCollection<Int16>(links));
                shrine.Fixture = fixture;
                if (nifs != null && fixture > 0)
                {
                    string fs = string.Format("fixture{0:00}", fixture);
                    shrine.X = NativeMethods.GetPrivateProfileInt32(fs, "x", nifs);
                    shrine.Y = NativeMethods.GetPrivateProfileInt32(fs, "y", nifs);
                    shrine.Z = NativeMethods.GetPrivateProfileInt32(fs, "z", nifs);
                }

                if (team == Team.Dragon) dragon = shrine;
                else if (team == Team.Pheonix) phoenix = shrine;
                else griffin = shrine;
            }

            return LeyGraph.Build(pools, dragon, phoenix, griffin);
        }

        // ================================================================
        // Healer — nexus proximity
        // ================================================================

        [Test]
        public void Healer_OnNexus_FullRegen()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Find Phoenix shrine position
            var shrine = graph.Nodes.Values.First(n => n.Type == LeyNodeType.Shrine && n.Team == Team.Pheonix);
            float regen = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Healer, shrine.X, shrine.Y, shrine.Z, Team.Pheonix, graph);
            Assert.AreEqual(1.0f, regen, 0.01f);
        }

        [Test]
        public void Healer_FarFromNexus_MinRegen()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            float regen = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Healer, 0, 0, 0, Team.Pheonix, graph);
            Assert.That(regen, Is.GreaterThanOrEqualTo(LeyPowerCalculator.HealerMinRegen - 0.01f));
            Assert.That(regen, Is.LessThan(0.3f), "Should be near the 15% floor");
        }

        [Test]
        public void Healer_MidRange_PartialRegen()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            var shrine = graph.Nodes.Values.First(n => n.Type == LeyNodeType.Shrine && n.Team == Team.Pheonix);
            // Half the proximity radius away
            int testX = shrine.X + (int)(LeyPowerCalculator.ShrineProximityRadius * 0.4f);
            float regen = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Healer, testX, shrine.Y, shrine.Z, Team.Pheonix, graph);
            Assert.That(regen, Is.GreaterThan(0f).And.LessThan(1f));
        }

        // ================================================================
        // Magician — node proximity + connected to shrine
        // ================================================================

        [Test]
        public void Magician_OnOwnedConnectedNode_FullRegen()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Own pool 8 (links to shrine 102 = Dragon)
            graph.Nodes[8].Team = Team.Dragon;
            var node = graph.Nodes[8];
            float regen = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Magician, node.X, node.Y, node.Z, Team.Dragon, graph);
            Assert.AreEqual(1.0f, regen, 0.01f);
        }

        [Test]
        public void Magician_NoOwnedNodes_NoRegen()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            float regen = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Magician, 3136, 5056, 4, Team.Dragon, graph);
            Assert.AreEqual(0f, regen);
        }

        [Test]
        public void Magician_OnOwnedNodeDisconnected_NoRegen()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Own pool 5 but block all paths to Dragon shrine (102)
            // Pool 5 links to: 101(Phoenix shrine), 0, 1 — not directly to Dragon shrine
            graph.Nodes[5].Team = Team.Dragon;
            // Pool 5 has no path to shrine 102 through Dragon-owned nodes
            var node = graph.Nodes[5];
            float regen = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Magician, node.X, node.Y, node.Z, Team.Dragon, graph);
            Assert.AreEqual(0f, regen, "Node not connected to Dragon shrine");
        }

        // ================================================================
        // Mystic — total team network (works anywhere)
        // ================================================================

        [Test]
        public void Mystic_NoOwnedNodes_NoRegen()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            float regen = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Mystic, 0, 0, 0, Team.Dragon, graph);
            Assert.AreEqual(0f, regen);
        }

        [Test]
        public void Mystic_SomeOwnedNodes_PartialRegen()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Dragon owns pool 8 (connected to shrine 102)
            graph.Nodes[8].Team = Team.Dragon;
            graph.Nodes[8].Pool.Team = Team.Dragon;
            float regen = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Mystic, 0, 0, 0, Team.Dragon, graph);
            // 1 node out of 9 active = ~11%
            Assert.That(regen, Is.GreaterThan(0f).And.LessThan(0.5f));
        }

        [Test]
        public void Mystic_RegenWorksAnywhere()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            graph.Nodes[8].Team = Team.Dragon;
            graph.Nodes[8].Pool.Team = Team.Dragon;
            // Same regen whether on the node or far away
            float regenNear = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Mystic, graph.Nodes[8].X, graph.Nodes[8].Y, 0, Team.Dragon, graph);
            float regenFar = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Mystic, 0, 0, 0, Team.Dragon, graph);
            Assert.AreEqual(regenNear, regenFar, 0.001f);
        }

        // ================================================================
        // Runemage — contact charge + drain
        // ================================================================

        [Test]
        public void Runemage_OnOwnNode_FullRegen()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            graph.Nodes[8].Team = Team.Dragon;
            var node = graph.Nodes[8];
            float regen = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Runemage, node.X, node.Y, node.Z, Team.Dragon, graph);
            Assert.AreEqual(1.0f, regen, 0.01f);
        }

        [Test]
        public void Runemage_AwayFromNode_UsesCharge()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            float charge = 0.6f;
            float regen = LeyPowerCalculator.GetRegenLevel(
                Character.PlayerClass.Runemage, 0, 0, 0, Team.Dragon, graph, charge);
            Assert.AreEqual(0.6f, regen, 0.01f);
        }

        [Test]
        public void Runemage_ChargeIncreases_OnNode()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            graph.Nodes[8].Team = Team.Dragon;
            var node = graph.Nodes[8];
            float charge = 0.5f;
            float newCharge = LeyPowerCalculator.UpdateRunemageCharge(charge, node.X, node.Y, node.Z, Team.Dragon, graph);
            Assert.That(newCharge, Is.GreaterThan(charge));
        }

        [Test]
        public void Runemage_ChargeDrains_AwayFromNode()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            float charge = 0.5f;
            float newCharge = LeyPowerCalculator.UpdateRunemageCharge(charge, 0, 0, 0, Team.Dragon, graph);
            Assert.That(newCharge, Is.LessThan(charge));
        }

        // ================================================================
        // Earthpower (team HUD indicator)
        // ================================================================

        [Test]
        public void Earthpower_NoOwnedNodes_Zero()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            Assert.AreEqual(0, LeyPowerCalculator.GetTeamEarthpower(Team.Dragon, graph));
        }

        [Test]
        public void Earthpower_OneNode_Proportional()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            graph.Nodes[8].Team = Team.Dragon;
            graph.Nodes[8].Pool.Team = Team.Dragon;
            int pct = LeyPowerCalculator.GetTeamEarthpower(Team.Dragon, graph);
            // 1 of 9 active nodes, each power=11 -> 11/99 ~= 11%
            Assert.That(pct, Is.InRange(10, 12));
        }

        [Test]
        public void Earthpower_AllTeams_SumsCorrectly()
        {
            var graph = BuildGrid00Graph();
            Assume.That(graph, Is.Not.Null);
            // Give each team 3 connected nodes
            // Dragon: 0, 7, 8 (all link to shrine 102)
            graph.Nodes[0].Team = Team.Dragon; graph.Nodes[0].Pool.Team = Team.Dragon;
            graph.Nodes[7].Team = Team.Dragon; graph.Nodes[7].Pool.Team = Team.Dragon;
            graph.Nodes[8].Team = Team.Dragon; graph.Nodes[8].Pool.Team = Team.Dragon;
            // Phoenix: 1, 5 (link to shrine 101... actually Phoenix=100)
            graph.Nodes[1].Team = Team.Pheonix; graph.Nodes[1].Pool.Team = Team.Pheonix;
            graph.Nodes[5].Team = Team.Pheonix; graph.Nodes[5].Pool.Team = Team.Pheonix;
            // Griffin: 6, 9 (link to shrine 101)
            graph.Nodes[6].Team = Team.Gryphon; graph.Nodes[6].Pool.Team = Team.Gryphon;
            graph.Nodes[9].Team = Team.Gryphon; graph.Nodes[9].Pool.Team = Team.Gryphon;

            var (d, p, g) = LeyPowerCalculator.GetAllTeamEarthpower(graph);
            Assert.That(d + p + g, Is.LessThanOrEqualTo(100), "Can't exceed 100% total");
            Assert.That(d, Is.GreaterThan(p), "Dragon has more nodes");
        }

        // ================================================================
        // Regen to power conversion
        // ================================================================

        [Test]
        public void RegenLevel_Zero_GivesBaseRate()
        {
            float pp = LeyPowerCalculator.RegenLevelToPowerPerTick(0f);
            Assert.AreEqual(LeyPowerCalculator.BaseRegenRate, pp, 0.0001f);
        }

        [Test]
        public void RegenLevel_Full_GivesMaxRate()
        {
            float pp = LeyPowerCalculator.RegenLevelToPowerPerTick(1.0f);
            Assert.AreEqual(LeyPowerCalculator.MaxRegenRate, pp, 0.0001f);
        }
    }
}
