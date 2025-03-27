#pragma once
#include "HotkeyManager.g.h"

namespace winrt::PowerToys::Interop::implementation
{
    struct HotkeyManager : HotkeyManagerT<HotkeyManager>
    {
        HotkeyManager();

        uint16_t RegisterHotkey(winrt::PowerToys::Interop::Hotkey const& _hotkey, winrt::PowerToys::Interop::HotkeyCallback const& _callback);
        void UnregisterHotkey(uint16_t _handle);
        bool HasConflict(winrt::PowerToys::Interop::Hotkey const& _hotkey, hstring const& _currentModuleName, hstring const& _currentHotkeyName);
        winrt::Windows::Foundation::Collections::IVector<winrt::PowerToys::Interop::HotkeyConflict> GetConflicts(winrt::PowerToys::Interop::Hotkey const& _hotkey, hstring const& _currentModuleName, hstring const& _currentHotkeyName);
        bool RegisterHotkeyWithMetadata(winrt::PowerToys::Interop::Hotkey const& _hotkey, winrt::PowerToys::Interop::HotkeyCallback const& _callback, hstring const& _moduleName, hstring const& _hotkeyName);
        void Close();

    private:
        KeyboardHook keyboardHook{ nullptr };
        std::map<uint16_t, HotkeyCallback> hotkeys;
        Hotkey pressedKeys{ };
        KeyboardEventCallback keyboardEventCallback;
        IsActiveCallback isActiveCallback;
        FilterKeyboardEvent filterKeyboardCallback;

        struct HotkeyMetadata
        {
            Hotkey hotkey = { .Win = false, .Ctrl = false, .Shift = false, .Alt = false, .Key = 0};
            std::wstring moduleName;
            std::wstring hotkeyName;
        };
        std::map<uint16_t, HotkeyMetadata> hotkeyMetadata;

        void KeyboardEventProc(KeyboardEvent ev);
        bool IsActiveProc();
        bool FilterKeyboardProc(KeyboardEvent ev);
        uint16_t GetHotkeyHandle(Hotkey hotkey);
        bool DoHotkeysConflict(const Hotkey& first, const Hotkey& second);
    };
}
namespace winrt::PowerToys::Interop::factory_implementation
{
    struct HotkeyManager : HotkeyManagerT<HotkeyManager, implementation::HotkeyManager>
    {
    };
}
