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
		public static Boolean Headless;
		public static ConsoleLogBox HeadlessMainLog;
		public static ConsoleLogBox HeadlessChatLog;

        [STAThread]
        public static void Main(String[] arguments)
        {
	        try
	        {
		        Arguments = arguments;
				Headless = Array.Exists(arguments, a => a == "--headless" || a == "-h");

				NativeMethods.BeginTimePeriod(1);

				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
				AppDomain.CurrentDomain.ProcessExit += (s, e) => NativeMethods.EndTimePeriod(1);

				if (Headless)
				{
					HeadlessMainLog = new ConsoleLogBox("Main");
					HeadlessChatLog = new ConsoleLogBox("Chat");
					HeadlessMainLog.WriteMessage("Starting in headless mode...", Color.Blue);
					StartServer();
					// Block the main thread — server runs on background threads
					HeadlessMainLog.WriteMessage("Server running. Press Ctrl+C to stop.", Color.Green);
					var exitEvent = new System.Threading.ManualResetEvent(false);
					Console.CancelKeyPress += (s, e) => { e.Cancel = true; exitEvent.Set(); };
					exitEvent.WaitOne();
				}
				else
				{
					Application.EnableVisualStyles();
					Application.SetCompatibleTextRenderingDefault(false);
					Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
					Application.ThreadException += OnThreadException;

					ServerForm = new ServerForm();
					ServerForm.Shown += (sender, e) =>
					{
						Log("Form shown — initializing server...", Color.Blue);
						StartServer();
					};
					Application.Run(ServerForm);
				}
	        }
	        finally
	        {
				if (!Headless) Application.Exit();
	        }
        }

		/// <summary>Write to log — works in both GUI and headless mode.
		/// In GUI mode, routes to the appropriate log tab by category.
		/// In headless mode, all categories go to console with a prefix.</summary>
		public static void Log(String text, Color color, String category = "Main")
		{
			if (Headless || ServerForm == null)
			{
				// Headless mode or GUI not yet initialized — write to console
				var prefix = category == "Main" ? "" : $"[{category}] ";
				if (HeadlessMainLog != null)
					HeadlessMainLog.WriteMessage($"{prefix}{text}", color);
				else
					Console.WriteLine($"{prefix}{text}");
			}
			else
			{
				switch (category)
				{
					case "Chat":    ServerForm.ChatLog?.WriteMessage(text, color); break;
					case "Cheat":   ServerForm.CheatLog?.WriteMessage(text, color); break;
					case "Admin":   ServerForm.AdminLog?.WriteMessage(text, color); break;
					case "Whisper": ServerForm.WhisperLog?.WriteMessage(text, color); break;
					case "Report":  ServerForm.ReportLog?.WriteMessage(text, color); break;
					case "Misc":    ServerForm.MiscLog?.WriteMessage(text, color); break;
					default:        ServerForm.MainLog?.WriteMessage(text, color); break;
				}
			}
		}

	    public static void StartServer()
	    {
		    if (ServerStarted) return;

		    ServerStarted = true;

            Log("Starting server initialization...", Color.Blue);

			try
			{
				SpellManager.LoadSpells();
				Log("Spells loaded successfully.", Color.Green);
			}
			catch (Exception ex)
			{
                Log($"FATAL: Failed to load Spells.dat → {ex.Message}", Color.Red);
                Log(ex.StackTrace, Color.Red);
                return;
            }

			Character.LoadFilteredNames();
			Grid.LoadAllGrids(Headless ? (ILogWriter)HeadlessMainLog : ServerForm.MainLog);

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

		        Log(String.Format("[Exception] {0}", trace), Color.Red);

		        MailManager.QueueMail("Server Crash", trace);

				Interval maxMailWait = new Interval(10000, false);
		        while (MailManager.HasPendingMail && !maxMailWait.HasElapsed)
		        {
			        Thread.Sleep(1);
		        }

		        if (!Headless) ServerForm?.PurgeAllLogMessages();
	        }
	        finally
	        {
				Environment.Exit(exception.HResult);
	        }
        }
    }
}