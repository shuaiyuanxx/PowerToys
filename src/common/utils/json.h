#pragma once

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Data.Json.h>

#include <optional>
#include <fstream>
#include <Windows.h>
#include <filesystem>
#include <common/logger/logger.h>

namespace json
{
    using namespace winrt::Windows::Data::Json;

    inline std::optional<JsonObject> from_file(std::wstring_view file_name)
    {
        try
        {
            // Get original attributes and ensure file is readable
            DWORD original_attributes = get_file_attributes_and_ensure_writable(file_name);
            
            std::ifstream file(file_name.data(), std::ios::binary);
            std::optional<JsonObject> result = std::nullopt;
            
            if (file.is_open())
            {
                using isbi = std::istreambuf_iterator<char>;
                std::string obj_str{ isbi{ file }, isbi{} };
                result = JsonValue::Parse(winrt::to_hstring(obj_str)).GetObjectW();
            }
            
            // Restore original attributes if they were changed
            restore_file_attributes(file_name, original_attributes);
            
            return result;
        }
        catch (...)
        {
            return std::nullopt;
        }
    }

    inline DWORD get_file_attributes_and_ensure_writable(std::wstring_view file_name)
    {
        // Get the file attributes
        DWORD attributes = GetFileAttributesW(file_name.data());
        
        // If the file has the hidden attribute, temporarily remove it to allow writing
        if (attributes != INVALID_FILE_ATTRIBUTES && (attributes & FILE_ATTRIBUTE_HIDDEN))
        {
            if (!SetFileAttributesW(file_name.data(), attributes & ~FILE_ATTRIBUTE_HIDDEN))
            {
                // Log the error if attributes couldn't be changed
                DWORD error = GetLastError();
                wchar_t message[512];
                FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                               NULL, error, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
                               message, 512, NULL);
                
                Logger::error(L"Failed to remove hidden attribute from file {}: {} (Error code: {})", 
                              file_name, message, error);
            }
            else
            {
                Logger::info(L"Removed hidden attribute from file to enable writing: {}", file_name);
            }
        }
        
        // Ensure parent directory is also not hidden
        std::filesystem::path file_path(file_name);
        if (file_path.has_parent_path())
        {
            DWORD dir_attributes = GetFileAttributesW(file_path.parent_path().c_str());
            if (dir_attributes != INVALID_FILE_ATTRIBUTES && (dir_attributes & FILE_ATTRIBUTE_HIDDEN))
            {
                if (!SetFileAttributesW(file_path.parent_path().c_str(), dir_attributes & ~FILE_ATTRIBUTE_HIDDEN))
                {
                    // Log the error if attributes couldn't be changed
                    DWORD error = GetLastError();
                    wchar_t message[512];
                    FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                                   NULL, error, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
                                   message, 512, NULL);
                    
                    Logger::error(L"Failed to remove hidden attribute from directory {}: {} (Error code: {})", 
                                  file_path.parent_path().wstring(), message, error);
                }
                else
                {
                    Logger::info(L"Removed hidden attribute from directory to enable access: {}", 
                                 file_path.parent_path().wstring());
                }
            }
        }
        
        return attributes;
    }

    inline void restore_file_attributes(std::wstring_view file_name, DWORD original_attributes)
    {
        // Only restore if the original attributes were valid and contained the hidden attribute
        if (original_attributes != INVALID_FILE_ATTRIBUTES && (original_attributes & FILE_ATTRIBUTE_HIDDEN))
        {
            if (!SetFileAttributesW(file_name.data(), original_attributes))
            {
                // Log the error if attributes couldn't be restored
                DWORD error = GetLastError();
                wchar_t message[512];
                FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                              NULL, error, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
                              message, 512, NULL);
                
                Logger::error(L"Failed to restore hidden attribute to file {}: {} (Error code: {})", 
                             file_name, message, error);
            }
            else
            {
                Logger::info(L"Restored hidden attribute to file: {}", file_name);
            }
        }
    }

    inline void to_file(std::wstring_view file_name, const JsonObject& obj)
    {
        // Get original attributes and ensure file is writable
        DWORD original_attributes = get_file_attributes_and_ensure_writable(file_name);
        
        // Write the file
        std::wstring obj_str{ obj.Stringify().c_str() };
        std::ofstream{ file_name.data(), std::ios::binary } << winrt::to_string(obj_str);
        
        // Restore original attributes if they were changed
        restore_file_attributes(file_name, original_attributes);
    }
    inline bool has(
        const json::JsonObject& o,
        std::wstring_view name,
        const json::JsonValueType type = JsonValueType::Object)
    {
        return o.HasKey(name) && o.GetNamedValue(name).ValueType() == type;
    }

    template<typename T>
    inline std::enable_if_t<std::is_arithmetic_v<T>, JsonValue> value(const T arithmetic)
    {
        return json::JsonValue::CreateNumberValue(arithmetic);
    }

    template<typename T>
    inline std::enable_if_t<!std::is_arithmetic_v<T>, JsonValue> value(T s)
    {
        return json::JsonValue::CreateStringValue(s);
    }

    inline JsonValue value(const bool boolean)
    {
        return json::JsonValue::CreateBooleanValue(boolean);
    }

    inline JsonValue value(JsonObject value)
    {
        return value.as<JsonValue>();
    }

    inline JsonValue value(JsonValue value)
    {
        return value; // identity function overload for convenience
    }

    template<typename T, typename D = std::optional<T>>
        requires std::constructible_from<std::optional<T>, D>
    void get(const json::JsonObject& o, const wchar_t* name, T& destination, D default_value = std::nullopt)
    {
        try
        {
            if constexpr (std::is_same_v<T, bool>)
            {
                destination = o.GetNamedBoolean(name);
            }
            else if constexpr (std::is_arithmetic_v<T>)
            {
                destination = static_cast<T>(o.GetNamedNumber(name));
            }
            else if constexpr (std::is_same_v<T, std::wstring>)
            {
                destination = o.GetNamedString(name);
            }
            else if constexpr (std::is_same_v<T, json::JsonObject>)
            {
                destination = o.GetNamedObject(name);
            }
            else
            {
                static_assert(std::bool_constant<std::is_same_v<T, T&>>::value, "Unsupported type");
            }
        }
        catch (...)
        {
            std::optional<T> maybe_default{ std::move(default_value) };
            if (maybe_default.has_value())
                destination = std::move(*maybe_default);
        }
    }

}
