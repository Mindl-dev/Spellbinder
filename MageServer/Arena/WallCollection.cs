using System;
using System.Linq;
using Helper;
using Color = System.Drawing.Color;

namespace MageServer
{
    public class WallCollection : ListCollection<Wall>
    {
        public Wall FindById(Int16 objectId)
        {
            return this.FirstOrDefault(w => objectId - w.ObjectId < 4 && objectId - w.ObjectId >= 0);
        }

        public (bool hit, float distance) RayIntersectsWall(SharpDX.Vector3 casterPos, float casterDirectionRadians, Wall wall, float maxRange = 512f)
        {
            // Wall is a rectangle centered at wall.Location, aligned to its Direction
            // halfLength = wall.Length / 2
            // halfThick = wall.Thickness / 2

            float halfLength = wall.Spell.Length / 2f;
            float halfThick = wall.Spell.Thick / 2f;
            float halfHeight = wall.Spell.WallHeight / 2f;  // Use per-spell wall height

            float effectiveThick = halfThick + 120f;  // add tolerance
            float effectiveLength = halfLength + 100f;  // add tolerance
            //float effectiveHeight = halfHeight + 100f;  // add tolerance

            // Direction vector of the ray (dispel facing)
            float dirX = (float)Math.Cos(casterDirectionRadians);
            float dirY = (float)Math.Sin(casterDirectionRadians);

            // Vector from caster to wall center
            float toCenterX = wall.Location.X - casterPos.X;
            float toCenterY = wall.Location.Y - casterPos.Y;
            float toCenterZ = wall.Location.Z - casterPos.Z;

            // Project onto ray direction and two perpendicular axes
            float projAlong = toCenterX * dirX + toCenterY * dirY;                     // along cast direction
            float projPerp = -toCenterX * dirY + toCenterY * dirX;                    // perpendicular in XY plane
            float projVert = toCenterZ;                                              // vertical

            // Check if projection falls within the wall's rectangular prism
            if (Math.Abs(projAlong) > effectiveLength) return (false, 0);
            if (Math.Abs(projPerp) > effectiveThick) return (false, 0);
            //if (Math.Abs(projVert) > effectiveHeight) return (false, 0);


            // Distance along the ray to the hit point
            float distance = Math.Abs(projAlong);

            return (distance <= maxRange, distance);
        }

        public Wall FindByVector(Player player, SharpDX.Vector3 casterPos, Single fDirection, Spell spell)
        {
            float directionRadians = fDirection * (2f * (float)Math.PI / 65536f);

            SharpDX.Vector3 rayDir = new SharpDX.Vector3(
                (float)Math.Cos(directionRadians),
                (float)Math.Sin(directionRadians),
                0f); // assuming 2D ray in XY plane

            SharpDX.Vector3 rayStart = casterPos - rayDir * 50f; // small backstep

            // End point of the ray at max range
            SharpDX.Vector3 rayEnd = casterPos + rayDir * (spell.Range + 50f);

            Wall bestWall = null;

            double bestDistance = float.MaxValue;

            foreach (var wall in player.ActiveArena.Walls)
            {
                // Must be owned by the caster or the active team
                if ((wall.Owner.WorldPlayer.PlayerId != player.PlayerId) || (wall.Owner.ActiveTeam != player.ActiveTeam))
                    continue;

                if (wall.BoundingBox.PointInBox(casterPos))
                {
                    // Caster standing inside/on the wall — always hit
                    float dist = 0f;
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestWall = wall;
                    }
                    continue; // no need to check line
                }

                var (hit, distance) = RayIntersectsWall(casterPos, directionRadians, wall, spell.Range);

                if (hit && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestWall = wall;
                }

            }

            return bestWall;
        }
    }
}
