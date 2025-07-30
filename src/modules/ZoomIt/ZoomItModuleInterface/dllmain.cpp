#include "pch.h"

#include <modules/interface/powertoy_module_interface.h>
#include <common/SettingsAPI/settings_objects.h>
#include "winrt/PowerToys.ZoomItSettingsInterop.h"

#include "trace.h"
#include <common/logger/logger.h>
#include <common/utils/logger_helper.h>
#include <common/utils/resources.h>
#include <common/utils/winapi_error.h>

#include <shellapi.h>
#include <common/interop/shared_constants.h>

namespace NonLocalizable
{
    const wchar_t ModulePath[] = L"PowerToys.ZoomIt.exe";
    const inline wchar_t ModuleKey[] = L"ZoomIt";
}

BOOL APIENTRY DllMain(HMODULE /*hModule*/,
                      DWORD ul_reason_for_call,
                      LPVOID /*lpReserved*/
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        Trace::RegisterProvider();
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    case DLL_PROCESS_DETACH:
        Trace::UnregisterProvider();
        break;
    }
    return TRUE;
}

class ZoomItModuleInterface : public PowertoyModuleIface
{
public:
    // Return the localized display name of the powertoy
    virtual PCWSTR get_name() override
    {
        return app_name.c_str();
    }

    // Return the non localized key of the powertoy, this will be cached by the runner
    virtual const wchar_t* get_key() override
    {
        return app_key.c_str();
    }

    // Return the configured status for the gpo policy for the module
    virtual powertoys_gpo::gpo_rule_configured_t gpo_policy_enabled_configuration() override
    {
        return powertoys_gpo::getConfiguredZoomItEnabledValue();
    }

    // Return JSON with the configuration options.
    // These are the settings shown on the settings page along with their current values.
    virtual bool get_config(wchar_t* buffer, int* buffer_size) override
    {
        HINSTANCE hinstance = reinterpret_cast<HINSTANCE>(&__ImageBase);

        // TODO: Read settings from Registry.

        // Create a Settings object.
        PowerToysSettings::Settings settings(hinstance, get_name());

        return settings.serialize_to_buffer(buffer, buffer_size);
    }

    // Passes JSON with the configuration settings for the powertoy.
    // This is called when the user hits Save on the settings page.
    virtual void set_config(const wchar_t*) override
    {
        try
        {
            // Parse the input JSON string.
            // TODO: Save settings to registry.
        }
        catch (std::exception&)
        {
            // Improper JSON.
        }
    }

    // Return the list of hotkeys for ZoomIt
    virtual size_t get_hotkeys(Hotkey* hotkeys, size_t buffer_size) override
    {
        if (hotkeys && buffer_size >= NUM_HOTKEYS)
        {
            hotkeys[0] = m_toggle_key;
            hotkeys[1] = m_live_zoom_toggle_key;
            hotkeys[2] = m_draw_toggle_key;
            hotkeys[3] = m_record_toggle_key;
            hotkeys[4] = m_snip_toggle_key;
            hotkeys[5] = m_break_timer_key;
            hotkeys[6] = m_demo_type_toggle_key;
        }

        return NUM_HOTKEYS;
    }

    // Enable the powertoy
    virtual void enable()
    {
        Logger::info(L"ZoomIt enabling");
        Enable();
    }

    // Disable the powertoy
    virtual void disable()
    {
        Logger::info(L"ZoomIt disabling");
        Disable(true);
    }

    // Returns if the powertoy is enabled
    virtual bool is_enabled() override
    {
        return m_enabled;
    }

    // Destroy the powertoy and free memory
    virtual void destroy() override
    {
        Disable(false);
        delete this;
    }

    ZoomItModuleInterface()
    {
        app_name = L"ZoomIt";
        app_key = NonLocalizable::ModuleKey;
        LoggerHelpers::init_logger(app_key, L"ModuleInterface", LogSettings::zoomItLoggerName);
        m_reload_settings_event_handle = CreateDefaultEvent(CommonSharedConstants::ZOOMIT_REFRESH_SETTINGS_EVENT);
        m_exit_event_handle = CreateDefaultEvent(CommonSharedConstants::ZOOMIT_EXIT_EVENT);

        init_settings();
    }

private:
    static const constexpr int NUM_HOTKEYS = 7;

    Hotkey m_toggle_key = { .win = false, .ctrl = true, .shift = false, .alt = false, .key = '1', .id = 0 };
    Hotkey m_live_zoom_toggle_key = { .win = false, .ctrl = true, .shift = false, .alt = false, .key = '4', .id = 1 };
    Hotkey m_draw_toggle_key = { .win = false, .ctrl = true, .shift = false, .alt = false, .key = '2', .id = 2 };
    Hotkey m_record_toggle_key = { .win = false, .ctrl = true, .shift = false, .alt = false, .key = '5', .id = 3 };
    Hotkey m_snip_toggle_key = { .win = false, .ctrl = true, .shift = false, .alt = false, .key = '6', .id = 4 };
    Hotkey m_break_timer_key = { .win = false, .ctrl = true, .shift = false, .alt = false, .key = '3', .id = 5 };
    Hotkey m_demo_type_toggle_key = { .win = false, .ctrl = true, .shift = false, .alt = false, .key = '7', .id = 6 };

    void init_settings()
    {
        try
        {
            winrt::hstring settingsJson = winrt::PowerToys::ZoomItSettingsInterop::ZoomItSettings::LoadSettingsJson();

            PowerToysSettings::PowerToyValues values =
                PowerToysSettings::PowerToyValues::from_json_string(settingsJson.c_str(), get_key());

            read_settings(values);
        }
        catch (...)
        {
            Logger::warn(L"An exception occurred while loading ZoomIt settings from registry. Using default values.");
        }
    }

