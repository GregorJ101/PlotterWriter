// ParallelPortWriter.h

#pragma once

#include "WrapperBaseClass.h"

#include "HelperMethods.h"


using namespace System;
using namespace System::Runtime::InteropServices;

using namespace std;

using namespace WrapperBaseClass;
using namespace HelperMethods;


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
            virtual String^ QueryPlotter (String^ systrBuffer) override { throw gcnew NotImplementedException (); }
            virtual int QueryPlotterInt (String^ systrBuffer) override { throw gcnew NotImplementedException (); }
            virtual String^ QueryIdentification () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryDigitizedPoint () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryStatus () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryStatusText () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryFactors () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryFactorsText () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryError () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryErrorText () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryActualPosition () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryActualPositionText () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryCommandedPosition () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryCommandedPositionText () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryOptions () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryOptionsText () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryHardClipLimits () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryHardClipLimitsText () override { throw gcnew NotImplementedException (); }
            virtual int QueryExtendedError () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryExtendedErrorText () override { throw gcnew NotImplementedException (); }
            virtual int QueryExtendedStatus () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryExtendedStatusText () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryOutputWindow () override { throw gcnew NotImplementedException (); }
            virtual String^ QueryOutputWindowText () override { throw gcnew NotImplementedException (); }
            virtual int GetPlotterBufferSize () override { throw gcnew NotImplementedException (); }
            virtual int GetPlotterBufferSpace () override { throw gcnew NotImplementedException (); }
            virtual bool IsPlotterBusy (bool bBufferSpaceTrace) override { throw gcnew NotImplementedException (); }
            virtual bool WaitForPlotter (int iTimeOutSeconds) override { throw gcnew NotImplementedException (); }
            virtual bool WaitForPlotter (int iTimeOutSeconds, bool bBufferSpaceTrace) override { throw gcnew NotImplementedException (); }
            virtual bool CloseOutputPort () override { throw gcnew NotImplementedException (); }
            virtual String^ GetPortName () override { return HelperMethodsCLI::ToSystemString (m_objParallelWriter->GetPortName ().c_str ());  }
            virtual bool IsSerial () override { return false; }

            ParallelWrapper ();
            !ParallelWrapper ();
            ~ParallelWrapper ();

        private:

            ParallelWriter* m_objParallelWriter;
	};
}
