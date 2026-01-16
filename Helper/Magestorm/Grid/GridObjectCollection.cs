using System;

namespace Helper
{
    public class GridObjectCollection : ListCollection<GridObject>
    {
        public GridObjectCollection()
        {
            Add(new GridObject());
        }
        public GridObject GetObjectByLocation(Single x, Single y, Grid grid)
        {
            int gIndex = ((int)x >> 6 << 7) + ((int)y >> 6);
            return (gIndex >= 0 && gIndex < grid.ObjectMap.Length) ? grid.ObjectMap[gIndex] : null;
        }
    }
}
