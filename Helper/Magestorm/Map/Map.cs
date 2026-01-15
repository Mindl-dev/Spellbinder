using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public class Map
    {
        public Int32 GridId;

        public byte[,] RoomGrid = new byte[128, 128];

        public Dictionary<int, RoomDefinition> Rooms = new Dictionary<int, RoomDefinition>();
        public struct RoomDefinition
        {
            public int ID;
            public int MinX, MaxX, MinY, MaxY;
            public uint ViewBitmask; // 32-bit mask: (1 << ViewID)

            /// <summary>
            /// Checks if a specific ViewID is active for this room.
            /// </summary>
            public bool IsViewActive(int viewId)
            {
                return (ViewBitmask & (1u << viewId)) != 0;
            }
        }
        public Map()
        {
           
        }

        public Map(Map map)
        {
          
        }

        public static RoomDefinition? GetRoomAt(float worldX, float worldY, Grid grid)
        {
            // Convert world units to 64-unit grid blocks (shl 6 equivalent)
            int gx = (int)worldX >> 6;
            int gy = (int)worldY >> 6;

            if (gx >= 0 && gx < 128 && gy >= 0 && gy < 128)
            {
                Map map = grid.Maps.FindById(grid.GridId);

                byte roomId = map.RoomGrid[gx, gy];
                if (roomId != 0 && map.Rooms.TryGetValue(roomId, out var room))
                {
                    return room;
                }
            }
            return null;
        }

    }
}
