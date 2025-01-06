// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using AdvancedPaste.Settings;
using Microsoft.PowerToys.Settings.UI.Library;
using Windows.Security.Credentials;

namespace AdvancedPaste.Services.OpenAI;

public sealed class VaultCredentialsProvider : IAICredentialsProvider
{
    public VaultCredentialsProvider(IUserSettings usersettings)
    {
        _userSettings = usersettings;
        AIProviders = _userSettings.AIProviders;
        Refresh();
    }

    public string Key { get; private set; }

    public string AzureOpenAIKey { get; private set; }

    public string AzureOpenAIEndpoint { get; private set; }

    public bool IsConfigured => AIProviders.Count > 0;

    private readonly IUserSettings _userSettings;

    private List<AIModelInfo> AvailableAIModels { get; set; } = new List<AIModelInfo>();

    public List<AdvancedPasteAIProviderInfo> AIProviders { get; set; }

    public bool Refresh()
    {
        var oldKey = Key;
        var oldAzureOpenAIKey = AzureOpenAIKey;
        var oldAzureOpenAIEndpoint = AzureOpenAIEndpoint;

        Key = LoadOpenAIKey();
        AzureOpenAIKey = LoadAzureOpenAIKey();
        AzureOpenAIEndpoint = LoadAzureOpenAIEndpoint();

        return (oldKey != Key) ||
            (oldAzureOpenAIKey != AzureOpenAIKey ||
            oldAzureOpenAIEndpoint != AzureOpenAIEndpoint);
    }

    public AIModelInfo? GetModelInfo(AIModelInfo.AIModelProvider modelProvider, string modelName)
    {
        foreach (var aiModel in AvailableAIModels)
        {
            if (modelProvider == aiModel.ModelProvider && modelName == aiModel.ModelName)
            {
                return aiModel;
            }
        }

        return null;
    }

    private string LoadOpenAIKey()
    {
        if (AIProviders.Count == 0)
        {
            return string.Empty;
        }

        foreach (var provider in AIProviders)
        {
            if (provider.ProviderName == "OpenAI")
            {
                try
                {
                    return new PasswordVault().Retrieve("https://platform.openai.com/api-keys", provider.KeyCredentialName)?.Password ?? string.Empty;
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        return string.Empty;
    }

    private string LoadAzureOpenAIKey()
    {
        if (AIProviders.Count == 0)
        {
            return string.Empty;
        }

        foreach (var provider in AIProviders)
        {
            if (provider.ProviderName == "Azure OpenAI")
            {
                try
                {
                    return new PasswordVault().Retrieve("PowerToysAdvancedPasteAzureOpenAI", provider.KeyCredentialName)?.Password ?? string.Empty;
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        return string.Empty;
    }

    private string LoadAzureOpenAIEndpoint()
    {
        if (AIProviders.Count == 0)
        {
            return string.Empty;
        }

        foreach (var provider in AIProviders)
        {
            if (provider.ProviderName == "Azure OpenAI")
            {
                try
                {
                    return new PasswordVault().Retrieve("PowerToysAdvancedPasteAzureOpenAI", provider.EndPointCredentialName)?.Password ?? string.Empty;
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        return string.Empty;
    }
}
