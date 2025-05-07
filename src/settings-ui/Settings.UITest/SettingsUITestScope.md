## General Settings

**Admin mode:**
 - [ ] restart PT and verify it runs as user
 - [ ] restart as admin and set "Always run as admin"
 - [ ] restart PT and verify it  runs as admin
 * if it's not on, turn on "Run at startup"
 - [ ] reboot the machine and verify PT runs as admin (it should not prompt the UAC dialog)
 * turn Always run as admin" off
 - [ ] reboot the machine and verify it now runs as user

**Modules on/off:**
 - [x] turn off all the modules and verify all module are off
 - [ ] restart PT and verify that all module are still off in the settings page and they are actually inactive
 - [x] turn on all the module, all module are now working
 - [ ] restart PT and verify that all module are still on in the settings page and they are actually working

**Quick access tray icon flyout:**
 - [x] Use left click on the system tray icon and verify the flyout appears. (It'll take a bit the first time)
 - [x] Try to launch a module from the launch screen in the flyout.
 - [x] Try disabling a module in the all apps screen in the flyout, make it a module that's launchable from the launch screen. Verify that the module is disabled and that it also disappeared from the launch screen in the flyout.
 - [x] Open the main settings screen on a module page. Verify that when you disable/enable the module on the flyout, that the Settings page is updated too.

**Settings backup/restore:**
 - [x] In the General tab, create a backup of the settings.
 - [x] Change some settings in some PowerToys.
 - [x] Restore the settings in the General tab and verify the Settings you've applied were reset.
