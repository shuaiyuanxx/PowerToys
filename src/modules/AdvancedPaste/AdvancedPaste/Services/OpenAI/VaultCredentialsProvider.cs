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
        Refresh();
    }

    public string Key { get; private set; }

    public string Endpoint { get; private set; }

    public bool IsConfigured => _userSettings.UserPreferModel != null;

    private readonly IUserSettings _userSettings;

    public bool Refresh()
    {
        var oldKey = Key;
        var oldEndpoint = Endpoint;

        Key = LoadKey();
        Endpoint = LoadEndpoint();

        return (oldKey != Key) ||
            oldEndpoint != Endpoint;
    }

    private string LoadKey()
    {
        try
        {
            string resource = _userSettings.UserPreferModel.ResourceName;
            string userName = _userSettings.UserPreferModel.KeyCredentialName;
            return new PasswordVault().Retrieve(resource, userName)?.Password ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private string LoadEndpoint()
    {
        try
        {
            string resource = _userSettings.UserPreferModel.ResourceName;
            string userName = _userSettings.UserPreferModel.EndPointCredentialName;
            return new PasswordVault().Retrieve(resource, userName)?.Password ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
