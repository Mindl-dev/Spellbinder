using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using NUnit.Framework;

namespace SpellServer.Tests
{
    /// <summary>Tests for Play.exe launcher logic.
    /// Since Play.cs is compiled separately (not part of SpellServer.dll),
    /// we duplicate the pure static functions here for testing.
    /// Keep these in sync with client/Play.cs.</summary>
    [TestFixture]
    public class LauncherTests
    {
        // ================================================================
        // Duplicated from Play.cs — keep in sync
        // ================================================================

        private static string ExtractJsonField(string json, string field)
        {
            string key = "\"" + field + "\"";
            int idx = json.IndexOf(key);
            if (idx < 0) return null;
            int colon = json.IndexOf(':', idx + key.Length);
            if (colon < 0) return null;
            int vStart = colon + 1;
            while (vStart < json.Length && json[vStart] == ' ') vStart++;
            if (vStart >= json.Length) return null;
            char first = json[vStart];
            if (first == '"')
            {
                int end = json.IndexOf('"', vStart + 1);
                while (end > 0 && json[end - 1] == '\\')
                    end = json.IndexOf('"', end + 1);
                if (end < 0) return null;
                return json.Substring(vStart + 1, end - vStart - 1);
            }
            if (first == '[')
            {
                int depth = 0;
                for (int i = vStart; i < json.Length; i++)
                {
                    if (json[i] == '[') depth++;
                    else if (json[i] == ']') { depth--; if (depth == 0) return json.Substring(vStart, i - vStart + 1); }
                }
            }
            int vEnd = vStart;
            while (vEnd < json.Length && json[vEnd] != ',' && json[vEnd] != '}' && json[vEnd] != ']')
                vEnd++;
            return json.Substring(vStart, vEnd - vStart).Trim();
        }

        private static List<string> SplitJsonArray(string json)
        {
            var items = new List<string>();
            json = json.Trim();
            if (json.Length < 2 || json[0] != '[') return items;
            int depth = 0; bool inStr = false; int start = 1;
            for (int i = 1; i < json.Length - 1; i++)
            {
                char c = json[i];
                if (c == '\\' && inStr) { i++; continue; }
                if (c == '"') inStr = !inStr;
                if (!inStr)
                {
                    if (c == '[' || c == '{') depth++;
                    else if (c == ']' || c == '}') depth--;
                    else if (c == ',' && depth == 0)
                    {
                        items.Add(json.Substring(start, i - start).Trim());
                        start = i + 1;
                    }
                }
            }
            string last = json.Substring(start, json.Length - 1 - start).Trim();
            if (last.Length > 0) items.Add(last);
            return items;
        }

        private static string StripQuotes(string s)
        {
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                return s.Substring(1, s.Length - 2);
            return s;
        }

        private static string GetServerAddress(string text)
        {
            int paren = text.IndexOf('(');
            if (paren >= 0)
            {
                int end = text.IndexOf(')', paren);
                if (end > paren)
                    return text.Substring(paren + 1, end - paren - 1).Trim();
            }
            return text.Trim();
        }

        private static string ResolveToIP(string hostOrIP)
        {
            IPAddress ip;
            if (IPAddress.TryParse(hostOrIP, out ip))
                return hostOrIP;
            try
            {
                var addresses = Dns.GetHostAddresses(hostOrIP);
                foreach (var addr in addresses)
                {
                    if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return addr.ToString();
                }
            }
            catch { }
            return hostOrIP;
        }

        // ================================================================
        // GetServerAddress tests
        // ================================================================

        [Test]
        public void GetServerAddress_PresetFormat()
        {
            Assert.AreEqual("45.33.60.131",
                GetServerAddress("Community Server  (45.33.60.131)"));
        }

        [Test]
        public void GetServerAddress_HostnamePreset()
        {
            Assert.AreEqual("spellbinder.blackeon.net",
                GetServerAddress("Community Server  (spellbinder.blackeon.net)"));
        }

