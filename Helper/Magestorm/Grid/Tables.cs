using Helper;
using System;
using System.CodeDom;

namespace Helper
{
    public class Tables
    {
        public Int32 GridId;
        public byte[,] TileId = new byte[128, 128];                   //byte_6f4e90
        //public byte[,] table1 = new byte[128, 128];                   //byte_873f30
        public byte[,] FloorDetail = new byte[128, 128];                //byte_8699a0
        public short[,] FloorZ= new short[128, 128];                //word_87ff30
        public short[,] WallHeight = new short[128, 128];           //word_7c740c
        public short[,] CeilingTable = new short[128, 128];         //word_7e8db8
        public byte[,] BlockTypeTable = new byte[128, 128];         //byte_7b4c90
        public short[,] table7 = new short[128, 128];               //word_6e4e8c
        public byte[,] RawTileFlagTable = new byte[128, 128];       //byte_7f0db8
        public byte[,] DetailMapIndexTable = new byte[128, 128];    //byte_7f4f48
        public byte[,] LogicFlag = new byte[128, 128];           //byte_6f0e90
        public byte[,] SpecialCollision = new byte[128, 128];                //byte_7d0690
        public byte[,] PathingFlags = new byte[128, 128];                //byte_7a8c90
        //public byte[,] table13 = new byte[128, 128];                //byte_6e0e8c
        public short[,] HighBoxZ = new short[128, 128];           //word_6cce88
        public short[,] table15 = new short[128, 128];              //word_7bf408
        public byte[,] ModifierTemplateTable = new byte[128, 128];  //byte_7e4db8
        public byte[,] ThinCollision = new byte[128, 128];     //byte_87bf30
        public byte[,] FinalModifier = new byte[128, 128];          //byte_7acc90

        public int[] SlopeProperty = new int[256]; // dword_86Daa0
        public short[,] HeightLibrary = new short[256, 64]; // word_86d9a0

        public readonly sbyte[] SlopeX = new sbyte[] { 0, 20, -32, 0, 0, 10, -16, 0, 0, -32, 20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //byte_7bcc98
        public readonly sbyte[] SlopeY = new sbyte[] { 0, 0, 0, 20, -32, 0, 0, 10, -16, 20, 20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //byte_7bcc99
        public readonly sbyte[] SlopeZ = new sbyte[] { 0, 0, 20, 0, 20, 0, 10, 0, 10, 20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //byte_7bcc9a

        public readonly sbyte[] SlopeNormalTable = new sbyte[] { 32, -32, 32, 32, -32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 0, 0, 0, -16, 0, 16, 0, 0, 16, 0, 0, 0, -16, 16, 0 };

        public byte[][,] SubPixelLibrary; // byte_7fbfc0 [StampIndex][Y, X]

        public int[,] SpecialCollisionTable = new int[128, 128]; //byte_6f8e90
        //public int[,] MaterialEffectTable = new int[128, 128]; //byte_7e0db8

        public byte[,] FinalCollisionMask = new byte[128, 128]; //byte_7b0c90

        public int[,] LedgeFlagWest = new int[128, 128];
        public int[,] LedgeFlagEast = new int[128, 128];
        public int[,] LedgeFlagNorth = new int[128, 128];
        public int[,] LedgeFlagSouth = new int[128, 128];

        /*public void BakeCollisionMask(Tables tables, Grid grid)
        {
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    byte mask = 0;
                    int blockIdx = (y << 7) + x;

                    if (blockIdx >= grid.GridBlocks.Count) continue;
                    
                    var block = grid.GridBlocks[blockIdx];

                    // 1. Basic Table Checks (Matching sub_462790)
                    // Bit 0: Static Block Geometry (Slot 6)
                    if (tables.LedgeFlagWest[x, y] == 1 || tables.LedgeFlagEast[x, y] == 1 ||
                        tables.LedgeFlagNorth[x, y] == 1 || tables.LedgeFlagSouth[x, y] == 1 ||
                        tables.BlockTypeTable[x, y] != 0)
                    {
                        mask |= 0x01; // Enable Sampler
                    }
                    // Bit 1: Modifier Template (Micro-geometry overrides)
                    // This ensures GridHeightLookup's 'mIdx' logic is respected
                    if (tables.ModifierTemplateTable[x, y] != 0) mask |= 0x02;
                    // Bit 2: Sub-Pixel Pattern (Rocks/Pyramids from Stamps)
                    if (tables.DetailMapIndexTable[x, y] != 0) mask |= 0x04;
                    // Bit 3: Raw Thin Data (Fences/Grated Walls)
                    if (tables.LogicFlag[x, y] != 0) mask |= 0x08;
                    // Bit 4: Ceiling / Sloped Roof
                    // Note: 1024 is the 'Sky' height. Only set bit if it's lower.
                    if (tables.CeilingTable[x, y] > 0 && tables.CeilingTable[x, y] < 1024) mask |= 0x10;
                    // Bit 5: Baked Thin Wall (Calculated Directional Collisions)
                    if (tables.ThinCollision[x, y] != 0) mask |= 0x20;
                    // Bit 6: Physics Flags (Solid/Water/Lava)
                    if (tables.SpecialCollisionTable[x, y] != 0) mask |= 0x40;

                    // 2. Neighbor Edge Detection (The logic at loc_46286D)
                    // Check Horizontal Neighbor (X + 1)
                    if (x < 127)
                    {
                        if (tables.BaseFloorZ[x, y] != tables.BaseFloorZ[x + 1, y]) mask |= 0x01;
                        if (tables.WallHeight[x, y] != tables.WallHeight[x + 1, y]) mask |= 0x02;
                    }

                    // Check Vertical Neighbor (Y + 1)
                    if (y < 127)
                    {
                        if (tables.FloorGlobalOffset[x, y] != tables.FloorGlobalOffset[x, y + 1]) mask |= 0x01;
                        if (tables.CeilingGlobalOffset[x, y] != tables.CeilingGlobalOffset[x, y + 1]) mask |= 0x02;
                    }

                    // 3. Final Skybox Safety
                    if (tables.FloorGlobalOffset[x, y] >= 1024 || tables.CeilingGlobalOffset[x, y] >= 1024)
                    {
                        // If it's sky, we often clear the wall bits so projectiles don't hit the "void"
                        mask &= 0xFE;
                    }

                    tables.CollisionMaskTable[x, y] = mask;
                }
            }
        }*/
    }
}
