using System;
using System.Collections.Generic;
using Helper;

namespace SpellServer.Bot
{
    /// <summary>
    /// Pre-computed walkability grid for bot pathfinding.
    /// Built once per arena from GridBlock data at arena load time.
    ///
    /// Grid is 128x128 cells, each 64x64 world units.
    /// World coord >> 6 = grid coord.
    /// </summary>
    public class NavGrid
    {
        public const int Size = 128;
        public const int CellSize = 64;
        public const int StepHeight = 32;       // max floor height diff for traversal (stairs)
        public const int MinHeadroom = 80;       // standing player height
        public const float DiagonalCost = 1.41f;
        public const float CardinalCost = 1.0f;

        public readonly byte[,] Walkability;     // 0=walkable, 1=solid
        public readonly short[,] FloorHeight;    // floor Z at cell center
        private readonly HashSet<long> _blockedEdges; // thin-blocked cell transitions

        public NavGrid(Grid grid)
        {
            Walkability = new byte[Size, Size];
            FloorHeight = new short[Size, Size];
            _blockedEdges = new HashSet<long>();

            Build(grid);
        }

        private void Build(Grid grid)
        {
            // Pass 1: classify each cell
            for (int gX = 0; gX < Size; gX++)
            {
                for (int gY = 0; gY < Size; gY++)
                {
                    var block = grid.GridBlocks.GetBlockByLocation(gX * CellSize, gY * CellSize);
                    if (block == null)
                    {
                        Walkability[gX, gY] = 1;
                        continue;
                    }

                    if (block.SpecialCollision == 1 || block.IsSolidPillar)
                    {
                        Walkability[gX, gY] = 1;
                        continue;
                    }

                    int worldX = gX * CellSize + CellSize / 2;
                    int worldY = gY * CellSize + CellSize / 2;
                    int floor = grid.GetFloorHeight(worldX, worldY, 0, grid);
                    int ceiling = grid.GetCeilingHeight(worldX, worldY, 0, grid);

                    if (ceiling - floor < MinHeadroom)
                    {
                        Walkability[gX, gY] = 1;
                        continue;
                    }

                    Walkability[gX, gY] = 0;
                    FloorHeight[gX, gY] = (short)floor;
                }
            }

            // Pass 2: mark edges blocked by Thins
            if (grid.Thins != null)
            {
                foreach (Thin thin in grid.Thins)
                {
                    if (thin == null || !thin.BlockPlayers) continue;

                    int minGX = Math.Max(0, (int)thin.X1 >> 6);
                    int maxGX = Math.Min(Size - 1, (int)thin.X2 >> 6);
                    int minGY = Math.Max(0, (int)thin.Y1 >> 6);
                    int maxGY = Math.Min(Size - 1, (int)thin.Y2 >> 6);

                    // Block all cell edges the thin spans
                    for (int gX = minGX; gX <= maxGX; gX++)
                    {
                        for (int gY = minGY; gY <= maxGY; gY++)
                        {
                            // Block transitions to neighboring cells
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    if (dx == 0 && dy == 0) continue;
                                    int nx = gX + dx, ny = gY + dy;
                                    if (nx < 0 || nx >= Size || ny < 0 || ny >= Size) continue;
                                    _blockedEdges.Add(PackEdge(gX, gY, nx, ny));
                                }
                            }
                        }
                    }
                }
            }

            int walkable = 0, solid = 0;
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    if (Walkability[x, y] == 0) walkable++; else solid++;

            Program.Log($"[NavGrid] Built: {walkable} walkable, {solid} solid, {_blockedEdges.Count} blocked edges",
                System.Drawing.Color.Green);
        }

        /// <summary>Can a bot walk from (fromGX,fromGY) to (toGX,toGY)?</summary>
        public bool IsTraversable(int fromGX, int fromGY, int toGX, int toGY)
        {
            if (toGX < 0 || toGX >= Size || toGY < 0 || toGY >= Size) return false;
            if (Walkability[toGX, toGY] != 0) return false;

            // Height check: can't walk up cliffs
            if (Math.Abs(FloorHeight[toGX, toGY] - FloorHeight[fromGX, fromGY]) > StepHeight) return false;

            // Diagonal: both cardinal neighbors must be walkable (no corner-cutting)
            int dx = toGX - fromGX, dy = toGY - fromGY;
            if (dx != 0 && dy != 0)
            {
                if (Walkability[fromGX + dx, fromGY] != 0) return false;
                if (Walkability[fromGX, fromGY + dy] != 0) return false;
            }

            // Thin blocking
            if (_blockedEdges.Contains(PackEdge(fromGX, fromGY, toGX, toGY))) return false;

            return true;
        }

        /// <summary>Convert grid coords to world coords (cell center).</summary>
        public static SharpDX.Vector3 GridToWorld(int gX, int gY, short floorZ)
        {
            return new SharpDX.Vector3(gX * CellSize + CellSize / 2, gY * CellSize + CellSize / 2, floorZ);
        }

        /// <summary>Convert world coords to grid coords.</summary>
        public static void WorldToGrid(float worldX, float worldY, out int gX, out int gY)
        {
            gX = (int)worldX >> 6;
            gY = (int)worldY >> 6;
        }

        private static long PackEdge(int x1, int y1, int x2, int y2)
        {
            return ((long)(uint)(x1 & 0xFF) << 24) | ((long)(uint)(y1 & 0xFF) << 16) | ((long)(uint)(x2 & 0xFF) << 8) | (uint)(y2 & 0xFF);
        }
    }
}
