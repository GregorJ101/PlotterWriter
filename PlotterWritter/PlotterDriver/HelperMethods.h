// HelperMethodsClass.h

#pragma once

using namespace System;
using namespace System::Runtime::InteropServices;

using namespace std;


namespace HelperMethods
{
	public class HelperMethodsCLI
	{
        public:
            static std::string ToStdString (System::String^ strIn);
            static System::String^ ToSystemString (const char* szIn);
        private:
            HelperMethodsCLI () { }
	};

	class HelperMethodsNative
	{
        public:
            static std::string ConvertStringUTF16toUTF8 (LPWSTR wstr);
            static std::wstring ConvertStringUTF8toUTF16 (LPSTR szUTF8);
            static std::string GetPlotterPrinterName ();
        private:
            HelperMethodsNative () { }
    };
}
