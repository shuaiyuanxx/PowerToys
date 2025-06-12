#include "pch.h"
#include "hotkey_manager.h"
#include "common/utils/json.h"
#include <common/SettingsAPI/settings_helpers.h>
#include <windows.h>
#include <unordered_map>
#include <cwchar>

namespace HotkeyManager
{
    Shortcut HotkeyToShortcut(const Hotkey& hotkey)
    {
        WORD modifiersMask = 0;
        if (hotkey.win)
            modifiersMask |= MOD_WIN;
        if (hotkey.ctrl)
            modifiersMask |= MOD_CONTROL;
        if (hotkey.shift)
            modifiersMask |= MOD_SHIFT;
        if (hotkey.alt)
            modifiersMask |= MOD_ALT;

        WORD vkCode = static_cast<WORD>(hotkey.key);

        return Shortcut(modifiersMask, vkCode);
    }

    Hotkey ShortcutToHotkey(const Shortcut& shortcut)
    {
        PowertoyModuleIface::Hotkey hotkey{};
        hotkey.win = (shortcut.modifiersMask & MOD_WIN) != 0;
        hotkey.ctrl = (shortcut.modifiersMask & MOD_CONTROL) != 0;
        hotkey.shift = (shortcut.modifiersMask & MOD_SHIFT) != 0;
        hotkey.alt = (shortcut.modifiersMask & MOD_ALT) != 0;

        hotkey.key = shortcut.vkCode > 255 ? 0 : static_cast<unsigned char>(shortcut.vkCode);

        return hotkey;
    }

    uint16_t GetHotkeyHandle(const Hotkey& hotkey)
    {
        uint16_t handle = hotkey.key;
        handle |= hotkey.win << 8;
        handle |= hotkey.ctrl << 9;
        handle |= hotkey.shift << 10;
        handle |= hotkey.alt << 11;
        return handle;
    }

    HotkeyManager* HotkeyManager::instance = nullptr;
    std::mutex HotkeyManager::instanceMutex;

    HotkeyManager& HotkeyManager::GetInstance()
    {
        std::lock_guard<std::mutex> lock(instanceMutex);
        if (instance == nullptr)
        {
            instance = new HotkeyManager();
        }
        return *instance;
    }

    HotkeyConflictType HotkeyManager::HasConflict(const Hotkey& hotkey, const std::wstring moduleName, const std::wstring hotkeyName)
    {
        uint16_t handle = GetHotkeyHandle(hotkey);

        if (handle == 0)
        {
            return HotkeyConflictType::NoConflict;
        }

        auto it = hotkeyMap.find(handle);

        if (it == hotkeyMap.end())
        {
            return HasConflictWithSystemHotkey(hotkey) ?
                       HotkeyConflictType::SystemConflict :
                       HotkeyConflictType::NoConflict;
        }

        auto& entryList = it->second;

        if (entryList.size() > 1)
        {
            return HotkeyConflictType::InAppConflict;
        }

        // only 1 hotkey in the list, do selfchecking
        auto& hotkeyEntry = entryList.front();
        if (hotkeyEntry.moduleName == moduleName && hotkeyEntry.hotkeyName == hotkeyName)
        {
            // A shortcut matching its own assignment is not considered a conflict.
            if (hotkeyEntry.isRegistered)
            {
                return HotkeyConflictType::NoConflict;
            }
            // If there is only a single hotkey entry, corresponding to the hotkey itself, 
            // and it still fails to register, the most likely cause is a conflict with a system-reserved shortcut.
            return HotkeyConflictType::SystemConflict;
        }


        return HotkeyConflictType::InAppConflict;
    }

    HotkeyConflictType HotkeyManager::HasConflict(const Shortcut& shortcut, const std::wstring moduleName, const std::wstring hotkeyName)
    {
        return HasConflict(ShortcutToHotkey(shortcut), moduleName, hotkeyName);
    }

