using System;
using System.Drawing;
using System.IO;

namespace Helper
{
    /// <summary>Headless replacement for LogBox — writes to console + file, no WinForms.</summary>
    public class ConsoleLogBox : ILogWriter
    {
        public readonly Object SyncRoot = new Object();
        public String LogName { get; set; }

        public String BaseLogDirectory => String.Format("{0}\\Logs\\", Directory.GetCurrentDirectory());
        public String FileLogDirectory => String.Format("{0}\\Logs\\{1}\\", Directory.GetCurrentDirectory(), LogName);
        public String FileName => String.Format("{0}\\Logs\\{1}\\{2}.txt", Directory.GetCurrentDirectory(), LogName, DateTime.Now.ToString("MMM.dd.yyyy"));

        public ConsoleLogBox(String logName = "Main")
        {
            LogName = logName;
        }

        public void WriteMessage(String text, Color color)
        {
            String timeStamp = String.Format("[{0} {1}] ", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
            String line = String.Format("{0}{1}", timeStamp, text);
            Console.WriteLine(line);
            WriteStringToFile(line + Environment.NewLine);
        }

        public void PurgeMessages() { }

        private void WriteStringToFile(String text)
        {
            try
            {
                if (!Directory.Exists(BaseLogDirectory)) Directory.CreateDirectory(BaseLogDirectory);
                if (!Directory.Exists(FileLogDirectory)) Directory.CreateDirectory(FileLogDirectory);
                File.AppendAllText(FileName, text);
            }
            catch (Exception) { }
        }
    }
}
