#include "pch.h"
#include "HotkeyManager.h"
#include "HotkeyManager.g.cpp"

namespace winrt::PowerToys::Interop::implementation
{
    HotkeyManager::HotkeyManager()
    {
        keyboardEventCallback = KeyboardEventCallback{ this, &HotkeyManager::KeyboardEventProc };
        isActiveCallback = IsActiveCallback{ this, &HotkeyManager::IsActiveProc };
        filterKeyboardCallback = FilterKeyboardEvent{ this, &HotkeyManager::FilterKeyboardProc };
        keyboardHook = KeyboardHook{ keyboardEventCallback, isActiveCallback, filterKeyboardCallback };
        keyboardHook.Start();
    }

    // When all Shortcut keys are pressed, fire the HotkeyCallback event.
    void HotkeyManager::KeyboardEventProc(KeyboardEvent ev)
    {
        // pressedKeys always stores the latest keyboard state
        auto pressedKeysHandle = GetHotkeyHandle(pressedKeys);
        if (hotkeys.find(pressedKeysHandle) != hotkeys.end())
        {
            hotkeys[pressedKeysHandle]();

            // After invoking the hotkey send a dummy key to prevent Start Menu from activating
            INPUT dummyEvent[1] = {};
            dummyEvent[0].type = INPUT_KEYBOARD;
            dummyEvent[0].ki.wVk = 0xFF;
            dummyEvent[0].ki.dwFlags = KEYEVENTF_KEYUP;
            SendInput(1, dummyEvent, sizeof(INPUT));
        }
    }

    // Hotkeys are intended to be global, therefore they are always active no matter the
    // context in which the keypress occurs.
    bool HotkeyManager::IsActiveProc()
    {
        return true;
    }
    bool HotkeyManager::FilterKeyboardProc(KeyboardEvent ev)
    {
        // Updating the pressed keys here so we know if the keypress event should be propagated or not.
        pressedKeys.Win = (GetAsyncKeyState(VK_LWIN) & 0x8000) || (GetAsyncKeyState(VK_RWIN) & 0x8000);
        pressedKeys.Ctrl = GetAsyncKeyState(VK_CONTROL) & 0x8000;
        pressedKeys.Alt = GetAsyncKeyState(VK_MENU) & 0x8000;
        pressedKeys.Shift = GetAsyncKeyState(VK_SHIFT) & 0x8000;
        pressedKeys.Key = static_cast<unsigned char>(ev.key);

        // Convert to hotkey handle
        auto pressedKeysHandle = GetHotkeyHandle(pressedKeys);

        // Check if any hotkey matches the pressed keys if the current key event is a key down event
        if ((ev.message == WM_KEYDOWN || ev.message == WM_SYSKEYDOWN) && hotkeys.find(pressedKeysHandle)!=hotkeys.end())
        {
            return true;
        }

        return false;
    }

    uint16_t HotkeyManager::RegisterHotkey(winrt::PowerToys::Interop::Hotkey const& _hotkey, winrt::PowerToys::Interop::HotkeyCallback const& _callback)
    {
        auto handle = GetHotkeyHandle(_hotkey);
        hotkeys[handle] = _callback;
        return handle;
    }

    void HotkeyManager::UnregisterHotkey(uint16_t _handle)
    {
        // Clean up metadata when a hotkey is unregistered
        auto metadataIt = hotkeyMetadata.find(_handle);
        if (metadataIt != hotkeyMetadata.end())
        {
            hotkeyMetadata.erase(metadataIt);
        }

        auto iter = hotkeys.find(_handle);
        if (iter != hotkeys.end()) {
            hotkeys.erase(iter);
        }
    }

    bool HotkeyManager::HasConflict(winrt::PowerToys::Interop::Hotkey const& _hotkey, hstring const& _currentModuleName, hstring const& _currentHotkeyName)
    {
        auto conflicts = GetConflicts(_hotkey, _currentModuleName, _currentHotkeyName);
        return conflicts.Size() > 0;
    }

    winrt::Windows::Foundation::Collections::IVector<winrt::PowerToys::Interop::HotkeyConflict> HotkeyManager::GetConflicts(winrt::PowerToys::Interop::Hotkey const& _hotkey, hstring const& _currentModuleName, hstring const& _currentHotkeyName)
    {
        auto result = winrt::single_threaded_vector<PowerToys::Interop::HotkeyConflict>();

        std::wstring currentModule = _currentModuleName.c_str();
        std::wstring currentHotkeyName = _currentHotkeyName.c_str();

        // Check for conflicts with all registered hotkeys
        for (const auto& [id, metadata] : hotkeyMetadata)
        {
            if (DoHotkeysConflict(_hotkey, metadata.hotkey))
            {
                PowerToys::Interop::HotkeyConflict conflict;
                conflict.ModuleName = metadata.moduleName;
                conflict.HotkeyName = metadata.hotkeyName;
                conflict.ConflictingHotkey = metadata.hotkey;
                result.Append(conflict);
            }
        }

        return result;
    }

    bool HotkeyManager::RegisterHotkeyWithMetadata(winrt::PowerToys::Interop::Hotkey const& _hotkey, winrt::PowerToys::Interop::HotkeyCallback const& _callback, hstring const& _moduleName, hstring const& _hotkeyName)
    {
        // Register the hotkey normally first
        uint16_t id = RegisterHotkey(_hotkey, _callback);
        if (id == 0)
        {
            return false;
        }

        // Store metadata
        HotkeyMetadata metadata{};
        metadata.hotkey = _hotkey;
        metadata.moduleName = _moduleName.c_str();
        metadata.hotkeyName = _hotkeyName.c_str();

        hotkeyMetadata[id] = metadata;
        return true;
    }

    bool HotkeyManager::DoHotkeysConflict(const Hotkey& first, const Hotkey& second)
    {
        // Basic case: exact match of all modifiers and key
        if (first.Win == second.Win &&
            first.Ctrl == second.Ctrl &&
            first.Shift == second.Shift &&
            first.Alt == second.Alt &&
            first.Key == second.Key)
        {
            return true;
        }

        // TODO: Additional conflict logic could be added here

        return false;
    }

    void HotkeyManager::Close()
    {
    }

    uint16_t HotkeyManager::GetHotkeyHandle(Hotkey hotkey)
    {
        uint16_t handle = hotkey.Key;
        handle |= hotkey.Win << 8;
        handle |= hotkey.Ctrl << 9;
        handle |= hotkey.Shift << 10;
        handle |= hotkey.Alt << 11;
        return handle;
    }
}
