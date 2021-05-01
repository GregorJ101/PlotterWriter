using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using HPGL;
using PlotterEngine;

namespace PlotterDriver
{
    public class CSerialPortDriver
    {
        #region Member Data
        public enum EClosePortMethod
        {
            EClosePortOnly,
            EAbortAndClose,
            EWaitForPlotterAndClose,
            EWaitForQueueAndClose
        };

        private const int MAX_PATH           = 260;
        private const int MAX_SEND_LENGTH    = 760;
        private const int BUFFER_SIZE        = 5120;
        private const int BUFFER_HEADROOM    = 20;
        private const int TEXT_BLOCK_SIZE    = 40;
        private const int WAIT_TIME_LIMIT_MS = 30000;

        public const int BAUD9600 = 9600;
        public const int BAUD4800 = 4800;
        public const int BAUD2400 = 2400;
        public const int BAUD1200 = 1200;
        public const int BAUD600  = 600;
        public const int BAUD300  = 300;
        public const int BAUD150  = 150;

        public const int SERIAL_PLOTTER_IS_BUSY           = -1;
        public const int SERIAL_PLOTTER_IS_IDLE           = 0;
        public const int SERIAL_PORT_OPENED               = 0;
        public const int SERIAL_OPERATION_SUCCESS         = 0;
        public const int SERIAL_INVALID_BAUD_RATE         = 1;
        public const int SERIAL_NO_SERIAL_PORT_AVAILABLE  = 2;
        public const int SERIAL_UNABLE_TO_OPEN_COM_PORT   = 3;
        public const int SERIAL_CLOSEHANDLE_FAILED        = 4;
        public const int SERIAL_NO_OUTPUT_PORT_FOUND      = 5;
        public const int SERIAL_NO_OUTPUT_TEXT_PROVIDED   = 6;
        public const int SERIAL_WRITEFILE_FAILED          = 7;
        public const int SERIAL_CLOSEOUTPUTPORT_FAILED    = 8;
        public const int SERIAL_UNABLE_TO_OPEN_NAMED_PORT = 9;
        public const int SERIAL_GETCOMMSTATE_FAILED       = 10;
        public const int SERIAL_SETCOMMSTATE_FAILED       = 11;
        public const int SERIAL_SETCOMMTIMEOUTS_FAILED    = 12;
        public const int SERIAL_SIMULATED_OUTPUT          = 13;

        private const int EMPTY_READ_DATA_STRING          = -1;
        private const int PLOTTER_BUSY_ERROR_CODE         = -2;

        private const string STRING_PLOTTER_ID       = "7475A";

        private bool m_bOutputTrace         = false;
        private bool m_bAbortPlot           = false;
        private bool m_bPauseAfterNewPen    = false;
        private int  m_iBaudRate            = 0;
        private int  m_iPlotterBufferSize   = -1;
        private int  m_iLastBufferSpace     = -1;

        private string m_strDevName         = "";

        private SerialPort m_objSerialPort = null;

        // For threading
        private SortedDictionary<string, string> m_sdOutputQueue            = new SortedDictionary<string, string> ();
        private int          m_iQueueSize               = 0;
        private bool         m_bKeepOutputThreadRunning = true;
        public  Object       m_objLock                  = new Object ();
        #endregion

        public CSerialPortDriver (bool bOutputTrace)
        {
            m_bOutputTrace         = bOutputTrace;
            m_iBaudRate            = 0;
            m_iPlotterBufferSize   = 0;

            if (OpenComPort (BAUD9600) != SERIAL_PORT_OPENED)
            {
                ConsoleOutput ("Unable to open port in OpenComPort", true);
                CloseOutputPort ();
                throw new Exception ("Unable to open port in OpenComPort");
            }

            m_bKeepOutputThreadRunning = true;
            StartWriteThreadAsync ();
        }

        public CSerialPortDriver (int iBaudRate, bool bOutputTrace = false)
        {
            m_bOutputTrace         = bOutputTrace;
            m_iBaudRate            = 0;
            m_iPlotterBufferSize   = 0;

            if (OpenComPort (iBaudRate) != SERIAL_PORT_OPENED)
            {
                ConsoleOutput ("Unable to open port in OpenComPort", true);
                CloseOutputPort ();
                throw new Exception ("Unable to open port in OpenComPort");
            }

            m_bKeepOutputThreadRunning = true;
            StartWriteThreadAsync ();
        }

        ~CSerialPortDriver ()
        {
            CloseOutputPort ();
        }

        public bool WriteTextString (string strBuffer, string strDocName)
        {
            if (m_objSerialPort != null &&
                m_objSerialPort.IsOpen  &&
                strBuffer       != null &&
                strBuffer.Length > 0)
            {
                //ConsoleOutput ("CSerialPortDriver.WriteTextString: " + strDocName + "  " + CGenericMethods.ExtractCommentHPGL (strBuffer));
                ConsoleOutput ("In WriteTextString (..., " + strDocName + ')');
                ConsoleOutput ("m_bAbortPlot: " + m_bAbortPlot.ToString () + " in WriteTextString");
                lock (m_objLock)
                {
                    m_sdOutputQueue.Add (strDocName, strBuffer);
                    m_iQueueSize   += +strBuffer.Length;
                }
                ConsoleOutput ("  m_iQueueSize in WriteTextString (" + strDocName + "): " + m_iQueueSize.ToString ());

                return true;
            }

            return false;
        }

        public void SetPauseAfterNewPen (bool bPauseAfterNewPen)
        {
            m_bPauseAfterNewPen = bPauseAfterNewPen;
        }

        public bool GetPauseAfterNewPen ()
        {
            return m_bPauseAfterNewPen;
        }

        public void SetOutputTrace (bool bOutputTrace)
        {
            m_bOutputTrace = bOutputTrace;
        }

        public bool GetOutputTrace ()
        {
            return m_bOutputTrace;
        }

        public string GetPortName ()
        {
            return m_strDevName;
        }

        public void ShowOutputQueue ()
        {
            lock (m_objLock)
            {
                int iIndexWidth = (m_sdOutputQueue.Count > 999) ? 4 :
                                  (m_sdOutputQueue.Count > 99) ? 3 :
                                  (m_sdOutputQueue.Count > 9) ? 2 : 1;
                int iIdx = 0;

                foreach (KeyValuePair<string, string> kvp in m_sdOutputQueue)
                {
                    Console.WriteLine ('[' + CGenericMethods.FillStringLength ((iIdx++).ToString (), iIndexWidth, ' ') + "] " +
                                       kvp.Key + "  " + ((kvp.Value.Length > 40) ? kvp.Value.Substring (0, 40) : kvp.Value));
                }
            }
        }

        public string GetHPGLString (int iIdx, int iMaxLength = 0)
        {
            if (m_sdOutputQueue.Count > iIdx)
            {
                string strHPGL = m_sdOutputQueue.ElementAt (iIdx).Value;

                strHPGL = (iMaxLength > 0 && iMaxLength < strHPGL.Length) ? strHPGL.Substring (0, iMaxLength) : strHPGL;

                return strHPGL;
            }

            return "";
        }

        public CHPGL.SPrintQueueEntry[] GetPrintQueueJobList ()
        {
            List<CHPGL.SPrintQueueEntry> lPrintQueueEntries = new List<CHPGL.SPrintQueueEntry> ();

            lock (m_objLock)
            {
                foreach (KeyValuePair<string, string> kvp in m_sdOutputQueue)
                {
                    CHPGL.SPrintQueueEntry pqe = new CHPGL.SPrintQueueEntry ();
                    pqe.strDocumentName = kvp.Key;
                    pqe.iDocumentLength = kvp.Value.Length;
                    lPrintQueueEntries.Add (pqe);
                }
            }

            return lPrintQueueEntries.ToArray ();
        }

        public int GetQueueLength ()
        {
            return m_sdOutputQueue.Count;
        }

        public int GetQueueSize ()
        {
            return m_iQueueSize;
        }

        public bool IsPortOpen ()
        {
            return (m_objSerialPort != null &&
                    m_objSerialPort.IsOpen  &&
                    m_iPlotterBufferSize > 0);
        }

        public void AbortPlot ()
        {
            ConsoleOutput ("In AbortPlot ()");
            m_bAbortPlot = true;

            ConsoleOutput ("  m_sdOutputQueue.Clear ()");
            lock (m_objLock)
            {
                m_sdOutputQueue.Clear ();
            }
            m_iQueueSize = 0;

            ConsoleOutput ("  WriteQueryString (CHPGL.EscAbortGraphicControl ()");
            WriteQueryString (CHPGL.EscAbortGraphicControl ());

            ConsoleOutput ("  Exiting AbortPlot ()");
        }

