﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.PowerToys.Settings.UI.Helpers;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.Library.Helpers;
using Microsoft.PowerToys.Settings.UI.Library.HotkeyConflicts;
using Microsoft.PowerToys.Settings.UI.Services;

namespace Microsoft.PowerToys.Settings.UI.ViewModels
{
    public abstract class PageViewModelBase : Observable
    {
        private readonly Dictionary<string, bool> _hotkeyConflictStatus = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> _hotkeyConflictTooltips = new Dictionary<string, string>();

        protected abstract string ModuleName { get; }

        protected PageViewModelBase()
        {
            if (GlobalHotkeyConflictManager.Instance != null)
            {
                GlobalHotkeyConflictManager.Instance.ConflictsUpdated += OnConflictsUpdated;
            }
        }

        public virtual void OnPageLoaded()
        {
            Debug.WriteLine($"=== PAGE LOADED: {ModuleName} ===");
            GlobalHotkeyConflictManager.Instance?.RequestAllConflicts();
        }

        /// <summary>
        /// Handles updates to hotkey conflicts for the module. This method is called when the
        /// <see cref="GlobalHotkeyConflictManager"/> raises the <c>ConflictsUpdated</c> event.
        /// </summary>
        /// <param name="sender">The source of the event, typically the <see cref="GlobalHotkeyConflictManager"/> instance.</param>
        /// <param name="e">An <see cref="AllHotkeyConflictsEventArgs"/> object containing details about the hotkey conflicts.</param>
        /// <remarks>
        /// Derived classes can override this method to provide custom handling for hotkey conflicts.
        /// Ensure that the overridden method maintains the expected behavior of processing and logging conflict data.
        /// </remarks>
        protected virtual void OnConflictsUpdated(object sender, AllHotkeyConflictsEventArgs e)
        {
            UpdateHotkeyConflictStatus(e.Conflicts);
            var allHotkeySettings = GetAllHotkeySettings();

            void UpdateConflictProperties()
            {
                if (allHotkeySettings != null)
                {
                    foreach (KeyValuePair<string, HotkeySettings[]> kvp in allHotkeySettings)
                    {
                        var module = kvp.Key;
                        var hotkeyList = kvp.Value;

                        for (int i = 0; i < hotkeyList.Length; i++)
                        {
                            var key = $"{module}_{i}";
                            hotkeyList[i].HasConflict = GetHotkeyConflictStatus(key);
                            hotkeyList[i].ConflictDescription = GetHotkeyConflictTooltip(key);
                        }
                    }
                }
            }

            _ = Task.Run(() =>
            {
                try
                {
                    var settingsWindow = App.GetSettingsWindow();
                    settingsWindow.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, UpdateConflictProperties);
                }
                catch
                {
                    UpdateConflictProperties();
                }
            });
        }

        protected virtual Dictionary<string, HotkeySettings[]> GetAllHotkeySettings()
        {
            return null;
        }

        protected ModuleConflictsData GetModuleRelatedConflicts(AllHotkeyConflictsData allConflicts)
        {
            var moduleConflicts = new ModuleConflictsData();

            if (allConflicts.InAppConflicts != null)
            {
                foreach (var conflict in allConflicts.InAppConflicts)
                {
                    if (IsModuleInvolved(conflict))
                    {
                        moduleConflicts.InAppConflicts.Add(conflict);
                    }
                }
            }

            if (allConflicts.SystemConflicts != null)
            {
                foreach (var conflict in allConflicts.SystemConflicts)
                {
                    if (IsModuleInvolved(conflict))
                    {
                        moduleConflicts.SystemConflicts.Add(conflict);
                    }
                }
            }

            return moduleConflicts;
        }

        protected virtual void UpdateHotkeyConflictStatus(AllHotkeyConflictsData allConflicts)
        {
            _hotkeyConflictStatus.Clear();
            _hotkeyConflictTooltips.Clear();

            var moduleRelatedConflicts = GetModuleRelatedConflicts(allConflicts);

            if (moduleRelatedConflicts.InAppConflicts.Count > 0)
            {
                foreach (var conflictGroup in moduleRelatedConflicts.InAppConflicts)
                {
                    foreach (var conflict in conflictGroup.Modules)
                    {
                        if (string.Equals(conflict.ModuleName, ModuleName, StringComparison.OrdinalIgnoreCase))
                        {
                            var keyName = $"{conflict.ModuleName.ToLowerInvariant()}_{conflict.HotkeyID}";
                            _hotkeyConflictStatus[keyName] = true;
                            _hotkeyConflictTooltips[keyName] = ResourceLoaderInstance.ResourceLoader.GetString("InAppHotkeyConflictTooltipText");
                        }
                    }
                }
            }

            if (moduleRelatedConflicts.SystemConflicts.Count > 0)
            {
                foreach (var conflictGroup in moduleRelatedConflicts.SystemConflicts)
                {
                    foreach (var conflict in conflictGroup.Modules)
                    {
                        if (string.Equals(conflict.ModuleName, ModuleName, StringComparison.OrdinalIgnoreCase))
                        {
                            var keyName = $"{conflict.ModuleName.ToLowerInvariant()}_{conflict.HotkeyID}";
                            _hotkeyConflictStatus[keyName] = true;
                            _hotkeyConflictTooltips[keyName] = ResourceLoaderInstance.ResourceLoader.GetString("SysHotkeyConflictTooltipText");
                        }
                    }
                }
            }
        }

        protected virtual bool GetHotkeyConflictStatus(string key)
        {
            return _hotkeyConflictStatus.ContainsKey(key) && _hotkeyConflictStatus[key];
        }

        protected virtual string GetHotkeyConflictTooltip(string key)
        {
            return _hotkeyConflictTooltips.TryGetValue(key, out string value) ? value : null;
        }

        private bool IsModuleInvolved(HotkeyConflictGroupData conflict)
        {
            if (conflict.Modules == null)
            {
                return false;
            }

            return conflict.Modules.Any(module =>
                string.Equals(module.ModuleName, ModuleName, StringComparison.OrdinalIgnoreCase));
        }

        public virtual void Dispose()
        {
            if (GlobalHotkeyConflictManager.Instance != null)
            {
                GlobalHotkeyConflictManager.Instance.ConflictsUpdated -= OnConflictsUpdated;
            }
        }
    }
}
