// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.AdvancedPaste.UITests.Helper;
using Microsoft.CodeCoverage.Core.Reports.Coverage;
using Microsoft.PowerToys.UITest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace Microsoft.AdvancedPaste.UITests
{
    [TestClass]
    public class AdvancedPasteUITest : UITestBase
    {
        private readonly string testFilesFolderPath;
        private readonly string pasteAsPlainTextFileName = "PasteAsPlainTextFile.rtf";

        // private readonly string pasteAsMarkdownFileName = "PasteAsMarkdownFile.html";

        // private readonly string pasteAsJsonFileName = "PasteAsJsonFile.xml";
        private Process? wordpadProcess;
        private IntPtr wordpadHWnd;

        public AdvancedPasteUITest()
            : base(PowerToysModule.PowerToysSettings, size: WindowSize.Small)
        {
            Type currentTestType = typeof(AdvancedPasteUITest);
            string? dirName = Path.GetDirectoryName(currentTestType.Assembly.Location);
            Assert.IsNotNull(dirName, "Failed to get directory name of the current test assembly.");

            string testFilesFolder = Path.Combine(dirName, "TestFiles");
            Assert.IsTrue(Directory.Exists(testFilesFolder), $"Test files directory not found at: {testFilesFolder}");

            testFilesFolderPath = testFilesFolder;

            // Open a temporary WordPad file for testing
            OpenWordPad();
        }

        [TestMethod]
        [TestCategory("UITest")]
        [TestCategory("AdvancedPaste")]
        public void TestWordDocumentCopyPaste()
        {
            TestCasePasteAsPlainText();
        }

        private string GetTestContentFromFile(string testFileName, bool readPlainText = false)
        {
            string testFilesPath = Path.Combine(testFilesFolderPath, testFileName);
            Assert.IsTrue(File.Exists(testFilesPath), $"Test file not found at: {testFilesPath}");

            string content = readPlainText ? FileReader.ReadRTFPlainText(testFilesPath) : FileReader.ReadContent(testFilesPath);
            Assert.IsNotNull(content, $"Failed to read content from file: {testFilesPath}");

            return content;
        }

        private void TestCasePasteAsPlainText()
        {
            // Copy some rich text(e.g word of the text is different color, another work is bold, underlined, etd.).
            string content = GetTestContentFromFile(pasteAsPlainTextFileName);
            SetClipboardContent(content);

            // Paste the text using standard Windows Ctrl + V shortcut and ensure that rich text is pasted(with all colors, formatting, etc.)
            PasteClipboardContent(Key.LCtrl, Key.V);
            string clipboardContent = GetClipboardContent();
            Assert.IsTrue(
                string.Equals(clipboardContent, content, StringComparison.Ordinal),
                "The pasted content should not be changed.");

            // Paste the text using Paste As Plain Text activation shortcut and ensure that plain text without any formatting is pasted.
            PasteClipboardContent(Key.Win, Key.LCtrl, Key.Alt, Key.V);
            clipboardContent = GetClipboardContent();
            string plainTextFromFile = GetTestContentFromFile(pasteAsPlainTextFileName, readPlainText: true);
            Assert.IsTrue(
                string.Equals(clipboardContent, plainTextFromFile, StringComparison.Ordinal),
                "The pasted content should be plain text without any formatting.");

            // Paste again the text using standard Windows Ctrl + V shortcut and ensure the text is now pasted plain without formatting as well.
            PasteClipboardContent(Key.LCtrl, Key.V);
            clipboardContent = GetClipboardContent();
            Assert.IsTrue(
                string.Equals(clipboardContent, plainTextFromFile, StringComparison.Ordinal),
                "The pasted content should be plain text without any formatting.");

            // Copy some rich text again.
            content = GetTestContentFromFile(pasteAsPlainTextFileName);
            SetClipboardContent(content);

            // Open Advanced Paste window using hotkey, click Paste as Plain Text button and confirm that plain text without any formatting is pasted.
            SetForegroundWindow(wordpadHWnd);
            Thread.Sleep(200);
            SendKeys(Key.Win, Key.Shift, Key.V); // Open Advanced Paste window
            Thread.Sleep(500);

            // ToDo: Implement a way to interact with the Advanced Paste window and click the Paste as Plain Text button.

            // Copy some rich text again.

            // Open Advanced Paste window using hotkey, press Ctrl + 1 and confirm that plain text without any formatting is pasted.
        }

        private void OpenWordPad()
        {
            string tempFile = Path.Combine(testFilesFolderPath, "DstFile.rtf");

            wordpadProcess = Process.Start("write.exe", tempFile);
            if (wordpadProcess == null)
            {
                throw new InvalidOperationException("Failed to start WordPad.");
            }

            IntPtr hWnd = wordpadProcess.MainWindowHandle;

            if (hWnd == IntPtr.Zero)
            {
                string windowTitle = Path.GetFileName(tempFile) + " - WordPad";
                hWnd = FindWindow("WordPadClass", windowTitle);
                Assert.IsNotNull(hWnd, $"Failed to find WordPad window with title: {windowTitle}");
            }

            wordpadHWnd = hWnd;
        }

        private void PasteClipboardContent(params Key[] keys)
        {
            SetForegroundWindow(wordpadHWnd);
            Thread.Sleep(200);

            SendKeys(keys);

            Thread.Sleep(500);
        }

        private void CloseWordPad()
        {
            if (wordpadProcess == null)
            {
                return;
            }

            wordpadProcess.Kill(true);

            wordpadProcess.Dispose();
            wordpadProcess = null;
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

        private string GetClipboardContent()
        {
            string clipboardText = string.Empty;
            var staThread = new Thread(() =>
            {
                try
                {
                    if (System.Windows.Forms.Clipboard.ContainsText())
                    {
                        clipboardText = System.Windows.Forms.Clipboard.GetText();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting clipboard content: {ex.Message}");
                }
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            return clipboardText;
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