        public bool WaitForPlotter (int iTimeOutSeconds, bool bBufferSpaceTrace)
        {
            ConsoleOutput (string.Format ("WaitForPlotter ({0} seconds, bBufferSpaceTrace: {1})", iTimeOutSeconds, bBufferSpaceTrace.ToString ()));

            int iStartCount       = Environment.TickCount & Int32.MaxValue,
                iEndCount         = iStartCount + (iTimeOutSeconds * 1000),
                iOutputQueueCount = 0;

            lock (m_objLock)
            {
                iOutputQueueCount = m_sdOutputQueue.Count;
            }

            bool bPlotterBusy = iOutputQueueCount > 0 ||
                                GetPlotterBufferSpace () < m_iPlotterBufferSize;
            while (bPlotterBusy)
            {
                int iTickCount        = Environment.TickCount & Int32.MaxValue;
                int iBufferSpace      = -1;
                int iTickRemaining    = iEndCount - iTickCount;
                int iSecondsRemaining = iTickRemaining / 1000;
                ConsoleOutput (string.Format ("  WaitForPlotter: {0}", iSecondsRemaining));
                //ConsoleOutput (string.Format ("  WaitForPlotter: {0} {1}", iTickCount - iStartCount, iEndCount - iTickCount));

                if (iTickCount > iEndCount)
                {
                    ConsoleOutput ("  WaitForPlotter exits w/false");
                    return false;
                }

                lock (m_objLock)
                {
                    iOutputQueueCount = m_sdOutputQueue.Count;
                    iBufferSpace = GetPlotterBufferSpace ();
                }
                ConsoleOutput (string.Format ("  WaitForPlotter: {0} {1}", iOutputQueueCount, iBufferSpace));

                bPlotterBusy = iOutputQueueCount > 0 ||
                               GetPlotterBufferSpace () < m_iPlotterBufferSize;
            }

            if (bPlotterBusy)
            {
                ThrowException (SERIAL_PLOTTER_IS_BUSY);
            }

            ConsoleOutput ("  WaitForPlotter exits w/true");
            return true;
        }

        public bool WaitForQueue (int iTimeOutSeconds)
        {
            int iStartCount = Environment.TickCount & Int32.MaxValue,
                iEndCount = iStartCount + (iTimeOutSeconds * 1000);

            while (m_sdOutputQueue.Count > 0)
            {
                int iTickCount = Environment.TickCount & Int32.MaxValue;
                if (m_bOutputTrace)
                {
                    Thread.Sleep (250);
                    ConsoleOutput (string.Format ("WaitForQueue: [{0}] {1} {2}", m_sdOutputQueue.Count,
                                                                                 iTickCount - iStartCount, iEndCount - iTickCount));
                }
                if (iTickCount > iEndCount)
                    return false;
            }

            return true;
        }

        public List<int> ReadDigitizedPoints ()
        {
            string strHPGL = CHPGL.Initialize () + CHPGL.SelectPen () + CHPGL.PlotAbsolute (0, 0) + CHPGL.DigitizePoint ();
            WriteTextString (strHPGL, CGenericMethods.FormatPlotDocName ("ReadDigitizedPoints", strHPGL.Length));

            string strTest = "";

            bool bFinished = false;
            int iLastXPos  = -1,
                iLastYPos  = -1,
                iLastPen   = -1;
            List<int> liPoints = new List<int> ();

            while (!bFinished)
            {
                int iPositionX = -1,
                    iPositionY = -1,
                    iPenPos    = -1;

                Thread.Sleep (100);
                string strStatus = QueryStatus ();
                int iLen = strStatus.Length;
                if (iLen > 0 && iLen < 5)
                {
                    int iStatus = CHPGL.SafeConvertToInt (strStatus);
                    if ((iStatus & 4) > 0)
                    {
                        strTest = QueryDigitizedPoint ();
                        ConsoleOutput (strTest);
                        int iComma1 = strTest.IndexOf (','),
                            iComma2 = strTest.IndexOf (',', iComma1 + 1);
                        string strX = strTest.Substring (0, iComma1),
                               strY = strTest.Substring (iComma1 + 1, iComma2 - iComma1 - 1);
                        ConsoleOutput (string.Format ("X: {0}  Y: {1}:  Pen: {2}", strX, strY, strTest[iComma2 + 1]));
                        iPositionX = Convert.ToInt16 (strX);
                        iPositionY = Convert.ToInt16 (strY);
                        iPenPos = Convert.ToInt16 (strTest[iComma2 + 1]) - (int)'0';
                        //iPositionX = Convert.ToInt16 (strTest.Substring (0, iComma1));
                        //iPositionY = Convert.ToInt16 (strTest.Substring (iComma1 + 1, iComma2 - iComma1 - 1));

                        if (iLastXPos == iPositionX &&
                            iLastYPos == iPositionY)
                        {
                            ConsoleOutput ("** Exit loop on same point entered twice.");
                            bFinished = true;
                            strHPGL = CHPGL.DigitizeClear ();
                            WriteTextString (strHPGL, CGenericMethods.FormatPlotDocName ("DigitizeClear", strHPGL.Length));
                            break;
                        }
                        else
                        {
                            liPoints.Add (iPositionX);
                            liPoints.Add (iPositionY);
                            liPoints.Add (iPenPos);
                            ConsoleOutput (string.Format ("Adding point: X {0}  Y {1}  Pen {2}", iPositionX, iPositionY, iPenPos));
                        }

                        iLastXPos = iPositionX;
                        iLastYPos = iPositionY;
                        iLastPen = iPenPos;

                        strHPGL = CHPGL.DigitizePoint ();
                        WriteTextString (strHPGL, CGenericMethods.FormatPlotDocName ("DigitizePoint", strHPGL.Length));
                    }
                }
            }

            return liPoints;
        }

        public bool CloseOutputPort (EClosePortMethod eClosePortMethod = EClosePortMethod.EClosePortOnly)
        {
            m_bKeepOutputThreadRunning = false;

            if (m_objSerialPort != null &&
                m_objSerialPort.IsOpen)
            {
                if (eClosePortMethod == EClosePortMethod.EAbortAndClose)
                {
                    AbortPlot ();
                }
                else if (eClosePortMethod == EClosePortMethod.EWaitForPlotterAndClose)
                {
                    WaitForPlotter (300, false);
                }
                else if (eClosePortMethod == EClosePortMethod.EWaitForQueueAndClose)
                {
                    WaitForQueue (300);
                }

                if (m_objSerialPort != null)
                {
                    m_objSerialPort.Close ();
                    m_objSerialPort      = null;

                    m_sdOutputQueue.Clear ();
                    m_iQueueSize               = 0;
                    m_bKeepOutputThreadRunning = true;

                    m_bOutputTrace             = false;
                    m_iPlotterBufferSize       = -1;
                    m_iLastBufferSpace         = -1;
                    m_strDevName               = "";
                }
            }

            return true;
        }

        public void ShowErrorAndStatus (bool bForceOutput = false)
        {
            ConsoleOutput (GetExtendedErrorText (), bForceOutput);
            if (m_bOutputTrace)
            {
                ConsoleOutput (GetExtendedStatusText (), bForceOutput);
            }
        }

        #region Threading methods
        private async void StartWriteThreadAsync ()
        {
            //ConsoleOutput ("In StartWriteThreadAsync ()");

            string result = await WriteTextStringAsync ("WriteTextString");

            //ConsoleOutput ("Await result: " + result);
        }

        private Task<string> WriteTextStringAsync (string name)
        {
            //ConsoleOutput (string.Format ("In WriteTextStringAsync ({0})", name));

            return Task.Run<string> (() =>
            {
                return WriteTextStringThread (name);
            });
        }

        private string WriteTextStringThread (string name)
        {
            //ConsoleOutput (string.Format ("In WriteTextStringThread ({0})", name));
            
            while (m_bKeepOutputThreadRunning)
            {
                //if (m_bAbortPlot)
                //{
                //    ConsoleOutput ("  m_bAbortPlot true in WriteTextStringThread");
                //}

                if (m_sdOutputQueue.Count > 0)
                {
                    WriteTextString ();
                }

                Thread.Sleep (100);
            }

            return string.Format ("Closing {0}", name);
        }
        #endregion

        #region State query methods
        public int GetPlotterBufferSize ()
        {
            return m_iPlotterBufferSize;
        }

        public int GetPlotterBufferSpace ()
        {
            int iRetry = 5;
            int iReturn = QueryPlotterInt (CHPGL.EscOutputBufferSpace ());

            while (iReturn < 0 && (iRetry--) > 0)
            {
                iReturn = QueryPlotterInt (CHPGL.EscOutputBufferSpace ());
                ConsoleOutput ("GetPlotterBufferSpace () retry: " + iRetry.ToString ());
            }

            return iReturn;
        }

        public bool IsPlotterBusy (bool bBufferSpaceTrace)
        {
            int iAvailableBufferSize = GetPlotterBufferSpace ();

            if (bBufferSpaceTrace)
            {
                if (m_bOutputTrace &&
                    m_iLastBufferSpace != iAvailableBufferSize)
                {
                    ConsoleOutput ("AvailableBufferSize: " + iAvailableBufferSize.ToString ());
                }
                m_iLastBufferSpace = iAvailableBufferSize;
            }

            return m_iPlotterBufferSize > iAvailableBufferSize;
        }

        public bool IsPlotterBufferEmpty ()
        {
            return GetPlotterBufferSpace () == m_iPlotterBufferSize;
        }

        public bool IsPlotterQueueEmpty ()
        {
            lock (m_objLock)
            {
                return m_sdOutputQueue.Count == 0;
            }
        }

        public string QueryIdentification ()
        {
            return QueryPlotter (CHPGL.OutputIdentification ());
        }

        public string QueryDigitizedPoint ()
        {
            return QueryPlotter (CHPGL.OutputDigitizedPoint ());
        }

        public string QueryStatus ()
        {
            return QueryPlotter (CHPGL.OutputStatus ());
        }

