#include "stdafx.h"

#include "SerialPortWriter.h"


namespace SerialPortWriter
{
    ///
    ///  class SerialWrapper
    ///
    String^ SerialWrapper::OpenComPort ()
    {
        return OpenComPort (9600, 50, 0, false);
    }

    String^ SerialWrapper::OpenComPort (int iBaudRate)
    {
        return OpenComPort (iBaudRate, 50, 0, false);
    }

    String^ SerialWrapper::OpenComPort (int iBaudRate, int iDeviceNumber)
    {
        return OpenComPort (iBaudRate, iDeviceNumber, 0, false);
    }

    String^ SerialWrapper::OpenComPort (int iBaudRate, int iDeviceNumber, int iCloseDelay)
    {
        return OpenComPort (iBaudRate, iDeviceNumber, iDeviceNumber, false);
    }

    String^ SerialWrapper::OpenComPort (int iBaudRate, int iDeviceNumber, int iCloseDelay, bool bHexInput)
    {
        int iStatus = m_pobjSerialWriter->OpenComPort (iBaudRate, iDeviceNumber, iCloseDelay, bHexInput);
        if (iStatus > 0)
            ThrowException (iStatus, iBaudRate);

        return GetPortName ();
    }

    String^ SerialWrapper::OpenNamedPort (int iPortNumber)
    {
        int iStatus = m_pobjSerialWriter->OpenNamedPort (iPortNumber);

        if (iStatus > 0)
            ThrowException (iStatus);

        return GetPortName ();
    }

