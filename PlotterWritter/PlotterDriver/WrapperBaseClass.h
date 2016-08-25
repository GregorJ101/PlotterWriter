// WrapperBaseClass.h

#pragma once

using namespace System;

namespace Constants
{
    //#define ESCAPE 0x1B
    const char ESCAPE                          = 0x1B;

    const bool SERIAL                          = true;
    const bool PARALLEL                        = false;

    // SerialWriter
    const int SERIAL_PLOTTER_IS_BUSY           = -1;
    const int SERIAL_PLOTTER_IS_IDLE           = 0;
    const int SERIAL_INVALID_BAUD_RATE         = 1;
    const int SERIAL_NO_SERIAL_PORT_AVAILABLE  = 2;
    const int SERIAL_UNABLE_TO_OPEN_COM_PORT   = 3;
    const int SERIAL_CLOSEHANDLE_FAILED        = 4;
    const int SERIAL_NO_OUTPUT_PORT_FOUND      = 5;
    const int SERIAL_NO_OUTPUT_TEXT_PROVIDED   = 6;
    const int SERIAL_WRITEFILE_FAILED          = 7;
    const int SERIAL_CLOSEOUTPUTPORT_FAILED    = 8;
    const int SERIAL_UNABLE_TO_OPEN_NAMED_PORT = 9;
    const int SERIAL_GETCOMMSTATE_FAILED       = 10;
    const int SERIAL_SETCOMMSTATE_FAILED       = 11;
    const int SERIAL_SETCOMMTIMEOUTS_FAILED    = 12;

    // ParallelWriter
    const int PARALLEL_NO_OUTPUT_PORT_FOUND    = 1;
    const int PARALLEL_OPENPRINTER_FAILED      = 2;
    const int PARALLEL_STARTDOCPRINTER_FAILED  = 3;
    const int PARALLEL_WRITEPRINTER_FAILED     = 4;
    const int PARALLEL_ENDDOCPRINTER_FAILED    = 5;
}

namespace WrapperBaseClass
{
    public ref class WrapperBase abstract
	{
        public:
            virtual String^ WriteTextString (String^ systrBuffer) abstract;
            virtual String^ QueryPlotter (String^ systrBuffer) abstract;
            virtual int QueryPlotterInt (String^ systrBuffer) abstract;
            virtual String^ QueryIdentification () abstract;
            virtual String^ QueryDigitizedPoint () abstract;
            virtual String^ QueryStatus () abstract;
            virtual String^ QueryStatusText () abstract;
            virtual String^ QueryFactors () abstract;
            virtual String^ QueryFactorsText () abstract;
            virtual String^ QueryError () abstract;
            virtual String^ QueryErrorText () abstract;
            virtual String^ QueryActualPosition () abstract;
            virtual String^ QueryActualPositionText () abstract;
            virtual String^ QueryCommandedPosition () abstract;
            virtual String^ QueryCommandedPositionText () abstract;
            virtual String^ QueryOptions () abstract;
            virtual String^ QueryOptionsText () abstract;
            virtual String^ QueryHardClipLimits () abstract;
            virtual String^ QueryHardClipLimitsText () abstract;
            virtual int QueryExtendedError () abstract;
            virtual String^ QueryExtendedErrorText () abstract;
            virtual int QueryExtendedStatus () abstract;
            virtual String^ QueryExtendedStatusText () abstract;
            virtual String^ QueryOutputWindow () abstract;
            virtual String^ QueryOutputWindowText () abstract;
            virtual int GetPlotterBufferSize () abstract;
            virtual int GetPlotterBufferSpace () abstract;
            virtual bool IsPlotterBusy (bool bBufferSpaceTrace) abstract;
            virtual bool WaitForPlotter (int iTimeOutSeconds) abstract;
            virtual bool WaitForPlotter (int iTimeOutSeconds, bool bBufferSpaceTrace) abstract;
            virtual bool CloseOutputPort () abstract;
            virtual String^ GetPortName () abstract;
            virtual bool IsSerial () abstract;

            // SerialWrapper
            literal int SERIAL_PLOTTER_IS_BUSY           = Constants::SERIAL_PLOTTER_IS_BUSY;
            literal int SERIAL_PLOTTER_IS_IDLE           = Constants::SERIAL_PLOTTER_IS_IDLE;
            literal int SERIAL_INVALID_BAUD_RATE         = Constants::SERIAL_INVALID_BAUD_RATE;
            literal int SERIAL_NO_SERIAL_PORT_AVAILABLE  = Constants::SERIAL_NO_SERIAL_PORT_AVAILABLE;
            literal int SERIAL_UNABLE_TO_OPEN_COM_PORT   = Constants::SERIAL_UNABLE_TO_OPEN_COM_PORT;
            literal int SERIAL_CLOSEHANDLE_FAILED        = Constants::SERIAL_CLOSEHANDLE_FAILED;
            literal int SERIAL_NO_OUTPUT_PORT_FOUND      = Constants::SERIAL_NO_OUTPUT_PORT_FOUND;
            literal int SERIAL_NO_OUTPUT_TEXT_PROVIDED   = Constants::SERIAL_NO_OUTPUT_TEXT_PROVIDED;
            literal int SERIAL_WRITEFILE_FAILED          = Constants::SERIAL_WRITEFILE_FAILED; 
            literal int SERIAL_CLOSEOUTPUTPORT_FAILED    = Constants::SERIAL_CLOSEOUTPUTPORT_FAILED;
            literal int SERIAL_UNABLE_TO_OPEN_NAMED_PORT = Constants::SERIAL_UNABLE_TO_OPEN_NAMED_PORT;
            literal int SERIAL_GETCOMMSTATE_FAILED       = Constants::SERIAL_GETCOMMSTATE_FAILED;
            literal int SERIAL_SETCOMMSTATE_FAILED       = Constants::SERIAL_SETCOMMSTATE_FAILED;
            literal int SERIAL_SETCOMMTIMEOUTS_FAILED    = Constants::SERIAL_SETCOMMTIMEOUTS_FAILED;

            // ParallelWrapper
            literal int PARALLEL_NO_OUTPUT_PORT_FOUND    = Constants::PARALLEL_NO_OUTPUT_PORT_FOUND;
            literal int PARALLEL_OPENPRINTER_FAILED      = Constants::PARALLEL_OPENPRINTER_FAILED;
            literal int PARALLEL_STARTDOCPRINTER_FAILED  = Constants::PARALLEL_STARTDOCPRINTER_FAILED;
            literal int PARALLEL_WRITEPRINTER_FAILED     = Constants::PARALLEL_WRITEPRINTER_FAILED;
            literal int PARALLEL_ENDDOCPRINTER_FAILED    = Constants::PARALLEL_ENDDOCPRINTER_FAILED;
    };
}