        public string QueryStatusText ()
        {
            string strResponse = QueryStatus ();
            int iStatus = CGenericMethods.SafeConvertToInt (strResponse);
            StringBuilder sbStatusText = new StringBuilder ("Plotter status: ");
            sbStatusText.Append (strResponse);
            sbStatusText.Append ("\n");

            if ((iStatus & 64) > 0)
            {
                sbStatusText.Append ("  Bit 6: Require service message set (always 0 for OS;\n");
                sbStatusText.Append ("         0 or 1 for HP-IB serial poll).\n");
            }
            if ((iStatus & 32) > 0)
            {
                sbStatusText.Append ("  Bit 5: Error; cleared by reading OE output in HP-IB system or by output\n");
                sbStatusText.Append ("         of the error in RS-232-C system, or by IN instruction.\n");
            }
            if ((iStatus & 16) > 0)
            {
                sbStatusText.Append ("  Bit 4: Ready for data; pinch wheels down.\n");
            }
            if ((iStatus & 8) > 0)
            {
                sbStatusText.Append ("  Bit 3: Initialized; cleared by reading OS output in HP-IB system\n");
                sbStatusText.Append ("         or by output of the status byte in RS-232-C system.\n");
            }
            if ((iStatus & 4) > 0)
            {
                sbStatusText.Append ("  Bit 2: Digitized point available; cleared by reading digitized value in HP-IB\n");
                sbStatusText.Append ("         system or by output of point in RS-232-C system, or by IN instruction.\n");
            }
            if ((iStatus & 2) > 0)
            {
                sbStatusText.Append ("  Bit 1: PI or P2 changed; cleared by reading output of OP in HP-IB system or\n");
                sbStatusText.Append ("         by actual output of Pl,P2 in RS-232-C system, or by IN instruction.\n");
            }
            if ((iStatus & 1) > 0)
            {
                sbStatusText.Append ("  Bit 0: Pen down.\n");
            }

            return sbStatusText.ToString ();
        }

        public string QueryFactors ()
        {
            return QueryPlotter (CHPGL.OutputFactors ());
        }

        public string QueryFactorsText ()
        {
            string strResponse = QueryFactors ();
            StringBuilder sbFactorText = new StringBuilder ("Plotter factors: ");
            sbFactorText.Append (strResponse);
            sbFactorText.Append ("\n");

            int iCommaIdx = strResponse.IndexOf (',');
            if (iCommaIdx > 0 && iCommaIdx < strResponse.Length)
            {
                sbFactorText.Append ("  ");
                sbFactorText.Append (strResponse.Substring (0, iCommaIdx));
                sbFactorText.Append (" units/mm x-axis\n  ");
                sbFactorText.Append (CGenericMethods.SafeConvertToInt (strResponse.Substring (iCommaIdx + 1)).ToString ());
                sbFactorText.Append (" units/mm y-axis\n");
            }

            return sbFactorText.ToString ();
        }

        public string QueryError ()
        {
            return QueryPlotter (CHPGL.OutputError ());
        }

        public string QueryErrorText ()
        {
            string strResponse = QueryError ();
            StringBuilder sbErrorText = new StringBuilder ("Plotter error: ");
            ConsoleOutput (string.Format ("*** Error code returned from SerialWrapper::QueryError (): [{0}] {1}", strResponse.Length, strResponse));
            if (strResponse.Length == 0)
            {
                ConsoleOutput ("*** No error code returned from SerialWrapper::QueryError ()");
                return strResponse; // Plotter returned no error code
            }

            if (strResponse.IndexOf (',') > 0)
            {
                ConsoleOutput ("*** Bad error code returned from SerialWrapper::QueryError (): " + strResponse);
                return strResponse; // Plotter returned bad error code
            }

            sbErrorText.Append (CGenericMethods.SafeConvertToInt (strResponse).ToString ());
            int iError = CGenericMethods.SafeConvertToInt (strResponse);

            if (0 == iError)
            {
                sbErrorText.Append ("  No error\n");
            }
            else if (1 == iError)
            {
                sbErrorText.Append ("  Instruction not recognized\n");
            }
            else if (2 == iError)
            {
                sbErrorText.Append ("  Wrong number of parameters\n");
            }
            else if (3 == iError)
            {
                sbErrorText.Append ("  Out-of-range parameters\n");
            }
            else if (4 == iError)
            {
                sbErrorText.Append ("  <Not used>\n");
            }
            else if (5 == iError)
            {
                sbErrorText.Append ("  Unknown character set\n");
            }
            else if (6 == iError)
            {
                sbErrorText.Append ("  Position overflow\n");
            }
            else if (7 == iError)
            {
                sbErrorText.Append ("  <Not used>\n");
            }
            else if (8 == iError)
            {
                sbErrorText.Append ("  Vector received while pinch wheels raised\n");
            }

            return sbErrorText.ToString ();
        }

        public string QueryActualPosition ()
        {
            return QueryPlotter (CHPGL.OutputActualPosition ());
        }

        public string QueryActualPositionText ()
        {
            string strResponse = QueryActualPosition ();
            int iComma1 = strResponse.IndexOf (','),
                iComma2 = strResponse.IndexOf (',', iComma1 + 1);

            StringBuilder sbActualPositionText = new StringBuilder ("Plotter Actual Pen Position: ");
            sbActualPositionText.Append (strResponse);
            sbActualPositionText.Append ("\n");

            if (iComma1 > 0 && iComma2 > 0)
            {
                sbActualPositionText.Append ("  X: ");
                sbActualPositionText.Append (strResponse.Substring (0, iComma1));
                sbActualPositionText.Append ("  Y: ");
                sbActualPositionText.Append (strResponse.Substring (iComma1 + 1, iComma2 - iComma1 - 1));
                sbActualPositionText.Append ((strResponse[iComma2 + 1] == '0') ? "  Pen up" : "  Pen down");
            }

            return sbActualPositionText.ToString ();
        }

        public string QueryCommandedPosition ()
        {
            return QueryPlotter (CHPGL.OutputCommandedPosition ());
        }

        public string QueryCommandedPositionText ()
        {
            string strResponse = QueryCommandedPosition ();
            int iComma1 = strResponse.IndexOf (','),
                iComma2 = strResponse.IndexOf (',', iComma1 + 1);

            StringBuilder sbCommandedPositionText = new StringBuilder ("Plotter Commanded Pen Position: ");
            sbCommandedPositionText.Append (strResponse);
            sbCommandedPositionText.Append ("\n");

            if (iComma1 > 0 && iComma2 > 0)
            {
                sbCommandedPositionText.Append ("  X: ");
                sbCommandedPositionText.Append (strResponse.Substring (0, iComma1));
                sbCommandedPositionText.Append ("  Y: ");
                sbCommandedPositionText.Append (strResponse.Substring (iComma1 + 1, iComma2 - iComma1 - 1));
                sbCommandedPositionText.Append ((strResponse[iComma2 + 1] == '0') ? "  Pen up" : "  Pen down");
            }

            return sbCommandedPositionText.ToString ();
        }

        public string QueryOptions ()
        {
            return QueryPlotter (CHPGL.OutputOptions ());
        }

        public string QueryOptionsText ()
        {
            string strResponse = QueryOptions ();
            int iComma1 = strResponse.IndexOf (','),
                iComma2 = strResponse.IndexOf (',', iComma1 + 1),
                iComma3 = strResponse.IndexOf (',', iComma2 + 1),
                iComma4 = strResponse.IndexOf (',', iComma3 + 1),
                iComma5 = strResponse.IndexOf (',', iComma4 + 1),
                iComma6 = strResponse.IndexOf (',', iComma5 + 1),
                iComma7 = strResponse.IndexOf (',', iComma6 + 1);

            StringBuilder sbOptionsText = new StringBuilder ("Plotter Options: ");
            sbOptionsText.Append (strResponse);
            sbOptionsText.Append ("\n");

            if (iComma1 > 0 && iComma2 > 0 && iComma3 > 0 && iComma4 > 0 &&
                iComma5 > 0 && iComma6 > 0 && iComma7 >0)
            {
               sbOptionsText.Append ("  Option 1: ");
               sbOptionsText.Append (strResponse.Substring (0, iComma1));
               sbOptionsText.Append ("  <undefined>\n");

               sbOptionsText.Append ("  Option 2: ");
               sbOptionsText.Append (strResponse.Substring (iComma1 + 1, iComma2 - iComma1 - 1));
               sbOptionsText.Append ("  Pen select capability is included.\n");

               sbOptionsText.Append ("  Option 3: ");
               sbOptionsText.Append (strResponse.Substring (iComma2 + 1, iComma3 - iComma2 - 1));
               sbOptionsText.Append ("  <undefined>\n");

               sbOptionsText.Append ("  Option 4: ");
               sbOptionsText.Append (strResponse.Substring (iComma3 + 1, iComma4 - iComma3 - 1));
               sbOptionsText.Append ("  <undefined>\n");

               sbOptionsText.Append ("  Option 5: ");
               sbOptionsText.Append (strResponse.Substring (iComma4 + 1, iComma5 - iComma4 - 1));
               sbOptionsText.Append ("  Arcs and circle instructions are included.\n");

               sbOptionsText.Append ("  Option 6: ");
               sbOptionsText.Append (strResponse.Substring (iComma5 + 1, iComma6 - iComma5 - 1));
               sbOptionsText.Append ("  <undefined>\n");

               sbOptionsText.Append ("  Option 7: ");
               sbOptionsText.Append (strResponse.Substring (iComma6 + 1, iComma7 - iComma6 - 1));
               sbOptionsText.Append ("  <undefined>\n");

               sbOptionsText.Append ("  Option 8: ");
               sbOptionsText.Append (CGenericMethods.SafeConvertToInt (strResponse.Substring (iComma7 + 1)).ToString ());
               sbOptionsText.Append ("  <undefined>\n");
            }

            return sbOptionsText.ToString ();
        }

        public string QueryHardClipLimits ()
        {
            return QueryPlotter (CHPGL.OutputHardClipLimits ());
        }

