using System;
using Helper;

namespace SpellServer
{
    /// <summary>
    /// Calculates earthpower regen rates and team power from the ley line network.
    ///
    /// From the game manual:
    ///   Healer:   power from proximity to own nexus
    ///   Magician: power from proximity to own nodes connected to nexus
    ///   Mystic:   power from team's total network (anywhere on map)
    ///   Runemage: power from direct contact with own node, charges up for limited time away
    ///
    /// Regen bar (purple) controls how fast power regenerates.
    /// Power bar (blue) is the actual mana pool.
    /// </summary>
    public static class LeyPowerCalculator
    {
        // Earthpower damage scaling
        // damage *= EarthpowerDamageBase + (EarthpowerDamageScale * teamEarthpower / 100)
        // At 0% earthpower: 0.7x damage. At 100%: 1.0x damage.
        // Ratio at 100% vs 0% = 1.43x advantage.
        public const float EarthpowerDamageBase = 0.7f;
        public const float EarthpowerDamageScale = 0.3f;

        // Biasing speed multipliers (applied to bias roll/amount)
        public const float BackHackBiasMultiplier = 0.25f;  // 4x slower when disconnected from network
        public const float NexusBiasMultiplier = 0.33f;      // 3x harder to bias enemy nexus

        // Distance thresholds (world units)
        public const float NodeContactRadius = 192f;   // "direct contact" — standing on/near the node
        public const float NodeProximityRadius = 512f;  // "close proximity" — nearby but not touching
        public const float ShrineProximityRadius = 640f; // healer nexus proximity

        // Regen rates (power points per tick, before multipliers)
        public const float BaseRegenRate = 0.005f;      // everyone gets a trickle
        public const float MaxRegenRate = 0.05f;         // full regen bar

        // Runemage charge
        public const float RunemageChargeRate = 0.08f;   // charges fast on node
        public const float RunemageMaxCharge = 1.0f;     // full charge = 100%
        public const float RunemageDrainRate = 0.002f;   // drains slowly away from node

        /// <summary>
        /// Calculate the regen bar fill level (0.0 - 1.0) for a player.
        /// This drives how fast their power (blue bar) regenerates.
        /// </summary>
        public static float GetRegenLevel(
            Character.PlayerClass playerClass,
            int playerX, int playerY, int playerZ,
            Team playerTeam,
            LeyGraph graph,
            float runemageCharge = 0f)
        {
            switch (playerClass)
            {
                case Character.PlayerClass.Healer:
                    return GetHealerRegen(playerX, playerY, playerZ, playerTeam, graph);

                case Character.PlayerClass.Magician:
                    return GetMagicianRegen(playerX, playerY, playerZ, playerTeam, graph);

                case Character.PlayerClass.Mystic:
                    return GetMysticRegen(playerTeam, graph);

                case Character.PlayerClass.Runemage:
                    return GetRunemageRegen(playerX, playerY, playerZ, playerTeam, graph, runemageCharge);

                default:
                    return 0f;
            }
        }

        // Healer regen floor — minimum regen even at max distance (e.g. enemy nexus)
        public const float HealerMinRegen = 0.15f;
        // Max map distance — used to normalize healer falloff (diagonal of a 128*64 grid)
        public const float HealerMaxDistance = 8192f;

        /// <summary>Healer: linear falloff from own nexus. 100% on nexus, ~15% at max distance.</summary>
        private static float GetHealerRegen(int x, int y, int z, Team team, LeyGraph graph)
        {
            float dist = graph.DistanceToShrine(x, y, z, team);
            if (dist <= NodeContactRadius)
                return 1.0f;

            // Linear falloff from 1.0 at contact to HealerMinRegen at HealerMaxDistance
            float t = Math.Min(1.0f, (dist - NodeContactRadius) / (HealerMaxDistance - NodeContactRadius));
            return HealerMinRegen + (1.0f - HealerMinRegen) * (1.0f - t);
        }

        /// <summary>
        /// Magician: power from proximity to own nodes that are connected to nexus.
        /// Must be near a team node AND that node must have a ley line path to the shrine.
        /// </summary>
        private static float GetMagicianRegen(int x, int y, int z, Team team, LeyGraph graph)
        {
            var nearest = graph.GetNearestTeamNode(x, y, z, team);
            if (nearest == null) return 0f;

            float dx = nearest.X - x;
            float dy = nearest.Y - y;
            float dz = nearest.Z - z;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

            if (dist > NodeProximityRadius) return 0f;

            // Node must be connected to shrine through team-owned chain
            if (!graph.IsConnectedToShrine(nearest.Id, team)) return 0f;

            if (dist <= NodeContactRadius)
                return 1.0f;

            return 1.0f - ((dist - NodeContactRadius) / (NodeProximityRadius - NodeContactRadius));
        }

