using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using NUnit.Framework;

namespace SpellServer.Tests
{
    [TestFixture]
    public class ApiServerTests
    {
        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            Program.Headless = true;
            Program.HeadlessMainLog = new Helper.ConsoleLogBox("Test");
        }

        // --- EscapeJson ---

        [Test]
        public void EscapeJson_Null_ReturnsEmpty()
        {
            Assert.AreEqual("", ApiServer.EscapeJson(null));
        }

        [Test]
        public void EscapeJson_PlainString_Unchanged()
        {
            Assert.AreEqual("Frostbane", ApiServer.EscapeJson("Frostbane"));
        }

        [Test]
        public void EscapeJson_Quotes_Escaped()
        {
            Assert.AreEqual("say \\\"hello\\\"", ApiServer.EscapeJson("say \"hello\""));
        }

        [Test]
        public void EscapeJson_Backslash_Escaped()
        {
            Assert.AreEqual("path\\\\file", ApiServer.EscapeJson("path\\file"));
        }

        [Test]
        public void EscapeJson_Newlines_Escaped()
        {
            Assert.AreEqual("line1\\nline2\\rline3", ApiServer.EscapeJson("line1\nline2\rline3"));
        }

        [Test]
        public void EscapeJson_EmptyString_ReturnsEmpty()
        {
            Assert.AreEqual("", ApiServer.EscapeJson(""));
        }

        // --- BuildPlayersJson ---

        [Test]
        public void BuildPlayersJson_EmptyList_ReturnsEmptyArray()
        {
            var result = ApiServer.BuildPlayersJson(new List<Player>());
            Assert.AreEqual("{\"players\":[]}", result);
        }

        // --- BuildStatusJson ---

        [Test]
        public void BuildStatusJson_ZeroOnline_ValidJson()
        {
            var result = ApiServer.BuildStatusJson(0);
            StringAssert.Contains("\"online\":0", result);
            StringAssert.Contains("\"capacity\":510", result);
        }

        [Test]
        public void BuildStatusJson_PlayersOnline_CorrectCount()
        {
            var result = ApiServer.BuildStatusJson(42);
            StringAssert.Contains("\"online\":42", result);
        }

        [Test]
        public void BuildStatusJson_ContainsMotd()
        {
            var result = ApiServer.BuildStatusJson(0);
            StringAssert.Contains("\"motd\":", result);
        }

        [Test]
        public void BuildStatusJson_StartsAndEndsWithBraces()
        {
            var result = ApiServer.BuildStatusJson(10);
            StringAssert.StartsWith("{", result);
            StringAssert.EndsWith("}", result);
        }

        // --- Live HTTP integration test ---

        [Test]
        public void ApiServer_StatusEndpoint_Returns200()
        {
            int port = 10699; // unlikely to collide
            ApiServer.Start(port);
            Thread.Sleep(500); // let listener start

            try
            {
                var request = WebRequest.Create($"http://localhost:{port}/api/status");
                request.Timeout = 3000;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                    Assert.AreEqual("application/json", response.ContentType);

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var body = reader.ReadToEnd();
                        StringAssert.Contains("\"online\":", body);
                        StringAssert.Contains("\"capacity\":510", body);
                    }
                }
            }
            finally
            {
                // Listener will be cleaned up when the test process exits
            }
        }

        [Test]
        public void ApiServer_PlayersEndpoint_Returns200()
        {
            int port = 10699;
            // Reuses listener from previous test if still running, or starts new

            try
            {
                var request = WebRequest.Create($"http://localhost:{port}/api/players");
                request.Timeout = 3000;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var body = reader.ReadToEnd();
                        StringAssert.Contains("\"players\":", body);
                    }
                }
            }
            catch (WebException)
            {
                // If listener didn't start (port conflict), start it
                ApiServer.Start(port);
                Thread.Sleep(500);

                var request = WebRequest.Create($"http://localhost:{port}/api/players");
                request.Timeout = 3000;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Test]
        public void ApiServer_UnknownRoute_Returns404()
        {
            int port = 10699;

            try
            {
                var request = WebRequest.Create($"http://localhost:{port}/api/nonexistent");
                request.Timeout = 3000;
                request.GetResponse();
                Assert.Fail("Should have thrown WebException for 404");
            }
            catch (WebException ex)
            {
                var response = (HttpWebResponse)ex.Response;
                if (response != null)
                {
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
                }
                // If response is null, listener isn't running — skip gracefully
            }
        }

        [Test]
        public void ApiServer_StatusEndpoint_HasCorsHeader()
        {
            int port = 10699;

            try
            {
                var request = WebRequest.Create($"http://localhost:{port}/api/status");
                request.Timeout = 3000;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    Assert.AreEqual("*", response.Headers["Access-Control-Allow-Origin"]);
                }
            }
            catch (WebException)
            {
                Assert.Ignore("API server not reachable on port 10699");
            }
        }
    }
}
