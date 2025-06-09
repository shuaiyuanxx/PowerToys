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
        private readonly string wordpadPath = @"C:\Program Files\wordpad\wordpad.exe";

        // private readonly string pasteAsMarkdownFileName = "PasteAsMarkdownFile.html";

        // private readonly string pasteAsJsonFileName = "PasteAsJsonFile.xml";
        public AdvancedPasteUITest()
            : base(PowerToysModule.PowerToysSettings, size: WindowSize.Small)
        {
            Type currentTestType = typeof(AdvancedPasteUITest);
            string? dirName = Path.GetDirectoryName(currentTestType.Assembly.Location);
            Assert.IsNotNull(dirName, "Failed to get directory name of the current test assembly.");

            string testFilesFolder = Path.Combine(dirName, "TestFiles");
            Assert.IsTrue(Directory.Exists(testFilesFolder), $"Test files directory not found at: {testFilesFolder}");

            testFilesFolderPath = testFilesFolder;
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

        private void SetupClipboardFromRtfFile(string testFileName)
        {
            string filePath = Path.Combine(testFilesFolderPath, testFileName);

            Process srcWordpadProcess = Process.Start(wordpadPath, filePath);
            if (srcWordpadProcess == null)
            {
                throw new InvalidOperationException("Failed to start WordPad.");
            }

            IntPtr hWnd = srcWordpadProcess.MainWindowHandle;

            if (hWnd == IntPtr.Zero)
            {
                string windowTitle = Path.GetFileName(filePath) + " - WordPad";
                hWnd = FindWindow("WordPadClass", windowTitle);
                Assert.IsNotNull(hWnd, $"Failed to find WordPad window with title: {windowTitle}");
            }

            SetForegroundWindow(hWnd);
            Thread.Sleep(500);

            this.SendKeys(Key.LCtrl, Key.A); // Select all text
            Thread.Sleep(500);
            this.SendKeys(Key.LCtrl, Key.C); // Copy selected text
            Thread.Sleep(500);
            this.SendKeys(Key.LCtrl, Key.V); // Copy selected text
            Thread.Sleep(500);
            this.SendKeys(Key.LCtrl, Key.V); // Copy selected text
            Thread.Sleep(500);

            srcWordpadProcess.Kill(true);
            Thread.Sleep(300);
        }

        private void TestCasePasteAsPlainText()
        {
            // Copy some rich text(e.g word of the text is different color, another work is bold, underlined, etd.).
            SetupClipboardFromRtfFile(pasteAsPlainTextFileName);
            string rawClipboardContent = GetClipboardContent();

            // Paste the text using standard Windows Ctrl + V shortcut and ensure that rich text is pasted(with all colors, formatting, etc.)
            PasteClipboardContent(Key.LCtrl, Key.V);
            string clipboardContent = GetClipboardContent();
            Assert.IsTrue(
                string.Equals(clipboardContent.Substring(0, 20), rawClipboardContent.Substring(0, 20), StringComparison.Ordinal),
                "The pasted content should not be changed.");

            // Paste the text using Paste As Plain Text activation shortcut and ensure that plain text without any formatting is pasted.
            PasteClipboardContent(Key.Win, Key.LCtrl, Key.Alt, Key.V);
            clipboardContent = GetClipboardContent();
            string plainTextFromFile = GetTestContentFromFile(pasteAsPlainTextFileName, readPlainText: true);
            Assert.IsTrue(
                string.Equals(clipboardContent, plainTextFromFile, StringComparison.Ordinal),
                "The pasted content should be plain text without any formatting.");

            // Paste again the text using standard Windows Ctrl + V shortcut and ensure the text is now pasted plain without formatting as well.
            PasteClipboardContent();
            clipboardContent = GetClipboardContent();
            Assert.IsTrue(
                string.Equals(clipboardContent, plainTextFromFile, StringComparison.Ordinal),
                "The pasted content should be plain text without any formatting.");

            // Copy some rich text again.
            SetupClipboardFromRtfFile(pasteAsPlainTextFileName);

            // Open Advanced Paste window using hotkey, click Paste as Plain Text button and confirm that plain text without any formatting is pasted.
            Thread.Sleep(200);
            SendKeys(Key.Win, Key.Shift, Key.V); // Open Advanced Paste window
            Thread.Sleep(500);

            // ToDo: Implement a way to interact with the Advanced Paste window and click the Paste as Plain Text button.

            // Copy some rich text again.

            // Open Advanced Paste window using hotkey, press Ctrl + 1 and confirm that plain text without any formatting is pasted.
        }

        private void PasteClipboardContent(params Key[] keys)
        {
            string tempFile = Path.Combine(testFilesFolderPath, "Document.rtf");

            Process wordpadProcess = Process.Start(wordpadPath, tempFile);
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

            SetForegroundWindow(hWnd);
            Thread.Sleep(200);

            this.SendKeys(keys);
            Thread.Sleep(500);

            wordpadProcess.Kill(true);
            Thread.Sleep(500);
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

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
    }
}
