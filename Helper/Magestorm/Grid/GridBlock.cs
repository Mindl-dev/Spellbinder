using Helper.Math;
using SharpDX;
using System;
using System.ComponentModel;
using System.Drawing;
using OrientedBoundingBox = Helper.Math.OrientedBoundingBox;

namespace Helper
{
    public enum GridBlockShape
    {
        None,
        CenterPointShort,
        WestShortSlant,
        EastStairway,
        MediumFullArchEastWest,
        SmallWestHalfArch,
        SmallEastHalfArch,
        SmallNorthHalfArch,
        SmallSouthHalfArch,
        CenterPointLong,
        CenterPointMid,
        MediumFullArchNorthSouth,
        Cylinder,
        EastCurvedRamp,
        WestCurvedRamp,
        SouthCurvedRamp,
        NorthCurvedRamp,
        SouthEastCurvedRamp,
        NorthEastCurvedRamp,
        NorthWestCurvedRamp,
        SouthWestCurvedRamp,
        EastAndSouthCurvedRamp,
        EastAndNorthCurvedRamp,
        WestAndNorthCurvedRamp,
        WestAndSouthCurvedRamp,
        LargeWestHalfArch,
        LargeEastHalfArch,
        LargeNorthHalfArch,
        LargeSouthHalfArch,
        LargeWestAndNorthHalfArch,
        LargeWestAndSouthHalfArch,
        LargeEastndSouthHalfArch,
        LargeEastndNorthHalfArch,
        EastLongSlant,
        WestLongSlant,
        SouthLongSlant,
        NorthLongSlant,
        EastLowLongSlant,
        WestLowLongSlant,
        SouthLowLongSlant,
        NorthLowLongSlant,
        EastHalfCutFullArch,
        WestHalfCutFullArch,
        SouthHalfCutFullArch,
        NorthHalfCutFullArch,
        WestFullVerticalHalfArch,
        EastFullVerticalHalfArch,
        NorthFullVerticalHalfArch,
        SouthFullVerticalHalfArch,
        SmallFullArchEastWest,
        SmallFullArchNorthSouth,
    }

    [DefaultPropertyAttribute("BlockId")]
    public class GridBlock
    {
        #region Constants
        private const String LocationCategory = "Location";
        private const String TexturesCategory = "Textures";
        private const String ObjectsCategory = "Objects";
        private const String UnknownCategory = "Unknown";
        #endregion

        #region Fields

        private byte _tileId;
        private byte _rawTileFlag;
        private byte _specialCollision;
        private byte _logicFlag;
        private byte _thinCollision;
        private byte _blockType;
        private byte _detailMapIndex;
        private byte _modifierTemplate;
        private byte _finalModifier;

        private short _wallHeight;
        private short _floorZ;
        private short _ceilingZ;
        private short _highBoxZ;
        private short _lowBoxZ;

        private int _ledgeFlagWest;
        private int _ledgeFlagEast;
        private int _ledgeFlagNorth;
        private int _ledgeFlagSouth;

        private Int32 _lowBoxTopMod;

        private Int32 _blockFlags;
        private Int32 _unknown16;

        private Int32 _lowTopTextureId;
        private Int32 _lowSidesTextureId;
        private Int32 _midTopTextureId;
        private Int32 _midSidesTextureId;
        private Int32 _highTextureId;
        private Int32 _ceilingTextureId;

        private GridBlockShape _lowTopShape;
        private GridBlockShape _midBottomShape;

        private Int32 _lowBoxTopZ;
        private Int32 _midBoxBottomZ;
        private Int32 _midBoxTopZ;
        private Int32 _highBoxBottomZ;

        private float _midBoxHeight;

        public OrientedBoundingBox ContainerBox;
        public OrientedBoundingBox LowBox;
        public OrientedBoundingBox MidBox;
        public OrientedBoundingBox HighBox;
        public Tile LowBoxTile;

        private Int32 _unknown0;
        private Int32 _unknown17;
        private Int32 _unknown18;

        #endregion

        #region Properties
        [ReadOnly(true), CategoryAttribute(LocationCategory)]
        public Int32 BlockId { get; private set; }

        [ReadOnly(true), CategoryAttribute(LocationCategory)]
        public Int32 X { get; private set; }

