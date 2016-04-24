// SerialPortWriter.h

#pragma once

#include "WrapperBaseClass.h"
#include "HelperMethodsClass.h"


using namespace System;
using namespace System::Text;
using namespace System::Runtime::InteropServices;

using namespace std;

using namespace WrapperBaseClass;
using namespace HelperMethodsClass;


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
            void SetOutputTrace (bool bOutputTrace)   { m_bOutputTrace = bOutputTrace; }
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
            int    m_iBaudRate;
            int    m_iDeviceNumber;
            int    m_iCloseDelay;
            HANDLE m_hSerial;
            string m_strDevName;

            static const int BAUD9600 = 9600;
            static const int BAUD4800 = 4800;
            static const int BAUD2400 = 2400;
            static const int BAUD1200 = 1200;
            static const int BAUD600  = 600;
            static const int BAUD300  = 300;
            static const int BAUD150  = 150;
    };

    ///
    ///  class SerialWrapper
    ///
    public ref class SerialWrapper : public WrapperBase
    {
        public:
            System::String^ OpenComPort ();
            System::String^ OpenComPort (int iBaudRate);
            System::String^ OpenComPort (int iBaudRate, int iDeviceNumber);
            System::String^ OpenComPort (int iBaudRate, int iDeviceNumber, int iCloseDelay);
            System::String^ OpenComPort (int iBaudRate, int iDeviceNumber, int iCloseDelay, bool bHexInput);
            bool CloseOutputPort ();
            System::String^ OpenNamedPort (int iPortNumber);
            virtual String^ WriteTextString (String^ systrBuffer) override;
            void SetOutputTrace (bool bOutputTrace) { m_pobjSerialWriter->SetOutputTrace (bOutputTrace); }
            virtual String^ GetPortName () override { return HelperMethodsCLI::ToSystemString (m_pobjSerialWriter->GetPortName ().c_str ());  }

            SerialWrapper();
            ~SerialWrapper();
            !SerialWrapper();

        private:
            void ThrowException (int iStatus) { ThrowException (iStatus, 0); }
            void ThrowException (int iStatus, int iBaud);

            SerialWriter* m_pobjSerialWriter;
    };
}
