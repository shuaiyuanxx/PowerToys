#include "pch.h"
#include "centralized_hotkeys.h"
#include "hotkey_conflict_detector.h"

#include <map>
#include <common/logger/logger.h>
#include <common/utils/winapi_error.h>
#include <common/SettingsAPI/settings_objects.h>

namespace CentralizedHotkeys
{
    std::map<Shortcut, std::vector<Action>> actions;
    std::map<Shortcut, int> ids;
    HWND runnerWindow;

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
        HotkeyConflictDetector::HotkeyConflictManager& hkmng = HotkeyConflictDetector::HotkeyConflictManager::GetInstance();
        HotkeyConflictDetector::Hotkey hotkey = HotkeyConflictDetector::ShortcutToHotkey(shortcut);
        bool succeed = hkmng.AddHotkey(hotkey, moduleName.c_str(), shortcut.hotkeyName);

        Logger::info(L"Hotkey {} {} in {} has been added to database with return value: {}", shortcut.hotkeyName, moduleName, ToWstring(shortcut), succeed);

        if (!succeed)
        {
            Logger::warn(L"Shortcut conflict detected. Shortcut: {}, from module: {}", ToWstring(shortcut), moduleName);
        }

        action.hotkeyName = shortcut.hotkeyName;
        actions[shortcut].push_back(action);

        // Register hotkey if it is the first shortcut
        if (succeed)
        {
            if (ids.find(shortcut) == ids.end())
            {
                static int nextId = 0;
                ids[shortcut] = nextId++;
            }

            if (!RegisterHotKey(runnerWindow, ids[shortcut], shortcut.modifiersMask, shortcut.vkCode))
            {
                Logger::warn(L"Failed to add {} shortcut. {}", ToWstring(shortcut), get_last_error_or_default(GetLastError()));
                return false;
            }

            actions[shortcut].back().isActivated = true;

            Logger::trace(L"{} shortcut registered", ToWstring(shortcut));
            return true;
        }
        
        // Got conflict, unregister the shortcut.
        for (int i = 0; i < actions[shortcut].size(); ++i)
        {
            if (actions[shortcut][i].isActivated)
            {
                if (!UnregisterHotKey(runnerWindow, ids[shortcut]))
                {
                    Logger::warn(L"Failed to unregister {} shortcut. {}", ToWstring(shortcut), get_last_error_or_default(GetLastError()));
                }
                else
                {
                    Logger::trace(L"{} shortcut unregistered", ToWstring(shortcut));
                }
                actions[shortcut][i].isActivated = false;
            }
        }

        return false;
    }

    void UnregisterHotkeysForModule(std::wstring moduleName)
    {
        HotkeyConflictDetector::HotkeyConflictManager& hkmng = HotkeyConflictDetector::HotkeyConflictManager::GetInstance();

        for (auto it = actions.begin(); it != actions.end(); it++)
        {
            auto val = std::find_if(it->second.begin(), it->second.end(), [moduleName](Action a) { return a.moduleName == moduleName; });
            if (val != it->second.end())
            {
                it->second.erase(val);

                if (it->second.empty())
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
                else if (it->second.size() == 1)
                {
                    Logger::info(L"Found only 1 shortcut, trying to register {}", ToWstring(it->first));

                    if (hkmng.HasConflict(HotkeyConflictDetector::ShortcutToHotkey(it->first), 
                        it->second.front().moduleName.c_str(), 
                        it->second.front().hotkeyName.c_str()))
                    {
                        if (ids.find(it->first) == ids.end())
                        {
                            static int nextId = 0;
                            ids[it->first] = nextId++;
                        }

                        if (!RegisterHotKey(runnerWindow, ids[it->first], it->first.modifiersMask, it->first.vkCode))
                        {
                            Logger::warn(L"Failed to add {} shortcut. {}", ToWstring(it->first), get_last_error_or_default(GetLastError()));
                        }

                        Logger::info(L"Succeed to register hotkey {}", ToWstring(it->first));

                        it->second.front().isActivated = true;
                    }
                }
            }
        }
    }

    void PopulateHotkey(Shortcut shortcut)
    {
        if (!actions.empty())
        {
            try
            {
                actions[shortcut].begin()->action(shortcut.modifiersMask, shortcut.vkCode);
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
