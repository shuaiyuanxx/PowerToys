#pragma once
#include <mutex>

#ifdef HOTKEYCONFLICTMANAGER_EXPORTS
#define HOTKEYAPI __declspec(dllexport)
#else
#define HOTKEYAPI __declspec(dllimport)
#endif

#pragma warning(disable : 4251)

namespace HotkeyConflict
{
    struct HOTKEYAPI Hotkey
    {
        bool Win = false;
        bool Ctrl = false;
        bool Shift = false;
        bool Alt = false;
        uint8_t Key = 0;
    };

    struct HotkeyConflictInfoImpl;

    struct HOTKEYAPI HotkeyConflictInfo
    {
        HotkeyConflictInfo();
        HotkeyConflictInfo& operator=(const HotkeyConflictInfo& other);
        HotkeyConflictInfo(const HotkeyConflictInfo& other);

        Hotkey hotkey;

        void SetModuleName(const wchar_t* name);
        const wchar_t* GetModuleName() const;

    private:
        std::unique_ptr<HotkeyConflictInfoImpl> pImpl;
    };

	class HOTKEYAPI HotkeyConflictManager
	{
	public:
        static HotkeyConflictManager& GetInstance();

        bool HasConflict(const Hotkey& hotkey);
        HotkeyConflictInfo GetConflict(const Hotkey& hotkey, const std::wstring& currentModuleName);
        bool AddHotkey(const Hotkey& hotkey, const std::wstring& moduleName);
        bool RemoveHotkey(const Hotkey& hotkey);

	private:
        class Impl;
        std::unique_ptr<Impl> pImpl;

        static std::mutex instanceMutex;
        static HotkeyConflictManager* instance;

        uint16_t GetHotkeyHandle(const Hotkey&);
        bool HasConflictWithSystemHotkey(const Hotkey&);
        
        HotkeyConflictManager();
	};
}
