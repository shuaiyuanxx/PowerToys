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

namespace Microsoft.Settings.UITests
{
    [TestClass]
    public class SettingsTests : UITestBase
    {
        public SettingsTests()
        : base(PowerToysModule.Runner)
        {
        }

        [TestMethod("PowerToys.Settings.AdminRunModes")]
        [TestCategory("Settings Test #1")]
        public void TestAdminModes()
        {
            RestartPowerToysAsAdmin();
            /*
            // Test 1: Restart PT and verify it runs as user
            RestartPowerToysAsUser();
            VerifyRunningAsUser();

            // Test 2: Restart as admin and set "Always run as admin"
            RestartPowerToysAsAdmin();
            SetAlwaysRunAsAdmin(true);

            // Test 3: Restart PT and verify it runs as admin
            RestartPowerToys();
            VerifyRunningAsAdmin();

            // Test 4: Ensure "Run at startup" is enabled
            NavigateToGeneralSettings();
            EnsureRunAtStartupEnabled();

            // Note: Test 4 (reboot and verify) would need to be a separate test
            // because we can't automate a full system reboot in a unit test

            // Test 5: Turn "Always run as admin" off
            SetAlwaysRunAsAdmin(false);

            // Test 6: Restart and verify it runs as user
            RestartPowerToys();
            VerifyRunningAsUser();
            */
        }

        private void RestartPowerToysAsUser()
        {
            // Exit any running PowerToys
            this.ExitScopeExe();

            // Start PowerToys as normal user
            this.Session.Attach(PowerToysModule.Runner);
            Task.Delay(2000).Wait(); // Wait for PowerToys to start
        }

        private void RestartPowerToysAsAdmin()
        {
            // Exit any running PowerToys
            this.ExitScopeExe();

            string runnerPath = @"\..\..\..\PowerToys.exe";

            var locationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            runnerPath = locationPath + runnerPath;

            try
            {
                // Ensure path exists
                if (!File.Exists(runnerPath))
                {
                    Assert.Fail($"PowerToys runner executable not found at path: {runnerPath}");
                    return;
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = runnerPath,
                    UseShellExecute = true,
                    Verb = "runas", // This requests admin privileges
                };

                Process? process = Process.Start(processInfo);

                // Wait for PowerToys to start
                Task.Delay(5000).Wait();

                // Attach to the PowerToys Runner process
                this.Session.Attach(PowerToysModule.Runner);

                // Additional delay to ensure UI is fully loaded
                Task.Delay(2000).Wait();

                // Verify it's running as admin
                Assert.IsTrue(this.Session.IsElevated.GetValueOrDefault(false), "PowerToys should be started as admin but isn't running elevated.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed run PowerToys as admin: {ex.Message}");
            }

            Assert.IsTrue(this.Session.IsElevated.GetValueOrDefault(false), "PowerToys should be started as admin.");
        }

        private void RestartPowerToys()
        {
            this.RestartScopeExe();
            Task.Delay(2000).Wait(); // Wait for PowerToys to restart
        }

        private void NavigateToGeneralSettings()
        {
            this.Session.Attach(PowerToysModule.PowerToysSettings);

            // Navigate to General settings
            this.Find<NavigationViewItem>("General").Click();
            Task.Delay(500).Wait();
        }

        private void SetAlwaysRunAsAdmin(bool enable)
        {
            NavigateToGeneralSettings();

            // Find and toggle the "Always run as administrator" option
            var alwaysRunToggle = this.Find<ToggleSwitch>("Always run as administrator");
            alwaysRunToggle.Toggle(enable);

            Task.Delay(500).Wait();

            // Save settings if needed
            if (this.HasOne("Save"))
            {
                this.Find<Button>("Save").Click();
                Task.Delay(500).Wait();
            }
        }

        private void EnsureRunAtStartupEnabled()
        {
            var runAtStartupToggle = this.Find<ToggleSwitch>("Run at startup");
            runAtStartupToggle.Toggle(true);

            Task.Delay(500).Wait();

            // Save settings if needed
            if (this.HasOne("Save"))
            {
                this.Find<Button>("Save").Click();
                Task.Delay(500).Wait();
            }
        }

        private void VerifyRunningAsUser()
        {
            // Verify PowerToys is running as a regular user
            Assert.IsFalse(this.Session.IsElevated.GetValueOrDefault(), "PowerToys should be running as a normal user");
        }

        private void VerifyRunningAsAdmin()
        {
            // Verify PowerToys is running as administrator
            Assert.IsTrue(this.Session.IsElevated.GetValueOrDefault(), "PowerToys should be running as administrator");
        }
    }
}
