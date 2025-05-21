#pragma once
#include "pch.h"
#include <unordered_map>
#include <string>

#include "../modules/interface/powertoy_module_interface.h"
#include "centralized_hotkeys.h"

namespace HotkeyConflictDetector
{
    using Hotkey = PowertoyModuleIface::Hotkey;
    using HotkeyEx = PowertoyModuleIface::HotkeyEx;
    using Shortcut = CentralizedHotkeys::Shortcut;

    struct HotkeyConflictInfo
    {
        Hotkey hotkey;
        std::wstring moduleName;
        std::wstring hotkeyName;
    };

    Hotkey HotkeyExToHotkey(const HotkeyEx& hotkeyEx);

    Hotkey ShortcutToHotkey(const CentralizedHotkeys::Shortcut& shortcut);

    enum HotkeyConflictType
    {
        NoConflict = 0,
        SystemConflict = 1,
        InAppConflict = 2,
    };

    class HotkeyConflictManager
    {
    public:
        static HotkeyConflictManager& GetInstance();

        HotkeyConflictType HasConflict(const Hotkey& hotkey, const wchar_t* moduleName, const wchar_t* hotkeyName);
        HotkeyConflictInfo GetConflict(const Hotkey& hotkey);
        bool AddHotkey(const Hotkey& hotkey, const wchar_t* moduleName, const wchar_t* hotkeyName);
        bool RemoveHotkey(const Hotkey& hotkey);
        bool RemoveHotkeyByModule(const std::wstring& moduleName);

    private:
        static std::mutex instanceMutex;
        static HotkeyConflictManager* instance;

        std::mutex hotkeyMutex;
        // Hotkey in hotkeyMap means the hotkey has been registered successfully
        std::unordered_map<uint16_t, HotkeyConflictInfo> hotkeyMap;
        // Hotkey in sysConflictHotkeyMap means the hotkey has conflict with system defined hotkeys
        std::unordered_map<uint16_t, std::vector<HotkeyConflictInfo>> sysConflictHotkeyMap;
        // Hotkey in inAppConflictHotkeyMap means the hotkey has conflict with other modules
        std::unordered_map<uint16_t, std::vector<HotkeyConflictInfo>> inAppConflictHotkeyMap;

        uint16_t GetHotkeyHandle(const Hotkey&);
        bool HasConflictWithSystemHotkey(const Hotkey&);

        bool UpdateHotkeyConflictToFile();

        HotkeyConflictManager() = default;
    };
};