        public string QueryHardClipLimitsText ()
        {
            string strResponse = QueryHardClipLimits ();
            int iComma1 = strResponse.IndexOf (','),
                iComma2 = strResponse.IndexOf (',', iComma1 + 1),
                iComma3 = strResponse.IndexOf (',', iComma2 + 1);
            StringBuilder sbHardClipLimitsText = new StringBuilder ("Plotter Hard Clip Limits: ");
            sbHardClipLimitsText.Append (strResponse);
            sbHardClipLimitsText.Append ("\n");

            if (iComma1 > 0 && iComma2 > 0 && iComma3 > 0)
            {
                sbHardClipLimitsText.Append ("  Lower Left X: ");
                sbHardClipLimitsText.Append (strResponse.Substring (0, iComma1));
                sbHardClipLimitsText.Append ("  Lower Left Y: ");
                sbHardClipLimitsText.Append (strResponse.Substring (iComma1 + 1, iComma2 - iComma1 - 1));
                sbHardClipLimitsText.Append ("\n  Upper Right X: ");
                sbHardClipLimitsText.Append (strResponse.Substring (iComma2 + 1, iComma3 - iComma2 - 1));
                sbHardClipLimitsText.Append ("  Upper Right Y: ");
                sbHardClipLimitsText.Append (CGenericMethods.SafeConvertToInt (strResponse.Substring (iComma3 + 1)).ToString ());
                sbHardClipLimitsText.Append ("\n");
            }

            return sbHardClipLimitsText.ToString ();
        }

        public int GetExtendedError ()
        {
            return QueryPlotterInt (CHPGL.EscOutputExtendedError ());
        }

        public string GetExtendedErrorText (bool bShowNoError = true)
        {
            int iExtendedError = GetExtendedError ();
            StringBuilder sbExtendedError = new StringBuilder ();
            if (0 == iExtendedError)
            {
                if (bShowNoError)
                {
                    sbExtendedError.Append ("Error 0: No I/O error has occurred.");
                }
            }
            else if (10 == iExtendedError)
            {
                sbExtendedError.Append ("Error 10: ");
                sbExtendedError.Append ("Output instruction received while another output instruction is executing. The original ");
                sbExtendedError.Append ("instruction will continue normally; the one in error will be ignored.");
            }
            else if (11 == iExtendedError)
            {
                sbExtendedError.Append ("Error 11: ");
                sbExtendedError.Append ("Invalid byte received after first two characters, <ESC>., in a device control instruction.");
            }
            else if (12 == iExtendedError)
            {
                sbExtendedError.Append ("Error 12: ");
                sbExtendedError.Append ("Invalid byte received while parsing a device control instruction. The parameter ");
                sbExtendedError.Append ("containing the invalid byte and all following parameters are defaulted.");
            }
            else if (13 == iExtendedError)
            {
                sbExtendedError.Append ("Error 13: ");
                sbExtendedError.Append ("Parameter out of range.");
            }
            else if (14 == iExtendedError)
            {
                sbExtendedError.Append ("Error 14: ");
                sbExtendedError.Append ("Too many parameters received. Additional parameters beyond the proper number are ignored; parsing of the ");
                sbExtendedError.Append ("instruction ends when a colon (normal exit) or the first byte of another instruction is received (abnormal exit).");
            }
            else if (15 == iExtendedError)
            {
                sbExtendedError.Append ("Error 15: ");
                sbExtendedError.Append ("A framing error, parity error, or overrun error has been detected.");
            }
            else if (16 == iExtendedError)
            {
                sbExtendedError.Append ("Error 16: ");
                sbExtendedError.Append ("The input buffer has overflowed. As a result, one or more bytes of data have been lost, and ");
                sbExtendedError.Append ("therefore an HP-GL error will probably occur.");
            }
            else if (EMPTY_READ_DATA_STRING == iExtendedError)
            {
                sbExtendedError.Append ("Error -1: ");
                sbExtendedError.Append ("No string returned from plotter.  Plotter is most likely in an error condition.  ESC.E should clear it.");
            }
            else
            {
                sbExtendedError.Append ("Undefined error: ");
                sbExtendedError.Append (iExtendedError.ToString ());
            }

            return sbExtendedError.ToString ();
        }

        public int GetExtendedStatus ()
        {
            return QueryPlotterInt (CHPGL.EscOutputExtendedStatus ());
        }

        public string GetExtendedStatusText ()
        {
            int iExtendedStatus = GetExtendedStatus ();
            StringBuilder sbExtendedStatus = new StringBuilder ();
            if (0 == iExtendedStatus)
            {
                sbExtendedStatus.Append ("Status: ");
                sbExtendedStatus.Append ("Buffer is not empty and plotter is processing HP-GL instructions.");
            }
            else if (8 == iExtendedStatus)
            {
                sbExtendedStatus.Append ("Status: ");
                sbExtendedStatus.Append ("Buffer is empty and is ready to process or is processing HP-GL instructions.");
            }
            else if (16 == iExtendedStatus)
            {
                sbExtendedStatus.Append ("Status: ");
                sbExtendedStatus.Append ("Buffer is not empty and VIEW has been pressed.");
            }
            else if (24 == iExtendedStatus)
            {
                sbExtendedStatus.Append ("Status: ");
                sbExtendedStatus.Append ("Buffer is empty and VIEW has been pressed.");
            }
            else if (32 == iExtendedStatus)
            {
                sbExtendedStatus.Append ("Status: ");
                sbExtendedStatus.Append ("Buffer is not empty and paper lever and pinch wheels are raised.");
            }
            else if (40 == iExtendedStatus)
            {
                sbExtendedStatus.Append ("Status: ");
                sbExtendedStatus.Append ("Buffer is empty and paper lever and pinch wheels are raised.");
            }
            else
            {
                sbExtendedStatus.Append ("Undefined status: ");
                sbExtendedStatus.Append (iExtendedStatus.ToString ());
            }

            return sbExtendedStatus.ToString ();
        }

        public string QueryOutputWindow ()
        {
            return QueryPlotter (CHPGL.OutputWindow ());
        }

        public string QueryOutputWindowText ()
        {
            string strResponse = QueryOutputWindow ();

            int iComma1 = strResponse.IndexOf (','),
                iComma2 = strResponse.IndexOf (',', iComma1 + 1),
                iComma3 = strResponse.IndexOf (',', iComma2 + 1);
            StringBuilder sbOutputWindowText = new StringBuilder ("Plotter Output Window: ");
            sbOutputWindowText.Append (strResponse);
            sbOutputWindowText.Append ("\n");

            if (iComma1 > 0 && iComma2 > 0 && iComma3 > 0)
            {
                sbOutputWindowText.Append ("  Lower Left X: ");
                sbOutputWindowText.Append (strResponse.Substring (0, iComma1));
                sbOutputWindowText.Append ("  Lower Left Y: ");
                sbOutputWindowText.Append (strResponse.Substring (iComma1 + 1, iComma2 - iComma1 - 1));
                sbOutputWindowText.Append ("\n  Upper Right X: ");
                sbOutputWindowText.Append (strResponse.Substring (iComma2 + 1, iComma3 - iComma2 - 1));
                sbOutputWindowText.Append ("  Upper Right Y: ");
                sbOutputWindowText.Append (CGenericMethods.SafeConvertToInt (strResponse.Substring (iComma3 + 1)).ToString ());
                sbOutputWindowText.Append ("\n");
            }

            return sbOutputWindowText.ToString ();
        }
        #endregion

        #region Private methods
        private int OpenComPort (int iBaudRate = BAUD9600)
        {
            if (iBaudRate == BAUD9600 ||
                iBaudRate == BAUD4800 ||
                iBaudRate == BAUD2400 ||
                iBaudRate == BAUD1200 ||
                iBaudRate == BAUD600  ||
                iBaudRate == BAUD300  ||
                iBaudRate == BAUD150)
            {
                m_iBaudRate = iBaudRate;
            }
            else
            {
                return SERIAL_INVALID_BAUD_RATE;
            }

            bool bPortFound = false;

            // Close output port if one is open
            CloseOutputPort ();

            // Determine COM port name
            ConsoleOutput ("Searching serial ports...", true);
            string[] straComPorts = GetAvailableComPorts ();
            foreach (string strPort in straComPorts)
            {
                ConsoleOutput ("Trying " + strPort, true);

                if (OpenNamedPort (strPort, iBaudRate))
                {
                    bPortFound = true;
                    ConsoleOutput ("Found " + strPort, true);
                    break;
                }
            }

            if (!bPortFound)
            {
                ConsoleOutput ("No serial port available", true);
                return SERIAL_NO_SERIAL_PORT_AVAILABLE;
            }

            ConsoleOutput ("OK", true);
            GetPlotterBufferSizeInternal ();
            if (m_iPlotterBufferSize < 0)
            {
                ConsoleOutput ("Unable to open named port in OpenComPort", true);
                CloseOutputPort ();
                throw new Exception ("Unable to open named port in OpenComPort");
            }

            return SERIAL_PORT_OPENED;
        }

        private bool OpenNamedPort (string strComPort, int iBaudRate)
        {
            if (!CloseOutputPort ())
                return false;

            m_objSerialPort = new SerialPort ();

            // Allow the user to set the appropriate properties.
            m_objSerialPort.PortName  = strComPort;
            m_objSerialPort.BaudRate  = m_iBaudRate;
            m_objSerialPort.Parity    = Parity.None;
            m_objSerialPort.DataBits  = 8;
            m_objSerialPort.StopBits  = StopBits.One;
            m_objSerialPort.Handshake = Handshake.None;
            m_objSerialPort.DtrEnable = true;
            m_objSerialPort.RtsEnable = true;

            // Set the read/write timeouts
            m_objSerialPort.ReadTimeout  = 500;
            m_objSerialPort.WriteTimeout = 500;

            m_objSerialPort.Open ();
            if (m_objSerialPort.IsOpen)
            {
                m_strDevName = strComPort;
            }

            if (IsImageMaker () &&
                GetPlotterBufferSizeInternal () > 0)
            {
                return true;
            }

            m_strDevName = "";
            CloseOutputPort ();

            return false;
        }

