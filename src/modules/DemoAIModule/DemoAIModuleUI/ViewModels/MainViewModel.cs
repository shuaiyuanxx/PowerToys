// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows;
using System.Windows.Media;

using Common.UI;
using DemoAIModule.Common;
using DemoAIModule.Helpers;
using DemoAIModule.ViewModelContracts;
using ManagedCommon;
using PowerToys.Interop;

namespace DemoAIModule.ViewModels
{
    [Export(typeof(IMainViewModel))]
    public class MainViewModel : ViewModelBase, IMainViewModel
    {
        private readonly AppStateHandler _appStateHandler;

        [ImportingConstructor]
        public MainViewModel(
            AppStateHandler appStateHandler,
            CancellationToken exitToken)
        {
            _appStateHandler = appStateHandler;

            NativeEventWaiter.WaitForEventLoop(
                Constants.ShowDemoAIModuleSharedEvent(),
                _appStateHandler.StartUserSession,
                Application.Current.Dispatcher,
                exitToken);
        }

        public void RegisterWindowHandle(System.Windows.Interop.HwndSource hwndSource)
        {
            _appStateHandler.RegisterWindowHandle(hwndSource);
        }
    }
}
