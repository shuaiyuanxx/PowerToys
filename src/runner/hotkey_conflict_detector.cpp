#include "pch.h"
#include "hotkey_conflict_detector.h"
#include "common/utils/json.h"
#include <common/SettingsAPI/settings_helpers.h>
#include <windows.h>
#include <unordered_map>
#include <cwchar>

namespace HotkeyConflictDetector
{
    Hotkey HotkeyExToHotkey(const HotkeyEx& hotkeyEx)
    {
        Hotkey hotkey;

        hotkey.win = (hotkeyEx.modifiersMask & MOD_WIN) != 0;
        hotkey.ctrl = (hotkeyEx.modifiersMask & MOD_CONTROL) != 0;
        hotkey.shift = (hotkeyEx.modifiersMask & MOD_SHIFT) != 0;
        hotkey.alt = (hotkeyEx.modifiersMask & MOD_ALT) != 0;

        hotkey.key = hotkeyEx.vkCode > 255 ? 0 : static_cast<unsigned char>(hotkeyEx.vkCode);

        return hotkey;
    }

    Hotkey ShortcutToHotkey(const CentralizedHotkeys::Shortcut& shortcut)
    {
        Hotkey hotkey;

        hotkey.win = (shortcut.modifiersMask & MOD_WIN) != 0;
        hotkey.ctrl = (shortcut.modifiersMask & MOD_CONTROL) != 0;
        hotkey.shift = (shortcut.modifiersMask & MOD_SHIFT) != 0;
        hotkey.alt = (shortcut.modifiersMask & MOD_ALT) != 0;

        hotkey.key = shortcut.vkCode > 255 ? 0 : static_cast<unsigned char>(shortcut.vkCode);

        return hotkey;
    }

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

    HotkeyConflictType HotkeyConflictManager::HasConflict(Hotkey const& _hotkey, const wchar_t* _moduleName, const wchar_t* _hotkeyName)
    {
        uint16_t handle = GetHotkeyHandle(_hotkey);

        auto it = hotkeyMap.find(handle);

        if (it == hotkeyMap.end())
        {
            return HasConflictWithSystemHotkey(_hotkey) ?
                HotkeyConflictType::SystemConflict :
                HotkeyConflictType::NoConflict;
        }
        if (wcscmp(it->second.moduleName.c_str(), _moduleName) == 0 && wcscmp(it->second.hotkeyName.c_str(), _hotkeyName) == 0)
        {
            // A shortcut matching its own assignment is not considered a conflict.
            return HotkeyConflictType::NoConflict;
        }

        return HotkeyConflictType::InAppConflict;
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

        HotkeyConflictType conflictType = HasConflict(_hotkey, _moduleName, _hotkeyName);
        if (conflictType != HotkeyConflictType::NoConflict)
        {
            if (conflictType == HotkeyConflictType::InAppConflict)
            {
                inAppConflictHotkeyMap[handle].insert({ _hotkey, _moduleName, _hotkeyName });
            }
            else
            {
                sysConflictHotkeyMap[handle].insert({ _hotkey, _moduleName, _hotkeyName });
            }
            
            UpdateHotkeyConflictToFile();
            return false;
        }

        HotkeyConflictInfo hotkeyInfo;
        hotkeyInfo.moduleName = _moduleName;
        hotkeyInfo.hotkeyName = _hotkeyName;
        hotkeyInfo.hotkey = _hotkey;
        hotkeyMap[handle] = hotkeyInfo;

        UpdateHotkeyConflictToFile();
        return true;
    }

