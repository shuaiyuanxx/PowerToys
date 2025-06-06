// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using Microsoft.PowerToys.UITest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.AdvancedPaste.UITests
{
    [TestClass]
    public class AdvancedPasteUITest : UITestBase
    {
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
                "wordTestFile");
#pragma warning restore CS8604 // Possible null reference argument.

            // Ensure the directory exists
            Assert.IsTrue(Directory.Exists(testFilesDir), $"Test files directory not found at: {testFilesDir}");

            // Create working copies of the test files
            string tempDirectory = Path.Combine(Path.GetTempPath(), "AdvancedPasteUITestDocFile_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            string sourceFilePath = Path.Combine(tempDirectory, "SourceDocument.docx");
            string targetFilePath = Path.Combine(tempDirectory, "TargetDocument.docx");

            // Copy the test files from the output directory
            File.Copy(Path.Combine(testFilesDir, "testSrc.docx"), sourceFilePath);
            File.Copy(Path.Combine(testFilesDir, "testDst.docx"), targetFilePath);

            try
            {
                // Initialize Word documents manager
                using (var wordManager = new WordManager())
                {
                    // Open Word documents
                    wordManager.OpenDocuments(sourceFilePath, targetFilePath);

                    // Add test content to the documents
                    wordManager.AddTestContent(10, 5);

                    // Get and verify initial content
                    string[] sourceContent = wordManager.GetSourceContent();
                    string[] targetContent = wordManager.GetTargetContent();

                    Assert.AreEqual(10, sourceContent.Length, "Source document should have 10 lines");
                    Assert.AreEqual(5, targetContent.Length, "Target document should have 5 lines");

                    // Copy specific lines from source document
                    string copiedText = wordManager.CopyTextFromSource(2, 4);

                    // Verify that text was copied correctly
                    Assert.IsTrue(copiedText.Contains("Source document line 2"), "Copied text should contain line 2");
                    Assert.IsTrue(copiedText.Contains("Source document line 3"), "Copied text should contain line 3");
                    Assert.IsTrue(copiedText.Contains("Source document line 4"), "Copied text should contain line 4");

                    // Paste text to target document at line 3
                    wordManager.PasteTextToTarget(3);

                    // Allow Word some time to process
                    Thread.Sleep(1000);

                    // Get updated content and verify paste operation
                    string[] updatedTargetContent = wordManager.GetTargetContent();

                    // Check that content was inserted properly
                    Assert.IsTrue(
                        updatedTargetContent.Length > targetContent.Length,
                        "Target document should have more lines after paste operation");

                    // Find the pasted content in the target document
                    bool foundLine2 = false;
                    bool foundLine3 = false;
                    bool foundLine4 = false;

                    foreach (string line in updatedTargetContent)
                    {
                        if (line.Contains("Source document line 2"))
                        {
                            foundLine2 = true;
                        }

                        if (line.Contains("Source document line 3"))
                        {
                            foundLine3 = true;
                        }

                        if (line.Contains("Source document line 4"))
                        {
                            foundLine4 = true;
                        }
                    }

                    Assert.IsTrue(foundLine2, "Target document should contain 'Source document line 2'");
                    Assert.IsTrue(foundLine3, "Target document should contain 'Source document line 3'");
                    Assert.IsTrue(foundLine4, "Target document should contain 'Source document line 4'");
                }
            }
            finally
            {
                // Clean up temporary files
                try
                {
                    if (File.Exists(sourceFilePath))
                    {
                        File.Delete(sourceFilePath);
                    }

                    if (File.Exists(targetFilePath))
                    {
                        File.Delete(targetFilePath);
                    }

                    if (Directory.Exists(tempDirectory))
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cleanup error: {ex.Message}");
                }
            }
        }
    }
}
