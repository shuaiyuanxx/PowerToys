#include "HotkeyConflictManager.h"
#include <windows.h>
#include <unordered_map>

namespace HotkeyConflict
{
    HotkeyConflictManager* HotkeyConflictManager::instance = nullptr;
    std::mutex HotkeyConflictManager::instanceMutex;

	struct HotkeyConflictInfoImpl
	{
        std::wstring moduleName;
	};

	HotkeyConflictInfo::HotkeyConflictInfo() :
		pImpl(std::make_unique<HotkeyConflictInfoImpl>())
	{
	}

	HotkeyConflictInfo::HotkeyConflictInfo(const HotkeyConflictInfo& other) :
        hotkey(other.hotkey),
        pImpl(std::make_unique<HotkeyConflictInfoImpl>())
    {
        if (other.pImpl)
        {
            pImpl->moduleName = other.pImpl->moduleName;
        }
    }

	HotkeyConflictInfo& HotkeyConflictInfo::operator=(const HotkeyConflictInfo& other)
	{
        if (this != &other)
        {
            hotkey = other.hotkey;
            if (!pImpl)
            {
                pImpl = std::make_unique<HotkeyConflictInfoImpl>();
            }
            if (other.pImpl)
            {
                pImpl->moduleName = other.pImpl->moduleName;
            }
        }
        return *this;
	}

	void HotkeyConflictInfo::SetModuleName(const wchar_t* name)
	{
		if (!pImpl)
		{
            pImpl = std::make_unique<HotkeyConflictInfoImpl>();
		}
        pImpl->moduleName = name ? name : L"";
	}

	const wchar_t* HotkeyConflictInfo::GetModuleName() const
	{
        return pImpl ? pImpl->moduleName.c_str() : L"";
	}

	HotkeyConflictManager& HotkeyConflictManager::GetInstance()
    {
        std::lock_guard<std::mutex> lock(instanceMutex);
        if (instance == nullptr)
        {
            instance = new HotkeyConflictManager();
        }
        return *instance;
    }

    class HotkeyConflictManager::Impl
    {
    public:
        std::unordered_map<uint16_t, HotkeyConflictInfo> hotkeyMetadata;
        std::mutex hotkey_mutex;
    };

	bool HotkeyConflictManager::HasConflict(Hotkey const& _hotkey)
	{
		uint16_t handle = GetHotkeyHandle(_hotkey);
		return (pImpl->hotkeyMetadata.find(handle) != pImpl->hotkeyMetadata.end() || HasConflictWithSystemHotkey(_hotkey));
	}

	HotkeyConflictInfo HotkeyConflictManager::GetConflict(Hotkey const& _hotkey, std::wstring const& _currentModuleName)
	{
		HotkeyConflictInfo conflictHotkeyInfo;

		uint16_t handle = GetHotkeyHandle(_hotkey);

		if (pImpl->hotkeyMetadata.find(handle) != pImpl->hotkeyMetadata.end())
        {
            return pImpl->hotkeyMetadata[handle];
        }

		// Check if shortcut has conflict with system pre-defined hotkeys
		if (HasConflictWithSystemHotkey(_hotkey))
		{
			conflictHotkeyInfo.hotkey = _hotkey;
            conflictHotkeyInfo.SetModuleName(L"System");
		}

		return conflictHotkeyInfo;
	}

	bool HotkeyConflictManager::AddHotkey(Hotkey const& _hotkey, std::wstring const& _moduleName)
	{
		std::lock_guard<std::mutex> lock(pImpl->hotkey_mutex);

		uint16_t handle = GetHotkeyHandle(_hotkey);

		if (HasConflict(_hotkey))
		{
			return false;
		}

		HotkeyConflictInfo hotkeyInfo;
        hotkeyInfo.SetModuleName(_moduleName.c_str());
		hotkeyInfo.hotkey = _hotkey;
		pImpl->hotkeyMetadata[handle] = hotkeyInfo;

		return true;
	}

	bool HotkeyConflictManager::RemoveHotkey(Hotkey const& _hotkey)
	{
		std::lock_guard<std::mutex> lock(pImpl->hotkey_mutex);

		uint16_t handle = GetHotkeyHandle(_hotkey);

		auto it = pImpl->hotkeyMetadata.find(handle);
		if (it == pImpl->hotkeyMetadata.end())
		{
			return false;
		}

		pImpl->hotkeyMetadata.erase(it);

		return true;
	}

	bool HotkeyConflictManager::HasConflictWithSystemHotkey(const Hotkey& hotkey)
	{
		// Convert PowerToys Hotkey format to Win32 RegisterHotKey format
		UINT modifiers = 0;
		if (hotkey.Win)
		{
			modifiers |= MOD_WIN;
		}
		if (hotkey.Ctrl)
		{
			modifiers |= MOD_CONTROL;
		}
		if (hotkey.Alt)
		{
			modifiers |= MOD_ALT;
		}
		if (hotkey.Shift)
		{
			modifiers |= MOD_SHIFT;
		}

		// No modifiers or no key is not a valid hotkey
		if (modifiers == 0 || hotkey.Key == 0)
		{
			return false;
		}

		// Use a unique ID for this test registration
		const int hotkeyId = 0x0FFF; // Arbitrary ID for temporary registration

		// Try to register the hotkey with Windows, using nullptr instead of a window handle
		if (!RegisterHotKey(nullptr, hotkeyId, modifiers, hotkey.Key))
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

	uint16_t HotkeyConflictManager::GetHotkeyHandle(const Hotkey& hotkey)
	{
		uint16_t handle = hotkey.Key;
		handle |= hotkey.Win << 8;
		handle |= hotkey.Ctrl << 9;
		handle |= hotkey.Shift << 10;
		handle |= hotkey.Alt << 11;
		return handle;
	}

	HotkeyConflictManager::HotkeyConflictManager() :
		pImpl(std::make_unique<Impl>())
	{
	}
}