        [ReadOnly(true), CategoryAttribute(LocationCategory)]
        public Int32 Y { get; private set; }

        [CategoryAttribute(LocationCategory)]
        public Int32 LowBoxTopZ
        {
            get { return _lowBoxTopZ; }
            set
            {
                _lowBoxTopZ = value;
                // The floor typically starts at 0 or a base floor height.
                // Let's assume the floor is 64 units thick, sitting just below the surface.
                float floorThickness = 64f;
                float bottom = _lowBoxTopZ - floorThickness;

                // Use the actual height (floorThickness) so the Origin is at the center of the visible floor
                LowBox = new OrientedBoundingBox(
                    new Vector3(X, Y, bottom),
                    new Vector3(64, 64, floorThickness),
                    0.0f);
            }
        }

        [CategoryAttribute(LocationCategory)]
        public Int32 MidBoxBottomZ
        {
            get { return _midBoxBottomZ; }
            set
            {
                _midBoxBottomZ = value;

                float height = _midBoxTopZ - _midBoxBottomZ;

                // Ensure we don't create a box with 0 or negative height

                if (height <= 0) height = 1.0f;

                MidBox = new OrientedBoundingBox(
                    new Vector3(X, Y, _midBoxBottomZ),
                    new Vector3(64, 64, height),
                    0.0f);
            }
        }

        [CategoryAttribute(LocationCategory)]
        public float MidBoxHeight
        {
            get { return _midBoxHeight; }
            set
            {
                if (value == 0)
                {
                    _midBoxHeight = value;
                }
                else
                {
                    _midBoxHeight = value;
                    _midBoxHeight = _midBoxTopZ - _midBoxBottomZ;
                }
            }
        }
            /*public Int32 MidBoxBottomZ
            {
                get { return _midBoxBottomZ; }
                set
                {
                    _midBoxTopZ = value;

                    UpdateMidBox();
                }
            }*/

            [CategoryAttribute(LocationCategory)]
        public Int32 MidBoxTopZ
        {
            get { return _midBoxTopZ; }
            set
            {
                _midBoxTopZ = value;

                MidBox = new OrientedBoundingBox(new Vector3(X, Y, _midBoxBottomZ), new Vector3(64, 64, _midBoxTopZ - _midBoxBottomZ), 0.0f);
            }
        }
        /*public Int32 MidBoxTopZ
        {
            get { return _midBoxTopZ; }
            set
            {
                _midBoxTopZ = value;

                UpdateMidBox();
            }
        }*/

        [CategoryAttribute(LocationCategory)]
        public Int32 HighBoxBottomZ
        {
            get { return _highBoxBottomZ; }
            set
            {
                _highBoxBottomZ = value;

                HighBox = new OrientedBoundingBox(new Vector3(X, Y, _highBoxBottomZ), new Vector3(64, 64, 64), 0.0f);
            }
        }

