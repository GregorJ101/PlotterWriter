// This is the main DLL file.

#include "stdafx.h"

#include "ParallelPortWriter.h"


namespace ParallelPortWriter
{
    ///
    ///  class ParallelWrapper
    ///
    String^ ParallelWrapper::WriteTextString (String^ systrBuffer)
    {
        string strBuffer (HelperMethodsCLI::ToStdString (systrBuffer));
        int iStatus = m_objParallelWriter->WriteTextString (strBuffer);

        if (iStatus > 0)
        {
            switch (iStatus)
            {
                case 1:
                {
                    Exception^ e = gcnew Exception ("Parallel: No output port found (error code 1)");
                    throw e;
                }
                case 2:
                {
                    Exception^ e = gcnew Exception ("Parallel: OpenPrinter failed (error code 2)");
                    throw e;
                }
                case 3:
                {
                    Exception^ e = gcnew Exception ("Parallel: StartDocPrinter failed (error code 3)");
                    throw e;
                }
                case 4:
                {
                    Exception^ e = gcnew Exception ("Parallel: WritePrinter failed (error code 4)");
                    throw e;
                }
                case 5:
                {
                    Exception^ e = gcnew Exception ("Parallel: EndDocPrinter failed (error code 5)");
                    throw e;
                }
            }
        }

        return GetPortName ();
    }

    String^ ParallelWrapper::WriteTextString (String^ systrPrinterName, String^ systrBuffer)
    {
        string strPrinterName (HelperMethodsCLI::ToStdString (systrPrinterName));
        string strBuffer (HelperMethodsCLI::ToStdString (systrBuffer));
        int iStatus =  m_objParallelWriter->WriteTextString (strPrinterName.c_str (), strBuffer);

        return GetPortName ();
    }

    ParallelWrapper::ParallelWrapper ()
    {
        m_objParallelWriter = new ParallelWriter ();
    }

    ParallelWrapper::!ParallelWrapper ()
    {
        delete m_objParallelWriter;
    }

    ParallelWrapper::~ParallelWrapper ()
    {
        this->!ParallelWrapper ();
    }

    ///
    ///  class ParallelWriter
    ///
    int ParallelWriter::WriteTextString (const string& strText)
    {
        if (m_strPlotterPrinterName.empty ())
        {
            GetPlotterPrinterName ();
        }

        if (m_strPlotterPrinterName.empty ())
        {
            return Constants::PARALLEL_NO_OUTPUT_PORT_FOUND; // Parallel: No output port found
        }

        return WriteTextString (m_strPlotterPrinterName.c_str (), strText);
    }

    int ParallelWriter::WriteTextString (const char* szPrinterName, const string& strText)
    {
        HANDLE     hPrinter;
        DOC_INFO_1 DocInfo;
        DWORD      dwJob;
        DWORD      dwBytesWritten;

        // Need a handle to the printer.
        wstring wstr = HelperMethodsNative::ConvertStringUTF8toUTF16 ((LPSTR)m_strPlotterPrinterName.c_str ());
        LPWSTR lstr = const_cast<LPWSTR>(wstr.c_str());
        if (!OpenPrinter (lstr, &hPrinter, NULL))
            return Constants::PARALLEL_OPENPRINTER_FAILED; // Parallel: OpenPrinter failed

        // Fill in the structure with info about this "document."
        DocInfo.pDocName    = L"ParallelPortWriter";
        DocInfo.pOutputFile = NULL;
        DocInfo.pDatatype   = L"RAW";

        // Inform the spooler the document is beginning.
        if ((dwJob = StartDocPrinter (hPrinter, 1, (LPBYTE)&DocInfo)) == 0)
        {
            ClosePrinter (hPrinter);
            return Constants::PARALLEL_STARTDOCPRINTER_FAILED; // Parallel: StartDocPrinter failed
        }

        // Send the data to the printer.
        if (!WritePrinter (hPrinter, (LPVOID)strText.c_str (), strText.length (), &dwBytesWritten))
        {
            EndPagePrinter (hPrinter);
            EndDocPrinter (hPrinter);
            ClosePrinter (hPrinter);
            return Constants::PARALLEL_WRITEPRINTER_FAILED; // Parallel: WritePrinter failed
        }

        // Inform the spooler that the document is ending.
        if (!EndDocPrinter (hPrinter))
        {
            ClosePrinter (hPrinter);
            return Constants::PARALLEL_ENDDOCPRINTER_FAILED; // Parallel: EndDocPrinter failed
        }

        // Tidy up the printer handle.
        ClosePrinter (hPrinter);

        // Check to see if correct number of bytes were written.
        return (dwBytesWritten == strText.length ()) ? 0 : 5;
    }

    ParallelWriter::ParallelWriter ()
    {
        GetPlotterPrinterName ();
    }

    void ParallelWriter::GetPlotterPrinterName ()
    {
        PRINTER_INFO_2*  pPrtInfo2;
        DWORD            dwPrtInfroCount = 0;
        DWORD            dwPrtInfroSize = 0;
        DWORD            dwPrtInfoLevel = 2;

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
                        m_strPlotterPrinterName = strPrinterName;
                        fprintf (stderr, "Using printer %s on port %s\n", m_strPlotterPrinterName.c_str (), strPortName.c_str ());
                    }
                }
            }
        }

        if (m_strPlotterPrinterName.empty ())
        {
            fprintf (stderr, "No printer port found!");
        }
    }
}
