// WrapperBaseClass.h

#pragma once

using namespace System;

namespace Constants
{
    // SerialWriter
    const int SERAL_INVALID_BAUD_RATE          = 1;
    const int SERAL_NO_SERIAL_PORT_AVAILABLE   = 2;
    const int SERIAL_CLOSEHANDLE_FAILED        = 3;
    const int SERIAL_NO_OUTPUT_PORT_FOUND      = 4;
    const int SERIAL_NO_OUTPUT_TEXT_PROVIDED   = 5;
    const int SERIAL_WRITEFILE_FAILED          = 6;
    const int SERIAL_CLOSEOUTPUTPORT_FAILED    = 7;
    const int SERIAL_UNABLE_TO_OPEN_NAMED_PORT = 8;
    const int SERIAL_GETCOMMSTATE_FAILED       = 9;
    const int SERIAL_SETCOMMSTATE_FAILED       = 10;
    const int SERIAL_SETCOMMTIMEOUTS_FAILED    = 11;

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
            virtual String^ GetPortName () abstract;

            // SerialWrapper
            literal int SERAL_INVALID_BAUD_RATE          = Constants::SERAL_INVALID_BAUD_RATE;
            literal int SERAL_NO_SERIAL_PORT_AVAILABLE   = Constants::SERAL_NO_SERIAL_PORT_AVAILABLE;
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
