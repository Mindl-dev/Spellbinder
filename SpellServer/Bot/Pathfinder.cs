using System;
using System.Collections.Generic;
using SharpDX;

namespace SpellServer.Bot
{
    /// <summary>
    /// A* pathfinder on the NavGrid. Returns a list of grid coordinates from start to goal.
    /// 8-directional movement with octile distance heuristic.
    /// </summary>
    public static class Pathfinder
    {
        private const int MaxSize = NavGrid.Size;

        // Direction offsets: 4 cardinal + 4 diagonal
        private static readonly int[] DX = { 0, 1, 0, -1, 1, 1, -1, -1 };
        private static readonly int[] DY = { 1, 0, -1, 0, 1, -1, 1, -1 };
        private static readonly float[] DCost = {
            NavGrid.CardinalCost, NavGrid.CardinalCost, NavGrid.CardinalCost, NavGrid.CardinalCost,
            NavGrid.DiagonalCost, NavGrid.DiagonalCost, NavGrid.DiagonalCost, NavGrid.DiagonalCost
        };

        /// <summary>
        /// Find a path from (startGX,startGY) to (goalGX,goalGY) on the NavGrid.
        /// Returns list of grid coords (gX,gY) or null if unreachable.
        /// </summary>
        public static List<Vector2> FindPath(NavGrid navGrid, int startGX, int startGY, int goalGX, int goalGY, int maxIterations = 16384)
        {
            if (startGX == goalGX && startGY == goalGY)
                return new List<Vector2> { new Vector2(startGX, startGY) };

            if (navGrid.Walkability[startGX, startGY] != 0 || navGrid.Walkability[goalGX, goalGY] != 0)
                return null;

            // Flat arrays for performance — no heap allocations per cell
            var gCost = new float[MaxSize, MaxSize];
            var parentX = new int[MaxSize, MaxSize];
            var parentY = new int[MaxSize, MaxSize];
            var closed = new bool[MaxSize, MaxSize];

            // Init gCost to infinity
            for (int x = 0; x < MaxSize; x++)
                for (int y = 0; y < MaxSize; y++)
                    gCost[x, y] = float.MaxValue;

            gCost[startGX, startGY] = 0;
            parentX[startGX, startGY] = -1;
            parentY[startGX, startGY] = -1;

            // Open list — simple sorted list (16K max cells, good enough)
            var open = new SortedSet<(float fCost, int gX, int gY)>(Comparer<(float, int, int)>.Create((a, b) =>
            {
                int c = a.Item1.CompareTo(b.Item1);
                if (c != 0) return c;
                c = a.Item2.CompareTo(b.Item2);
                if (c != 0) return c;
                return a.Item3.CompareTo(b.Item3);
            }));

            open.Add((Heuristic(startGX, startGY, goalGX, goalGY), startGX, startGY));

            int iterations = 0;
            while (open.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var (_, cx, cy) = open.Min;
                open.Remove(open.Min);

                if (cx == goalGX && cy == goalGY)
                    return ReconstructPath(parentX, parentY, startGX, startGY, goalGX, goalGY);

                if (closed[cx, cy]) continue;
                closed[cx, cy] = true;

                for (int d = 0; d < 8; d++)
                {
                    int nx = cx + DX[d], ny = cy + DY[d];
                    if (nx < 0 || nx >= MaxSize || ny < 0 || ny >= MaxSize) continue;
                    if (closed[nx, ny]) continue;
                    if (!navGrid.IsTraversable(cx, cy, nx, ny)) continue;

                    float newG = gCost[cx, cy] + DCost[d];
                    if (newG < gCost[nx, ny])
                    {
                        gCost[nx, ny] = newG;
                        parentX[nx, ny] = cx;
                        parentY[nx, ny] = cy;
                        float f = newG + Heuristic(nx, ny, goalGX, goalGY);
                        open.Add((f, nx, ny));
                    }
                }
            }

            return null; // unreachable
        }

        /// <summary>Octile distance heuristic for 8-directional movement.</summary>
        private static float Heuristic(int ax, int ay, int bx, int by)
        {
            int dx = Math.Abs(ax - bx), dy = Math.Abs(ay - by);
            return Math.Max(dx, dy) + 0.41f * Math.Min(dx, dy);
        }

        private static List<Vector2> ReconstructPath(int[,] parentX, int[,] parentY, int startGX, int startGY, int goalGX, int goalGY)
        {
            var path = new List<Vector2>();
            int cx = goalGX, cy = goalGY;
            while (cx != startGX || cy != startGY)
            {
                path.Add(new Vector2(cx, cy));
                int px = parentX[cx, cy], py = parentY[cx, cy];
                cx = px;
                cy = py;
            }
            path.Add(new Vector2(startGX, startGY));
            path.Reverse();
            return path;
        }
    }
}
