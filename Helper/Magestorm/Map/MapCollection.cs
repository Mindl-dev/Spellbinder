using System;
using System.Linq;
using static Helper.Map;

namespace Helper
{
    public class MapsCollection : ListCollection<Map>
    {
        public Map FindById(Int32 gridId)
        {
            return this.FirstOrDefault(g => gridId == g.GridId);
        }
    }
}
