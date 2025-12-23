using System;
using System.Linq;
using Helper;
using Color = System.Drawing.Color;

namespace SpellServer
{
    public class RuneCollection : ListCollection<Rune>
    {
        public Rune FindById(Int16 objectId)
        {
            return this.FirstOrDefault(s => objectId == s.ObjectId);
        }
        public (bool hit, float distance) RayIntersectsRune(SharpDX.Vector3 casterPos, float casterDirectionRadians, Rune rune, float maxRange = 512f)
        {
            // Rune is a rectangle centered at rune.Location, aligned to its Direction
            // halfWidth = rune.Width / 2
            // halfThick = rune.Tall / 2

            float halfThick = rune.Spell.Width / 2f;
            float halfWidth = rune.Spell.Width / 2f;
            float halfTall = rune.Spell.Tall / 2f;  // Use per-spell rune tall

            float effectiveWidth = halfWidth + 120f;  // add tolerance
            float effectiveThick = halfThick + 100f;  // add tolerance

            // Direction vector of the ray (dispel facing)
            float dirX = (float)Math.Cos(casterDirectionRadians);
            float dirY = (float)Math.Sin(casterDirectionRadians);

            // Vector from caster to rune center
            float toCenterX = rune.Location.X - casterPos.X;
            float toCenterY = rune.Location.Y - casterPos.Y;
            float toCenterZ = rune.Location.Z - casterPos.Z;

            // Project onto ray direction and two perpendicular axes
            float projAlong = toCenterX * dirX + toCenterY * dirY;                     // along cast direction
            float projPerp = -toCenterX * dirY + toCenterY * dirX;                    // perpendicular in XY plane
            float projVert = toCenterZ;                                              // vertical

            Program.ServerForm.MainLog.WriteMessage($"projAlong: {projAlong.ToString()}, projPerp: {projPerp.ToString()}, projVert: {projVert.ToString()}, effectiveWidth: {effectiveWidth.ToString()}, effectiveThick: {effectiveThick.ToString()}", Color.Red);

            // Check if projection falls within the rune's rectangular prism
            if (Math.Abs(projAlong) > effectiveWidth) return (false, 0);
            if (Math.Abs(projPerp) > effectiveThick) return (false, 0);
            //if (Math.Abs(projVert) > effectiveTall) return (false, 0);
                        
            // Distance along the ray to the hit point
            float distance = Math.Abs(projAlong);

            return (distance <= maxRange, distance);
        }

        public Rune FindByVector(Player player, SharpDX.Vector3 casterPos, Single fDirection, Spell spell)
        {
            Program.ServerForm.MainLog.WriteMessage($"FindByVector", Color.Red);

            float directionRadians = fDirection * (2f * (float)Math.PI / 65536f);

            SharpDX.Vector3 rayDir = new SharpDX.Vector3(
                (float)Math.Cos(directionRadians),
                (float)Math.Sin(directionRadians),
                0f); // assuming 2D ray in XY plane

            SharpDX.Vector3 rayStart = casterPos - rayDir * 50f; // small backstep

            // End point of the ray at max range
            SharpDX.Vector3 rayEnd = casterPos + rayDir * (spell.Range + 50f);

            Rune bestRune = null;

            double bestDistance = float.MaxValue;

            foreach (var rune in player.ActiveArena.Runes)
            {
                // Must be owned by the caster
                if (rune.Owner.WorldPlayer.PlayerId != player.PlayerId)
                    continue;

                if (rune.BoundingBox.PointInBox(casterPos))
                {
                    // Caster standing inside/on the rune — always hit
                    float dist = 0f;
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestRune = rune;
                    }
                    continue; // no need to check line
                }

                var (hit, distance) = RayIntersectsRune(casterPos, directionRadians, rune, spell.Range);

                if (hit && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestRune = rune;
                }

            }

            return bestRune;
        }
    }

}