    void read_settings(PowerToysSettings::PowerToyValues& settings)
    {
        const auto settingsObject = settings.get_raw_json();
        const auto properties_json = settingsObject.GetNamedObject(L"properties", json::JsonObject{});

        if (properties_json.GetView().Size())
        {
            Logger::trace(L"ZoomIt reading settings from registry via ZoomItSettingsInterop");

            const std::array<std::pair<const wchar_t*, std::pair<Hotkey*, int>>, NUM_HOTKEYS> hotkeyMappings = { { { L"ToggleKey", { &m_toggle_key, 0 } },
                                                                                                                   { L"LiveZoomToggleKey", { &m_live_zoom_toggle_key, 1 } },
                                                                                                                   { L"DrawToggleKey", { &m_draw_toggle_key, 2 } },
                                                                                                                   { L"RecordToggleKey", { &m_record_toggle_key, 3 } },
                                                                                                                   { L"SnipToggleKey", { &m_snip_toggle_key, 4 } },
                                                                                                                   { L"BreakTimerKey", { &m_break_timer_key, 5 } },
                                                                                                                   { L"DemoTypeToggleKey", { &m_demo_type_toggle_key, 6 } } } };

            for (const auto& [keyName, hotkeyData] : hotkeyMappings)
            {
                auto [hotkeyPtr, index] = hotkeyData;
                auto parsed_hotkey = parse_single_hotkey(keyName, properties_json, index);
                if (parsed_hotkey.key != 0) // Valid hotkey parsed
                {
                    *hotkeyPtr = parsed_hotkey;
                    Logger::trace(L"ZoomIt loaded hotkey from registry");
                }
                else
                {
                    Logger::trace(L"ZoomIt using default hotkey");
                }
            }
        }
        else
        {
            Logger::info(L"ZoomIt registry settings are empty, using default hotkeys");
        }
    }

    Hotkey parse_single_hotkey(const wchar_t* keyName, const winrt::Windows::Data::Json::JsonObject& settingsObject, int hotkey_id)
    {
        try
        {
            if (settingsObject.HasKey(keyName))
            {
                const auto hotkeyObject = settingsObject.GetNamedObject(keyName);
                const auto valueObject = hotkeyObject.GetNamedObject(L"value");

                bool win = valueObject.GetNamedBoolean(L"win", false);
                bool ctrl = valueObject.GetNamedBoolean(L"ctrl", false);
                bool alt = valueObject.GetNamedBoolean(L"alt", false);
                bool shift = valueObject.GetNamedBoolean(L"shift", false);
                unsigned char key = static_cast<unsigned char>(valueObject.GetNamedNumber(L"code", 0));

                return { win, ctrl, shift, alt, key, hotkey_id };
            }
        }
        catch (...)
        {
            Logger::error(L"Failed to parse ZoomIt hotkey from registry settings. Using default value.");
        }

        return {};
    }

    bool is_enabled_by_default() const override
    {
        return false;
    }

    void Enable()
    {
        m_enabled = true;

        // Log telemetry
        Trace::EnableZoomIt(true);

        // Pass the PID.
        unsigned long powertoys_pid = GetCurrentProcessId();
        std::wstring executable_args = L"";
        executable_args.append(std::to_wstring(powertoys_pid));

        ResetEvent(m_reload_settings_event_handle);
        ResetEvent(m_exit_event_handle);

        SHELLEXECUTEINFOW sei{ sizeof(sei) };
        sei.fMask = { SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAG_NO_UI };
        sei.lpFile = NonLocalizable::ModulePath;
        sei.nShow = SW_SHOWNORMAL;
        sei.lpParameters = executable_args.data();
        if (ShellExecuteExW(&sei) == false)
        {
            Logger::error(L"Failed to start zoomIt");
            auto message = get_last_error_message(GetLastError());
            if (message.has_value())
            {
                Logger::error(message.value());
            }
        }
        else
        {
            m_hProcess = sei.hProcess;
        }
    }

    void Disable(bool const traceEvent)
    {
        m_enabled = false;

        // Log telemetry
        if (traceEvent)
        {
            Trace::EnableZoomIt(false);
        }

        // Tell the ZoomIt process to exit.
        SetEvent(m_exit_event_handle);

        ResetEvent(m_reload_settings_event_handle);

        // Wait for 1.5 seconds for the process to end correctly and stop etw tracer
        WaitForSingleObject(m_hProcess, 1500);

        // If process is still running, terminate it
        if (m_hProcess)
        {
            TerminateProcess(m_hProcess, 0);
            m_hProcess = nullptr;
        }
    }

    bool is_process_running()
    {
        return WaitForSingleObject(m_hProcess, 0) == WAIT_TIMEOUT;
    }

    virtual void call_custom_action(const wchar_t* action) override
    {
        try
        {
            PowerToysSettings::CustomActionObject action_object =
                PowerToysSettings::CustomActionObject::from_json_string(action);

            if (action_object.get_name() == L"refresh_settings")
            {
                init_settings();
                SetEvent(m_reload_settings_event_handle);
            }
        }
        catch (std::exception&)
        {
            Logger::error(L"Failed to parse action.");
        }
    }

    std::wstring app_name;
    std::wstring app_key; //contains the non localized key of the powertoy

    bool m_enabled = false;
    HANDLE m_hProcess = nullptr;

    HANDLE m_reload_settings_event_handle = NULL;
    HANDLE m_exit_event_handle = NULL;
};

extern "C" __declspec(dllexport) PowertoyModuleIface* __cdecl powertoy_create()
{
    return new ZoomItModuleInterface();
}
