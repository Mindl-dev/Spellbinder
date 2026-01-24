using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SpellServer.Forms;
using SpellServer.Properties;
using Helper;
using Helper.Timing;

namespace SpellServer
{
    public static class Program
    {
        public static Boolean IsUserExit = true;
		public static Boolean ServerStarted;
		public static String[] Arguments;
		public static ServerForm ServerForm;

        [STAThread]
        public static void Main(String[] arguments)
        {
	        try
	        {
		        Arguments = arguments;

				NativeMethods.BeginTimePeriod(1);

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

				Application.ThreadException += OnThreadException;
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
				AppDomain.CurrentDomain.ProcessExit += (s, e) => NativeMethods.EndTimePeriod(1);

                ServerForm = new ServerForm();

                // THIS IS THE MAGIC LINE
                ServerForm.Shown += (sender, e) =>
                {
                    // Form is now fully shown — UI thread is running — SAFE TO DO HEAVY WORK
                    Program.ServerForm.MainLog.WriteMessage("Form shown — initializing server...", Color.Blue);
                    StartServer();  // ← This now calls LoadSpells() safely
                };

                Application.Run(ServerForm = new ServerForm());
	        }
	        finally 
	        {
				Application.Exit();
	        }
        }

	    public static void StartServer()
	    {
		    if (ServerStarted) return;

		    ServerStarted = true;

            Program.ServerForm.MainLog.WriteMessage("Starting server initialization...", Color.Blue);

			try
			{
				SpellManager.LoadSpells();
				Program.ServerForm.MainLog.WriteMessage("Spells loaded successfully.", Color.Green);
			}
			catch (Exception ex)
			{
                Program.ServerForm.MainLog.WriteMessage($"FATAL: Failed to load Spells.dat → {ex.Message}", Color.Red);
                Program.ServerForm.MainLog.WriteMessage(ex.StackTrace, Color.Red);
                return;
            }

			Character.LoadFilteredNames();
			Grid.LoadAllGrids(ServerForm.MainLog);

			MySQL.OnlineAccounts.SetAllOffline();
			MySQL.OnlineCharacters.SetAllOffline();
		    MySQL.ServerSettings.SetExpMultiplier(Settings.Default.ExpMultiplier);

			CabalManager.LoadCabals();

			Network.Listen();
	    }

	    private static void OnThreadException(Object sender, ThreadExceptionEventArgs e)
        {
            ExceptionClose(e.Exception);
        }

	    private static void OnUnhandledException(Object sender, UnhandledExceptionEventArgs e)
        {
            ExceptionClose((Exception)e.ExceptionObject);
        }

	    private static void ExceptionClose(Exception exception)
        {
	        try
	        {
		        IsUserExit = false;
		        Settings.Default.Locked = true;
		        String trace = exception.GetStackTrace();

		        ServerForm.MainLog.WriteMessage(String.Format("[Exception] {0}", trace), Color.Red);

		        MailManager.QueueMail("Server Crash", trace);

				Interval maxMailWait = new Interval(10000, false);
		        while (MailManager.HasPendingMail && !maxMailWait.HasElapsed)
		        {
			        Thread.Sleep(1);
		        }

		        ServerForm.PurgeAllLogMessages();
	        }
	        finally
	        {
				Environment.Exit(exception.HResult);
	        }
        }
    }
}