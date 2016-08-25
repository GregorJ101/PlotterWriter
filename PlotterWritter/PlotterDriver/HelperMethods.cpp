// This is the main DLL file.

#include "stdafx.h"

#include "HelperMethods.h"


namespace HelperMethods
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

    std::string HelperMethodsNative::GetPlotterPrinterName ()
    {
        PRINTER_INFO_2*  pPrtInfo2;
        DWORD            dwPrtInfroCount = 0;
        DWORD            dwPrtInfroSize = 0;
        DWORD            dwPrtInfoLevel = 2;
        string           strPlotterPrinterName;

        EnumPrinters (PRINTER_ENUM_LOCAL|PRINTER_ENUM_CONNECTIONS , NULL, dwPrtInfoLevel, NULL, 0, &dwPrtInfroSize, &dwPrtInfroCount );
        pPrtInfo2 = (PRINTER_INFO_2*)new BYTE[dwPrtInfroSize];

        if (EnumPrinters (PRINTER_ENUM_LOCAL|PRINTER_ENUM_CONNECTIONS , NULL, dwPrtInfoLevel, (LPBYTE)pPrtInfo2, dwPrtInfroSize, &dwPrtInfroSize, &dwPrtInfroCount))
        {
            int iHighUSB = 0;
            string strPrinterName;
            string strPortName;
            string strDriverName;
            for (int iIdx = 0; iIdx < (int)dwPrtInfroCount; iIdx++ )
            {
                strPrinterName = HelperMethodsNative::ConvertStringUTF16toUTF8 (pPrtInfo2[iIdx].pPrinterName);
                strPortName    = HelperMethodsNative::ConvertStringUTF16toUTF8 (pPrtInfo2[iIdx].pPortName);
                strDriverName  = HelperMethodsNative::ConvertStringUTF16toUTF8 (pPrtInfo2[iIdx].pDriverName);

                if (strDriverName.compare ("Generic / Text Only") == 0 &&
                    strPortName.find ("USB") >= 0                      &&
                    strPortName.size () > 4)
                {
                    string s = strPortName.substr (3);
                    int iUSBPort = atoi (s.c_str ());
                    if (iHighUSB < iUSBPort)
                    {
                        iHighUSB = iUSBPort;
                        strPlotterPrinterName = strPrinterName;
                        fprintf (stderr, "Using printer %s on port %s\n", strPlotterPrinterName.c_str (), strPortName.c_str ());
                    }
                }
            }
        }

        if (strPlotterPrinterName.empty ())
        {
            fprintf (stderr, "No printer port found!");
        }

        return strPlotterPrinterName;
    }
}