        private string QueryPlotter (string strBuffer)
        {
            lock (m_objLock)
            {
                if ((strBuffer.Length > 2                   &&
                     strBuffer.Substring (0, 2) == "\x1B.") ||
                    m_strDevName.Length > 0                 &&
                    (m_sdOutputQueue.Count      == 0        ||
                     IsPlotterBufferEmpty ()))
                {
                    string strReturn = "";

                    if (WriteQueryString (strBuffer))
                    {
                        strReturn = ReadData (false);
                    }

                    return strReturn;
                }
            }

            return "<busy>";
        }

        private int QueryPlotterInt (string strBuffer)
        {
            string strQuery = "";
            strQuery = QueryPlotter (strBuffer);
            ConsoleOutput ("QueryPlotterInt: " + strQuery);
            if (strQuery.Length > 0)
            {
                return CGenericMethods.SafeConvertToInt (strQuery);
            }
            else if (strBuffer == "<busy>")
            {
                return PLOTTER_BUSY_ERROR_CODE;
            }

            return 0;
        }

        private int GetPlotterBufferSizeInternal ()
        {
            m_iPlotterBufferSize = QueryPlotterInt (CHPGL.EscOutputBufferSize ());
            return m_iPlotterBufferSize;
        }

        private bool IsImageMaker ()
        {
            return QueryIdentification ().TrimEnd () == STRING_PLOTTER_ID;
        }

        private bool WriteTextString ()
        {
            bool bSuccess     = false;
            bool bForceOutput = false;

            try
            {
                string strBuffer = "";
                lock (m_objLock)
                {
                    if (m_sdOutputQueue.Count > 0)
                    {
                        strBuffer = CGenericMethods.RemoveCommentHPGL (m_sdOutputQueue.ElementAt (0).Value);
                        ConsoleOutput ("Initial strBuffer: \"" + strBuffer + '\"', bForceOutput);
                    }
                    else
                    {
                        return false;
                    }
                }

                if (m_objSerialPort != null &&
                    m_objSerialPort.IsOpen  &&
                    strBuffer       != null &&
                    strBuffer.Length > 0)
                {
                    int iBytesToSend = strBuffer.Length;
                    int iPlotterBufferSpace = GetPlotterBufferSpace ();
                    ConsoleOutput ("  iBytesToSend [1]: "    + iBytesToSend.ToString () +
                                   "  iPlotterBufferSpace: " + iPlotterBufferSpace.ToString (), bForceOutput); 
                    if (iBytesToSend > MAX_SEND_LENGTH ||
                        iBytesToSend > iPlotterBufferSpace)
                    {
                        int iShowSegmentLength        = 5;
                        int iCurrentSegmentEndLength  = -1;
                        string strBufferSegment       = strBuffer;
                        string strEndOfCurrentSegment = "";
                        ConsoleOutput (string.Format ("Start [{0}] WriteTextSubString (\"{1}\")", strBufferSegment.Length, strBufferSegment), bForceOutput);

                        while (iBytesToSend > 0)
                        {
                            if (m_bAbortPlot)
                            {
                                m_bAbortPlot = false;
                                ConsoleOutput ("  m_bAbortPlot reset in WriteTextString ()");
                                iBytesToSend = 0;
                                strBuffer = "";
                                strBufferSegment = "";
                                break;
                            }

                            ConsoleOutput ("  m_bAbortPlot: " + m_bAbortPlot.ToString (), bForceOutput);
                            int iMaxSendLength = GetPlotterBufferSpace ();
                            ConsoleOutput ("  iMaxSendLength [1]: " + iMaxSendLength.ToString (), bForceOutput);
                            if (iMaxSendLength == 0)
                            {
                                continue;
                            }

                            iMaxSendLength = Math.Min (iMaxSendLength, MAX_SEND_LENGTH);
                            if (iMaxSendLength < MAX_SEND_LENGTH)
                            {
                                ConsoleOutput ("  iMaxSendLength [2]: " + iMaxSendLength.ToString (), bForceOutput);
                            }

                            ConsoleOutput ("  iBytesToSend [2]: " + iBytesToSend.ToString () + "  iMaxSendLength: " + iMaxSendLength.ToString (),
                                           bForceOutput);
                            if (iBytesToSend > iMaxSendLength) // String may be longer than available buffer space
                            {
                                if (strBufferSegment.Length == 0)
                                {
                                    break;
                                }

                                if (m_bOutputTrace || bForceOutput)
                                {
                                    iCurrentSegmentEndLength = iShowSegmentLength;
                                    if (iCurrentSegmentEndLength > strBufferSegment.Length)
                                    {
                                        iCurrentSegmentEndLength = strBufferSegment.Length;
                                    }
                                    strEndOfCurrentSegment = strBufferSegment.Substring (iMaxSendLength - iShowSegmentLength, iCurrentSegmentEndLength);

                                    ConsoleOutput (string.Format ("Loop [{0}] WriteTextSubString (\"{1}\")",
                                                                  strBufferSegment.Length,
                                                                  strBufferSegment.Substring (0, iMaxSendLength)), bForceOutput);
                                }
                                ShowErrorAndStatus (false);
                                WriteTextSubString (strBufferSegment.Substring (0, iMaxSendLength));
                                ShowErrorAndStatus (bForceOutput);

                                strBufferSegment = strBufferSegment.Substring (iMaxSendLength);
                                ConsoleOutput ("  strBufferSegment: \"" + strBufferSegment + '\"', bForceOutput);
                                iBytesToSend = strBufferSegment.Length;
                                ConsoleOutput ("  iBytesToSend [3]: " + iBytesToSend.ToString (), bForceOutput); 
                                if (iBytesToSend == 0)
                                {
                                    break;
                                }

                                if (m_bOutputTrace || bForceOutput)
                                {
                                    int iNextSegmentStartLength = strBufferSegment.Length > iShowSegmentLength ? iShowSegmentLength :
                                                                                                                 strBufferSegment.Length;
                                    string strStartOfNextSegment = strBufferSegment.Substring (0, iNextSegmentStartLength);

                                    ConsoleOutput (string.Format ("String break: \"{0}\" ... \"{1}\"", strEndOfCurrentSegment, strStartOfNextSegment),
                                                   bForceOutput);
                                }

                                Thread.Sleep (50);
                                ConsoleOutput ("WriteTextString GetPlotterBufferSpace (): " + GetPlotterBufferSpace ().ToString (), bForceOutput);

                                while (GetPlotterBufferSpace () < iMaxSendLength)
                                {
                                    // Wait until there's enough buffer space for the next segment
                                    Thread.Sleep (10);
                                    ConsoleOutput ("(while) WriteTextString GetPlotterBufferSpace (): " + GetPlotterBufferSpace ().ToString (), bForceOutput);
                                }
                            }
                            else
                            {
                                ConsoleOutput (string.Format ("End [{0}] WriteTextSubString (\"{1}\")  iBytesToSend: {2}",
                                                              strBufferSegment.Length, strBufferSegment, iBytesToSend), bForceOutput);
                                iBytesToSend = strBufferSegment.Length;

                                ShowErrorAndStatus (false);
                                WriteTextSubString (strBufferSegment);
                                ShowErrorAndStatus (bForceOutput);
                                strBufferSegment = "";

                                bSuccess = true;
                            }
                        }
                    }
                    else
                    {
                        ConsoleOutput (string.Format ("Last WriteTextSubString (\"{0}\")", strBuffer), bForceOutput);
                        ShowErrorAndStatus (false);
                        WriteTextSubString (strBuffer);
                        ShowErrorAndStatus (bForceOutput);
                        strBuffer = "";

                        bSuccess = true;
                    }

                    string strElement0 = "";
                    lock (m_objLock)
                    {
                        if (m_sdOutputQueue.Count > 0)
                        {
                            strElement0 = m_sdOutputQueue.ElementAt (0).Value;
                        }
                        ConsoleOutput ("  m_lstrOutputQueue[0]: \"" + strElement0 + "\" [" + m_sdOutputQueue.Count + ']', bForceOutput);
                        if (m_sdOutputQueue.Count > 0)
                        {
                            ConsoleOutput ("  (In lock): " + m_sdOutputQueue.ElementAt (0).Key, bForceOutput);
                            m_sdOutputQueue.Remove (m_sdOutputQueue.ElementAt (0).Key);
                        }

                        m_iQueueSize -= strElement0.Length;
                        ConsoleOutput ("  (Leaving lock)", bForceOutput);
                    }
                    if (m_sdOutputQueue.Count == 0 &&
                        m_iQueueSize   == 0)
                    {
                        ConsoleOutput ("  m_bAbortPlot reset after lock (" + m_bAbortPlot.ToString () + ')', bForceOutput);
                        m_bAbortPlot = false;
                    }
                    ConsoleOutput ("  (After lock) m_iQueueLength: " + m_sdOutputQueue.Count.ToString () +
                                                "  m_iQueueSize: "   + m_iQueueSize.ToString (), bForceOutput);
                }
            }
            catch (System.ArgumentOutOfRangeException oore)
            {
                Console.WriteLine ("** ArgumentOutOfRangeException in WriteTextString (): " + oore.Message);
                if (oore.InnerException != null)
                {
                    Console.WriteLine ("  " + oore.InnerException.Message);
                }
                Console.WriteLine (oore.StackTrace);
            }
            catch (Exception e)
            {
                Console.WriteLine ("** Exception in WriteTextString (): " + e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine ("  " + e.InnerException.Message);
                }
                Console.WriteLine (e.StackTrace);
            }

            return bSuccess;
        }

