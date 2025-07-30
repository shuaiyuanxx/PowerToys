// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Microsoft.PowerToys.Settings.UI.Controls;
using Microsoft.PowerToys.Settings.UI.Helpers;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.Library.Helpers;
using Microsoft.PowerToys.Settings.UI.Library.HotkeyConflicts;
using Microsoft.PowerToys.Settings.UI.Library.Interfaces;
using Microsoft.PowerToys.Settings.UI.Views;

namespace Microsoft.PowerToys.Settings.UI.ViewModels
{
    public class ShortcutConflictViewModel : PageViewModelBase, IDisposable
    {
        private readonly ISettingsUtils _settingsUtils;
        private readonly ISettingsRepository<GeneralSettings> _generalSettingsRepository;
        private readonly Dictionary<string, PageViewModelBase> _moduleViewModels = new();
        private readonly Dictionary<string, Func<PageViewModelBase>> _viewModelFactories = new();
        private readonly Dictionary<string, HotkeySettings> _originalSettings = new();

        private AllHotkeyConflictsData _conflictsData = new();
        private ObservableCollection<HotkeyConflictGroupData> _conflictItems = new();
        private PowerLauncherSettings powerLauncherSettings;

        private Dispatcher dispatcher;

        public ShortcutConflictViewModel(
            ISettingsUtils settingsUtils,
            ISettingsRepository<GeneralSettings> settingsRepository,
            Func<string, int> ipcMSGCallBackFunc)
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            _settingsUtils = settingsUtils ?? throw new ArgumentNullException(nameof(settingsUtils));
            _generalSettingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));

            SendConfigMSG = ipcMSGCallBackFunc;

            powerLauncherSettings = SettingsRepository<PowerLauncherSettings>.GetInstance(_settingsUtils)?.SettingsConfig;

            InitializeViewModelFactories();
        }

        public AllHotkeyConflictsData ConflictsData
        {
            get => _conflictsData;
            set
            {
                if (Set(ref _conflictsData, value))
                {
                    UpdateConflictItems();
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<HotkeyConflictGroupData> ConflictItems
        {
            get => _conflictItems;
            private set => Set(ref _conflictItems, value);
        }

        protected override string ModuleName => "ShortcutConflictsWindow";

        private Func<string, int> SendConfigMSG { get; }

        private void InitializeViewModelFactories()
        {
            try
            {
                _viewModelFactories["advancedpaste"] = () => new AdvancedPasteViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SettingsRepository<AdvancedPasteSettings>.GetInstance(_settingsUtils),
                    SendConfigMSG);

                _viewModelFactories["alwaysontop"] = () => new AlwaysOnTopViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SettingsRepository<AlwaysOnTopSettings>.GetInstance(_settingsUtils),
                    SendConfigMSG);

                _viewModelFactories["colorpicker"] = () => new ColorPickerViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SettingsRepository<ColorPickerSettings>.GetInstance(_settingsUtils),
                    SendConfigMSG);

                _viewModelFactories["cropandlock"] = () => new CropAndLockViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SettingsRepository<CropAndLockSettings>.GetInstance(_settingsUtils),
                    SendConfigMSG);

                _viewModelFactories["powerocr"] = () => new PowerOcrViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SettingsRepository<PowerOcrSettings>.GetInstance(_settingsUtils),
                    SendConfigMSG);

                _viewModelFactories["workspaces"] = () => new WorkspacesViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SettingsRepository<WorkspacesSettings>.GetInstance(_settingsUtils),
                    SendConfigMSG);

                _viewModelFactories["peek"] = () => new PeekViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SendConfigMSG,
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());

                _viewModelFactories["mouseutils"] = () => new MouseUtilsViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SettingsRepository<FindMyMouseSettings>.GetInstance(_settingsUtils),
                    SettingsRepository<MouseHighlighterSettings>.GetInstance(_settingsUtils),
                    SettingsRepository<MouseJumpSettings>.GetInstance(_settingsUtils),
                    SettingsRepository<MousePointerCrosshairsSettings>.GetInstance(_settingsUtils),
                    SendConfigMSG);

                // powertoys run
                _viewModelFactories["powertoys run"] = () => new PowerLauncherViewModel(
                    powerLauncherSettings,
                    SettingsRepository<GeneralSettings>.GetInstance(_settingsUtils),
                    ShellPage.SendDefaultIPCMessage,
                    App.IsDarkTheme);

                // measure tool
                _viewModelFactories["measure tool"] = () => new MeasureToolViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SettingsRepository<MeasureToolSettings>.GetInstance(_settingsUtils),
                    SendConfigMSG);

                // shortcut guide
                _viewModelFactories["shortcut guide"] = () => new ShortcutGuideViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SettingsRepository<ShortcutGuideSettings>.GetInstance(_settingsUtils),
                    SendConfigMSG);

                // textextractor
                _viewModelFactories["textextractor"] = () => new PowerOcrViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SettingsRepository<PowerOcrSettings>.GetInstance(_settingsUtils),
                    SendConfigMSG);

                // mousewithoutborders
                _viewModelFactories["mousewithoutborders"] = () => new MouseWithoutBordersViewModel(
                    _settingsUtils,
                    _generalSettingsRepository,
                    SendConfigMSG,
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing ViewModel factories: {ex.Message}");
            }
        }

        public string GetAdvancedPasteCustomActionName(int actionId)
        {
            try
            {
                var advancedPasteViewModel = GetOrCreateViewModel("advancedpaste") as AdvancedPasteViewModel;
                if (advancedPasteViewModel?.CustomActions != null)
                {
                    var customAction = advancedPasteViewModel.CustomActions.FirstOrDefault(ca => ca.Id == actionId);
                    return customAction?.Name;
                }
            }
            catch (Exception)
            {
                // If we can't get the custom action name, return null
            }

            return null;
        }

        private PageViewModelBase GetOrCreateViewModel(string moduleKey)
        {
            if (!_moduleViewModels.TryGetValue(moduleKey, out var viewModel))
            {
                if (_viewModelFactories.TryGetValue(moduleKey, out var factory))
                {
                    try
                    {
                        viewModel = factory();
                        _moduleViewModels[moduleKey] = viewModel;

                        System.Diagnostics.Debug.WriteLine($"Lazy-loaded ViewModel for module: {moduleKey}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating ViewModel for {moduleKey}: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No factory found for module: {moduleKey}");
                    return null;
                }
            }

            return viewModel;
        }

        protected override void OnConflictsUpdated(object sender, AllHotkeyConflictsEventArgs e)
        {
            dispatcher.BeginInvoke(() =>
            {
                ConflictsData = e.Conflicts ?? new AllHotkeyConflictsData();
            });
        }

        private void UpdateConflictItems()
        {
            var items = new ObservableCollection<HotkeyConflictGroupData>();
            _originalSettings.Clear();

            if (ConflictsData?.InAppConflicts != null)
            {
                foreach (var conflict in ConflictsData.InAppConflicts)
                {
                    ProcessConflictGroup(conflict, false);
                    items.Add(conflict);
                }
            }

            if (ConflictsData?.SystemConflicts != null)
            {
                foreach (var conflict in ConflictsData.SystemConflicts)
                {
                    ProcessConflictGroup(conflict, true);
                    items.Add(conflict);
                }
            }

            ConflictItems = items;
            OnPropertyChanged(nameof(ConflictItems));
        }

        private void ProcessConflictGroup(HotkeyConflictGroupData conflict, bool isSystemConflict)
        {
            foreach (var module in conflict.Modules)
            {
                module.PropertyChanged += OnModuleHotkeyDataPropertyChanged;

                module.HotkeySettings = GetHotkeySettingsFromViewModel(module.ModuleName, module.HotkeyID);

                module.Header = LocalizationHelper.GetLocalizedHotkeyHeader(module.ModuleName, module.HotkeyID);

                if (module.HotkeySettings != null)
                {
                    // Store original settings for rollback
                    var key = $"{module.ModuleName}_{module.HotkeyID}";
                    _originalSettings[key] = module.HotkeySettings with { };

                    // Set conflict properties
                    module.HotkeySettings.HasConflict = true;
                    module.HotkeySettings.IsSystemConflict = isSystemConflict;
                }

                module.IsSystemConflict = isSystemConflict;
            }
        }

        private void OnModuleHotkeyDataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ModuleHotkeyData moduleData && e.PropertyName == nameof(ModuleHotkeyData.HotkeySettings))
            {
                UpdateModuleViewModelHotkeySettings(moduleData.ModuleName, moduleData.HotkeyID, moduleData.HotkeySettings);
            }
        }

        private void UpdateModuleViewModelHotkeySettings(string moduleName, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            try
            {
                var moduleKey = GetModuleKey(moduleName);
                var viewModel = GetOrCreateViewModel(moduleKey);
                if (viewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to get or create ViewModel for {moduleName}");
                    return;
                }

                switch (moduleKey)
                {
                    case "advancedpaste":
                        UpdateAdvancedPasteHotkeySettings(viewModel as AdvancedPasteViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "alwaysontop":
                        UpdateAlwaysOnTopHotkeySettings(viewModel as AlwaysOnTopViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "colorpicker":
                        UpdateColorPickerHotkeySettings(viewModel as ColorPickerViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "cropandlock":
                        UpdateCropAndLockHotkeySettings(viewModel as CropAndLockViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "fancyzones":
                        UpdateFancyZonesHotkeySettings(viewModel as FancyZonesViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "measure tool":
                        UpdateMeasureToolHotkeySettings(viewModel as MeasureToolViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "shortcut guide":
                        UpdateShortcutGuideHotkeySettings(viewModel as ShortcutGuideViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "textextractor":
                        UpdatePowerOcrHotkeySettings(viewModel as PowerOcrViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "workspaces":
                        UpdateWorkspacesHotkeySettings(viewModel as WorkspacesViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "peek":
                        UpdatePeekHotkeySettings(viewModel as PeekViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "powertoys run":
                        UpdatePowerLauncherHotkeySettings(viewModel as PowerLauncherViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "cmdpal":
                        UpdateCmdPalHotkeySettings(viewModel as CmdPalViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "mousewithoutborders":
                        UpdateMouseWithoutBordersHotkeySettings(viewModel as MouseWithoutBordersViewModel, hotkeyID, newHotkeySettings);
                        break;
                    case "mouseutils":
                        UpdateMouseUtilsHotkeySettings(viewModel as MouseUtilsViewModel, moduleName, hotkeyID, newHotkeySettings);
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"Unknown module key: {moduleKey}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating hotkey settings for {moduleName}.{hotkeyID}: {ex.Message}");
            }
        }

        private void UpdateCmdPalHotkeySettings(CmdPalViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // CmdPal module only has one activation shortcut, and cannot be modified here
            /*if (!AreHotkeySettingsEqual(viewModel.Hotkey, newHotkeySettings))
            {
                viewModel.Hotkey = newHotkeySettings;
                System.Diagnostics.Debug.WriteLine($"Updated CmdPal Hotkey");
            }*/
        }

        private void UpdateMouseWithoutBordersHotkeySettings(MouseWithoutBordersViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            switch (hotkeyID)
            {
                case 0:
                    if (!AreHotkeySettingsEqual(viewModel.ToggleEasyMouseShortcut, newHotkeySettings))
                    {
                        viewModel.ToggleEasyMouseShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated MouseWithoutBorders ToggleEasyMouseShortcut");
                    }

                    break;

                case 1:
                    if (!AreHotkeySettingsEqual(viewModel.LockMachinesShortcut, newHotkeySettings))
                    {
                        viewModel.LockMachinesShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated MouseWithoutBorders LockMachinesShortcut");
                    }

                    break;

                case 2:
                    if (!AreHotkeySettingsEqual(viewModel.HotKeySwitch2AllPC, newHotkeySettings))
                    {
                        viewModel.HotKeySwitch2AllPC = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated MouseWithoutBorders HotKeySwitch2AllPC");
                    }

                    break;

                case 3:
                    if (!AreHotkeySettingsEqual(viewModel.ReconnectShortcut, newHotkeySettings))
                    {
                        viewModel.ReconnectShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated MouseWithoutBorders ReconnectShortcut");
                    }

                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown MouseWithoutBorders hotkey name: {hotkeyID}");
                    break;
            }
        }

        // Update methods for each module
        private void UpdateAdvancedPasteHotkeySettings(AdvancedPasteViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            switch (hotkeyID)
            {
                case 1:
                    if (!AreHotkeySettingsEqual(viewModel.AdvancedPasteUIShortcut, newHotkeySettings))
                    {
                        viewModel.AdvancedPasteUIShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated AdvancedPaste AdvancedPasteUIShortcut");
                    }

                    break;

                case 0:
                    if (!AreHotkeySettingsEqual(viewModel.PasteAsPlainTextShortcut, newHotkeySettings))
                    {
                        viewModel.PasteAsPlainTextShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated AdvancedPaste PasteAsPlainTextShortcut");
                    }

                    break;

                case 2:
                    if (!AreHotkeySettingsEqual(viewModel.PasteAsMarkdownShortcut, newHotkeySettings))
                    {
                        viewModel.PasteAsMarkdownShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated AdvancedPaste PasteAsMarkdownShortcut");
                    }

                    break;

                case 3:
                    if (!AreHotkeySettingsEqual(viewModel.PasteAsJsonShortcut, newHotkeySettings))
                    {
                        viewModel.PasteAsJsonShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated AdvancedPaste PasteAsJsonShortcut");
                    }

                    break;

                case 4:
                    if (viewModel.AdditionalActions?.ImageToText != null &&
                        !AreHotkeySettingsEqual(viewModel.AdditionalActions.ImageToText.Shortcut, newHotkeySettings))
                    {
                        viewModel.AdditionalActions.ImageToText.Shortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated AdvancedPaste ImageToText shortcut");
                    }

                    break;

                case 5:
                    if (viewModel.AdditionalActions?.PasteAsFile?.PasteAsTxtFile != null &&
                        !AreHotkeySettingsEqual(viewModel.AdditionalActions.PasteAsFile.PasteAsTxtFile.Shortcut, newHotkeySettings))
                    {
                        viewModel.AdditionalActions.PasteAsFile.PasteAsTxtFile.Shortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated AdvancedPaste PasteAsTxtFile shortcut");
                    }

                    break;

                case 6:
                    if (viewModel.AdditionalActions?.PasteAsFile?.PasteAsPngFile != null &&
                        !AreHotkeySettingsEqual(viewModel.AdditionalActions.PasteAsFile.PasteAsPngFile.Shortcut, newHotkeySettings))
                    {
                        viewModel.AdditionalActions.PasteAsFile.PasteAsPngFile.Shortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated AdvancedPaste PasteAsPngFile shortcut");
                    }

                    break;

                case 7:
                    if (viewModel.AdditionalActions?.PasteAsFile?.PasteAsHtmlFile != null &&
                        !AreHotkeySettingsEqual(viewModel.AdditionalActions.PasteAsFile.PasteAsHtmlFile.Shortcut, newHotkeySettings))
                    {
                        viewModel.AdditionalActions.PasteAsFile.PasteAsHtmlFile.Shortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated AdvancedPaste PasteAsHtmlFile shortcut");
                    }

                    break;

                case 8:
                    if (viewModel.AdditionalActions?.Transcode?.TranscodeToMp3 != null &&
                        !AreHotkeySettingsEqual(viewModel.AdditionalActions.Transcode.TranscodeToMp3.Shortcut, newHotkeySettings))
                    {
                        viewModel.AdditionalActions.Transcode.TranscodeToMp3.Shortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated AdvancedPaste TranscodeToMp3 shortcut");
                    }

                    break;

                case 9:
                    if (viewModel.AdditionalActions?.Transcode?.TranscodeToMp4 != null &&
                        !AreHotkeySettingsEqual(viewModel.AdditionalActions.Transcode.TranscodeToMp4.Shortcut, newHotkeySettings))
                    {
                        viewModel.AdditionalActions.Transcode.TranscodeToMp4.Shortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated AdvancedPaste TranscodeToMp4 shortcut");
                    }

                    break;

                case var _ when hotkeyID > 9:
                    var customActionId = hotkeyID - 10;
                    var customAction = viewModel.CustomActions?.FirstOrDefault(ca => ca.Id == customActionId);
                    if (customAction != null && !AreHotkeySettingsEqual(customAction.Shortcut, newHotkeySettings))
                    {
                        customAction.Shortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated AdvancedPaste CustomAction {customActionId} shortcut");
                    }

                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown AdvancedPaste hotkey name: {hotkeyID}");
                    break;
            }
        }

        private void UpdateAlwaysOnTopHotkeySettings(AlwaysOnTopViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // AlwaysOnTop module only has one hotkey setting
            if (!AreHotkeySettingsEqual(viewModel.Hotkey, newHotkeySettings))
            {
                viewModel.Hotkey = newHotkeySettings;
                System.Diagnostics.Debug.WriteLine($"Updated AlwaysOnTop hotkey settings");
            }
        }

        private void UpdateColorPickerHotkeySettings(ColorPickerViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // ColorPicker module only has one activation shortcut
            if (!AreHotkeySettingsEqual(viewModel.ActivationShortcut, newHotkeySettings))
            {
                viewModel.ActivationShortcut = newHotkeySettings;
                System.Diagnostics.Debug.WriteLine($"Updated ColorPicker hotkey settings");
            }
        }

        private void UpdateCropAndLockHotkeySettings(CropAndLockViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // Update based on hotkey name for CropAndLock module
            switch (hotkeyID)
            {
                case 1:
                    if (!AreHotkeySettingsEqual(viewModel.ThumbnailActivationShortcut, newHotkeySettings))
                    {
                        viewModel.ThumbnailActivationShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated CropAndLock ThumbnailActivationShortcut");
                    }

                    break;

                case 0:
                    if (!AreHotkeySettingsEqual(viewModel.ReparentActivationShortcut, newHotkeySettings))
                    {
                        viewModel.ReparentActivationShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated CropAndLock ReparentActivationShortcut");
                    }

                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown CropAndLock hotkey name: {hotkeyID}");
                    break;
            }
        }

        private void UpdateFancyZonesHotkeySettings(FancyZonesViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // FancyZones module only has one editor hotkey
            if (!AreHotkeySettingsEqual(viewModel.EditorHotkey, newHotkeySettings))
            {
                viewModel.EditorHotkey = newHotkeySettings;
                System.Diagnostics.Debug.WriteLine($"Updated FancyZones EditorHotkey");
            }
        }

        private void UpdateMeasureToolHotkeySettings(MeasureToolViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // MeasureTool module only has one activation shortcut
            if (!AreHotkeySettingsEqual(viewModel.ActivationShortcut, newHotkeySettings))
            {
                viewModel.ActivationShortcut = newHotkeySettings;
                System.Diagnostics.Debug.WriteLine($"Updated MeasureTool ActivationShortcut");
            }
        }

        private void UpdateShortcutGuideHotkeySettings(ShortcutGuideViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // ShortcutGuide module only has one shortcut to open the guide
            if (!AreHotkeySettingsEqual(viewModel.OpenShortcutGuide, newHotkeySettings))
            {
                viewModel.OpenShortcutGuide = newHotkeySettings;
                System.Diagnostics.Debug.WriteLine($"Updated ShortcutGuide OpenShortcutGuide");
            }
        }

        private void UpdatePowerOcrHotkeySettings(PowerOcrViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // PowerOCR module only has one activation shortcut
            if (!AreHotkeySettingsEqual(viewModel.ActivationShortcut, newHotkeySettings))
            {
                viewModel.ActivationShortcut = newHotkeySettings;
                System.Diagnostics.Debug.WriteLine($"Updated PowerOCR ActivationShortcut");
            }
        }

        private void UpdateWorkspacesHotkeySettings(WorkspacesViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // Workspaces module only has one hotkey
            if (!AreHotkeySettingsEqual(viewModel.Hotkey, newHotkeySettings))
            {
                viewModel.Hotkey = newHotkeySettings;
                System.Diagnostics.Debug.WriteLine($"Updated Workspaces Hotkey");
            }
        }

        private void UpdatePeekHotkeySettings(PeekViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // Peek module only has one activation shortcut
            if (!AreHotkeySettingsEqual(viewModel.ActivationShortcut, newHotkeySettings))
            {
                viewModel.ActivationShortcut = newHotkeySettings;
                System.Diagnostics.Debug.WriteLine($"Updated Peek ActivationShortcut");
            }
        }

        private void UpdatePowerLauncherHotkeySettings(PowerLauncherViewModel viewModel, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // PowerLauncher module only has one shortcut to open the launcher
            if (!AreHotkeySettingsEqual(viewModel.OpenPowerLauncher, newHotkeySettings))
            {
                viewModel.OpenPowerLauncher = newHotkeySettings;
                System.Diagnostics.Debug.WriteLine($"Updated PowerLauncher OpenPowerLauncher");
            }
        }

        private void UpdateMouseUtilsHotkeySettings(MouseUtilsViewModel viewModel, string moduleName, int hotkeyID, HotkeySettings newHotkeySettings)
        {
            if (viewModel == null)
            {
                return;
            }

            // Update based on specific mouse utility module name
            switch (moduleName?.ToLowerInvariant())
            {
                case "mousehighlighter":
                    if (!AreHotkeySettingsEqual(viewModel.MouseHighlighterActivationShortcut, newHotkeySettings))
                    {
                        viewModel.MouseHighlighterActivationShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated MouseUtils MouseHighlighterActivationShortcut");
                    }

                    break;

                case "mousejump":
                    if (!AreHotkeySettingsEqual(viewModel.MouseJumpActivationShortcut, newHotkeySettings))
                    {
                        viewModel.MouseJumpActivationShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated MouseUtils MouseJumpActivationShortcut");
                    }

                    break;

                case "mousepointercrosshairs":
                    if (!AreHotkeySettingsEqual(viewModel.MousePointerCrosshairsActivationShortcut, newHotkeySettings))
                    {
                        viewModel.MousePointerCrosshairsActivationShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated MouseUtils MousePointerCrosshairsActivationShortcut");
                    }

                    break;

                case "findmymouse":
                    if (!AreHotkeySettingsEqual(viewModel.FindMyMouseActivationShortcut, newHotkeySettings))
                    {
                        viewModel.FindMyMouseActivationShortcut = newHotkeySettings;
                        System.Diagnostics.Debug.WriteLine($"Updated MouseUtils FindMyMouseActivationShortcut");
                    }

                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown MouseUtils module name: {moduleName}");
                    break;
            }
        }

        // Helper methods
        private bool AreHotkeySettingsEqual(HotkeySettings settings1, HotkeySettings settings2)
        {
            if (settings1 == null && settings2 == null)
            {
                return true;
            }

            if (settings1 == null || settings2 == null)
            {
                return false;
            }

            return settings1.Win == settings2.Win &&
                   settings1.Ctrl == settings2.Ctrl &&
                   settings1.Alt == settings2.Alt &&
                   settings1.Shift == settings2.Shift &&
                   settings1.Code == settings2.Code;
        }

        private HotkeySettings GetHotkeySettingsFromViewModel(string moduleName, int hotkeyID)
        {
            try
            {
                var moduleKey = GetModuleKey(moduleName);
                var viewModel = GetOrCreateViewModel(moduleKey);
                if (viewModel == null)
                {
                    return null;
                }

                return moduleKey switch
                {
                    "advancedpaste" => GetAdvancedPasteHotkeySettings(viewModel as AdvancedPasteViewModel, hotkeyID),
                    "alwaysontop" => GetAlwaysOnTopHotkeySettings(viewModel as AlwaysOnTopViewModel, hotkeyID),
                    "colorpicker" => GetColorPickerHotkeySettings(viewModel as ColorPickerViewModel, hotkeyID),
                    "cropandlock" => GetCropAndLockHotkeySettings(viewModel as CropAndLockViewModel, hotkeyID),
                    "fancyzones" => GetFancyZonesHotkeySettings(viewModel as FancyZonesViewModel, hotkeyID),
                    "measure tool" => GetMeasureToolHotkeySettings(viewModel as MeasureToolViewModel, hotkeyID),
                    "shortcut guide" => GetShortcutGuideHotkeySettings(viewModel as ShortcutGuideViewModel, hotkeyID),
                    "powerocr" or "textextractor" => GetPowerOcrHotkeySettings(viewModel as PowerOcrViewModel, hotkeyID),
                    "workspaces" => GetWorkspacesHotkeySettings(viewModel as WorkspacesViewModel, hotkeyID),
                    "peek" => GetPeekHotkeySettings(viewModel as PeekViewModel, hotkeyID),
                    "powertoys run" => GetPowerLauncherHotkeySettings(viewModel as PowerLauncherViewModel, hotkeyID),
                    "cmdpal" => GetCmdPalHotkeySettings(viewModel as CmdPalViewModel, hotkeyID),
                    "mousewithoutborders" => GetMouseWithoutBordersHotkeySettings(viewModel as MouseWithoutBordersViewModel, hotkeyID),
                    "mouseutils" => GetMouseUtilsHotkeySettings(viewModel as MouseUtilsViewModel, moduleName, hotkeyID),
                    _ => null,
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting hotkey settings for {moduleName}.{hotkeyID}: {ex.Message}");
                return null;
            }
        }

        private HotkeySettings GetMouseWithoutBordersHotkeySettings(MouseWithoutBordersViewModel viewModel, int hotkeyID)
        {
            if (viewModel == null)
            {
                return null;
            }

            return hotkeyID switch
            {
                0 => viewModel.ToggleEasyMouseShortcut,
                1 => viewModel.LockMachinesShortcut,
                2 => viewModel.HotKeySwitch2AllPC,
                3 => viewModel.ReconnectShortcut,
                _ => null,
            };
        }

        private string GetModuleKey(string moduleName)
        {
            return moduleName?.ToLowerInvariant() switch
            {
                "mousehighlighter" or "mousejump" or "mousepointercrosshairs" or "findmymouse" => "mouseutils",
                _ => moduleName?.ToLowerInvariant(),
            };
        }

        // Get methods that return direct references to ViewModel properties for two-way binding
        private HotkeySettings GetAdvancedPasteHotkeySettings(AdvancedPasteViewModel viewModel, int hotkeyID)
        {
            if (viewModel == null)
            {
                return null;
            }

            return hotkeyID switch
            {
                0 => viewModel.PasteAsPlainTextShortcut,
                1 => viewModel.AdvancedPasteUIShortcut,
                2 => viewModel.PasteAsMarkdownShortcut,
                3 => viewModel.PasteAsJsonShortcut,
                4 => viewModel.AdditionalActions?.ImageToText?.Shortcut,
                5 => viewModel.AdditionalActions?.PasteAsFile?.PasteAsTxtFile?.Shortcut,
                6 => viewModel.AdditionalActions?.PasteAsFile?.PasteAsPngFile?.Shortcut,
                7 => viewModel.AdditionalActions?.PasteAsFile?.PasteAsHtmlFile?.Shortcut,
                8 => viewModel.AdditionalActions?.Transcode?.TranscodeToMp3?.Shortcut,
                9 => viewModel.AdditionalActions?.Transcode?.TranscodeToMp4?.Shortcut,
                _ when hotkeyID > 9 => GetCustomActionShortcut(viewModel, hotkeyID - 10),
                _ => null,
            };
        }

        private HotkeySettings GetCustomActionShortcut(AdvancedPasteViewModel viewModel, int customActionID)
        {
            if (viewModel?.CustomActions == null)
            {
                return null;
            }

            var customAction = viewModel.CustomActions.FirstOrDefault(ca => ca.Id == customActionID);
            return customAction?.Shortcut;
        }

        private HotkeySettings GetAlwaysOnTopHotkeySettings(AlwaysOnTopViewModel viewModel, int hotkeyID)
        {
            return viewModel?.Hotkey;
        }

        private HotkeySettings GetColorPickerHotkeySettings(ColorPickerViewModel viewModel, int hotkeyID)
        {
            return viewModel?.ActivationShortcut;
        }

        private HotkeySettings GetCropAndLockHotkeySettings(CropAndLockViewModel viewModel, int hotkeyID)
        {
            if (viewModel == null)
            {
                return null;
            }

            return hotkeyID switch
            {
                0 => viewModel.ReparentActivationShortcut,
                1 => viewModel.ThumbnailActivationShortcut,
                _ => null,
            };
        }

        private HotkeySettings GetFancyZonesHotkeySettings(FancyZonesViewModel viewModel, int hotkeyID)
        {
            return viewModel?.EditorHotkey;
        }

        private HotkeySettings GetMeasureToolHotkeySettings(MeasureToolViewModel viewModel, int hotkeyID)
        {
            return viewModel?.ActivationShortcut;
        }

        private HotkeySettings GetShortcutGuideHotkeySettings(ShortcutGuideViewModel viewModel, int hotkeyID)
        {
            return viewModel?.OpenShortcutGuide;
        }

        private HotkeySettings GetPowerOcrHotkeySettings(PowerOcrViewModel viewModel, int hotkeyID)
        {
            return viewModel?.ActivationShortcut;
        }

        private HotkeySettings GetWorkspacesHotkeySettings(WorkspacesViewModel viewModel, int hotkeyID)
        {
            return viewModel?.Hotkey;
        }

        private HotkeySettings GetPeekHotkeySettings(PeekViewModel viewModel, int hotkeyID)
        {
            return viewModel?.ActivationShortcut;
        }

        private HotkeySettings GetPowerLauncherHotkeySettings(PowerLauncherViewModel viewModel, int hotkeyID)
        {
            return viewModel?.OpenPowerLauncher;
        }

        private HotkeySettings GetMouseUtilsHotkeySettings(MouseUtilsViewModel viewModel, string moduleName, int hotkeyID)
        {
            if (viewModel == null)
            {
                return null;
            }

            return moduleName?.ToLowerInvariant() switch
            {
                "mousehighlighter" => viewModel.MouseHighlighterActivationShortcut,
                "mousejump" => viewModel.MouseJumpActivationShortcut,
                "mousepointercrosshairs" => viewModel.MousePointerCrosshairsActivationShortcut,
                "findmymouse" => viewModel.FindMyMouseActivationShortcut,
                _ => null,
            };
        }

        private HotkeySettings GetCmdPalHotkeySettings(CmdPalViewModel viewModel, int hotkeyID)
        {
            return viewModel?.Hotkey;
        }

        public override void Dispose()
        {
            // Unsubscribe from property change events
            foreach (var conflictGroup in ConflictItems)
            {
                foreach (var module in conflictGroup.Modules)
                {
                    module.PropertyChanged -= OnModuleHotkeyDataPropertyChanged;
                }
            }

            // Dispose all created module ViewModels
            foreach (var viewModel in _moduleViewModels.Values)
            {
                viewModel?.Dispose();
            }

            base.Dispose();
        }
    }
}
