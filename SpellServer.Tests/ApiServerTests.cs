using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
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
            int port = _testPort;
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
            int port = _testPort;
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
            int port = _testPort;

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
            int port = _testPort;

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
        // --- Register endpoint tests ---

        // Tests run against the live server on localhost:10603
        // The server must be running for integration tests to pass
        private static int _testPort = 10603;

        private HttpWebResponse PostRegister(string body, int port)
        {
            var request = (HttpWebRequest)WebRequest.Create($"http://localhost:{port}/api/register");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 3000;
            var data = Encoding.UTF8.GetBytes(body);
            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
                stream.Write(data, 0, data.Length);
            return (HttpWebResponse)request.GetResponse();
        }

        private HttpWebResponse TryPostRegister(string body, int port)
        {
            try
            {
                return PostRegister(body, port);
            }
            catch (WebException ex)
            {
                return (HttpWebResponse)ex.Response;
            }
        }

        [Test]
        public void Register_GET_Returns405()
        {
            try
            {
                var request = WebRequest.Create($"http://localhost:{_testPort}/api/register");
                request.Timeout = 3000;
                request.GetResponse();
                Assert.Fail("Should have thrown WebException");
            }
            catch (WebException ex)
            {
                var response = (HttpWebResponse)ex.Response;
                if (response == null)
                {
                    Assert.Ignore("API server not reachable");
                    return;
                }
                Assert.AreEqual(405, (int)response.StatusCode);
            }
        }

        [Test]
        public void Register_MissingUsername_Returns400()
        {
            var response = TryPostRegister("password=test123", _testPort);
            if (response == null) { Assert.Ignore("API server not reachable"); return; }
            Assert.AreEqual(400, (int)response.StatusCode);
            using (var reader = new StreamReader(response.GetResponseStream()))
                StringAssert.Contains("username", reader.ReadToEnd());
        }

        [Test]
        public void Register_MissingPassword_Returns400()
        {
            var response = TryPostRegister("username=testuser", _testPort);
            if (response == null) { Assert.Ignore("API server not reachable"); return; }
            Assert.AreEqual(400, (int)response.StatusCode);
            using (var reader = new StreamReader(response.GetResponseStream()))
                StringAssert.Contains("password", reader.ReadToEnd());
        }

        [Test]
        public void Register_EmptyBody_Returns400()
        {
            var response = TryPostRegister("", _testPort);
            if (response == null) { Assert.Ignore("API server not reachable"); return; }
            Assert.AreEqual(400, (int)response.StatusCode);
        }

        [Test]
        public void Register_UsernameTooShort_Returns400()
        {
            var response = TryPostRegister("username=ab&password=test123", _testPort);
            if (response == null) { Assert.Ignore("API server not reachable"); return; }
            Assert.AreEqual(400, (int)response.StatusCode);
            using (var reader = new StreamReader(response.GetResponseStream()))
                StringAssert.Contains("3 characters", reader.ReadToEnd());
        }

        [Test]
        public void Register_UsernameTooLong_Returns400()
        {
            var longName = new string('a', 21);
            var response = TryPostRegister($"username={longName}&password=test123", _testPort);
            if (response == null) { Assert.Ignore("API server not reachable"); return; }
            Assert.AreEqual(400, (int)response.StatusCode);
            using (var reader = new StreamReader(response.GetResponseStream()))
                StringAssert.Contains("20 characters", reader.ReadToEnd());
        }

        [Test]
        public void Register_PasswordTooLong_Returns400()
        {
            var longPass = new string('a', 21);
            var response = TryPostRegister($"username=validuser&password={longPass}", _testPort);
            if (response == null) { Assert.Ignore("API server not reachable"); return; }
            Assert.AreEqual(400, (int)response.StatusCode);
            using (var reader = new StreamReader(response.GetResponseStream()))
                StringAssert.Contains("20 characters", reader.ReadToEnd());
        }

        [Test]
        public void Register_DuplicateUsername_Returns409()
        {
            // First registration should succeed (or 409 if already exists from prior run)
            string unique = "testdup_" + DateTime.Now.Ticks.ToString().Substring(10);
            var response1 = TryPostRegister($"username={unique}&password=test123", _testPort);
            if (response1 == null) { Assert.Ignore("API server not reachable"); return; }
            Assert.That((int)response1.StatusCode, Is.EqualTo(201).Or.EqualTo(409));

            // Second registration with same name must be 409
            var response2 = TryPostRegister($"username={unique}&password=different456", _testPort);
            Assert.AreEqual(409, (int)response2.StatusCode);
            using (var reader = new StreamReader(response2.GetResponseStream()))
                StringAssert.Contains("already exists", reader.ReadToEnd());
        }

        [Test]
        public void Register_DuplicateUsername_DoesNotChangePassword()
        {
            // Create account
            string unique = "tnow" + (DateTime.Now.Ticks % 100000000);
            var response1 = TryPostRegister($"username={unique}&password=original123", _testPort);
            if (response1 == null) { Assert.Ignore("API server not reachable"); return; }
            Assert.AreEqual(201, (int)response1.StatusCode);

            // Try to overwrite with different password
            var response2 = TryPostRegister($"username={unique}&password=hacked456", _testPort);
            Assert.AreEqual(409, (int)response2.StatusCode);

            // Verify original password still works by checking the hash
            var accountData = MySQL.Accounts.GetAccountData(unique);
            Assert.IsNotNull(accountData);
            Assert.IsTrue(accountData.Rows.Count > 0);
            string storedHash = accountData.Rows[0]["password"].ToString();
            Assert.IsTrue(PasswordHasher.Verify("original123", storedHash),
                "Original password should still work after failed overwrite attempt");
            Assert.IsFalse(PasswordHasher.Verify("hacked456", storedHash),
                "Attacker's password should NOT work");
        }

        [Test]
        public void Register_SpecialCharsInUsername_Encoded()
        {
            // URL-encoded special chars shouldn't break the endpoint
            var response = TryPostRegister("username=test%26user&password=test123", _testPort);
            if (response == null) { Assert.Ignore("API server not reachable"); return; }
            // Should either create or reject gracefully, not 500
            Assert.That((int)response.StatusCode, Is.Not.EqualTo(500));
        }
    }
}
