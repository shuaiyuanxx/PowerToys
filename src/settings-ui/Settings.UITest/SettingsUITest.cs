// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.PowerToys.UITest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hosts.UITests
{
    [TestClass]
    public class SettingsUITest : UITestBase
    {
        public SettingsUITest()
            : base(PowerToysModule.PowerToysSettings)
        {
        }

        [TestMethod]
        public void TestEmptyView()
        {
            this.Find<NavigationViewItem>("General").Click();

            this.Find<Button>("Restart PowerToys as administrator").Click();

            // Cannot attach to restart as admin settings
            Task.Delay(10000).Wait();
            this.Session.Attach(PowerToysModule.PowerToysSettings);

            // this.ReattachAfterSelfRestart(PowerToysModule.PowerToysSettings);
            // this.Find<NavigationViewItem>("General").Click();
        }
    }
}
