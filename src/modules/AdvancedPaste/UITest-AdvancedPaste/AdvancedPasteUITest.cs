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
                "TestFiles");
#pragma warning restore CS8604 // Possible null reference argument.

            // Ensure the directory exists
            Assert.IsTrue(Directory.Exists(testFilesDir), $"Test files directory not found at: {testFilesDir}");

            string[] filePaths = Directory.GetFiles(testFilesDir);
            string dstFilePath = Path.Combine(testFilesDir, "DstFile.rtf");
            IntPtr hWnd = OpenWordPad(dstFilePath);

            foreach (string filePath in filePaths)
            {
                string content = FileReader.ReadContent(filePath);
                Assert.IsNotNull(content, $"Failed to read content from file: {filePath}");

                SetClipboardContent(content);

                PasteClipboardContent(hWnd);
            }
        }

        private IntPtr OpenWordPad(string filePath)
        {
            wordpadProcess = Process.Start("write.exe", filePath);
            if (wordpadProcess == null)
            {
                throw new InvalidOperationException("Failed to start WordPad.");
            }

            // wordpadProcess.WaitForInputIdle();
            IntPtr hWnd = wordpadProcess.MainWindowHandle;

            if (hWnd == IntPtr.Zero)
            {
                string windowTitle = Path.GetFileName(filePath) + ".rtf - WordPad";
                hWnd = FindWindow("WordPadClass", windowTitle);
                Assert.IsNotNull(hWnd, $"Failed to find WordPad window with title: {windowTitle}");
            }

            return hWnd;
        }

        private void PasteClipboardContent(IntPtr hWnd)
        {
            SetForegroundWindow(hWnd);
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

        private void SetClipboardContent(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException("Content cannot be null or empty", nameof(content));
            }

            try
            {
                var staThread = new Thread(() =>
                {
                    try
                    {
                        System.Windows.Forms.Clipboard.SetText(content);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error setting clipboard content: {ex.Message}");
                    }
                });

                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set clipboard content: {ex.Message}");
                throw;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            CloseWordPad();
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
    }
}
