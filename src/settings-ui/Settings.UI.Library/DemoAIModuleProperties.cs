// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

using Settings.UI.Library.Attributes;

namespace Microsoft.PowerToys.Settings.UI.Library
{
    public class DemoAIModuleProperties
    {
        public static readonly HotkeySettings DefaultDemoAIModuleUIShortcut = new HotkeySettings(true, false, false, true, 0x44); // Win+Shift+D

        public DemoAIModuleProperties()
        {
            DemoAIModuleUIShortcut = DefaultDemoAIModuleUIShortcut;
        }

        [JsonPropertyName("demo-ai-module-ui-hotkey")]
        public HotkeySettings DemoAIModuleUIShortcut { get; set; }

        public override string ToString()
            => JsonSerializer.Serialize(this);
    }
}
