using System;
using System.Collections.Generic;
using System.Text;

using WrapperBaseClass;
using ParallelPortWriter;
using SerialPortWriter;
using HPGL;

namespace PlotterBuffer
{
    public class CPlotterBuffer
    {
        bool m_bIsSorted = false;
        private List<string> m_lstrHPGLCommands = new List<string> ();

        public int BufferEnqueue (String strCommand)
        {
            m_lstrHPGLCommands.Add (strCommand);
            m_bIsSorted = false;

            return m_lstrHPGLCommands.Count;
        }

        public void BufferSort ()
        {
            BufferSort (false);
        }

        // This version will break other status settings, like
        //    character set selection,
        //    scaling,
        //    clipping windows,
        //    absolute/relative motion selection, ...
        //    basically anything that is set by instructions that don't move the pen
        public void BufferSort (bool bOptimizePenTravel)
        {
            if (m_bIsSorted)
                return;

            List<string> strlCommands = new List<string> ();
            int iStart = 0;
            int iNextSep = 0;
            int iPen = -1;

            StringBuilder sbNoPen = new StringBuilder ();
            StringBuilder sbPen1 = new StringBuilder ();
            StringBuilder sbPen2 = new StringBuilder ();
            StringBuilder sbPen3 = new StringBuilder ();
            StringBuilder sbPen4 = new StringBuilder ();
            StringBuilder sbPen5 = new StringBuilder ();
            StringBuilder sbPen6 = new StringBuilder ();
            StringBuilder sbPen7 = new StringBuilder ();
            StringBuilder sbPen8 = new StringBuilder ();
            StringBuilder sbPen0 = new StringBuilder ();

            foreach (string str in m_lstrHPGLCommands)
            {
                // split line into individual HPGL instructions
                iStart = 0;
                iNextSep = 0;
                while (iNextSep < str.Length - 1)
                {
                    bool bIsSPcmd = false;
                    iNextSep = str.IndexOf (';', iStart);
                    string strCommand = str.Substring (iStart, iNextSep - iStart + 1);
                    iStart = iNextSep + 1;

                    if (strCommand.Length == 4 &&
                        strCommand[0] == 'S' &&
                        strCommand[1] == 'P' &&
                        strCommand[3] == ';')
                    {
                        char cPen = strCommand[2];
                        if (cPen >= '0' && cPen <= '8')
                        {
                            bIsSPcmd = true;
                            iPen = Convert.ToInt16 (cPen) - 0x30;
                        }
                    }
                    else  if (strCommand.Length == 3 &&
                              strCommand[0] == 'S' &&
                              strCommand[1] == 'P' &&
                              strCommand[2] == ';')
                    {
                        bIsSPcmd = true;
                        iPen = 0;
                    }
                    //Console.WriteLine (strCommand);

                    if (iPen == 1)
                    {
                        if (!bIsSPcmd || sbPen1.Length == 0)
                            sbPen1.Append (strCommand);
                    }
                    else if (iPen == 2)
                    {
                        if (!bIsSPcmd || sbPen2.Length == 0)
                            sbPen2.Append (strCommand);
                    }
                    else if (iPen == 3)
                    {
                        if (!bIsSPcmd || sbPen3.Length == 0)
                            sbPen3.Append (strCommand);
                    }
                    else if (iPen == 4)
                    {
                        if (!bIsSPcmd || sbPen4.Length == 0)
                            sbPen4.Append (strCommand);
                    }
                    else if (iPen == 5)
                    {
                        if (!bIsSPcmd || sbPen5.Length == 0)
                            sbPen5.Append (strCommand);
                    }
                    else if (iPen == 6)
                    {
                        if (!bIsSPcmd || sbPen6.Length == 0)
                            sbPen6.Append (strCommand);
                    }
                    else if (iPen == 7)
                    {
                        if (!bIsSPcmd || sbPen7.Length == 0)
                            sbPen7.Append (strCommand);
                    }
                    else if (iPen == 8)
                    {
                        if (!bIsSPcmd || sbPen8.Length == 0)
                            sbPen8.Append (strCommand);
                    }
                    else if (iPen == 0)
                    {
                        if (!bIsSPcmd || sbPen0.Length == 0)
                            sbPen0.Append (strCommand);
                    }
                    else
                    {
                        sbNoPen.Append (strCommand);
                    }
                }
            }

            m_lstrHPGLCommands.Clear ();

            if (sbNoPen.Length > 0)
                m_lstrHPGLCommands.Add (sbNoPen.ToString ());

            if (sbPen1.Length > 4)
                m_lstrHPGLCommands.Add (sbPen1.ToString ());

            if (sbPen2.Length > 4)
                m_lstrHPGLCommands.Add (sbPen2.ToString ());

            if (sbPen3.Length > 4)
                m_lstrHPGLCommands.Add (sbPen3.ToString ());

            if (sbPen4.Length > 4)
                m_lstrHPGLCommands.Add (sbPen4.ToString ());

            if (sbPen5.Length > 4)
                m_lstrHPGLCommands.Add (sbPen5.ToString ());

            if (sbPen6.Length > 4)
                m_lstrHPGLCommands.Add (sbPen6.ToString ());

            if (sbPen7.Length > 4)
                m_lstrHPGLCommands.Add (sbPen7.ToString ());

            if (sbPen8.Length > 4)
                m_lstrHPGLCommands.Add (sbPen8.ToString ());

            if (sbPen0.Length > 0)
                m_lstrHPGLCommands.Add (sbPen0.ToString ());

            m_bIsSorted = true;
        }

