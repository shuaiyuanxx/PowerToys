#pragma once
#include "pch.h"
#include <unordered_map>
#include <string>

#include "../modules/interface/powertoy_module_interface.h"

namespace HotkeyConflictDetector
{
    using Hotkey = PowertoyModuleIface::Hotkey;

    struct HotkeyConflictInfo
    {
        Hotkey hotkey;
        std::wstring moduleName;
        std::wstring hotkeyName;
    };

    class HotkeyConflictManager
    {
    public:
        static HotkeyConflictManager& GetInstance();

        bool HasConflict(const Hotkey& hotkey, const wchar_t* moduleName, const wchar_t* hotkeyName);
        HotkeyConflictInfo GetConflict(const Hotkey& hotkey);
        bool AddHotkey(const Hotkey& hotkey, const wchar_t* moduleName, const wchar_t* hotkeyName);
        bool RemoveHotkey(const Hotkey& hotkey);
        bool RemoveHotkeyByModule(const std::wstring& moduleName);

    private:
        static std::mutex instanceMutex;
        static HotkeyConflictManager* instance;

        std::mutex hotkeyMutex;
        std::unordered_map<uint16_t, HotkeyConflictInfo> hotkeyMap;

        uint16_t GetHotkeyHandle(const Hotkey&);
        bool HasConflictWithSystemHotkey(const Hotkey&);

        HotkeyConflictManager() = default;
    };

};

