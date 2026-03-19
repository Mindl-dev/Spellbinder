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
                    case "/api/register":
                        HandleRegister(context);
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

        private static void HandleRegister(HttpListenerContext context)
        {
            if (context.Request.HttpMethod != "POST")
            {
                Respond(context, 405, "{\"error\":\"POST required\"}");
                return;
            }

            string body;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                body = reader.ReadToEnd();

            // Parse username=X&password=Y (form-encoded)
            string username = null, password = null;
            foreach (var pair in body.Split('&'))
            {
                var kv = pair.Split(new[] { '=' }, 2);
                if (kv.Length != 2) continue;
                var key = Uri.UnescapeDataString(kv[0]).Trim();
                var val = Uri.UnescapeDataString(kv[1]).Trim();
                if (key == "username") username = val;
                else if (key == "password") password = val;
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Respond(context, 400, "{\"error\":\"username and password required\"}");
                return;
            }

            if (username.Length > 20 || password.Length > 20)
            {
                Respond(context, 400, "{\"error\":\"username and password must be 20 characters or less\"}");
                return;
            }

            if (username.Length < 3)
            {
                Respond(context, 400, "{\"error\":\"username must be at least 3 characters\"}");
                return;
            }

            // Check if account exists
            var existing = MySQL.Accounts.GetAccountData(username);
            if (existing != null && existing.Rows.Count > 0)
            {
                Respond(context, 409, "{\"error\":\"account already exists\"}");
                return;
            }

            // Create account with hashed password
            string hashedPassword = PasswordHasher.Hash(password);
            bool created = MySQL.Accounts.CreateAccount(username, hashedPassword);

            if (created)
            {
                Program.Log($"[API] Account created: {username}", Color.Green);
                Respond(context, 201, "{\"ok\":true,\"message\":\"account created\"}");
            }
            else
            {
                Respond(context, 500, "{\"error\":\"failed to create account\"}");
            }
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