    // make sure there is conflict before get the conflict
    HotkeyEntry HotkeyManager::GetConflict(const Hotkey& hotkey)
    {
        uint16_t handle = GetHotkeyHandle(hotkey);

        if (hotkeyMap.find(handle) != hotkeyMap.end())
        {
            // return the first item
            return hotkeyMap[handle].front();
        }

        // If cannot find record in hotkeyMap, then the conflict must be system conflict
        HotkeyEntry hotkeyEntry(hotkey, L"System", L"", []() { return false; });
        return hotkeyEntry;
    }

    bool HotkeyManager::AddRecord(const Shortcut& shortcut, const std::wstring& moduleName, const std::wstring& hotkeyName, std::function<void(WORD, WORD)> fun)
    {
        uint16_t handle = GetHotkeyHandle(ShortcutToHotkey(shortcut));

        if (handle == 0)
        {
            return false;
        }

        std::lock_guard<std::mutex> lock(hotkeyMutex);
        auto& entryList = hotkeyMap[handle];
        for (const auto& entry : entryList)
        {
            if (entry.moduleName == moduleName && entry.hotkeyName == hotkeyName)
            {
                return false;
            }
        }

        entryList.emplace_back(shortcut, moduleName, hotkeyName, fun);

        return true;
    }

    bool HotkeyManager::AddRecord(const Hotkey& hotkey, const std::wstring& moduleName, const std::wstring& hotkeyName, std::function<bool()> fun)
    {
        uint16_t handle = GetHotkeyHandle(hotkey);

        if (handle == 0)
        {
            return false;
        }

        std::lock_guard<std::mutex> lock(hotkeyMutex);
        auto& entryList = hotkeyMap[handle];
        for (const auto& entry : entryList)
        {
            if (entry.moduleName == moduleName && entry.hotkeyName == hotkeyName)
            {
                return false;
            }
        }

        entryList.emplace_back(hotkey, moduleName, hotkeyName, fun);

        return true;
    }

    void HotkeyManager::RemoveRecord(Shortcut const& shortcut, const std::wstring& moduleName)
    {
        uint16_t handle = GetHotkeyHandle(ShortcutToHotkey(shortcut));

        std::lock_guard<std::mutex> lock(hotkeyMutex);
        auto it = hotkeyMap.find(handle);
        if (it != hotkeyMap.end())
        {
            auto& entryList = it->second;
            for (auto entryIt = entryList.begin(); entryIt != entryList.end(); )
            {
                if (entryIt->moduleName == moduleName)
                {
                    entryIt = entryList.erase(entryIt);
                }
                else
                {
                    ++entryIt;
                }
            }

            if (entryList.empty())
            {
                hotkeyMap.erase(it);
            }
        }
    }

    // isShortcut: true -> remove shortcut from hotkeyMap, this type use RegisterHotkey API
    // isShortcut: false -> remove hotkey from hotkeyMap, this hotkey register by low level keyboard hook
    void HotkeyManager::RemoveRecordByModule(const std::wstring& moduleName, bool isShortcut)
    {
        std::lock_guard<std::mutex> lock(hotkeyMutex);

        for (auto it = hotkeyMap.begin(); it != hotkeyMap.end(); )
        {
            auto& entryList = it->second;
            for (auto entryIt = entryList.begin(); entryIt != entryList.end(); )
            {
                if (entryIt->moduleName == moduleName && entryIt->isShortcut == isShortcut)
                {
                    entryIt = entryList.erase(entryIt);
                }
                else
                {
                    ++entryIt;
                }
            }
           
            if (entryList.empty())
            {
                it = hotkeyMap.erase(it);
            }
            else
            {
                ++it;
            }
        }
    }

    bool HotkeyManager::HasConflictWithSystemHotkey(const Hotkey& hotkey)
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

    std::unordered_map<uint16_t, std::list<HotkeyEntry>>& HotkeyManager::GetHotkeyEntries()
    {
        return hotkeyMap;
    }