        private bool WriteQueryString (string strBuffer)
        {
            if (m_objSerialPort != null &&
                m_objSerialPort.IsOpen)
            {
                try
                {
                    ConsoleOutput ("WriteQueryString: " + strBuffer);
                    lock (m_objLock)
                    {
                        m_objSerialPort.Write (strBuffer);
                    }
                    return true;
                }
                catch (Exception e)
                {
                    ConsoleOutput ("** Exception in WriteQueryString (): " + e.Message, true);
                    if (e.InnerException != null)
                    {
                        ConsoleOutput ("  " + e.InnerException.Message, true);
                    }
                    ConsoleOutput (e.StackTrace, true);

                    if (m_objSerialPort != null)
                    {
                        m_objSerialPort.Close ();
                        m_objSerialPort = null;
                    }

                    throw e;
                }
            }

            return false;
        }

        private string ReadData (bool bShowEmptyBuffer)
        {
            StringBuilder sbBuffer = new StringBuilder ();

            if (m_objSerialPort != null &&
                m_objSerialPort.IsOpen)
            {
                int iReadLimit        = 15;
                int iReadCount        = 0;
                int iEmptyBufferCount = 0;
                string strBuffer      = "";

                while (iReadCount < iReadLimit)
                {
                    Thread.Sleep (100);
                    try
                    {
                        lock (m_objLock)
                        {
                            strBuffer = m_objSerialPort.ReadExisting ().TrimEnd ();
                        }
                        ConsoleOutput ("ReadData: " + strBuffer.Replace ("\r", "\\r"));
                    }
                    catch (Exception e)
                    {
                        ConsoleOutput ("** Exception in ReadData (): " + e.Message, true);
                        if (e.InnerException != null)
                        {
                            ConsoleOutput ("  " + e.InnerException.Message, true);
                        }
                        ConsoleOutput (e.StackTrace, true);

                        if (!e.Message.Contains ("timed out") &&
                            m_objSerialPort != null)
                        {
                            m_objSerialPort.Close ();
                            m_objSerialPort = null;
                        }
                    }

                    ++iReadCount;
                    if (strBuffer.Length > 0)
                    {
                        sbBuffer.Append (strBuffer);
                        ConsoleOutput (iReadCount.ToString () + "  Buffer: " + strBuffer);

                        if (m_iBaudRate > BAUD4800)
                        {
                            break;
                        }
                    }
                    else if (sbBuffer.Length > 0 &&
                             strBuffer.Length == 0)
                    {
                        ++iEmptyBufferCount;
                    }

                    if ((m_iBaudRate > BAUD2400    &&
                            iEmptyBufferCount > 0) ||
                        (m_iBaudRate > BAUD1200    &&
                            iEmptyBufferCount > 1) ||
                        iEmptyBufferCount > 2)
                    {
                        break;
                    }
                }

                if (m_bOutputTrace)
                {
                    if (sbBuffer.Length > 0)
                    {
                        ConsoleOutput (string.Format ("({0}, {1}) {2}", iReadCount, iEmptyBufferCount, sbBuffer.ToString ()));
                    }
                    else
                    {
                        ConsoleOutput (string.Format ("({0}, {1}) <No data read>", iReadCount, iEmptyBufferCount), true);
                    }
                }
            }

            if (bShowEmptyBuffer &&
                sbBuffer.Length == 0)
            {
                ConsoleOutput ("<empty buffer>", true);
            }

            return sbBuffer.ToString ();
        }

        private bool WriteTextSubString (string strBuffer)
        {
            if (m_objSerialPort != null &&
                m_objSerialPort.IsOpen &&
                strBuffer != null &&
                strBuffer.Length > 0)
            {
                try
                {
                    lock (m_objLock)
                    {
                        m_objSerialPort.Write (strBuffer);
                    }
                    if (m_bPauseAfterNewPen &&
                        CGenericMethods.IsSelectPenCommandInHPGL (strBuffer))
                    {
                        while (GetPlotterBufferSpace () < m_iPlotterBufferSize)
                        {
                            Thread.Sleep (250);
                        }
                        Thread.Sleep (5000);
                    }
                }
                catch (Exception e)
                {
                    ConsoleOutput ("** Exception in WriteTextSubString (): " + e.Message, true);
                    if (e.InnerException != null)
                    {
                        ConsoleOutput ("  " + e.InnerException.Message, true);
                    }
                    ConsoleOutput (e.StackTrace, true);

                    if (m_objSerialPort != null)
                    {
                        m_objSerialPort.Close ();
                        m_objSerialPort = null;
                    }

                    throw e;
                }
            }

            return true;
        }

        private static string[] GetAvailableComPorts ()
        {
            SortedList<string, int> slstrPortNames1 = new SortedList<string, int> ();
            SortedList<int, string> slstrPortNames2 = new SortedList<int, string> ();
            List<string> lstrPortNames = new List<string> ();
            string[] straPortNames = SerialPort.GetPortNames ();

            if (straPortNames.Length > 0)
            {
                foreach (string str in straPortNames)
                {
                    slstrPortNames1.Add (str, 0); // Sorted by device name
                }
            }

            foreach (KeyValuePair<string, int> kvp in slstrPortNames1)
            {
                slstrPortNames2.Add (slstrPortNames2.Count, kvp.Key);
            }

            for (int iIdx = slstrPortNames2.Count - 1; iIdx >= 0; --iIdx)
            {
                string strPortName = slstrPortNames2.ElementAt (iIdx).Value;
                lstrPortNames.Add (strPortName);
            }

            return lstrPortNames.ToArray ();
        }

        private void ThrowException (int iStatus)
        {
            ThrowException (iStatus, 0);
        }

        private void ThrowException (int iStatus, int iBaud)
        {
            if (iStatus > 0)
            {
                switch (iStatus)
                {
                    // From OpenComPort
                    case SERIAL_INVALID_BAUD_RATE:
                    {
                        StringBuilder strbldError = new StringBuilder ("Seral: Invalid baud rate: must be 9600, 4800, 2400, 1200, 600, 300, or 150. (error code 1)");
                        if (iBaud > 0)
                        {
                            strbldError.Append ("  Got: ");
                            strbldError.Append (iBaud.ToString ());
                        }
                        throw new Exception (strbldError.ToString ());
                    }

                    case SERIAL_NO_SERIAL_PORT_AVAILABLE:
                    {
                        throw new Exception ("Seral: No serial port available in OpenComPort (error code 2)");
                    }

                    case SERIAL_UNABLE_TO_OPEN_COM_PORT:
                    {
                        throw new Exception ("Seral: Unable to open named port in OpenComPort (error code 3)");
                    }

                    // From CloseOutputPort
                    case SERIAL_CLOSEHANDLE_FAILED:
                    {
                        throw new Exception ("Serial: CloseHandle failed in CloseOutputPort (error code 4)");
                    }

                    // From WriteTextString
                    case SERIAL_NO_OUTPUT_PORT_FOUND:
                    {
                        throw new Exception ("Serial: No output port found in WriteTextString (error code 5)");
                    }
                    case SERIAL_NO_OUTPUT_TEXT_PROVIDED:
                    {
                        throw new Exception ("Serial: No output text provided in WriteTextString (error code 6)");
                    }
                    case SERIAL_WRITEFILE_FAILED:
                    {
                        throw new Exception ("Serial: WriteFile failed in WriteTextString (error code 7)");
                    }

                    // From OpenNamedPort
                    case SERIAL_CLOSEOUTPUTPORT_FAILED:
                    {
                        throw new Exception ("Serial: CloseOutputPort failed in OpenNamedPort (error code 8)");
                    }
                    case SERIAL_UNABLE_TO_OPEN_NAMED_PORT:
                    {
                        throw new Exception ("Serial: Unable to open named port in OpenNamedPort (error code 9)");
                    }

                    // From SetComPortSettings
                    case SERIAL_GETCOMMSTATE_FAILED:
                    {
                        throw new Exception ("Serial: GetCommState failed in SetComPortSettings (error code 10)");
                    }
                    case SERIAL_SETCOMMSTATE_FAILED:
                    {
                        throw new Exception ("Serial: SetCommState failed in SetComPortSettings (error code 11)");
                    }
                    case SERIAL_SETCOMMTIMEOUTS_FAILED:
                    {
                        throw new Exception ("Serial: SetCommTimeouts failed in SetComPortSettings (error code 12)");
                    }

                    // From WaitForPlotter
                    case SERIAL_PLOTTER_IS_BUSY:
                    {
                        throw new Exception ("Serial: Timeout waiting for plotter to finish drawing plot (error code -1");
                    }
                }
            }
        }

        private static void ShowStatus (string strHeader, SerialPort sp)
        {
            bool bBreak         = sp.BreakState;     // 0
            bool bIsOpen        = sp.IsOpen;         // 1
            bool bCD            = sp.CDHolding;      // 2
            bool bCTS           = sp.CtsHolding;     // 3
            bool bDSR           = sp.DsrHolding;     // 4
            bool bDTR           = sp.DtrEnable;      // 5
            bool bRTS           = (sp.Handshake == Handshake.None || sp.Handshake == Handshake.XOnXOff ? sp.RtsEnable : false); // 6
            int iBytes          = sp.BytesToRead;    // 7
            int iReadBufferSize = sp.ReadBufferSize; // 8
            StopBits sb         = sp.StopBits;       // 9
            Parity parity       = sp.Parity;         // 10
            int iBaud           = sp.BaudRate;       // 11

            Console.WriteLine (string.Format ("{0}: Break: {1}, Open: {2}, CD: {3}, CTS: {4}, DSR: {5}, DTR: {6}, RTS: {7}, " +
                                              "bytes: {8}, BuffSize: {9}, SB: {10}, Par: {11}, baud: {12}", strHeader,
                                              (bBreak ? 'T' : 'F'), (bIsOpen ? 'T' : 'F'), (bCD ? 'T' : 'F'), (bCTS ? 'T' : 'F'),
                                              (bDSR ? 'T' : 'F'), (bDTR ? 'T' : 'F'), (bRTS ? 'T' : 'F'), iBytes,
                                              iReadBufferSize, sb.ToString (), parity.ToString (), iBaud));
        }

