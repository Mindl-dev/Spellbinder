using System;
using Helper;

namespace MageServer
{
    public class ArenaTeam
    {
        public Shrine Shrine;
        public CTFOrb ShrineOrb;
        
        public ArenaTeam(Shrine shrine)
        {
            Shrine = shrine;

            Int16 objectId = 0;

            switch (Shrine.Team)
            {
                case Team.Dragon:
                {
                    objectId = 28000;
                    break;
                }
                case Team.Gryphon:
                {
                    objectId = 28001;
                    break;
                }
                case Team.Pheonix:
                {
                    objectId = 28002;
                    break;
                }
            }

            ShrineOrb = new CTFOrb(Shrine.Team, objectId);
        }
    }
}