    /*
    bool HotkeyManager::UpdateHotkeyConflictToFile()
    {
        static bool isDirty = true;
        static bool writeScheduled = false;
        isDirty = true;

        // If a write is already scheduled, don't schedule another one
        if (writeScheduled)
            return true;

        writeScheduled = true;

        // Schedule a write after a short delay to batch multiple changes
        std::thread([this]() {
            std::this_thread::sleep_for(std::chrono::milliseconds(300));

            std::lock_guard<std::mutex> lock(hotkeyMutex);
            if (isDirty)
            {
                using namespace json;
                JsonObject root;

                // Serialize hotkey to a unique string format for grouping
                auto serializeHotkey = [](const Hotkey& hotkey) -> JsonObject {
                    JsonObject obj;
                    obj.Insert(L"win", value(hotkey.win));
                    obj.Insert(L"ctrl", value(hotkey.ctrl));
                    obj.Insert(L"shift", value(hotkey.shift));
                    obj.Insert(L"alt", value(hotkey.alt));
                    obj.Insert(L"key", value(static_cast<int>(hotkey.key)));
                    return obj;
                };

                // New format: Group conflicts by hotkey
                JsonArray inAppConflictsArray;
                JsonArray sysConflictsArray;

                // Process in-app conflicts
                std::map<uint16_t, std::vector<HotkeyEntry>> groupedInAppConflicts;
                for (const auto& [handle, conflicts] : inAppConflictHotkeyMap)
                {
                    groupedInAppConflicts[handle].push_back(hotkeyMap[handle]);
                    for (const auto& info : conflicts)
                    {
                        groupedInAppConflicts[handle].push_back(info);
                    }
                }

                // Serialize grouped in-app conflicts
                for (const auto& [handle, conflictInfos] : groupedInAppConflicts)
                {
                    if (!conflictInfos.empty())
                    {
                        JsonObject conflictGroup;

                        // All entries have the same hotkey, so use the first one for the key
                        conflictGroup.Insert(L"hotkey", serializeHotkey(conflictInfos[0].hotkey));

                        // Create an array of module info without repeating the hotkey
                        JsonArray modules;
                        for (const auto& info : conflictInfos)
                        {
                            JsonObject moduleInfo;
                            moduleInfo.Insert(L"moduleName", value(info.moduleName));
                            moduleInfo.Insert(L"hotkeyName", value(info.hotkeyName));
                            modules.Append(moduleInfo);
                        }

                        conflictGroup.Insert(L"modules", modules);
                        inAppConflictsArray.Append(conflictGroup);
                    }
                }

                // Process system conflicts
                std::map<uint16_t, std::vector<HotkeyEntry>> groupedSysConflicts;
                for (const auto& [handle, conflicts] : sysConflictHotkeyMap)
                {
                    for (const auto& info : conflicts)
                    {
                        groupedSysConflicts[handle].push_back(info);
                    }
                }

                // Serialize grouped system conflicts
                for (const auto& [handle, conflictInfos] : groupedSysConflicts)
                {
                    if (!conflictInfos.empty())
                    {
                        JsonObject conflictGroup;

                        // All entries have the same hotkey, so use the first one for the key
                        conflictGroup.Insert(L"hotkey", serializeHotkey(conflictInfos[0].hotkey));

                        // Create an array of module info without repeating the hotkey
                        JsonArray modules;
                        for (const auto& info : conflictInfos)
                        {
                            JsonObject moduleInfo;
                            moduleInfo.Insert(L"moduleName", value(info.moduleName));
                            moduleInfo.Insert(L"hotkeyName", value(info.hotkeyName));
                            modules.Append(moduleInfo);
                        }

                        conflictGroup.Insert(L"modules", modules);
                        sysConflictsArray.Append(conflictGroup);
                    }
                }

                // Add the grouped conflicts to the root object
                root.Insert(L"inAppConflicts", inAppConflictsArray);
                root.Insert(L"sysConflicts", sysConflictsArray);

                try
                {
                    constexpr const wchar_t* hotkey_conflicts_filename = L"\\hotkey_conflicts.json";
                    const std::wstring save_file_location = PTSettingsHelper::get_root_save_folder_location() + hotkey_conflicts_filename;
                    to_file(save_file_location, root);
                    isDirty = false;
                }
                catch (...)
                {
                    // Write failed, leave isDirty as true for next attempt
                }
            }

            writeScheduled = false;
        }).detach();

        return true;
    }*/

    
}