        [Test]
        public void GetServerAddress_RawIP()
        {
            Assert.AreEqual("127.0.0.1", GetServerAddress("127.0.0.1"));
        }

        [Test]
        public void GetServerAddress_RawHostname()
        {
            Assert.AreEqual("myserver.com", GetServerAddress("myserver.com"));
        }

        [Test]
        public void GetServerAddress_Whitespace()
        {
            Assert.AreEqual("127.0.0.1", GetServerAddress("  127.0.0.1  "));
        }

        [Test]
        public void GetServerAddress_Empty()
        {
            Assert.AreEqual("", GetServerAddress(""));
        }

        // ================================================================
        // ResolveToIP tests
        // ================================================================

        [Test]
        public void ResolveToIP_AlreadyIP_Passthrough()
        {
            Assert.AreEqual("127.0.0.1", ResolveToIP("127.0.0.1"));
            Assert.AreEqual("45.33.60.131", ResolveToIP("45.33.60.131"));
            Assert.AreEqual("0.0.0.0", ResolveToIP("0.0.0.0"));
        }

        [Test]
        public void ResolveToIP_Localhost()
        {
            string result = ResolveToIP("localhost");
            Assert.AreEqual("127.0.0.1", result);
        }

        [Test]
        public void ResolveToIP_InvalidHostname_Fallback()
        {
            string result = ResolveToIP("this.host.definitely.does.not.exist.invalid");
            // Should return the original string as fallback
            Assert.AreEqual("this.host.definitely.does.not.exist.invalid", result);
        }

        // ================================================================
        // ExtractJsonField tests
        // ================================================================

        [Test]
        public void ExtractJsonField_StringValue()
        {
            string json = "{\"name\":\"Frostbane\",\"level\":10}";
            Assert.AreEqual("Frostbane", ExtractJsonField(json, "name"));
        }

        [Test]
        public void ExtractJsonField_NumberValue()
        {
            string json = "{\"name\":\"Frostbane\",\"level\":10}";
            Assert.AreEqual("10", ExtractJsonField(json, "level"));
        }

        [Test]
        public void ExtractJsonField_MissingField()
        {
            string json = "{\"name\":\"Frostbane\"}";
            Assert.IsNull(ExtractJsonField(json, "level"));
        }

        [Test]
        public void ExtractJsonField_ArrayValue()
        {
            string json = "{\"players\":[{\"a\":1},{\"b\":2}]}";
            string result = ExtractJsonField(json, "players");
            Assert.AreEqual("[{\"a\":1},{\"b\":2}]", result);
        }

        [Test]
        public void ExtractJsonField_EmptyString()
        {
            string json = "{\"name\":\"\"}";
            Assert.AreEqual("", ExtractJsonField(json, "name"));
        }

        [Test]
        public void ExtractJsonField_EscapedQuotes()
        {
            string json = "{\"msg\":\"say \\\"hello\\\"\"}";
            Assert.AreEqual("say \\\"hello\\\"", ExtractJsonField(json, "msg"));
        }

        [Test]
        public void ExtractJsonField_BooleanValue()
        {
            string json = "{\"ok\":true}";
            Assert.AreEqual("true", ExtractJsonField(json, "ok"));
        }

        // ================================================================
        // SplitJsonArray tests
        // ================================================================

        [Test]
        public void SplitJsonArray_SimpleArray()
        {
            var result = SplitJsonArray("[1,2,3]");
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("1", result[0]);
            Assert.AreEqual("2", result[1]);
            Assert.AreEqual("3", result[2]);
        }

        [Test]
        public void SplitJsonArray_ObjectArray()
        {
            var result = SplitJsonArray("[{\"a\":1},{\"b\":2}]");
            Assert.AreEqual(2, result.Count);
            StringAssert.StartsWith("{", result[0]);
            StringAssert.StartsWith("{", result[1]);
        }

