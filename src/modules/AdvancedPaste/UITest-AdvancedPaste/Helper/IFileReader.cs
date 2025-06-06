// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.AdvancedPaste.UITests.Helper;

public interface IFileReader
{
    string ReadContent(string filePath);

    ContentType GetContentType();

    bool IsSupported(string filePath);
}

public enum ContentType
{
    PlainText,
    RichText,
}
