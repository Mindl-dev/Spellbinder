using System.Drawing;

namespace Helper
{
    public interface ILogWriter
    {
        void WriteMessage(string text, Color color);
    }
}
