using System;
using Helper;
using Helper.Math;
using Helper.Timing;
using SharpDX;
using Color = System.Drawing.Color;
using OrientedBoundingBox = Helper.Math.OrientedBoundingBox;

namespace SpellServer
{
    [Flags]
    public enum ObjectState : ushort
    {
        Inactive = 0,
        Active = 1,
        Collision = 3,
        Ghost = 8,
    }

    [Flags]
    public enum SpellFlags
    {
        None = 0,
        Homing = 0x01,
        Pierce = 0x10,
        Invisible = 0x20,
    }
    public class Projectile
    {
        public ObjectState _state;
        public ObjectState State
        {
            get { return _state; }
            set { _state = value; }
        }

        public Single Angle;
        public readonly Single OriginalAngle;
        public OrientedBoundingBox BoundingBox;
        public Single Direction;
        public Single GravityStepDelta;
        public Single GravityStepCount;
        public Vector3 OriginalOrigin;
        public Vector3 Size;
        public Vector3 Location;
        public ArenaPlayer Owner;
        public Spell Spell;
        public Team Team;
        public Single Velocity;
        public TickCounter DistanceTicks;
        public Interval Duration;
        public int MaxTargets { get; set; } = 0;
        public int Gravity { get; set; } = 0;
        public int Bounce { get; set; } = 0;
        public int MaxBounces { get; set; } = 0;
        public int BounceCount { get; set; } = 0;
        public int HitCount { get; set; } = 0;

        public bool bouncedThisTick;

        public float VerticalVelocity;  // Up/down component
        public float HorizontalVelocity;     // Base speed (unchanged or lightly damped)

        public float VelocityX;
        public float VelocityY;

        public Wall hitWall;
        public GridBlock hitBlock;
        public Thin hitThin;
        public Tile hitTile;
        public ArenaPlayer hitPlayer;
        public Projectile hitSpell;

        public ProjectileGroup ParentGroup;

        public Projectile(Vector3 location, Spell spell, Single direction, Single angle, ArenaPlayer owner)
        {
            State = ObjectState.Active;
            Spell = spell;
            Location = location;
            Direction = direction;
            Velocity = spell.Velocity;
            Angle = angle;
            Size = new Vector3(spell.Width, spell.Width, spell.Tall);
            
            Bounce = spell.Bounce;
            MaxBounces = (Bounce > 0 && MaxBounces == 0) ? 20 : spell.MaxBounces;
            BounceCount = 0;
            bouncedThisTick = false;

            Single zRadians = MathHelper.DegreesToRadians(Angle);
            Single cosZRadians = (Single)Math.Cos(zRadians);

            Location.X += -(((Single)Math.Sin(Direction) + (Size.X * 0.5f)) * cosZRadians);
            Location.Y += (Single)Math.Sin(Direction) * cosZRadians;

            if (zRadians < 0)
            {
                Location.Z += Math.Abs(zRadians * 4f) + 4f;
            }

            Owner = owner;
            Team = owner.ActiveTeam;
            GravityStepDelta = 0;
            GravityStepCount = 0;
            DistanceTicks = new TickCounter(30, 50);
            Duration = new Interval(Spell.DurationTimer, false);
            BoundingBox = new OrientedBoundingBox(Location, Size, direction);

            OriginalOrigin = BoundingBox.Origin;
            OriginalAngle = Angle;

            hitWall = null;
            hitBlock = null;
            hitThin = null;
            hitTile = null;
            hitPlayer = null;
            hitSpell = null;

            Gravity = (spell.Gravity == true) ? 1 : 0;

            MaxTargets = spell.MaxTargets;

            float horizontalSpeed = (float)(Velocity * Math.Cos(MathHelper.DegreesToRadians(Angle)));

            VelocityX = (float)-(horizontalSpeed * Math.Sin(Direction));
            VelocityY = (float)(horizontalSpeed * Math.Cos(Direction));

            VerticalVelocity = (float)Math.Sin(MathHelper.DegreesToRadians(Angle)) * Velocity;

            Velocity = horizontalSpeed;
        }
    }
}
