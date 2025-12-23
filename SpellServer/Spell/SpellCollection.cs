using Helper;

namespace SpellServer
{
    public class SpellCollection : ListCollection<Spell>
    {
        public SpellCollection()
        {
            Add(new Spell());
        }
    }
}
