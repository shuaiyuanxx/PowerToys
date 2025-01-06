// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace AdvancedPaste.Services.OpenAI;

public struct AIModelInfo
{
    public enum AIModelProvider
    {
        OpenAI,
        AzureOpenAI,
    }

    public AIModelProvider ModelProvider { get; private set; }

    public string ModelName { get; private set; }

    public string DeployName { get; private set; }

    public string EndPoint { get; private set; }

    public string Key { get; private set; }
}
