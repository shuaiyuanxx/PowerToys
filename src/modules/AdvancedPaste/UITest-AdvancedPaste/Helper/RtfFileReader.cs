// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Windows.Forms;
using RtfPipe;

namespace Microsoft.AdvancedPaste.UITests.Helper;

public class RtfFileReader : IFileReader
{
    public string ReadContent(string filePath)
    {
        if (!IsSupported(filePath))
            throw new ArgumentException("File type not supported.", nameof(filePath));

        try
        {
            string rtfContent = File.ReadAllText(filePath);

            string htmlContent = Rtf.ToHtml(rtfContent);

            return htmlContent;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read RTF file: {ex.Message}", ex);
        }
    }

    public string ReadPlainText(string filePath)
    {
        if (!IsSupported(filePath))
            throw new ArgumentException("不支持的文件格式");

        try
        {
            RichTextBox rtb = new RichTextBox();
            rtb.LoadFile(filePath, RichTextBoxStreamType.RichText);
            return rtb.Text;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"读取RTF文件失败: {ex.Message}", ex);
        }
    }

    public string ReadRawRtf(string filePath)
    {
        if (!IsSupported(filePath))
            throw new ArgumentException("不支持的文件格式");

        return File.ReadAllText(filePath);
    }

    public ContentType GetContentType()
    {
        return ContentType.RichText;
    }

    public bool IsSupported(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return false;

        return Path.GetExtension(filePath).ToLowerInvariant() == ".rtf";
    }
}