        [Test]
        public void SplitJsonArray_Empty()
        {
            var result = SplitJsonArray("[]");
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void SplitJsonArray_SingleElement()
        {
            var result = SplitJsonArray("[42]");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("42", result[0]);
        }

        [Test]
        public void SplitJsonArray_NestedArrays()
        {
            var result = SplitJsonArray("[[1,2],[3,4]]");
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("[1,2]", result[0]);
            Assert.AreEqual("[3,4]", result[1]);
        }

        [Test]
        public void SplitJsonArray_StringsWithCommas()
        {
            var result = SplitJsonArray("[\"a,b\",\"c,d\"]");
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("\"a,b\"", result[0]);
            Assert.AreEqual("\"c,d\"", result[1]);
        }

        [Test]
        public void SplitJsonArray_NotAnArray()
        {
            var result = SplitJsonArray("{\"a\":1}");
            Assert.AreEqual(0, result.Count);
        }

        // ================================================================
        // StripQuotes tests
        // ================================================================

        [Test]
        public void StripQuotes_Quoted()
        {
            Assert.AreEqual("hello", StripQuotes("\"hello\""));
        }

        [Test]
        public void StripQuotes_Unquoted()
        {
            Assert.AreEqual("hello", StripQuotes("hello"));
        }

        [Test]
        public void StripQuotes_SingleChar()
        {
            Assert.AreEqual("\"", StripQuotes("\""));
        }

        [Test]
        public void StripQuotes_WithWhitespace()
        {
            Assert.AreEqual("hello", StripQuotes("  \"hello\"  "));
        }

        // ================================================================
        // Full player list parsing (integration)
        // ================================================================

        [Test]
        public void ParsePlayerList_RealServerResponse()
        {
            string json = "{\"players\":[{\"account\":\"Moonshard\",\"location\":\"Tavern\",\"character\":\"Frostbane\",\"level\":12,\"class\":\"Mystic\",\"arena\":\"Kaelgard Keep\",\"team\":\"Dragon\"}]}";
            string arrayJson = ExtractJsonField(json, "players");
            Assert.IsNotNull(arrayJson);

            var items = SplitJsonArray(arrayJson);
            Assert.AreEqual(1, items.Count);

            string player = items[0];
            Assert.AreEqual("Moonshard", ExtractJsonField(player, "account"));
            Assert.AreEqual("Frostbane", ExtractJsonField(player, "character"));
            Assert.AreEqual("Mystic", ExtractJsonField(player, "class"));
            Assert.AreEqual("12", ExtractJsonField(player, "level"));
            Assert.AreEqual("Kaelgard Keep", ExtractJsonField(player, "arena"));
            Assert.AreEqual("Dragon", ExtractJsonField(player, "team"));
        }

        [Test]
        public void ParsePlayerList_EmptyResponse()
        {
            string json = "{\"players\":[]}";
            string arrayJson = ExtractJsonField(json, "players");
            var items = SplitJsonArray(arrayJson);
            Assert.AreEqual(0, items.Count);
        }

        [Test]
        public void ParsePlayerList_MultiplePlayersOnline()
        {
            string json = "{\"players\":[{\"account\":\"A\"},{\"account\":\"B\"},{\"account\":\"C\"}]}";
            string arrayJson = ExtractJsonField(json, "players");
            var items = SplitJsonArray(arrayJson);
            Assert.AreEqual(3, items.Count);
            Assert.AreEqual("A", ExtractJsonField(items[0], "account"));
            Assert.AreEqual("B", ExtractJsonField(items[1], "account"));
            Assert.AreEqual("C", ExtractJsonField(items[2], "account"));
        }

        // ================================================================
        // Crash diagnostics tests
        // ================================================================

        /// <summary>Duplicated from Play.cs DiagnoseCrash — generates crash report string</summary>
        private static string BuildCrashReport(int exitCode, double secondsRan, string gameDir)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SpellBinder Crash Report ===");
            sb.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            uint uExitCode = unchecked((uint)exitCode);
            sb.AppendLine("Exit code: " + exitCode + " (0x" + uExitCode.ToString("X8") + ")");
            switch (uExitCode)
            {
                case 0xC0000005: sb.AppendLine("Meaning: ACCESS_VIOLATION"); break;
                case 0xC0000135: sb.AppendLine("Meaning: DLL_NOT_FOUND"); break;
                case 0xC0000142: sb.AppendLine("Meaning: DLL_INIT_FAILED"); break;
            }
            sb.AppendLine("Ran for: " + secondsRan.ToString("F1") + " seconds");
            sb.AppendLine();

            sb.AppendLine("--- File check ---");
            string[] expectedFiles = { "game.dll", "main.dat", "DDraw.dll", "D3DImm.dll", "dgVoodoo.conf" };
            foreach (string f in expectedFiles)
            {
                string path = Path.Combine(gameDir, f);
                if (File.Exists(path))
                    sb.AppendLine("  OK   " + f);
                else
                    sb.AppendLine("  MISS " + f);
            }
            return sb.ToString();
        }

