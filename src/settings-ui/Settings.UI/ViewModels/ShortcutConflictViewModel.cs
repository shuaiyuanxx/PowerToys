// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Windows;
using System.Windows.Threading;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Helpers;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.Library.Helpers;
using Microsoft.PowerToys.Settings.UI.Library.HotkeyConflicts;
using Microsoft.PowerToys.Settings.UI.Library.Interfaces;
using Microsoft.PowerToys.Settings.UI.SerializationContext;
using Microsoft.PowerToys.Settings.UI.Services;
using Microsoft.Windows.ApplicationModel.Resources;

namespace Microsoft.PowerToys.Settings.UI.ViewModels
{
    public class ShortcutConflictViewModel : PageViewModelBase, IDisposable
    {
        private readonly SettingsFactory _settingsFactory;
        private readonly Func<string, int> _ipcMSGCallBackFunc;
        private readonly Dispatcher _dispatcher;

        private AllHotkeyConflictsData _conflictsData = new();
        private ObservableCollection<HotkeyConflictGroupData> _conflictItems = new();
        private ResourceLoader resourceLoader;

        public ShortcutConflictViewModel(
            ISettingsUtils settingsUtils,
            ISettingsRepository<GeneralSettings> settingsRepository,
            Func<string, int> ipcMSGCallBackFunc)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _ipcMSGCallBackFunc = ipcMSGCallBackFunc ?? throw new ArgumentNullException(nameof(ipcMSGCallBackFunc));
            resourceLoader = ResourceLoaderInstance.ResourceLoader;

            // Create SettingsFactory
            _settingsFactory = new SettingsFactory(settingsUtils ?? throw new ArgumentNullException(nameof(settingsUtils)));
        }

        public AllHotkeyConflictsData ConflictsData
        {
            get => _conflictsData;
            set
            {
                if (Set(ref _conflictsData, value))
                {
                    UpdateConflictItems();
                }
            }
        }

        public ObservableCollection<HotkeyConflictGroupData> ConflictItems
        {
            get => _conflictItems;
            private set => Set(ref _conflictItems, value);
        }

        protected override string ModuleName => "ShortcutConflictsWindow";