        private void ConsoleOutput (string strOutput, bool bForceOutput = false)
        {
            if (m_bOutputTrace || bForceOutput)
            {
                Console.WriteLine (strOutput);
            }
        }
        #endregion
    }

    public class CParallelPortDriver
    {
        #region Member Data
        private const string STRING_GENERIC_TEXT_ONLY    = "Generic / Text Only";
        private const string STRING_PARALLEL_PORT_WRITER = "ParallelPortWriter";
        private const string STRING_USB                  = "USB";
        private const string STRING_RAW                  = "RAW";

        private static SortedList<string, int> m_slQueueEntries         = new SortedList<string, int> ();
        private static SortedDictionary<string, string> m_sdOutputQueue = new SortedDictionary<string, string> ();
        private static string m_strPortName = "";
        private static int                     m_iQueueLength   = -1;
        private static int                     m_iQueueSize     = 0;
        #endregion

        public static string GetPrinterName ()
        {
            LocalPrintServer lps = new LocalPrintServer ();
            PrintQueueCollection pqc = lps.GetPrintQueues ();

            foreach (PrintQueue pq in pqc)
            {
                PrintDriver pd = pq.QueueDriver;
                //Console.WriteLine ("PrinterDriver: " + pq.Name);

                if (pd.Name == STRING_GENERIC_TEXT_ONLY     &&
                    pq.QueuePort.Name.Contains (STRING_USB) && // USB002
                    pq.Name.Contains (STRING_USB))             // "USB Parallel Port"
                {
                    m_strPortName = pq.Name;
                    return pq.Name;
                }
            }

            Console.WriteLine ("No parallel port defined:");
            Console.WriteLine ("  Printer name: USB Parallel Port");
            Console.WriteLine ("  Printer Port: USB###");
            Console.WriteLine ("  Printer Driver: Generic / Text Only");
            Console.WriteLine ();
            throw (new Exception ("No parallel port defined"));
        }

        public static int GetQueueLength ()
        {
            return m_iQueueLength < 0 ? GetPrintQueueJobCount () : m_iQueueLength;
        }

        public static int GetQueueSize (bool bForceUpdate = false)
        {
            if (bForceUpdate)
            {
                GetPrintQueueJobCount ();
            }

            return m_iQueueSize;
        }

        public static bool PrintToSpooler (string strTextToPrint, string strDocName = STRING_PARALLEL_PORT_WRITER,
                                           string strPortName = "") //, bool bDateStamp = false, bool bTimeStamp = true)
        {
            try
            {
                // Create the printer server and print queue objects
                LocalPrintServer lps = new LocalPrintServer ();

                if (strPortName == null ||
                    strPortName.Length == 0)
                {
                    if (m_strPortName == null ||
                        m_strPortName.Length == 0)
                    {
                        GetPrinterName ();
                    }
                    strPortName = m_strPortName;
                }
                
                PrintQueue pqPlotter = lps.GetPrintQueue (strPortName);

                // Call AddJob
                PrintSystemJobInfo psjiPlotter = pqPlotter.AddJob (strDocName);

                // Write a Byte buffer to the JobStream and close the stream
                Stream stPlotter = psjiPlotter.JobStream;

                // Convert to Byte array
                Byte[] yaTextToPrint = new Byte[strTextToPrint.Length];
                for (int iIdx = 0; iIdx < strTextToPrint.Length; ++iIdx)
                {
                    yaTextToPrint[iIdx] = (Byte)strTextToPrint[iIdx];
                }

                // Write to plotter
                stPlotter.Write (yaTextToPrint, 0, yaTextToPrint.Length);
                stPlotter.Close ();

                // Add new document to queue lists
                m_slQueueEntries.Add (strDocName, strTextToPrint.Length);
                m_sdOutputQueue.Add (strDocName, strTextToPrint);
                GetPrintQueueJobCount ();
            }
            catch (Exception e)
            {
                Console.WriteLine ("** Exception in PrintToSpooler (): " + e.Message, true);
                if (e.InnerException != null)
                {
                    Console.WriteLine ("  " + e.InnerException.Message, true);
                }
                Console.WriteLine (e.StackTrace, true);

                return false;
            }

            return true;
        }

        public static int GetPrintQueueJobCount ()
        {
            if (m_strPortName == "")
            {
                return -1;
            }

            // Get local print server
            LocalPrintServer lps = new LocalPrintServer ();
            int iJobCount = 0;

            // Load queue for specific printer
            PrintQueue pq = null;
            try
            {
                pq = lps.GetPrintQueue (m_strPortName, new string[0] { });
                iJobCount = pq.NumberOfJobs;
            }
            catch (Exception)
            {
                return 0; // Fail silently and report 0 print jobs, which is probably accurate anyway or the call would have worked
            }

            m_iQueueSize = 0;
            PrintJobInfoCollection pjic = pq.GetPrintJobInfoCollection ();
            foreach (PrintSystemJobInfo psji in pjic)
            {
                //int iJobSize = psji.JobSize; // First job size always reported as 0
                //Console.WriteLine ("Document name: " + psji.Name);
                if (m_slQueueEntries.ContainsKey (psji.Name))
                {
                    int iDocSize = m_slQueueEntries[psji.Name];
                    //Console.WriteLine ("Added {0} for {1}", iDocSize, psji.Name);
                    m_iQueueSize += iDocSize;
                }
            }

            m_iQueueLength = iJobCount;
            return iJobCount;
        }

        public static void ClearOldHPGLStrings ()
        {
            while (m_sdOutputQueue.Count > m_iQueueLength)
            {
                m_sdOutputQueue.Remove(m_sdOutputQueue.ElementAt (0).Key);
            }
        }

        public static string GetHPGLString (int iIdx, int iMaxLength = 0)
        {
            while (m_sdOutputQueue.Count > m_iQueueLength)
            {
                m_sdOutputQueue.Remove(m_sdOutputQueue.ElementAt (0).Key);
            }

            if (m_sdOutputQueue.Count > iIdx)
            {
                string strHPGL = m_sdOutputQueue.ElementAt (iIdx).Value;

                strHPGL = (iMaxLength > 0 && iMaxLength < strHPGL.Length) ? strHPGL.Substring (0, iMaxLength) : strHPGL;

                return strHPGL;
            }

            return "";
        }

        public static CHPGL.SPrintQueueEntry[] GetPrintQueueJobList ()
        {
            List<CHPGL.SPrintQueueEntry> lstrPrintDocuments = new List<CHPGL.SPrintQueueEntry> ();

            if (m_strPortName == "")
            {
                return lstrPrintDocuments.ToArray ();
            }

            // Get local print server
            LocalPrintServer lps = new LocalPrintServer ();

            // Load queue for specific printer
            PrintQueue pq = null;
            try
            {
                pq = lps.GetPrintQueue (m_strPortName, new string[0] { });
                //JobCount = pq.NumberOfJobs;
            }
            catch (Exception)
            {
                // Fail silently and report 0 print jobs, which is probably accurate anyway or the call would have worked
                return lstrPrintDocuments.ToArray ();
            }

            m_iQueueSize = 0;
            PrintJobInfoCollection pjic = pq.GetPrintJobInfoCollection ();
            foreach (PrintSystemJobInfo psji in pjic)
            {
                int iDocSize = 0;
                //int iJobSize = psji.JobSize; // First job size always reported as 0
                //Console.WriteLine ("Document name: " + psji.Name);
                if (m_slQueueEntries.ContainsKey (psji.Name))
                {
                    iDocSize = m_slQueueEntries[psji.Name];
                    //Console.WriteLine ("Added {0} for {1}", iDocSize, psji.Name);
                    m_iQueueSize += iDocSize;

                    CHPGL.SPrintQueueEntry objPrintQueueEntry = new CHPGL.SPrintQueueEntry ();
                    objPrintQueueEntry.strDocumentName = psji.Name;
                    objPrintQueueEntry.iDocumentLength = iDocSize;
                    lstrPrintDocuments.Add (objPrintQueueEntry);
                }
                else
                {
                    iDocSize = ExtractDocSize (psji.Name);
                    //Console.WriteLine ("Added {0} for {1}", iDocSize, psji.Name);
                    m_iQueueSize += iDocSize;

                    CHPGL.SPrintQueueEntry objPrintQueueEntry = new CHPGL.SPrintQueueEntry ();
                    objPrintQueueEntry.strDocumentName = psji.Name;
                    objPrintQueueEntry.iDocumentLength = iDocSize;
                    
                    lstrPrintDocuments.Add (objPrintQueueEntry);
                    m_slQueueEntries.Add (psji.Name, iDocSize);
                }
            }

            return lstrPrintDocuments.ToArray ();
        }

        public static void ClearPrintQueue ()
        {
            //Console.WriteLine ("ClearPrintQueue ()");

            if (m_strPortName == null ||
                m_strPortName == "")
            {
                return;
            }

            m_slQueueEntries.Clear ();
            m_sdOutputQueue.Clear ();

            // Get local print server
            LocalPrintServer lps = new LocalPrintServer ();

            // Load queue for specific printer
            PrintQueue pq = lps.GetPrintQueue (m_strPortName, new string[0] { });

            PrintJobInfoCollection pjic = pq.GetPrintJobInfoCollection ();
            foreach (PrintSystemJobInfo psji in pjic)
            {
                psji.Cancel ();
            }
        }