        /// <summary>
        /// Mystic: power from team's total network. Works anywhere on the map.
        /// More team nodes connected to shrine = higher regen.
        /// </summary>
        private static float GetMysticRegen(Team team, LeyGraph graph)
        {
            int teamPower = graph.GetTeamPower(team);
            if (teamPower <= 0) return 0f;

            // Total possible power = all earthblood nodes' power
            int totalPossible = 0;
            foreach (var node in graph.Nodes.Values)
            {
                if (node.Type == LeyNodeType.Earthblood && node.Power > 0)
                    totalPossible += node.Power;
            }

            if (totalPossible <= 0) return 0f;

            // Regen scales with fraction of total power owned
            return Math.Min(1.0f, (float)teamPower / totalPossible);
        }

        /// <summary>
        /// Runemage: charges up on direct contact with own node, then drains away.
        /// Returns regen level based on whether on a node or using stored charge.
        /// </summary>
        private static float GetRunemageRegen(int x, int y, int z, Team team, LeyGraph graph, float currentCharge)
        {
            var nearest = graph.GetNearestTeamNode(x, y, z, team);
            if (nearest != null)
            {
                float dx = nearest.X - x;
                float dy = nearest.Y - y;
                float dz = nearest.Z - z;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

                if (dist <= NodeContactRadius)
                    return 1.0f; // on node = full regen + charging
            }

            // Away from node — use stored charge
            return currentCharge;
        }

        /// <summary>
        /// Update runemage charge state. Call each tick.
        /// Returns new charge value.
        /// </summary>
        public static float UpdateRunemageCharge(
            float currentCharge,
            int playerX, int playerY, int playerZ,
            Team playerTeam,
            LeyGraph graph)
        {
            var nearest = graph.GetNearestTeamNode(playerX, playerY, playerZ, playerTeam);
            bool onNode = false;

            if (nearest != null)
            {
                float dx = nearest.X - playerX;
                float dy = nearest.Y - playerY;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                onNode = dist <= NodeContactRadius;
            }

            if (onNode)
            {
                // Charge up
                return Math.Min(RunemageMaxCharge, currentCharge + RunemageChargeRate);
            }
            else
            {
                // Drain
                return Math.Max(0f, currentCharge - RunemageDrainRate);
            }
        }

        /// <summary>
        /// Convert regen level (0-1) to actual power points regenerated per tick.
        /// </summary>
        public static float RegenLevelToPowerPerTick(float regenLevel)
        {
            return BaseRegenRate + (regenLevel * (MaxRegenRate - BaseRegenRate));
        }

        /// <summary>
        /// Get the damage multiplier for a team based on their earthpower.
        /// 0% earthpower = 0.7x, 100% = 1.0x. Configurable via constants.
        /// </summary>
        public static float GetDamageMultiplier(Team team, LeyGraph graph)
        {
            int earthpower = GetTeamEarthpower(team, graph);
            return EarthpowerDamageBase + (EarthpowerDamageScale * earthpower / 100f);
        }

        /// <summary>
        /// Calculate team earthpower as a percentage (0-100) for the HUD indicator.
        /// This is the "Earthpower" bar that shows relative team strength.
        /// </summary>
        public static int GetTeamEarthpower(Team team, LeyGraph graph)
        {
            int teamPower = graph.GetTeamPower(team);

            int totalPossible = 0;
            foreach (var node in graph.Nodes.Values)
            {
                if (node.Type == LeyNodeType.Earthblood && node.Power > 0)
                    totalPossible += node.Power;
            }

            if (totalPossible <= 0) return 0;

            return (int)Math.Round(100.0 * teamPower / totalPossible);
        }

        /// <summary>
        /// Get relative earthpower for all three teams (sums to ~100 when all nodes are owned).
        /// Returns (dragon, phoenix, griffin) percentages.
        /// </summary>
        public static (int dragon, int phoenix, int griffin) GetAllTeamEarthpower(LeyGraph graph)
        {
            return (
                GetTeamEarthpower(Team.Dragon, graph),
                GetTeamEarthpower(Team.Pheonix, graph),
                GetTeamEarthpower(Team.Gryphon, graph)
            );
        }
    }
}
