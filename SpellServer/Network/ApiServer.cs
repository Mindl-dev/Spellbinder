using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace SpellServer
{
    /// <summary>Lightweight HTTP API for launcher and dev tools.
    /// Listens on port 8080. No auth — read-only public data only.</summary>
    public static class ApiServer
    {
        private static HttpListener _listener;
        private static Thread _thread;

        public static void Start(int port = 10603)
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://+:{port}/");
                _listener.Start();

                _thread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "ApiServer"
                };
                _thread.Start();

                Program.Log($"API listening on port {port}.", Color.Green);
            }
            catch (Exception ex)
            {
                Program.Log($"API server failed to start: {ex.Message}", Color.Red);
            }
        }

        private static void ListenLoop()
        {
            while (_listener != null && _listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var path = context.Request.Url.AbsolutePath.TrimEnd('/').ToLower();

                switch (path)
                {
                    case "/api/players":
                        HandlePlayers(context);
                        break;
                    case "/api/status":
                        HandleStatus(context);
                        break;
                    default:
                        Respond(context, 404, "{\"error\":\"not found\"}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Program.Log($"API error: {ex.Message}", Color.Red);
                try { Respond(context, 500, "{\"error\":\"internal server error\"}"); }
                catch { }
            }
        }

        private static void HandlePlayers(HttpListenerContext context)
        {
            string json;
            lock (PlayerManager.Players.SyncRoot)
            {
                json = BuildPlayersJson(PlayerManager.Players);
            }
            Respond(context, 200, json);
        }

        private static void HandleStatus(HttpListenerContext context)
        {
            int online;
            lock (PlayerManager.Players.SyncRoot)
            {
                online = PlayerManager.Players.Count(p => p.IsLoggedIn && !p.Flags.HasFlag(PlayerFlag.Hidden));
            }
            Respond(context, 200, BuildStatusJson(online));
        }

        /// <summary>Build JSON for /api/players. Public for testing.</summary>
        public static string BuildPlayersJson(System.Collections.Generic.IEnumerable<Player> players)
        {
            var sb = new StringBuilder();
            sb.Append("{\"players\":[");

            bool first = true;
            foreach (Player player in players)
            {
                if (!player.IsLoggedIn) continue;
                if (player.Flags.HasFlag(PlayerFlag.Hidden)) continue;

                if (!first) sb.Append(",");
                first = false;

                sb.Append("{");
                sb.AppendFormat("\"account\":\"{0}\"", EscapeJson(player.Username));
                sb.AppendFormat(",\"location\":\"{0}\"", player.WorldLocation);

                if (player.ActiveCharacter != null)
                {
                    var c = player.ActiveCharacter;
                    sb.AppendFormat(",\"character\":\"{0}\"", EscapeJson(c.Name));
                    sb.AppendFormat(",\"level\":{0}", c.Level);
                    sb.AppendFormat(",\"class\":\"{0}\"", c.Class);
                }

                if (player.ActiveArena != null)
                {
                    sb.AppendFormat(",\"arena\":\"{0}\"", EscapeJson(player.ActiveArena.GameName));
                    sb.AppendFormat(",\"team\":\"{0}\"", player.ActiveTeam);
                }

                sb.Append("}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>Build JSON for /api/status. Public for testing.</summary>
        public static string BuildStatusJson(int online)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"online\":{0}", online);
            sb.AppendFormat(",\"capacity\":{0}", 510);
            sb.AppendFormat(",\"motd\":\"{0}\"", EscapeJson(Properties.Settings.Default.MessageOfTheDay));
            sb.Append("}");
            return sb.ToString();
        }

        private static void Respond(HttpListenerContext context, int statusCode, string json)
        {
            var response = context.Response;
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            response.Headers.Add("Access-Control-Allow-Origin", "*");

            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        public static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