        public static bool WaitForSpooler (int iTimeOutSeconds, bool bBufferSpaceTrace)
        {
            //Console.WriteLine (string.Format ("WaitForSpooler ({0} seconds, bBufferSpaceTrace: {1})", iTimeOutSeconds, bBufferSpaceTrace.ToString ()));

            int iStartCount       = Environment.TickCount & Int32.MaxValue,
                iEndCount         = iStartCount + (iTimeOutSeconds * 1000),
                iOutputQueueCount = 0;

            bool bPlotterBusy = GetPrintQueueJobCount () > 0;

            while (bPlotterBusy)
            {
                int iTickCount        = Environment.TickCount & Int32.MaxValue;
                int iTickRemaining    = iEndCount - iTickCount;
                int iSecondsRemaining = iTickRemaining / 1000;
                //Console.WriteLine (string.Format ("  WaitForSpooler: {0}", iSecondsRemaining));

                if (iTickCount > iEndCount)
                {
                    //Console.WriteLine ("  WaitForSpooler exits w/false");
                    return false;
                }

                iOutputQueueCount = GetPrintQueueJobCount ();
                //Console.WriteLine ("  WaitForSpooler: " + iOutputQueueCount.ToString ());
                bPlotterBusy = iOutputQueueCount > 0;
            }

            //Console.WriteLine ("  WaitForSpooler exits w/true");
            return true;
        }

        private static int ExtractDocSize (string strDocName)
        {
            int iLastUnderscoreIdx = strDocName.LastIndexOf ('_');
            if (iLastUnderscoreIdx > 0 &&
                iLastUnderscoreIdx < strDocName.Length)
            {
                string strDocLength = strDocName.Substring (iLastUnderscoreIdx + 1);
                int iDocLength = CGenericMethods.SafeConvertToInt (strDocLength);
                return iDocLength;
            }

            return 0;
        }

        //public static void ShowPrintProperties (PrintQueue pq)
        //{
        //    Console.WriteLine ("AveragePagesPerMinute: " + pq.AveragePagesPerMinute.ToString ());
        //    Console.WriteLine ("ClientPrintSchemaVersion: " + pq.ClientPrintSchemaVersion.ToString ());
        //    Console.WriteLine ("Comment: " + pq.Comment);
        //    Console.WriteLine ("DefaultPriority: " + pq.DefaultPriority.ToString ());
        //    Console.WriteLine ("Description: " + pq.Description);
        //    Console.WriteLine ("FullName: " + pq.FullName);
        //    Console.WriteLine ("HasPaperProblem: " + pq.HasPaperProblem.ToString ());
        //    Console.WriteLine ("HasToner: " + pq.HasToner.ToString ());
        //    Console.WriteLine ("InPartialTrust: " + pq.InPartialTrust.ToString ());
        //    Console.WriteLine ("IsBidiEnabled: " + pq.IsBidiEnabled.ToString ());
        //    Console.WriteLine ("IsBusy: " + pq.IsBusy.ToString ());
        //    Console.WriteLine ("IsDevQueryEnabled: " + pq.IsDevQueryEnabled.ToString ());
        //    Console.WriteLine ("IsDirect: " + pq.IsDirect.ToString ());
        //    Console.WriteLine ("IsDoorOpened: " + pq.IsDoorOpened.ToString ());
        //    Console.WriteLine ("IsHidden: " + pq.IsHidden.ToString ());
        //    Console.WriteLine ("IsInError: " + pq.IsInError.ToString ());
        //    Console.WriteLine ("IsInitializing: " + pq.IsInitializing.ToString ());
        //    Console.WriteLine ("IsIOActive: " + pq.IsIOActive.ToString ());
        //    Console.WriteLine ("IsManualFeedRequired: " + pq.IsManualFeedRequired.ToString ());
        //    Console.WriteLine ("IsNotAvailable: " + pq.IsNotAvailable.ToString ());
        //    Console.WriteLine ("IsOffline: " + pq.IsOffline.ToString ());
        //    Console.WriteLine ("IsOutOfMemory: " + pq.IsOutOfMemory.ToString ());
        //    Console.WriteLine ("IsOutOfPaper: " + pq.IsOutOfPaper.ToString ());
        //    Console.WriteLine ("IsOutputBinFull: " + pq.IsOutputBinFull.ToString ());
        //    Console.WriteLine ("IsPaperJammed: " + pq.IsPaperJammed.ToString ());
        //    Console.WriteLine ("IsPaused: " + pq.IsPaused.ToString ());
        //    Console.WriteLine ("IsPendingDeletion: " + pq.IsPendingDeletion.ToString ());
        //    Console.WriteLine ("IsPowerSaveOn: " + pq.IsPowerSaveOn.ToString ());
        //    Console.WriteLine ("IsPrinting: " + pq.IsPrinting.ToString ());
        //    Console.WriteLine ("IsProcessing: " + pq.IsProcessing.ToString ());
        //    Console.WriteLine ("IsPublished: " + pq.IsPublished.ToString ());
        //    Console.WriteLine ("IsQueued: " + pq.IsQueued.ToString ());
        //    Console.WriteLine ("IsRawOnlyEnabled: " + pq.IsRawOnlyEnabled.ToString ());
        //    Console.WriteLine ("IsServerUnknown: " + pq.IsServerUnknown.ToString ());
        //    Console.WriteLine ("IsShared: " + pq.IsShared.ToString ());
        //    Console.WriteLine ("IsTonerLow: " + pq.IsTonerLow.ToString ());
        //    Console.WriteLine ("IsWaiting: " + pq.IsWaiting.ToString ());
        //    Console.WriteLine ("IsWarmingUp: " + pq.IsWarmingUp.ToString ());
        //    Console.WriteLine ("IsXpsDevice: " + pq.IsXpsDevice.ToString ());
        //    Console.WriteLine ("KeepPrintedJobs: " + pq.KeepPrintedJobs.ToString ());
        //    Console.WriteLine ("Location: " + pq.Location);
        //    //Console.WriteLine ("MaxPrintSchemaVersion: " + pq.MaxPrintSchemaVersion.ToString ());
        //    Console.WriteLine ("Name: " + pq.Name);
        //    Console.WriteLine ("NeedUserIntervention: " + pq.NeedUserIntervention.ToString ());
        //    Console.WriteLine ("NumberOfJobs: " + pq.NumberOfJobs.ToString ());
        //    Console.WriteLine ("PagePunt: " + pq.PagePunt.ToString ());
        //    Console.WriteLine ("PrintingIsCancelled: " + pq.PrintingIsCancelled.ToString ());
        //    Console.WriteLine ("Priority: " + pq.Priority.ToString ());
        //    Console.WriteLine ("ScheduleCompletedJobsFirst: " + pq.ScheduleCompletedJobsFirst.ToString ());
        //    Console.WriteLine ("SeparatorFile: " + pq.SeparatorFile);
        //    Console.WriteLine ("ShareName: " + pq.ShareName);
        //    Console.WriteLine ("StartTimeOfDay: " + pq.StartTimeOfDay.ToString ());
        //    Console.WriteLine ("UntilTimeOfDay: " + pq.UntilTimeOfDay.ToString ());
        //    Console.WriteLine ("PrintJobSettings.Description: " + pq.CurrentJobSettings.Description);
        //    //Console.WriteLine ("HostingPrintServer.BeepEnabled: " + pq.HostingPrintServer.BeepEnabled.ToString ());
        //    //Console.WriteLine ("HostingPrintServer.DefaultSpoolDirectory: " + pq.HostingPrintServer.DefaultSpoolDirectory);
        //    //Console.WriteLine ("HostingPrintServer.MajorVersion: " + pq.HostingPrintServer.MajorVersion.ToString ());
        //    //Console.WriteLine ("HostingPrintServer.MinorVersion: " + pq.HostingPrintServer.MinorVersion.ToString ());
        //    //Console.WriteLine ("HostingPrintServer.NetPopup: " + pq.HostingPrintServer.NetPopup.ToString ());
        //    //Console.WriteLine ("HostingPrintServer.RestartJobOnPoolEnabled: " + pq.HostingPrintServer.RestartJobOnPoolEnabled.ToString ());
        //    //Console.WriteLine ("HostingPrintServer.RestartJobOnPoolTimeout: " + pq.HostingPrintServer.RestartJobOnPoolTimeout.ToString ());
        //    //Console.WriteLine ("HostingPrintServer.SubSystemVersion: " + pq.HostingPrintServer.SubSystemVersion.ToString ());
        //    //Console.WriteLine ("HostingPrintServer.DefaultPortThreadPriority: " + pq.HostingPrintServer.DefaultPortThreadPriority.ToString ());
        //    //Console.WriteLine ("HostingPrintServer.DefaultSchedulerPriority: " + pq.HostingPrintServer.DefaultSchedulerPriority.ToString ());
        //    //Console.WriteLine ("HostingPrintServer.EventLog: " + pq.HostingPrintServer.EventLog.ToString ());
        //    //Console.WriteLine ("HostingPrintServer.PortThreadPriority: " + pq.HostingPrintServer.PortThreadPriority.ToString ());
        //    //Console.WriteLine ("HostingPrintServer.SchedulerPriority: " + pq.HostingPrintServer.SchedulerPriority.ToString ());
        //    Console.WriteLine ("QueueAttributes: " + pq.QueueAttributes.ToString ());
        //    Console.WriteLine ("QueueStatus: " + pq.QueueStatus.ToString ());
        //}
    }
}
