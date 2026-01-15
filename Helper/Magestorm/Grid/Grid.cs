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
        }
        public Grid(Grid grid)
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
        /*private void ProcessTileFlags(Grid grid)
        {
            // The assembly uses nested loops (128 x 128)
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    byte rawFlag = .RawTileFlagTable[x, y];

                    // Default: clear the destination tables for this tile
                    tables.SpecialCollisionTable[x, y] = 0;

                    // --- Sorting Logic ---

                    if (rawFlag >= 0xFA) // 250+
                    {
                        tables.SpecialCollisionTable[x, y] = 1;
                    }
                    else if (rawFlag == 0x64) // 100 ('d')
                    {
                        tables.SpecialCollisionTable[x, y] = 2;
                    }
                    else if (rawFlag == 0x65) // 101 ('e')
                    {
                        tables.SpecialCollisionTable[x, y] = 3;
                    }
                    else if (rawFlag == 0x66) // 102 ('f')
                    {
                        tables.SpecialCollisionTable[x, y] = 4;
                    }
                    else if (rawFlag == 0x6E) // 110 ('n')
                    {
                        tables.SpecialCollisionTable[x, y] = 5;
                    }

                    // The assembly ends by zeroing the raw table entry at 46275C
                    // to indicate processing is complete.
                    tables.RawTileFlagTable[x, y] = 0;
                }
            }
        }*/
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
        private T[,] LoadFlatTable<T>(string path, int elementSize)
        {
            T[,] result = new T[128, 128];
            byte[] data = File.ReadAllBytes(path);

            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    // This is the index the Assembly (v4) expects
                    int index = (y << 7) + x;

                    int offset = index * elementSize;

                    if (elementSize == 1)
                        result[x, y] = (T)(object)data[offset];
                    else
                        result[x, y] = (T)(object)BitConverter.ToInt16(data, offset);
                }
            }
            return result;
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
            //int gX = worldX >> 6;
            //int gY = worldY >> 6;

            //int subX = gX / 8;
            //int subY = gY / 8;

            int cellIdx = (subX * 8) + subY;
            
            int rawByteOffset = (blockType * 130) + (cellIdx * 2);

            return BitConverter.ToInt16(_rawTerrainData, rawByteOffset);
        }

        public int GetCeilingMeshHeight(byte blockType, int subX, int subY)
        {
            //int gX = worldX >> 6;
            //int gY = worldY >> 6;

            //int subX = gX / 8;
            //int subY = gY / 8;

            int cellIdx = (subX * 8) + subY;
            // ASM: word_86DA20[130 * blockType + cellIdx]
            // Note: word_86DA20 is exactly 128 bytes after word_86D9A0
            int rawByteOffset = (blockType * 130) + 128 + (cellIdx * 2);

            return BitConverter.ToInt16(_rawTerrainData, rawByteOffset);
        }

        /*public void LoadStaticTables(byte[] rawData)
        {
            int blockSize = 130;
            int totalTemplates = rawData.Length / blockSize;

            HeightLibrary = new short[totalTemplates, 64];
            SlopeProperty = new int[totalTemplates];

            for (int i = 0; i < totalTemplates; i++)
            {
                int fileOffset = i * blockSize;

                // 1. Extract 8x8 Heights (First 128 bytes of the block) 
                // We copy directly into the row of our 2D array
                Buffer.BlockCopy(rawData, fileOffset, HeightLibrary, i * 64 * 2, 128);

                // 2. Extract Property (First 4 bytes of the second 128-byte half) 
                // As you noted, the property/ID is at offset +128 within the block
                SlopeProperty[i] = BitConverter.ToInt32(rawData, fileOffset + 128);
            }
        }*/
        private void LoadOverlayTable<T>(byte[] buffer, int stride, int pushValue, int size, out T[,] table) where T : struct
        {
            table = new T[128, 128];
            int elementSize = Marshal.SizeOf<T>();

            // We follow the CSV/Memory order: Tile 0, 1, 2...
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    // This is the index the Assembly (v4) expects
                    int index = (y << 7) + x;

                    // The formula based on the client stride logic
                    // We use pushValue directly. 
                    // Index 0 of the file is Tile 0, Slot 0.
                    int currentEcx = (index * stride) + pushValue;
                    int bufIdx = currentEcx * size; // Everything is 2-byte aligned

                    if (bufIdx + elementSize <= buffer.Length)
                    {
                        if (typeof(T) == typeof(byte))
                        {
                            table[x, y] = (T)(object)buffer[bufIdx];
                        }
                        else if (typeof(T) == typeof(short))
                        {
                            table[x, y] = (T)(object)BitConverter.ToInt16(buffer, bufIdx);
                        }
                    }
                }
            }
        }
        /*private void LoadProperData(Tables tables)
        {
            // Load the 16-bit Short tables (Floor, Wall, Ceiling)
            tables.FloorGlobalOffset = LoadFlatTable<short>(FloorGlobalOffsetFilename, 2);
            tables.CeilingGlobalOffset = LoadFlatTable<short>(CeilingGlobalOffsetFilename, 2);
            tables.CeilingTable = LoadFlatTable<short>(CeilingTableFilename, 2);
            tables.BlockTypeTable = LoadFlatTable<byte>(BlockTypeTableFilename, 1);
        }*/
        /*private void LoadGridTables(Tables tables)
        {
            const int MAIN_SIZE = 16384 * 38; // 622592 bytes
            //const int STRIDE = 19; // Your version

            byte[] buffer = new byte[MAIN_SIZE];

            using (FileStream fs = new FileStream(GridFilename, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(6, SeekOrigin.Begin);
                int bytesRead = fs.Read(buffer, 0, MAIN_SIZE);
                if (bytesRead < MAIN_SIZE)
                {
                    // Partial — pad with zeros (client handles similar)
                    Array.Clear(buffer, bytesRead, MAIN_SIZE - bytesRead);
                }
            }

            LoadOverlayTable<byte>(buffer, STRIDE, 0, 1, out tables.LowTileID); //byte_6f4e90
            LoadOverlayTable<byte>(buffer, STRIDE, 1, 1, out tables.table1); //byte_873f30
            LoadOverlayTable<byte>(buffer, STRIDE, 2, 1, out tables.FloorDetail); //byte_8699a0
            LoadOverlayTable<short>(buffer, STRIDE, 3, 2, out tables.FloorGlobalOffset);  // word_87FF30
            LoadOverlayTable<short>(buffer, STRIDE, 4, 2, out tables.CeilingGlobalOffset); //word_7c740c
            LoadOverlayTable<short>(buffer, STRIDE, 5, 2, out tables.CeilingTable);      // word_7E8DB8 (critical)
            LoadOverlayTable<byte>(buffer, STRIDE, 6, 1, out tables.BlockTypeTable);      // byte_7B4C90
            LoadOverlayTable<short>(buffer, STRIDE, 7, 2, out tables.table7); //word_6e4e8c
            LoadOverlayTable<byte>(buffer, STRIDE, 8, 1, out tables.RawTileFlagTable); //byte_7f0db8
            LoadOverlayTable<byte>(buffer, STRIDE, 9, 1, out tables.DetailMapIndexTable); //byte_7f4f48
            LoadOverlayTable<byte>(buffer, STRIDE, 10, 1, out tables.RawThinTable); //byte_6f0e90
            LoadOverlayTable<byte>(buffer, STRIDE, 11, 1, out tables.table11); //byte_7d0690
            LoadOverlayTable<byte>(buffer, STRIDE, 12, 1, out tables.table12); //byte_7a8c90
            LoadOverlayTable<byte>(buffer, STRIDE, 13, 1, out tables.table13); //byte_6e0e8c
            LoadOverlayTable<short>(buffer, STRIDE, 14, 2, out tables.UpperTable);     // word_6CCE88
            LoadOverlayTable<short>(buffer, STRIDE, 15, 2, out tables.table15); //word_7bf408
            LoadOverlayTable<byte>(buffer, STRIDE, 16, 1, out tables.ModifierTemplateTable); //byte_7e4db8
            LoadOverlayTable<byte>(buffer, STRIDE, 17, 1, out tables.ThinCollisionTable); //byte_87bf30
            LoadOverlayTable<byte>(buffer, STRIDE, 18, 1,out tables.FinalModifier);       // byte_7ACC90 (subtractive)

            using (FileStream fs = new FileStream(GridFilename, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(0, SeekOrigin.Begin);
                int bytesRead = fs.Read(buffer, 0, 20464);
                if (bytesRead < 20464)
                {
                    // Partial — pad with zeros (client handles similar)
                    Array.Clear(buffer, bytesRead, 20464 - bytesRead);
                }
            }

            LoadStaticTables(buffer, tables);

            LoadSubPixelLibrary(tables);

            ProcessTileFlags(tables);

            //LoadProperData(tables);

            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    if (tables.HighBoxZ[x, y] == 0 || tables.HighBoxZ[x, y] == 32767)
                        tables.HighBoxZ[x, y] = 1024;

                    if (tables.CeilingGlobalOffset[x, y] == 32767)
                        tables.CeilingGlobalOffset[x, y] = 1024;

                    if (tables.CeilingTable[x, y] == 32767)
                        tables.CeilingTable[x, y] = 1024;

                    if (tables.table13[x, y] == 0)
                        tables.table13[x, y] = tables.FloorDetail[x, y];

                    if (tables.table12[x, y] == 0)
                        tables.table12[x, y] = tables.FloorDetail[x, y];

                    if (tables.DetailMapIndexTable[x, y] >= 0x21 && tables.DetailMapIndexTable[x, y] <= 0x28)
                    {
                        tables.CeilingTable[x, y] = (byte)(tables.DetailMapIndexTable[x, y] - 0x20); // 1 through 8
                        tables.DetailMapIndexTable[x, y] = 0;
                    }

                    if (tables.RawThinTable[x, y] >= 0x21 && tables.RawThinTable[x, y] <= 0x28)
                    {
                        tables.ThinCollisionTable[x, y] = (byte)(tables.RawThinTable[x, y] - 0x20);
                        tables.RawThinTable[x, y] = 0;
                    }

                }
            }
        }*/
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
                gridBuffer.Seek(622592, SeekOrigin.Begin);

                for (Int32 i = 1; i <= 997; i++)
                {
                    Byte[] gridBytes = new Byte[16];
                    gridBuffer.Read(gridBytes, 0, 16);

                    GridObject obj = new GridObject
                    {
                        ObjectId = BitConverter.ToInt32(gridBytes, 0),
                        X = BitConverter.ToInt32(gridBytes, 4),
                        Y = BitConverter.ToInt32(gridBytes, 8),
                        Z = BitConverter.ToInt32(gridBytes, 12)
                    };

                    GridObjects.Add(obj);
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
        /*public int GridHeightLookup(int worldX, int worldY, Grid grid)
        {
            GridBlock block = grid.GridBlocks.GetBlockByLocation(worldX, worldY);

            int gX = worldX >> 6;
            int gY = worldY >> 6;

            // Column-Major index
            int idx = (gY << 7) + gX;

            // 1. Get Base Floor
            int height = block.FloorZ;

            // 2. Add Geometry Offset (HeightLibrary)
            byte blockType = block.TileId;
            if (blockType > 0 && blockType < grid.HeightLibrary.GetLength(0))
            {
                // Find which 8x8 cell we are in within the tile
                int subX = (worldX >> 3) & 7; // 0-7
                int subY = (worldY >> 3) & 7; // 0-7
                int cellIdx = (subY * 8) + subX;

                height += grid.HeightLibrary[blockType, cellIdx];

                // 3. Apply Slope Math
                int slopeId = grid.SlopeProperty[blockType];
                if (slopeId > 0)
                {
                    int relX = worldX & 0x3F; // 0-63
                    int relY = worldY & 0x3F; // 0-63

                    // Use the slopeId to index into your SlopeX/Y/Z tables 
                    // and add the result to 'height'
                    // Slope math: Ensure SlopeX and SlopeY match the orientation
                    height += ((int)Grid.SlopeX[slopeId] * relX) >> 6;
                    height += ((int)Grid.SlopeY[slopeId] * relY) >> 6;
                    height += Grid.SlopeZ[slopeId];
                }
            }

            return height;
        }*/

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

        /*public short GetFloorHeight(int gridX, int gridY, Tables tables)
        {
            if (gridX < 0 || gridX >= 128 || gridY < 0 || gridY >= 128)
                return 0;  // or 32767 for ceiling-like, but floor usually 0 or low value

            int index = gridY * 128 + gridX;
            short baseZ = (short)GridBlocks[index].LowBoxTopZ;
            int blockFlag = GridBlocks[index].BlockFlags;
            short globalOffset = tables.FloorGlobalOffset[gridY, gridX];
            byte finalModifier = tables.FinalModifier[gridY, gridX];
            if (blockFlag == 0)
                return (short)(baseZ + globalOffset);
            else
                return (short)(baseZ + globalOffset - finalModifier);
        }
        public short GetCeilingHeight(int gridX, int gridY, Tables tables)
        {
            if (gridX < 0 || gridX >= 128 || gridY < 0 || gridY >= 128) return 32767;
            // Return UpperTable if it exists, otherwise return "Sky" height
            short val = tables.UpperTable[gridY, gridX];
            return val == 0 ? (short)32767 : val;
        }
        public short GetCeilingTable(int gridX, int gridY, Tables tables)
        {
            if (gridX < 0 || gridX >= 128 || gridY < 0 || gridY >= 128) return 32767;
            return tables.CeilingTable[gridY, gridX];
        }*/
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

        /*public Boolean Collides(OrientedBoundingBox box)
        {
            try
            {
                GridBlockCollection gridBlockCollection = GridBlocks.GetBlocksNearBoundingBox(box);
                if (gridBlockCollection == null) return true;

                if (gridBlockCollection.Any(gridBlock => box.Collides(gridBlock.LowBox) || box.Collides(gridBlock.MidBox) || (box.Collides(gridBlock.HighBox) && !gridBlock.HasSkybox)))
                {
                    return true;
                }

                if (gridBlockCollection.Where(gridBlock => gridBlock.LowBoxTile != null).Any(gridBlock => gridBlock.LowBoxTile.TileBlocks.Where(tileBlock => tileBlock.BottomBoundingBox != null).Any(tileBlock => box.Collides(tileBlock.BottomBoundingBox))))
                {
                    return true;
                }

				if (Triggers.Where(t => t.TriggerType == TriggerType.Elevator).Where(t => (Int32)System.Math.Floor(box.Origin.X / 64) == t.X1 && (Int32)System.Math.Floor(box.Origin.Y / 64) == t.Y1).Any(t => box.Origin.Z > t.OffHeight && box.Origin.Z < t.Position.Z))
                {
                    return true;
                }

            }
            catch (Exception)
            {
                return true;
            }

            return false;
        }*/

        public CollisionResult TileCollides(OrientedBoundingBox box, Vector3 projectileVelocity)
        {
            GridBlockCollection gridBlockCollection = GridBlocks.GetBlocksNearBoundingBox(box);
            if (gridBlockCollection.Count == 0)
                return new CollisionResult(false);

            foreach (GridBlock gridBlock in gridBlockCollection)
            {
                if (gridBlock == null || gridBlock.LowBoxTile == null)
                    continue;

                foreach (TileBlock tileBlock in gridBlock.LowBoxTile.TileBlocks)
                {
                    // Check both top and bottom OBBs
                    OrientedBoundingBox hitBox = null;
                    if (tileBlock.TopBoundingBox != null && box.Collides(tileBlock.TopBoundingBox))
                    {
                        hitBox = tileBlock.TopBoundingBox;
                    }
                    else if (tileBlock.BottomBoundingBox != null && box.Collides(tileBlock.BottomBoundingBox))
                    {
                        hitBox = tileBlock.BottomBoundingBox;
                    }

                    if (hitBox != null)
                    {
                        // Calculate normal based on hit face
                        Vector3 normal = CalculateTileNormal(hitBox, box.Origin, projectileVelocity);
                        return new CollisionResult(true, tileBlock, normal);
                    }
                }
            }

            return new CollisionResult(false);
        }

        private Vector3 CalculateTileNormal(OrientedBoundingBox hitBox, Vector3 projectilePos, Vector3 projectileVelocity)
        {
            // Find which face was hit by checking position relative to box center
            Vector3 toCenter = projectilePos - hitBox.Origin;
            Vector3 extents = hitBox.Extents;

            // Normalize position by extents to find closest face
            float normX = System.Math.Abs(toCenter.X / extents.X);
            float normY = System.Math.Abs(toCenter.Y / extents.Y);
            float normZ = System.Math.Abs(toCenter.Z / extents.Z);

            Vector3 normal = new Vector3(0, 0, 0);

            // Pick dominant axis (closest face)
            if (normX >= normY && normX >= normZ)
                normal.X = System.Math.Sign(toCenter.X); // Left/right face
            else if (normY >= normZ)
                normal.Y = System.Math.Sign(toCenter.Y); // Front/back face
            else
                normal.Z = System.Math.Sign(toCenter.Z); // Top/bottom face

            // Ensure normal points toward incoming projectile (opposite velocity)
            float dot = Vector3.Dot(normal, -projectileVelocity);
            if (dot < 0)
                normal = -normal; // Flip to outward

            return normal;
        }

        public Boolean TileCollides(OrientedBoundingBox box)
        {
            GridBlockCollection gridBlockCollection = GridBlocks.GetBlocksNearBoundingBox(box);

            if (gridBlockCollection.Count == 0) return false;

            foreach (GridBlock gridBlock in gridBlockCollection)
            {
                if (gridBlock != null && gridBlock.LowBoxTile != null)
                {
                    foreach (TileBlock tileBlock in gridBlock.LowBoxTile.TileBlocks)
                    {
                        if (tileBlock.TopBoundingBox != null)
                        {
                            if (box.Collides(tileBlock.TopBoundingBox)) return true;
                        }

                        if (tileBlock.BottomBoundingBox != null)
                        {
                            if (box.Collides(tileBlock.BottomBoundingBox)) return true;
                        }
                    }
                }
            }

            return false;
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
