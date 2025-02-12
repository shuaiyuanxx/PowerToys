// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

using Common.UI;
using DemoAIModule.ViewModelContracts;
using Microsoft.PowerToys.Settings.UI.Library.Enumerations;

namespace DemoAIModule.Helpers
{
    [Export(typeof(AppStateHandler))]
    public class AppStateHandler
    {
        private DemoWindow _demoWindow;

        private bool _demoAIModuleShown;
        private Lock _demoAIModuleVisibilityLock = new Lock();

        private HwndSource _hwndSource;
        private const int _globalHotKeyId = 0x0001;

        // Blocks using the escape key to close the color picker editor when the adjust color flyout is open.
        public static bool BlockEscapeKeyClosingDemoAIModuleEditor { get; set; }

        [ImportingConstructor]
        public AppStateHandler()
        {
            Application.Current.MainWindow.Closed += MainWindow_Closed;
        }

        // public event EventHandler AppShown;
        // public event EventHandler AppHidden;
        public event EventHandler AppClosed;

        public event EventHandler EnterPressed;

        public event EventHandler UserSessionStarted;

        public event EventHandler UserSessionEnded;

        public void StartUserSession()
        {
            EndUserSession(); // Ends current user session if there's an active one.
            lock (_demoAIModuleVisibilityLock)
            {
                ShowDemoAIModule();

                if (!(System.Windows.Application.Current as DemoAIModuleUI.App).IsRunningDetachedFromPowerToys())
                {
                    UserSessionStarted?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool EndUserSession()
        {
            lock (_demoAIModuleVisibilityLock)
            {
                if (_demoAIModuleShown)
                {
                    HideDemoAIModule();

                    if (!(System.Windows.Application.Current as DemoAIModuleUI.App).IsRunningDetachedFromPowerToys())
                    {
                        UserSessionEnded?.Invoke(this, EventArgs.Empty);
                    }

                    return true;
                }

                return false;
            }
        }

        public static void SetTopMost()
        {
            Application.Current.MainWindow.Topmost = false;
            Application.Current.MainWindow.Topmost = true;
        }

        private void ShowDemoAIModule()
        {
            if (!_demoAIModuleShown)
            {
                if (_demoWindow == null)
                {
                    _demoWindow = new DemoWindow(this);
                }

                _demoWindow.Show();
                _demoAIModuleShown = true;
            }
        }

        private void HideDemoAIModule()
        {
            if (_demoAIModuleShown)
            {
                _demoWindow.Hide();
            }
        }

        public bool IsDemoAIModuleVisible()
        {
            return _demoAIModuleShown;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            AppClosed?.Invoke(this, EventArgs.Empty);
        }

        internal void RegisterWindowHandle(System.Windows.Interop.HwndSource hwndSource)
        {
            _hwndSource = hwndSource;
        }

        public bool HandleEnterPressed()
        {
            if (!IsDemoAIModuleVisible())
            {
                return false;
            }

            EnterPressed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool HandleEscPressed()
        {
            if (!BlockEscapeKeyClosingDemoAIModuleEditor)
            {
                return EndUserSession();
            }
            else
            {
                return false;
            }
        }
    }
}
