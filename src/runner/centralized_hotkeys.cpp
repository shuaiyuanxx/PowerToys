#include "pch.h"
#include "centralized_hotkeys.h"
#include "hotkey_manager.h"

#include <map>
#include <common/logger/logger.h>
#include <common/utils/winapi_error.h>
#include <common/SettingsAPI/settings_objects.h>

namespace CentralizedHotkeys
{
    std::map<Shortcut, std::function<void(WORD, WORD)>> actions;
    std::map<Shortcut, int> ids;
    std::mutex mutex;
    HWND runnerWindow;
    int nextId = 0;

    std::wstring ToWstring(const Shortcut& shortcut)
    {
        std::wstring res = L"";
        if (shortcut.modifiersMask & MOD_SHIFT)
        {
            res += L"shift+";
        }

        if (shortcut.modifiersMask & MOD_CONTROL)
        {
            res += L"ctrl+";
        }

        if (shortcut.modifiersMask & MOD_WIN)
        {
            res += L"win+";
        }

        if (shortcut.modifiersMask & MOD_ALT)
        {
            res += L"alt+";
        }

        res += PowerToysSettings::HotkeyObject::key_from_code(shortcut.vkCode);

        return res;
    }

    bool AddHotkeyAction(Shortcut shortcut, Action action, std::wstring moduleName)
    {
        auto& hotkeyManager = HotkeyManager::HotkeyManager::GetInstance();
        hotkeyManager.AddRecord(shortcut, moduleName, shortcut.hotkeyName, action.action);
        return true;
    }

    void RegisterHotkeys()
    {
        Logger::info(L"Registering hotkeys...");
        auto& hotkeyManager = HotkeyManager::HotkeyManager::GetInstance();
        auto& hotkeyEntries = hotkeyManager.GetHotkeyEntries();

        std::lock_guard<std::mutex> lock(mutex);
        for (auto it = hotkeyEntries.begin(); it != hotkeyEntries.end(); ++it)
        {
            if (it->second.size() == 1 && it->second.front().isShortcut)
            {
                Shortcut shortcut = HotkeyManager::HotkeyToShortcut(it->second.front().hotkey);
                auto action = std::get<std::function<void(WORD, WORD)>>(it->second.front().action);
                actions[shortcut] = action;
                
                ids[shortcut] = nextId++;

                if (!RegisterHotKey(runnerWindow, ids[shortcut], shortcut.modifiersMask, shortcut.vkCode))
                {
                    it->second.front().isRegistered = false;
                    Logger::warn(L"Failed to add {} shortcut. {}", ToWstring(shortcut), get_last_error_or_default(GetLastError()));
                }
                else
                {
                    it->second.front().isRegistered = true;
                    Logger::trace(L"{} shortcut registered", ToWstring(shortcut));
                }
            }
        }
    }

    void UnregisterHotkeys()
    {
        std::lock_guard<std::mutex> lock(mutex);
        for (auto it = actions.begin(); it != actions.end(); ++it)
        {
            if (!UnregisterHotKey(runnerWindow, ids[it->first]))
            {
                Logger::warn(L"Failed to unregister {} shortcut. {}", ToWstring(it->first), get_last_error_or_default(GetLastError()));
            }
            else
            {
                Logger::trace(L"{} shortcut unregistered", ToWstring(it->first));
            }
        }

        actions.clear();
        ids.clear();
        nextId = 0;
    }

    void UnregisterHotkeysForModule(std::wstring moduleName)
    {
        auto& hotkeyManager = HotkeyManager::HotkeyManager::GetInstance();
        hotkeyManager.RemoveRecordByModule(moduleName, true);
    }

    void PopulateHotkey(Shortcut shortcut)
    {
        if (!actions.empty())
        {
            try
            {
                actions[shortcut](shortcut.modifiersMask, shortcut.vkCode);
            }
            catch(std::exception& ex)
            {
                Logger::error("Failed to execute hotkey's action. {}", ex.what());
            }
            catch(...)
            {
                Logger::error(L"Failed to execute hotkey's action");
            }
        }
    }

    void RegisterWindow(HWND hwnd)
    {
        runnerWindow = hwnd;
    }
}
