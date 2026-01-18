using Helper.Timing;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OrientedBoundingBox = Helper.Math.OrientedBoundingBox;

namespace Helper
{
    public class Grid
    {
        public Shrine PheonixShrine;
        public Shrine DragonShrine;
        public Shrine GryphonShrine;

        public String GameName;
        public String GridFilename;
        public String RoomFilename;
        public String GeometryFilename;
        public String SubPixelFilename;
        public String ObjectsFilename;
        public String FloorGlobalOffsetFilename;
        public String CeilingGlobalOffsetFilename;
        public String CeilingTableFilename;
        public String BlockTypeTableFilename;
        public String AllGridDataFilename;
        public Int32 GridId;
        public Int32 MapId;
        public Byte MaxPlayers;
        public String MiscFilename;
        public String Name;  
        public String ShortGameName;
        public Int16 TimeLimit;
        public String TriggerFilename;
        public String WorldFilename;
        public Single ExpBonus;

        private byte[] _rawTerrainData;

        public GridBlockCollection GridBlocks;
        public GridObjectCollection GridObjects;
        public GridObjectDefinitionCollection GridObjectDefinitions;
        public ThinCollection Thins;
        public TileCollection Tiles;
        public TriggerCollection Triggers;
        public PoolCollection Pools;
        public TablesCollection Tables;
        public MapsCollection Maps;

        public int[] SlopeProperty = new int[256]; // dword_86Daa0
        public short[,] HeightLibrary = new short[256, 64]; // word_86d9a0

