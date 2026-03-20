using System;
using System.Collections.Generic;
using System.Net;
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
    }
}