        public string GetAdvancedPasteCustomActionName(int actionId)
        {
            try
            {
                var advancedPasteSettings = GetFreshSettings(ModuleNames.AdvancedPaste) as AdvancedPasteSettings;
                return advancedPasteSettings?.Properties?.CustomActions?.Value?.FirstOrDefault(ca => ca.Id == actionId)?.Name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Always gets fresh settings from file, no caching
        /// </summary>
        private IHotkeyConfig GetFreshSettings(string moduleKey)
        {
            try
            {
                var settings = _settingsFactory.GetSettings(moduleKey);
                if (settings != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Loaded fresh settings for module: {moduleKey}");
                }

                return settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading fresh settings for {moduleKey}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Always gets fresh hotkey accessors from file, no caching
        /// </summary>
        private HotkeyAccessor[] GetFreshHotkeyAccessors(string moduleName)
        {
            try
            {
                var settings = GetFreshSettings(moduleName);
                if (settings != null)
                {
                    var allAccessors = settings.GetAllHotkeyAccessors();
                    if (allAccessors.TryGetValue(moduleName, out var accessors))
                    {
                        System.Diagnostics.Debug.WriteLine($"Loaded fresh {accessors.Length} hotkey accessors for module: {moduleName}");
                        return accessors;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading fresh hotkey accessors for {moduleName}: {ex.Message}");
            }

            return null;
        }

        private HotkeyAccessor GetHotkeyAccessor(string moduleName, int hotkeyID)
        {
            var accessors = GetFreshHotkeyAccessors(moduleName);
            if (accessors != null && hotkeyID >= 0 && hotkeyID < accessors.Length)
            {
                return accessors[hotkeyID];
            }

            return null;
        }

        protected override void OnConflictsUpdated(object sender, AllHotkeyConflictsEventArgs e)
        {
            _dispatcher.BeginInvoke(() =>
            {
                ConflictsData = e.Conflicts ?? new AllHotkeyConflictsData();
            });
        }

        private void UpdateConflictItems()
        {
            var items = new ObservableCollection<HotkeyConflictGroupData>();

            ProcessConflicts(ConflictsData?.InAppConflicts, false, items);
            ProcessConflicts(ConflictsData?.SystemConflicts, true, items);

            ConflictItems = items;
            OnPropertyChanged(nameof(ConflictItems));
        }

        private void ProcessConflicts(IEnumerable<HotkeyConflictGroupData> conflicts, bool isSystemConflict, ObservableCollection<HotkeyConflictGroupData> items)
        {
            if (conflicts == null)
            {
                return;
            }

            foreach (var conflict in conflicts)
            {
                ProcessConflictGroup(conflict, isSystemConflict);
                items.Add(conflict);
            }
        }

        private void ProcessConflictGroup(HotkeyConflictGroupData conflict, bool isSystemConflict)
        {
            foreach (var module in conflict.Modules)
            {
                SetupModuleData(module, isSystemConflict);
            }
        }

        private void SetupModuleData(ModuleHotkeyData module, bool isSystemConflict)
        {
            try
            {
                // Always get fresh hotkey accessor from file
                var hotkeyAccessor = GetHotkeyAccessor(module.ModuleName, module.HotkeyID);

                if (hotkeyAccessor != null)
                {
                    // Get current hotkey settings (fresh from file) using the accessor's getter
                    module.HotkeySettings = hotkeyAccessor.Getter();

                    // Set header using localization key
                    module.Header = GetHotkeyLocalizationHeader(module.ModuleName, module.HotkeyID, hotkeyAccessor.LocalizationHeaderKey);
                    module.IsSystemConflict = isSystemConflict;

                    // Set module display info
                    var moduleType = ModuleNames.ToModuleType(module.ModuleName);
                    if (moduleType.HasValue)
                    {
                        var displayName = resourceLoader.GetString(ModuleHelper.GetModuleLabelResourceName(moduleType.Value));
                        module.DisplayName = displayName;
                        module.IconPath = ModuleHelper.GetModuleTypeFluentIconName(moduleType.Value);
                    }

                    if (module.HotkeySettings != null)
                    {
                        SetConflictProperties(module.HotkeySettings, isSystemConflict);
                    }

                    module.PropertyChanged += OnModuleHotkeyDataPropertyChanged;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Could not find hotkey accessor for {module.ModuleName}.{module.HotkeyID}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up module data for {module.ModuleName}: {ex.Message}");
            }
        }

        private void SetConflictProperties(HotkeySettings settings, bool isSystemConflict)
        {
            settings.HasConflict = true;
            settings.IsSystemConflict = isSystemConflict;
        }

        private void OnModuleHotkeyDataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ModuleHotkeyData moduleData && e.PropertyName == nameof(ModuleHotkeyData.HotkeySettings))
            {
                UpdateModuleHotkeySettings(moduleData.ModuleName, moduleData.HotkeyID, moduleData.HotkeySettings);
            }
        }

        private void UpdateModuleHotkeySettings(string moduleName, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            try
            {
                // Get fresh hotkey accessor to ensure we're working with current data
                var hotkeyAccessor = GetHotkeyAccessor(moduleName, hotkeyID);
                if (hotkeyAccessor != null)
                {
                    // Use the accessor's setter to update the hotkey settings
                    hotkeyAccessor.Setter(newHotkeySettings);
                    System.Diagnostics.Debug.WriteLine($"Updated {moduleName} hotkey {hotkeyID} using accessor setter");

                    // Save the settings and send IPC notification
                    SaveModuleSettingsAndNotify(moduleName);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Could not find hotkey accessor for updating {moduleName}.{hotkeyID}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating hotkey settings for {moduleName}.{hotkeyID}: {ex.Message}");
            }
        }

        private void SaveModuleSettingsAndNotify(string moduleName)
        {
            try
            {
                var moduleKey = GetModuleKey(moduleName);
                var settings = GetFreshSettings(moduleKey);

                if (settings is ISettingsConfig settingsConfig)
                {
                    // Save settings to file using the repository
                    // SaveSettingsToFile(settings);

                    // Send IPC notification using the same format as other ViewModels
                    SendConfigMSG(settingsConfig, moduleKey);

                    System.Diagnostics.Debug.WriteLine($"Saved settings and sent IPC notification for module: {moduleName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings and notifying for {moduleName}: {ex.Message}");
            }
        }

        private void SaveSettingsToFile(IHotkeyConfig settings)
        {
            try
            {
                // Get the repository for this settings type using reflection
                var settingsType = settings.GetType();
                var repositoryMethod = typeof(SettingsFactory).GetMethod("GetRepository");
                if (repositoryMethod != null)
                {
                    var genericMethod = repositoryMethod.MakeGenericMethod(settingsType);
                    var repository = genericMethod.Invoke(_settingsFactory, null);

                    if (repository != null)
                    {
                        var saveMethod = repository.GetType().GetMethod("SaveSettingsToFile");
                        saveMethod?.Invoke(repository, null);
                        System.Diagnostics.Debug.WriteLine($"Saved settings to file for type: {settingsType.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings to file: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends IPC notification using the same format as other ViewModels
        /// </summary>
        private void SendConfigMSG(ISettingsConfig settingsConfig, string moduleKey)
        {
            try
            {
                var jsonTypeInfo = GetJsonTypeInfo(settingsConfig.GetType());
                var serializedSettings = jsonTypeInfo != null
                    ? JsonSerializer.Serialize(settingsConfig, jsonTypeInfo)
                    : JsonSerializer.Serialize(settingsConfig);

                var ipcMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "{{ \"powertoys\": {{ \"{0}\": {1} }} }}",
                    moduleKey,
                    serializedSettings);

                var result = _ipcMSGCallBackFunc(ipcMessage);
                System.Diagnostics.Debug.WriteLine($"Sent IPC notification for {moduleKey}, result: {result}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending IPC notification for {moduleKey}: {ex.Message}");
            }
        }

        private JsonTypeInfo GetJsonTypeInfo(Type settingsType)
        {
            try
            {
                var contextType = typeof(SourceGenerationContextContext);
                var defaultProperty = contextType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
                var defaultContext = defaultProperty?.GetValue(null) as JsonSerializerContext;

                if (defaultContext != null)
                {
                    var typeInfoProperty = contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                           p.PropertyType.GetGenericTypeDefinition() == typeof(JsonTypeInfo<>) &&
                                           p.PropertyType.GetGenericArguments()[0] == settingsType);

                    return typeInfoProperty?.GetValue(defaultContext) as JsonTypeInfo;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting JsonTypeInfo for {settingsType.Name}: {ex.Message}");
            }

            return null;
        }

        private string GetHotkeyLocalizationHeader(string moduleName, int hotkeyID, string headerKey)
        {
            // Handle AdvancedPaste custom actions
            if (string.Equals(moduleName, ModuleNames.AdvancedPaste, StringComparison.OrdinalIgnoreCase)
                && hotkeyID > 9)
            {
                return headerKey;
            }

            try
            {
                return resourceLoader.GetString($"{headerKey}/Header");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting hotkey header for {moduleName}.{hotkeyID}: {ex.Message}");
                return headerKey; // Return the key itself as fallback
            }
        }

        private static string GetModuleKey(string moduleName)
        {
            return moduleName?.ToLowerInvariant() switch
            {
                ModuleNames.MouseHighlighter or ModuleNames.MouseJump or
                ModuleNames.MousePointerCrosshairs or ModuleNames.FindMyMouse => ModuleNames.MouseUtils,
                _ => moduleName,
            };
        }

        public override void Dispose()
        {
            UnsubscribeFromEvents();
            base.Dispose();
        }

        private void UnsubscribeFromEvents()
        {
            foreach (var conflictGroup in ConflictItems)
            {
                foreach (var module in conflictGroup.Modules)
                {
                    module.PropertyChanged -= OnModuleHotkeyDataPropertyChanged;
                }
            }
        }
    }
}
