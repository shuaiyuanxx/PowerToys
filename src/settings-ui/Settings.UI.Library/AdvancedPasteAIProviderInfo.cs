// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.PowerToys.Settings.UI.Library.Helpers;

namespace Microsoft.PowerToys.Settings.UI.Library;

public sealed class AdvancedPasteAIProviderInfo : Observable, INotifyPropertyChanged
{
    private string _providername = string.Empty;
    private string _modelName = string.Empty;
    private string _deployname = string.Empty;
    private string _endPointCredentialName = string.Empty;
    private string _keyCredentialName = string.Empty;

    [JsonPropertyName("ProviderName")]
    public string ProviderName
    {
        get => _providername;
        set => Set(ref _providername, value);
    }

    [JsonPropertyName("ModelName")]
    public string ModelName
    {
        get => _modelName;
        set => Set(ref _modelName, value);
    }

    [JsonPropertyName("DeployName")]
    public string DeployName
    {
        get => _deployname;
        set => Set(ref _deployname, value);
    }

    [JsonPropertyName("EndPointCredentialName")]
    public string EndPointCredentialName
    {
        get => _endPointCredentialName;
        set => Set(ref _endPointCredentialName, value);
    }

    [JsonPropertyName("KeyCredentialName")]
    public string KeyCredentialName
    {
        get => _keyCredentialName;
        set => Set(ref _keyCredentialName, value);
    }
}
