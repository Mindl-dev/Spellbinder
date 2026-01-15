using System;
using System.Linq;

namespace Helper
{
    public class TablesCollection : ListCollection<Tables>
    {
        public TablesCollection()
        {
        }
        public Tables GetById(Int32 gridId)
        {
            return this.FirstOrDefault(t => gridId == t.GridId);
        }
    }
}
