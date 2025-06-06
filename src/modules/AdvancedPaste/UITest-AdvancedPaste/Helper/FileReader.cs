// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.AdvancedPaste.UITests.Helper;

public class FileReader
{
    public static string ReadContent(string filePath)
    {
        try
        {
            return File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read file: {ex.Message}", ex);
        }
    }

    public static string ReadRTFPlainText(string filePath)
    {
        try
        {
            using (var rtb = new System.Windows.Forms.RichTextBox())
            {
                rtb.Rtf = File.ReadAllText(filePath);
                return rtb.Text;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read plain text from file: {ex.Message}", ex);
        }
    }
}
