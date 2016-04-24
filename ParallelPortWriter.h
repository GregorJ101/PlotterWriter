// ParallelPortWriter.h

#pragma once

#include "WrapperBaseClass.h"

#include "HelperMethodsClass.h"


using namespace System;
using namespace System::Runtime::InteropServices;

using namespace std;

using namespace WrapperBaseClass;
using namespace HelperMethodsClass;


namespace ParallelPortWriter
{
    ///
    ///  class ParallelWriter
    ///
    class ParallelWriter
    {
        public:
            virtual int WriteTextString (const string& strText);
            int WriteTextString (const char* szPrinterName, const string& strText);
            string GetPortName () const { return m_strPlotterPrinterName; }

            ParallelWriter ();
            ~ParallelWriter () { }

        private:
            void GetPlotterPrinterName ();

            string m_strPlotterPrinterName;
    };

    ///
    ///  class ParallelWrapper
    ///
    public ref class ParallelWrapper : public WrapperBase
	{
        public:
            virtual String^ WriteTextString (String^ systrBuffer) override;
            String^ WriteTextString (String^ systrPrinterName, String^ systrBuffer);
            virtual String^ GetPortName () override { return HelperMethodsCLI::ToSystemString (m_objParallelWriter->GetPortName ().c_str ());  }

            ParallelWrapper ();
            !ParallelWrapper ();
            ~ParallelWrapper ();

        private:

            ParallelWriter* m_objParallelWriter;
	};
}
