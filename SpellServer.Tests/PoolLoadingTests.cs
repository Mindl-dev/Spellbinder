using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Helper;

namespace SpellServer.Tests
{
    [TestFixture]
    public class PoolLoadingTests
    {
        // Path to Content/Grids — tests run from SpellServer.Tests/bin/Debug
        // so we walk up to the repo root
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

        private static string FindNifsDat(string gridId)
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            for (int i = 0; i < 6; i++)
            {
                string candidate = Path.Combine(dir, "Content", "Grids", gridId, "NIFS", "NIFS.DAT");
                if (File.Exists(candidate)) return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        private static PoolCollection LoadPoolsFromWorld(string worldFile)
        {
            string nifsDir = Path.Combine(Path.GetDirectoryName(worldFile), "NIFS", "NIFS.DAT");
            string nifs = File.Exists(nifsDir) ? nifsDir : null;
            return Grid.LoadPools(worldFile, nifs);
        }

        // ================================================================
        // Grid00 — Kaelgard Keep (10 earthblood nodes, earthblood04 commented out)
        // ================================================================

        [Test]
        public void Grid00_LoadsCorrectPoolCount()
        {
            string world = FindWorldDat("grid00");
            Assume.That(world, Is.Not.Null, "WORLD.DAT not found — skipping");
            var pools = LoadPoolsFromWorld(world);
            Assert.AreEqual(10, pools.Count);
        }

        [Test]
        public void Grid00_ActivePoolsHavePower11()
        {
            string world = FindWorldDat("grid00");
            Assume.That(world, Is.Not.Null);
            var pools = LoadPoolsFromWorld(world);
            foreach (var pool in pools)
            {
                if (pool.PoolId == 4)
                {
                    // earthblood04 is commented out in WORLD.DAT — INI API returns 0
                    Assert.AreEqual(0, pool.Power, "Pool 4 (commented out) should have power 0");
                }
                else
                {
                    Assert.AreEqual(11, pool.Power, $"Pool {pool.PoolId} has unexpected power");
                }
            }
        }

        [Test]
        public void Grid00_Pool0_HasCorrectLinks()
        {
            string world = FindWorldDat("grid00");
            Assume.That(world, Is.Not.Null);
            var pools = LoadPoolsFromWorld(world);
            var pool0 = pools.FindById(0);
            Assert.IsNotNull(pool0);
            // earthblood00: link1=102, link2=5, link3=7
            CollectionAssert.AreEquivalent(new short[] { 102, 5, 7 }, pool0.Links.ToArray());
        }

        [Test]
        public void Grid00_Pool0_HasFixture()
        {
            string world = FindWorldDat("grid00");
            Assume.That(world, Is.Not.Null);
            var pools = LoadPoolsFromWorld(world);
            var pool0 = pools.FindById(0);
            Assert.AreEqual(13, pool0.Fixture);
        }

        [Test]
        public void Grid00_Pool1_Has5Links()
        {
            string world = FindWorldDat("grid00");
            Assume.That(world, Is.Not.Null);
            var pools = LoadPoolsFromWorld(world);
            var pool1 = pools.FindById(1);
            Assert.IsNotNull(pool1);
            // earthblood01: link1=101, link2=7, link3=6, link4=5, link5=2
            Assert.AreEqual(5, pool1.Links.Count);
            CollectionAssert.AreEquivalent(new short[] { 101, 7, 6, 5, 2 }, pool1.Links.ToArray());
        }

        [Test]
        public void Grid00_Pool8_HasRadius()
        {
            string world = FindWorldDat("grid00");
            Assume.That(world, Is.Not.Null);
            var pools = LoadPoolsFromWorld(world);
            var pool8 = pools.FindById(8);
            Assert.IsNotNull(pool8);
            // earthblood08: radius=5
            Assert.AreEqual(5, pool8.Radius);
        }

        [Test]
        public void Grid00_AllPoolsStartNeutral()
        {
            string world = FindWorldDat("grid00");
            Assume.That(world, Is.Not.Null);
            var pools = LoadPoolsFromWorld(world);
            foreach (var pool in pools)
            {
                Assert.AreEqual(Team.Neutral, pool.Team, $"Pool {pool.PoolId}");
                Assert.AreEqual(0, pool.CurrentBias, $"Pool {pool.PoolId}");
            }
        }

        [Test]
        public void Grid00_LinksReferenceValidTargets()
        {
            string world = FindWorldDat("grid00");
            Assume.That(world, Is.Not.Null);
            var pools = LoadPoolsFromWorld(world);
            foreach (var pool in pools)
            {
                foreach (short link in pool.Links)
                {
                    if (link >= 100)
                    {
                        // Shrine reference (100=dragon, 101=phoenix, 102=griffin)
                        Assert.That(link, Is.InRange((short)100, (short)102),
                            $"Pool {pool.PoolId} has invalid shrine link {link}");
                    }
                    else
                    {
                        // Pool reference
                        Assert.IsNotNull(pools.FindById(link),
                            $"Pool {pool.PoolId} links to nonexistent pool {link}");
                    }
                }
            }
        }

        [Test]
        public void Grid00_Pool5_HasPosition()
        {
            string world = FindWorldDat("grid00");
            Assume.That(world, Is.Not.Null);
            var pools = LoadPoolsFromWorld(world);
            var pool5 = pools.FindById(5);
            // pool5 fixture=1 -> [fixture01]: x=3072, y=2624, z=420
            Assert.AreEqual(1, pool5.Fixture);
            Assert.AreEqual(3072, pool5.X);
            Assert.AreEqual(2624, pool5.Y);
            Assert.AreEqual(420, pool5.Z);
        }

        [Test]
        public void Grid00_Pool0_HasPosition()
        {
            string world = FindWorldDat("grid00");
            Assume.That(world, Is.Not.Null);
            var pools = LoadPoolsFromWorld(world);
            var pool0 = pools.FindById(0);
            // pool0 fixture=13 -> [fixture13]: x=2112, y=3072, z=260
            Assert.AreEqual(13, pool0.Fixture);
            Assert.AreEqual(2112, pool0.X);
            Assert.AreEqual(3072, pool0.Y);
            Assert.AreEqual(260, pool0.Z);
        }

        // ================================================================
        // Grid01 — Rathespa Temple (9 nodes, no commented out)
        // ================================================================

        [Test]
        public void Grid01_LoadsCorrectPoolCount()
        {
            string world = FindWorldDat("grid01");
            Assume.That(world, Is.Not.Null);
            var pools = LoadPoolsFromWorld(world);
            Assert.AreEqual(9, pools.Count);
        }

        // ================================================================
        // Grid02 — Tehouxican Ruins (14 nodes)
        // ================================================================

        [Test]
        public void Grid02_LoadsCorrectPoolCount()
        {
            string world = FindWorldDat("grid02");
            Assume.That(world, Is.Not.Null);
            var pools = LoadPoolsFromWorld(world);
            Assert.AreEqual(14, pools.Count);
        }
    }
}
