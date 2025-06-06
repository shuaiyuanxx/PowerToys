// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.AdvancedPaste.UITests.Helper;
using Microsoft.PowerToys.UITest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.AdvancedPaste.UITests
{
    [TestClass]
    public class AdvancedPasteUITest : UITestBase
    {
        private Process? wordpadProcess;

        public AdvancedPasteUITest()
            : base(PowerToysModule.PowerToysSettings, size: WindowSize.Small)
        {
            OpenWordPad();
        }

        [TestMethod]
        [TestCategory("UITest")]
        [TestCategory("AdvancedPaste")]
        public void TestWordDocumentCopyPaste()
        {
            // Get path to the output directory where the Word files should be copied
            Type currentTestType = typeof(AdvancedPasteUITest);
#pragma warning disable CS8604 // Possible null reference argument.
            string testFilesDir = Path.Combine(
                Path.GetDirectoryName(currentTestType.Assembly.Location),
                "wordTestFile");
#pragma warning restore CS8604 // Possible null reference argument.

            // Ensure the directory exists
            Assert.IsTrue(Directory.Exists(testFilesDir), $"Test files directory not found at: {testFilesDir}");

            string[] filePaths = Directory.GetFiles(testFilesDir);

            foreach (string filePath in filePaths)
            {
                string content = FileReader.ReadContent(filePath);
                Assert.IsNotNull(content, $"Failed to read content from file: {filePath}");

                // Copy the content to clipboard
                Clipboard.SetText(content);

                PasteClipboardContent();
            }
        }

        private IntPtr OpenWordPad()
        {
            wordpadProcess = Process.Start("write.exe");
            if (wordpadProcess == null)
            {
                throw new InvalidOperationException("Failed to start WordPad.");
            }

            wordpadProcess.WaitForInputIdle();

            IntPtr hWnd = wordpadProcess.MainWindowHandle;

            if (hWnd == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not get WordPad main window handle.");
            }

            SetForegroundWindow(hWnd);

            Thread.Sleep(500); // Wait for the window to be ready

            return hWnd;
        }

        private void PasteClipboardContent()
        {
            if (wordpadProcess == null || wordpadProcess.HasExited)
            {
                throw new InvalidOperationException("WordPad is not running.");
            }

            SetForegroundWindow(wordpadProcess.MainWindowHandle);
            Thread.Sleep(200);

            System.Windows.Forms.SendKeys.SendWait("^v");

            Thread.Sleep(500);
        }

        private void CloseWordPad(int timeout = 3000)
        {
            if (wordpadProcess == null || wordpadProcess.HasExited)
            {
                return;
            }

            try
            {
                wordpadProcess.CloseMainWindow();
                if (!wordpadProcess.WaitForExit(timeout))
                {
                    wordpadProcess.Kill(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing WordPad: {ex.Message}");
            }
            finally
            {
                wordpadProcess.Dispose();
                wordpadProcess = null;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            CloseWordPad();
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