        [Test]
        public void CrashReport_AccessViolation_ShowsExitCode()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-crash-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
            try
            {
                string report = BuildCrashReport(unchecked((int)0xC0000005), 2.5, testDir);
                StringAssert.Contains("0xC0000005", report);
                StringAssert.Contains("ACCESS_VIOLATION", report);
                StringAssert.Contains("2.5 seconds", report);
            }
            finally { Directory.Delete(testDir, true); }
        }

        [Test]
        public void CrashReport_DllNotFound_ShowsMeaning()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-crash-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
            try
            {
                string report = BuildCrashReport(unchecked((int)0xC0000135), 0.1, testDir);
                StringAssert.Contains("DLL_NOT_FOUND", report);
            }
            finally { Directory.Delete(testDir, true); }
        }

        [Test]
        public void CrashReport_NormalExit_NoMeaning()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-crash-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
            try
            {
                string report = BuildCrashReport(0, 30.0, testDir);
                StringAssert.Contains("0x00000000", report);
                Assert.IsFalse(report.Contains("Meaning:"));
            }
            finally { Directory.Delete(testDir, true); }
        }

        [Test]
        public void CrashReport_DetectsMissingDgVoodoo()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-crash-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
            try
            {
                // Only create game.dll and main.dat — no dgVoodoo
                File.WriteAllText(Path.Combine(testDir, "game.dll"), "x");
                File.WriteAllText(Path.Combine(testDir, "main.dat"), "x");
                string report = BuildCrashReport(unchecked((int)0xC0000005), 1.0, testDir);
                StringAssert.Contains("MISS DDraw.dll", report);
                StringAssert.Contains("MISS D3DImm.dll", report);
                StringAssert.Contains("OK   game.dll", report);
            }
            finally { Directory.Delete(testDir, true); }
        }

        [Test]
        public void CrashReport_AllFilesPresent()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-crash-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
            try
            {
                foreach (string f in new[] { "game.dll", "main.dat", "DDraw.dll", "D3DImm.dll", "dgVoodoo.conf" })
                    File.WriteAllText(Path.Combine(testDir, f), "x");
                string report = BuildCrashReport(unchecked((int)0xC0000005), 1.0, testDir);
                Assert.IsFalse(report.Contains("MISS"));
            }
            finally { Directory.Delete(testDir, true); }
        }

        [Test]
        public void CrashReport_WritesToLogFile()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-crash-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
            try
            {
                string report = BuildCrashReport(unchecked((int)0xC0000005), 3.0, testDir);
                string logPath = Path.Combine(testDir, "crash.log");
                File.AppendAllText(logPath, report + "\n\n");

                Assert.IsTrue(File.Exists(logPath));
                string contents = File.ReadAllText(logPath);
                StringAssert.Contains("ACCESS_VIOLATION", contents);
                StringAssert.Contains("Crash Report", contents);
            }
            finally { Directory.Delete(testDir, true); }
        }

        // ================================================================
        // Update preservation tests
        // Simulates the backup → extract → restore flow from ApplyUpdate()
        // ================================================================

        private static readonly string[] PreservedFiles = { "main.dat", "user.dat", "keyboard.dat" };

        private static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string file in Directory.GetFiles(source))
            {
                string destFile = Path.Combine(dest, Path.GetFileName(file));
                try { File.Copy(file, destFile, true); } catch { }
            }
            foreach (string dir in Directory.GetDirectories(source))
            {
                CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
            }
        }

        /// <summary>Simulate the backup/restore logic from ApplyUpdate()</summary>
        private static void SimulateUpdate(string baseDir, string updateZip)
        {
            string gameDir = Path.Combine(baseDir, "game");

            // Backup preserved files
            var backups = new Dictionary<string, string>();
            foreach (var name in PreservedFiles)
            {
                string path = Path.Combine(gameDir, name);
                if (File.Exists(path))
                {
                    string bak = path + ".bak";
                    File.Copy(path, bak, true);
                    backups[name] = bak;
                }
            }

            // Extract update
            string tempDir = Path.Combine(Path.GetTempPath(), "SpellBinder-update-test-" + Guid.NewGuid().ToString("N"));
            ZipFile.ExtractToDirectory(updateZip, tempDir);

            string extractedRoot = tempDir;
            string[] subdirs = Directory.GetDirectories(tempDir);
            if (subdirs.Length == 1 && Directory.Exists(Path.Combine(subdirs[0], "game")))
                extractedRoot = subdirs[0];

            CopyDirectory(extractedRoot, baseDir);

            // Restore preserved files
            foreach (var kvp in backups)
            {
                string path = Path.Combine(gameDir, kvp.Key);
                if (File.Exists(kvp.Value))
                {
                    File.Copy(kvp.Value, path, true);
                    File.Delete(kvp.Value);
                }
            }

            // Cleanup
            try { Directory.Delete(tempDir, true); } catch { }
        }

        private string SetupFakeInstall(string testDir)
        {
            string baseDir = Path.Combine(testDir, "installed");
            string gameDir = Path.Combine(baseDir, "game");
            Directory.CreateDirectory(gameDir);

            File.WriteAllText(Path.Combine(baseDir, "Play.exe"), "old-launcher");
            File.WriteAllText(Path.Combine(baseDir, "version.txt"), "v0.3.0");
            File.WriteAllText(Path.Combine(gameDir, "game.dll"), "old-game-dll");
            File.WriteAllText(Path.Combine(gameDir, "main.dat"), "address=192.168.1.50");
            File.WriteAllText(Path.Combine(gameDir, "user.dat"), "my-custom-keybinds");
            File.WriteAllText(Path.Combine(gameDir, "keyboard.dat"), "my-keyboard-config");
            File.WriteAllText(Path.Combine(gameDir, "arena.dat"), "old-arena");

            return baseDir;
        }

        private string CreateFakeUpdateZip(string testDir)
        {
            string updateDir = Path.Combine(testDir, "update-content", "SpellBinder-win");
            string gameDir = Path.Combine(updateDir, "game");
            Directory.CreateDirectory(gameDir);

            File.WriteAllText(Path.Combine(updateDir, "Play.exe"), "new-launcher");
            File.WriteAllText(Path.Combine(updateDir, "version.txt"), "v0.4.0");
            File.WriteAllText(Path.Combine(gameDir, "game.dll"), "new-patched-game-dll");
            File.WriteAllText(Path.Combine(gameDir, "main.dat"), "address=default-server");
            File.WriteAllText(Path.Combine(gameDir, "user.dat"), "default-user-settings");
            File.WriteAllText(Path.Combine(gameDir, "keyboard.dat"), "default-keybinds");
            File.WriteAllText(Path.Combine(gameDir, "arena.dat"), "new-arena");

            string zipPath = Path.Combine(testDir, "SpellBinder-win.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(Path.Combine(testDir, "update-content"), zipPath);
            return zipPath;
        }

        [Test]
        public void Update_PreservesUserDat()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                string baseDir = SetupFakeInstall(testDir);
                string zip = CreateFakeUpdateZip(testDir);
                SimulateUpdate(baseDir, zip);

                Assert.AreEqual("my-custom-keybinds",
                    File.ReadAllText(Path.Combine(baseDir, "game", "user.dat")));
            }
            finally { try { Directory.Delete(testDir, true); } catch { } }
        }

        [Test]
        public void Update_PreservesMainDat()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                string baseDir = SetupFakeInstall(testDir);
                string zip = CreateFakeUpdateZip(testDir);
                SimulateUpdate(baseDir, zip);

                Assert.AreEqual("address=192.168.1.50",
                    File.ReadAllText(Path.Combine(baseDir, "game", "main.dat")));
            }
            finally { try { Directory.Delete(testDir, true); } catch { } }
        }

        [Test]
        public void Update_PreservesKeyboardDat()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                string baseDir = SetupFakeInstall(testDir);
                string zip = CreateFakeUpdateZip(testDir);
                SimulateUpdate(baseDir, zip);

                Assert.AreEqual("my-keyboard-config",
                    File.ReadAllText(Path.Combine(baseDir, "game", "keyboard.dat")));
            }
            finally { try { Directory.Delete(testDir, true); } catch { } }
        }

        [Test]
        public void Update_OverwritesGameDll()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                string baseDir = SetupFakeInstall(testDir);
                string zip = CreateFakeUpdateZip(testDir);
                SimulateUpdate(baseDir, zip);

                Assert.AreEqual("new-patched-game-dll",
                    File.ReadAllText(Path.Combine(baseDir, "game", "game.dll")));
            }
            finally { try { Directory.Delete(testDir, true); } catch { } }
        }

        [Test]
        public void Update_OverwritesVersionTxt()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                string baseDir = SetupFakeInstall(testDir);
                string zip = CreateFakeUpdateZip(testDir);
                SimulateUpdate(baseDir, zip);

                Assert.AreEqual("new-launcher",
                    File.ReadAllText(Path.Combine(baseDir, "Play.exe")));
            }
            finally { try { Directory.Delete(testDir, true); } catch { } }
        }

        [Test]
        public void Update_NoBackupFiles_Left()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                string baseDir = SetupFakeInstall(testDir);
                string zip = CreateFakeUpdateZip(testDir);
                SimulateUpdate(baseDir, zip);

                string gameDir = Path.Combine(baseDir, "game");
                Assert.IsFalse(File.Exists(Path.Combine(gameDir, "main.dat.bak")));
                Assert.IsFalse(File.Exists(Path.Combine(gameDir, "user.dat.bak")));
                Assert.IsFalse(File.Exists(Path.Combine(gameDir, "keyboard.dat.bak")));
            }
            finally { try { Directory.Delete(testDir, true); } catch { } }
        }

        [Test]
        public void Update_PreservesFiles_WhenNoUserDatExists()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "sb-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                string baseDir = SetupFakeInstall(testDir);
                // Delete user.dat to simulate fresh install
                File.Delete(Path.Combine(baseDir, "game", "user.dat"));
                string zip = CreateFakeUpdateZip(testDir);
                SimulateUpdate(baseDir, zip);

                // Should get the default from the update since there was nothing to preserve
                Assert.AreEqual("default-user-settings",
                    File.ReadAllText(Path.Combine(baseDir, "game", "user.dat")));
                // But main.dat should still be preserved
                Assert.AreEqual("address=192.168.1.50",
                    File.ReadAllText(Path.Combine(baseDir, "game", "main.dat")));
            }
            finally { try { Directory.Delete(testDir, true); } catch { } }
        }
    }
}
