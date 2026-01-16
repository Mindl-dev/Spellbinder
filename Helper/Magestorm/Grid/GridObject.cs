using SharpDX;
using System;
using OrientedBoundingBox = Helper.Math.OrientedBoundingBox;

namespace Helper
{
    public class GridObject
    {
        public Int32 ObjectId;
        public Int32 X;
        public Int32 Y;
        public Int32 Z;
        public float Rotation;
        public Int32 GridBlockId;

        public OrientedBoundingBox ContainerBox;
    }
}
