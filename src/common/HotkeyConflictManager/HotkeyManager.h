// HotkeyManager.h : Declaration of the CHotkeyManager

#pragma once
#include "resource.h"       // main symbols

#include <unordered_map>
#include <mutex>

#include "HotkeyConflictManager_i.h"



using namespace ATL;


// CHotkeyManager

class ATL_NO_VTABLE CHotkeyManager :
	public CComObjectRootEx<CComMultiThreadModel>,
	public CComCoClass<CHotkeyManager, &CLSID_HotkeyManager>,
	public ISupportErrorInfo,
	public IDispatchImpl<IHotkeyManager, &IID_IHotkeyManager, &LIBID_HotkeyConflictManagerLib, /*wMajor =*/ 1, /*wMinor =*/ 0>
{
public:

DECLARE_REGISTRY_RESOURCEID(106)


BEGIN_COM_MAP(CHotkeyManager)
	COM_INTERFACE_ENTRY(IHotkeyManager)
	COM_INTERFACE_ENTRY(IDispatch)
	COM_INTERFACE_ENTRY(ISupportErrorInfo)
END_COM_MAP()

// ISupportsErrorInfo
	STDMETHOD(InterfaceSupportsErrorInfo)(REFIID riid);


	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

protected:
	CHotkeyManager() {}

public:
	STDMETHOD(HasConflict)(Hotkey hotkey, BSTR moduleName, BSTR hotkeyName, VARIANT_BOOL* result);
	STDMETHOD(GetConflictInfo)(Hotkey hotkey, BSTR moduleName, BSTR hotkeyName, HotkeyConflictInfo* pConflictInfo);
	STDMETHOD(AddHotkey)(Hotkey hotkey, BSTR moduleName, BSTR hotkeyName, VARIANT_BOOL* result);
	STDMETHOD(RemoveHotkey)(Hotkey hotkey, VARIANT_BOOL* result);

	static CComPtr<CHotkeyManager> CHotkeyManager::GetInstance();

private:
	std::unordered_map<uint16_t, HotkeyConflictInfo> hotkeyMap;
	static std::mutex instanceMutex;
	static CComPtr<CHotkeyManager> instance;

	uint16_t GetHotkeyHandle(const Hotkey& hotkey);
	bool HasConflictWithSystemHotkey(const Hotkey& hotkey);
};

OBJECT_ENTRY_AUTO(__uuidof(HotkeyManager), CHotkeyManager)
