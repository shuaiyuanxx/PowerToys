// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.PowerToys.Settings.UI.Library
{
    /// <summary>
    /// Wrapper for the HotkeyConflictManager C++ DLL
    /// </summary>
    public class HotkeyConflictManagerWrapper
    {
        /// <summary>
        /// Structure to match the C++ Hotkey struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Hotkey
        {
            [MarshalAs(UnmanagedType.Bool)]
            public bool Win;

            [MarshalAs(UnmanagedType.Bool)]
            public bool Ctrl;

            [MarshalAs(UnmanagedType.Bool)]
            public bool Shift;

            [MarshalAs(UnmanagedType.Bool)]
            public bool Alt;

            public byte Key;

            /// <summary>
            /// Converts a HotkeySettings to a native Hotkey
            /// </summary>
            /// <param name="settings">The source HotkeySettings</param>
            /// <returns>A Hotkey structure compatible with the native DLL</returns>
            public static Hotkey FromHotkeySettings(HotkeySettings settings)
            {
                return new Hotkey
                {
                    Win = settings.Win,
                    Ctrl = settings.Ctrl,
                    Shift = settings.Shift,
                    Alt = settings.Alt,
                    Key = (byte)settings.Code,
                };
            }
        }

        // Corrected function name - it should be GetInstance, not HotkeyConflictManager_GetInstance
        [DllImport("HotkeyConflictManager.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?GetInstance@HotkeyConflictManager@HotkeyConflict@@SAAAV12@XZ")]
        private static extern IntPtr GetInstance();

        // Corrected function name with proper C++ name mangling
        [DllImport("HotkeyConflictManager.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?HasConflict@HotkeyConflictManager@HotkeyConflict@@QEAA_NAEBUHotkey@2@PEB_W1@Z")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool HasConflict(
            IntPtr instance,
            ref Hotkey hotkey,
            [MarshalAs(UnmanagedType.LPWStr)] string moduleName,
            [MarshalAs(UnmanagedType.LPWStr)] string hotkeyName);

        // Corrected function name with proper C++ name mangling
        [DllImport("HotkeyConflictManager.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?AddHotkey@HotkeyConflictManager@HotkeyConflict@@QEAA_NAEBUHotkey@2@PEB_W1@Z")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddHotkey(
            IntPtr instance,
            ref Hotkey hotkey,
            [MarshalAs(UnmanagedType.LPWStr)] string moduleName,
            [MarshalAs(UnmanagedType.LPWStr)] string hotkeyName);

        // Corrected function name with proper C++ name mangling
        [DllImport("HotkeyConflictManager.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?RemoveHotkey@HotkeyConflictManager@HotkeyConflict@@QEAA_NAEBUHotkey@2@@Z")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveHotkey(IntPtr instance, ref Hotkey hotkey);

        // Cache for the instance pointer
        private static IntPtr _instance = IntPtr.Zero;

        /// <summary>
        /// Gets the instance pointer to the HotkeyConflictManager
        /// </summary>
        private static IntPtr Instance
        {
            get
            {
                if (_instance == IntPtr.Zero)
                {
                    _instance = GetInstance();
                    if (_instance == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Failed to get HotkeyConflictManager instance");
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Checks if a hotkey settings has any conflicts
        /// </summary>
        /// <param name="hotkeySettings">The hotkey settings to check</param>
        /// <param name="moduleName">The name of the module owning the hotkey</param>
        /// <param name="hotkeyName">The name of the hotkey</param>
        /// <returns>True if there's a conflict, false otherwise</returns>
        public static bool HasConflict(HotkeySettings hotkeySettings, string moduleName, string hotkeyName)
        {
            if (hotkeySettings == null || !hotkeySettings.IsValid())
            {
                return false;
            }

            var hotkey = Hotkey.FromHotkeySettings(hotkeySettings);
            return HasConflict(Instance, ref hotkey, moduleName, hotkeyName);
        }

        /// <summary>
        /// Registers a hotkey with the conflict manager
        /// </summary>
        /// <param name="hotkeySettings">The hotkey settings to register</param>
        /// <param name="moduleName">The name of the module owning the hotkey</param>
        /// <param name="hotkeyName">The name of the hotkey</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool RegisterHotkey(HotkeySettings hotkeySettings, string moduleName, string hotkeyName)
        {
            if (hotkeySettings == null || !hotkeySettings.IsValid())
            {
                return false;
            }

            var hotkey = Hotkey.FromHotkeySettings(hotkeySettings);
            return AddHotkey(Instance, ref hotkey, moduleName, hotkeyName);
        }

        /// <summary>
        /// Unregisters a hotkey from the conflict manager
        /// </summary>
        /// <param name="hotkeySettings">The hotkey settings to unregister</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool UnregisterHotkey(HotkeySettings hotkeySettings)
        {
            if (hotkeySettings == null || !hotkeySettings.IsValid())
            {
                return false;
            }

            var hotkey = Hotkey.FromHotkeySettings(hotkeySettings);
            return RemoveHotkey(Instance, ref hotkey);
        }
    }
}
