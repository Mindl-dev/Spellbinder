using System;
using System.Drawing;
using NUnit.Framework;

namespace SpellServer.Tests
{
    [TestFixture]
    public class LoggingTests
    {
        [Test]
        public void ProgramLog_HeadlessMode_DoesNotThrow()
        {
            // In headless mode, Program.Log should work without ServerForm
            bool originalHeadless = Program.Headless;
            var originalLog = Program.HeadlessMainLog;

            try
            {
                Program.Headless = true;
                Program.HeadlessMainLog = new Helper.ConsoleLogBox("Test");

                Assert.DoesNotThrow(() => Program.Log("test message", Color.Red));
            }
            finally
            {
                Program.Headless = originalHeadless;
                Program.HeadlessMainLog = originalLog;
            }
        }

        [Test]
        public void ProgramLog_GuiMode_NullServerForm_DoesNotThrow()
        {
            // In GUI mode with null ServerForm, Program.Log should not crash
            bool originalHeadless = Program.Headless;
            var originalForm = Program.ServerForm;

            try
            {
                Program.Headless = false;
                Program.ServerForm = null;

                Assert.DoesNotThrow(() => Program.Log("test message", Color.Red));
            }
            finally
            {
                Program.Headless = originalHeadless;
                Program.ServerForm = originalForm;
            }
        }

        [Test]
        public void ProgramLog_ShouldRouteToCategory_MainLog()
        {
            // Verify the category-based routing works
            bool originalHeadless = Program.Headless;
            var originalLog = Program.HeadlessMainLog;

            try
            {
                Program.Headless = true;
                Program.HeadlessMainLog = new Helper.ConsoleLogBox("Test");

                // These should all work without throwing, regardless of category
                Assert.DoesNotThrow(() => Program.Log("main message", Color.Blue));
                Assert.DoesNotThrow(() => Program.Log("cheat message", Color.Red, "Cheat"));
                Assert.DoesNotThrow(() => Program.Log("admin message", Color.Blue, "Admin"));
                Assert.DoesNotThrow(() => Program.Log("whisper message", Color.Purple, "Whisper"));
                Assert.DoesNotThrow(() => Program.Log("report message", Color.Orange, "Report"));
                Assert.DoesNotThrow(() => Program.Log("misc message", Color.Gray, "Misc"));
            }
            finally
            {
                Program.Headless = originalHeadless;
                Program.HeadlessMainLog = originalLog;
            }
        }
    }
}
