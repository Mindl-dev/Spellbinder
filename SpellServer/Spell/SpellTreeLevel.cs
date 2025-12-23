using System;

namespace SpellServer
{
    public class SpellTreeLevel
    {
        public readonly Int32 Level;
        public readonly Int32 List;

        public SpellTreeLevel(Int32 level, Int32 list)
        {
            Level = level;
            List = list;
        }
    }
}
