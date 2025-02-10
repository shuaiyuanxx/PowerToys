// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Timers;

using global::PowerToys.GPOWrapper;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.Library.Helpers;
using Microsoft.PowerToys.Settings.UI.Library.Interfaces;
using Microsoft.PowerToys.Settings.UI.Library.ViewModels.Commands;
using Microsoft.PowerToys.Settings.Utilities;

namespace Microsoft.PowerToys.Settings.UI.ViewModels;

public class DemoAIModuleViewModel : Observable
{
    private GeneralSettings GeneralSettingsConfig { get; set; }

    private readonly DemoAIModuleSettings _demoAIModuleSettings;
    private readonly ISettingsUtils _settingsUtils;

    private Func<string, int> SendConfigMSG { get; }

    public DemoAIModuleViewModel(
        ISettingsUtils settingsUtils,
        ISettingsRepository<GeneralSettings> settingsRepository,
        ISettingsRepository<DemoAIModuleSettings> demoAIModuleSettingsRepository,
        Func<string, int> ipcMSGCallBackFunc)
    {
        // To obtain the general settings configurations of PowerToys Settings.
        ArgumentNullException.ThrowIfNull(settingsRepository);

        GeneralSettingsConfig = settingsRepository.SettingsConfig;

        // To obtain the settings configurations of Fancy zones.
        ArgumentNullException.ThrowIfNull(settingsRepository);

        _settingsUtils = settingsUtils ?? throw new ArgumentNullException(nameof(settingsUtils));

        ArgumentNullException.ThrowIfNull(demoAIModuleSettingsRepository);

        _demoAIModuleSettings = demoAIModuleSettingsRepository.SettingsConfig;

        SendConfigMSG = ipcMSGCallBackFunc;
    }

    public HotkeySettings DemoAIModuleUIShortcut
    {
        get => _demoAIModuleSettings.Properties.DemoAIModuleUIShortcut;
        set
        {
            if (_demoAIModuleSettings.Properties.DemoAIModuleUIShortcut != value)
            {
                _demoAIModuleSettings.Properties.DemoAIModuleUIShortcut = value ?? DemoAIModuleProperties.DefaultDemoAIModuleUIShortcut;
                OnPropertyChanged(nameof(DemoAIModuleUIShortcut));

                SaveAndNotifySettings();
            }
        }
    }

    private void SaveAndNotifySettings()
    {
        _settingsUtils.SaveSettings(_demoAIModuleSettings.ToJsonString(), DemoAIModuleSettings.ModuleName);
        NotifySettingsChanged();
    }

    private void NotifySettingsChanged()
    {
        // Using InvariantCulture as this is an IPC message
        SendConfigMSG(
            string.Format(
                CultureInfo.InvariantCulture,
                "{{ \"powertoys\": {{ \"{0}\": {1} }} }}",
                AdvancedPasteSettings.ModuleName,
                JsonSerializer.Serialize(_demoAIModuleSettings)));
    }

    private void InitializeEnabledValue()
    {
        Debug.WriteLine("Initialize.");
    }

    public void RefreshEnabledState()
    {
        InitializeEnabledValue();

        // OnPropertyChanged(nameof(Enabled));
    }
}