        public static readonly sbyte[] SlopeX = new sbyte[] { 0, 20, -32, 0, 0, 10, -16, 0, 0, -32, 20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //byte_7bcc98
        public static readonly sbyte[] SlopeY = new sbyte[] { 0, 0, 0, 20, -32, 0, 0, 10, -16, 20, 20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //byte_7bcc99
        public static readonly sbyte[] SlopeZ = new sbyte[] { 0, 0, 20, 0, 20, 0, 10, 0, 10, 20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //byte_7bcc9a

        public static readonly sbyte[] SlopeNormalTable = new sbyte[] { 32, -32, 32, 32, -32, 32, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 0, 0, 0, -16, 0, 16, 0, 0, 16, 0, 0, 0, -16, 16, 0 };

        public byte[][,] SubPixelLibrary; // byte_7fbfc0 [StampIndex][Y, X]

        public ListCollection<Int16> Links = new ListCollection<Int16>();

        public HashSet<long> HollowZones = new HashSet<long>();

        public static readonly HashSet<long> HollowZonesGrid0 = new HashSet<long> { Pack(27, 63), Pack(27, 64), Pack(27, 65), Pack(28, 63), Pack(28, 64), Pack(28, 65), Pack(29, 63), Pack(29, 64), Pack(29, 65), Pack(60, 36), Pack(60, 37), Pack(60, 38), Pack(61, 36), Pack(61, 37), Pack(61, 38), Pack(62, 36), Pack(62, 37), Pack(62, 38), Pack(93, 63), Pack(93, 64), Pack(93, 65), Pack(94, 63), Pack(94, 64), Pack(94, 65), Pack(95, 63), Pack(95, 64), Pack(95, 65), Pack(1, 2), Pack(1, 3), Pack(1, 4), Pack(2, 2), Pack(2, 3), Pack(2, 4), Pack(3, 2), Pack(3, 3), Pack(3, 4), Pack(32, 48), Pack(32, 49), Pack(32, 50), Pack(33, 48), Pack(33, 49), Pack(33, 50), Pack(34, 48), Pack(34, 49), Pack(34, 50), Pack(43, 63), Pack(43, 64), Pack(43, 65), Pack(44, 63), Pack(44, 64), Pack(44, 65), Pack(45, 63), Pack(45, 64), Pack(45, 65), Pack(47, 41), Pack(47, 42), Pack(47, 43), Pack(48, 41), Pack(48, 42), Pack(48, 43), Pack(48, 79), Pack(48, 80), Pack(48, 81), Pack(49, 41), Pack(49, 42), Pack(49, 43), Pack(49, 79), Pack(49, 80), Pack(49, 81), Pack(50, 79), Pack(50, 80), Pack(50, 81), Pack(56, 54), Pack(56, 55), Pack(56, 56), Pack(57, 54), Pack(57, 55), Pack(57, 56), Pack(58, 54), Pack(58, 55), Pack(58, 56), Pack(74, 79), Pack(74, 80), Pack(74, 81), Pack(75, 47), Pack(75, 48), Pack(75, 49), Pack(75, 79), Pack(75, 80), Pack(75, 81), Pack(76, 47), Pack(76, 48), Pack(76, 49), Pack(76, 79), Pack(76, 80), Pack(76, 81), Pack(77, 47), Pack(77, 48), Pack(77, 49), Pack(79, 63), Pack(79, 64), Pack(79, 65), Pack(80, 63), Pack(80, 64), Pack(80, 65), Pack(81, 63), Pack(81, 64), Pack(81, 65), Pack(91, 48), Pack(91, 49), Pack(91, 50), Pack(92, 48), Pack(92, 49), Pack(92, 50), Pack(93, 48), Pack(93, 49), Pack(93, 50) };
        
        public static readonly HashSet<long> HollowZonesGrid1 = new HashSet<long> { Pack(14, 52), Pack(14, 53), Pack(14, 54), Pack(15, 52), Pack(15, 53), Pack(15, 54), Pack(16, 52), Pack(16, 53), Pack(16, 54), Pack(64, 107), Pack(64, 108), Pack(64, 109), Pack(65, 107), Pack(65, 108), Pack(65, 109), Pack(66, 107), Pack(66, 108), Pack(66, 109), Pack(112, 52), Pack(112, 53), Pack(112, 54), Pack(113, 52), Pack(113, 53), Pack(113, 54), Pack(114, 52), Pack(114, 53), Pack(114, 54), Pack(1, 2), Pack(1, 3), Pack(1, 4), Pack(2, 2), Pack(2, 3), Pack(2, 4), Pack(3, 2), Pack(3, 3), Pack(3, 4), Pack(26, 53), Pack(26, 54), Pack(26, 55), Pack(27, 53), Pack(27, 54), Pack(27, 55), Pack(28, 53), Pack(28, 54), Pack(28, 55), Pack(50, 41), Pack(50, 42), Pack(50, 43), Pack(50, 65), Pack(50, 66), Pack(50, 67), Pack(50, 80), Pack(50, 81), Pack(50, 82), Pack(51, 41), Pack(51, 42), Pack(51, 43), Pack(51, 65), Pack(51, 66), Pack(51, 67), Pack(51, 80), Pack(51, 81), Pack(51, 82), Pack(52, 41), Pack(52, 42), Pack(52, 43), Pack(52, 65), Pack(52, 66), Pack(52, 67), Pack(52, 80), Pack(52, 81), Pack(52, 82), Pack(64, 95), Pack(64, 96), Pack(64, 97), Pack(65, 95), Pack(65, 96), Pack(65, 97), Pack(66, 95), Pack(66, 96), Pack(66, 97), Pack(78, 41), Pack(78, 42), Pack(78, 43), Pack(78, 65), Pack(78, 66), Pack(78, 67), Pack(78, 80), Pack(78, 81), Pack(78, 82), Pack(79, 41), Pack(79, 42), Pack(79, 43), Pack(79, 65), Pack(79, 66), Pack(79, 67), Pack(79, 80), Pack(79, 81), Pack(79, 82), Pack(80, 41), Pack(80, 42), Pack(80, 43), Pack(80, 65), Pack(80, 66), Pack(80, 67), Pack(80, 80), Pack(80, 81), Pack(80, 82), Pack(100, 53), Pack(100, 54), Pack(100, 55), Pack(101, 53), Pack(101, 54), Pack(101, 55), Pack(102, 53), Pack(102, 54), Pack(102, 55) };
        
        public static readonly HashSet<long> HollowZonesGrid2 = new HashSet<long> { Pack(10, 72), Pack(10, 73), Pack(10, 74), Pack(11, 72), Pack(11, 73), Pack(11, 74), Pack(12, 72), Pack(12, 73), Pack(12, 74), Pack(63, 15), Pack(63, 16), Pack(63, 17), Pack(64, 15), Pack(64, 16), Pack(64, 17), Pack(65, 15), Pack(65, 16), Pack(65, 17), Pack(119, 75), Pack(119, 76), Pack(119, 77), Pack(120, 75), Pack(120, 76), Pack(120, 77), Pack(121, 75), Pack(121, 76), Pack(121, 77), Pack(1, 2), Pack(1, 3), Pack(1, 4), Pack(2, 2), Pack(2, 3), Pack(2, 4), Pack(3, 2), Pack(3, 3), Pack(3, 4), Pack(35, 76), Pack(35, 77), Pack(35, 78), Pack(36, 76), Pack(36, 77), Pack(36, 78), Pack(37, 76), Pack(37, 77), Pack(37, 78), Pack(44, 49), Pack(44, 50), Pack(44, 51), Pack(45, 49), Pack(45, 50), Pack(45, 51), Pack(46, 49), Pack(46, 50), Pack(46, 51), Pack(46, 98), Pack(46, 99), Pack(46, 100), Pack(47, 98), Pack(47, 99), Pack(47, 100), Pack(48, 98), Pack(48, 99), Pack(48, 100), Pack(49, 72), Pack(49, 73), Pack(49, 74), Pack(50, 72), Pack(50, 73), Pack(50, 74), Pack(51, 72), Pack(51, 73), Pack(51, 74), Pack(63, 27), Pack(63, 28), Pack(63, 29), Pack(63, 62), Pack(63, 63), Pack(63, 64), Pack(64, 27), Pack(64, 28), Pack(64, 29), Pack(64, 62), Pack(64, 63), Pack(64, 64), Pack(65, 27), Pack(65, 28), Pack(65, 29), Pack(65, 62), Pack(65, 63), Pack(65, 64), Pack(67, 48), Pack(67, 49), Pack(67, 50), Pack(68, 48), Pack(68, 49), Pack(68, 50), Pack(68, 101), Pack(68, 102), Pack(68, 103), Pack(69, 48), Pack(69, 49), Pack(69, 50), Pack(69, 101), Pack(69, 102), Pack(69, 103), Pack(70, 101), Pack(70, 102), Pack(70, 103), Pack(78, 64), Pack(78, 65), Pack(78, 66), Pack(79, 64), Pack(79, 65), Pack(79, 66), Pack(80, 64), Pack(80, 65), Pack(80, 66), Pack(82, 93), Pack(82, 94), Pack(82, 95), Pack(83, 93), Pack(83, 94), Pack(83, 95), Pack(84, 93), Pack(84, 94), Pack(84, 95), Pack(88, 52), Pack(88, 53), Pack(88, 54), Pack(89, 52), Pack(89, 53), Pack(89, 54), Pack(90, 52), Pack(90, 53), Pack(90, 54), Pack(93, 78), Pack(93, 79), Pack(93, 80), Pack(94, 78), Pack(94, 79), Pack(94, 80), Pack(95, 78), Pack(95, 79), Pack(95, 80), Pack(108, 76), Pack(108, 77), Pack(108, 78), Pack(109, 76), Pack(109, 77), Pack(109, 78), Pack(110, 76), Pack(110, 77), Pack(110, 78) };

        public static readonly HashSet<long> HollowZonesGrid3 = new HashSet<long> { Pack(26, 50), Pack(26, 51), Pack(26, 52), Pack(27, 50), Pack(27, 51), Pack(27, 52), Pack(28, 50), Pack(28, 51), Pack(28, 52), Pack(57, 106), Pack(57, 107), Pack(57, 108), Pack(58, 106), Pack(58, 107), Pack(58, 108), Pack(59, 106), Pack(59, 107), Pack(59, 108), Pack(94, 57), Pack(94, 58), Pack(94, 59), Pack(95, 57), Pack(95, 58), Pack(95, 59), Pack(96, 57), Pack(96, 58), Pack(96, 59), Pack(1, 2), Pack(1, 3), Pack(1, 4), Pack(2, 2), Pack(2, 3), Pack(2, 4), Pack(3, 2), Pack(3, 3), Pack(3, 4), Pack(18, 57), Pack(18, 58), Pack(18, 59), Pack(19, 57), Pack(19, 58), Pack(19, 59), Pack(20, 57), Pack(20, 58), Pack(20, 59), Pack(26, 32), Pack(26, 33), Pack(26, 34), Pack(27, 32), Pack(27, 33), Pack(27, 34), Pack(27, 43), Pack(27, 44), Pack(27, 45), Pack(28, 32), Pack(28, 33), Pack(28, 34), Pack(28, 43), Pack(28, 44), Pack(28, 45), Pack(29, 43), Pack(29, 44), Pack(29, 45), Pack(35, 57), Pack(35, 58), Pack(35, 59), Pack(36, 57), Pack(36, 58), Pack(36, 59), Pack(37, 57), Pack(37, 58), Pack(37, 59), Pack(45, 27), Pack(45, 28), Pack(45, 29), Pack(46, 27), Pack(46, 28), Pack(46, 29), Pack(47, 27), Pack(47, 28), Pack(47, 29), Pack(49, 113), Pack(49, 114), Pack(49, 115), Pack(50, 113), Pack(50, 114), Pack(50, 115), Pack(51, 113), Pack(51, 114), Pack(51, 115), Pack(53, 39), Pack(53, 40), Pack(53, 41), Pack(54, 39), Pack(54, 40), Pack(54, 41), Pack(55, 39), Pack(55, 40), Pack(55, 41), Pack(56, 23), Pack(56, 24), Pack(56, 25), Pack(57, 23), Pack(57, 24), Pack(57, 25), Pack(57, 88), Pack(57, 89), Pack(57, 90), Pack(58, 23), Pack(58, 24), Pack(58, 25), Pack(58, 55), Pack(58, 56), Pack(58, 57), Pack(58, 88), Pack(58, 89), Pack(58, 90), Pack(58, 98), Pack(58, 99), Pack(58, 100), Pack(59, 55), Pack(59, 56), Pack(59, 57), Pack(59, 88), Pack(59, 89), Pack(59, 90), Pack(59, 98), Pack(59, 99), Pack(59, 100), Pack(60, 55), Pack(60, 56), Pack(60, 57), Pack(60, 98), Pack(60, 99), Pack(60, 100), Pack(66, 113), Pack(66, 114), Pack(66, 115), Pack(67, 40), Pack(67, 41), Pack(67, 42), Pack(67, 113), Pack(67, 114), Pack(67, 115), Pack(68, 40), Pack(68, 41), Pack(68, 42), Pack(68, 113), Pack(68, 114), Pack(68, 115), Pack(69, 40), Pack(69, 41), Pack(69, 42), Pack(72, 25), Pack(72, 26), Pack(72, 27), Pack(73, 25), Pack(73, 26), Pack(73, 27), Pack(74, 25), Pack(74, 26), Pack(74, 27), Pack(86, 64), Pack(86, 65), Pack(86, 66), Pack(87, 64), Pack(87, 65), Pack(87, 66), Pack(88, 64), Pack(88, 65), Pack(88, 66), Pack(94, 39), Pack(94, 40), Pack(94, 41), Pack(95, 39), Pack(95, 40), Pack(95, 41), Pack(95, 49), Pack(95, 50), Pack(95, 51), Pack(96, 39), Pack(96, 40), Pack(96, 41), Pack(96, 49), Pack(96, 50), Pack(96, 51), Pack(97, 49), Pack(97, 50), Pack(97, 51), Pack(103, 64), Pack(103, 65), Pack(103, 66), Pack(104, 64), Pack(104, 65), Pack(104, 66), Pack(105, 64), Pack(105, 65), Pack(105, 66) };

        public GridObject[] ObjectMap = new GridObject[128 * 128];
        public static long Pack(int x, int y)
        {
            return ((long)x << 32) | (uint)y;
        }
        public struct CollisionResult
        {
            public bool Hit;
            public TileBlock HitTileBlock;
            public Vector3 Normal;

            public CollisionResult(bool hit, TileBlock tileBlock = null, Vector3 normal = default)
            {
                Hit = hit;
                HitTileBlock = tileBlock;
                Normal = normal;
            }
        }
        public Grid()
        {
            GridBlocks = new GridBlockCollection(true);
            GridObjects = new GridObjectCollection();
            GridObjectDefinitions = new GridObjectDefinitionCollection();
            Thins = new ThinCollection();
            Tiles = new TileCollection(true);
            Triggers = new TriggerCollection(true);
            Pools = new PoolCollection();
            Tables = new TablesCollection();
            Maps = new MapsCollection();
            HollowZones = new HashSet<long>();
        }
        public Grid(Grid grid) : this()
        {
            GridBlocks = grid.GridBlocks;
            Thins = grid.Thins;
            Tiles = grid.Tiles;
            Triggers = new TriggerCollection(false);

            foreach (Trigger t in grid.Triggers)
            {
                Trigger nTrigger = new Trigger
                {
                    Duration = null,
                    Cooldown = new Interval(2000, false),
                    Enabled = t.Enabled,
                    EndAngle = t.EndAngle,
                    InitialState = t.InitialState,
                    IsFromValhalla = t.IsFromValhalla,
                    MaxAngleRate = t.MaxAngleRate,
                    MaxRate = t.MaxRate,
                    MoveCeiling = t.MoveCeiling,
                    MoveFloor = t.MoveFloor,
                    MoveRooftop = t.MoveRooftop,
                    NextTrigger = t.NextTrigger,
                    NextTriggerTiming = t.NextTriggerTiming,
                    OffHeight = t.OffHeight,
                    OffSound = t.OffSound,
                    OffText = t.OffText,
                    OnHeight = t.OnHeight,
                    OnSound = t.OnSound,
                    OnText = t.OnText,
                    Position = new Vector3(t.Position.X, t.Position.Y, t.Position.Z),
                    Random = t.Random,
                    ResetTimer = t.ResetTimer,
                    SlideAmount = t.SlideAmount,
                    SlideAxis = t.SlideAxis,
                    Speed = t.Speed,
                    StartAngle = t.StartAngle,
                    CurrentState = TriggerState.Inactive,
                    Team = t.Team,
                    TextureOff = t.TextureOff,
                    TextureOn = t.TextureOn,
                    TriggerId = t.TriggerId,
                    TriggerType = t.TriggerType,
                    X0 = t.X0,
                    X1 = t.X1,
                    X2 = t.X2,
                    X3 = t.X3,
                    X4 = t.X4,
                    Y0 = t.Y0,
                    Y1 = t.Y1,
                    Y2 = t.Y2,
                    Y3 = t.Y3,
                    Y4 = t.Y4
                };

                Triggers.Add(nTrigger);
            }

            PheonixShrine = new Shrine(grid.PheonixShrine.Team, grid.PheonixShrine.ShrineId, grid.PheonixShrine.Power, grid.PheonixShrine.CurrentBias, grid.PheonixShrine.Links);
            DragonShrine = new Shrine(grid.DragonShrine.Team, grid.DragonShrine.ShrineId, grid.DragonShrine.Power, grid.DragonShrine.CurrentBias, grid.DragonShrine.Links);
            GryphonShrine = new Shrine(grid.GryphonShrine.Team, grid.GryphonShrine.ShrineId, grid.GryphonShrine.Power, grid.GryphonShrine.CurrentBias, grid.GryphonShrine.Links);

            GameName = grid.GameName;
            GridFilename = grid.GridFilename;
            RoomFilename = grid.RoomFilename;
            GeometryFilename = grid.GeometryFilename;
            SubPixelFilename = grid.SubPixelFilename;
            CeilingGlobalOffsetFilename = grid.CeilingGlobalOffsetFilename;
            FloorGlobalOffsetFilename = grid.FloorGlobalOffsetFilename;
            BlockTypeTableFilename = grid.BlockTypeTableFilename;
            CeilingTableFilename = grid.CeilingTableFilename;
            AllGridDataFilename = grid.AllGridDataFilename;
            GridId = grid.GridId;
            MaxPlayers = grid.MaxPlayers;
            MiscFilename = grid.MiscFilename;
            Name = grid.Name;

            Pools = new PoolCollection();
            foreach (Pool p in grid.Pools)
            {
                Pools.Add(new Pool(p));
            }

            ShortGameName = grid.ShortGameName;
            TimeLimit = grid.TimeLimit;
            TriggerFilename = grid.TriggerFilename;
            WorldFilename = grid.WorldFilename;
            ExpBonus = grid.ExpBonus;

            Maps = grid.Maps;

            SubPixelLibrary = grid.SubPixelLibrary;

            _rawTerrainData = grid._rawTerrainData;

            if (grid.HollowZones != null)
            {
                this.HollowZones = new HashSet<long>(grid.HollowZones);
            }
            else
            {
                this.HollowZones = new HashSet<long>();
            }

            ObjectMap = grid.ObjectMap;

        }
        public Shrine GetShrineById(Byte shrineId)
        {
            if (DragonShrine.ShrineId == shrineId)
            {
                return DragonShrine;
            }

            if (PheonixShrine.ShrineId == shrineId)
            {
                return PheonixShrine;
            }

            if (GryphonShrine.ShrineId == shrineId)
            {
                return GryphonShrine;
            }

            return null;
        }
        public Shrine GetShrineByTeam(Team team)
        {
            switch (team)
            {
                case Team.Dragon:
                {
                    return DragonShrine;
                }
                case Team.Pheonix:
                {
                    return PheonixShrine;
                }
                case Team.Gryphon:
                {
                    return GryphonShrine;
                }
                default:
                {
                    return null;
                }
            }
        }
        private void LoadHollowZones(int id, LogBox logBox)
        {
            // 1. Point to your static data definitions
            HashSet<long> sourceData = null;

            switch (id)
            {
                case 0: sourceData = HollowZonesGrid0; break;
                case 1: sourceData = HollowZonesGrid1; break;
                case 2: sourceData = HollowZonesGrid2; break;
                case 3: sourceData = HollowZonesGrid3; break;
            }

            // 2. Critical Check: If the ID is wrong, we cannot continue safely.
            if (sourceData == null)
            {
                // Instead of leaving it null or using wrong data, we initialize 
                // it as an empty set to prevent the crash, but log a major error.
                this.HollowZones = new HashSet<long>();
                logBox.WriteMessage($"[ERROR] Grid ID {id} has no defined HollowZones! Physics will be broken.", System.Drawing.Color.Red);
                return;
            }

            // 3. Create a fresh instance for this specific Grid object
            // This ensures grid.HollowZones is NEVER null.
            this.HollowZones = new HashSet<long>(sourceData);
        }
        public static void LoadAllGrids(LogBox logBox)
		{
			String fName = String.Format("{0}\\Arenas.dat", Directory.GetCurrentDirectory());
			Int32 aCount = NativeMethods.GetPrivateProfileInt32("arenadefs", "numarenas", fName);

			logBox.WriteMessage(String.Format("Loading {0} Arenas...", aCount), System.Drawing.Color.Blue);

			Int16 i;
			for (i = 0; i < aCount; i++)
			{
				Grid grid = new Grid();
				if (!grid.Load(i, logBox))
				{
					logBox.WriteMessage(String.Format("Error loading Grid #{0}", i), System.Drawing.Color.Red);
					continue;
				}

                grid.GenerateAdjacencyFlags(grid);

                GridManager.Grids.Add(grid);
				logBox.WriteMessage(String.Format("Loaded Grid: {0} ({1})", grid.GameName, grid.Name), System.Drawing.Color.Green);

                /*if (grid.GridId == 0)
                {

                    for (int x = 29; x < 30; x++)
                    {
                        for (int y = 61; y < 70; y++)
                        { 
                            GridBlock block = grid.GridBlocks.GetBlockByLocation(x << 6, y << 6);

                            //int subX = (x >> 3) & 7; // 0-7
                            //int subY = (y >> 3) & 7; // 0-7

                            //int height = grid.GetFloorMeshHeight(block.BlockType, subX, subY);

                            int ceilingHeight = grid.GetCeilingHeight(x << 6, y << 6, 0, grid);

                            int floorHeight = grid.GetFloorHeight(x << 6, y << 6, 0, grid);

                            int ceilingZ = block.CeilingZ;

                            int highBoxZ = block.HighBoxZ;

                            logBox.WriteMessage($"DATA CHECK: GetCeilingHeight[x,y]={ceilingHeight} @ [{x},{y}]", System.Drawing.Color.Blue);
                            logBox.WriteMessage($"DATA CHECK: GetFloorHeight[x,y]={floorHeight} @ [{x},{y}]", System.Drawing.Color.Blue);
                            logBox.WriteMessage($"DATA CHECK: CeilingZ[x,y]={ceilingZ} @ [{x},{y}]", System.Drawing.Color.Blue);
                            logBox.WriteMessage($"DATA CHECK: HighBoxZ[x,y]={highBoxZ} @ [{x},{y}]", System.Drawing.Color.Blue);
                        }
                    }
                }*/

                Application.DoEvents();
			}

			logBox.WriteMessage(String.Format("{0} out of {1} Arenas loaded.", i, aCount), System.Drawing.Color.Blue);

        }

        public Boolean Load(Int32 gridId, LogBox logBox)
        {
            try
            {
                String arenaDatFilename = String.Format("{0}\\Arenas.dat", Directory.GetCurrentDirectory());
                const String gridDatLocation = "{0}\\Grids\\Grid{1:00}\\{2}";

                String keyName = String.Format("arena{0:00}", gridId);

                WorldFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "World.dat");
                GridFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "Grid.dat");
                RoomFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "roomview.txt");
                GeometryFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "Geometry.dat");
                SubPixelFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "SubPixel.dat");
                CeilingGlobalOffsetFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "CeilingGlobalOffset.dat");
                CeilingTableFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "CeilingTable.dat");
                FloorGlobalOffsetFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "FloorGlobalOffset.dat");
                BlockTypeTableFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "BlockTypeTable.dat");
                ObjectsFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "Objects.dat");
                MiscFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "Misc.dat");
                TriggerFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "Trigger.dat");
                AllGridDataFilename = String.Format(gridDatLocation, Directory.GetCurrentDirectory(), gridId, "allgriddata.bin");
                GridId = gridId;
                Name = NativeMethods.GetPrivateProfileString(keyName, "grid", arenaDatFilename);
                GameName = NativeMethods.GetPrivateProfileString(keyName, "name", arenaDatFilename);
                ShortGameName = NativeMethods.GetPrivateProfileString(keyName, "short_name", arenaDatFilename);
                MaxPlayers = NativeMethods.GetPrivateProfileByte(keyName, "maxplayers", arenaDatFilename);
                TimeLimit = NativeMethods.GetPrivateProfileInt16(keyName, "timelimit", arenaDatFilename);
                ExpBonus = NativeMethods.GetPrivateProfileSingle(keyName, "expbonus", arenaDatFilename);

                Pools = new PoolCollection();

                Int32 poolCount = NativeMethods.GetPrivateProfileInt32("earthblooddefs", "numearthblood", WorldFilename);
                for (Int32 x = 0; x < poolCount; x++)
                {
                    Pools.Add(new Pool(Convert.ToByte(x), NativeMethods.GetPrivateProfileInt16(String.Format("earthblood{0:00}", x), "power", WorldFilename), 100));
                }

                Int32 shrineCount = NativeMethods.GetPrivateProfileInt32("shrinedefs", "numshrines", WorldFilename);
                for (Int32 x = 0; x < shrineCount; x++)
                {
                    String shrineString = String.Format("shrine{0:00}", x);

                    Int16 power = NativeMethods.GetPrivateProfileInt16(shrineString, "power", WorldFilename);
                    Int16 bias = NativeMethods.GetPrivateProfileInt16(shrineString, "bias", WorldFilename);
                    Int16 link1 = NativeMethods.GetPrivateProfileInt16(shrineString, "link1", WorldFilename);
                    Int16 link2 = NativeMethods.GetPrivateProfileInt16(shrineString, "link2", WorldFilename);
                    Int16 link3 = NativeMethods.GetPrivateProfileInt16(shrineString, "link3", WorldFilename);

                    Links.Clear();

                    Links.Add(link1);
                    Links.Add(link2);
                    Links.Add(link3);

                    switch (NativeMethods.GetPrivateProfileString(shrineString, "alignment", WorldFilename))
                    {
                        case "chaos":
                            {
                                DragonShrine = new Shrine(Team.Dragon, (Byte)x, power, bias, Links);
                                break;
                            }

                        case "balance":
                            {
                                PheonixShrine = new Shrine(Team.Pheonix, (Byte)x, power, bias, Links);
                                break;
                            }

                        case "order":
                            {
                                GryphonShrine = new Shrine(Team.Gryphon, (Byte)x, power, bias, Links);
                                break;
                            }
                    }
                }

                Map map = new Map();
                map.GridId = gridId;

                LoadTriggers();
                LoadThins();
                LoadTiles();
                LoadObjectDefinitions();
                LoadGrid(true, logBox);
                LoadMapData(map);
                LoadHollowZones(gridId, logBox);

                Maps.Add(map);

            }
            catch (Exception e)
            {
                logBox.WriteMessage(String.Format("{0}", e.ToString()), System.Drawing.Color.Blue);
                return false;
            }

            return true;
        }
        public Boolean Save()
        {
            using (FileStream gridStream = new FileStream(GridFilename, FileMode.Create, FileAccess.ReadWrite))
            {
                for (Int32 i = 1; i <= 16384; i++)
                {
                    Byte[] gridBytes = BitConverter.GetBytes(GridBlocks[i].Unknown0);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].LowBoxTopMod);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].LowSidesTextureId);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].LowTopTextureId);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].HighTextureId);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].LowBoxTopZ);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].MidBoxBottomZ);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].MidBoxTopZ);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].TileId);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].HighBoxBottomZ);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].BlockFlags);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes((Int16)GridBlocks[i].LowTopShape);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes((Int16)GridBlocks[i].MidBottomShape);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].MidSidesTextureId);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].MidTopTextureId);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].CeilingTextureId);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].Unknown16);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].Unknown17);
                    gridStream.Write(gridBytes, 0, 2);
                    gridBytes = BitConverter.GetBytes(GridBlocks[i].Unknown18);
                    gridStream.Write(gridBytes, 0, 2);
                }

                gridStream.Write(new byte[4], 0, 4);

                for (Int32 i = 1; i <= 997; i++)
                {
                    Byte[] gridBytes = BitConverter.GetBytes(GridObjects[i].ObjectId);
                    gridStream.Write(gridBytes, 0, 4);
                    gridBytes = BitConverter.GetBytes(GridObjects[i].X);
                    gridStream.Write(gridBytes, 0, 4);
                    gridBytes = BitConverter.GetBytes(GridObjects[i].Y);
                    gridStream.Write(gridBytes, 0, 4);
                    gridBytes = BitConverter.GetBytes(GridObjects[i].Z);
                    gridStream.Write(gridBytes, 0, 4);
                }
            }

            using (FileStream objectStream = new FileStream(ObjectsFilename, FileMode.Create, FileAccess.ReadWrite))
            {
                for (Int32 i = 1; i <= 139; i++)
                {
                    Byte[] objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].DefinitionId);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].ImageId);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk3);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk4);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk5);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk6);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk7);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk8);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk9);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk10);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk11);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unused1);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk12);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk13);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk14);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk15);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk16);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk17);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk18);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unused2);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unused3);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk19);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk20);
                    objectStream.Write(objectBytes, 0, 4);
                    objectBytes = BitConverter.GetBytes(GridObjectDefinitions[i].Unk21);
                    objectStream.Write(objectBytes, 0, 4);
                    objectStream.Write(Encoding.UTF8.GetBytes(GridObjectDefinitions[i].Identifier), 0, 20);
                }
            }

            return true;
        }

        private void LoadTriggers()
        {
            Int32 numtriggers = NativeMethods.GetPrivateProfileInt32("triggerdefs", "numtriggers", TriggerFilename);

            for (Int16 i = 1; i <= numtriggers; i++)
            {
                String keyName = String.Format("trigger{0:00}", i);
                String stype = NativeMethods.GetPrivateProfileString(keyName, "type", TriggerFilename);

                Trigger trigger = new Trigger
                {
                    TriggerId = i,
                    TextureOff = NativeMethods.GetPrivateProfileInt32(keyName, "texture_off", TriggerFilename),
                    TextureOn = NativeMethods.GetPrivateProfileInt32(keyName, "texture_on", TriggerFilename),
                    ResetTimer = NativeMethods.GetPrivateProfileInt32(keyName, "reset_timer", TriggerFilename),
                    Enabled = NativeMethods.GetPrivateProfileBoolean(keyName, "enabled", TriggerFilename),
                    InitialState = (TriggerState)NativeMethods.GetPrivateProfileInt32(keyName, "initial_state", TriggerFilename),
                    NextTrigger = NativeMethods.GetPrivateProfileInt32(keyName, "next_trigger", TriggerFilename),
                    OnSound = NativeMethods.GetPrivateProfileInt32(keyName, "on_sound", TriggerFilename),
                    OffSound = NativeMethods.GetPrivateProfileInt32(keyName, "off_sound", TriggerFilename)
                };

                switch (stype)
                {
                    case "door":
                        {
                            trigger.TriggerType = TriggerType.Door;
                            trigger.SlideAxis = NativeMethods.GetPrivateProfileInt32(keyName, "slide_axis", TriggerFilename);
                            trigger.SlideAmount = NativeMethods.GetPrivateProfileInt32(keyName, "slide_amount", TriggerFilename);
                            trigger.MaxRate = NativeMethods.GetPrivateProfileInt32(keyName, "max_rate", TriggerFilename);
                            trigger.StartAngle = NativeMethods.GetPrivateProfileInt32(keyName, "start_angle", TriggerFilename);
                            trigger.EndAngle = NativeMethods.GetPrivateProfileInt32(keyName, "end_angle", TriggerFilename);
                            break;
                        }

                    case "elevator":
                        {
                            trigger.TriggerType = TriggerType.Elevator;
                            trigger.X1 = NativeMethods.GetPrivateProfileInt32(keyName, "x1", TriggerFilename);
                            trigger.Y1 = NativeMethods.GetPrivateProfileInt32(keyName, "y1", TriggerFilename);
                            trigger.X2 = NativeMethods.GetPrivateProfileInt32(keyName, "x2", TriggerFilename);
                            trigger.Y2 = NativeMethods.GetPrivateProfileInt32(keyName, "y2", TriggerFilename);
                            trigger.OffHeight = NativeMethods.GetPrivateProfileInt32(keyName, "off_height", TriggerFilename);
                            trigger.OnHeight = NativeMethods.GetPrivateProfileInt32(keyName, "on_height", TriggerFilename);
                            trigger.Speed = NativeMethods.GetPrivateProfileInt32(keyName, "speed", TriggerFilename);
                            trigger.MoveCeiling = NativeMethods.GetPrivateProfileInt32(keyName, "move_ceiling", TriggerFilename);
                            trigger.MoveRooftop = NativeMethods.GetPrivateProfileInt32(keyName, "move_rooftop", TriggerFilename);
                            trigger.MoveFloor = NativeMethods.GetPrivateProfileInt32(keyName, "move_floor", TriggerFilename);
                            break;
                        }
                    case "teleport":
                        {
                            trigger.TriggerType = TriggerType.Teleport;
                            trigger.Random = NativeMethods.GetPrivateProfileInt32(keyName, "random", TriggerFilename);
                            trigger.Team = NativeMethods.GetPrivateProfileInt32(keyName, "team", TriggerFilename);
                            trigger.X0 = NativeMethods.GetPrivateProfileInt32(keyName, "x0", TriggerFilename);
                            trigger.Y0 = NativeMethods.GetPrivateProfileInt32(keyName, "y0", TriggerFilename);
                            trigger.X1 = NativeMethods.GetPrivateProfileInt32(keyName, "x1", TriggerFilename);
                            trigger.Y1 = NativeMethods.GetPrivateProfileInt32(keyName, "y1", TriggerFilename);
                            trigger.X2 = NativeMethods.GetPrivateProfileInt32(keyName, "x2", TriggerFilename);
                            trigger.Y2 = NativeMethods.GetPrivateProfileInt32(keyName, "y2", TriggerFilename);
                            trigger.X3 = NativeMethods.GetPrivateProfileInt32(keyName, "x3", TriggerFilename);
                            trigger.Y3 = NativeMethods.GetPrivateProfileInt32(keyName, "y3", TriggerFilename);
                            trigger.X4 = NativeMethods.GetPrivateProfileInt32(keyName, "x4", TriggerFilename);
                            trigger.Y4 = NativeMethods.GetPrivateProfileInt32(keyName, "y4", TriggerFilename);
                            trigger.IsFromValhalla = NativeMethods.GetPrivateProfileBoolean(keyName, "valhalla", TriggerFilename);
                            break;
                        }
                    case "null":
                        {
                            trigger.TriggerType = TriggerType.Lever;
                            trigger.OnText = NativeMethods.GetPrivateProfileString(keyName, "on_text", TriggerFilename);
                            trigger.OffText = NativeMethods.GetPrivateProfileString(keyName, "off_text", TriggerFilename);

                            break;
                        }
                }

                trigger.CurrentState = trigger.InitialState;
                trigger.Position = new Vector3(trigger.X1, trigger.Y1, trigger.OffHeight);
                trigger.Duration = null;

                Triggers.Add(trigger);
            }
        }
        private void LoadThins()
        {
            FileStream thinBuffer = File.OpenRead(MiscFilename);

            for (Int32 i = 1; i <= 250; i++)
            {
                Int32 pos = 0;

                Byte[] thinBytes = new Byte[92];
                thinBuffer.Read(thinBytes, 0, 92);

                Thin thin = new Thin
                                {
                                    ThinId = i,
                                    Unknown0 = BitConverter.ToInt32(thinBytes, 0),
                                    Unknown1 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Unknown2 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Unknown3 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Unknown4 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    X1 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Y1 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    X2 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Y2 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    TextureId = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Unknown10 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Tall = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Unknown12 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Unknown13 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Unknown14 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Unknown15 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    TriggerId = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Unknown17 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Unknown18 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Z = BitConverter.ToInt32(thinBytes, pos += 4),
                                    Unknown20 = BitConverter.ToInt32(thinBytes, pos += 4),
                                    BlockPlayers = BitConverter.ToInt32(thinBytes, pos += 4) > 0,
                                    BlockProjectiles = BitConverter.ToInt32(thinBytes, pos) > 0
                                };

                thin.BoundingBox = new OrientedBoundingBox(new Vector3(thin.X1, thin.Y1, thin.Z), new Vector3(thin.X2, thin.Y2, thin.Z), new Vector3(0, 0, thin.Tall));

                Thins.Add(thin);
            }

            thinBuffer.Close();
        }
        private void LoadTiles()
        {
            FileStream tileBuffer = File.OpenRead(MiscFilename);
            tileBuffer.Seek(0x0BF68, SeekOrigin.Begin);

            for (Int32 i = 1; i <= 100; i++)
            {
                Byte[] tileBytes = new Byte[256];
                tileBuffer.Read(tileBytes, 0, 256);

                Tile tile = new Tile(i);

                Int32 pos = 0;

                for (Int32 j = 0; j < 64; j++)
                {
                    tile.TileBlocks.Add(new TileBlock(BitConverter.ToInt16(tileBytes, pos+2), BitConverter.ToInt16(tileBytes, pos), j));
                    pos = pos + 4;
                }

                Tiles.Add(tile);
            }

            tileBuffer.Close();
        }
        public void LoadMapData(Map map)
        {
            // 1. Load the Room Definitions from World.dat
            if (System.IO.File.Exists(WorldFilename))
            {
                // Based on assembly, we look for [roomXX] sections
                for (int i = 0; i < 255; i++) // Standard cap for room IDs
                {
                    string section = $"room{i}";
                    // Using your existing NativeMethods helper to check if section exists
                    int minX = NativeMethods.GetPrivateProfileInt32(section, "minx", WorldFilename);
                    if (minX == 0 && i > 0) continue; // Skip if room doesn't exist

                    Map.RoomDefinition room = new Map.RoomDefinition
                    {
                        ID = i,
                        MinX = minX,
                        MaxX = NativeMethods.GetPrivateProfileInt32(section, "maxx", WorldFilename),
                        MinY = NativeMethods.GetPrivateProfileInt32(section, "miny", WorldFilename),
                        MaxY = NativeMethods.GetPrivateProfileInt32(section, "maxy", WorldFilename)
                    };

                    // Parse the "views=6,10,14..." string into a bitmask
                    string viewsStr = NativeMethods.GetPrivateProfileString(section, "views", WorldFilename);
                    if (!string.IsNullOrEmpty(viewsStr))
                    {
                        string[] viewIds = viewsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        uint mask = 0;
                        foreach (string vId in viewIds)
                        {
                            if (int.TryParse(vId.Trim(), out int v))
                            {
                                mask |= (1u << v); // Replicates assembly .text:00461ECF
                            }
                        }
                        room.ViewBitmask = mask;
                    }

                    map.Rooms[i] = room;
                }
            }

            // 2. Load the Spatial Grid from roomview-gridXX.txt
            if (System.IO.File.Exists(RoomFilename))
            {
                string[] lines = System.IO.File.ReadAllLines(RoomFilename);
                // Assembly iterates 128x128 grid
                for (int x = 0; x < lines.Length && x < 128; x++)
                {
                    // Expected format: "11, 11, 20, ..."
                    string line = lines[x];
                    int dataStart = line.IndexOf(']') + 1;
                    if (dataStart <= 0) continue;

                    string data = line.Substring(dataStart);
                    string[] values = data.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int y = 0; y < values.Length && y < 128; y++)
                    {
                        if (byte.TryParse(values[y].Trim(), out byte roomId))
                        {
                            map.RoomGrid[x, y] = roomId;
                        }
                    }
                }
            }
        }
        public void GenerateAdjacencyFlags(Grid grid)
        {
            for (int x = 0; x < 128; x++)
            {
                for (int y = 0; y < 128; y++)
                {
                    GridBlock currentblock = grid.GridBlocks.GetBlockByLocation(x << 6, y << 6);
                    GridBlock westblock = grid.GridBlocks.GetBlockByLocation((x - 1) << 6, y << 6);
                    GridBlock eastblock = grid.GridBlocks.GetBlockByLocation((x + 1) << 6, y << 6);
                    GridBlock northblock = grid.GridBlocks.GetBlockByLocation(x << 6, (y - 1) << 6);
                    GridBlock southblock = grid.GridBlocks.GetBlockByLocation(x << 6, (y + 1) << 6);

                    short myH = currentblock.FloorZ;

                    // Only set the flag if WE are higher than the neighbor.
                    // This means we are the "Wall" that the neighbor will crash into.

                    if (x > 0 && myH > westblock.FloorZ)
                        currentblock.LedgeFlagWest = 1;

                    if (x < 127 && myH > eastblock.FloorZ)
                        currentblock.LedgeFlagEast = 1;

                    if (y > 0 && myH > northblock.FloorZ)
                        currentblock.LedgeFlagNorth = 1;

                    if (y < 127 && myH > southblock.FloorZ)
                        westblock.LedgeFlagSouth = 1;
                }
            }
        }
        private void LoadSubPixelLibrary()
        {
            byte[] rawData = File.ReadAllBytes(SubPixelFilename);
            const int stampSize = 4096;
            const int totalStamps = 64; // The 3FFF0 file contains 64 blocks

            SubPixelLibrary = new byte[totalStamps][,];

            for (int i = 0; i < totalStamps; i++)
            {
                SubPixelLibrary[i] = new byte[64, 64];
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        int fileOffset = (i * stampSize) + (y * 64) + x;

                        // Final safety check for the 3FFF0 (missing last 16 bytes)
                        if (fileOffset < rawData.Length)
                            SubPixelLibrary[i][x, y] = rawData[fileOffset];
                        else
                            SubPixelLibrary[i][x, y] = 0;
                    }
                }
            }
        }
        public void LoadStaticTables(byte[] rawData)
        {
            // Simply store the flat dump from the IDC script
            _rawTerrainData = rawData;

            int blockSize = 130;
            int totalTemplates = rawData.Length / blockSize;

            // We can still pre-cache the SlopeProperty for speed
            SlopeProperty = new int[totalTemplates];
            for (int i = 0; i < totalTemplates; i++)
            {
                // Read as Int16 (2 bytes) to stay within the 130-byte block
                SlopeProperty[i] = BitConverter.ToInt16(rawData, (i * blockSize) + 128);
            }
        }
        public int GetFloorMeshHeight(byte blockType, int subX, int subY)
        {            
            int cellIdx = (subX * 8) + subY;
            
            int rawByteOffset = (blockType * 130) + (cellIdx * 2);

            return BitConverter.ToInt16(_rawTerrainData, rawByteOffset);
        }

        public int GetCeilingMeshHeight(byte blockType, int subX, int subY)
        {
            int cellIdx = (subX * 8) + subY;
            // ASM: word_86DA20[130 * blockType + cellIdx]
            // Note: word_86DA20 is exactly 128 bytes after word_86D9A0
            int rawByteOffset = (blockType * 130) + 128 + (cellIdx * 2);

            return BitConverter.ToInt16(_rawTerrainData, rawByteOffset);
        }
        private void LoadGrid(Boolean isServer, LogBox logBox)
        {
            const int MAIN_SIZE = 16384 * 38; // 622592 bytes

            byte[] buffer = new byte[MAIN_SIZE];

            using (FileStream fs = new FileStream(GeometryFilename, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(0, SeekOrigin.Begin);
                int bytesRead = fs.Read(buffer, 0, 20464);
                if (bytesRead < 20464)
                {
                    // Partial — pad with zeros (client handles similar)
                    Array.Clear(buffer, bytesRead, 20464 - bytesRead);
                }
            }

            LoadStaticTables(buffer);

            LoadSubPixelLibrary();

            using (var reader = new BinaryReader(File.OpenRead(AllGridDataFilename)))
            {
                for (int i = 0; i < 16384; i++)
                {
                    // The assembly (sub_45AC80) proved: Index = (X * 128) + Y
                    int x = i / 128; // quotient is X
                    int y = i % 128; // remainder is Y

                    int index = (x << 7) + y;

                    int worldX = x << 6;
                    int worldY = y << 6;

                    var block = new GridBlock(index, worldX, worldY);

                    block.TileId = reader.ReadByte(); // byte_6f4e90
                    block.FloorZ = reader.ReadInt16(); // word_87ff30
                    block.WallHeight = reader.ReadInt16(); // word_7c740c
                    block.CeilingZ = reader.ReadInt16(); // word_7e8db8
                    block.BlockType = reader.ReadByte(); // byte_7b4c90
                    block.RawTileFlag = reader.ReadByte(); //byte_7f0db8
                    block.DetailMapIndex = reader.ReadByte(); // byte_7f4f48
                    block.LogicFlag = reader.ReadByte(); // byte_6f0e90
                    block.LowBoxZ = reader.ReadInt16(); // word_6e4e8c
                    block.HighBoxZ = reader.ReadInt16(); // word_6cce88
                    block.ModifierTemplate = reader.ReadByte(); // byte_7e4db8
                    block.ThinCollision = reader.ReadByte(); // byte_87bf30
                    block.FinalModifier = reader.ReadByte(); // byte_7acc90
                    block.SpecialCollision = reader.ReadByte(); // byte_6f8e90
                    block.LowBoxTopZ = block.FloorZ;
                    block.MidBoxTopZ = (block.CeilingZ > block.HighBoxZ) ? block.HighBoxZ : block.CeilingZ;
                    block.MidBoxHeight = (block.SpecialCollision == 0) ? 0 : 1;
                    block.MidBoxBottomZ = block.FloorZ;
                    block.HighBoxBottomZ = block.CeilingZ;
                    block.HasSkybox = (block.CeilingZ >= 1024);
                    block.IsSolidPillar = (block.CeilingZ <= block.FloorZ);

                    // --- PHYSICAL BOXES (Synced to Tables) ---

                    // 1. ContainerBox: The full vertical column of the tile
                    block.ContainerBox = new OrientedBoundingBox(new Vector3(worldX, worldY, -1024), new Vector3(64, 64, 2048), 0.0f);

                    // 2. LowBox: The "Dirt/Rock" under the floor. 
                    // Height represents everything from the bottom of the world up to the walkable floor.
                    float lowHeight = 1024 + block.FloorZ;
                    block.LowBox = new OrientedBoundingBox(new Vector3(worldX, worldY, -1024), new Vector3(64, 64, lowHeight), 0.0f);

                    // 3. MidBox: The "Wall/Room" volume
                    // Use the logic we discussed: Ceiling or Ledge, whichever is lower.
                    int midHeight = block.HasSkybox ? 0 : System.Math.Max(0, block.MidBoxTopZ - block.MidBoxBottomZ);

                    // Center the MidBox so SAT math works perfectly
                    block.MidBox = new OrientedBoundingBox(
                        new Vector3(worldX, worldY, block.MidBoxBottomZ),
                        new Vector3(64, 64, midHeight),
                        0.0f
                    );

                    // 4. HighBox: The "Roof/Ledge" volume
                    // This starts at HighBoxZ and goes up to the Ceiling (or a fixed thickness if outdoor)
                    if (block.HighBoxZ < 1024)
                    {
                        int highHeight = System.Math.Max(0, block.CeilingZ - block.HighBoxZ);
                        block.HighBox = new OrientedBoundingBox(
                            new Vector3(worldX, worldY, block.HighBoxZ),
                            new Vector3(64, 64, highHeight),
                            0.0f
                        );
                    }

                    // --- TILE SUB-BLOCKS (Micro-Collision) ---
                    if (block.TileId > 0 && Tiles.Any(t => t.TileId == block.TileId))
                    {
                        block.LowBoxTile = new Tile(block.TileId);
                        foreach (TileBlock tb in Tiles[block.TileId].TileBlocks)
                        {
                            block.LowBoxTile.TileBlocks.Add(new TileBlock(tb.TopHeight, tb.BottomHeight, tb.Index));
                        }

                        for (int subY = 0; subY < 8; subY++)
                        {
                            for (int subX = 0; subX < 8; subX++)
                            {
                                TileBlock tileBlock = block.LowBoxTile.TileBlocks[(subY * 8) + subX];
                                float tx = worldX + (8 * subX);
                                float ty = worldY + (8 * subY);

                                if (tileBlock.TopHeight > 0)
                                {
                                    tileBlock.TopBoundingBox = new OrientedBoundingBox(new Vector3(tx, ty, block.FloorZ - tileBlock.TopHeight + 128), new Vector3(8f, 8f, tileBlock.TopHeight), 0);
                                }
                                if (tileBlock.BottomHeight > 0)
                                {
                                    tileBlock.BottomBoundingBox = new OrientedBoundingBox(new Vector3(tx, ty, block.FloorZ), new Vector3(8f, 8f, tileBlock.BottomHeight), 0);
                                }
                            }
                        }
                    }

                    GridBlocks.Add(block);
                }
                
            }
            using (FileStream gridBuffer = new FileStream(GridFilename, FileMode.Open, FileAccess.Read))
            {
                gridBuffer.Seek(622596, SeekOrigin.Begin);

                for (Int32 i = 1; i <= 497; i++)
                {
                    Byte[] gridBytes = new Byte[16];
                    gridBuffer.Read(gridBytes, 0, 16);

                    GridObject obj = new GridObject
                    {
                        ObjectId = BitConverter.ToInt32(gridBytes, 0),
                        X = BitConverter.ToInt32(gridBytes, 4),
                        Y = BitConverter.ToInt32(gridBytes, 8),
                        Z = BitConverter.ToInt16(gridBytes, 12), // Low Word
                        Rotation = ((BitConverter.ToInt16(gridBytes, 14) << 12) / 360.0f) // High Word
                    };

                    obj.GridBlockId = ((obj.X >> 6) << 7) + (obj.Y >> 6);

                    obj.ContainerBox = new OrientedBoundingBox(new Vector3(obj.X, obj.Y, obj.Z), new Vector3(64, 64, 64), obj.Rotation);

                    GridObjects.Add(obj);

                    ObjectMap[obj.GridBlockId] = obj;
                }
            }
        }
        public List<GridBlock> GetBroadphaseBlocks(Vector3 position, float radius)
        {
            List<GridBlock> candidates = new List<GridBlock>();

            // Determine the min/max grid coordinates covered by the projectile's area
            int minGX = (int)(position.X - radius) >> 6;
            int maxGX = (int)(position.X + radius) >> 6;
            int minGY = (int)(position.Y - radius) >> 6;
            int maxGY = (int)(position.Y + radius) >> 6;

            // Loop through the 1x1, 2x1, or 2x2 block area
            for (int gx = minGX; gx <= maxGX; gx++)
            {
                for (int gy = minGY; gy <= maxGY; gy++)
                {
                    if (gx >= 0 && gx < 128 && gy >= 0 && gy < 128)
                    {
                        var block = this.GridBlocks.GetBlockByLocation(gx << 6, gy << 6);
                        if (block != null) candidates.Add(block);
                    }
                }
            }
            return candidates;
        }
        public int GetFloorHeight(int worldX, int worldY, int Z, Grid grid)
        {
            GridBlock block = grid.GridBlocks.GetBlockByLocation(worldX, worldY);

            if (block == null) return 0;

            int height = block.CeilingZ;

            if (Z < height)
            {
                byte blockType = block.BlockType;

                int localX = worldX & 0x3F; // 0-63
                int localY = worldY & 0x3F; // 0-63

                int subX = (worldX >> 3) & 7; // 0-7
                int subY = (worldY >> 3) & 7; // 0-7

                int slopeId = block.ModifierTemplate;

                if (grid.SlopeProperty[blockType] != 0)
                {
                    slopeId = grid.SlopeProperty[blockType];
                    blockType = 0;
                }

                height = block.FloorZ;

                height += grid.SubPixelLibrary[block.DetailMapIndex][localX, localY];
                
                if (slopeId != 0)
                {
                    int sHeight = Grid.SlopeZ[slopeId] +
                         ((localX * Grid.SlopeX[slopeId]) >> 6) +
                         ((localY * Grid.SlopeY[slopeId]) >> 6);

                    if (sHeight >= 0)
                    {
                        height += sHeight;
                    }
                }
                else
                {
                    height += GetFloorMeshHeight(blockType, subX, subY);
                }
            }

            return height;
        }
        public int GetCeilingHeight(int worldX, int worldY, int Z, Grid grid)
        {
            GridBlock block = grid.GridBlocks.GetBlockByLocation(worldX, worldY);

            int gX = worldX >> 6;
            int gY = worldY >> 6;

            int index = (gX << 7) + gY;

            int height = block.CeilingZ;

            if (Z >= height) return block.HighBoxZ;

            byte blockType = block.BlockType;

            int localX = worldX & 0x3F; // 0-63
            int localY = worldY & 0x3F; // 0-63

            int subX = (worldX >> 3) & 7; // 0-7
            int subY = (worldY >> 3) & 7; // 0-7

            height = block.WallHeight;

            height -= GetCeilingMeshHeight(blockType, subX, subY);

            height -= grid.SubPixelLibrary[block.LogicFlag][localX, localY];

            if (block.ThinCollision != 0)
            {
                int sHeight = Grid.SlopeZ[block.ThinCollision] +
                     ((localX * Grid.SlopeX[block.ThinCollision]) >> 6) +
                     ((localY * Grid.SlopeY[block.ThinCollision]) >> 6);

                if (sHeight >= 0)
                {
                    height -= sHeight;
                }
            }

            return height;
        }
        private void LoadObjectDefinitions()
        {
            using (FileStream objectBuffer = new FileStream(ObjectsFilename, FileMode.Open, FileAccess.Read))
            {
                for (Int32 i = 1; i <= 139; i++)
                {
                    Int32 pos = 0;

                    Byte[] objectBytes = new Byte[116];
                    objectBuffer.Read(objectBytes, 0, 116);

                    GridObjectDefinition obj = new GridObjectDefinition
                    {
                        DefinitionId = BitConverter.ToInt32(objectBytes, 0),
                        ImageId = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk3 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk4 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk5 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk6 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk7 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk8 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk9 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk10 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk11 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unused1 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk12 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk13 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk14 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk15 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk16 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk17 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk18 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unused2 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unused3 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk19 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk20 = BitConverter.ToInt32(objectBytes, pos += 4),
                        Unk21 = BitConverter.ToInt32(objectBytes, pos += 4),
                    };

                    Array.Copy(objectBytes, pos + 4, obj.Identifier, 0, 20);

                    GridObjectDefinitions.Add(obj);
                }
            }
        }
        public (bool Collides, GridBlock CollidingBlock) Collides(OrientedBoundingBox box)
        {
            try
            {
                GridBlockCollection gridBlockCollection = GridBlocks.GetBlocksNearBoundingBox(box);
                if (gridBlockCollection == null)
                    return (true, null); // Treat null as collision (safe default)

                // Check main Low/Mid/High boxes
                foreach (GridBlock gridBlock in gridBlockCollection)
                {
                    if (gridBlock == null) continue;

                    if (box.Collides(gridBlock.LowBox) ||
                        box.Collides(gridBlock.MidBox) ||
                        (box.Collides(gridBlock.HighBox) && !gridBlock.HasSkybox))
                    {
                        return (true, gridBlock);
                    }
                }

                // Check Low tile sub-blocks
                foreach (GridBlock gridBlock in gridBlockCollection)
                {
                    if (gridBlock == null || gridBlock.LowBoxTile == null) continue;

                    foreach (var tileBlock in gridBlock.LowBoxTile.TileBlocks)
                    {
                        if (tileBlock?.BottomBoundingBox != null && box.Collides(tileBlock.BottomBoundingBox))
                        {
                            return (true, gridBlock);
                        }
                    }
                }

                // Elevator trigger check (uses origin only)
                int gridX = (int)System.Math.Floor(box.Origin.X / 64);
                int gridY = (int)System.Math.Floor(box.Origin.Y / 64);

                if (Triggers
                        .Where(t => t.TriggerType == TriggerType.Elevator)
                        .Where(t => gridX == t.X1 && gridY == t.Y1)
                        .Any(t => box.Origin.Z > t.OffHeight && box.Origin.Z < t.Position.Z))
                {
                    // No specific block for elevator, return true with null block
                    return (true, null);
                }
            }
            catch (Exception)
            {
                return (true, null); // On error, treat as collision (safe)
            }

            return (false, null); // No collision
        }
        public Boolean LineToBoxIsBlocked(Vector3 startPoint, OrientedBoundingBox targetBox)
        {
            try
            {
                if (targetBox.Corners.Any(t => GridBlocks.GetBlocksInLine(startPoint, t).Count > 0))
                {
                    if (GridBlocks.GetBlocksInLine(startPoint, targetBox.Origin).Count > 0)
                    {
                        return true;
                    }
                }

                GridBlockCollection gridBlockCollection = GridBlocks.GetBlocksAroundLine(startPoint, targetBox.Origin);

                foreach (GridBlock gridBlock in gridBlockCollection)
                {
                    if (gridBlock.LowBoxTile != null)
                    {
                        foreach (TileBlock tileBlock in gridBlock.LowBoxTile.TileBlocks)
                        {
                            if (tileBlock.TopBoundingBox != null)
                            {
                                if (tileBlock.TopBoundingBox.LineInBox(startPoint, targetBox.Origin))
                                {
                                    return true;
                                }
                            }

                            if (tileBlock.BottomBoundingBox != null)
                            {
                                if (tileBlock.BottomBoundingBox.LineInBox(startPoint, targetBox.Origin))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return true;
            }

            return false;
        }
    }
}
