// SerialPortWriter.h

#pragma once

#include "WrapperBaseClass.h"
#include "HelperMethods.h"


using namespace System;
using namespace System::Text;
using namespace System::Runtime::InteropServices;

using namespace std;

using namespace WrapperBaseClass;
using namespace HelperMethods;
using namespace Constants;

namespace SerialPortWriter
{
    ///
    ///  class SerialWriter
    ///
    class SerialWriter
    {
        public:
            int OpenComPort (int iBaudRate = BAUD9600, int iDeviceNumber = 50, int iCloseDelay = 0, bool bHexInput = false);
            int OpenNamedPort (int iPortNumber);
            bool CloseOutputPort ();
            virtual int WriteTextString (const char* szBuffer);
            int WriteData (const char* szBuffer);
            string ReadData (bool bShowEmptyBuffer);
            int ReadData (void* vBbuffer, int iLimit);
            int GetPlotterBufferSize () { return GetPlotterBufferSize (false); }
            int GetPlotterBufferSize (bool bShowConsoleTrace);
            int GetPlotterBufferSpace ();
            int IsPlotterBusy (bool bBufferSpaceTrace);
            void SetOutputTrace (bool bOutputTrace)   { m_bOutputTrace = bOutputTrace; }
            void SetConsoleOutputTrace (bool bConsoleOutputTrace)   { m_bConsoleOutputTrace = bConsoleOutputTrace; }
            bool GetConsoleOutputTrace ()   { return m_bConsoleOutputTrace; }
            string GetPortName () const { return m_strDevName; }

            SerialWriter ();
            SerialWriter (int iBaudRate, int iDeviceNumber = 50, int iCloseDelay = 0, bool bHexInput = false);
            ~SerialWriter ();

        private:
            int    SetComPortSettings ();
            void   OutputDebugText (const char* sz1);
            void   OutputDebugText (const char* sz1, const char* sz2);
            void   OutputDebugText (const char* sz1, int i);
            void   OutputDebugText (const char* sz1, DWORD dw, const char* sz2);

            bool   m_bHexInput;
            bool   m_bOutputTrace;
            bool   m_bConsoleOutputTrace;
            int    m_iBaudRate;
            int    m_iDeviceNumber;
            int    m_iCloseDelay;
            int    m_iTotalBufferSize;
            HANDLE m_hSerial;
            string m_strDevName;

            static const int BUFFER_SIZE        = 5120;
            static const int BUFFER_HEADROOM    = 20;
            static const int TEXT_BLOCK_SIZE    = 40;
            static const int WAIT_TIME_LIMIT_MS = 30000;
            static const int BAUD9600           = 9600;
            static const int BAUD4800           = 4800;
            static const int BAUD2400           = 2400;
            static const int BAUD1200           = 1200;
            static const int BAUD600            = 600;
            static const int BAUD300            = 300;
            static const int BAUD150            = 150;
    };

    ///
    ///  class SerialWrapper
    ///
    public ref class SerialWrapper : public WrapperBase
    {
        public:
            String^ OpenComPort ();
            String^ OpenComPort (int iBaudRate);
            String^ OpenComPort (int iBaudRate, int iDeviceNumber);
            String^ OpenComPort (int iBaudRate, int iDeviceNumber, int iCloseDelay);
            String^ OpenComPort (int iBaudRate, int iDeviceNumber, int iCloseDelay, bool bHexInput);
            String^ OpenNamedPort (int iPortNumber);
            virtual String^ WriteTextString (String^ systrBuffer) override;
            virtual String^ QueryPlotter (String^ systrBuffer) override;
            virtual int QueryPlotterInt (String^ systrBuffer) override;
            virtual String^ QueryIdentification () override;
            virtual String^ QueryDigitizedPoint () override;
            virtual String^ QueryStatus () override;
            virtual String^ QueryStatusText () override;
            virtual String^ QueryFactors () override;
            virtual String^ QueryFactorsText () override;
            virtual String^ QueryError () override;
            virtual String^ QueryErrorText () override;
            virtual String^ QueryActualPosition () override;
            virtual String^ QueryActualPositionText () override;
            virtual String^ QueryCommandedPosition () override;
            virtual String^ QueryCommandedPositionText () override;
            virtual String^ QueryOptions () override;
            virtual String^ QueryOptionsText () override;
            virtual String^ QueryHardClipLimits () override;
            virtual String^ QueryHardClipLimitsText () override;
            virtual int QueryExtendedError () override;
            virtual String^ QueryExtendedErrorText () override;
            virtual int QueryExtendedStatus () override;
            virtual String^ QueryExtendedStatusText () override;
            virtual String^ QueryOutputWindow () override;
            virtual String^ QueryOutputWindowText () override;
            virtual int GetPlotterBufferSize () override { return m_pobjSerialWriter->GetPlotterBufferSize (); }
            virtual int GetPlotterBufferSpace () override { return m_pobjSerialWriter->GetPlotterBufferSpace (); }
            virtual bool IsPlotterBusy (bool bBufferSpaceTrace) override
            { return m_pobjSerialWriter->IsPlotterBusy (bBufferSpaceTrace) == Constants::SERIAL_PLOTTER_IS_BUSY; }
            virtual bool WaitForPlotter (int iTimeOutSeconds) override;
            virtual bool WaitForPlotter (int iTimeOutSeconds, bool bBufferSpaceTrace) override;
            void SetOutputTrace (bool bOutputTrace) { m_pobjSerialWriter->SetOutputTrace (bOutputTrace); }
            void SetConsoleOutputTrace (bool bConsoleOutputTrace) { m_pobjSerialWriter->SetConsoleOutputTrace (bConsoleOutputTrace); }
            virtual bool CloseOutputPort () override;
            virtual String^ GetPortName () override { return HelperMethodsCLI::ToSystemString (m_pobjSerialWriter->GetPortName ().c_str ()); }
            virtual bool IsSerial () override { return true; }

            SerialWrapper();
            ~SerialWrapper();
            !SerialWrapper();

        private:
            void ThrowException (int iStatus) { ThrowException (iStatus, 0); }
            void ThrowException (int iStatus, int iBaud);

            SerialWriter* m_pobjSerialWriter;
    };
}
