#pragma once
#include "HotkeyConflictManager.g.h"
#include <mutex>

namespace winrt::PowerToys::Interop::implementation
{
    struct HotkeyConflictManager : HotkeyConflictManagerT<HotkeyConflictManager>
    {
        HotkeyConflictManager();

        bool HasConflict(winrt::PowerToys::Interop::Hotkey const& _hotkey);
        winrt::PowerToys::Interop::HotkeyConflict GetConflict(winrt::PowerToys::Interop::Hotkey const& _hotkey, hstring const& _currentModuleName);
        bool AddHotkey(winrt::PowerToys::Interop::Hotkey const& _hotkey, hstring const& _moduleName);
        bool RemoveHotkey(winrt::PowerToys::Interop::Hotkey const& _hotkey);
        void Close();

    private:
        std::mutex hotkey_mutex;
        static std::unordered_map<uint16_t, HotkeyConflict> hotkeyMetadata;

        uint16_t GetHotkeyHandle(Hotkey hotkey);
        bool HasConflictWithSystemHotkey(const Hotkey& hotkey);
    };
}
namespace winrt::PowerToys::Interop::factory_implementation
{
    struct HotkeyConflictManager : HotkeyConflictManagerT<HotkeyConflictManager, implementation::HotkeyConflictManager>
    {
    };
}
