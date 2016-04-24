#include "stdafx.h"

#include "SerialPortWriter.h"


namespace SerialPortWriter
{
    ///
    ///  class SerialWrapper
    ///
    System::String^ SerialWrapper::OpenComPort ()
    {
        return OpenComPort (9600, 50, 0, false);
    }

    System::String^ SerialWrapper::OpenComPort (int iBaudRate)
    {
        return OpenComPort (iBaudRate, 50, 0, false);
    }

    System::String^ SerialWrapper::OpenComPort (int iBaudRate, int iDeviceNumber)
    {
        return OpenComPort (iBaudRate, iDeviceNumber, 0, false);
    }

    System::String^ SerialWrapper::OpenComPort (int iBaudRate, int iDeviceNumber, int iCloseDelay)
    {
        return OpenComPort (iBaudRate, iDeviceNumber, iDeviceNumber, false);
    }

    System::String^ SerialWrapper::OpenComPort (int iBaudRate, int iDeviceNumber, int iCloseDelay, bool bHexInput)
    {
        int iStatus = m_pobjSerialWriter->OpenComPort (iBaudRate, iDeviceNumber, iCloseDelay, bHexInput);
        if (iStatus > 0)
            ThrowException (iStatus, iBaudRate);

        return GetPortName ();
    }

    System::String^ SerialWrapper::OpenNamedPort (int iPortNumber)
    {
        int iStatus = m_pobjSerialWriter->OpenNamedPort (iPortNumber);

        if (iStatus > 0)
            ThrowException (iStatus);

        return GetPortName ();
    }

    bool SerialWrapper::CloseOutputPort ()
    {
        if (!m_pobjSerialWriter->CloseOutputPort ())
            ThrowException (Constants::SERIAL_NO_OUTPUT_TEXT_PROVIDED); // Serial: CloseHandle failed in CloseOutputPort

        return true;
    }

    String^ SerialWrapper::WriteTextString (String^ strBuffer)
    {
        string strIn (HelperMethodsCLI::ToStdString (strBuffer));
        int iStatus = m_pobjSerialWriter->WriteTextString (strIn.c_str ());

        if (iStatus > 0)
        {
            ThrowException (iStatus);
        }

        return GetPortName ();
    }

    SerialWrapper::SerialWrapper ()
    {
        m_pobjSerialWriter = new SerialWriter;
    }

    SerialWrapper::~SerialWrapper ()
    {
        this->!SerialWrapper ();
    }

    SerialWrapper::!SerialWrapper ()
    {
        delete m_pobjSerialWriter;
    }

    void SerialWrapper::ThrowException (int iStatus, int iBaud)
    {
        if (iStatus > 0)
        {
            switch (iStatus)
            {
                // SerialWriter::OpenComPort
                case WrapperBase::SERAL_INVALID_BAUD_RATE:
                {
                    StringBuilder strbldError ("Seral: Invalid baud rate: must be 9600, 4800, 2400, 1200, 600, 300, or 150.");
                    if (iBaud > 0)
                    {
                        strbldError.Append ("  Got: ");
                        strbldError.Append (iBaud.ToString ());
                    }
                    Exception^ e = gcnew Exception (strbldError.ToString ());
                    throw e;
                }
                case WrapperBase::SERAL_NO_SERIAL_PORT_AVAILABLE:
                {
                    Exception^ e = gcnew Exception ("Seral: No serial port available in OpenComPort");
                    throw e;
                }

                // SerialWriter::CloseOutputPort
                case WrapperBase::SERIAL_CLOSEHANDLE_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: CloseHandle failed in CloseOutputPort");
                    throw e;
                }

                // SerialWriter::WriteTextString
                case WrapperBase::SERIAL_NO_OUTPUT_PORT_FOUND:
                {
                    Exception^ e = gcnew Exception ("Serial: No output port found in WriteTextString (error code 4)");
                    throw e;
                }
                case WrapperBase::SERIAL_NO_OUTPUT_TEXT_PROVIDED:
                {
                    Exception^ e = gcnew Exception ("Serial: No output text provided in WriteTextString (error code 5)");
                    throw e;
                }
                case WrapperBase::SERIAL_WRITEFILE_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: WriteFile failed in WriteTextString (error code 6)");
                    throw e;
                }

                // SerialWriter::OpenNamedPort
                case WrapperBase::SERIAL_CLOSEOUTPUTPORT_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: CloseOutputPort failed in OpenNamedPort (error code 7)");
                    throw e;
                }
                case WrapperBase::SERIAL_UNABLE_TO_OPEN_NAMED_PORT:
                {
                    Exception^ e = gcnew Exception ("Serial: Unable to open named port in OpenNamedPort (error code 8)");
                    throw e;
                }

