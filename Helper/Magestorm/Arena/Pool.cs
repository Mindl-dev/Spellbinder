using System;

namespace Helper
{
    public class Pool
    {
        public readonly Object SyncRoot = new Object();

        public Int16 Power;
        public Int16 MaxBias;
        public Byte PoolId;
        public Team Team;
        public Byte Fixture;
        public Int16 Radius;
        public Int32 X;
        public Int32 Y;
        public Int32 Z;
        public ListCollection<Int16> Links;
        private Int16 _currentBias;

        public Pool(Byte poolId, Int16 power, Int16 maxBias)
        {
            PoolId = poolId;
            Team = Team.Neutral;
            MaxBias = maxBias;
            CurrentBias = 0;
            Power = power;
            Links = new ListCollection<Int16>();
        }

        public Pool(Pool p)
        {
            PoolId = p.PoolId;
            Team = Team.Neutral;
            MaxBias = p.MaxBias;
            CurrentBias = 0;
            Power = p.Power;
            Fixture = p.Fixture;
            Radius = p.Radius;
            X = p.X;
            Y = p.Y;
            Z = p.Z;
            Links = new ListCollection<Int16>(p.Links);
        }

        public Int16 CurrentBias
        {
            get { return _currentBias; }
            set
            {
                if (value < 0) value = 0;
                if (value > 100) value = 100;

                _currentBias = value;
            }
        }
    } 
}
