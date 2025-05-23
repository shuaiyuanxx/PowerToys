// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Helpers;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.PowerToys.Settings.UI.Views
{
    public sealed partial class CmdPalPage : Page, IRefreshablePage
    {
        private CmdPalViewModel ViewModel { get; set; }

        public CmdPalPage()
        {
            var settingsUtils = new SettingsUtils();
            ViewModel = new CmdPalViewModel(
                settingsUtils,
                SettingsRepository<GeneralSettings>.GetInstance(settingsUtils),
                ShellPage.SendDefaultIPCMessage,
                DispatcherQueue);
            DataContext = ViewModel;
            InitializeComponent();
        }

        public void RefreshEnabledState()
        {
            ViewModel.RefreshEnabledState();
        }

        private async void CmdPalSettingsDeeplink_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                // Launch CmdPal settings using proper URI handling
                await Windows.System.Launcher.LaunchUriAsync(new Uri("x-cmdpal://settings"));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to launch CmdPal settings: {ex.Message}");
            }
        }
    }
}
