// This is the main DLL file.

#include "stdafx.h"

#include "HelperMethodsClass.h"


namespace HelperMethodsClass
{
    // Convert from System::String to STL string
    std::string HelperMethodsCLI::ToStdString (System::String^ strIn)
    {
        IntPtr ip = Marshal::StringToHGlobalAnsi (strIn);
        string strOut = static_cast<const char*>(ip.ToPointer ());
        Marshal::FreeHGlobal (ip);

        return strOut;
    }

    // Convert from non-const char* to System::String
    System::String^ HelperMethodsCLI::ToSystemString (const char* szIn)
    {
        String^ strOut = Marshal::PtrToStringAnsi (static_cast<IntPtr>(const_cast<char*>(szIn)));

        return strOut;
    }

    std::string HelperMethodsNative::ConvertStringUTF16toUTF8 (LPWSTR wstr)
    {
        if (wstr == nullptr)
            return "";

        std::wstring_convert<std::codecvt_utf8<wchar_t>> converter;
        const std::wstring wide_string = wstr;
        const std::string utf8_string = converter.to_bytes(wide_string);
        return utf8_string;
    }

    std::wstring HelperMethodsNative::ConvertStringUTF8toUTF16 (LPSTR szUTF8)
    {
        if (szUTF8 == nullptr)
            return L"";

        std::string strUTF8 (szUTF8);
        int size = MultiByteToWideChar (CP_UTF8, 0, &strUTF8[0], (int)strUTF8.size(), NULL, 0);
        std::wstring wstrUTF16 (size, 0);
        MultiByteToWideChar(CP_UTF8, 0, &strUTF8[0], (int)strUTF8.size(), &wstrUTF16[0], size);
        return wstrUTF16;
    }
}
