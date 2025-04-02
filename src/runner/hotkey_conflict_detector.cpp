#include "pch.h"
#include "hotkey_conflict_detector.h"
#include <windows.h>
#include <unordered_map>
#include <cwchar>

namespace HotkeyConflictDetector
{
    HotkeyConflictManager* HotkeyConflictManager::instance = nullptr;
    std::mutex HotkeyConflictManager::instanceMutex;

    HotkeyConflictManager& HotkeyConflictManager::GetInstance()
    {
        std::lock_guard<std::mutex> lock(instanceMutex);
        if (instance == nullptr)
        {
            instance = new HotkeyConflictManager();
        }
        return *instance;
    }

    bool HotkeyConflictManager::HasConflict(Hotkey const& _hotkey, const wchar_t* _moduleName, const wchar_t* _hotkeyName)
    {
        uint16_t handle = GetHotkeyHandle(_hotkey);

        auto it = hotkeyMap.find(handle);

        if (it == hotkeyMap.end())
        {
            return HasConflictWithSystemHotkey(_hotkey);
        }
        if (wcscmp(it->second.moduleName.c_str(), _moduleName) == 0 && wcscmp(it->second.hotkeyName.c_str(), _hotkeyName) == 0)
        {
            // A shortcut matching its own assignment is not considered a conflict.
            return false;
        }

        return true;
    }

    HotkeyConflictInfo HotkeyConflictManager::GetConflict(Hotkey const& _hotkey)
    {
        HotkeyConflictInfo conflictHotkeyInfo;

        uint16_t handle = GetHotkeyHandle(_hotkey);

        if (hotkeyMap.find(handle) != hotkeyMap.end())
        {
            return hotkeyMap[handle];
        }

        // Check if shortcut has conflict with system pre-defined hotkeys
        if (HasConflictWithSystemHotkey(_hotkey))
        {
            conflictHotkeyInfo.hotkey = _hotkey;
            conflictHotkeyInfo.moduleName = L"System";
        }

        return conflictHotkeyInfo;
    }

    bool HotkeyConflictManager::AddHotkey(Hotkey const& _hotkey, const wchar_t* _moduleName, const wchar_t* _hotkeyName)
    {
        uint16_t handle = GetHotkeyHandle(_hotkey);

        if (HasConflict(_hotkey, _moduleName, _hotkeyName))
        {
            return false;
        }

        HotkeyConflictInfo hotkeyInfo;
        hotkeyInfo.moduleName = _moduleName;
        hotkeyInfo.hotkeyName = _hotkeyName;
        hotkeyInfo.hotkey = _hotkey;
        hotkeyMap[handle] = hotkeyInfo;

        return true;
    }

    bool HotkeyConflictManager::RemoveHotkey(Hotkey const& _hotkey)
    {
        uint16_t handle = GetHotkeyHandle(_hotkey);

        auto it = hotkeyMap.find(handle);
        if (it == hotkeyMap.end())
        {
            return false;
        }

        hotkeyMap.erase(it);

        return true;
    }

    bool HotkeyConflictManager::RemoveHotkeyByModule(const std::wstring& moduleName)
    {
        std::lock_guard<std::mutex> lock(hotkeyMutex);
        auto it = hotkeyMap.begin();
        while (it != hotkeyMap.end())
        {
            if (it->second.moduleName == moduleName)
            {
                it = hotkeyMap.erase(it);
            }
            else
            {
                ++it;
            }
        }
        return true;
    }

    bool HotkeyConflictManager::HasConflictWithSystemHotkey(const Hotkey& hotkey)
    {
        // Convert PowerToys Hotkey format to Win32 RegisterHotKey format
        UINT modifiers = 0;
        if (hotkey.win)
        {
            modifiers |= MOD_WIN;
        }
        if (hotkey.ctrl)
        {
            modifiers |= MOD_CONTROL;
        }
        if (hotkey.alt)
        {
            modifiers |= MOD_ALT;
        }
        if (hotkey.shift)
        {
            modifiers |= MOD_SHIFT;
        }

        // No modifiers or no key is not a valid hotkey
        if (modifiers == 0 || hotkey.key == 0)
        {
            return false;
        }

        // Use a unique ID for this test registration
        const int hotkeyId = 0x0FFF; // Arbitrary ID for temporary registration

        // Try to register the hotkey with Windows, using nullptr instead of a window handle
        if (!RegisterHotKey(nullptr, hotkeyId, modifiers, hotkey.key))
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

    uint16_t HotkeyConflictManager::GetHotkeyHandle(const Hotkey& hotkey)
    {
        uint16_t handle = hotkey.key;
        handle |= hotkey.win << 8;
        handle |= hotkey.ctrl << 9;
        handle |= hotkey.shift << 10;
        handle |= hotkey.alt << 11;
        return handle;
    }
}