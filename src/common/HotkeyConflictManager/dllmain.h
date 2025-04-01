// dllmain.h : Declaration of module class.

class CHotkeyConflictManagerModule : public ATL::CAtlDllModuleT< CHotkeyConflictManagerModule >
{
public :
	DECLARE_LIBID(LIBID_HotkeyConflictManagerLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_HOTKEYCONFLICTMANAGER, "{46908bd3-00ee-4716-a32f-bae116bf4382}")
};

extern class CHotkeyConflictManagerModule _AtlModule;
