// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.PowerToys.UITest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Devices.PointOfService.Provider;

namespace Microsoft.Settings.UITests
{
    [TestClass]
    public class SettingsTests : UITestBase
    {
        private readonly string[] dashboardModuleList =
        {
            "Advanced Paste",
            "Always On Top",
            "Awake",
            "Color Picker",
            "Command Palette",
            "Environment Variables",
            "FancyZones",
            "File Locksmith",
            "Find My Mouse",
            "Hosts File Editor",
            "Image Resizer",
            "Keyboard Manager",
            "Mouse Highlighter",
            "Mouse Jump",
            "Mouse Pointer Crosshairs",
            "Mouse Without Borders",
            "New+",
            "Peek",
            "PowerRename",
            "PowerToys Run",
            "Quick Accent",
            "Registry Preview",
            "Screen Ruler",
            "Shortcut Guide",
            "Text Extractor",
            "Workspaces",
            "ZoomIt",

            // "Crop And Lock", // this module cannot be found, why?
        };

        public SettingsTests()
            : base(PowerToysModule.PowerToysSettings, size: WindowSize.Large)
        {
        }

        [TestMethod("PowerToys.Settings.ModulesOnAndOffTest")]
        [TestCategory("Settings Test #1")]
        public void TestAllmoduleOnAndOff()
        {
            DisableAllModules();

            EnableAllModules();
        }

        private void DisableAllModules()
        {
            Find<NavigationViewItem>("Dashboard").Click();

            foreach (var moduleName in dashboardModuleList)
            {
                var moduleButton = Find<Button>(moduleName);
                Assert.IsNotNull(moduleButton);
                var toggle = moduleButton.Find<ToggleSwitch>("Enable module");
                Assert.IsNotNull(toggle);
                if (toggle.IsOn)
                {
                    toggle.Click();
                }
            }
        }

        private void EnableAllModules()
        {
            Find<NavigationViewItem>("Dashboard").Click();

            foreach (var moduleName in dashboardModuleList)
            {
                var moduleButton = Find<Button>(moduleName);
                Assert.IsNotNull(moduleButton);
                var toggle = moduleButton.Find<ToggleSwitch>("Enable module");
                Assert.IsNotNull(toggle);
                if (!toggle.IsOn)
                {
                    toggle.Click();
                }

                Scroll(direction: "Down");
            }
        }
    }
}