        [CategoryAttribute(LocationCategory)]
        public Int32 LowBoxTopMod
        {
            get { return _lowBoxTopMod; }
            set { _lowBoxTopMod = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public byte TileId
        {
            get { return _tileId; }
            set { _tileId = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public short FloorZ
        {
            get { return _floorZ; }
            set { _floorZ = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public short WallHeight
        {
            get { return _wallHeight; }
            set { _wallHeight = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public short CeilingZ
        {
            get { return _ceilingZ; }
            set { _ceilingZ = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public byte BlockType
        {
            get { return _blockType; }
            set { _blockType = value; }
        }
        [CategoryAttribute(ObjectsCategory)]
        public byte RawTileFlag
        {
            get { return _rawTileFlag; }
            set { _rawTileFlag = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public byte DetailMapIndex
        {
            get { return _detailMapIndex; }
            set { _detailMapIndex = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public byte LogicFlag
        {
            get { return _logicFlag; }
            set { _logicFlag = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public short HighBoxZ
        {
            get { return _highBoxZ; }
            set { _highBoxZ = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public short LowBoxZ
        {
            get { return _lowBoxZ; }
            set { _lowBoxZ = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public byte ModifierTemplate
        {
            get { return _modifierTemplate; }
            set { _modifierTemplate = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public byte ThinCollision
        {
            get { return _thinCollision; }
            set { _thinCollision = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public byte FinalModifier
        {
            get { return _finalModifier; }
            set { _finalModifier = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public byte SpecialCollision
        {
            get { return _specialCollision; }
            set { _specialCollision = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public int LedgeFlagWest
        {
            get { return _ledgeFlagWest; }
            set { _ledgeFlagWest = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public int LedgeFlagEast
        {
            get { return _ledgeFlagEast; }
            set { _ledgeFlagEast = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public int LedgeFlagNorth
        {
            get { return _ledgeFlagNorth; }
            set { _ledgeFlagNorth = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public int LedgeFlagSouth
        {
            get { return _ledgeFlagSouth; }
            set { _ledgeFlagSouth = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public bool IsSolidPillar { get; set; }

        [CategoryAttribute(ObjectsCategory)]
        public GridBlockShape LowTopShape
        {
            get { return _lowTopShape; }
            set { _lowTopShape = value; }
        }

        [CategoryAttribute(ObjectsCategory)]
        public GridBlockShape MidBottomShape
        {
            get { return _midBottomShape; }
            set { _midBottomShape = value; }
        }

        [CategoryAttribute(UnknownCategory)]
        public Int32 BlockFlags
        {
            get { return _blockFlags; }
            set { _blockFlags = value; }
        }

        [CategoryAttribute(UnknownCategory)]
        public Int32 Unknown16
        {
            get { return _unknown16; }
            set { _unknown16 = value; }
        }

        [CategoryAttribute(TexturesCategory)]
        public Int32 LowTopTextureId
        {
            get { return _lowTopTextureId; }
            set
            {
                _lowTopTextureId = value;
            }
        }

        [CategoryAttribute(TexturesCategory)]
        public Int32 LowSidesTextureId
        {
            get { return _lowSidesTextureId; }
            set
            {
                _lowSidesTextureId = value;
            }
        }

        [CategoryAttribute(TexturesCategory)]
        public Int32 MidTopTextureId
        {
            get { return _midTopTextureId; }
            set
            {
                _midTopTextureId = value;
            }
        }

        [CategoryAttribute(TexturesCategory)]
        public Int32 MidSidesTextureId
        {
            get { return _midSidesTextureId; }
            set
            {
                _midSidesTextureId = value;
            }
        }

        [CategoryAttribute(TexturesCategory)]
        public Int32 HighTextureId
        {
            get { return _highTextureId; }
            set
            {
                _highTextureId = value;
            }
        }

        [CategoryAttribute(TexturesCategory)]
        public Int32 CeilingTextureId
        {
            get { return _ceilingTextureId; }
            set { _ceilingTextureId = value; }
        }

        [CategoryAttribute(UnknownCategory)]
        public Int32 Unknown17
        {
            get { return _unknown17; }
            set { _unknown17 = value; }
        }

        [CategoryAttribute(UnknownCategory)]
        public Int32 Unknown18
        {
            get { return _unknown18; }
            set { _unknown18 = value; }
        }

        [CategoryAttribute(UnknownCategory)]
        public Int32 Unknown0
        {
            get { return _unknown0; }
            set { _unknown0 = value; }
        }

        #endregion

        public Boolean HasSkybox
        {
            get
            {
                // After the fix-up loop, 1024(0x400) is the value for open sky.
                // We check for >= 1024 to be safe against slope offsets.
                return MidBoxTopZ >= 1024;
            }
            set
            {
                // If we set HasSkybox = true, we move the ceiling to the sky limit (1024)
                // If we set it to false, we usually default it to 0 (solid) or leave it.
                if (value)
                {
                    MidBoxTopZ = 1024;
                }
                else if (MidBoxTopZ >= 1024)
                {
                    MidBoxTopZ = 0; // Or keep current value if it's already a real ceiling
                }
            }
        }
        public GridBlock(Int32 blockId, Int32 x, Int32 y)
        {
            BlockId = blockId;
            X = x;
            Y = y;
        }
        /*private void UpdateMidBox()
        {
            float height = _midBoxTopZ - _midBoxBottomZ;
            if (height <= 0) height = 1.0f;

            MidBox = new OrientedBoundingBox(
                new Vector3(X, Y, _midBoxBottomZ),
                new Vector3(64, 64, height),
                0.0f);
        }*/
    }
}
