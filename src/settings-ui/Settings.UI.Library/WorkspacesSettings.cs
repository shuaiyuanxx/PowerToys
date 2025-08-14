// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerToys.Settings.UI.Library.Helpers;
using Microsoft.PowerToys.Settings.UI.Library.Interfaces;

namespace Microsoft.PowerToys.Settings.UI.Library
{
    public class WorkspacesSettings : BasePTModuleSettings, ISettingsConfig, IHotkeyConfig
    {
        public const string ModuleName = "Workspaces";
        public const string ModuleVersion = "0.0.1";
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public WorkspacesSettings()
        {
            Name = ModuleName;
            Version = ModuleVersion;
            Properties = new WorkspacesProperties();
        }

        [JsonPropertyName("properties")]
        public WorkspacesProperties Properties { get; set; }

        public string GetModuleName()
        {
            return Name;
        }

        public bool UpgradeSettingsConfiguration()
        {
            return false;
        }

        public Dictionary<string, HotkeyAccessor[]> GetAllHotkeyAccessors()
        {
            var hotkeyAccessors = new List<HotkeyAccessor>
            {
                new HotkeyAccessor(
                    () => Properties.Hotkey.Value,
                    value => Properties.Hotkey.Value = value,
                    "Workspaces_ActivationShortcut"),
            };

            var hotkeysDict = new Dictionary<string, HotkeyAccessor[]>
            {
                [ModuleName] = hotkeyAccessors.ToArray(),
            };

            return hotkeysDict;
        }

        public virtual void Save(ISettingsUtils settingsUtils)
        {
            // Save settings to file
            var options = _serializerOptions;

            ArgumentNullException.ThrowIfNull(settingsUtils);

            settingsUtils.SaveSettings(JsonSerializer.Serialize(this, options), ModuleName);
        }
    }
}
