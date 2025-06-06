// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.AdvancedPaste.UITests.Helper;

public class TxtFileReader
{
    private readonly Encoding _encoding;

    public TxtFileReader(Encoding encoding)
    {
        _encoding = encoding ?? Encoding.UTF8;
    }

    public string ReadContent(string filePath)
    {
        if (!IsSupported(filePath))
        {
            throw new ArgumentException("Not a supported file format");
        }

        try
        {
            return File.ReadAllText(filePath, _encoding);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read TXT file: {ex.Message}", ex);
        }
    }

    public ContentType GetContentType()
    {
        return ContentType.PlainText;
    }

    public bool IsSupported(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        return Path.GetExtension(filePath).Equals(".txt", StringComparison.OrdinalIgnoreCase);
    }
}