    bool SerialWrapper::CloseOutputPort ()
    {
        if (!m_pobjSerialWriter->CloseOutputPort ())
            ThrowException (Constants::SERIAL_CLOSEHANDLE_FAILED); // Serial: CloseHandle failed in CloseOutputPort

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

    String^ SerialWrapper::QueryPlotter (String^ systrBuffer)
    {
        string strIn (HelperMethodsCLI::ToStdString (systrBuffer));
        int iStatus = m_pobjSerialWriter->WriteData (strIn.c_str ());

        if (iStatus > 0)
        {
            ThrowException (iStatus);
        }

        String^ strData = HelperMethodsCLI::ToSystemString (m_pobjSerialWriter->ReadData (m_pobjSerialWriter->GetConsoleOutputTrace ()).c_str ());
        return strData;
    }

    int SerialWrapper::QueryPlotterInt (String^ systrBuffer)
    {
        String^ strData = QueryPlotter (systrBuffer);
        if (strData->Length == 0)
            return -1;
        else
            return Convert::ToInt16 (strData);
    }

    String^ SerialWrapper::QueryIdentification ()
    {
        return QueryPlotter ("OI;");
    }

    String^ SerialWrapper::QueryDigitizedPoint ()
    {
        String^ strResponse = QueryPlotter ("OD;");
        return strResponse;
    }

    String^ SerialWrapper::QueryStatus ()
    {
        String^ strResponse = QueryPlotter ("OS;");
        return strResponse;
    }

    String^ SerialWrapper::QueryStatusText ()
    {
        String^ strResponse = QueryStatus ();
        int iStatus = Convert::ToInt16 (strResponse);
        String^ strStatusText = gcnew String ("Plotter status: ");
        strStatusText += strResponse;
        strStatusText += "\n";

        if (iStatus & 64)
        {
            strStatusText += "  Bit 6: Require service message set (always 0 for OS;\n";
            strStatusText += "         0 or 1 for HP-IB serial poll).\n";
        }
        if (iStatus & 32)
        {
            strStatusText += "  Bit 5: Error; cleared by reading OE output in HP-IB system or by output\n";
            strStatusText += "         of the error in RS-232-C system, or by IN instruction.\n";
        }
        if (iStatus & 16)
        {
            strStatusText += "  Bit 4: Ready for data; pinch wheels down.\n";
        }
        if (iStatus & 8)
        {
            strStatusText += "  Bit 3: Initialized; cleared by reading OS output in HP-IB system\n";
            strStatusText += "         or by output of the status byte in RS-232-C system.\n";
        }
        if (iStatus & 4)
        {
            strStatusText += "  Bit 2: Digitized point available; cleared by reading digitized value in HP-IB\n";
            strStatusText += "         system or by output of point in RS-232-C system, or by IN instruction.\n";
        }
        if (iStatus & 2)
        {
            strStatusText += "  Bit 1: PI or P2 changed; cleared by reading output of OP in HP-IB system or\n";
            strStatusText += "         by actual output of Pl,P2 in RS-232-C system, or by IN instruction.\n";
        }
        if (iStatus & 1)
        {
            strStatusText += "  Bit 0: Pen down.\n";
        }

        return strStatusText;
    }

    String^ SerialWrapper::QueryFactors ()
    {
        String^ strResponse = QueryPlotter ("OF;");
        return strResponse;
    }

    String^ SerialWrapper::QueryFactorsText ()
    {
        String^ strResponse = QueryFactors ();
        String^ strFactorText = gcnew String ("Plotter factors: ");
        strFactorText += strResponse;
        strFactorText += "\n";

        int iCommaIdx = strResponse->IndexOf (',');
        if (iCommaIdx > 0 && iCommaIdx < strResponse->Length)
        {
            strFactorText += "  ";
            strFactorText += strResponse->Substring (0, iCommaIdx);
            strFactorText += " units/mm x-axis\n  ";
            strFactorText += Convert::ToString (Convert::ToInt16 (strResponse->Substring (iCommaIdx + 1)));
            strFactorText += " units/mm y-axis\n";
        }

        return strFactorText;
    }

    String^ SerialWrapper::QueryError ()
    {
        String^ strResponse = QueryPlotter ("OE;");
        return strResponse;
    }

    String^ SerialWrapper::QueryErrorText ()
    {
        String^ strResponse = QueryError ();
        String^ strErrorText = gcnew String ("Plotter error: ");
        //printf ("*** Error code returned from SerialWrapper::QueryError (): [%d] %s\n", strResponse->Length, strResponse);
        if (strResponse->Length == 0)
        {
            if (m_pobjSerialWriter->GetConsoleOutputTrace ())
                printf ("*** No error code returned from SerialWrapper::QueryError ()\n");
            return strResponse; // Plotter returned no error code
        }
        if (strResponse->IndexOf (',') > 0)
        {
            if (m_pobjSerialWriter->GetConsoleOutputTrace ())
                printf ("*** Bad error code returned from SerialWrapper::QueryError (): %s\n", strResponse);
            return strResponse; // Plotter returned bad error code
        }

        strErrorText += Convert::ToString (Convert::ToInt16 (strResponse));
        int iError = Convert::ToInt16 (strResponse);

        if (0 == iError)
        {
            strErrorText += "  No error\n";
        }
        else if (1 == iError)
        {
            strErrorText += "  Instruction not recognized\n";
        }
        else if (2 == iError)
        {
            strErrorText += "  Wrong number of parameters\n";
        }
        else if (3 == iError)
        {
            strErrorText += "  Out-of-range parameters\n";
        }
        else if (4 == iError)
        {
            strErrorText += "  <Not used>\n";
        }
        else if (5 == iError)
        {
            strErrorText += "  Unknown character set\n";
        }
        else if (6 == iError)
        {
            strErrorText += "  Position overflow\n";
        }
        else if (7 == iError)
        {
            strErrorText += "  <Not used>\n";
        }
        else if (8 == iError)
        {
            strErrorText += "  Vector received while pinch wheels raised\n";
        }

        return strErrorText;
    }

    String^ SerialWrapper::QueryActualPosition ()
    {
        String^ strResponse = QueryPlotter ("OA;");
        return strResponse;
    }

    String^ SerialWrapper::QueryActualPositionText ()
    {
        String^ strResponse = QueryActualPosition ();
        int iComma1 = strResponse->IndexOf (','),
            iComma2 = strResponse->IndexOf (',', iComma1 + 1);

        String^ strActualPositionText = gcnew String ("Plotter Actual Pen Position: ");
        strActualPositionText += strResponse;
        strActualPositionText += "\n";

        if (iComma1 > 0 && iComma2 > 0)
        {
            strActualPositionText += "  X: ";
            strActualPositionText += strResponse->Substring (0, iComma1);
            strActualPositionText += "  Y: ";
            strActualPositionText += strResponse->Substring (iComma1 + 1, iComma2 - iComma1 - 1);
            strActualPositionText += (strResponse[iComma2 + 1] == '0') ? "  Pen up" : "  Pen down";
        }

        return strActualPositionText;
    }

    String^ SerialWrapper::QueryCommandedPosition ()
    {
        String^ strResponse = QueryPlotter ("OC;");
        return strResponse;
    }

    String^ SerialWrapper::QueryCommandedPositionText ()
    {
        String^ strResponse = QueryCommandedPosition ();
        int iComma1 = strResponse->IndexOf (','),
            iComma2 = strResponse->IndexOf (',', iComma1 + 1);

        String^ strCommandedPositionText = gcnew String ("Plotter Commanded Pen Position: ");
        strCommandedPositionText += strResponse;
        strCommandedPositionText += "\n";

        if (iComma1 > 0 && iComma2 > 0)
        {
            strCommandedPositionText += "  X: ";
            strCommandedPositionText += strResponse->Substring (0, iComma1);
            strCommandedPositionText += "  Y: ";
            strCommandedPositionText += strResponse->Substring (iComma1 + 1, iComma2 - iComma1 - 1);
            strCommandedPositionText += (strResponse[iComma2 + 1] == '0') ? "  Pen up" : "  Pen down";
        }

        return strCommandedPositionText;
    }

    String^ SerialWrapper::QueryOptions ()
    {
        String^ strResponse = QueryPlotter ("OO;");
        return strResponse;
    }

    String^ SerialWrapper::QueryOptionsText ()
    {
        String^ strResponse = QueryOptions ();
        int iComma1 = strResponse->IndexOf (','),
            iComma2 = strResponse->IndexOf (',', iComma1 + 1),
            iComma3 = strResponse->IndexOf (',', iComma2 + 1),
            iComma4 = strResponse->IndexOf (',', iComma3 + 1),
            iComma5 = strResponse->IndexOf (',', iComma4 + 1),
            iComma6 = strResponse->IndexOf (',', iComma5 + 1),
            iComma7 = strResponse->IndexOf (',', iComma6 + 1);

        String^ strOptionsText = gcnew String ("Plotter Options: ");
        strOptionsText += strResponse;
        strOptionsText += "\n";

        if (iComma1 > 0 && iComma2 > 0 && iComma3 > 0 && iComma4 > 0 &&
            iComma5 > 0 && iComma6 > 0 && iComma7 >0)
        {
           strOptionsText += "  Option 1: ";
           strOptionsText += strResponse->Substring (0, iComma1);
           strOptionsText += "  <undefined>\n";

           strOptionsText += "  Option 2: ";
           strOptionsText += strResponse->Substring (iComma1 + 1, iComma2 - iComma1 - 1);
           strOptionsText += "  Pen select capability is included.\n";

           strOptionsText += "  Option 3: ";
           strOptionsText += strResponse->Substring (iComma2 + 1, iComma3 - iComma2 - 1);
           strOptionsText += "  <undefined>\n";

           strOptionsText += "  Option 4: ";
           strOptionsText += strResponse->Substring (iComma3 + 1, iComma4 - iComma3 - 1);
           strOptionsText += "  <undefined>\n";

           strOptionsText += "  Option 5: ";
           strOptionsText += strResponse->Substring (iComma4 + 1, iComma5 - iComma4 - 1);
           strOptionsText += "  Arcs and circle instructions are included.\n";

           strOptionsText += "  Option 6: ";
           strOptionsText += strResponse->Substring (iComma5 + 1, iComma6 - iComma5 - 1);
           strOptionsText += "  <undefined>\n";

           strOptionsText += "  Option 7: ";
           strOptionsText += strResponse->Substring (iComma6 + 1, iComma7 - iComma6 - 1);
           strOptionsText += "  <undefined>\n";

           strOptionsText += "  Option 8: ";
           strOptionsText += Convert::ToString (Convert::ToInt16 (strResponse->Substring (iComma7 + 1)));
           strOptionsText += "  <undefined>\n";
        }

        return strOptionsText;
    }

    String^ SerialWrapper::QueryHardClipLimits ()
    {
        String^ strResponse = QueryPlotter ("OH;");
        return strResponse;
    }

    String^ SerialWrapper::QueryHardClipLimitsText ()
    {
        String^ strResponse = QueryHardClipLimits ();
        int iComma1 = strResponse->IndexOf (','),
            iComma2 = strResponse->IndexOf (',', iComma1 + 1),
            iComma3 = strResponse->IndexOf (',', iComma2 + 1);
        String^ strHardClipLimitsText = gcnew String ("Plotter Hard Clip Limits: ");
        strHardClipLimitsText += strResponse;
        strHardClipLimitsText += "\n";

        if (iComma1 > 0 && iComma2 > 0 && iComma3 > 0)
        {
            strHardClipLimitsText += "  Lower Left X: ";
            strHardClipLimitsText += strResponse->Substring (0, iComma1);
            strHardClipLimitsText += "  Lower Left Y: ";
            strHardClipLimitsText += strResponse->Substring (iComma1 + 1, iComma2 - iComma1 - 1);
            strHardClipLimitsText += "\n  Upper Right X: ";
            strHardClipLimitsText += strResponse->Substring (iComma2 + 1, iComma3 - iComma2 - 1);
            strHardClipLimitsText += "  Upper Right Y: ";
            strHardClipLimitsText += Convert::ToString (Convert::ToInt16 (strResponse->Substring (iComma3 + 1)));
            strHardClipLimitsText += "\n";
        }

        return strHardClipLimitsText;
    }

    int SerialWrapper::QueryExtendedError ()
    {
        String^ strCommand = gcnew String ("\x1B.E");
        return QueryPlotterInt (strCommand);
    }

    String^ SerialWrapper::QueryExtendedErrorText ()
    {
        int iExtendedError = QueryExtendedError ();
        String^ strExtendedError = gcnew String ("");
        if (0 == iExtendedError)
        {
            strExtendedError += "Error 0: No I/O error has occurred.";
        }
        else if (10 == iExtendedError)
        {
            strExtendedError += "Error 10: ";
            strExtendedError += "Output instruction received while another output instruction is executing. The original ";
            strExtendedError += "instruction will continue normally; the one in error will be ignored.";
        }
        else if (11 == iExtendedError)
        {
            strExtendedError += "Error 11: ";
            strExtendedError += "Invalid byte received after first two characters, <ESC>., in a device control instruction.";
        }
        else if (12 == iExtendedError)
        {
            strExtendedError += "Error 12: ";
            strExtendedError += "Invalid byte received while parsing a device control instruction. The parameter ";
            strExtendedError += "containing the invalid byte and all following parameters are defaulted.";
        }
        else if (13 == iExtendedError)
        {
            strExtendedError += "Error 13: ";
            strExtendedError += "Parameter out of range.";
        }
        else if (14 == iExtendedError)
        {
            strExtendedError += "Error 14: ";
            strExtendedError += "Too many parameters received. Additional parameters beyond the proper number are ignored; parsing of the ";
            strExtendedError += "instruction ends when a colon (normal exit) or the first byte of another instruction is received (abnormal exit).";
        }
        else if (15 == iExtendedError)
        {
            strExtendedError += "Error 15: ";
            strExtendedError += "A framing error, parity error, or overrun error has been detected.";
        }
        else if (16 == iExtendedError)
        {
            strExtendedError += "Error 16: ";
            strExtendedError += "The input buffer has overflowed. As a result, one or more bytes of data have been lost, and ";
            strExtendedError += "therefore an HP-GL error will probably occur.";
        }
        else if (-1 == iExtendedError)
        {
            strExtendedError += "Error -1: ";
            strExtendedError += "No string returned from plotter.  Plotter is most likely in an error condition.  ESC.E should clear it.";
        }
        else
        {
            strExtendedError += "Undefined error: ";
            strExtendedError += Convert::ToString (iExtendedError);
        }

        return strExtendedError;
    }

    int SerialWrapper::QueryExtendedStatus ()
    {
        String^ strCommand = gcnew String ("\x1B.O");
        return QueryPlotterInt (strCommand);
    }

    String^ SerialWrapper::QueryExtendedStatusText ()
    {
        int iExtendedStatus = QueryExtendedStatus ();
        String^ strExtendedStatus = gcnew String ("");
        if (0 == iExtendedStatus)
        {
            strExtendedStatus += "Status: ";
            strExtendedStatus += "Buffer is not empty and plotter is processing HP-GL instructions.";
        }
        else if (8 == iExtendedStatus)
        {
            strExtendedStatus += "Status: ";
            strExtendedStatus += "Buffer is empty and is ready to process or is processing HP-GL instructions.";
        }
        else if (16 == iExtendedStatus)
        {
            strExtendedStatus += "Status: ";
            strExtendedStatus += "Buffer is not empty and VIEW has been pressed.";
        }
        else if (24 == iExtendedStatus)
        {
            strExtendedStatus += "Status: ";
            strExtendedStatus += "Buffer is empty and VIEW has been pressed.";
        }
        else if (32 == iExtendedStatus)
        {
            strExtendedStatus += "Status: ";
            strExtendedStatus += "Buffer is not empty and paper lever and pinch wheels are raised.";
        }
        else if (40 == iExtendedStatus)
        {
            strExtendedStatus += "Status: ";
            strExtendedStatus += "Buffer is empty and paper lever and pinch wheels are raised.";
        }
        else
        {
            strExtendedStatus += "Undefined status: ";
            strExtendedStatus += Convert::ToString (iExtendedStatus);
        }

        return strExtendedStatus;
    }

    String^ SerialWrapper::QueryOutputWindow ()
    {
        String^ strResponse = QueryPlotter ("OW;");
        return strResponse;
    }

    String^ SerialWrapper::QueryOutputWindowText ()
    {
        String^ strResponse = QueryOutputWindow ();
        int iComma1 = strResponse->IndexOf (','),
            iComma2 = strResponse->IndexOf (',', iComma1 + 1),
            iComma3 = strResponse->IndexOf (',', iComma2 + 1);
        String^ strOutputWindowText = gcnew String ("Plotter Output Window: ");
        strOutputWindowText += strResponse;
        strOutputWindowText += "\n";

        if (iComma1 > 0 && iComma2 > 0 && iComma3 > 0)
        {
            strOutputWindowText += "  Lower Left X: ";
            strOutputWindowText += strResponse->Substring (0, iComma1);
            strOutputWindowText += "  Lower Left Y: ";
            strOutputWindowText += strResponse->Substring (iComma1 + 1, iComma2 - iComma1 - 1);
            strOutputWindowText += "\n  Upper Right X: ";
            strOutputWindowText += strResponse->Substring (iComma2 + 1, iComma3 - iComma2 - 1);
            strOutputWindowText += "  Upper Right Y: ";
            strOutputWindowText += Convert::ToString (Convert::ToInt16 (strResponse->Substring (iComma3 + 1)));
            strOutputWindowText += "\n";
        }

        return strOutputWindowText;
    }

    bool SerialWrapper::WaitForPlotter (int iTimeOutSeconds)
    {
        return WaitForPlotter (iTimeOutSeconds, m_pobjSerialWriter->GetConsoleOutputTrace ());
    }

    bool SerialWrapper::WaitForPlotter (int iTimeOutSeconds, bool bBufferSpaceTrace)
    {
        int iStartCount = Environment::TickCount & Int32::MaxValue,
            iEndCount = iStartCount + (iTimeOutSeconds * 1000);

        int iPlotterBusyStatus = m_pobjSerialWriter->IsPlotterBusy (bBufferSpaceTrace);
        while (SERIAL_PLOTTER_IS_BUSY == iPlotterBusyStatus)
        {
            int iTickCount = Environment::TickCount & Int32::MaxValue;
            if (iTickCount > iEndCount)
                return false;

            iPlotterBusyStatus = m_pobjSerialWriter->IsPlotterBusy (bBufferSpaceTrace);
        }
        if (SERIAL_PLOTTER_IS_IDLE != iPlotterBusyStatus)
            ThrowException (iPlotterBusyStatus);

        return true;
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
                case WrapperBase::SERIAL_INVALID_BAUD_RATE:
                {
                    StringBuilder strbldError ("Seral: Invalid baud rate: must be 9600, 4800, 2400, 1200, 600, 300, or 150. (error code 1)");
                    if (iBaud > 0)
                    {
                        strbldError.Append ("  Got: ");
                        strbldError.Append (iBaud.ToString ());
                    }
                    Exception^ e = gcnew Exception (strbldError.ToString ());
                    throw e;
                }

                case WrapperBase::SERIAL_NO_SERIAL_PORT_AVAILABLE:
                {
                    Exception^ e = gcnew Exception ("Seral: No serial port available in OpenComPort (error code 2)");
                    throw e;
                }

                case WrapperBase::SERIAL_UNABLE_TO_OPEN_COM_PORT:
                {
                    Exception^ e = gcnew Exception ("Seral: Unable to open named port in OpenComPort (error code 3)");
                    throw e;
                }

                // SerialWriter::CloseOutputPort
                case WrapperBase::SERIAL_CLOSEHANDLE_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: CloseHandle failed in CloseOutputPort (error code 4)");
                    throw e;
                }

                // SerialWriter::WriteTextString
                case WrapperBase::SERIAL_NO_OUTPUT_PORT_FOUND:
                {
                    Exception^ e = gcnew Exception ("Serial: No output port found in WriteTextString (error code 5)");
                    throw e;
                }
                case WrapperBase::SERIAL_NO_OUTPUT_TEXT_PROVIDED:
                {
                    Exception^ e = gcnew Exception ("Serial: No output text provided in WriteTextString (error code 6)");
                    throw e;
                }
                case WrapperBase::SERIAL_WRITEFILE_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: WriteFile failed in WriteTextString (error code 7)");
                    throw e;
                }

