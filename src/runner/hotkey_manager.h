#pragma once
#include "pch.h"
#include <unordered_map>
#include <unordered_set>
#include <string>
#include <variant>

#include "../modules/interface/powertoy_module_interface.h"
#include "centralized_hotkeys.h"

namespace HotkeyManager
{
    using Hotkey = PowertoyModuleIface::Hotkey;
    using HotkeyEx = PowertoyModuleIface::HotkeyEx;
    using Shortcut = CentralizedHotkeys::Shortcut;

    enum HotkeyConflictType
    {
        NoConflict = 0,
        SystemConflict = 1,
        InAppConflict = 2,
    };

    Shortcut HotkeyToShortcut(const Hotkey& hotkey);
    Hotkey ShortcutToHotkey(const CentralizedHotkeys::Shortcut& shortcut);
    uint16_t GetHotkeyHandle(const Hotkey&);

    struct HotkeyEntry
    {
        Hotkey hotkey;
        std::wstring moduleName;
        std::wstring hotkeyName;
        // The hotkey type can be PowertoyModuleIface::Hotkey or CentralizedHotkeys::Shortcut
        bool isShortcut;
        // The hotkey has been registered or not
        bool isRegistered;


        std::variant<std::function<void(WORD, WORD)>, std::function<bool()>> action;

        HotkeyEntry(Hotkey hotkey, std::wstring mName, std::wstring hkName, std::function<bool()> fun)
        {
            this->hotkey = hotkey;
            this->moduleName = mName;
            this->hotkeyName = hkName;
            this->action = fun;
            this->isShortcut = false;
            this->isRegistered = false;
        }

        HotkeyEntry(Shortcut shortcut, std::wstring mName, std::wstring hkName, std::function<void(WORD, WORD)> fun)
        {
            this->hotkey = ShortcutToHotkey(shortcut);
            this->moduleName = mName;
            this->hotkeyName = hkName;
            this->action = fun;
            this->isShortcut = true;
            this->isRegistered = false;
        }

        inline bool operator==(const HotkeyEntry& other) const  
        {  
           return hotkey == other.hotkey &&  
                  moduleName == other.moduleName &&  
                  hotkeyName == other.hotkeyName;  
        }
    };



    class HotkeyManager
    {
    public:
        static HotkeyManager& GetInstance();

        HotkeyConflictType HasConflict(const Hotkey& hotkey, const std::wstring moduleName, const std::wstring hotkeyName);
        HotkeyConflictType HasConflict(const Shortcut& shortcut, const std::wstring moduleName, const std::wstring hotkeyName);
        HotkeyEntry GetConflict(const Hotkey& hotkey);
        bool AddRecord(const Hotkey& hotkey, const std::wstring& moduleName, const std::wstring& hotkeyName, std::function<bool()> fun);
        bool AddRecord(const Shortcut& shortcut, const std::wstring& moduleName, const std::wstring& hotkeyName, std::function<void(WORD, WORD)> fun);
        void RemoveRecord(const Shortcut& shortcut, const std::wstring& moduleName);
        void RemoveRecordByModule(const std::wstring& moduleName, bool isShortcut);
        std::unordered_map<uint16_t, std::list<HotkeyEntry>>& GetHotkeyEntries();

    private:
        static std::mutex instanceMutex;
        static HotkeyManager* instance;

        std::mutex hotkeyMutex;
        // Hotkey in hotkeyMap means the hotkey has been registered successfully
        std::unordered_map<uint16_t, std::list<HotkeyEntry>> hotkeyMap;
        // Hotkey in sysConflictHotkeyMap means the hotkey has conflict with system defined hotkeys
        std::unordered_map<uint16_t, std::unordered_set<HotkeyEntry>> sysConflictHotkeyMap;
        // Hotkey in inAppConflictHotkeyMap means the hotkey has conflict with other modules
        std::unordered_map<uint16_t, std::unordered_set<HotkeyEntry>> inAppConflictHotkeyMap;

        bool HasConflictWithSystemHotkey(const Hotkey&);

        //bool UpdateHotkeyConflictToFile();

        HotkeyManager() = default;
    };
};

namespace std
{
    template<>
    struct hash<HotkeyManager::HotkeyEntry>
    {
        size_t operator()(const HotkeyManager::HotkeyEntry& info) const
        {

            size_t hotkeyHash =
                (info.hotkey.win ? 1ULL : 0ULL) |
                ((info.hotkey.ctrl ? 1ULL : 0ULL) << 1) |
                ((info.hotkey.shift ? 1ULL : 0ULL) << 2) |
                ((info.hotkey.alt ? 1ULL : 0ULL) << 3) |
                (static_cast<size_t>(info.hotkey.key) << 4);

            size_t moduleHash = std::hash<std::wstring>{}(info.moduleName);
            size_t nameHash = std::hash<std::wstring>{}(info.hotkeyName);

            return hotkeyHash ^ 
                ((moduleHash << 1) | (moduleHash >> (sizeof(size_t) * 8 - 1))) ^    // rotate left 1 bit
                ((nameHash << 2) | (nameHash >> (sizeof(size_t) * 8 - 2)));         // rotate left 2 bits
        }
    };
}
