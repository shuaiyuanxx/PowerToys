#include "pch.h"
#include "HotkeyConflictManager.h"
#include "HotkeyConflictManager.g.cpp"

namespace winrt::PowerToys::Interop::implementation
{
    std::unordered_map<uint16_t, winrt::PowerToys::Interop::HotkeyConflict> winrt::PowerToys::Interop::implementation::HotkeyConflictManager::hotkeyMetadata;

    HotkeyConflictManager::HotkeyConflictManager()
    {
    }

    bool HotkeyConflictManager::HasConflict(Hotkey const& _hotkey)
    {
        uint16_t handle = GetHotkeyHandle(_hotkey);
        return (hotkeyMetadata.find(handle) != hotkeyMetadata.end() || HasConflictWithSystemHotkey(_hotkey));
    }

    HotkeyConflict HotkeyConflictManager::GetConflict(Hotkey const& _hotkey, hstring const& _currentModuleName)
    {
        HotkeyConflict conflictHotkeyInfo;
        std::wstring currentModule = _currentModuleName.c_str();
        uint16_t handle = GetHotkeyHandle(_hotkey);

        if (hotkeyMetadata.find(handle) != hotkeyMetadata.end())
        {
            return hotkeyMetadata[handle];
        }

        // Check if shortcut has conflict with system pre-defined hotkeys
        if (HasConflictWithSystemHotkey(_hotkey))
        {
            conflictHotkeyInfo.ConflictingHotkey = _hotkey;
            conflictHotkeyInfo.ModuleName = L"System";
        }

        return conflictHotkeyInfo;
    }

    bool HotkeyConflictManager::AddHotkey(Hotkey const& _hotkey, hstring const& _moduleName)
    {
        std::lock_guard<std::mutex> lock(hotkey_mutex);

        uint16_t handle = GetHotkeyHandle(_hotkey);

        if (HasConflict(_hotkey))
        {
            return false;
        }

        HotkeyConflict hotkeyInfo;
        hotkeyInfo.ModuleName = _moduleName.c_str();
        hotkeyInfo.ConflictingHotkey = _hotkey;
        hotkeyMetadata[handle] = hotkeyInfo;

        return true;
    }

    bool HotkeyConflictManager::RemoveHotkey(winrt::PowerToys::Interop::Hotkey const& _hotkey)
    {
        std::lock_guard<std::mutex> lock(hotkey_mutex);

        uint16_t handle = GetHotkeyHandle(_hotkey);

        auto it = hotkeyMetadata.find(handle);
        if (it == hotkeyMetadata.end())
        {
            return false;
        }

        hotkeyMetadata.erase(it);

        return true;
    }

    bool HotkeyConflictManager::HasConflictWithSystemHotkey(const Hotkey& hotkey)
    {
        // Convert PowerToys Hotkey format to Win32 RegisterHotKey format
        UINT modifiers = 0;
        if (hotkey.Win)
        {
            modifiers |= MOD_WIN;
        }
        if (hotkey.Ctrl)
        {
            modifiers |= MOD_CONTROL;
        }
        if (hotkey.Alt)
        {
            modifiers |= MOD_ALT;
        }
        if (hotkey.Shift)
        {
            modifiers |= MOD_SHIFT;
        }

        // No modifiers or no key is not a valid hotkey
        if (modifiers == 0 || hotkey.Key == 0)
        {
            return false;
        }

        // Use a unique ID for this test registration
        const int hotkeyId = 0x0FFF; // Arbitrary ID for temporary registration

        // Try to register the hotkey with Windows, using nullptr instead of a window handle
        if (!RegisterHotKey(nullptr, hotkeyId, modifiers, hotkey.Key))
        {
            // If registration fails with ERROR_HOTKEY_ALREADY_REGISTERED, it means the hotkey
            // is already in use by the system or another application
            if (GetLastError() == ERROR_HOTKEY_ALREADY_REGISTERED)
            {
                return true;
            }
        }
        else
        {
            // If registration succeeds, unregister it immediately
            UnregisterHotKey(nullptr, hotkeyId);
        }

        return false;
    }

    uint16_t HotkeyConflictManager::GetHotkeyHandle(Hotkey hotkey)
    {
        uint16_t handle = hotkey.Key;
        handle |= hotkey.Win << 8;
        handle |= hotkey.Ctrl << 9;
        handle |= hotkey.Shift << 10;
        handle |= hotkey.Alt << 11;
        return handle;
    }

    void HotkeyConflictManager::Close()
    {
    }
}