                // SerialWriter::SetComPortSettings
                case WrapperBase::SERIAL_GETCOMMSTATE_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: GetCommState failed in SetComPortSettings (error code 9)");
                    throw e;
                }
                case WrapperBase::SERIAL_SETCOMMSTATE_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: SetCommState failed in SetComPortSettings (error code 10)");
                    throw e;
                }
                case WrapperBase::SERIAL_SETCOMMTIMEOUTS_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: SetCommTimeouts failed in SetComPortSettings (error code 11)");
                    throw e;
                }
            }
        }
    }

    ///
    ///  class SerialWriter
    ///
    int SerialWriter::OpenComPort (int iBaudRate, int iDeviceNumber, int iCloseDelay, bool bHexInput)
    {
        if (iBaudRate ==BAUD9600 ||
            iBaudRate ==BAUD4800 ||
            iBaudRate ==BAUD2400 ||
            iBaudRate ==BAUD1200 ||
            iBaudRate ==BAUD600  ||
            iBaudRate ==BAUD300  ||
            iBaudRate ==BAUD150)
        {
            m_iBaudRate = iBaudRate;
        }
        else
        {
            return Constants::SERAL_INVALID_BAUD_RATE;
        }
            // Parse device number. SerialSend actually just
            // begins searching at this number and continues
            // working down to zero.
        m_iDeviceNumber = iDeviceNumber;
            // Parse close delay duration. After transmitting
            // the specified text, SerialSend will delay by
            // this number of milliseconds before closing the
            // COM port. Some devices seem to require this.
        m_iCloseDelay   = iCloseDelay;
            // Parse flag for hex byte parsing.
            // If this flag is set, then arbitrary byte values can be
            // included in the string to send using '\x' notation.
            // For example, the command "SerialSend /hex Hello\x0D"
            // sends six bytes in total, the last being the carriage
            // return character, '\r' which has hex value 0x0D.
        m_bHexInput     = bHexInput;

        // Close output port if one is open
        CloseOutputPort ();

        // Determine COM port name
        // Open the highest available serial port number
        OutputDebugText ("Searching serial ports...\n");
        int iPortNumber = m_iDeviceNumber;
        while (iPortNumber >= 0)
        {
            OutputDebugText ("\r                        ");
            OutputDebugText ("\rTrying COM%d...", iPortNumber);

            if (OpenNamedPort (iPortNumber) != 0)
                iPortNumber--;
            else
                break;
        }
   
        if (iPortNumber < 0)
        {
            fprintf (stderr, "No serial port available\n");
            return Constants::SERAL_NO_SERIAL_PORT_AVAILABLE;
        }
   
        OutputDebugText ("OK\n");

        return 0;
    }

    int SerialWriter::OpenNamedPort (int iPortNumber)
    {
        if (!CloseOutputPort ())
            return Constants::SERIAL_CLOSEHANDLE_FAILED; // Serial: CloseOutputPort failed in OpenNamedPort (error code 1)

        char szBuffer[32];
        sprintf (szBuffer, "\\\\.\\COM%d", iPortNumber);
        wchar_t wtext[200];
        mbstowcs (wtext, szBuffer, strlen (szBuffer)+1); //Plus null

        m_hSerial = CreateFile ((LPCWSTR)&wtext,
                                GENERIC_READ | GENERIC_WRITE,
                                0,
                                NULL,
                                OPEN_EXISTING,
                                FILE_ATTRIBUTE_NORMAL,
                                NULL);

        if (m_hSerial != INVALID_HANDLE_VALUE)
        {
            m_strDevName = szBuffer;
            fprintf (stderr, "Using COM port: %s\n", m_strDevName.c_str ());
            return SetComPortSettings ();
        }

        return Constants::SERIAL_NO_OUTPUT_PORT_FOUND; // Serial: Unable to open named port in OpenNamedPort (error code 4)
    }

    bool SerialWriter::CloseOutputPort ()
    {
        if (m_hSerial != nullptr &&
            m_hSerial != INVALID_HANDLE_VALUE)
        {
            // Close serial port
            OutputDebugText ("Closing serial port...");
            if (CloseHandle (m_hSerial) == 0)
            {
                OutputDebugText ("\nError closing %s\n", m_strDevName.c_str ());
                return false; // Serial: CloseHandle failed in CloseOutputPort
            }
            m_hSerial = 0;
            OutputDebugText ("OK\n");
        }

        return true;
    }

    int SerialWriter::WriteTextString (const char* szBuffer)
    {
        if (m_strDevName.empty ())
        {
            int iStatus = OpenComPort ();
            if (iStatus != 0)
            {
                return iStatus;
            }
        }

        if (m_strDevName.empty ())
        {
            return Constants::SERIAL_WRITEFILE_FAILED; // Serial: No output port found in WriteTextString (error code 6)
        }

        size_t iIdx1 = 0,
               iIdx2 = 0;
        char szTextToSend[MAX_PATH];
        char szDigits[MAX_PATH];
        const char* buffer = szBuffer;
        // Check that some text to send was provided
        if (strlen (buffer) == 0)
        {
            OutputDebugText ("Usage:\n\n\tSerialSend [/m_iBaudRate BAUDRATE] ");
            OutputDebugText ("[/devnum DEVICE_NUMBER] [/hex] \"TEXT_TO_SEND\"\n");
            return Constants::SERIAL_CLOSEOUTPUTPORT_FAILED; // Serial: No output text provided in WriteTextString (error code 7)
        }

        // If hex parsing is enabled, modify text to send
        szTextToSend[0] = 0x00;
        szDigits[0] = 0x00;
        while (iIdx1 < strlen (buffer))
        {
            if (m_bHexInput && buffer[iIdx1] == '\\')
            {
                iIdx1++;
                if (buffer[iIdx1] == '\\') szTextToSend[iIdx2] = '\\';
                else if (buffer[iIdx1] == 'n') szTextToSend[iIdx2] = '\n';
                else if (buffer[iIdx1] == 'r') szTextToSend[iIdx2] = '\r';
                else if (buffer[iIdx1] == 'x')
                {
                    szDigits[0] = buffer[++iIdx1];
                    szDigits[1] = buffer[++iIdx1];
                    szDigits[2] = '\0';
                    szTextToSend[iIdx2] = (char)strtol (szDigits, NULL, 16);
                }
            }
            else
            {
                szTextToSend[iIdx2] = buffer[iIdx1];
            }
           
            iIdx2++; iIdx1++;
        }
        szTextToSend[iIdx2] = '\0'; // Null character to terminate string
       
        // Send specified text
        DWORD dwBytesWritten = 0,
              dwTotalBytesWritten = 0;
        OutputDebugText ("Sending text... ");
        while (dwTotalBytesWritten < iIdx2)
        {
            if (!WriteFile (m_hSerial, szTextToSend + dwTotalBytesWritten,
                iIdx2 - dwTotalBytesWritten, &dwBytesWritten, NULL))
            {
                OutputDebugText ("Error writing text to %s\n", m_strDevName.c_str ());
                CloseHandle (m_hSerial);
                return Constants::SERIAL_UNABLE_TO_OPEN_NAMED_PORT; // Serial: WriteFile failed in WriteTextString (error code 8)
            }

            dwTotalBytesWritten += dwBytesWritten;
        }
        OutputDebugText ("\n%d bytes written to %s\n", dwTotalBytesWritten, m_strDevName.c_str ());
     
        // Flush transmit buffer before closing serial port
        FlushFileBuffers (m_hSerial);
        if (m_iCloseDelay > 0)
        {
            OutputDebugText ("Delaying for %d ms before closing COM port... ", m_iCloseDelay);
            Sleep (m_iCloseDelay);
            OutputDebugText ("OK\n");
        }

        return 0;
    }

    SerialWriter::SerialWriter ()
    {
        m_hSerial = 0;
        m_bOutputTrace = false;
        OpenComPort (BAUD9600, 50, 0, false);
    }

    SerialWriter::SerialWriter (int iBaudRate, int iDeviceNumber, int iCloseDelay, bool bHexInput)
    {
        m_hSerial = 0;
        m_bOutputTrace = false;
        OpenComPort (iBaudRate, iDeviceNumber, iCloseDelay, bHexInput);
    }

    SerialWriter::~SerialWriter ()
    {
        CloseOutputPort ();
    }

    int SerialWriter::SetComPortSettings ()
    {
        DCB dcbSerialParams;
        COMMTIMEOUTS timeouts;
        ZeroMemory (&dcbSerialParams, sizeof (DCB));
        ZeroMemory (&timeouts, sizeof (COMMTIMEOUTS));

        // Set device parameters (38400 baud, 1 start bit,
        // 1 stop bit, no parity)
        dcbSerialParams.DCBlength = sizeof (dcbSerialParams);
        if (GetCommState(m_hSerial, &dcbSerialParams) == 0)
        {
            OutputDebugText ("Error getting device state\n");
            CloseHandle (m_hSerial);
            return Constants::SERIAL_GETCOMMSTATE_FAILED; // Serial: GetCommState failed in SetComPortSettings (error code 9)
        }
        //dcbSerialParams.BaudRate = CBR_38400;
        dcbSerialParams.BaudRate = m_iBaudRate;
        dcbSerialParams.ByteSize = 8;
        dcbSerialParams.StopBits = ONESTOPBIT;
        dcbSerialParams.Parity = NOPARITY;
        if (SetCommState (m_hSerial, &dcbSerialParams) == 0)
        {
            OutputDebugText ("Error setting device parameters\n");
            CloseHandle (m_hSerial);
            return Constants::SERIAL_SETCOMMSTATE_FAILED; // Serial: SetCommState failed in SetComPortSettings (error code 10)
        }
   
        // Set COM port timeout settings
        timeouts.ReadIntervalTimeout = 50;
        timeouts.ReadTotalTimeoutConstant = 50;
        timeouts.ReadTotalTimeoutMultiplier = 10;
        timeouts.WriteTotalTimeoutConstant = 50;
        timeouts.WriteTotalTimeoutMultiplier = 10;
        if (SetCommTimeouts (m_hSerial, &timeouts) == 0)
        {
            OutputDebugText ("Error setting timeouts\n");
            CloseHandle (m_hSerial);
            return Constants::SERIAL_SETCOMMTIMEOUTS_FAILED; // Serial: SetCommTimeouts failed in SetComPortSettings (error code 11)
        }

        return 0;
    }

    void SerialWriter::OutputDebugText (const char* sz1)
    {
#ifdef _DEBUG
        if (m_bOutputTrace)
        {
            fprintf (stderr, sz1);
        }
#endif
    }

    void SerialWriter::OutputDebugText (const char* sz1, const char* sz2)
    {
#ifdef _DEBUG
        if (m_bOutputTrace)
        {
            fprintf (stderr, sz1, sz2);
        }
#endif
    }

    void SerialWriter::OutputDebugText (const char* sz1, int i)
    {
#ifdef _DEBUG
        if (m_bOutputTrace)
        {
            fprintf (stderr, sz1, i);
        }
#endif
    }

    void SerialWriter::OutputDebugText (const char* sz1, DWORD dw, const char* sz2)
    {
#ifdef _DEBUG
        if (m_bOutputTrace)
        {
            fprintf (stderr, sz1, dw, sz2);
        }
#endif
    }
}