    bool HotkeyConflictManager::RemoveHotkey(Hotkey const& _hotkey, const std::wstring& moduleName)
    {
        uint16_t handle = GetHotkeyHandle(_hotkey);
        bool foundRecord = false;

        auto it = hotkeyMap.find(handle);
        if (it != hotkeyMap.end() && it->second.moduleName == moduleName)
        {
            hotkeyMap.erase(it);
            foundRecord = true;
        }

        auto it_sys = sysConflictHotkeyMap.find(handle);
        if (it_sys != sysConflictHotkeyMap.end())
        {
            auto& sysConflicts = it_sys->second;
            for (auto it_conf = sysConflicts.begin(); it_conf != sysConflicts.end();)
            {
                if (it_conf->moduleName == moduleName)
                {
                    it_conf = sysConflicts.erase(it_conf);
                    foundRecord = true;
                }
                else
                {
                    ++it_conf;
                }
            }

            if (sysConflicts.empty())
            {
                sysConflictHotkeyMap.erase(it_sys);
            }
        }

        auto it_inApp = inAppConflictHotkeyMap.find(handle);
        if (it_inApp != inAppConflictHotkeyMap.end())
        {
            auto& inAppConflicts = it_inApp->second;
            for (auto it_conf = inAppConflicts.begin(); it_conf != inAppConflicts.end();)
            {
                if (it_conf->moduleName == moduleName)
                {
                    it_conf = inAppConflicts.erase(it_conf);
                    foundRecord = true;
                }
                else
                {
                    ++it_conf;
                }
            }

            if (inAppConflicts.empty())
            {
                inAppConflictHotkeyMap.erase(it_inApp);
            }
        }

        if (foundRecord)
        {
            UpdateHotkeyConflictToFile();
        }

        return foundRecord;
    }

    bool HotkeyConflictManager::RemoveHotkeyByModule(const std::wstring& moduleName)
    {
        std::lock_guard<std::mutex> lock(hotkeyMutex);
        bool foundRecord = false;

        for (auto it = hotkeyMap.begin(); it != hotkeyMap.end();)
        {
            if (it->second.moduleName == moduleName)
            {
                it = hotkeyMap.erase(it);
                foundRecord = true;
            }
            else
            {
                ++it;
            }
        }

        for (auto it = sysConflictHotkeyMap.begin(); it != sysConflictHotkeyMap.end();)
        {
            auto& conflictSet = it->second;
            for (auto setIt = conflictSet.begin(); setIt != conflictSet.end();)
            {
                if (setIt->moduleName == moduleName)
                {
                    setIt = conflictSet.erase(setIt);
                    foundRecord = true;
                }
                else
                {
                    ++setIt;
                }
            }
            if (conflictSet.empty())
            {
                it = sysConflictHotkeyMap.erase(it);
            }
            else
            {
                ++it;
            }
        }

        for (auto it = inAppConflictHotkeyMap.begin(); it != inAppConflictHotkeyMap.end();)
        {
            auto& conflictSet = it->second;
            for (auto setIt = conflictSet.begin(); setIt != conflictSet.end();)
            {
                if (setIt->moduleName == moduleName)
                {
                    setIt = conflictSet.erase(setIt);
                    foundRecord = true;
                }
                else
                {
                    ++setIt;
                }
            }
            if (conflictSet.empty())
            {
                it = inAppConflictHotkeyMap.erase(it);
            }
            else
            {
                ++it;
            }
        }

        if (foundRecord)
        {
            UpdateHotkeyConflictToFile();
        }

        return foundRecord;
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

    bool HotkeyConflictManager::UpdateHotkeyConflictToFile()
    {
        using namespace json;
        JsonObject root;

        auto serializeHotkey = [](const Hotkey& hotkey) -> JsonObject {
            JsonObject obj;
            obj.Insert(L"win", value(hotkey.win));
            obj.Insert(L"ctrl", value(hotkey.ctrl));
            obj.Insert(L"shift", value(hotkey.shift));
            obj.Insert(L"alt", value(hotkey.alt));
            obj.Insert(L"key", value(static_cast<int>(hotkey.key)));
            return obj;
        };

        auto serializeConflictMap = [&](const std::unordered_map<uint16_t, std::unordered_set<HotkeyConflictInfo>>& map) -> JsonArray {
            JsonArray arr;
            for (const auto& [handle, conflicts] : map)
            {
                for (const auto& info : conflicts)
                {
                    JsonObject obj;
                    obj.Insert(L"hotkey", serializeHotkey(info.hotkey));
                    obj.Insert(L"moduleName", value(info.moduleName));
                    obj.Insert(L"hotkeyName", value(info.hotkeyName));
                    arr.Append(obj);
                }
            }
            return arr;
        };

        root.Insert(L"inAppConflicts", serializeConflictMap(inAppConflictHotkeyMap));
        root.Insert(L"sysConflicts", serializeConflictMap(sysConflictHotkeyMap));

        try
        {
            constexpr const wchar_t* hotkey_conflicts_filename = L"\\hotkey_conflicts.json";
            const std::wstring save_file_location = PTSettingsHelper::get_root_save_folder_location() + hotkey_conflicts_filename;
            to_file(save_file_location, root);
            return true;
        }
        catch (...)
        {
            return false;
        }
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