        public bool IsSorted ()
        {
            return m_bIsSorted;
        }

        public String BufferPrint (bool bSerial)
        {
            WrapperBase wb = null;

            if (bSerial)
            {
                wb = new SerialWrapper ();
            }
            else
            {
                wb = new ParallelWrapper ();
            }

            string strPortName = "";

            foreach (string str in m_lstrHPGLCommands)
            {
                Console.WriteLine (str);
                wb.WriteTextString (str);
            }

            if (bSerial)
            {
                if (wb.WaitForPlotter (2000, true))
                {
                    string strStatus = wb.QueryPlotter (CHPGL.OutputIdentification ());
                    Console.WriteLine ("OutputIdentification: " + strStatus);
                    strStatus = wb.QueryPlotter (CHPGL.OutputStatus ());
                    Console.WriteLine ("OutputStatus: " + strStatus);
                    strStatus = wb.QueryPlotter (CHPGL.OutputFactors ());
                    Console.WriteLine ("OutputFactors: " + strStatus);
                    strStatus = wb.QueryPlotter (CHPGL.OutputError ());
                    Console.WriteLine ("OutputError: " + strStatus);
                    strStatus = wb.QueryPlotter (CHPGL.OutputActualPosition ());
                    Console.WriteLine ("OutputActualPosition: " + strStatus);
                    strStatus = wb.QueryPlotter (CHPGL.OutputCommandedPosition ());
                    Console.WriteLine ("OutputCommandedPosition: " + strStatus);
                    strStatus = wb.QueryPlotter (CHPGL.OutputOptions ());
                    Console.WriteLine ("OutputOptions: " + strStatus);
                    strStatus = wb.QueryPlotter (CHPGL.OutputHardClipLimits ());
                    Console.WriteLine ("OutputHardClipLimits: " + strStatus);
                }
                else
                {
                    Console.WriteLine ("WaitForPlotter () timed out.");
                }
            }

            if (bSerial)
                wb.CloseOutputPort ();

            return strPortName;
        }

        public int GetLineCount ()
        {
            return m_lstrHPGLCommands.Count;
        }

        public int GetInstructionCount ()
        {
            int iInstructionCount = 0;
            foreach (string str in m_lstrHPGLCommands)
            {
                int iIndex = 0;
                while (iIndex >= 0)
                {
                    iIndex = str.IndexOf (';', iIndex + 1);
                    if (iIndex > 0)
                        ++iInstructionCount;
                }
            }

            return iInstructionCount;
        }

        public int GetBufferSize ()
        {
            int iByteCount = 0;
            foreach (string str in m_lstrHPGLCommands)
            {
                iByteCount += str.Length;
            }

            return iByteCount;
        }

        public void ClearBuffer ()
        {
            m_lstrHPGLCommands.Clear ();
        }
    }
}
