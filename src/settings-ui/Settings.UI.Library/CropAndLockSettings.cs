// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.PowerToys.Settings.UI.Library.Helpers;
using Microsoft.PowerToys.Settings.UI.Library.Interfaces;

namespace Microsoft.PowerToys.Settings.UI.Library
{
    public class CropAndLockSettings : BasePTModuleSettings, ISettingsConfig, IHotkeyConfig
    {
        public const string ModuleName = "CropAndLock";
        public const string ModuleVersion = "0.0.1";

        public CropAndLockSettings()
        {
            Name = ModuleName;
            Version = ModuleVersion;
            Properties = new CropAndLockProperties();
        }

        [JsonPropertyName("properties")]
        public CropAndLockProperties Properties { get; set; }

        public string GetModuleName()
        {
            return Name;
        }

        public Dictionary<string, HotkeyAccessor[]> GetAllHotkeyAccessors()
        {
            var hotkeyAccessors = new List<HotkeyAccessor>
            {
                new HotkeyAccessor(
                    () => Properties.ReparentHotkey.Value,
                    value => Properties.ReparentHotkey.Value = value,
                    "CropAndLock_ReparentActivation_Shortcut"),
                new HotkeyAccessor(
                    () => Properties.ThumbnailHotkey.Value,
                    value => Properties.ThumbnailHotkey.Value = value,
                    "CropAndLock_ThumbnailActivation_Shortcut"),
            };

            var hotkeysDict = new Dictionary<string, HotkeyAccessor[]>
            {
                [ModuleName] = hotkeyAccessors.ToArray(),
            };

            return hotkeysDict;
        }

        public bool UpgradeSettingsConfiguration()
        {
            return false;
        }
    }
}
