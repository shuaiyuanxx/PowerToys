// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace DemoAIModule.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            INotifyPropertyChanged viewModel = (INotifyPropertyChanged)this.DataContext;
            viewModel.PropertyChanged += (sender, args) =>
            {
                var colorTextBlock = (TextBlock)FindName("ColorTextBlock");

                var peer = UIElementAutomationPeer.CreatePeerForElement(colorTextBlock);

                peer.RaiseAutomationEvent(AutomationEvents.MenuOpened);
            };
        }

        public MainView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }
    }
}
