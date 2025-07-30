// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerToys.Settings.UI.Helpers;

namespace Microsoft.PowerToys.Settings.UI.Helpers
{
    internal sealed class LocalizationHelper
    {
        private static readonly Dictionary<(string ModuleName, int HotkeyID), string> HotkeyToResourceKeyMap = new()
        {
            // AdvancedPaste module mappings
            { ("advancedpaste", 0), "PasteAsPlainText_Shortcut" },
            { ("advancedpaste", 1), "AdvancedPasteUI_Shortcut" },
            { ("advancedpaste", 2), "PasteAsMarkdown_Shortcut" },
            { ("advancedpaste", 3), "PasteAsJson_Shortcut" },
            { ("advancedpaste", 4), "ImageToText" },
            { ("advancedpaste", 5), "PasteAsTxtFile" },
            { ("advancedpaste", 6), "PasteAsPngFile" },
            { ("advancedpaste", 7), "PasteAsHtmlFile" },
            { ("advancedpaste", 8), "TranscodeToMp3" },
            { ("advancedpaste", 9), "TranscodeToMp4" },

            // AlwaysOnTop module mappings
            { ("alwaysontop", 0), "AlwaysOnTop_ActivationShortcut" },

            // ColorPicker module mappings
            { ("colorpicker", 0), "Activation_Shortcut" },

            // CropAndLock module mappings
            { ("cropandlock", 0), "CropAndLock_ReparentActivation_Shortcut" },
            { ("cropandlock", 1), "CropAndLock_ThumbnailActivation_Shortcut" },

            // MeasureTool module mappings
            { ("measure tool", 0), "MeasureTool_ActivationShortcut" },

            // ShortcutGuide module mappings
            { ("shortcut guide", 0), "Activation_Shortcut" },

            // PowerOCR/TextExtractor module mappings
            { ("textextractor", 0), "Activation_Shortcut" },

            // Workspaces module mappings
            { ("workspaces", 0), "Workspaces_ActivationShortcut" },

            // Peek module mappings
            { ("peek", 0), "Activation_Shortcut" },

            // PowerLauncher module mappings
            { ("powertoys run", 0), "PowerLauncher_OpenPowerLauncher" },

            // MouseUtils module mappings
            { ("mousehighlighter", 0), "MouseUtils_MouseHighlighter_ActivationShortcut" },
            { ("mousejump", 0), "MouseUtils_MouseJump_ActivationShortcut" },
            { ("mousepointercrosshairs", 0), "MouseUtils_MousePointerCrosshairs_ActivationShortcut" },
            { ("findmymouse", 0), "MouseUtils_FindMyMouse_ActivationShortcut" },

            // Mouse without borders module mappings
            { ("mousewithoutborders", 0), "MouseWithoutBorders_ToggleEasyMouseShortcut" },
            { ("mousewithoutborders", 1), "MouseWithoutBorders_LockMachinesShortcut" },
            { ("mousewithoutborders", 2), "MouseWithoutBorders_Switch2AllPcShortcut" },
            { ("mousewithoutborders", 3), "MouseWithoutBorders_ReconnectShortcut" },

            // ZoomIt module mappings
            { ("zoomit", 0), "ZoomIt_Zoom_Shortcut" },
            { ("zoomit", 1), "ZoomIt_LiveZoom_Shortcut" },
            { ("zoomit", 2), "ZoomIt_Draw_Shortcut" },
            { ("zoomit", 3), "ZoomIt_Record_Shortcut" },
            { ("zoomit", 4), "ZoomIt_Snip_Shortcut" },
            { ("zoomit", 5), "ZoomIt_Break_Shortcut" },
            { ("zoomit", 6), "ZoomIt_DemoType_Shortcut" },
        };

        // Delegate for getting custom action names
        public static Func<string, int, string> GetCustomActionNameDelegate { get; set; }

        /// <summary>
        /// Gets the localized header text based on module name and hotkey name
        /// </summary>
        /// <param name="moduleName">The name of the module (case-insensitive)</param>
        /// <param name="hotkeyID">The ID of the hotkey</param>
        /// <returns>The localized header text, or the hotkey name if no resource is found</returns>
        public static string GetLocalizedHotkeyHeader(string moduleName, int hotkeyID)
        {
            if (string.IsNullOrEmpty(moduleName) || hotkeyID < 0)
            {
                return string.Empty;
            }

            var key = (moduleName.ToLowerInvariant(), hotkeyID);

            // Try to get from resource file using resource key mapping
            if (HotkeyToResourceKeyMap.TryGetValue(key, out string resourceKey))
            {
                var localizedText = GetLocalizedStringFromResource(resourceKey);
                if (!string.IsNullOrEmpty(localizedText))
                {
                    return localizedText;
                }
            }

            // Handle custom actions for AdvancedPaste, whose IDs start from 10
            if (moduleName.Equals("advancedpaste", StringComparison.OrdinalIgnoreCase) && hotkeyID > 9)
            {
                // Try to get the custom action name using the delegate
                if (GetCustomActionNameDelegate != null)
                {
                    var actionID = hotkeyID - 10; // Adjust ID for custom actions
                    var customActionName = GetCustomActionNameDelegate(moduleName, actionID);
                    if (!string.IsNullOrEmpty(customActionName))
                    {
                        return customActionName;
                    }
                }

                // Fallback to resource
                var customActionText = GetLocalizedStringFromResource("PasteAsCustom_Shortcut");
                if (!string.IsNullOrEmpty(customActionText))
                {
                    return customActionText;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets a localized string from the resource file using ResourceLoaderInstance
        /// Tries multiple variations of the resource key to handle different naming conventions
        /// </summary>
        /// <param name="resourceKey">The resource key</param>
        /// <returns>The localized string, or null if not found</returns>
        private static string GetLocalizedStringFromResource(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey))
            {
                return null;
            }

            try
            {
                var resourceLoader = ResourceLoaderInstance.ResourceLoader;
                if (resourceLoader != null)
                {
                    // Try different variations of the resource key
                    string[] keyVariations =
                    {
                        $"{resourceKey}.Header",  // Try with .Header suffix first
                        resourceKey,              // Try the key as-is
                        $"{resourceKey}/Header",  // Try with /Header suffix (some resources use this format)
                        $"{resourceKey}_Header",   // Try with _Header suffix
                    };

                    foreach (var keyVariation in keyVariations)
                    {
                        try
                        {
                            var result = resourceLoader.GetString(keyVariation);
                            if (!string.IsNullOrEmpty(result))
                            {
                                return result;
                            }
                        }
                        catch
                        {
                            // Continue to next variation
                            continue;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If resource loading fails, return null to allow fallback
            }

            return null;
        }
    }
}
