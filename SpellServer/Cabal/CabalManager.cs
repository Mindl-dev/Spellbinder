using Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellServer
{
    public class CabalManager : ListCollection<Cabal>
    {
        public static CabalManager Cabals = new CabalManager();
        public Int16 AvailableId
        {
            get
            {
                for (Int16 i = 1; i <= 510; i++)
                {
                    if (FindById(i) == null)
                    {
                        return i;
                    }
                }

                return 0;
            }
        }
        public static List<Int32> GetMemberIds(Int32 cabalId)
        {
            List<Int32> members = new List<Int32>();

            var table = MySQL.Character.FindByCabalId(cabalId);

            if (table == null) return members;

            foreach (DataRow row in table.Rows)
            {                
                members.Add(Convert.ToInt32(row["charid"]));
            }

            return members;
        }
        public Cabal FindById(Int32 cabalId)
        {
            if (cabalId <= 0) return null;

            return this.FirstOrDefault(c => cabalId == c.CabalId);
        }
        public Cabal FindByCabalName(String cabalName)
        {
            return this.FirstOrDefault(c => cabalName.ToLower() == c.CabalName.ToLower());
        }
        public Cabal FindByCabalTag(String cabalTag)
        {
            return this.FirstOrDefault(c => cabalTag.ToLower() == c.CabalTag.ToLower());
        }
        public static void LoadCabals()
        {
            Cabals = MySQL.Cabals.LoadCabals();
        }
    }
}