                // SerialWriter::OpenNamedPort
                case WrapperBase::SERIAL_CLOSEOUTPUTPORT_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: CloseOutputPort failed in OpenNamedPort (error code 8)");
                    throw e;
                }
                case WrapperBase::SERIAL_UNABLE_TO_OPEN_NAMED_PORT:
                {
                    Exception^ e = gcnew Exception ("Serial: Unable to open named port in OpenNamedPort (error code 9)");
                    throw e;
                }

                // SerialWriter::SetComPortSettings
                case WrapperBase::SERIAL_GETCOMMSTATE_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: GetCommState failed in SetComPortSettings (error code 10)");
                    throw e;
                }
                case WrapperBase::SERIAL_SETCOMMSTATE_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: SetCommState failed in SetComPortSettings (error code 11)");
                    throw e;
                }
                case WrapperBase::SERIAL_SETCOMMTIMEOUTS_FAILED:
                {
                    Exception^ e = gcnew Exception ("Serial: SetCommTimeouts failed in SetComPortSettings (error code 12)");
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
            return Constants::SERIAL_INVALID_BAUD_RATE;
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

        //string str = HelperMethodsNative::GetPlotterPrinterName ();
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
            return Constants::SERIAL_NO_SERIAL_PORT_AVAILABLE;
        }
   
        OutputDebugText ("OK\n");
        m_iTotalBufferSize = GetPlotterBufferSize (true);
        if (-1 == m_iTotalBufferSize)
        {
            fprintf (stderr, "Unable to open named port in OpenComPort\n");
            throw exception ("Unable to open named port in OpenComPort");

            return Constants::SERIAL_UNABLE_TO_OPEN_COM_PORT;
        }
            
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
            // Successfully opened port
            m_strDevName = szBuffer;
            fprintf (stderr, "Using COM port: %s\n", m_strDevName.c_str ());
            return SetComPortSettings ();
        }
        else
        {
            // Port not opened
            m_hSerial = 0;
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
        int iSpace = GetPlotterBufferSpace ();
        if (m_bConsoleOutputTrace)
            printf ("Entering WriteTextString(); buffer space: %d\n", iSpace);
        string strBuffer (szBuffer);
        int iStart           = 0,
            iNextSep         = 0,
            iBytesSent       = 0,
            iBufferSpace     = 0,
            iMaxStringLength = MAX_PATH - 1;
        string strToSend;

        if (m_bConsoleOutputTrace)
            printf ("1: strBuffer.length: %d\n", strBuffer.length ());
        if ((int)(strBuffer.length () + 1) < GetPlotterBufferSpace ())
        {
            // No need to fragment short strings.
            return WriteData (szBuffer);
        }

        // String too big for buffer
        while (iNextSep < (int)(strBuffer.length () - 1))
        {
            if ((int)strBuffer.length () - iNextSep > iMaxStringLength)
            {
                // Remaining string too long for plotter buffer; break it down
                iNextSep = strBuffer.rfind (';', iStart + iMaxStringLength - BUFFER_HEADROOM);
                if (iNextSep == string::npos)
                {
                    iNextSep = iMaxStringLength;
                }
                if (m_bConsoleOutputTrace)
                    printf ("2: iNextSep: %d\n", iNextSep);

                strToSend = strBuffer.substr (iStart, iNextSep - iStart + 1);
                if (m_bConsoleOutputTrace)
                    printf ("3: First substring length: %d\n", strToSend.length ());
                iStart = iNextSep + 1;
                if (m_bConsoleOutputTrace)
                {
                    printf ("4: iStart: %d\n", iStart);
                    printf ("5: iMaxStringLength was %d, now is %d\n", iMaxStringLength, TEXT_BLOCK_SIZE);
                }
                iMaxStringLength = TEXT_BLOCK_SIZE;
            }
            else
            {
                strToSend = strBuffer.substr (iStart);
                iNextSep = strBuffer.length ();
                if (m_bConsoleOutputTrace)
                    printf ("6: iNextSep: %d\n", iNextSep);
            }

            // Wait for space in plotter buffer
            iBufferSpace = GetPlotterBufferSpace ();
            if (m_bConsoleOutputTrace)
                printf ("7: Buffer space 1: %d\n", iBufferSpace);
            int iLastBufferSpace = iBufferSpace;
            int iStartTick = ::GetTickCount ();
            while (iBufferSpace < iMaxStringLength + 1)
            {
                Sleep (100);
                iBufferSpace = GetPlotterBufferSpace ();
                //printf ("8: Buffer space: %d\n", iBufferSpace);
                int iElapsed = ::GetTickCount () - iStartTick;
                if (iElapsed > WAIT_TIME_LIMIT_MS)
                {
                    if (m_bConsoleOutputTrace)
                        printf ("Elapsed time %d ms exceeds limit: %d ms\n", iElapsed, WAIT_TIME_LIMIT_MS);
                    return -1;
                }

                if (iBufferSpace > iLastBufferSpace)
                {
                    if (m_bConsoleOutputTrace)
                        printf ("9: Buffer space 2: %d (%d ms)\n", iBufferSpace, ::GetTickCount () - iStartTick);
                    iLastBufferSpace = iBufferSpace;
                    iStartTick = ::GetTickCount ();
                }
            }

            if (m_bConsoleOutputTrace)
                printf (">> WriteData %s\n\n", strToSend.c_str ());
            int iStatus = WriteData (strToSend.c_str ());
            if (iStatus != 0)
                return iStatus;
        }

        return 0;
    }

    int SerialWriter::WriteData (const char* szBuffer)
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
            return Constants::SERIAL_NO_OUTPUT_PORT_FOUND; // Serial: No output port found in WriteTextString (error code 4)
        }

        size_t iIdx1 = 0,
               iIdx2 = 0;
        char szTextToSend[MAX_PATH];
        char szDigits[MAX_PATH];
        const char* buffer = szBuffer;
        // Check that some text to send was provided
        if (strlen (buffer) == 0)
        {
            return Constants::SERIAL_NO_OUTPUT_TEXT_PROVIDED; // Serial: No output text provided in WriteTextString (error code 5)
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
                return Constants::SERIAL_WRITEFILE_FAILED; // Serial: WriteFile failed in WriteTextString (error code 6)
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

    string SerialWriter::ReadData (bool bShowEmptyBuffer)
    {
        char szBuffer[BUFFER_SIZE];
        ::ZeroMemory (szBuffer, BUFFER_SIZE);
        int iBytesRead = ReadData ((void*) szBuffer, BUFFER_SIZE - 1);
        if (iBytesRead == 0)
        {
            OutputDebugText ("<empty buffer> in SerialWriter::ReadData ()\n");
            if (bShowEmptyBuffer)
                puts ("<empty buffer>");
            szBuffer[0] = '-';
            szBuffer[1] = '1';
            szBuffer[2] = '\0';
        }
        return szBuffer;
    }

    int SerialWriter::ReadData (void* vBbuffer, int iLimit)
    {
	    if (m_hSerial == NULL)
            return 0 ;

	    DWORD   dwBytesRead  = 0,
                dwErrorFlags = 0;
	    COMSTAT comstat;

        ::ZeroMemory ((void*)&comstat, sizeof (COMSTAT));
	    ClearCommError (m_hSerial, &dwErrorFlags, &comstat);

        int iRepeat = 100; // For a maximum of 1 second, a long time for some input to show up
        int iLastInQue = 0;
        while (iRepeat--)
        {
            //printf ("0x%04x, %01d, %01d, %01d, %01d, %01d, %01d, %01d, bytes: %d / %d\n",
            //        dwErrorFlags,
            //        comstat.fCtsHold,
            //        comstat.fDsrHold,
            //        comstat.fRlsdHold,
            //        comstat.fXoffHold,
            //        comstat.fXoffSent,
            //        comstat.fEof,
            //        comstat.fTxim,
            //        comstat.cbInQue,
            //        comstat.cbOutQue);
            iLastInQue = comstat.cbInQue;
	        ClearCommError (m_hSerial, &dwErrorFlags, &comstat);
            if (comstat.cbInQue > 0 &&
                comstat.cbInQue == iLastInQue)
            {
                break;
            }
            Sleep (10);
        }

	    dwBytesRead = (DWORD) comstat.cbInQue;
	    if (iLimit < (int) dwBytesRead)
            dwBytesRead = (DWORD) iLimit;

	    ReadFile (m_hSerial, vBbuffer, dwBytesRead, &dwBytesRead, NULL);

	    return (int)dwBytesRead;
    }

    int SerialWriter::GetPlotterBufferSize (bool bShowConsoleTrace)
    {
        if (m_iTotalBufferSize == 0)
        {
            int iStatus = WriteData ("\x1B.L"); // Request total buffer space
            if (iStatus > 0)
            {
                return iStatus;
            }

            m_iTotalBufferSize = atoi (ReadData (bShowConsoleTrace).c_str ());
        }

        return m_iTotalBufferSize;
    }

    int SerialWriter::GetPlotterBufferSpace ()
    {
        int iStatus = WriteData ("\x1B.B"); // Get available buffer space
        if (iStatus > 0)
        {
            return iStatus;
        }

        string strRead = ReadData (m_bConsoleOutputTrace);
        return atoi (strRead.c_str ());
    }

    int SerialWriter::IsPlotterBusy (bool bBufferSpaceTrace)
    {
        static int s_iLastBufferSpace;

        GetPlotterBufferSize (m_bConsoleOutputTrace);

        int iAvailableBufferSize = GetPlotterBufferSpace ();
        if (bBufferSpaceTrace)
        {
            if (s_iLastBufferSpace != iAvailableBufferSize)
                printf ("%d\n", iAvailableBufferSize);
            s_iLastBufferSpace = iAvailableBufferSize;
        }

        return (m_iTotalBufferSize > iAvailableBufferSize) ? Constants::SERIAL_PLOTTER_IS_BUSY : Constants::SERIAL_PLOTTER_IS_IDLE;
    }

    SerialWriter::SerialWriter ()
    {
        m_bHexInput           = false;
        m_bOutputTrace        = false;
        m_bConsoleOutputTrace = false;
        m_iBaudRate           = 0;
        m_iDeviceNumber       = 0;
        m_iCloseDelay         = 0;
        m_iTotalBufferSize    = 0;
        m_hSerial             = 0;
        OpenComPort (BAUD9600, 50, 0, false);
    }

    SerialWriter::SerialWriter (int iBaudRate, int iDeviceNumber, int iCloseDelay, bool bHexInput)
    {
        m_bHexInput           = false;
        m_bOutputTrace        = false;
        m_bConsoleOutputTrace = false;
        m_iBaudRate           = 0;
        m_iDeviceNumber       = 0;
        m_iCloseDelay         = 0;
        m_iTotalBufferSize    = 0;
        m_hSerial             = 0;
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
