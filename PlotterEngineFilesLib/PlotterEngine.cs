using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
// using System.Management;
using System.Printing;
using System.Text;
using System.Threading;

using PlotterDriver;
using HPGL;

// TODO:
//   [x] Code to sort by position with each separate group, keeping all groups separate
//   [x] Implement class CChartStatistics for CChartRulerFrameShape.CreateXYRulerPair
//   [x] Clean up tests in TestCGenericRectangleMethods
//   [x] Implement tests used in TestPlotEllipse in CGenericRectangleMethods class
//   [x] Add test cases for PointsAreInRect methods
//   [x] Add test cases for overloaded GetBoundingRectangle methods
//   [-] Add test case for overloaded GetBoundingRectangle using PlotFourQuadrants
//   [ ] Copy CRect class & rect struct from MFC and adapt to C#
//       [ ] Extend functionality to include all that C# Rectangle class has
//       [ ] Add functionality as desired
//   [ ] Add sanity tests to CChartRulerFrameShape.CreateXYRulerPair and the CChartRulerFrameShape constructor
//   [ ] Add sanity tests to all other constructors and methods that need protection
//   [?] Add GetBoundingRectangle as abstract method (or simple call to CGenericMethods.GetBoundingRectangle) to CDrawingShapeElement
//   [ ] Review CComplexLinesShape constructors for any parameters that should have -1 as
//       default values to control initialization to non-zero dymanic default values
//   [ ] Make CHPGL.GenPenHolderPoint aware of page orientation (and scaling?)
//
//   For each derived shape class:
//                                         +------------------------------ CCircleShape
//                                         |   +-------------------------- CArcShape
//                                         |   |   +---------------------- CRectangleShape
//                                         |   |   |   +------------------ CWedgeShape
//                                         |   |   |   |   +-------------- CTextLabel
//                                         |   |   |   |   |   +---------- CComplexLinesShape
//                                         |   |   |   |   |   |   +------ CChartRulerFrameShape
//                                         |   |   |   |   |   |   |   +-- CShapeConfigSettings (needs DI The Absolute Direction Instruction, SI The Absolute Character Size Instruction)
//                                         v   v   v   v   v   v   v   v
//     GetHPGL                            [x] [x] [x] [x] [x] [x] [x] [-]
//     ComputeStartAndEndPoints           [x] [x] [x] [x] [x] [x] [x] [-]
//     Input sanity checks                [ ] [ ] [ ] [ ] [ ] [ ] [ ] [ ]
//     GetBoundingRectangle (?)           [ ] [ ] [ ] [ ] [ ] [ ] [ ] [ ]
//     RotatePoints                       [ ] [ ] [ ] [ ] [ ] [ ] [ ] [-]
//     ResizeOrMove                       [ ] [ ] [ ] [ ] [ ] [ ] [ ] [-]
//     ReversePointSequence               [ ] [ ] [ ] [ ] [ ] [x] [ ] [-]
//     SortToOptimizePenUpTravelDistance  [-] [-] [-] [-] [-] [?] [-] [-]

namespace PlotterEngine
{
    public class CPlotterEngine
    {
        #region Member Data
        public enum ESortMode
        {
            EUnsorted,
            ESortByPenOnly,
            ESortByGroupOnly,
            ESortByPenAndDistance,
            ESortByGroupAndDistance
        }

        public enum EPlotterPort
        {
            ENoOutput,
            ESerialPort,
            EParallelPort,
            EAutoDetect,
            EUnspecified
        }

        private static int    s_iGroupNumber = 0;

        private static CSerialPortDriver    s_objSerialPortDriver       = null;
        private StringBuilder        m_sbOutputHPGL              = new StringBuilder ();
        private EPenSelect           m_eCurrentPen               = EPenSelect.ESelectNoPen;
        private EPlotterPort         m_ePlotterPort              = EPlotterPort.EUnspecified;
        private EPlotterPort         m_eLastPlotterPort          = EPlotterPort.EUnspecified;
        private string               m_strOutputFilename         = "";
        private string               m_strOutputFolder           = "";
        private int                  m_iSequenceNo               = 0;
        private bool                 m_bSortedByPenAndDistance   = false;
        private bool                 m_bSortedByGroupAndDistance = false;

        private const string HPGL_FOLDER             = @"D:\SoftwareDev\PlotterWriter\HPGL\";
        private const string HPGL_EXTENSION          = ".hpgl";
        private const string STRING_SIMPLE_HPGL_TEST = "SP1;PA5000,5000;CI500;PA0,0;SP;";

        private SortedList<int,    CDrawingShapeElement> m_slElementsInSeq     = new SortedList<int, CDrawingShapeElement> ();
        private SortedList<string, CDrawingShapeElement> m_slElementsAllPens   = new SortedList<string, CDrawingShapeElement> ();
        private SortedList<string, CDrawingShapeElement> m_slElementsAllGroups = new SortedList<string, CDrawingShapeElement> ();

        private SortedList<string, CDrawingShapeElement> m_slElementsPen1 = new SortedList<string, CDrawingShapeElement> ();
        private SortedList<string, CDrawingShapeElement> m_slElementsPen2 = new SortedList<string, CDrawingShapeElement> ();
        private SortedList<string, CDrawingShapeElement> m_slElementsPen3 = new SortedList<string, CDrawingShapeElement> ();
        private SortedList<string, CDrawingShapeElement> m_slElementsPen4 = new SortedList<string, CDrawingShapeElement> ();
        private SortedList<string, CDrawingShapeElement> m_slElementsPen5 = new SortedList<string, CDrawingShapeElement> ();
        private SortedList<string, CDrawingShapeElement> m_slElementsPen6 = new SortedList<string, CDrawingShapeElement> ();
        private SortedList<string, CDrawingShapeElement> m_slElementsPen7 = new SortedList<string, CDrawingShapeElement> ();
        private SortedList<string, CDrawingShapeElement> m_slElementsPen8 = new SortedList<string, CDrawingShapeElement> ();

        private List<CDrawingShapeElement> m_lSortedElementsPen1 = new List<CDrawingShapeElement> ();
        private List<CDrawingShapeElement> m_lSortedElementsPen2 = new List<CDrawingShapeElement> ();
        private List<CDrawingShapeElement> m_lSortedElementsPen3 = new List<CDrawingShapeElement> ();
        private List<CDrawingShapeElement> m_lSortedElementsPen4 = new List<CDrawingShapeElement> ();
        private List<CDrawingShapeElement> m_lSortedElementsPen5 = new List<CDrawingShapeElement> ();
        private List<CDrawingShapeElement> m_lSortedElementsPen6 = new List<CDrawingShapeElement> ();
        private List<CDrawingShapeElement> m_lSortedElementsPen7 = new List<CDrawingShapeElement> ();
        private List<CDrawingShapeElement> m_lSortedElementsPen8 = new List<CDrawingShapeElement> ();

        private List<EPenSelect> m_lpsSequentialPens = new List<EPenSelect> ();
        private List<EPenSelect> m_lpsRandomizedPens = new List<EPenSelect> ();

        private static CPlotterEngine s_objPlotterEngine = null;

        // Imported from PlotterWriterConsoleUI
        private int    m_iPenCount       = 0;
        private int    m_iPenIndex       = 0;
        private bool   m_bRandomPen      = false;
        private bool   m_bSequentialPen  = false;

        private ESortMode    m_eSortMode = ESortMode.ESortByPenAndDistance;
        private static DateTime s_dtNow = DateTime.Now;
        private static int s_iKey = s_dtNow.Minute + s_dtNow.Second + s_dtNow.Millisecond;
        private Random m_rand = new Random (s_iKey);
        #endregion

        private CPlotterEngine ()
        {
        }

        private CPlotterEngine (EPlotterPort ePlotterPort)
        {
            m_ePlotterPort = ePlotterPort;
            if (m_ePlotterPort != EPlotterPort.ESerialPort   &&
                m_ePlotterPort != EPlotterPort.EParallelPort &&
                m_ePlotterPort != EPlotterPort.EAutoDetect   &&
                m_ePlotterPort != EPlotterPort.ENoOutput)
            {
                throw new Exception ("Invalied plotter port specified in CPlotterEngine constructor");
            }

            if (ePlotterPort == EPlotterPort.ESerialPort ||
                ePlotterPort == EPlotterPort.EParallelPort)
            {
                m_eLastPlotterPort = ePlotterPort;
            }
        }

        public static CPlotterEngine GetPlotterEngine (EPlotterPort ePlotterPort)
        {
            if (s_objPlotterEngine == null)
            {
                s_objPlotterEngine = new CPlotterEngine (ePlotterPort);
            }

            return s_objPlotterEngine;
        }

        public bool SetPlotterPort (EPlotterPort ePlotterPort, bool bShowWarningMessage)
        {
            return SetPlotterPort (ePlotterPort, CSerialPortDriver.EClosePortMethod.EClosePortOnly, bShowWarningMessage);
        }

        public bool SetPlotterPort (EPlotterPort ePlotterPort,
                                    CSerialPortDriver.EClosePortMethod eClosePortMethod = CSerialPortDriver.EClosePortMethod.EClosePortOnly,
                                    bool bShowWarningMessage                            = true)
        {
            if (ePlotterPort == EPlotterPort.EUnspecified)
            {
                return false;
            }

            // If port selection matches what's already there, do nothing
            if (ePlotterPort == m_ePlotterPort)
            {
                return true;
            }

            // If IsSerialPortOpen () and ESerialPort, do nothing
            if (ePlotterPort == EPlotterPort.ESerialPort &&
                IsSerialPortOpen ())
            {
                return true;
            }

            // If IsSerialPortOpen () and EAutoDetect, set to ESerialPort
            if (ePlotterPort == EPlotterPort.EAutoDetect &&
                IsSerialPortOpen ())
            {
                m_ePlotterPort     = EPlotterPort.ESerialPort;
                m_eLastPlotterPort = m_ePlotterPort;
                return true;
            }

            // If IsSerialPortOpen () and neither ESerialPort nor EAutoDetect, close serial port
            if (ePlotterPort != EPlotterPort.ESerialPort &&
                ePlotterPort != EPlotterPort.EAutoDetect &&
                IsSerialPortOpen ())
            {
                if (eClosePortMethod == CSerialPortDriver.EClosePortMethod.EClosePortOnly &&
                    (!s_objSerialPortDriver.IsPlotterQueueEmpty () ||
                     !s_objSerialPortDriver.IsPlotterBufferEmpty ()))
                {
                    return false;
                }

                s_objSerialPortDriver.CloseOutputPort (eClosePortMethod);
                s_objSerialPortDriver = null;

                if (m_eLastPlotterPort == EPlotterPort.ESerialPort && bShowWarningMessage)
                {
                    ConsoleOutput ("The plotter must be reset before it will accept input on the parallel port");
                }

                m_ePlotterPort     = EPlotterPort.ESerialPort;
                m_eLastPlotterPort = m_ePlotterPort;
                return true;
            }

            // If !IsSerialPortOpen () and ESerialPort, create CSerialPortDriver object and call IsPortOpen (), then set to ESerialPort
            if (ePlotterPort == EPlotterPort.ESerialPort &&
                !IsSerialPortOpen ())
            {
                s_objSerialPortDriver = new CSerialPortDriver (false);
                if (IsSerialPortOpen ())
                {
                    //if (m_eLastPlotterPort == EPlotterPort.EParallelPort)
                    //{
                    //    ConsoleOutput ("The plotter must be reset before it will accept input on the serial port");
                    //}
                    m_ePlotterPort     = ePlotterPort;
                    m_eLastPlotterPort = m_ePlotterPort;
                    return true;
                }

                return false;
            }

            if (ePlotterPort == EPlotterPort.EParallelPort)
            {
                if (IsSerialPortOpen ())
                {
                    if (eClosePortMethod == CSerialPortDriver.EClosePortMethod.EClosePortOnly &&
                        (!s_objSerialPortDriver.IsPlotterQueueEmpty () ||
                         !s_objSerialPortDriver.IsPlotterBufferEmpty ()))
                    {
                        return false;
                    }

                    s_objSerialPortDriver.CloseOutputPort (eClosePortMethod);
                    s_objSerialPortDriver = null;
                }

                if (m_eLastPlotterPort == EPlotterPort.ESerialPort && bShowWarningMessage)
                {
                    ConsoleOutput ("The plotter must be reset before it will accept input on the parallel port");
                }

                m_ePlotterPort     = ePlotterPort;
                m_eLastPlotterPort = m_ePlotterPort;
                return true;
            }

            if (ePlotterPort == EPlotterPort.EAutoDetect)
            {
                //if (m_objSerialPortDriver != null)
                //{
                //    if (m_objSerialPortDriver.IsPortOpen ())
                //    {
                //        m_ePlotterPort = EPlotterPort.ESerialPort;
                //        return true;
                //    }
                //}
                if (IsSerialPortOpen ())
                {
                    m_ePlotterPort = EPlotterPort.ESerialPort;
                    return true;
                }

                s_objSerialPortDriver = new CSerialPortDriver (false);
                if (IsSerialPortOpen ())
                {
                    m_ePlotterPort = EPlotterPort.ESerialPort;
                    return true;
                }
                else
                {
                    if (m_eLastPlotterPort == EPlotterPort.EParallelPort && bShowWarningMessage)
                    {
                        ConsoleOutput ("The plotter must be reset before it will accept input on the serial port");
                    }
                    m_ePlotterPort     = EPlotterPort.EParallelPort;
                    m_eLastPlotterPort = m_ePlotterPort;
                    return true;
                }
            }

            if (ePlotterPort == EPlotterPort.ENoOutput)
            {
                if (IsSerialPortOpen ())
                {
                    if (eClosePortMethod == CSerialPortDriver.EClosePortMethod.EClosePortOnly &&
                        (!s_objSerialPortDriver.IsPlotterQueueEmpty () ||
                         !s_objSerialPortDriver.IsPlotterBufferEmpty ()))
                    {
                        return false;
                    }

                    s_objSerialPortDriver.CloseOutputPort (eClosePortMethod);
                    s_objSerialPortDriver = null;
                }

                m_ePlotterPort = ePlotterPort;
                return true;
            }

            return false;
        }

        public void SetPauseAfterNewPen (bool bPauseAfterNewPen)
        {
            if (IsSerialPortOpen ())
            {
                s_objSerialPortDriver.SetPauseAfterNewPen (bPauseAfterNewPen);
            }
        }

        public bool GetPauseAfterNewPen ()
        {
            if (IsSerialPortOpen ())
            {
                return s_objSerialPortDriver.GetPauseAfterNewPen ();
            }

            return false;
        }

        private void ConsoleOutput (string strOutput)
        {
            Console.WriteLine (strOutput);
        }

        public bool IsSerialPortOpen ()
        {
            return s_objSerialPortDriver != null &&
                   s_objSerialPortDriver.IsPortOpen ();
        }

        public EPlotterPort GetPlotterPort ()
        {
            return m_ePlotterPort;
        }

        public string GetPortName ()
        {
            if (IsSerialPortOpen ())
            {
                return s_objSerialPortDriver.GetPortName ();
            }
            else
            {
                return CParallelPortDriver.GetPrinterName ();
            }
        }

        public ESortMode GetSortMode ()
        {
            return m_eSortMode;
        }

        public void CloseSerialPort ()
        {
            if (IsSerialPortOpen ())
            {
                s_objSerialPortDriver.CloseOutputPort ();
            }
        }

        public void SetSortMode (ESortMode eSortMode)
        {
            m_eSortMode = eSortMode;
        }

        public CHPGL.SPrintQueueEntry[] GetPrintQueueJobList ()
        {
            if (m_ePlotterPort == EPlotterPort.ESerialPort)
            {
                return s_objSerialPortDriver.GetPrintQueueJobList ();
            }
            else if (m_ePlotterPort == EPlotterPort.EParallelPort)
            {
                return CParallelPortDriver.GetPrintQueueJobList ();
            }

            return null;
        }

        public void ClearOldHPGLStrings ()
        {
            if (m_ePlotterPort == EPlotterPort.EParallelPort)
            {
                CParallelPortDriver.ClearOldHPGLStrings ();
            }
        }

        public int GetQueueLength ()
        {
            if (m_ePlotterPort == EPlotterPort.ESerialPort)
            {
                return s_objSerialPortDriver.GetQueueLength ();
            }
            else if (m_ePlotterPort == EPlotterPort.EParallelPort)
            {
                return CParallelPortDriver.GetQueueLength ();
            }

            return 0;
        }

        public int GetBytesInPlotterBuffer ()
        {
            if (m_ePlotterPort == EPlotterPort.ESerialPort)
            {
                return s_objSerialPortDriver.GetPlotterBufferSize () - s_objSerialPortDriver.GetPlotterBufferSpace ();
            }

            return -1;
        }

        public void ShowOutputQueue ()
        {
            if (m_ePlotterPort == EPlotterPort.ESerialPort)
            {
                s_objSerialPortDriver.ShowOutputQueue ();
            }
        }

        public int GetQueueSize (bool bForceUpdate = false)
        {
            if (m_ePlotterPort == EPlotterPort.ESerialPort)
            {
                return s_objSerialPortDriver.GetQueueSize ();
            }
            else if (m_ePlotterPort == EPlotterPort.EParallelPort)
            {
                return CParallelPortDriver.GetQueueSize (bForceUpdate);
            }

            return 0;
        }

        public string GetHPGLString (int iIdx, int iMaxLength = 0)
        {
            if (m_ePlotterPort == EPlotterPort.ESerialPort)
            {
                return s_objSerialPortDriver.GetHPGLString (iIdx, iMaxLength);
            }
            else
            {
                return CParallelPortDriver.GetHPGLString (iIdx, iMaxLength);
            }
        }

        public void SetOutputPath (string strOutputPath)
        {
            if (Directory.Exists (strOutputPath))
            {
                m_strOutputFolder = strOutputPath;
            }
        }

        public string GetOutputPath ()
        {
            return m_strOutputFolder;
        }

        public int  GetPenCount ()
        {
            return m_iPenCount;
        }

        public int  GetPenIndex ()
        {
            return m_iPenIndex;
        }

        public bool GetRandomPen ()
        {
            return m_bRandomPen;
        }

        public bool GetSequentialPen ()
        {
            return m_bSequentialPen;
        }

        public void SetPenCount (int iPenCount)
        {
            m_iPenCount = iPenCount;
        }

        public void SetPenIndex (int iPenIndex)
        {
            m_iPenIndex = iPenIndex;
        }

        public void SetRandomPen (bool bRandomPen)
        {
            m_bRandomPen = bRandomPen;
        }

        public void SetSequentialPen (bool bSequentialPen)
        {
            m_bSequentialPen = bSequentialPen;
        }

        public static EPenSelect GetNextPen (EPenSelect epsNextPen = EPenSelect.ESelectNoPen)
        {
            return s_objPlotterEngine.GetNextPen ((int)epsNextPen);
        }

        public EPenSelect GetNextPen (int iNextPen)
        {
            if (iNextPen == (int)EPenSelect.ESelectNoPen)
            {
                return EPenSelect.ESelectPen1;
            }

            int iNextPenArg = iNextPen;
            try
            {
                if (m_bSequentialPen ||
                    iNextPen == (int)EPenSelect.ESelectAllPens)
                {
                    if (m_lpsSequentialPens.Count == 0)
                    {
                        for (int iPen = 1; iPen <= m_iPenCount; ++iPen)
                        {
                            m_lpsSequentialPens.Add ((EPenSelect)iPen);
                        }
                    }

                    EPenSelect epsReturn = m_lpsSequentialPens[0];
                    m_lpsSequentialPens.RemoveAt (0);

                    //Console.WriteLine ("  CPlotterEngine.GetNextPen ({0}) returns {1}", iNextPenArg, epsReturn.ToString ());
                    return epsReturn;
                }
                else if (m_bRandomPen ||
                         iNextPen == (int)EPenSelect.ESelectPenRandom)
                {
                    EPenSelect epsReturn = EPenSelect.ESelectNoPen;

                    if (m_lpsRandomizedPens.Count > 0)
                    {
                        epsReturn = m_lpsRandomizedPens.ElementAt (0);
                        m_lpsRandomizedPens.RemoveAt (0);
                    }
                    else
                    {
                        m_lpsSequentialPens.Clear ();
                        for (int iPen = 1; iPen <= m_iPenCount; ++iPen)
                        {
                            m_lpsSequentialPens.Add ((EPenSelect)iPen);
                        }

                        while (m_lpsSequentialPens.Count > 0)
                        {
                            if (m_lpsSequentialPens.Count > 1)
                            {
                                int iRandomIdx = m_rand.Next (1, m_lpsSequentialPens.Count);
                                Debug.Assert (iRandomIdx > 0 && iRandomIdx < m_lpsSequentialPens.Count);
                                EPenSelect ePenSelect = m_lpsSequentialPens[iRandomIdx];
                                m_lpsSequentialPens.RemoveAt (iRandomIdx);
                                m_lpsRandomizedPens.Add (ePenSelect);
                            }
                            else
                            {
                                m_lpsRandomizedPens.Add (m_lpsSequentialPens[0]);
                                m_lpsSequentialPens.RemoveAt (0);
                            }
                        }

                        epsReturn = m_lpsRandomizedPens.ElementAt (0);
                        m_lpsRandomizedPens.RemoveAt (0);
                    }

                    Debug.Assert (epsReturn != EPenSelect.ESelectNoPen);
                    //Console.WriteLine ("  CPlotterEngine.GetNextPen ({0}) returns {1}", iNextPenArg, epsReturn.ToString ());
                    return epsReturn;
                }
                else
                {
                    return (EPenSelect)(iNextPen % m_iPenCount) + 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine ("** Exception in GetNextPen (): " + e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine ("  " + e.InnerException.Message);
                }
                Console.WriteLine (e.StackTrace);
                return (EPenSelect)1;
            }
        }

        public void AddElement (CDrawingShapeElement objDrawingShapeElement)
        {
            objDrawingShapeElement.ComputeStartAndEndPoints ();

            EPenSelect ePenSelect = objDrawingShapeElement.GetPenSelection ();
            // Key string format:
            //   1. Group ID #
            //   2. Optimized-position sequence #
            //   3. Original sequence generated here
            string strKey = string.Format ("{0:000}-9999-{1:0000} ({2})", objDrawingShapeElement.GetSortGroup (), ++m_iSequenceNo, ePenSelect.ToString ());
            //Console.WriteLine ("Adding new element: " + strKey);

            m_slElementsInSeq.Add (m_iSequenceNo, objDrawingShapeElement);
            m_slElementsAllPens.Add (strKey, objDrawingShapeElement);

            if (ePenSelect == EPenSelect.ESelectPen1)
            {
                m_slElementsPen1.Add (strKey, objDrawingShapeElement);
            }
            else if (ePenSelect == EPenSelect.ESelectPen2)
            {
                m_slElementsPen2.Add (strKey, objDrawingShapeElement);
            }
            else if (ePenSelect == EPenSelect.ESelectPen3)
            {
                m_slElementsPen3.Add (strKey, objDrawingShapeElement);
            }
            else if (ePenSelect == EPenSelect.ESelectPen4)
            {
                m_slElementsPen4.Add (strKey, objDrawingShapeElement);
            }
            else if (ePenSelect == EPenSelect.ESelectPen5)
            {
                m_slElementsPen5.Add (strKey, objDrawingShapeElement);
            }
            else if (ePenSelect == EPenSelect.ESelectPen6)
            {
                m_slElementsPen6.Add (strKey, objDrawingShapeElement);
            }
            else if (ePenSelect == EPenSelect.ESelectPen7)
            {
                m_slElementsPen7.Add (strKey, objDrawingShapeElement);
            }
            else if (ePenSelect == EPenSelect.ESelectPen8)
            {
                m_slElementsPen8.Add (strKey, objDrawingShapeElement);
            }
            else
            {
                throw new Exception ("Invalid pen number in CPlotterEngine.AddElement");
            }
        }

        public void AddElements (CDrawingShapeElement[] objaDrawingShapeElements)
        {
            foreach (CDrawingShapeElement dse in objaDrawingShapeElements)
            {
                AddElement (dse);
            }
        }

        public void SortAllEntriesByDistance ()
        {
            if (m_eSortMode == ESortMode.EUnsorted)
            {
                return;
            }

            // Only sort by distance if already grouped by pen selection
            SortByPenAndDistance (m_slElementsPen1, ref m_lSortedElementsPen1);
            SortByPenAndDistance (m_slElementsPen2, ref m_lSortedElementsPen2);
            SortByPenAndDistance (m_slElementsPen3, ref m_lSortedElementsPen3);
            SortByPenAndDistance (m_slElementsPen4, ref m_lSortedElementsPen4);
            SortByPenAndDistance (m_slElementsPen5, ref m_lSortedElementsPen5);
            SortByPenAndDistance (m_slElementsPen6, ref m_lSortedElementsPen6);
            SortByPenAndDistance (m_slElementsPen7, ref m_lSortedElementsPen7);
            SortByPenAndDistance (m_slElementsPen8, ref m_lSortedElementsPen8);

            // Also sort by distance if only grouped by group number and all use the same pen
            if (AllEntriesUseSamePen (m_slElementsAllPens))
            {
                SortByGroupAndDistance (m_slElementsAllPens, ref m_slElementsAllGroups);
            }
        }

        private bool AllEntriesUseSamePen (SortedList<string, CDrawingShapeElement> slElements)
        {
            if (slElements.Count > 1)
            {
                EPenSelect ePenSelect = slElements.ElementAt (0).Value.GetPenSelection ();

                foreach (KeyValuePair<string, CDrawingShapeElement> kvp in slElements)
                {
                    if (kvp.Value.GetPenSelection () != ePenSelect)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsValidKeyString (string strKey)
        {
            //  0123456789012
            // "000-9999-0000"
            if (strKey.Length == 13)
            {
                if (strKey[3] != '-' ||
                    strKey[8] != '-')
                {
                    return false;
                }

                if (!CGenericMethods.IsNumeric (strKey.Substring (0, 3)) ||
                    !CGenericMethods.IsNumeric (strKey.Substring (4, 4)) ||
                    !CGenericMethods.IsNumeric (strKey.Substring (9, 4)))
                {
                    return false;
                }
            }

            return true;
        }

        private void SortByPenAndDistance (SortedList<string, CDrawingShapeElement> slElements, ref List<CDrawingShapeElement> lElementsSorted)
        {
            // For each element:
            //   Determine EPenSelect value for starting Y position, save as last location
            //   Compute distance from last location
            //   Select element with nearest starting point and eliminate from candidate list
            // Repeat until only one element remains which becomes the last

            if (m_eSortMode != ESortMode.ESortByPenAndDistance)
            {
                return;
            }

            if (slElements.Count == 1)
            {
                string strKey = slElements.ElementAt (0).Key;
                lElementsSorted.Add (slElements[strKey]);
            }
            else if (slElements.Count > 1)
            {
                Point ptLast = new Point (0, 0);
                Point ptHolder = new Point (0, 0);
                SortedList<string, CDrawingShapeElement> slElementsUnsorted = new SortedList<string, CDrawingShapeElement> ();
                SortedList<int, string> slTempSort = new SortedList<int, string> ();

                foreach (KeyValuePair<string, CDrawingShapeElement> kvp in slElements)
                {
                    CDrawingShapeElement dse = kvp.Value;
                    slElementsUnsorted.Add (kvp.Key, kvp.Value);
                }

                // Determine EPenSelect value for starting Y position, save as last location
                EPenSelect ePenSelect = slElementsUnsorted.ElementAt (0).Value.GetPenSelection ();
                ptHolder = ptLast = CHPGL.GenPenHolderPoint (ePenSelect);

                while (slElementsUnsorted.Count > 0)
                {
                    // Compute distance from last location
                    for (int iIdx = 0; iIdx < slElementsUnsorted.Count; ++iIdx)
                    {
                        Point ptElementStart = slElementsUnsorted.ElementAt (iIdx).Value.GetStartPoint ();
                        int iDistance = CGenericMethods.GetWindowDeltaDiagonal (ptLast, ptElementStart);
                        while (slTempSort.ContainsKey (iDistance))
                        {
                            ++iDistance; // Prevent duplicate key collisions
                        }
                        slTempSort.Add (iDistance, slElementsUnsorted.ElementAt (iIdx).Key);
                    }

                    // Select element with nearest starting point and eliminate from candidate list
                    //foreach (KeyValuePair<int, string> kvp in slTempSort)
                    //{
                    //    Console.WriteLine (string.Format ("{0}, {1}, {2}, {3}", kvp.Key, kvp.Value,
                    //                                      slElementsUnsorted[kvp.Value].GetStartPoint (),
                    //                                      slElementsUnsorted[kvp.Value].GetEndPoint ()));
                    //}
                    string strKey = slTempSort.ElementAt (0).Value;
                    ptLast = slElementsUnsorted[strKey].GetEndPoint ();
                    lElementsSorted.Add (slElementsUnsorted[strKey]);
                    //Console.WriteLine ("Sorted elements: " + lElementsSorted.Count);
                    slElementsUnsorted.Remove (strKey);
                    slTempSort.Clear ();
                    m_bSortedByPenAndDistance = true;
                }

                //Console.WriteLine ("Sorted entries:");
                //foreach (CDrawingShapeElement dse in lElementsSorted)
                //{
                //    Console.WriteLine (string.Format ("{0}, {1}, {2}", dse.GetStartPoint (), dse.GetEndPoint (),
                //                                      CGenericMethods.GetWindowDeltaDiagonal (ptHolder, dse.GetStartPoint ())));
                //}
                //Console.WriteLine ();
            }
        }

        private void SortByGroupAndDistance (SortedList<string, CDrawingShapeElement> slElementsInput, ref SortedList<string, CDrawingShapeElement> slElementsSorted)
        {
            // For each element:
            //   Determine EPenSelect value for starting Y position, save as last location
            //   Compute distance from last location
            //   Select element with nearest starting point and eliminate from candidate list
            // Repeat until only one element remains which becomes the last

            if (m_eSortMode != ESortMode.ESortByGroupAndDistance)
            {
                return;
            }

            if (slElementsInput.Count == 1)
            {
                slElementsSorted.Add (slElementsInput.ElementAt (0).Key, slElementsInput.ElementAt (0).Value);
            }
            else if (slElementsInput.Count > 1)
            {
                Point ptLast = new Point (0, 0);
                Point ptHolder = new Point (0, 0);
                SortedList<string, CDrawingShapeElement> slElementsUnsorted   = new SortedList<string, CDrawingShapeElement> ();
                SortedList<string, CDrawingShapeElement> slTempGroupUnsorted  = new SortedList<string, CDrawingShapeElement> ();
                SortedList<int, string>                  slTempSortByDistance = new SortedList<int, string> ();

                foreach (KeyValuePair<string, CDrawingShapeElement> kvp in slElementsInput)
                {
                    CDrawingShapeElement dse = kvp.Value;
                    slElementsUnsorted.Add (kvp.Key, kvp.Value);
                    if (!IsValidKeyString (kvp.Key))
                    {
                        throw new Exception ("Invalid key string found in SortByGroupAndDistance ()");
                    }
                }

                // Determine EPenSelect value for starting Y position, save as last location
                EPenSelect ePenSelect = slElementsUnsorted.ElementAt (0).Value.GetPenSelection ();
                ptHolder = ptLast = CHPGL.GenPenHolderPoint (ePenSelect);

                while (slElementsUnsorted.Count > 0)
                {
                    // Sort each group separately
                    // slElementsUnsorted is a temporary buffer of all elements to be sorted
                    // slTempGroupUnsorted is a temporary buffer holding all entries to be sorted with matching group numbers
                    string strGroupNew  = slElementsUnsorted.ElementAt (0).Key.Substring (0, 3);
                    string strGroupLast = strGroupNew;
                    while (slElementsUnsorted.Count > 0)
                    {
                        KeyValuePair<string, CDrawingShapeElement> kvp = slElementsUnsorted.ElementAt (0);
                        strGroupLast = strGroupNew;
                        strGroupNew  = kvp.Key.Substring (0, 3);
                        if (strGroupNew != strGroupLast)
                        {
                            break;
                        }

                        slTempGroupUnsorted.Add (kvp.Key, kvp.Value);
                        slElementsUnsorted.RemoveAt (0);
                    }

                    int iNewSeqNo = 0;
                    while (slTempGroupUnsorted.Count > 0)
                    {
                        // Compute distance from last location
                        for (int iIdx = 0; iIdx < slTempGroupUnsorted.Count; ++iIdx)
                        {
                            Point ptElementStart = slTempGroupUnsorted.ElementAt (iIdx).Value.GetStartPoint ();
                            int iDistance = CGenericMethods.GetWindowDeltaDiagonal (ptLast, ptElementStart);
                            while (slTempSortByDistance.ContainsKey (iDistance))
                            {
                                ++iDistance; // Prevent duplicate key collisions
                            }
                            slTempSortByDistance.Add (iDistance, slTempGroupUnsorted.ElementAt (iIdx).Key);
                        }

                        // Select element with nearest starting point and eliminate from candidate list
                        //Console.WriteLine ();
                        //foreach (KeyValuePair<int, string> kvp in slTempSortByDistance)
                        //{
                        //    Console.WriteLine (string.Format ("{0}, {1}, {2}, {3}", kvp.Key, kvp.Value,
                        //                                      slTempGroupUnsorted[kvp.Value].GetStartPoint (),
                        //                                      slTempGroupUnsorted[kvp.Value].GetEndPoint ()));
                        //}

                        string strOldKey = slTempSortByDistance.ElementAt (0).Value;
                        CDrawingShapeElement objDrawingShapeElement = slTempGroupUnsorted[strOldKey];
                        string strNewKey = string.Format ("{0:000}-{1:0000}-{2}", objDrawingShapeElement.GetSortGroup (), ++iNewSeqNo, strOldKey.Substring (9, 4));

                        ptLast = slTempGroupUnsorted[strOldKey].GetEndPoint ();
                        slElementsSorted.Add (strNewKey, objDrawingShapeElement);

                        //Console.WriteLine ("Sorted elements: " + slElementsSorted.Count);

                        slTempGroupUnsorted.Remove (strOldKey);
                        slTempSortByDistance.Clear ();
                        m_bSortedByGroupAndDistance = true;
                    }
                }

                //Console.WriteLine ("Sorted entries:");
                //foreach (KeyValuePair<string, CDrawingShapeElement> kvp in slElementsSorted)
                //{
                //    Console.WriteLine (string.Format ("{0}, {1}, {2}, {3}", kvp.Key, kvp.Value.GetStartPoint (), kvp.Value.GetEndPoint (),
                //                                      CGenericMethods.GetWindowDeltaDiagonal (ptHolder, kvp.Value.GetStartPoint ())));
                //}
                //Console.WriteLine ();
            }
        }

        private bool WriteToPlotter (string strHPGL, string strDocumentName = "")
        {
            //Console.WriteLine ("WriteToPlotter (" + (IsSerial () ? "Serial, " : "Parallel, ") + strHPGL + ")");
            //Console.WriteLine ("WriteToPlotter (" + ((m_ePlotterPort == EPlotterPort.ESerialPort) ? "Serial, " : "Parallel, \"") + strHPGL + "\")");
            //if (strHPGL.Contains (";SP"))
            //{
            //    int iIdx = strHPGL.IndexOf (";SP");

            //    Console.WriteLine ("  New pen: " + strHPGL.Substring (iIdx + 3, 1));
            //}

            if (strDocumentName == null ||
                strDocumentName == "")
            {
                strDocumentName = ExtractFilenameOnly (m_strOutputFilename);
            }

            strDocumentName = CGenericMethods.FormatPlotDocName (strDocumentName, /*++m_iDocumentSequence, ++m_iDocumentCount,*/ strHPGL.Length);

            if (m_strOutputFolder.Length > 0)
            {
                m_sbOutputHPGL.Append (strHPGL);
            }

            if (m_ePlotterPort == EPlotterPort.EParallelPort)
            {
                return CParallelPortDriver.PrintToSpooler (strHPGL, strDocumentName);
            }
            else if (IsSerialPortOpen ())
            {
                strHPGL = CGenericMethods.PrependCommentHPGL (strHPGL, strDocumentName);
                return s_objSerialPortDriver.WriteTextString (strHPGL, strDocumentName);
            }

            return false;
        }

        private void WriteOutputFile (string strOutputFilename = "")
        {
            //Console.WriteLine ("WriteOutputFile (" + strOutputFilename + ")");

            if (strOutputFilename == "")
            {
                strOutputFilename = m_strOutputFilename;
            }

            try
            {
                //Console.WriteLine ("  Trying File.WriteAllText (" + strOutputFilename + "), ...)");
                File.WriteAllText (FormatOutputFilename (strOutputFilename), m_sbOutputHPGL.ToString ());
            }
            catch (System.Runtime.InteropServices.SEHException seh)
            {
                Console.WriteLine ("SEH: " + seh.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine ("** Exception in WriteOutputFile (): " + e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine ("  " + e.InnerException.Message);
                }
                Console.WriteLine (e.StackTrace);
            }

            m_sbOutputHPGL.Clear ();
        }

        private string ExtractFilenameOnly (string strFilePath)
        {
            int iLastSlashIdx = strFilePath.LastIndexOf ('\\');
            int iLastDotIdx   = strFilePath.LastIndexOf ('.');

            if (iLastDotIdx > 0 &&
                iLastDotIdx < strFilePath.Length)
            {
                strFilePath = strFilePath.Substring (0, iLastDotIdx);
            }

            if (iLastSlashIdx > 0 &&
                iLastSlashIdx < strFilePath.Length)
            {
                strFilePath = strFilePath.Substring (iLastSlashIdx + 1);
            }

            return strFilePath;
        }

        private string FormatOutputFilename (string strOutputFilenameRaw)
        {
            int iLastSlashIdx = strOutputFilenameRaw.LastIndexOf ('\\');
            int iLastDotIdx   = strOutputFilenameRaw.LastIndexOf ('.');

            if (iLastSlashIdx > 0 &&
                iLastDotIdx   > 0)
            {
                return strOutputFilenameRaw;
            }

            string strOutputFilePath  = (iLastSlashIdx > -1 ? strOutputFilenameRaw.Substring (0, iLastSlashIdx)  : "");
            string strOutputFilename  = (iLastSlashIdx > -1 ? strOutputFilenameRaw.Substring (iLastSlashIdx + 1) : strOutputFilenameRaw);
            string strOutputExtension = (iLastDotIdx   > -1 && iLastDotIdx < strOutputFilenameRaw.Length - 1 ? strOutputFilenameRaw.Substring (iLastDotIdx) : "");

            if (strOutputFilePath.Length == 0)
            {
                if (Directory.Exists (m_strOutputFolder))
                {
                    strOutputFilePath = m_strOutputFolder;
                }
                else if (Directory.Exists (HPGL_FOLDER))
                {
                    strOutputFilePath = HPGL_FOLDER;
                }
                else
                {
                    strOutputFilePath = Directory.GetCurrentDirectory ();
                }
            }

            StringBuilder sbOutputFilename = new StringBuilder (strOutputFilePath);
            if (sbOutputFilename.Length > 0 &&
                strOutputFilePath[strOutputFilePath.Length - 1] != '\\')
            {
                sbOutputFilename.Append ('\\');
            }
            sbOutputFilename.Append (strOutputFilename);
            if (strOutputExtension.Length == 0)
            {
                sbOutputFilename.Append (HPGL_EXTENSION);
            }

            //Console.WriteLine ("  FormatOutputFilename (" + strOutputFilenameRaw + ") returns: " + sbOutputFilename.ToString ());
            return sbOutputFilename.ToString ();
        }

        public void OutputHPGL (string strOutputText, string strOutputFilename)
        {
            if (m_ePlotterPort == EPlotterPort.ENoOutput &&
                strOutputFilename == "")
            {
                throw new Exception ("No output file and no output port spedified");
            }

            if (m_strOutputFilename == "" && strOutputFilename.Length > 0)
            {
                m_strOutputFilename = FormatOutputFilename (strOutputFilename);
            }

            WriteToPlotter (strOutputText, strOutputFilename);

            if (strOutputFilename.Length == 0)
            {
                WriteToPlotter (CHPGL.PlotAbsolute (CHPGL.GenPenHolderPoint (m_eCurrentPen)) + CHPGL.SelectPen (0), strOutputFilename);
            }
        }

        public void OutputHPGL (string strOutputFilename = "", ESortMode eSortMode = ESortMode.ESortByPenAndDistance)
        {
            //Console.WriteLine ("OutputHPGL (" + ePlotterPort.ToString () + ", " + strOutputFilename + ", ...)");

            if (m_ePlotterPort == EPlotterPort.ENoOutput &&
                strOutputFilename == "")
            {
                throw new Exception ("No output file and no output port specified");
            }

            if (m_strOutputFilename == "")
            {
                m_strOutputFilename = FormatOutputFilename (strOutputFilename);
            }

            if (eSortMode == ESortMode.ESortByPenAndDistance &&
                m_bSortedByPenAndDistance)
            {
                OutputHPGL (m_lSortedElementsPen1, strOutputFilename);
                OutputHPGL (m_lSortedElementsPen2, strOutputFilename);
                OutputHPGL (m_lSortedElementsPen3, strOutputFilename);
                OutputHPGL (m_lSortedElementsPen4, strOutputFilename);
                OutputHPGL (m_lSortedElementsPen5, strOutputFilename);
                OutputHPGL (m_lSortedElementsPen6, strOutputFilename);
                OutputHPGL (m_lSortedElementsPen7, strOutputFilename);
                OutputHPGL (m_lSortedElementsPen8, strOutputFilename);
            }
            else if (eSortMode == ESortMode.ESortByPenOnly)
            {
                OutputHPGL (m_slElementsPen1, strOutputFilename);
                OutputHPGL (m_slElementsPen2, strOutputFilename);
                OutputHPGL (m_slElementsPen3, strOutputFilename);
                OutputHPGL (m_slElementsPen4, strOutputFilename);
                OutputHPGL (m_slElementsPen5, strOutputFilename);
                OutputHPGL (m_slElementsPen6, strOutputFilename);
                OutputHPGL (m_slElementsPen7, strOutputFilename);
                OutputHPGL (m_slElementsPen8, strOutputFilename);
            }
            else if (eSortMode == ESortMode.ESortByGroupAndDistance &&
                     m_bSortedByGroupAndDistance)
            {
                OutputHPGL (m_slElementsAllGroups, strOutputFilename);
            }
            else if (eSortMode == ESortMode.ESortByGroupOnly)
            {
                OutputHPGL (m_slElementsAllPens, strOutputFilename);
            }
            else
            {
                OutputHPGL (m_slElementsInSeq, strOutputFilename);
            }

            WriteToPlotter (CHPGL.PlotAbsolute (CHPGL.GenPenHolderPoint (m_eCurrentPen)) +
                            CHPGL.SelectPen (0)                                          +
                            CHPGL.PlotAbsolute (0, 0));
            m_eCurrentPen = EPenSelect.ESelectNoPen;
        }

        private void OutputHPGL (SortedList<string, CDrawingShapeElement> slElements, string strDocumentName = "")
        {
            //Console.WriteLine ("OutputHPGL (SortedList<string, CDrawingShapeElement>");

            for (int iIdx = 0; iIdx < slElements.Count; ++iIdx)
            {
                CDrawingShapeElement dse = slElements.ElementAt(iIdx).Value;
                bool bNewPen  = (m_eCurrentPen != dse.GetPenSelection ());
                m_eCurrentPen = dse.GetPenSelection ();
                if (bNewPen)
                {
                    string strNewPen = CHPGL.PlotAbsolute (0, CHPGL.GetPenYAxisValue ((int)m_eCurrentPen)) +
                                       CHPGL.SelectPen ((int)m_eCurrentPen);
                    //Console.WriteLine (strNewPen);
                    WriteToPlotter (strNewPen, strDocumentName);
                }

                bool bPlotterConfigsHPGL = (iIdx == 0 ||
                                            !dse.IsConfigEqualTo (slElements.ElementAt(iIdx - 1).Value));

                string strHPGL = dse.GetHPGL (bPlotterConfigsHPGL);
                //Console.WriteLine (strHPGL);
                WriteToPlotter (strHPGL, strDocumentName);
            }
        }

        private void OutputHPGL (List<CDrawingShapeElement> ldseElements, string strDocumentName = "")
        {
            //Console.WriteLine ("OutputHPGL (SortedList<string, CDrawingShapeElement>");

            for (int iIdx = 0; iIdx < ldseElements.Count; ++iIdx)
            {
                CDrawingShapeElement dse = ldseElements.ElementAt(iIdx);
                bool bNewPen = (m_eCurrentPen != dse.GetPenSelection ());
                m_eCurrentPen = dse.GetPenSelection ();
                if (bNewPen)
                {
                    string strNewPen = CHPGL.PlotAbsolute (0, CHPGL.GetPenYAxisValue ((int)m_eCurrentPen)) +
                                       CHPGL.SelectPen ((int)m_eCurrentPen);
                    //Console.WriteLine (strNewPen);
                    WriteToPlotter (strNewPen, strDocumentName);
                }

                bool bPlotterConfigsHPGL = (iIdx == 0 ||
                                            !dse.IsConfigEqualTo (ldseElements.ElementAt(iIdx - 1)));

                //Console.WriteLine (dse.GetHPGL (bPlotterConfigsHPGL));
                WriteToPlotter (dse.GetHPGL (bPlotterConfigsHPGL), strDocumentName);
            }
        }

        private void OutputHPGL (SortedList<int, CDrawingShapeElement> slElements, string strDocumentName = "")
        {
            //Console.WriteLine ("OutputHPGL (SortedList<int, CDrawingShapeElement>)");

            for (int iIdx = 0; iIdx < slElements.Count; ++iIdx)
            {
                CDrawingShapeElement dse = slElements.ElementAt(iIdx).Value;
                bool bNewPen = (m_eCurrentPen != dse.GetPenSelection ());
                m_eCurrentPen = dse.GetPenSelection ();
                if (bNewPen)
                {
                    string strNewPen = CHPGL.SelectPen ((int)m_eCurrentPen) +
                                       CHPGL.PlotAbsolute (CHPGL.GenPenHolderPoint ((int)m_eCurrentPen));
                    //Console.WriteLine (strNewPen);
                    WriteToPlotter (strNewPen, strDocumentName);
                    //Console.WriteLine (CHPGL.SelectPen ((int)m_eCurrentPen));
                }

                bool bPlotterConfigsHPGL = (iIdx == 0 ||
                                            !dse.IsConfigEqualTo (slElements.ElementAt(iIdx - 1).Value));

                string strHPGL = dse.GetHPGL (bPlotterConfigsHPGL);
                //Console.WriteLine (strHPGL);
                WriteToPlotter (strHPGL, strDocumentName);
                //Console.WriteLine ();
            }
        }

        public void ClearAll ()
        {
            m_iSequenceNo = 0;
            m_bSortedByGroupAndDistance = false;
            m_bSortedByPenAndDistance   = false;

            m_slElementsInSeq.Clear ();
            m_slElementsAllPens.Clear ();
            m_slElementsAllGroups.Clear ();

            m_slElementsPen1.Clear ();
            m_slElementsPen2.Clear ();
            m_slElementsPen3.Clear ();
            m_slElementsPen4.Clear ();
            m_slElementsPen5.Clear ();
            m_slElementsPen6.Clear ();
            m_slElementsPen7.Clear ();
            m_slElementsPen8.Clear ();

            m_lSortedElementsPen1.Clear ();
            m_lSortedElementsPen2.Clear ();
            m_lSortedElementsPen3.Clear ();
            m_lSortedElementsPen4.Clear ();
            m_lSortedElementsPen5.Clear ();
            m_lSortedElementsPen6.Clear ();
            m_lSortedElementsPen7.Clear ();
            m_lSortedElementsPen8.Clear ();

            m_lpsSequentialPens.Clear ();
            m_lpsRandomizedPens.Clear ();

            if (m_sbOutputHPGL.Length > 0)
            {
                WriteOutputFile ();
            }

            m_strOutputFilename = "";
        }

        public void ClearPens ()
        {
            m_lpsSequentialPens.Clear ();
            m_lpsRandomizedPens.Clear ();
        }

        public void AbortPlot ()
        {
            if (m_ePlotterPort == EPlotterPort.EParallelPort)
            {
                //Console.WriteLine ("In CPlotterEngine.AbortPlot ()");

                CParallelPortDriver.ClearPrintQueue ();

                string strHPGL = CHPGL.EscAbortGraphicControl ();
                CParallelPortDriver.PrintToSpooler (strHPGL, "000_Abort1_" + CGenericMethods.FillStringLength (strHPGL.Length.ToString (), 5));
                CParallelPortDriver.PrintToSpooler (strHPGL, "000_Abort2_" + CGenericMethods.FillStringLength (strHPGL.Length.ToString (), 5));
                CParallelPortDriver.PrintToSpooler (strHPGL, "000_Abort3_" + CGenericMethods.FillStringLength (strHPGL.Length.ToString (), 5));

                strHPGL = CHPGL.EscReset ();
                CParallelPortDriver.PrintToSpooler (strHPGL, "000_Abort4_" + CGenericMethods.FillStringLength (strHPGL.Length.ToString (), 5));

                strHPGL = CHPGL.Initialize () + CHPGL.SelectPen () + CHPGL.PlotAbsolute (0, 0);
                CParallelPortDriver.PrintToSpooler (strHPGL, "000_Abort5_" + CGenericMethods.FillStringLength (strHPGL.Length.ToString (), 5));

                //Console.WriteLine ("  Exiting CPlotterEngine.AbortPlot ()");
            }
            else if (IsSerialPortOpen ())
            {
                //Console.WriteLine ("In CPlotterEngine.AbortPlot ()");

                //Console.WriteLine ("  calling s_objSerialPortDriver.AbortPlot ()");
                lock (s_objSerialPortDriver.m_objLock)
                {
                    s_objSerialPortDriver.AbortPlot ();
                }

                Thread.Sleep (100);
                //Console.WriteLine ("WriteTextString ... 000_Abort1");
                string strHPGL = CHPGL.EscAbortGraphicControl ();
                s_objSerialPortDriver.WriteTextString (strHPGL, "000_Abort1_" + CGenericMethods.FillStringLength (strHPGL.Length.ToString (), 5));

                Thread.Sleep (100);
                //Console.WriteLine ("WriteTextString ... 000_Abort2");
                s_objSerialPortDriver.WriteTextString (strHPGL, "000_Abort2_" + CGenericMethods.FillStringLength (strHPGL.Length.ToString (), 5));

                Thread.Sleep (100);
                //Console.WriteLine ("WriteTextString ... 000_Abort3");
                strHPGL = CHPGL.EscReset ();
                s_objSerialPortDriver.WriteTextString (strHPGL, "000_Abort3_" + CGenericMethods.FillStringLength (strHPGL.Length.ToString (), 5));

                Thread.Sleep (100);
                //Console.WriteLine ("WriteTextString ... 000_Abort4");
                strHPGL = CHPGL.Initialize () + CHPGL.SelectPen () + CHPGL.PlotAbsolute (0, 0);
                s_objSerialPortDriver.WriteTextString (strHPGL, "000_Abort4_" + CGenericMethods.FillStringLength (strHPGL.Length.ToString (), 5));

                //Console.WriteLine ("  Exiting CPlotterEngine.AbortPlot ()");
            }
        }

        public bool WaitForPlotter (int iTimeOutSeconds, bool bBufferSpaceTrace)
        {
            if (m_ePlotterPort == EPlotterPort.ESerialPort)
            {
                return s_objSerialPortDriver == null ? false : s_objSerialPortDriver.WaitForPlotter (iTimeOutSeconds, bBufferSpaceTrace);
            }
            else if (m_ePlotterPort == EPlotterPort.EParallelPort)
            {
                return CParallelPortDriver.WaitForSpooler (iTimeOutSeconds, bBufferSpaceTrace);
            }

            return false;
        }

        public static int GetPrintQueueJobCount ()
        {
            return CParallelPortDriver.GetPrintQueueJobCount ();
        }

        public static void ClearPrintQueue ()
        {
            s_objPlotterEngine.AbortPlot ();
        }

        public int GetGroupNumber (bool bIncrement = false)
        {
            return bIncrement ? ++s_iGroupNumber : s_iGroupNumber;
        }

        #region GenericPlotterMethods
        public void PlotPoints (Point[] aptPlotPoints, EPenSelect ePenSelection = EPenSelect.ESelectPen1, bool bSerial = true, bool bLastAction = true)
        {
            if (aptPlotPoints.Length > 2)
            {
                if (ePenSelection == EPenSelect.ESelectPenRandom)
                {
                    ePenSelection = CPlotterEngine.GetNextPen ();// lePens[0];
                }

                StringBuilder sbldPlotterCommands = new StringBuilder ();

                WriteToPlotter (CHPGL.Initialize () + CHPGL.SelectPen ((int)ePenSelection));

                for (int iIdx = 0; iIdx < aptPlotPoints.Length; ++iIdx)
                {
                    Point pt = aptPlotPoints[iIdx];
                    sbldPlotterCommands.Append (CHPGL.PlotAbsolute (pt.X, pt.Y));

                    if (iIdx == 0)
                    {
                        sbldPlotterCommands.Append (CHPGL.PenDown ());
                    }
                }

                // Clean up
                sbldPlotterCommands.Append (CHPGL.PenUp ());
                if (bLastAction)
                {
                    sbldPlotterCommands.Append (CHPGL.PlotAbsolute (0, 0) + CHPGL.SelectPen (0));
                }

                WriteToPlotter (sbldPlotterCommands.ToString ());
            }
        }

        public void PlotPoints (Point[] aptPlotPoints, EPenSelect ePenSelection = EPenSelect.ESelectPen1)
        {
            if (aptPlotPoints.Length > 2)
            {
                StringBuilder sbldPlotterCommands = new StringBuilder ();

                WriteToPlotter (CHPGL.Initialize () + CHPGL.SelectPen ((int)ePenSelection));

                for (int iIdx = 0; iIdx < aptPlotPoints.Length; ++iIdx)
                {
                    Point pt = aptPlotPoints[iIdx];
                    sbldPlotterCommands.Append (CHPGL.PlotAbsolute (pt.X, pt.Y));

                    if (iIdx == 0)
                    {
                        sbldPlotterCommands.Append (CHPGL.PenDown ());
                    }
                }

                sbldPlotterCommands.Append (CHPGL.PenUp ());

                WriteToPlotter (sbldPlotterCommands.ToString ());
            }
        }

        public string PlotPoints (List<Point> lptPlotPoints, EPenSelect ePenSelection = EPenSelect.ESelectPen1)
        {
            StringBuilder sbPlotPoints = new StringBuilder ();

            return sbPlotPoints.ToString ();
        }

        public string PlotPoints (List<List<Point>> llptPlotPoints, EPenSelect ePenSelection = EPenSelect.ESelectPen1)
        {
            StringBuilder sbPlotPoints = new StringBuilder ();

            foreach (List<Point> lpt in llptPlotPoints)
            {
                sbPlotPoints.Append (PlotPoints (lpt, ePenSelection));
            }

            return sbPlotPoints.ToString ();
        }

        public Point[] PlotRectangle (Rectangle rcToPlot)
        {
            List<Point> lptRectangle = new List<Point> ();

            lptRectangle.Add (new Point (rcToPlot.X, rcToPlot.Y));                         // Top left
            lptRectangle.Add (new Point (rcToPlot.Right, rcToPlot.Y));                     // Top right
            lptRectangle.Add (new Point (rcToPlot.Right, rcToPlot.Top - rcToPlot.Height)); // Bottom right
            lptRectangle.Add (new Point (rcToPlot.Left, rcToPlot.Top - rcToPlot.Height));  // Bottom left
            lptRectangle.Add (new Point (rcToPlot.X, rcToPlot.Y));                         // Top left

            return lptRectangle.ToArray ();
        }
        #endregion

        #region Unit Test methods
        public void UnitTestCPPSerialPort ()
        {
            if (IsSerialPortOpen ())
            {
                StringBuilder sbCommand = new StringBuilder ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939");
                sbCommand.Append (",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669");
                sbCommand.Append (",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241");
                sbCommand.Append (",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758");
                sbCommand.Append (",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330");
                sbCommand.Append (",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060");
                sbCommand.Append (",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009");
                sbCommand.Append (",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190");
                sbCommand.Append (",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561");
                sbCommand.Append (",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034");
                sbCommand.Append (",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499");
                sbCommand.Append (",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848");
                sbCommand.Append (",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997");
                sbCommand.Append (",2930,2999,2965;PU;PA0,0;");
                string s = sbCommand.ToString ();
                int i = s.Length;
                WriteToPlotter (sbCommand.ToString ());
                int iSpace = s_objSerialPortDriver.GetPlotterBufferSpace ();
                while (iSpace < 5120)
                {
                    Console.WriteLine ("Buffer size: " + iSpace.ToString ());
                    iSpace = s_objSerialPortDriver.GetPlotterBufferSpace ();
                }

                //CloseOutputPort ();
            }
        }

        public static void UnitTestCSerialPortDriver ()
        {
            // Draw a single large circle

            //CSerialPortDriver.WriteTextString ("0123456789ABCDEFGHIJKL");
            //CSerialPortDriver spd = new CSerialPortDriver (false);
            CSerialPortDriver spd = new CSerialPortDriver (CSerialPortDriver.BAUD9600, true);

            Console.WriteLine ("IsPortOpen returns " + (spd.IsPortOpen () ? "Open" : "Closed"));

            //bool bTest = spd.GetSimulateOutput ();
            //spd.SetSimulateOutput (true);
            ////bTest = spd.GetSimulateOutput ();
            ////spd.SetSimulateOutput (false);

            //bTest = spd.GetOutputTrace ();
            //spd.SetOutputTrace (true);

            //bTest = spd.GetDetailedOutputTrace ();
            //spd.SetDetailedOutputTrace (true);

            Console.WriteLine ("GetPortName returns: " + spd.GetPortName ());

            //// State query methods
            ////spd.GetPlotterID (); // Test method

            ////int iTest = spd.GetPlotterBufferSize (true);
            ////iTest = spd.GetPlotterBufferSpace ();
            ////iTest = spd.IsPlotterBusy (true);
            ////Console.WriteLine ("QueryPlotter returns: " + spd.QueryPlotter (CHPGL.OutputIdentification ()));
            ////iTest = spd.QueryPlotterInt (CHPGL.EscOutputBufferSize ());
            Console.WriteLine ("QueryIdentification returns: " + spd.QueryIdentification ());
            Console.WriteLine ("QueryDigitizedPoint returns: " + spd.QueryDigitizedPoint ());
            Console.WriteLine ("QueryStatus returns: " + spd.QueryStatus ());
            Console.WriteLine ("QueryStatusText returns: " + spd.QueryStatusText ());
            Console.WriteLine ("QueryFactors returns: " + spd.QueryFactors ());
            Console.WriteLine ("QueryFactorsText returns: " + spd.QueryFactorsText ());
            Console.WriteLine ("QueryError returns: " + spd.QueryError ());
            Console.WriteLine ("QueryErrorText returns: " + spd.QueryErrorText ());
            Console.WriteLine ("QueryActualPosition returns: " + spd.QueryActualPosition ());
            Console.WriteLine ("QueryActualPositionText returns: " + spd.QueryActualPositionText ());
            Console.WriteLine ("QueryCommandedPosition returns: " + spd.QueryCommandedPosition ());
            Console.WriteLine ("QueryCommandedPositionText returns: " + spd.QueryCommandedPositionText ());
            Console.WriteLine ("QueryOptions returns: " + spd.QueryOptions ());
            Console.WriteLine ("QueryOptionsText returns: " + spd.QueryOptionsText ());
            Console.WriteLine ("QueryHardClipLimits returns: " + spd.QueryHardClipLimits ());
            Console.WriteLine ("QueryHardClipLimitsText returns: " + spd.QueryHardClipLimitsText ());
            //iTest = spd.GetExtendedError ();
            Console.WriteLine ("GetExtendedErrorText returns: " + spd.GetExtendedErrorText ());
            //iTest = spd.GetExtendedStatus ();
            Console.WriteLine ("GetExtendedStatusText returns: " + spd.GetExtendedStatusText ());
            Console.WriteLine ("QueryOutputWindow returns: " + spd.QueryOutputWindow ());
            Console.WriteLine ("QueryOutputWindowText returns: " + spd.QueryOutputWindowText ());

            //spd.WriteTextString (STRING_SIMPLE_HPGL_TEST);
            //Console.WriteLine ("QueueLength: " + spd.GetQueueLength ());
            //Console.WriteLine ("QueueSize:   " + spd.GetQueueSize () + '\n');

            //spd.WriteTextString (STRING_SIMPLE_HPGL_TEST);
            //Console.WriteLine ("QueueLength: " + spd.GetQueueLength ());
            //Console.WriteLine ("QueueSize:   " + spd.GetQueueSize () + '\n');

            //spd.WriteTextString (STRING_SIMPLE_HPGL_TEST);
            //Console.WriteLine ("QueueLength: " + spd.GetQueueLength ());
            //Console.WriteLine ("QueueSize:   " + spd.GetQueueSize () + '\n');

            //spd.WriteTextString (STRING_SIMPLE_HPGL_TEST);
            //Console.WriteLine ("QueueLength: " + spd.GetQueueLength ());
            //Console.WriteLine ("QueueSize:   " + spd.GetQueueSize () + '\n');

            //spd.WriteTextString (STRING_SIMPLE_HPGL_TEST);
            //Console.WriteLine ("QueueLength: " + spd.GetQueueLength ());
            //Console.WriteLine ("QueueSize:   " + spd.GetQueueSize () + '\n');

            //spd.WriteTextString (STRING_SIMPLE_HPGL_TEST);
            //Console.WriteLine ("QueueLength: " + spd.GetQueueLength ());
            //Console.WriteLine ("QueueSize:   " + spd.GetQueueSize () + '\n');

            //while (spd.GetQueueLength () > 0)
            //{
            //    Console.WriteLine ("Loop QueueLength: " + spd.GetQueueLength ());
            //    Console.WriteLine ("Loop QueueSize:   " + spd.GetQueueSize () + '\n');
            //    Thread.Sleep (500);
            //}
            //Console.WriteLine ("After Loop");

            StringBuilder sbCommand = new StringBuilder ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939");
            sbCommand.Append (",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669");
            sbCommand.Append (",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241");
            sbCommand.Append (",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758");
            sbCommand.Append (",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330");
            sbCommand.Append (",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060");
            sbCommand.Append (",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009");
            sbCommand.Append (",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190");
            sbCommand.Append (",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561");
            sbCommand.Append (",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034");
            sbCommand.Append (",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499");
            sbCommand.Append (",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848");
            sbCommand.Append (",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997");
            sbCommand.Append (",2930,2999,2965;PU;PA0,0;");

            sbCommand.Append ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939");
            sbCommand.Append (",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669");
            sbCommand.Append (",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241");
            sbCommand.Append (",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758");
            sbCommand.Append (",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330");
            sbCommand.Append (",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060");
            sbCommand.Append (",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009");
            sbCommand.Append (",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190");
            sbCommand.Append (",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561");
            sbCommand.Append (",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034");
            sbCommand.Append (",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499");
            sbCommand.Append (",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848");
            sbCommand.Append (",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997");
            sbCommand.Append (",2930,2999,2965;PU;PA0,0;");

            sbCommand.Append ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939");
            sbCommand.Append (",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669");
            sbCommand.Append (",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241");
            sbCommand.Append (",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758");
            sbCommand.Append (",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330");
            sbCommand.Append (",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060");
            sbCommand.Append (",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009");
            sbCommand.Append (",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190");
            sbCommand.Append (",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561");
            sbCommand.Append (",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034");
            sbCommand.Append (",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499");
            sbCommand.Append (",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848");
            sbCommand.Append (",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997");
            sbCommand.Append (",2930,2999,2965;PU;PA0,0;");

            sbCommand.Append ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939");
            sbCommand.Append (",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669");
            sbCommand.Append (",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241");
            sbCommand.Append (",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758");
            sbCommand.Append (",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330");
            sbCommand.Append (",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060");
            sbCommand.Append (",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009");
            sbCommand.Append (",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190");
            sbCommand.Append (",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561");
            sbCommand.Append (",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034");
            sbCommand.Append (",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499");
            sbCommand.Append (",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848");
            sbCommand.Append (",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997");
            sbCommand.Append (",2930,2999,2965;PU;PA0,0;");

            sbCommand.Append ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939");
            sbCommand.Append (",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669");
            sbCommand.Append (",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241");
            sbCommand.Append (",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758");
            sbCommand.Append (",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330");
            sbCommand.Append (",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060");
            sbCommand.Append (",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009");
            sbCommand.Append (",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190");
            sbCommand.Append (",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561");
            sbCommand.Append (",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034");
            sbCommand.Append (",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499");
            sbCommand.Append (",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848");
            sbCommand.Append (",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997");
            sbCommand.Append (",2930,2999,2965;PU;PA0,0;");
            //bool bTest = spd.WaitForPlotter (10, true);

            string s = sbCommand.ToString ();
            int i = s.Length;
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver");
            Thread.Sleep (500);
            Console.WriteLine ("QueryIdentification returns: " + spd.QueryIdentification ());

            bool bTest = spd.WaitForQueue (300);

            int iSpace = spd.GetPlotterBufferSpace ();
            try
            {
                while (iSpace < 5120 &&
                       iSpace > 0)
                {
                    Thread.Sleep (100);
                    Console.WriteLine ("(test) Buffer size: " + iSpace.ToString ());
                    iSpace = spd.GetPlotterBufferSpace ();
                }
                Console.WriteLine ("QueryActualPosition: " + spd.QueryActualPosition ());
                Console.WriteLine ("QueryActualPositionText: " + spd.QueryActualPositionText ());
            }
            catch (Exception e)
            {
                Console.WriteLine ("** Exception in UnitTestCSerialPortDriver (): " + e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine ("  " + e.InnerException.Message);
                }
                Console.WriteLine (e.StackTrace);
            }

            spd.CloseOutputPort (CSerialPortDriver.EClosePortMethod.EWaitForQueueAndClose);
        }

        public static void UnitTestCSerialPortDriver3 ()
        {
            // Draw FourQuadrants - single call to WriteTextString ()

            CSerialPortDriver spd = new CSerialPortDriver (CSerialPortDriver.BAUD9600, false);

            Console.WriteLine ("IsPortOpen returns " + (spd.IsPortOpen () ? "Open" : "Closed"));

            Console.WriteLine ("GetPortName returns: " + spd.GetPortName ());

            StringBuilder sbCommand = new StringBuilder ("PA0,220;SP1;");

            sbCommand.Append ("DF;PA;SS;PU;PA3750,1300;PD;PA3984,5050;PU;PA4218,5050;PD;PA3750,1534;PU;PA3750,1768;PD;PA4");
            sbCommand.Append ("453,5050;PU;PA4687,5050;PD;PA3750,2003;PU;PA3750,2237;PD;PA4921,5050;PU;PA5156,5050;PD;PA3750,2471;PU;PA3750,2706;P");
            sbCommand.Append ("D;PA5390,5050;PU;PA5625,5050;PD;PA3750,2940;PU;PA3750,3175;PD;PA5859,5050;PU;PA6093,5050;PD;PA3750,3409;PU;PA3750,3");
            sbCommand.Append ("643;PD;PA6328,5050;PU;PA6562,5050;PD;PA3750,3878;PU;PA3750,4112;PD;PA6796,5050;PU;PA7031,5050;PD;PA3750,4346;PU;PA3");
            sbCommand.Append ("750,4581;PD;PA7265,5050;PU;PA7500,5050;PD;PA3750,4815;PU;");

            sbCommand.Append ("PU;PA3750,5050;PD;PA3984,1300;PU;PA4218,1300;PD;PA3750,4815;PU;PA3750,4581;PD;PA4453,1300;");
            sbCommand.Append ("PU;PA4687,1300;PD;PA3750,4346;PU;PA3750,4112;PD;PA4921,1300;PU;PA5156,1300;PD;PA3750,3878;PU;PA3750,3643;PD;PA5390,");
            sbCommand.Append ("1300;PU;PA5625,1300;PD;PA3750,3409;PU;PA3750,3175;PD;PA5859,1300;PU;PA6093,1300;PD;PA3750,2940;PU;PA3750,2706;PD;PA");
            sbCommand.Append ("6328,1300;PU;PA6562,1300;PD;PA3750,2471;PU;PA3750,2237;PD;PA6796,1300;PU;PA7031,1300;PD;PA3750,2003;PU;PA3750,1768;");
            sbCommand.Append ("PD;PA7265,1300;PU;PA7500,1300;PD;PA3750,1534;PU;");

            sbCommand.Append ("PU;PA0,5050;PD;PA234,8800;PU;PA468,8800;PD;PA0,5284;PU;PA0,5518;PD;PA703,8800;PU;PA937,880");
            sbCommand.Append ("0;PD;PA0,5753;PU;PA0,5987;PD;PA1171,8800;PU;PA1406,8800;PD;PA0,6221;PU;PA0,6456;PD;PA1640,8800;PU;PA1875,8800;PD;PA");
            sbCommand.Append ("0,6690;PU;PA0,6925;PD;PA2109,8800;PU;PA2343,8800;PD;PA0,7159;PU;PA0,7393;PD;PA2578,8800;PU;PA2812,8800;PD;PA0,7628;");
            sbCommand.Append ("PU;PA0,7862;PD;PA3046,8800;PU;PA3281,8800;PD;PA0,8096;PU;PA0,8331;PD;PA3515,8800;PU;PA3750,8800;PD;PA0,8565;PU;");

            sbCommand.Append ("PA0,1588;SP2;");

            sbCommand.Append ("DF;PA;SS;PU;PA0,1300;PD;PA3750,1534;PU;PA3750,1768;PD;PA234,1300;PU;PA468,1300;PD;PA3750,2");
            sbCommand.Append ("003;PU;PA3750,2237;PD;PA703,1300;PU;PA937,1300;PD;PA3750,2471;PU;PA3750,2706;PD;PA1171,1300;PU;PA1406,1300;PD;PA375");
            sbCommand.Append ("0,2940;PU;PA3750,3175;PD;PA1640,1300;PU;PA1875,1300;PD;PA3750,3409;PU;PA3750,3643;PD;PA2109,1300;PU;PA2343,1300;PD;");
            sbCommand.Append ("PA3750,3878;PU;PA3750,4112;PD;PA2578,1300;PU;PA2812,1300;PD;PA3750,4346;PU;PA3750,4581;PD;PA3046,1300;PU;PA3281,130");
            sbCommand.Append ("0;PD;PA3750,4815;PU;PA3750,5050;PD;PA3515,1300;PU;");

            sbCommand.Append ("PU;PA0,5050;PD;PA3750,4815;PU;PA3750,4581;PD;PA234,5050;PU;PA468,5050;PD;PA3750,4346;PU;PA");
            sbCommand.Append ("3750,4112;PD;PA703,5050;PU;PA937,5050;PD;PA3750,3878;PU;PA3750,3643;PD;PA1171,5050;PU;PA1406,5050;PD;PA3750,3409;PU");
            sbCommand.Append (";PA3750,3175;PD;PA1640,5050;PU;PA1875,5050;PD;PA3750,2940;PU;PA3750,2706;PD;PA2109,5050;PU;PA2343,5050;PD;PA3750,24");
            sbCommand.Append ("71;PU;PA3750,2237;PD;PA2578,5050;PU;PA2812,5050;PD;PA3750,2003;PU;PA3750,1768;PD;PA3046,5050;PU;PA3281,5050;PD;PA37");
            sbCommand.Append ("50,1534;PU;PA3750,1300;PD;PA3515,5050;PU;");

            sbCommand.Append ("PU;PA7500,5050;PD;PA7265,8800;PU;PA7031,8800;PD;PA7500,5284;PU;PA7500,5518;PD;PA6796,8800;");
            sbCommand.Append ("PU;PA6562,8800;PD;PA7500,5753;PU;PA7500,5987;PD;PA6328,8800;PU;PA6093,8800;PD;PA7500,6221;PU;PA7500,6456;PD;PA5859,");
            sbCommand.Append ("8800;PU;PA5625,8800;PD;PA7500,6690;PU;PA7500,6925;PD;PA5390,8800;PU;PA5156,8800;PD;PA7500,7159;PU;PA7500,7393;PD;PA");
            sbCommand.Append ("4921,8800;PU;PA4687,8800;PD;PA7500,7628;PU;PA7500,7862;PD;PA4453,8800;PU;PA4218,8800;PD;PA7500,8096;PU;PA7500,8331;");
            sbCommand.Append ("PD;PA3984,8800;PU;PA3750,8800;PD;PA7500,8565;PU;");

            sbCommand.Append ("PA0,2956;SP3;");

            sbCommand.Append ("DF;PA;SS;PU;PA3750,1300;PD;PA7500,1534;PU;PA7500,1768;PD;PA3984,1300;PU;PA4218,1300;PD;PA7");
            sbCommand.Append ("500,2003;PU;PA7500,2237;PD;PA4453,1300;PU;PA4687,1300;PD;PA7500,2471;PU;PA7500,2706;PD;PA4921,1300;PU;PA5156,1300;P");
            sbCommand.Append ("D;PA7500,2940;PU;PA7500,3175;PD;PA5390,1300;PU;PA5625,1300;PD;PA7500,3409;PU;PA7500,3643;PD;PA5859,1300;PU;PA6093,1");
            sbCommand.Append ("300;PD;PA7500,3878;PU;PA7500,4112;PD;PA6328,1300;PU;PA6562,1300;PD;PA7500,4346;PU;PA7500,4581;PD;PA6796,1300;PU;PA7");
            sbCommand.Append ("031,1300;PD;PA7500,4815;PU;PA7500,5050;PD;PA7265,1300;PU;");

            sbCommand.Append ("PU;PA3750,5050;PD;PA3515,8800;PU;PA3281,8800;PD;PA3750,5284;PU;PA3750,5518;PD;PA3046,8800;");
            sbCommand.Append ("PU;PA2812,8800;PD;PA3750,5753;PU;PA3750,5987;PD;PA2578,8800;PU;PA2343,8800;PD;PA3750,6221;PU;PA3750,6456;PD;PA2109,");
            sbCommand.Append ("8800;PU;PA1875,8800;PD;PA3750,6690;PU;PA3750,6925;PD;PA1640,8800;PU;PA1406,8800;PD;PA3750,7159;PU;PA3750,7393;PD;PA");
            sbCommand.Append ("1171,8800;PU;PA937,8800;PD;PA3750,7628;PU;PA3750,7862;PD;PA703,8800;PU;PA468,8800;PD;PA3750,8096;PU;PA3750,8331;PD;");
            sbCommand.Append ("PA234,8800;PU;PA0,8800;PD;PA3750,8565;PU;");

            sbCommand.Append ("PU;PA3750,5050;PD;PA0,4815;PU;PA0,4581;PD;PA3515,5050;PU;PA3281,5050;PD;PA0,4346;PU;PA0,41");
            sbCommand.Append ("12;PD;PA3046,5050;PU;PA2812,5050;PD;PA0,3878;PU;PA0,3643;PD;PA2578,5050;PU;PA2343,5050;PD;PA0,3409;PU;PA0,3175;PD;P");
            sbCommand.Append ("A2109,5050;PU;PA1875,5050;PD;PA0,2940;PU;PA0,2706;PD;PA1640,5050;PU;PA1406,5050;PD;PA0,2471;PU;PA0,2237;PD;PA1171,5");
            sbCommand.Append ("050;PU;PA937,5050;PD;PA0,2003;PU;PA0,1768;PD;PA703,5050;PU;PA468,5050;PD;PA0,1534;PU;PA0,1300;PD;PA234,5050;PU;");

            sbCommand.Append ("PA0,4324;SP4;");

            sbCommand.Append ("DF;PA;SS;PU;PA0,5050;PD;PA234,1300;PU;PA468,1300;PD;PA0,4815;PU;PA0,4581;PD;PA703,1300;PU;");
            sbCommand.Append ("PA937,1300;PD;PA0,4346;PU;PA0,4112;PD;PA1171,1300;PU;PA1406,1300;PD;PA0,3878;PU;PA0,3643;PD;PA1640,1300;PU;PA1875,1");
            sbCommand.Append ("300;PD;PA0,3409;PU;PA0,3175;PD;PA2109,1300;PU;PA2343,1300;PD;PA0,2940;PU;PA0,2706;PD;PA2578,1300;PU;PA2812,1300;PD;");
            sbCommand.Append ("PA0,2471;PU;PA0,2237;PD;PA3046,1300;PU;PA3281,1300;PD;PA0,2003;PU;PA0,1768;PD;PA3515,1300;PU;PA3750,1300;PD;PA0,153");
            sbCommand.Append ("4;PU;");

            sbCommand.Append ("PU;PA0,8800;PD;PA234,5050;PU;PA468,5050;PD;PA0,8565;PU;PA0,8331;PD;PA703,5050;PU;PA937,505");
            sbCommand.Append ("0;PD;PA0,8096;PU;PA0,7862;PD;PA1171,5050;PU;PA1406,5050;PD;PA0,7628;PU;PA0,7393;PD;PA1640,5050;PU;PA1875,5050;PD;PA");
            sbCommand.Append ("0,7159;PU;PA0,6925;PD;PA2109,5050;PU;PA2343,5050;PD;PA0,6690;PU;PA0,6456;PD;PA2578,5050;PU;PA2812,5050;PD;PA0,6221;");
            sbCommand.Append ("PU;PA0,5987;PD;PA3046,5050;PU;PA3281,5050;PD;PA0,5753;PU;PA0,5518;PD;PA3515,5050;PU;PA3750,5050;PD;PA0,5284;PU;");

            sbCommand.Append ("PU;PA7500,8800;PD;PA3750,8565;PU;PA3750,8331;PD;PA7265,8800;PU;PA7031,8800;PD;PA3750,8096;");
            sbCommand.Append ("PU;PA3750,7862;PD;PA6796,8800;PU;PA6562,8800;PD;PA3750,7628;PU;PA3750,7393;PD;PA6328,8800;PU;PA6093,8800;PD;PA3750,");
            sbCommand.Append ("7159;PU;PA3750,6925;PD;PA5859,8800;PU;PA5625,8800;PD;PA3750,6690;PU;PA3750,6456;PD;PA5390,8800;PU;PA5156,8800;PD;PA");
            sbCommand.Append ("3750,6221;PU;PA3750,5987;PD;PA4921,8800;PU;PA4687,8800;PD;PA3750,5753;PU;PA3750,5518;PD;PA4453,8800;PU;PA4218,8800;");
            sbCommand.Append ("PD;PA3750,5284;PU;PA3750,5050;PD;PA3984,8800;PU;");

            sbCommand.Append ("PA0,5692;SP5;");

            sbCommand.Append ("DF;PA;SS;PU;PA3750,5050;PD;PA7500,5284;PU;PA7500,5518;PD;PA3984,5050;PU;PA4218,5050;PD;PA7");
            sbCommand.Append ("500,5753;PU;PA7500,5987;PD;PA4453,5050;PU;PA4687,5050;PD;PA7500,6221;PU;PA7500,6456;PD;PA4921,5050;PU;PA5156,5050;P");
            sbCommand.Append ("D;PA7500,6690;PU;PA7500,6925;PD;PA5390,5050;PU;PA5625,5050;PD;PA7500,7159;PU;PA7500,7393;PD;PA5859,5050;PU;PA6093,5");
            sbCommand.Append ("050;PD;PA7500,7628;PU;PA7500,7862;PD;PA6328,5050;PU;PA6562,5050;PD;PA7500,8096;PU;PA7500,8331;PD;PA6796,5050;PU;PA7");
            sbCommand.Append ("031,5050;PD;PA7500,8565;PU;PA7500,8800;PD;PA7265,5050;PU;");

            sbCommand.Append ("PU;PA3750,8800;PD;PA3515,5050;PU;PA3281,5050;PD;PA3750,8565;PU;PA3750,8331;PD;PA3046,5050;");
            sbCommand.Append ("PU;PA2812,5050;PD;PA3750,8096;PU;PA3750,7862;PD;PA2578,5050;PU;PA2343,5050;PD;PA3750,7628;PU;PA3750,7393;PD;PA2109,");
            sbCommand.Append ("5050;PU;PA1875,5050;PD;PA3750,7159;PU;PA3750,6925;PD;PA1640,5050;PU;PA1406,5050;PD;PA3750,6690;PU;PA3750,6456;PD;PA");
            sbCommand.Append ("1171,5050;PU;PA937,5050;PD;PA3750,6221;PU;PA3750,5987;PD;PA703,5050;PU;PA468,5050;PD;PA3750,5753;PU;PA3750,5518;PD;");
            sbCommand.Append ("PA234,5050;PU;PA0,5050;PD;PA3750,5284;PU;");

            sbCommand.Append ("PA0,7060;SP6;");

            sbCommand.Append ("DF;PA;SS;PU;PA7500,5050;PD;PA3750,5284;PU;PA3750,5518;PD;PA7265,5050;PU;PA7031,5050;PD;PA3");
            sbCommand.Append ("750,5753;PU;PA3750,5987;PD;PA6796,5050;PU;PA6562,5050;PD;PA3750,6221;PU;PA3750,6456;PD;PA6328,5050;PU;PA6093,5050;P");
            sbCommand.Append ("D;PA3750,6690;PU;PA3750,6925;PD;PA5859,5050;PU;PA5625,5050;PD;PA3750,7159;PU;PA3750,7393;PD;PA5390,5050;PU;PA5156,5");
            sbCommand.Append ("050;PD;PA3750,7628;PU;PA3750,7862;PD;PA4921,5050;PU;PA4687,5050;PD;PA3750,8096;PU;PA3750,8331;PD;PA4453,5050;PU;PA4");
            sbCommand.Append ("218,5050;PD;PA3750,8565;PU;PA3750,8800;PD;PA3984,5050;PU;");

            sbCommand.Append ("PU;PA7500,1300;PD;PA7265,5050;PU;PA7031,5050;PD;PA7500,1534;PU;PA7500,1768;PD;PA6796,5050;");
            sbCommand.Append ("PU;PA6562,5050;PD;PA7500,2003;PU;PA7500,2237;PD;PA6328,5050;PU;PA6093,5050;PD;PA7500,2471;PU;PA7500,2706;PD;PA5859,");
            sbCommand.Append ("5050;PU;PA5625,5050;PD;PA7500,2940;PU;PA7500,3175;PD;PA5390,5050;PU;PA5156,5050;PD;PA7500,3409;PU;PA7500,3643;PD;PA");
            sbCommand.Append ("4921,5050;PU;PA4687,5050;PD;PA7500,3878;PU;PA7500,4112;PD;PA4453,5050;PU;PA4218,5050;PD;PA7500,4346;PU;PA7500,4581;");
            sbCommand.Append ("PD;PA3984,5050;PU;PA3750,5050;PD;PA7500,4815;PU;");

            sbCommand.Append ("PA0,7060;SP0;PA0,0;");
            string s = sbCommand.ToString ();
            int i = s.Length;
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver3");
            Thread.Sleep (500);
            Console.WriteLine ("QueryIdentification returns: " + spd.QueryIdentification ());

            bool bTest = spd.WaitForQueue (300);

            int iSpace = spd.GetPlotterBufferSpace ();
            try
            {
                while (iSpace < 5120 &&
                       iSpace > 0)
                {
                    Thread.Sleep (100);
                    Console.WriteLine ("(test) Buffer size: " + iSpace.ToString ());
                    iSpace = spd.GetPlotterBufferSpace ();
                }
                Console.WriteLine ("QueryActualPosition: " + spd.QueryActualPosition ());
                Console.WriteLine ("QueryActualPositionText: " + spd.QueryActualPositionText ());
            }
            catch (Exception e)
            {
                Console.WriteLine ("** Exception in UnitTestCSerialPortDriver3 (): " + e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine ("  " + e.InnerException.Message);
                }
                Console.WriteLine (e.StackTrace);
            }

            spd.CloseOutputPort (CSerialPortDriver.EClosePortMethod.EWaitForQueueAndClose);
        }

        public static void UnitTestCSerialPortDriver4 ()
        {
            // Draw FourQuadrants - multiple calls to WriteTextString ()

            CSerialPortDriver spd = new CSerialPortDriver (CSerialPortDriver.BAUD9600, false);

            Console.WriteLine ("IsPortOpen returns " + (spd.IsPortOpen () ? "Open" : "Closed"));

            Console.WriteLine ("GetPortName returns: " + spd.GetPortName ());

            spd.WriteTextString ("PA0,220;SP1;", "UnitTestCSerialPortDriver4 01");

            StringBuilder sbCommand = new StringBuilder ("DF;PA;SS;PU;PA3750,1300;PD;PA3984,5050;PU;PA4218,5050;PD;PA3750,1534;PU;PA3750,1768;PD;PA4");
            sbCommand.Append ("453,5050;PU;PA4687,5050;PD;PA3750,2003;PU;PA3750,2237;PD;PA4921,5050;PU;PA5156,5050;PD;PA3750,2471;PU;PA3750,2706;P");
            sbCommand.Append ("D;PA5390,5050;PU;PA5625,5050;PD;PA3750,2940;PU;PA3750,3175;PD;PA5859,5050;PU;PA6093,5050;PD;PA3750,3409;PU;PA3750,3");
            sbCommand.Append ("643;PD;PA6328,5050;PU;PA6562,5050;PD;PA3750,3878;PU;PA3750,4112;PD;PA6796,5050;PU;PA7031,5050;PD;PA3750,4346;PU;PA3");
            sbCommand.Append ("750,4581;PD;PA7265,5050;PU;PA7500,5050;PD;PA3750,4815;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 02");
            sbCommand.Clear ();

            sbCommand.Append ("PU;PA3750,5050;PD;PA3984,1300;PU;PA4218,1300;PD;PA3750,4815;PU;PA3750,4581;PD;PA4453,1300;");
            sbCommand.Append ("PU;PA4687,1300;PD;PA3750,4346;PU;PA3750,4112;PD;PA4921,1300;PU;PA5156,1300;PD;PA3750,3878;PU;PA3750,3643;PD;PA5390,");
            sbCommand.Append ("1300;PU;PA5625,1300;PD;PA3750,3409;PU;PA3750,3175;PD;PA5859,1300;PU;PA6093,1300;PD;PA3750,2940;PU;PA3750,2706;PD;PA");
            sbCommand.Append ("6328,1300;PU;PA6562,1300;PD;PA3750,2471;PU;PA3750,2237;PD;PA6796,1300;PU;PA7031,1300;PD;PA3750,2003;PU;PA3750,1768;");
            sbCommand.Append ("PD;PA7265,1300;PU;PA7500,1300;PD;PA3750,1534;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 03");
            sbCommand.Clear ();

            sbCommand.Append ("PU;PA0,5050;PD;PA234,8800;PU;PA468,8800;PD;PA0,5284;PU;PA0,5518;PD;PA703,8800;PU;PA937,880");
            sbCommand.Append ("0;PD;PA0,5753;PU;PA0,5987;PD;PA1171,8800;PU;PA1406,8800;PD;PA0,6221;PU;PA0,6456;PD;PA1640,8800;PU;PA1875,8800;PD;PA");
            sbCommand.Append ("0,6690;PU;PA0,6925;PD;PA2109,8800;PU;PA2343,8800;PD;PA0,7159;PU;PA0,7393;PD;PA2578,8800;PU;PA2812,8800;PD;PA0,7628;");
            sbCommand.Append ("PU;PA0,7862;PD;PA3046,8800;PU;PA3281,8800;PD;PA0,8096;PU;PA0,8331;PD;PA3515,8800;PU;PA3750,8800;PD;PA0,8565;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 04");
            sbCommand.Clear ();

            spd.WriteTextString ("PA0,1588;SP2;", "UnitTestCSerialPortDriver4 05");

            sbCommand.Append ("DF;PA;SS;PU;PA0,1300;PD;PA3750,1534;PU;PA3750,1768;PD;PA234,1300;PU;PA468,1300;PD;PA3750,2");
            sbCommand.Append ("003;PU;PA3750,2237;PD;PA703,1300;PU;PA937,1300;PD;PA3750,2471;PU;PA3750,2706;PD;PA1171,1300;PU;PA1406,1300;PD;PA375");
            sbCommand.Append ("0,2940;PU;PA3750,3175;PD;PA1640,1300;PU;PA1875,1300;PD;PA3750,3409;PU;PA3750,3643;PD;PA2109,1300;PU;PA2343,1300;PD;");
            sbCommand.Append ("PA3750,3878;PU;PA3750,4112;PD;PA2578,1300;PU;PA2812,1300;PD;PA3750,4346;PU;PA3750,4581;PD;PA3046,1300;PU;PA3281,130");
            sbCommand.Append ("0;PD;PA3750,4815;PU;PA3750,5050;PD;PA3515,1300;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 06");
            sbCommand.Clear ();

            sbCommand.Append ("PU;PA0,5050;PD;PA3750,4815;PU;PA3750,4581;PD;PA234,5050;PU;PA468,5050;PD;PA3750,4346;PU;PA");
            sbCommand.Append ("3750,4112;PD;PA703,5050;PU;PA937,5050;PD;PA3750,3878;PU;PA3750,3643;PD;PA1171,5050;PU;PA1406,5050;PD;PA3750,3409;PU");
            sbCommand.Append (";PA3750,3175;PD;PA1640,5050;PU;PA1875,5050;PD;PA3750,2940;PU;PA3750,2706;PD;PA2109,5050;PU;PA2343,5050;PD;PA3750,24");
            sbCommand.Append ("71;PU;PA3750,2237;PD;PA2578,5050;PU;PA2812,5050;PD;PA3750,2003;PU;PA3750,1768;PD;PA3046,5050;PU;PA3281,5050;PD;PA37");
            sbCommand.Append ("50,1534;PU;PA3750,1300;PD;PA3515,5050;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 07");
            sbCommand.Clear ();

            sbCommand.Append ("PU;PA7500,5050;PD;PA7265,8800;PU;PA7031,8800;PD;PA7500,5284;PU;PA7500,5518;PD;PA6796,8800;");
            sbCommand.Append ("PU;PA6562,8800;PD;PA7500,5753;PU;PA7500,5987;PD;PA6328,8800;PU;PA6093,8800;PD;PA7500,6221;PU;PA7500,6456;PD;PA5859,");
            sbCommand.Append ("8800;PU;PA5625,8800;PD;PA7500,6690;PU;PA7500,6925;PD;PA5390,8800;PU;PA5156,8800;PD;PA7500,7159;PU;PA7500,7393;PD;PA");
            sbCommand.Append ("4921,8800;PU;PA4687,8800;PD;PA7500,7628;PU;PA7500,7862;PD;PA4453,8800;PU;PA4218,8800;PD;PA7500,8096;PU;PA7500,8331;");
            sbCommand.Append ("PD;PA3984,8800;PU;PA3750,8800;PD;PA7500,8565;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 08");
            sbCommand.Clear ();

            spd.WriteTextString ("PA0,2956;SP3;", "UnitTestCSerialPortDriver4 09");

            sbCommand.Append ("DF;PA;SS;PU;PA3750,1300;PD;PA7500,1534;PU;PA7500,1768;PD;PA3984,1300;PU;PA4218,1300;PD;PA7");
            sbCommand.Append ("500,2003;PU;PA7500,2237;PD;PA4453,1300;PU;PA4687,1300;PD;PA7500,2471;PU;PA7500,2706;PD;PA4921,1300;PU;PA5156,1300;P");
            sbCommand.Append ("D;PA7500,2940;PU;PA7500,3175;PD;PA5390,1300;PU;PA5625,1300;PD;PA7500,3409;PU;PA7500,3643;PD;PA5859,1300;PU;PA6093,1");
            sbCommand.Append ("300;PD;PA7500,3878;PU;PA7500,4112;PD;PA6328,1300;PU;PA6562,1300;PD;PA7500,4346;PU;PA7500,4581;PD;PA6796,1300;PU;PA7");
            sbCommand.Append ("031,1300;PD;PA7500,4815;PU;PA7500,5050;PD;PA7265,1300;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 10");
            sbCommand.Clear ();

            sbCommand.Append ("PU;PA3750,5050;PD;PA3515,8800;PU;PA3281,8800;PD;PA3750,5284;PU;PA3750,5518;PD;PA3046,8800;");
            sbCommand.Append ("PU;PA2812,8800;PD;PA3750,5753;PU;PA3750,5987;PD;PA2578,8800;PU;PA2343,8800;PD;PA3750,6221;PU;PA3750,6456;PD;PA2109,");
            sbCommand.Append ("8800;PU;PA1875,8800;PD;PA3750,6690;PU;PA3750,6925;PD;PA1640,8800;PU;PA1406,8800;PD;PA3750,7159;PU;PA3750,7393;PD;PA");
            sbCommand.Append ("1171,8800;PU;PA937,8800;PD;PA3750,7628;PU;PA3750,7862;PD;PA703,8800;PU;PA468,8800;PD;PA3750,8096;PU;PA3750,8331;PD;");
            sbCommand.Append ("PA234,8800;PU;PA0,8800;PD;PA3750,8565;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 11");
            sbCommand.Clear ();

            sbCommand.Append ("PU;PA3750,5050;PD;PA0,4815;PU;PA0,4581;PD;PA3515,5050;PU;PA3281,5050;PD;PA0,4346;PU;PA0,41");
            sbCommand.Append ("12;PD;PA3046,5050;PU;PA2812,5050;PD;PA0,3878;PU;PA0,3643;PD;PA2578,5050;PU;PA2343,5050;PD;PA0,3409;PU;PA0,3175;PD;P");
            sbCommand.Append ("A2109,5050;PU;PA1875,5050;PD;PA0,2940;PU;PA0,2706;PD;PA1640,5050;PU;PA1406,5050;PD;PA0,2471;PU;PA0,2237;PD;PA1171,5");
            sbCommand.Append ("050;PU;PA937,5050;PD;PA0,2003;PU;PA0,1768;PD;PA703,5050;PU;PA468,5050;PD;PA0,1534;PU;PA0,1300;PD;PA234,5050;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 12");
            sbCommand.Clear ();

            spd.WriteTextString ("PA0,4324;SP4;", "UnitTestCSerialPortDriver4 13");

            sbCommand.Append ("DF;PA;SS;PU;PA0,5050;PD;PA234,1300;PU;PA468,1300;PD;PA0,4815;PU;PA0,4581;PD;PA703,1300;PU;");
            sbCommand.Append ("PA937,1300;PD;PA0,4346;PU;PA0,4112;PD;PA1171,1300;PU;PA1406,1300;PD;PA0,3878;PU;PA0,3643;PD;PA1640,1300;PU;PA1875,1");
            sbCommand.Append ("300;PD;PA0,3409;PU;PA0,3175;PD;PA2109,1300;PU;PA2343,1300;PD;PA0,2940;PU;PA0,2706;PD;PA2578,1300;PU;PA2812,1300;PD;");
            sbCommand.Append ("PA0,2471;PU;PA0,2237;PD;PA3046,1300;PU;PA3281,1300;PD;PA0,2003;PU;PA0,1768;PD;PA3515,1300;PU;PA3750,1300;PD;PA0,153");
            sbCommand.Append ("4;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 14");
            sbCommand.Clear ();

            sbCommand.Append ("PU;PA0,8800;PD;PA234,5050;PU;PA468,5050;PD;PA0,8565;PU;PA0,8331;PD;PA703,5050;PU;PA937,505");
            sbCommand.Append ("0;PD;PA0,8096;PU;PA0,7862;PD;PA1171,5050;PU;PA1406,5050;PD;PA0,7628;PU;PA0,7393;PD;PA1640,5050;PU;PA1875,5050;PD;PA");
            sbCommand.Append ("0,7159;PU;PA0,6925;PD;PA2109,5050;PU;PA2343,5050;PD;PA0,6690;PU;PA0,6456;PD;PA2578,5050;PU;PA2812,5050;PD;PA0,6221;");
            sbCommand.Append ("PU;PA0,5987;PD;PA3046,5050;PU;PA3281,5050;PD;PA0,5753;PU;PA0,5518;PD;PA3515,5050;PU;PA3750,5050;PD;PA0,5284;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 15");
            sbCommand.Clear ();

            sbCommand.Append ("PU;PA7500,8800;PD;PA3750,8565;PU;PA3750,8331;PD;PA7265,8800;PU;PA7031,8800;PD;PA3750,8096;");
            sbCommand.Append ("PU;PA3750,7862;PD;PA6796,8800;PU;PA6562,8800;PD;PA3750,7628;PU;PA3750,7393;PD;PA6328,8800;PU;PA6093,8800;PD;PA3750,");
            sbCommand.Append ("7159;PU;PA3750,6925;PD;PA5859,8800;PU;PA5625,8800;PD;PA3750,6690;PU;PA3750,6456;PD;PA5390,8800;PU;PA5156,8800;PD;PA");
            sbCommand.Append ("3750,6221;PU;PA3750,5987;PD;PA4921,8800;PU;PA4687,8800;PD;PA3750,5753;PU;PA3750,5518;PD;PA4453,8800;PU;PA4218,8800;");
            sbCommand.Append ("PD;PA3750,5284;PU;PA3750,5050;PD;PA3984,8800;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 16");
            sbCommand.Clear ();

            spd.WriteTextString ("PA0,5692;SP5;", "UnitTestCSerialPortDriver4 17");

            sbCommand.Append ("DF;PA;SS;PU;PA3750,5050;PD;PA7500,5284;PU;PA7500,5518;PD;PA3984,5050;PU;PA4218,5050;PD;PA7");
            sbCommand.Append ("500,5753;PU;PA7500,5987;PD;PA4453,5050;PU;PA4687,5050;PD;PA7500,6221;PU;PA7500,6456;PD;PA4921,5050;PU;PA5156,5050;P");
            sbCommand.Append ("D;PA7500,6690;PU;PA7500,6925;PD;PA5390,5050;PU;PA5625,5050;PD;PA7500,7159;PU;PA7500,7393;PD;PA5859,5050;PU;PA6093,5");
            sbCommand.Append ("050;PD;PA7500,7628;PU;PA7500,7862;PD;PA6328,5050;PU;PA6562,5050;PD;PA7500,8096;PU;PA7500,8331;PD;PA6796,5050;PU;PA7");
            sbCommand.Append ("031,5050;PD;PA7500,8565;PU;PA7500,8800;PD;PA7265,5050;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 18");
            sbCommand.Clear ();

            sbCommand.Append ("PU;PA3750,8800;PD;PA3515,5050;PU;PA3281,5050;PD;PA3750,8565;PU;PA3750,8331;PD;PA3046,5050;");
            sbCommand.Append ("PU;PA2812,5050;PD;PA3750,8096;PU;PA3750,7862;PD;PA2578,5050;PU;PA2343,5050;PD;PA3750,7628;PU;PA3750,7393;PD;PA2109,");
            sbCommand.Append ("5050;PU;PA1875,5050;PD;PA3750,7159;PU;PA3750,6925;PD;PA1640,5050;PU;PA1406,5050;PD;PA3750,6690;PU;PA3750,6456;PD;PA");
            sbCommand.Append ("1171,5050;PU;PA937,5050;PD;PA3750,6221;PU;PA3750,5987;PD;PA703,5050;PU;PA468,5050;PD;PA3750,5753;PU;PA3750,5518;PD;");
            sbCommand.Append ("PA234,5050;PU;PA0,5050;PD;PA3750,5284;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 19");
            sbCommand.Clear ();

            spd.WriteTextString ("PA0,7060;SP6;", "UnitTestCSerialPortDriver4 20");

            sbCommand.Append ("DF;PA;SS;PU;PA7500,5050;PD;PA3750,5284;PU;PA3750,5518;PD;PA7265,5050;PU;PA7031,5050;PD;PA3");
            sbCommand.Append ("750,5753;PU;PA3750,5987;PD;PA6796,5050;PU;PA6562,5050;PD;PA3750,6221;PU;PA3750,6456;PD;PA6328,5050;PU;PA6093,5050;P");
            sbCommand.Append ("D;PA3750,6690;PU;PA3750,6925;PD;PA5859,5050;PU;PA5625,5050;PD;PA3750,7159;PU;PA3750,7393;PD;PA5390,5050;PU;PA5156,5");
            sbCommand.Append ("050;PD;PA3750,7628;PU;PA3750,7862;PD;PA4921,5050;PU;PA4687,5050;PD;PA3750,8096;PU;PA3750,8331;PD;PA4453,5050;PU;PA4");
            sbCommand.Append ("218,5050;PD;PA3750,8565;PU;PA3750,8800;PD;PA3984,5050;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 21");
            sbCommand.Clear ();

            sbCommand.Append ("PU;PA7500,1300;PD;PA7265,5050;PU;PA7031,5050;PD;PA7500,1534;PU;PA7500,1768;PD;PA6796,5050;");
            sbCommand.Append ("PU;PA6562,5050;PD;PA7500,2003;PU;PA7500,2237;PD;PA6328,5050;PU;PA6093,5050;PD;PA7500,2471;PU;PA7500,2706;PD;PA5859,");
            sbCommand.Append ("5050;PU;PA5625,5050;PD;PA7500,2940;PU;PA7500,3175;PD;PA5390,5050;PU;PA5156,5050;PD;PA7500,3409;PU;PA7500,3643;PD;PA");
            sbCommand.Append ("4921,5050;PU;PA4687,5050;PD;PA7500,3878;PU;PA7500,4112;PD;PA4453,5050;PU;PA4218,5050;PD;PA7500,4346;PU;PA7500,4581;");
            sbCommand.Append ("PD;PA3984,5050;PU;PA3750,5050;PD;PA7500,4815;PU;");
            spd.WriteTextString (sbCommand.ToString (), "UnitTestCSerialPortDriver4 22");
            sbCommand.Clear ();

            spd.WriteTextString ("PA0,7060;SP0;PA0,0;", "UnitTestCSerialPortDriver4 23");

            Thread.Sleep (500);
            Console.WriteLine ("  AbortPlot () ...");
            spd.AbortPlot ();
            Console.WriteLine ("QueueLength: " + spd.GetQueueLength ().ToString ());
            Console.WriteLine ("QueueSize: "   + spd.GetQueueSize ().ToString () + '\n');

            bool bTest = spd.WaitForQueue (3000);

            int iSpace = spd.GetPlotterBufferSpace ();
            try
            {
                while (iSpace < 5120 &&
                       iSpace > 0)
                {
                    Thread.Sleep (100);
                    Console.WriteLine ("(test) Buffer size: " + iSpace.ToString ());
                    iSpace = spd.GetPlotterBufferSpace ();
                }

                bTest = spd.WaitForQueue (3000);

                Console.WriteLine ("QueryActualPosition: " + spd.QueryActualPosition ());
                Console.WriteLine ("QueryActualPositionText: " + spd.QueryActualPositionText ());
            }
            catch (Exception e)
            {
                Console.WriteLine ("** Exception in UnitTestCSerialPortDriver4 (): " + e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine ("  " + e.InnerException.Message);
                }
                Console.WriteLine (e.StackTrace);
            }

            spd.CloseOutputPort (CSerialPortDriver.EClosePortMethod.EWaitForQueueAndClose);
        }

        public static void UnitTestCParallelPortDriver ()
        {
            Console.WriteLine ("GetPortName returns: " + CParallelPortDriver.GetPrinterName ());

            CParallelPortDriver.PrintToSpooler ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939" +
                      ",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669" +
                      ",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241" +
                      ",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758" +
                      ",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330" +
                      ",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060" +
                      ",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009" +
                      ",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190" +
                      ",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561" +
                      ",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034" +
                      ",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499" +
                      ",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848" +
                      ",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997" +
                      ",2930,2999,2965;PU;PA0,0;", CParallelPortDriver.GetPrinterName ());
            Console.WriteLine ("QueueLength: " + CParallelPortDriver.GetQueueLength ());
            Console.WriteLine ("QueueSize:   " + CParallelPortDriver.GetQueueSize () + '\n');

            CParallelPortDriver.PrintToSpooler ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939" +
                      ",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669" +
                      ",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241" +
                      ",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758" +
                      ",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330" +
                      ",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060" +
                      ",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009" +
                      ",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190" +
                      ",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561" +
                      ",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034" +
                      ",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499" +
                      ",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848" +
                      ",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997" +
                      ",2930,2999,2965;PU;PA0,0;", CParallelPortDriver.GetPrinterName ());
            Console.WriteLine ("QueueLength: " + CParallelPortDriver.GetQueueLength ());
            Console.WriteLine ("QueueSize:   " + CParallelPortDriver.GetQueueSize () + '\n');

            CParallelPortDriver.PrintToSpooler ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939" +
                      ",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669" +
                      ",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241" +
                      ",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758" +
                      ",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330" +
                      ",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060" +
                      ",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009" +
                      ",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190" +
                      ",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561" +
                      ",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034" +
                      ",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499" +
                      ",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848" +
                      ",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997" +
                      ",2930,2999,2965;PU;PA0,0;", CParallelPortDriver.GetPrinterName ());
            Console.WriteLine ("QueueLength: " + CParallelPortDriver.GetQueueLength ());
            Console.WriteLine ("QueueSize:   " + CParallelPortDriver.GetQueueSize () + '\n');

            CParallelPortDriver.PrintToSpooler ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939" +
                      ",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669" +
                      ",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241" +
                      ",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758" +
                      ",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330" +
                      ",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060" +
                      ",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009" +
                      ",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190" +
                      ",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561" +
                      ",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034" +
                      ",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499" +
                      ",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848" +
                      ",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997" +
                      ",2930,2999,2965;PU;PA0,0;", CParallelPortDriver.GetPrinterName ());
            Console.WriteLine ("QueueLength: " + CParallelPortDriver.GetQueueLength ());
            Console.WriteLine ("QueueSize:   " + CParallelPortDriver.GetQueueSize () + '\n');

            CParallelPortDriver.PrintToSpooler ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939" +
                      ",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669" +
                      ",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241" +
                      ",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758" +
                      ",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330" +
                      ",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060" +
                      ",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009" +
                      ",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190" +
                      ",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561" +
                      ",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034" +
                      ",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499" +
                      ",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848" +
                      ",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997" +
                      ",2930,2999,2965;PU;PA0,0;", CParallelPortDriver.GetPrinterName ());
            Console.WriteLine ("QueueLength: " + CParallelPortDriver.GetQueueLength ());
            Console.WriteLine ("QueueSize:   " + CParallelPortDriver.GetQueueSize () + '\n');

            CParallelPortDriver.PrintToSpooler ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939" +
                      ",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669" +
                      ",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241" +
                      ",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758" +
                      ",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330" +
                      ",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060" +
                      ",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009" +
                      ",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190" +
                      ",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561" +
                      ",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034" +
                      ",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499" +
                      ",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848" +
                      ",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997" +
                      ",2930,2999,2965;PU;PA0,0;", CParallelPortDriver.GetPrinterName ());
            Console.WriteLine ("QueueLength: " + CParallelPortDriver.GetQueueLength ());
            Console.WriteLine ("QueueSize:   " + CParallelPortDriver.GetQueueSize () + '\n');

            Console.WriteLine ("  ClearPrintQueue ()  ...");
            CParallelPortDriver.ClearPrintQueue ();
            Console.WriteLine ("QueueLength: " + CParallelPortDriver.GetQueueLength ().ToString ());
            Console.WriteLine ("QueueSize:   " + CParallelPortDriver.GetQueueSize ().ToString () + '\n');

            while (CParallelPortDriver.GetQueueLength () > 0)
            {
                Console.WriteLine ("Loop QueueLength: " + CParallelPortDriver.GetQueueLength ());
                Console.WriteLine ("Loop QueueSize:   " + CParallelPortDriver.GetQueueSize () + '\n');
                Thread.Sleep (50);
            }
            Console.WriteLine ("After Loop");

            StringBuilder sbCommand = new StringBuilder ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939");
            sbCommand.Append (",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669");
            sbCommand.Append (",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241");
            sbCommand.Append (",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758");
            sbCommand.Append (",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330");
            sbCommand.Append (",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060");
            sbCommand.Append (",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009");
            sbCommand.Append (",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190");
            sbCommand.Append (",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561");
            sbCommand.Append (",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034");
            sbCommand.Append (",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499");
            sbCommand.Append (",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848");
            sbCommand.Append (",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997");
            sbCommand.Append (",2930,2999,2965;PU;PA0,0;");

            sbCommand.Append ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939");
            sbCommand.Append (",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669");
            sbCommand.Append (",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241");
            sbCommand.Append (",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758");
            sbCommand.Append (",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330");
            sbCommand.Append (",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060");
            sbCommand.Append (",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009");
            sbCommand.Append (",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190");
            sbCommand.Append (",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561");
            sbCommand.Append (",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034");
            sbCommand.Append (",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499");
            sbCommand.Append (",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848");
            sbCommand.Append (",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997");
            sbCommand.Append (",2930,2999,2965;PU;PA0,0;");

            sbCommand.Append ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939");
            sbCommand.Append (",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669");
            sbCommand.Append (",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241");
            sbCommand.Append (",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758");
            sbCommand.Append (",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330");
            sbCommand.Append (",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060");
            sbCommand.Append (",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009");
            sbCommand.Append (",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190");
            sbCommand.Append (",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561");
            sbCommand.Append (",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034");
            sbCommand.Append (",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499");
            sbCommand.Append (",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848");
            sbCommand.Append (",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997");
            sbCommand.Append (",2930,2999,2965;PU;PA0,0;");

            sbCommand.Append ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939");
            sbCommand.Append (",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669");
            sbCommand.Append (",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241");
            sbCommand.Append (",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758");
            sbCommand.Append (",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330");
            sbCommand.Append (",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060");
            sbCommand.Append (",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009");
            sbCommand.Append (",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190");
            sbCommand.Append (",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561");
            sbCommand.Append (",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034");
            sbCommand.Append (",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499");
            sbCommand.Append (",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848");
            sbCommand.Append (",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997");
            sbCommand.Append (",2930,2999,2965;PU;PA0,0;");

            sbCommand.Append ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939");
            sbCommand.Append (",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669");
            sbCommand.Append (",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241");
            sbCommand.Append (",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758");
            sbCommand.Append (",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330");
            sbCommand.Append (",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060");
            sbCommand.Append (",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009");
            sbCommand.Append (",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190");
            sbCommand.Append (",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561");
            sbCommand.Append (",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034");
            sbCommand.Append (",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499");
            sbCommand.Append (",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848");
            sbCommand.Append (",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997");
            sbCommand.Append (",2930,2999,2965;PU;PA0,0;");

            string s = sbCommand.ToString ();
            int i = s.Length;
            CParallelPortDriver.PrintToSpooler (sbCommand.ToString (), CParallelPortDriver.GetPrinterName ());
        }

        public static void UnitTestCParallelPortDriver2 ()
        {
            Console.WriteLine ("GetPortName returns: " + CParallelPortDriver.GetPrinterName ());
            for (int iIdx = 0; iIdx < 10; ++iIdx)
            {
                CParallelPortDriver.PrintToSpooler ("PU;PA3000,3000;PD;PA2999,3034,2997,3069,2994,3104,2990,3139,2984,3173,2978,3207,2970,3241,2961,3275,2951,3309,2939" +
                                                    ",3342,2927,3374,2913,3406,2898,3438,2882,3469,2866,3500,2848,3529,2829,3559,2809,3587,2788,3615,2766,3642,2743,3669,2719,3694,2694,3719,2669" +
                                                    ",3743,2642,3766,2615,3788,2587,3809,2559,3829,2529,3848,2500,3866,2469,3882,2438,3898,2406,3913,2374,3927,2342,3939,2309,3951,2275,3961,2241" +
                                                    ",3970,2207,3978,2173,3984,2139,3990,2104,3994,2069,3997,2034,3999,1999,3999,1965,3999,1930,3997,1895,3994,1860,3990,1826,3984,1792,3978,1758" +
                                                    ",3970,1724,3961,1690,3951,1657,3939,1625,3927,1593,3913,1561,3898,1530,3882,1499,3866,1470,3848,1440,3829,1412,3809,1384,3788,1357,3766,1330" +
                                                    ",3743,1305,3719,1280,3694,1256,3669,1233,3642,1211,3615,1190,3587,1170,3559,1151,3529,1133,3499,1117,3469,1101,3438,1086,3406,1072,3374,1060" +
                                                    ",3342,1048,3309,1038,3275,1029,3241,1021,3207,1015,3173,1009,3139,1005,3104,1002,3069,1000,3034,1000,2999,1000,2965,1002,2930,1005,2895,1009" +
                                                    ",2860,1015,2826,1021,2792,1029,2758,1038,2724,1048,2690,1060,2657,1072,2625,1086,2593,1101,2561,1117,2530,1133,2499,1151,2470,1170,2440,1190" +
                                                    ",2412,1211,2384,1233,2357,1256,2330,1280,2305,1305,2280,1330,2256,1357,2233,1384,2211,1412,2190,1440,2170,1470,2151,1500,2133,1530,2117,1561" +
                                                    ",2101,1593,2086,1625,2072,1657,2060,1690,2048,1724,2038,1758,2029,1792,2021,1826,2015,1860,2009,1895,2005,1930,2002,1965,2000,1999,2000,2034" +
                                                    ",2000,2069,2002,2104,2005,2139,2009,2173,2015,2207,2021,2241,2029,2275,2038,2309,2048,2342,2060,2374,2072,2406,2086,2438,2101,2469,2117,2499" +
                                                    ",2133,2529,2151,2559,2170,2587,2190,2615,2211,2642,2233,2669,2256,2694,2280,2719,2305,2743,2330,2766,2357,2788,2384,2809,2412,2829,2440,2848" +
                                                    ",2470,2866,2499,2882,2530,2898,2561,2913,2593,2927,2625,2939,2657,2951,2690,2961,2724,2970,2758,2978,2792,2984,2826,2990,2860,2994,2895,2997" +
                                                    ",2930,2999,2965;PU;PA0,0;", "UnitTestCParallelPortDriver2");
            }
        }
        #endregion
    }

    public class CUDCPoints // User-Defined Character
    {
        private List<float> m_lfUdcPoints = new List<float> ();

        public int AddPointPair (float fX, float fY)
        {
            if (fX > -99.0F &&
                fX < 99.0F)
            {
                m_lfUdcPoints.Add (fX);
            }
            else
            {
                throw new Exception ("Value of fX out of range in CUDCPoints.AddPointPair ()");
            }

            if (fY > -99.0F &&
                fY < 99.0F)
            {
                m_lfUdcPoints.Add (fY);
            }
            else
            {
                throw new Exception ("Value of fY out of range in CUDCPoints.AddPointPair ()");
            }

            return m_lfUdcPoints.Count ();
        }

        public int AddPenDownStep ()
        {
            m_lfUdcPoints.Add (99.0F);

            return m_lfUdcPoints.Count ();
        }

        public int AddPenUpStep ()
        {
            m_lfUdcPoints.Add (-99.0F);

            return m_lfUdcPoints.Count ();
        }

        public void ClearPoints ()
        {
            m_lfUdcPoints.Clear ();
        }

        public List<float> GetPointsList ()
        {
            return m_lfUdcPoints;
        }
    }

    public class CGlobalPlotterSettings
    {
        const int DEFAULT_MASK_VALUE = 233;

        int   m_iMaskValue      = DEFAULT_MASK_VALUE; // IM 223,0,0  Chap 1  (223,0,0)
        bool  m_bScalePlot      = false;              // Scale  SC;  Chap 2  (Off)
        Point m_ptScaleP1       = new Point ();
        Point m_ptScaleP2       = new Point ();
        Point m_ptInputWindowP1 = new Point (0, 0);   // Input window  IW;  Chap 2  (Set to current hard-clip limits)
        Point m_ptInputWindowP2 = new Point (CHPGL.MAX_X_VALUE, CHPGL.MAX_Y_VALUE);

        public CGlobalPlotterSettings ()
        {
            Initialize ();
        }

        public CGlobalPlotterSettings (int iMaskValue, bool bScalePlot, Point ptScaleP1, Point ptScaleP2, Point ptInputWindowP1, Point ptInputWindowP2)
        {
            m_iMaskValue      = iMaskValue;
            m_bScalePlot      = bScalePlot;
            m_ptScaleP1       = ptScaleP1;
            m_ptScaleP2       = ptScaleP2;
            m_ptInputWindowP1 = ptInputWindowP1;
            m_ptInputWindowP2 = ptInputWindowP2;
        }

        protected void Initialize ()
        {
            // IN;
            ResetToDefault ();

            // Additional initialization
            //   Engineering A normal  250, 596   10250, 7796
            //   Engineering A rotated 154, 244   7354,  10244
            //   Engineering B normal  522, 259   15722, 10259
            //   Engineering B rotated 283, 934   10283, 16134
        }

        public void SetMaskValue (int iMaskValue)
        {
            if (iMaskValue < 0 ||
                iMaskValue > 223)
            {
                throw new Exception ("CPlotterConfigSettings.SetMaskValue value out of range");
            }

            m_iMaskValue = iMaskValue;
        }

        public int GetMaskValue () { return m_iMaskValue; }

        public void SetInputWindow (Point ptP1, Point ptP2)
        {
            m_ptInputWindowP1 = ptP1;
            m_ptInputWindowP2 = ptP2;
        }

        public void SetInputWindow (int iP1X, int iP1Y, int iP2X, int iP2Y)
        {
            m_ptInputWindowP1.X = iP1X;
            m_ptInputWindowP1.Y = iP1Y;
            m_ptInputWindowP2.X = iP2X;
            m_ptInputWindowP2.Y = iP2Y;
        }

        public void ResetInputWindow ()
        {
            m_ptInputWindowP1.X = 0;
            m_ptInputWindowP1.Y = 0;
            m_ptInputWindowP2.X = CHPGL.MAX_X_VALUE;
            m_ptInputWindowP2.Y = CHPGL.MAX_Y_VALUE;
        }

        public Point GetWindowP1 ()     { return m_ptInputWindowP1; }
        public Point GetWindowP2 ()     { return m_ptInputWindowP2; }
        public int   GetWindowDeltaX () { return Math.Abs (m_ptInputWindowP2.X - m_ptInputWindowP1.X); }
        public int   GetWindowDeltaY () { return Math.Abs (m_ptInputWindowP2.Y - m_ptInputWindowP1.Y); }
        public int   GetWindowDeltaDiagonal ()
        {
            return CGenericMethods.GetWindowDeltaDiagonal (m_ptInputWindowP1, m_ptInputWindowP2);
        }

        public void SetScale (Point ptP1, Point ptP2)
        {
            m_bScalePlot = true;
            m_ptScaleP1  = ptP1;
            m_ptScaleP2  = ptP2;
        }

        public void SetScale (int iP1X, int iP1Y, int iP2X, int iP2Y)
        {
            m_bScalePlot  = true;
            m_ptScaleP1.X = iP1X;
            m_ptScaleP1.Y = iP1Y;
            m_ptScaleP2.X = iP2X;
            m_ptScaleP2.Y = iP2Y;
        }

        public void ResetScale ()
        {
            m_bScalePlot  = false;
            m_ptScaleP1.X = 0;
            m_ptScaleP1.Y = 0;
            m_ptScaleP2.X = CHPGL.MAX_X_VALUE;
            m_ptScaleP2.Y = CHPGL.MAX_Y_VALUE;
        }

        public void ResetToDefault ()
        {
            m_iMaskValue = DEFAULT_MASK_VALUE;

            ResetInputWindow ();
            ResetScale ();
        }

        public bool IsEqualTo (CGlobalPlotterSettings that)
        {
            return (this.m_iMaskValue      == that.m_iMaskValue      &&
                    this.m_bScalePlot      == that.m_bScalePlot      &&
                    this.m_ptScaleP1       == that.m_ptScaleP1       &&
                    this.m_ptScaleP2       == that.m_ptScaleP2       &&
                    this.m_ptInputWindowP1 == that.m_ptInputWindowP1 &&
                    this.m_ptInputWindowP2 == that.m_ptInputWindowP2);
        }

        public void LoadValuesFromString (string strConfigValues)
        {
            string[] straValues = CGenericMethods.ParseDelimitedString (strConfigValues);

            m_iMaskValue        = CGenericMethods.SafeConvertToInt (straValues[0]);
            m_bScalePlot        = straValues[1] == "T";
            m_ptScaleP1.X       = CGenericMethods.SafeConvertToInt (straValues[2]);
            m_ptScaleP1.Y       = CGenericMethods.SafeConvertToInt (straValues[3]);
            m_ptScaleP2.X       = CGenericMethods.SafeConvertToInt (straValues[4]);
            m_ptScaleP2.Y       = CGenericMethods.SafeConvertToInt (straValues[5]);
            m_ptInputWindowP1.X = CGenericMethods.SafeConvertToInt (straValues[6]);
            m_ptInputWindowP1.Y = CGenericMethods.SafeConvertToInt (straValues[7]);
            m_ptInputWindowP2.X = CGenericMethods.SafeConvertToInt (straValues[8]);
            m_ptInputWindowP2.Y = CGenericMethods.SafeConvertToInt (straValues[9]);
        }

        public string SaveConfigValuesToString ()
        {
            string strConfigValues = string.Format ("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                                                    m_iMaskValue,
                                                    m_bScalePlot ? 'T' : 'F',
                                                    m_ptScaleP1.X,
                                                    m_ptScaleP1.Y,
                                                    m_ptScaleP2.X,
                                                    m_ptScaleP2.Y,
                                                    m_ptInputWindowP1.X,
                                                    m_ptInputWindowP1.Y,
                                                    m_ptInputWindowP2.X,
                                                    m_ptInputWindowP2.Y);

            return strConfigValues;
        }

        public string GetHPGL ()
        {
            StringBuilder sbHPGL = new StringBuilder (CHPGL.InputMask   (m_iMaskValue)                                         +  // IM 223,0,0  Chap 1  (223,0,0)
                                                      (m_bScalePlot ? CHPGL.Scale (m_ptScaleP1, m_ptScaleP2) : CHPGL.Scale ()) +  // Scale  SC;  Chap 2  (Off)
                                                      CHPGL.InputWindow (m_ptInputWindowP1, m_ptInputWindowP2));                  // Input window  IW;  Chap 2  (Set to current hard-clip limits)

            return sbHPGL.ToString ();
        }
    }

    #region SHAPE ELEMENTS
    public class CShapeConfigSettings
    {
        #region DATA DEFINITIONS
        // Sort keys:
        protected const int   DEFAULT_PEN                             = -1;
        protected const int   DEFAULT_SORT_GROUP                      = 0;
        protected const int   DEFAULT_PEN_UP_TRAVEL_DISTANCE          = -1;

        // Line settings:
        protected const bool  DEFAULT_PLOT_ABSOLUTE                   = true;      // PA;     Chap 3  (Absolute (PA))
        public    const int   DEFAULT_LINE_TYPE                       = -1;        // LT;     Chap 4  (0 - Solid line)
        protected const float DEFAULT_LINE_PATTERN_LENGTH             = 0.0F;      // LT;     Chap 4  (4% of the diagonal distance between P1 to P2)
        protected const float DEFAULT_PEN_VELOCITY                    = 0.0F;      // VS;     Chap 3  (38.1 cm/s (15 in./s))
        protected const float DEFAULT_PEN_THICKNESS                   = 0.0F;      // PT;     Chap 3  (Set to 0.3 mm)
        protected const char  DEFAULT_SYMBOL_MODE_CHARACTER           = ' ';       // SM;     Chap 3  (Off)
        protected const float DEFAULT_TICK_LENGHT_POSITIVE_X          = 0.0F;      // TL;     Chap 3  (tp = tn = 0.5% of (P2 x - P1 x) for Y-tick and
        protected const float DEFAULT_TICK_LENGHT_NEGATIVE_X          = 0.0F;      //                            0.5% of (P2 y - P1 y) for X-tick)
        protected const float DEFAULT_TICK_LENGHT_POSITIVE_Y          = 0.0F;      // TL;     Chap 3  (tp = tn = 0.5% of (P2 x - P1 x) for Y-tick and
        protected const float DEFAULT_TICK_LENGHT_NEGATIVE_Y          = 0.0F;      //                            0.5% of (P2 y - P1 y) for X-tick)
        protected const int   DEFAULT_FILL_TYPE                       = 1;         // FT;     Chap 3  (Set to type 1, bidirectional solid fill)
        protected const float DEFAULT_FILL_TYPE_SPACING               = 0.0F;      // FT;     Chap 3  (1% of the diagonal distance between P1 and P2)
        protected const int   DEFAULT_FILL_TYPE_ANGLE                 = 0;         // FT;     Chap 3  (Set to 0 degrees)

        // Character settings:
        protected const float DEFAULT_CHARACTER_PRINT_ANGLE           = 0.0F;
        protected const float DEFAULT_ABSOLUTE_CHAR_DIRECTION_RUN     = 0.0F;      // DR1,O;  Chap 5  (Horizontal (DR1,O))
        protected const float DEFAULT_ABSOLUTE_CHAR_DIRECTION_RISE    = 0.0F;      // DR1,O;  Chap 5  (Horizontal (DR1,O))
        //protected const float DEFAULT_ABSOLUTE_CHARACTER_SIZE_WIDTH   = 0.0F;      // SR;     Chap 5  Width = 0.75% of (P2 x - P1 x)
        //protected const float DEFAULT_ABSOLUTE_CHARACTER_SIZE_HEIGHT  = 0.0F;      //                 Height = 1.5% of (P2 y - P1 y)
        protected const float DEFAULT_RELATIVE_CHAR_DIRECTION_RUN     = 0.0F;      // DR1,O;  Chap 5  (Horizontal (DR1,O))
        protected const float DEFAULT_RELATIVE_CHAR_DIRECTION_RISE    = 0.0F;      // DR1,O;  Chap 5  (Horizontal (DR1,O))
        //protected const float DEFAULT_RELATIVE_CHARACTER_SIZE_WIDTH   = 0.0F;      // SR;     Chap 5  Width = 0.75% of (P2 x - P1 x)
        //protected const float DEFAULT_RELATIVE_CHARACTER_SIZE_HEIGHT  = 0.0F;      //                 Height = 1.5% of (P2 y - P1 y)
        protected const int   DEFAULT_STANDARD_CHARACTER_SET          = 0;         // CSO;    Chap 5  (Set 0)
        protected const int   DEFAULT_ALTERNATE_CHARACTER_SET         = 0;         // CAO;    Chap 5  (Set 0)
        protected const bool  DEFAULT_STANDARD_CHARACTER_SET_SELECTED = true;      // SS;     Chap 5  (Standard)
        protected const float DEFAULT_CHARACTER_SLANT_ANGLE           = 0.0F;      // SLO;    Chap 5  (0 degrees)
        protected const char  DEFAULT_LABEL_TERMINATOR                = CHPGL.ETX; // DT<ETX> Chap 5  (ETX (ASCII decimal equivalent 3))
        protected const float CHARACTER_SIZE_TO_SPACE_FACTOR          = 1.37F;

        public static CGlobalPlotterSettings s_objGPS = new CGlobalPlotterSettings ();

        // Dynamic default values:
        protected float m_fDefaultAbsCharSizeWidth      = 0.0F;                                    // 0.0F      SR;     Chap 5  Width = 0.75% of (P2 x - P1 x)
        protected float m_fDefaultAbsCharSizeHeight     = 0.0F;                                    // 0.0F                      Height = 1.5% of (P2 y - P1 y)
        protected float m_fDefaultRelCharSizeWidth      = 0.0F;                                    // 0.0F      SR;     Chap 5  Width = 0.75% of (P2 x - P1 x)
        protected float m_fDefaultRelCharSizeHeight     = 0.0F;                                    // 0.0F                      Height = 1.5% of (P2 y - P1 y)
        protected float m_fDefaultFillTypeSpacing       = 0.0F;                                    // 0.0F      FT;     Chap 3  (1% of the diagonal distance between P1 and P2)
        protected float m_fDefaultLinePatternLength     = 0.0F;                                    // 0.0F      LT;     Chap 3  (4% of the diagonal distance between P1 to P2)

        // Sort keys:
        protected int   m_iSortGroup                    = DEFAULT_SORT_GROUP;
        protected int   m_iPenUpTravelDistance          = DEFAULT_PEN_UP_TRAVEL_DISTANCE;

        // Line settings:
        protected bool  m_bPlotAbsolute                 = DEFAULT_PLOT_ABSOLUTE;                   // true      PA;     Chap 3  (Absolute (PA))
        protected int   m_iLineType                     = DEFAULT_LINE_TYPE;                       // 0         LT;     Chap 3  (0 - Solid line)
        protected float m_fLinePatternLength            = DEFAULT_LINE_PATTERN_LENGTH;             // 0.0F      LT;     Chap 3  (4% of the diagonal distance between P1 to P2)
        protected float m_fPenVelocity                  = DEFAULT_PEN_VELOCITY;                    // 0.0F      VS;     Chap 3  (38.1 cm/s (15 in./s))
        protected float m_fPenThickness                 = DEFAULT_PEN_THICKNESS;                   // 0.0F      PT;     Chap 3  (Set to 0.3 mm)
        protected char  m_cSymbolModeCharacter          = DEFAULT_SYMBOL_MODE_CHARACTER;           // ' '       SM;     Chap 3  (Off)
        //protected float m_fTickLenghtPositiveX          = DEFAULT_TICK_LENGHT_POSITIVE_X;          // 0.0F      TL;     Chap 3  (tp = tn = 0.5% of (P2 x - P1 x) for Y-tick and
        //protected float m_fTickLenghtNegativeX          = DEFAULT_TICK_LENGHT_NEGATIVE_X;          // 0.0F                                 0.5% of (P2 y - P1 y) for X-tick)
        //protected float m_fTickLenghtPositiveY          = DEFAULT_TICK_LENGHT_POSITIVE_Y;          // 0.0F      TL;     Chap 3  (tp = tn = 0.5% of (P2 x - P1 x) for Y-tick and
        //protected float m_fTickLenghtNegativeY          = DEFAULT_TICK_LENGHT_NEGATIVE_Y;          // 0.0F                                 0.5% of (P2 y - P1 y) for X-tick)
        protected int   m_iFillType                     = DEFAULT_FILL_TYPE;                       // 1         FT;     Chap 3  (Set to type 1, bidirectional solid fill)
        protected float m_fFillTypeSpacing              = DEFAULT_FILL_TYPE_SPACING;               // 0.0F      FT;     Chap 3  (1% of the diagonal distance between P1 and P2)
        protected int   m_iFillTypeAngle                = DEFAULT_FILL_TYPE_ANGLE;                 // 0         FT;     Chap 3  (Set to 0 degrees)

        // Character settings:
        protected float m_fCharacterPrintAngle          = DEFAULT_CHARACTER_PRINT_ANGLE;
        protected float m_fAbsoluteCharDirectionRun     = DEFAULT_ABSOLUTE_CHAR_DIRECTION_RUN;     // 0         DR1,O;  Chap 5  (Horizontal (DR1,O))
        protected float m_fAbsoluteCharDirectionRise    = DEFAULT_ABSOLUTE_CHAR_DIRECTION_RISE;    // 0         DR1,O;  Chap 5  (Horizontal (DR1,O))
        protected float m_fAbsoluteCharacterSizeWidth   = 0.0F;                                    // 0.0F      SR;     Chap 5  Width = 0.75% of (P2 x - P1 x)
        protected float m_fAbsoluteCharacterSizeHeight  = 0.0F;                                    // 0.0F                      Height = 1.5% of (P2 y - P1 y)
        protected float m_fRelativeCharDirectionRun     = DEFAULT_RELATIVE_CHAR_DIRECTION_RUN;     // 0         DR1,O;  Chap 5  (Horizontal (DR1,O))
        protected float m_fRelativeCharDirectionRise    = DEFAULT_RELATIVE_CHAR_DIRECTION_RISE;    // 0         DR1,O;  Chap 5  (Horizontal (DR1,O))
        protected float m_fRelativeCharacterSizeWidth   = 0.0F;                                    // 0.0F      SR;     Chap 5  Width = 0.75% of (P2 x - P1 x)
        protected float m_fRelativeCharacterSizeHeight  = 0.0F;                                    // 0.0F                      Height = 1.5% of (P2 y - P1 y)
        protected int   m_iStandardCharacterSet         = DEFAULT_STANDARD_CHARACTER_SET;          // 0         CSO;    Chap 5  (Set 0)
        protected int   m_iAlternateCharacterSet        = DEFAULT_ALTERNATE_CHARACTER_SET;         // 0         CAO;    Chap 5  (Set 0)
        protected bool  m_bStandardCharacterSetSelected = DEFAULT_STANDARD_CHARACTER_SET_SELECTED; // true      SS;     Chap 5  (Standard)
        protected float m_fCharacterSlantAngle          = DEFAULT_CHARACTER_SLANT_ANGLE;           // 0.0F      SLO;    Chap 5  (0 degrees)
        protected char  m_cLabelTerminator              = DEFAULT_LABEL_TERMINATOR;                // CHPGL.ETX DT<ETX> Chap 5  (ETX (ASCII decimal equivalent 3))
        #endregion

        #region GET METHODS
        // Sort keys:
        public int   GetSortGroup ()                    { return m_iSortGroup; }
        public int   GetPenUpTravelDistance ()          { return m_iPenUpTravelDistance; }

        // Line settings:                               
        public bool  IsAbsolutePlotMode ()              { return m_bPlotAbsolute; }
        public int   GetLineType ()                     { return m_iLineType; }
        public float GetLinePatternLength ()            { return m_fLinePatternLength; }
        public float GetPenVelocity ()                  { return m_fPenVelocity; }
        public float GetPenThickness ()                 { return m_fPenThickness; }
        public char  GetSymbolModeCharacter ()          { return m_cSymbolModeCharacter; }
        //public float GetTickLenghtPositiveX ()          { return m_fTickLenghtPositiveX; }
        //public float GetTickLenghtNegativeX ()          { return m_fTickLenghtNegativeX; }
        //public float GetTickLenghtPositiveY ()          { return m_fTickLenghtPositiveY; }
        //public float GetTickLenghtNegativeY ()          { return m_fTickLenghtNegativeY; }
        public int   GetFillType ()                     { return m_iFillType; }
        public float GetFillTypeSpacing ()              { return m_fFillTypeSpacing; }
        public int   GetFillTypeAngle ()                { return m_iFillTypeAngle; }

        // Character settings:
        public float GetCharacterPrintAngle ()          { return m_fCharacterPrintAngle; }
        public float GetAbsoluteCharDirectionRun ()     { return m_fAbsoluteCharDirectionRun; }
        public float GetAbsoluteCharDirectionRise ()    { return m_fAbsoluteCharDirectionRise; }
        public float GetAbsoluteCharacterSizeWidth ()   { return m_fAbsoluteCharacterSizeWidth; }
        public float GetAbsoluteCharacterSizeHeight ()  { return m_fAbsoluteCharacterSizeHeight; }
        public float GetRelativeCharDirectionRun ()     { return m_fRelativeCharDirectionRun; }
        public float GetRelativeCharDirectionRise ()    { return m_fRelativeCharDirectionRise; }
        public float GetRelativeCharacterSizeWidth ()   { return m_fRelativeCharacterSizeWidth; }
        public float GetRelativeCharacterSizeHeight ()  { return m_fRelativeCharacterSizeHeight; }
        public int   GetStandardCharacterSet ()         { return m_iStandardCharacterSet; }
        public int   GetAlternateCharacterSet ()        { return m_iAlternateCharacterSet; }
        public bool  GetStandardCharacterSetSelected () { return m_bStandardCharacterSetSelected; }
        public float GetCharacterSlantAngle ()          { return m_fCharacterSlantAngle; }
        public char  GetLabelTerminator ()              { return m_cLabelTerminator; }
        #endregion

        #region SET METHODS
        // Sort keys:
        public void SetSortGroup                    (int iSortGroup)                      { m_iSortGroup                    = iSortGroup; }
        public void SetPenUpTravelDistance          (int iPenUpTravelDistance)            { m_iPenUpTravelDistance          = iPenUpTravelDistance; }

        // Line settings:                                                                 
        public void SetAbsolutePlotMode ()          { m_bPlotAbsolute = true; }
        public void SetRelativePlotMode ()          { m_bPlotAbsolute = false; }
        public void SetLineType                     (int   iLineType)                     { m_iLineType                     = iLineType; }
        public void SetLinePatternLength            (float fLinePatternLength)            { m_fLinePatternLength            = fLinePatternLength; }
        public void SetPenVelocity                  (float fPenVelocity)                  { m_fPenVelocity                  = fPenVelocity; }
        public void SetPenThickness                 (float fPenThickness)                 { m_fPenThickness                 = fPenThickness; }
        public void SetSymbolModeCharacter          (char  cSymbolModeCharacter)          { m_cSymbolModeCharacter          = cSymbolModeCharacter; }
        //public void SetTickLenghtPositiveX          (float fTickLenghtPositiveX)          { m_fTickLenghtPositiveX          = fTickLenghtPositiveX; }
        //public void SetTickLenghtNegativeX          (float fTickLenghtNegativeX)          { m_fTickLenghtNegativeX          = fTickLenghtNegativeX; }
        //public void SetTickLenghtPositiveY          (float fTickLenghtPositiveY)          { m_fTickLenghtPositiveY          = fTickLenghtPositiveY; }
        //public void SetTickLenghtNegativeY          (float fTickLenghtNegativeY)          { m_fTickLenghtNegativeY          = fTickLenghtNegativeY; }
        public void SetFillType                     (int   iFillType)                     { m_iFillType                     = iFillType; }
        public void SetFillTypeSpacing              (float fFillTypeSpacing)              { m_fFillTypeSpacing              = fFillTypeSpacing; }
        public void SetFillTypeAngle                (int   iFillTypeAngle)                { m_iFillTypeAngle                = iFillTypeAngle; }

        // Character settings:
        public void SetCharacterPrintAngle          (float fCharacterPrintAngle)          { m_fCharacterPrintAngle          = fCharacterPrintAngle; }
        public void SetAbsoluteCharDirectionRun     (float fAbsoluteCharDirectionRun)     { m_fAbsoluteCharDirectionRun     = fAbsoluteCharDirectionRun; }
        public void SetAbsoluteCharDirectionRise    (float fAbsoluteCharDirectionRise)    { m_fAbsoluteCharDirectionRise    = fAbsoluteCharDirectionRise; }
        public void SetAbsoluteCharacterSizeWidth   (float fAbsoluteCharacterSizeWidth)   { m_fAbsoluteCharacterSizeWidth   = fAbsoluteCharacterSizeWidth; }
        public void SetAbsoluteCharacterSizeHeight  (float fAbsoluteCharacterSizeHeight)  { m_fAbsoluteCharacterSizeHeight  = fAbsoluteCharacterSizeHeight; }
        public void SetRelativeCharDirectionRun     (float fRelativeCharDirectionRun)     { m_fRelativeCharDirectionRun     = fRelativeCharDirectionRun; }
        public void SetRelativeCharDirectionRise    (float fRelativeCharDirectionRise)    { m_fRelativeCharDirectionRise    = fRelativeCharDirectionRise; }
        public void SetRelativeCharacterSizeWidth   (float fRelativeCharacterSizeWidth)   { m_fRelativeCharacterSizeWidth   = fRelativeCharacterSizeWidth; }
        public void SetRelativeCharacterSizeHeight  (float fRelativeCharacterSizeHeight)  { m_fRelativeCharacterSizeHeight  = fRelativeCharacterSizeHeight; }
        public void SetStandardCharacterSet         (int   iStandardCharacterSet)         { m_iStandardCharacterSet         = iStandardCharacterSet; }
        public void SetAlternateCharacterSet        (int   iAlternateCharacterSet)        { m_iAlternateCharacterSet        = iAlternateCharacterSet; }
        public void SetStandardCharacterSetSelected (bool  bStandardCharacterSetSelected) { m_bStandardCharacterSetSelected = bStandardCharacterSetSelected; }
        public void SetCharacterSlantAngle          (float fCharacterSlantAngle)          { m_fCharacterSlantAngle          = fCharacterSlantAngle; }
        public void SetLabelTerminator              (char  cLabelTerminator)              { m_cLabelTerminator              = cLabelTerminator; }
        #endregion

        public CShapeConfigSettings ()
        {
            ResetToDefault ();
        }

        protected void ResetToDefault ()
        {
            // Dynamic default values
            m_fDefaultAbsCharSizeWidth     = (float)Math.Round (s_objGPS.GetWindowDeltaX () * .0075, 2);      // SR;     Chap 5  Width = 0.75% of (P2 x - P1 x)
            m_fDefaultAbsCharSizeHeight    = (float)Math.Round (s_objGPS.GetWindowDeltaX () * .015, 2);       //                 Height = 1.5% of (P2 y - P1 y)
            m_fDefaultRelCharSizeWidth     = (float)Math.Round (s_objGPS.GetWindowDeltaX () * .0075, 2);      // SR;     Chap 5  Width = 0.75% of (P2 x - P1 x)
            m_fDefaultRelCharSizeHeight    = (float)Math.Round (s_objGPS.GetWindowDeltaX () * .015, 2);       //                 Height = 1.5% of (P2 y - P1 y)
            m_fDefaultFillTypeSpacing      = (float)Math.Round (s_objGPS.GetWindowDeltaDiagonal () * .01, 2); // FT;     Chap 3  (1% of the diagonal distance between P1 and P2)
            m_fDefaultLinePatternLength    = (float)Math.Round (s_objGPS.GetWindowDeltaDiagonal () * .04, 2); // 0.0F      LT;     Chap 3  (4% of the diagonal distance between P1 to P2)

            // Line settings:                                        
            m_bPlotAbsolute                 = DEFAULT_PLOT_ABSOLUTE;
            m_iLineType                     = DEFAULT_LINE_TYPE;                                               // (-1 - Solid line)
            m_fLinePatternLength            = m_fDefaultLinePatternLength;                                     // LT;  Chap 3  (4% of the diagonal distance between P1 to P2)
            m_fPenVelocity                  = DEFAULT_PEN_VELOCITY;                                            // 38.1F;
            m_fPenThickness                 = DEFAULT_PEN_THICKNESS;                                           // 0.3F;
            m_cSymbolModeCharacter          = DEFAULT_SYMBOL_MODE_CHARACTER;                                   // CHPGL.NUL;
            //m_fTickLenghtPositiveX          = (float)Math.Round (s_objGPS.GetWindowDeltaX () * .005, 2);       // TL;  Chap 3  (tp = tn = 0.5% of (P2 x - P1 x) for Y-tick and
            //m_fTickLenghtNegativeX          = (float)Math.Round (s_objGPS.GetWindowDeltaX () * .005, 2);       //                         0.5% of (P2 y - P1 y) for X-tick)
            //m_fTickLenghtPositiveY          = (float)Math.Round (s_objGPS.GetWindowDeltaY () * .005, 2);       // TL;  Chap 3  (tp = tn = 0.5% of (P2 x - P1 x) for Y-tick and
            //m_fTickLenghtNegativeY          = (float)Math.Round (s_objGPS.GetWindowDeltaY () * .005, 2);       //                         0.5% of (P2 y - P1 y) for X-tick)
            m_iFillType                     = DEFAULT_FILL_TYPE;                                               // 1;
            m_fFillTypeSpacing              = m_fDefaultFillTypeSpacing;                                       // FT;  Chap 3  (1% of the diagonal distance between P1 and P2)
            m_iFillTypeAngle                = DEFAULT_FILL_TYPE_ANGLE;                                         // 0;

            // Character settings:                                   
            m_fAbsoluteCharDirectionRun     = DEFAULT_RELATIVE_CHAR_DIRECTION_RUN;                             // 1         DR1,O;  Chap 5  (Horizontal (DR1,O))
            m_fAbsoluteCharDirectionRise    = DEFAULT_RELATIVE_CHAR_DIRECTION_RISE;                            // 0         DR1,O;  Chap 5  (Horizontal (DR1,O))
            m_fAbsoluteCharacterSizeWidth   = m_fDefaultAbsCharSizeWidth;                                      // SR;     Chap 5  Width = 0.75% of (P2 x - P1 x)
            m_fAbsoluteCharacterSizeHeight  = m_fDefaultAbsCharSizeHeight;                                     //                 Height = 1.5% of (P2 y - P1 y)
            m_fRelativeCharDirectionRun     = DEFAULT_RELATIVE_CHAR_DIRECTION_RUN;                             // 1         DR1,O;  Chap 5  (Horizontal (DR1,O))
            m_fRelativeCharDirectionRise    = DEFAULT_RELATIVE_CHAR_DIRECTION_RISE;                            // 0         DR1,O;  Chap 5  (Horizontal (DR1,O))
            m_fRelativeCharacterSizeWidth   = m_fDefaultRelCharSizeWidth;                                      // SR;     Chap 5  Width = 0.75% of (P2 x - P1 x)
            m_fRelativeCharacterSizeHeight  = m_fDefaultRelCharSizeHeight;                                     //                 Height = 1.5% of (P2 y - P1 y)
            m_iStandardCharacterSet         = DEFAULT_STANDARD_CHARACTER_SET;                                  // 0         CSO;    Chap 5  (Set 0)
            m_iAlternateCharacterSet        = DEFAULT_ALTERNATE_CHARACTER_SET;                                 // 0         CAO;    Chap 5  (Set 0)
            m_bStandardCharacterSetSelected = DEFAULT_STANDARD_CHARACTER_SET_SELECTED;                         // true      SS;     Chap 5  (Standard)
            m_fCharacterSlantAngle          = DEFAULT_CHARACTER_SLANT_ANGLE;                                   // 0.0F      SLO;    Chap 5  (0 degrees)
            m_cLabelTerminator              = DEFAULT_LABEL_TERMINATOR;                                        // CHPGL.ETX DT<ETX> Chap 5  (ETX (ASCII decimal equivalent 3))
        }

#if DEBUG
        public void SetTestValues ()
        {
            // Sort keys:
            m_iSortGroup                    = 2;
            m_iPenUpTravelDistance          = 202;

            // Line settings:                                        
            m_bPlotAbsolute                 = false;
            m_iLineType                     = 3;
            m_fLinePatternLength            = 55.5F;
            m_fPenVelocity                  = 38.1F;
            m_fPenThickness                 = 0.3F;
            m_cSymbolModeCharacter          = CHPGL.LF;
            //m_fTickLenghtPositiveX          = 13;
            //m_fTickLenghtNegativeX          = 14;
            //m_fTickLenghtPositiveY          = 15;
            //m_fTickLenghtNegativeY          = 16;
            m_iFillType                     = 2;
            m_fFillTypeSpacing              = 21.0F;
            m_iFillTypeAngle                = 0;

            // Character settings:                                   
            m_fAbsoluteCharDirectionRun     = 21;
            m_fAbsoluteCharDirectionRise    = 20;
            m_fAbsoluteCharacterSizeWidth   = 50.0F;
            m_fAbsoluteCharacterSizeHeight  = 55.5F;
            m_fRelativeCharDirectionRun     = 11;
            m_fRelativeCharDirectionRise    = 10;
            m_fRelativeCharacterSizeWidth   = 40.0F;
            m_fRelativeCharacterSizeHeight  = 35.5F;
            m_iStandardCharacterSet         = 4;
            m_iAlternateCharacterSet        = 2;
            m_bStandardCharacterSetSelected = false;
            m_fCharacterSlantAngle          = 5;
            m_cLabelTerminator              = CHPGL.CR;
        }
#endif

        public bool IsConfigEqualTo (CShapeConfigSettings that)
        {
            //return this.m_bPlotAbsolute                 == that.m_bPlotAbsolute                 &&
            //       this.m_iLineType                     == that.m_iLineType                     &&
            //       this.m_fLinePatternLength            == that.m_fLinePatternLength            &&
            //       this.m_fPenVelocity                  == that.m_fPenVelocity                  &&
            //       this.m_fPenThickness                 == that.m_fPenThickness                 &&
            //       this.m_cSymbolModeCharacter          == that.m_cSymbolModeCharacter          &&
            //       this.m_fTickLenghtPositiveX          == that.m_fTickLenghtPositiveX          &&
            //       this.m_fTickLenghtNegativeX          == that.m_fTickLenghtNegativeX          &&
            //       this.m_fTickLenghtPositiveY          == that.m_fTickLenghtPositiveY          &&
            //       this.m_fTickLenghtNegativeY          == that.m_fTickLenghtNegativeY          &&
            //       this.m_iFillType                     == that.m_iFillType                     &&
            //       this.m_fFillTypeSpacing              == that.m_fFillTypeSpacing              &&
            //       this.m_iFillTypeAngle                == that.m_iFillTypeAngle                &&
            //       this.m_fAbsoluteCharDirectionRun     == that.m_fAbsoluteCharDirectionRun     &&
            //       this.m_fAbsoluteCharDirectionRise    == that.m_fAbsoluteCharDirectionRise    &&
            //       this.m_fAbsoluteCharacterSizeWidth   == that.m_fAbsoluteCharacterSizeWidth   &&
            //       this.m_fAbsoluteCharacterSizeHeight  == that.m_fAbsoluteCharacterSizeHeight  &&
            //       this.m_fRelativeCharDirectionRun     == that.m_fRelativeCharDirectionRun     &&
            //       this.m_fRelativeCharDirectionRise    == that.m_fRelativeCharDirectionRise    &&
            //       this.m_fRelativeCharacterSizeWidth   == that.m_fRelativeCharacterSizeWidth   &&
            //       this.m_fRelativeCharacterSizeHeight  == that.m_fRelativeCharacterSizeHeight  &&
            //       this.m_iStandardCharacterSet         == that.m_iStandardCharacterSet         &&
            //       this.m_iAlternateCharacterSet        == that.m_iAlternateCharacterSet        &&
            //       this.m_bStandardCharacterSetSelected == that.m_bStandardCharacterSetSelected &&
            //       this.m_fCharacterSlantAngle          == that.m_fCharacterSlantAngle          &&
            //       this.m_cLabelTerminator              == that.m_cLabelTerminator;

            //if (this.m_iSortGroup                    != that.m_iSortGroup)
            //{
            //    return false;
            //}
            //if (this.m_iPenUpTravelDistance          != that.m_iPenUpTravelDistance)
            //{
            //    return false;
            //}
            if (this.m_bPlotAbsolute != that.m_bPlotAbsolute)
            {
                return false;
            }
            if (this.m_iLineType != that.m_iLineType)
            {
                return false;
            }
            if (this.m_fLinePatternLength != that.m_fLinePatternLength)
            {
                return false;
            }
            if (this.m_fPenVelocity != that.m_fPenVelocity)
            {
                return false;
            }
            if (this.m_fPenThickness != that.m_fPenThickness)
            {
                return false;
            }
            if (this.m_cSymbolModeCharacter != that.m_cSymbolModeCharacter)
            {
                return false;
            }
            //if (this.m_fTickLenghtPositiveX != that.m_fTickLenghtPositiveX)
            //{
            //    return false;
            //}
            //if (this.m_fTickLenghtNegativeX != that.m_fTickLenghtNegativeX)
            //{
            //    return false;
            //}
            //if (this.m_fTickLenghtPositiveY != that.m_fTickLenghtPositiveY)
            //{
            //    return false;
            //}
            //if (this.m_fTickLenghtNegativeY != that.m_fTickLenghtNegativeY)
            //{
            //    return false;
            //}
            if (this.m_iFillType != that.m_iFillType)
            {
                return false;
            }
            if (this.m_fFillTypeSpacing != that.m_fFillTypeSpacing)
            {
                return false;
            }
            if (this.m_iFillTypeAngle != that.m_iFillTypeAngle)
            {
                return false;
            }
            if (this.m_fAbsoluteCharDirectionRun != that.m_fAbsoluteCharDirectionRun)
            {
                return false;
            }
            if (this.m_fAbsoluteCharDirectionRise != that.m_fAbsoluteCharDirectionRise)
            {
                return false;
            }
            if (this.m_fAbsoluteCharacterSizeWidth != that.m_fAbsoluteCharacterSizeWidth)
            {
                return false;
            }
            if (this.m_fAbsoluteCharacterSizeHeight != that.m_fAbsoluteCharacterSizeHeight)
            {
                return false;
            }
            if (this.m_fRelativeCharDirectionRun != that.m_fRelativeCharDirectionRun)
            {
                return false;
            }
            if (this.m_fRelativeCharDirectionRise != that.m_fRelativeCharDirectionRise)
            {
                return false;
            }
            if (this.m_fRelativeCharacterSizeWidth != that.m_fRelativeCharacterSizeWidth)
            {
                return false;
            }
            if (this.m_fRelativeCharacterSizeHeight != that.m_fRelativeCharacterSizeHeight)
            {
                return false;
            }
            if (this.m_iStandardCharacterSet != that.m_iStandardCharacterSet)
            {
                return false;
            }
            if (this.m_iAlternateCharacterSet != that.m_iAlternateCharacterSet)
            {
                return false;
            }
            if (this.m_bStandardCharacterSetSelected != that.m_bStandardCharacterSetSelected)
            {
                return false;
            }
            if (this.m_fCharacterSlantAngle != that.m_fCharacterSlantAngle)
            {
                return false;
            }
            if (this.m_cLabelTerminator != that.m_cLabelTerminator)
            {
                return false;
            }

            return true;
        }

        public void LoadValuesConfigFromString (string strConfigValues)
        {
            string[] straValues = CGenericMethods.ParseDelimitedString (strConfigValues);

            m_iSortGroup                    = CGenericMethods.SafeConvertToInt (straValues[0]);
            m_iPenUpTravelDistance          = CGenericMethods.SafeConvertToInt (straValues[1]);
            m_bPlotAbsolute                 = straValues[2] == "T";
            m_iLineType                     = CGenericMethods.SafeConvertToInt (straValues[3]);
            m_fLinePatternLength            = CGenericMethods.SafeConvertToFloat (straValues[4]);
            m_fPenVelocity                  = CGenericMethods.SafeConvertToFloat (straValues[5]);
            m_fPenThickness                 = CGenericMethods.SafeConvertToFloat (straValues[6]);
            m_cSymbolModeCharacter          = straValues[7][0];
            //m_fTickLenghtPositiveX          = CGenericMethods.SafeConvertToFloat (straValues[8]);
            //m_fTickLenghtNegativeX          = CGenericMethods.SafeConvertToFloat (straValues[9]);
            //m_fTickLenghtPositiveY          = CGenericMethods.SafeConvertToFloat (straValues[10]);
            //m_fTickLenghtNegativeY          = CGenericMethods.SafeConvertToFloat (straValues[11]);
            m_iFillType                     = CGenericMethods.SafeConvertToInt (straValues[8]);
            m_fFillTypeSpacing              = CGenericMethods.SafeConvertToFloat (straValues[9]);
            m_iFillTypeAngle                = CGenericMethods.SafeConvertToInt (straValues[10]);
            m_fAbsoluteCharDirectionRun     = CGenericMethods.SafeConvertToFloat (straValues[11]);
            m_fAbsoluteCharDirectionRise    = CGenericMethods.SafeConvertToFloat (straValues[12]);
            m_fAbsoluteCharacterSizeWidth   = CGenericMethods.SafeConvertToFloat (straValues[13]);
            m_fAbsoluteCharacterSizeHeight  = CGenericMethods.SafeConvertToFloat (straValues[14]);
            m_fRelativeCharDirectionRun     = CGenericMethods.SafeConvertToFloat (straValues[15]);
            m_fRelativeCharDirectionRise    = CGenericMethods.SafeConvertToFloat (straValues[16]);
            m_fRelativeCharacterSizeWidth   = CGenericMethods.SafeConvertToFloat (straValues[17]);
            m_fRelativeCharacterSizeHeight  = CGenericMethods.SafeConvertToFloat (straValues[18]);
            m_iStandardCharacterSet         = CGenericMethods.SafeConvertToInt (straValues[19]);
            m_iAlternateCharacterSet        = CGenericMethods.SafeConvertToInt (straValues[20]);
            m_bStandardCharacterSetSelected = straValues[21] == "T";
            m_fCharacterSlantAngle          = CGenericMethods.SafeConvertToFloat (straValues[22]);
            m_cLabelTerminator              = (char)CGenericMethods.SafeConvertToInt (straValues[23]);
        }

        // DI The Absolute Direction Instruction
        // SI The Absolute Character Size Instruction

        public string SaveConfigValuesToString ()
        {
            string strConfigValues = string.Format ("{0},{1},{2},{3},{4:#####0.####},{5:#####0.####},{6:#####0.####},"            +
                                                    "{7},{8:#####0.####},{9:#####0.####},{10},{11:#####0.####},{12:#####0.####}," +
                                                    "{13:#####0.####},{14:#####0.####},{15:#####0.####},{16:#####0.####},"      +
                                                    "{17:#####0.####},{18:#####0.####},{19},{20},{21},{22:#####0.####},{23}",
                                                    m_iSortGroup,                                //  0
                                                    m_iPenUpTravelDistance,                      //  1
                                                    m_bPlotAbsolute ? 'T' : 'F',                 //  2   b
                                                    m_iLineType,                                 //  3
                                                    m_fLinePatternLength,                        //  4 f
                                                    m_fPenVelocity,                              //  5 f
                                                    m_fPenThickness,                             //  6 f
                                                    m_cSymbolModeCharacter,                      //  7     c
                                                    //m_fTickLenghtPositiveX,                      //  8 f
                                                    //m_fTickLenghtNegativeX,                      //  9 f
                                                    //m_fTickLenghtPositiveY,                      // 10 f
                                                    //m_fTickLenghtNegativeY,                      // 11 f
                                                    m_iFillType,                                 //  8 f
                                                    m_fFillTypeSpacing,                          //  9 f
                                                    m_iFillTypeAngle,                            // 10
                                                    m_fAbsoluteCharDirectionRun,                 // 11
                                                    m_fAbsoluteCharDirectionRise,                // 12
                                                    m_fAbsoluteCharacterSizeWidth,               // 13 f
                                                    m_fAbsoluteCharacterSizeHeight,              // 14 f
                                                    m_fRelativeCharDirectionRun,                 // 15
                                                    m_fRelativeCharDirectionRise,                // 16
                                                    m_fRelativeCharacterSizeWidth,               // 17 f
                                                    m_fRelativeCharacterSizeHeight,              // 18 f
                                                    m_iStandardCharacterSet,                     // 19
                                                    m_iAlternateCharacterSet,                    // 20
                                                    m_bStandardCharacterSetSelected ? 'T' : 'F', // 21   b
                                                    m_fCharacterSlantAngle,                      // 22 f
                                                    (int)m_cLabelTerminator);                    // 23     c

            return strConfigValues;
        }

        protected string GetConfigHPGL ()
        {
            StringBuilder sbConfigInstructionString = new StringBuilder ("DF;"); // Begin with default state

            // Line settings:
            if (m_bPlotAbsolute == DEFAULT_PLOT_ABSOLUTE)                 // true      PA;     Chap 3  (Absolute (PA))
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.PlotAbsolute ());
            }
            else
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.PlotRelative ());
            }

            if (m_iLineType          != DEFAULT_LINE_TYPE ||                // -1        LT;     Chap 4  (0 - Solid line)
                m_fLinePatternLength != m_fDefaultLinePatternLength)        // 0.0F      LT;     Chap 4  (4% of the diagonal distance between P1 to P2)
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.DesignateLine (m_iLineType, m_fLinePatternLength));
            }

            if (m_fPenVelocity != DEFAULT_PEN_VELOCITY)                     // 0.0F      VS;     Chap 3  (38.1 cm/s (15 in./s))
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.VelocitySelect (m_fPenVelocity));
            }

            if (m_fPenThickness != DEFAULT_PEN_THICKNESS)                   // 0.0F      PT;     Chap 3  (Set to 0.3 mm)
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.PenThickness (m_fPenThickness));
            }

            if (m_cSymbolModeCharacter != DEFAULT_SYMBOL_MODE_CHARACTER)    // ' '       SM;     Chap 3  (Off)
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.SymbolMode (m_cSymbolModeCharacter));
            }

            if (m_iFillType        != DEFAULT_FILL_TYPE         ||              // 1         FT;     Chap 3  (Set to type 1, bidirectional solid fill)
                m_fFillTypeSpacing != m_fDefaultFillTypeSpacing ||              // 0.0F      FT;     Chap 3  (1% of the diagonal distance between P1 and P2)
                m_iFillTypeAngle   != DEFAULT_FILL_TYPE_ANGLE)                  // 0         FT;     Chap 3  (Set to 0 degrees)
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.FillType (m_iFillType, m_fFillTypeSpacing, m_iFillTypeAngle));
            }

            // No point in setting these here since choice of X or Y depends on whether vertical (YT) or horizontal (XT) ticks are drawn
            //if (m_fTickLenghtPositiveX != DEFAULT_TICK_LENGHT_POSITIVE_X || // 0.0F      TL;     Chap 4  (tp = tn = 0.5% of (P2 x - P1 x) for Y-tick and
            //    m_fTickLenghtNegativeX != DEFAULT_TICK_LENGHT_NEGATIVE_X)   // 0.0F                                 0.5% of (P2 y - P1 y) for X-tick)
            //{
            //    sbConfigInstructionString.Append (/*";" +*/ CHPGL.TickLength (,);
            //}

            //if (m_fTickLenghtPositiveY != DEFAULT_TICK_LENGHT_POSITIVE_Y || // 0.0F      TL;     Chap 4  (tp = tn = 0.5% of (P2 x - P1 x) for Y-tick and
            //    m_fTickLenghtNegativeY != DEFAULT_TICK_LENGHT_NEGATIVE_Y)   // 0.0F                                 0.5% of (P2 y - P1 y) for X-tick)
            //{
            //    sbConfigInstructionString.Append (/*";" +*/ CHPGL);
            //}

            // Character settings:
            if (m_fAbsoluteCharDirectionRun  != DEFAULT_ABSOLUTE_CHAR_DIRECTION_RUN ||      // 1         DI The Absolute Direction Instruction
                m_fAbsoluteCharDirectionRise != DEFAULT_ABSOLUTE_CHAR_DIRECTION_RISE)       // 0
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.AbsoluteDirection (m_fAbsoluteCharDirectionRun, m_fAbsoluteCharDirectionRise));
            }

            if (m_fAbsoluteCharacterSizeWidth  != m_fDefaultAbsCharSizeWidth ||             // 0.0F      SI The Absolute Character Size Instruction
                m_fAbsoluteCharacterSizeHeight != m_fDefaultAbsCharSizeHeight)              // 0.0F      
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.AbsoluteCharacterSize (m_fAbsoluteCharacterSizeWidth, m_fAbsoluteCharacterSizeHeight));
            }

            if (m_fRelativeCharDirectionRun  != DEFAULT_RELATIVE_CHAR_DIRECTION_RUN ||      // 1         DR1,O;  Chap 5  (Horizontal (DR1,O))
                m_fRelativeCharDirectionRise != DEFAULT_RELATIVE_CHAR_DIRECTION_RISE)       // 0
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.RelativeDirection (m_fRelativeCharDirectionRun, m_fRelativeCharDirectionRise));
            }

            if (m_fRelativeCharacterSizeWidth  != m_fDefaultRelCharSizeWidth ||             // 0.0F      SR;     Chap 5  Width = 0.75% of (P2 x - P1 x)
                m_fRelativeCharacterSizeHeight != m_fDefaultRelCharSizeHeight)              // 0.0F                      Height = 1.5% of (P2 y - P1 y)
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.RelativeCharacterSize (m_fRelativeCharacterSizeWidth, m_fRelativeCharacterSizeHeight));
            }

            if (m_iStandardCharacterSet != DEFAULT_STANDARD_CHARACTER_SET)                  // 0         CSO;    Chap 5  (Set 0)
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.DesignateStandardCharacterSet (m_iStandardCharacterSet));
            }

            if (m_iAlternateCharacterSet != DEFAULT_ALTERNATE_CHARACTER_SET)                // 0         CAO;    Chap 5  (Set 0)
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.DesignateAlternateCharacterSet (m_iAlternateCharacterSet));
            }

            if (m_bStandardCharacterSetSelected == DEFAULT_STANDARD_CHARACTER_SET_SELECTED) // true      SS;     Chap 5  (Standard)
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.SelectStandardCharacterSet ());
            }
            else
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.SelectAlternateCharacterSet ());
            }

            if (m_fCharacterSlantAngle != DEFAULT_CHARACTER_SLANT_ANGLE)                    // 0.0F      SLO;    Chap 5  (0 degrees)
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.AbsoluteCharacterSlant (m_fCharacterSlantAngle));
            }

            if (m_cLabelTerminator != DEFAULT_LABEL_TERMINATOR)                             // CHPGL.ETX DT<ETX> Chap 5  (ETX (ASCII decimal equivalent 3))
            {
                sbConfigInstructionString.Append (/*";" +*/ CHPGL.DefineLabelTerminator (m_cLabelTerminator));
            }

            return sbConfigInstructionString.ToString ();
        }
    }

    // How to suppress matching status instructions in following shape objects?
    // How to handle series of characters with different colors for each letter?
    // How to determine end points for arcs and other shapes with end points deteremined by the plotter hardware?
    public abstract class CDrawingShapeElement : CShapeConfigSettings
    {
        protected bool       m_bSortedForTravelDistance = false;
        protected EPenSelect m_ePenSelect               = EPenSelect.ESelectNoPen;
        protected Point      m_ptStartPoint             = new Point (-1, -1);
        protected Point      m_ptEndPoint               = new Point (-1, -1);

        public CDrawingShapeElement () { }

        public void SetPenSelection (EPenSelect ePenSelect)
        {
            m_ePenSelect = ePenSelect;
        }

        public EPenSelect GetPenSelection ()
        {
            return m_ePenSelect;
        }

        public Point GetStartPoint ()
        {
            return m_ptStartPoint;
        }

        public Point GetEndPoint ()
        {
            return m_ptEndPoint;
        }

        public abstract void ComputeStartAndEndPoints ();

        public abstract void RotatePoints (int iDegrees, Point pAxisPoint);

        public abstract void ResizeOrMove (Point ptLowerLeft, Point ptUpperRight);

        public abstract bool IsEqualTo (CDrawingShapeElement that);

        public abstract void LoadValuesFromString (string strShapeValues);

        public abstract string SaveValuesToString ();

        public abstract string GetHPGL (bool bIncludeConfigValues = true, EPlotMode ePlotMode = EPlotMode.ePlotDefault);
    }

    public class CComplexLinesShape : CDrawingShapeElement // CL
    {
        private List<Point[]> m_lpaPlotPointSequences = new List<Point[]> ();

        // Sort keys:
        //int   m_iSortGroup                    = 0;
        //int   m_iPenUpTravelDistance          = -1;

        // Line settings:
        //bool  m_bPlotAbsolute                 = true;
        //int   m_iLineType                     = 0;
        //float m_fLinePatternLength            = 0.0F;
        //float m_fPenVelocity                  = 0.0F;
        //float m_fPenThickness                 = 0.0F;
        //char  m_cSymbolModeCharacter          = ' ';
        //float m_fTickLenghtPositive           = 0.0F;
        //float m_fTickLenghtNegative           = 0.0F;
        //int   m_fFillType                     = 1;
        //float m_fFillTypeSpacing              = 0.0F;
        //int   m_iFillTypeAngle                = 0;

        // Character settings:
        //int   m_iRelativeCharDirectionRun     = 0;
        //int   m_iRelativeCharDirectionRise    = 0;
        //float m_fRelativeCharacterSizeWidth   = 0.0F;
        //float m_fRelativeCharacterSizeHeight  = 0.0F;
        //int   m_iStandardCharacterSet         = 0;
        //int   m_iAlternateCharacterSet        = 0;
        //bool  m_bStandardCharacterSetSelected = true;
        //float m_fCharacterSlantAngle          = 0.0F;
        //char  m_cLabelTerminator              = ' ';
        public CComplexLinesShape () { }

        //public CComplexLinesShape (Point[] ptaPointSequence, EPenSelect ePenSelect = EPenSelect.ESelectPen1, bool bSorted = false)
        //{
        //    m_ePenSelect = ePenSelect;
        //    m_bSortedForTravelDistance = bSorted;
        //    m_lpaPlotPointSequences.Add (ptaPointSequence);
        //}

        //public CComplexLinesShape (List<Point[]> lptaPointSequences, EPenSelect ePenSelect = EPenSelect.ESelectPen1, bool bSorted = false)
        //{
        //    m_ePenSelect = ePenSelect;
        //    m_bSortedForTravelDistance = bSorted;
        //    m_lpaPlotPointSequences = lptaPointSequences;
        //}

        public CComplexLinesShape (Point[] ptaPointSequences, EPenSelect ePenSelect, bool bSorted = false, int iSortGroup = 0,
                                   bool bPlotAbsolute = true, int iLineType = DEFAULT_LINE_TYPE, float fLinePatternLength = -1.0F, float fPenVelocity = 0.0F,
                                   float fPenThickness = 0.0F, char cSymbolModeCharacter = ' '//, float fTickLenghtPositiveX = 0.0F,
                                   //float fTickLenghtNegativeX = 0.0F, float fTickLenghtPositiveY = 0.0F, float fTickLenghtNegativeY = 0.0F,
                                   /*int fFillType = 1, float fFillTypeSpacing = -1.0F, int iFillTypeAngle = 0*/)
        {
            m_bSortedForTravelDistance = bSorted;
            m_iSortGroup               = iSortGroup;
            m_ePenSelect               = ePenSelect;

            m_bPlotAbsolute            = bPlotAbsolute;
            if (iLineType > -1)
            {
                m_iLineType = iLineType;
            }
            if (fLinePatternLength > -1.0F)
            {
                m_fLinePatternLength   = fLinePatternLength;
            }
            m_fPenVelocity             = fPenVelocity;
            m_fPenThickness            = fPenThickness;
            m_cSymbolModeCharacter     = cSymbolModeCharacter;
            //m_fTickLenghtPositiveX     = fTickLenghtPositiveX;
            //m_fTickLenghtNegativeX     = fTickLenghtNegativeX;
            //m_fTickLenghtPositiveY     = fTickLenghtPositiveY;
            //m_fTickLenghtNegativeY     = fTickLenghtNegativeY;
            //m_iFillType                = fFillType;
            //if (fFillTypeSpacing > -1.0F)
            //{
            //    m_fFillTypeSpacing     = fFillTypeSpacing;
            //}
            //m_iFillTypeAngle           = iFillTypeAngle;
            m_lpaPlotPointSequences.Add (ptaPointSequences);
        }

        public CComplexLinesShape (List<Point[]> lptaPointSequences, EPenSelect ePenSelect, bool bSorted = false, int iSortGroup = 0,
                                   bool bPlotAbsolute = true, int iLineType = DEFAULT_LINE_TYPE, float fLinePatternLength = -1.0F, float fPenVelocity = 0.0F,
                                   float fPenThickness = 0.0F, char cSymbolModeCharacter = ' '//, float fTickLenghtPositiveX = 0.0F,
                                   //float fTickLenghtNegativeX = 0.0F, float fTickLenghtPositiveY = 0.0F, float fTickLenghtNegativeY = 0.0F,
                                   /*int fFillType = 1, float fFillTypeSpacing = -1.0F, int iFillTypeAngle = 0*/)
        {
            m_lpaPlotPointSequences    = lptaPointSequences;
            m_bSortedForTravelDistance = bSorted;
            m_ePenSelect               = ePenSelect;
            m_iSortGroup               = iSortGroup;

            m_bPlotAbsolute            = bPlotAbsolute;
            if (iLineType > -1)
            {
                m_iLineType = iLineType;
            }
            if (fLinePatternLength > -1.0F)
            {
                m_fLinePatternLength   = fLinePatternLength;
            }
            //m_iLineType                = iLineType;
            //m_fLinePatternLength       = fLinePatternLength;
            m_fPenVelocity             = fPenVelocity;
            m_fPenThickness            = fPenThickness;
            m_cSymbolModeCharacter     = cSymbolModeCharacter;
            //m_fTickLenghtPositiveX     = fTickLenghtPositiveX;
            //m_fTickLenghtNegativeX     = fTickLenghtNegativeX;
            //m_fTickLenghtPositiveY     = fTickLenghtPositiveY;
            //m_fTickLenghtNegativeY     = fTickLenghtNegativeY;
            //m_iFillType                = fFillType;
            //if (fFillTypeSpacing > -1.0F)
            //{
            //    m_fFillTypeSpacing     = fFillTypeSpacing;
            //}
            //m_iFillTypeAngle           = iFillTypeAngle;

            //foreach (Point[] pta in lptaPointSequences)
            //{
            //    m_lpaPlotPointSequences.Add (pta);
            //}
        }

        public void SetPen (EPenSelect ePenSelect) { m_ePenSelect = ePenSelect; }
        public void AddPoints (Point[] ptaPointSequence) { m_lpaPlotPointSequences.Add (ptaPointSequence); }

        public void ReversePointSequence ()
        {
            Point pTemp = new Point ();

            foreach (Point[] pa in m_lpaPlotPointSequences)
            {
                for (int iIdxFront = 0, iIdxBack = pa.Length - 1; iIdxFront < iIdxBack; iIdxFront++, iIdxBack--)
                {
                    pTemp = pa[iIdxFront];
                    pa[iIdxFront] = pa[iIdxBack];
                    pa[iIdxBack] = pTemp;
                }
            }
        }

        //public void SortToOptimizePenUpTravelDistance ()
        //{
        //    if (!m_bSortedForTravelDistance)
        //    {
        //        m_bSortedForTravelDistance = true;
        //    }
        //}

        public override void ComputeStartAndEndPoints ()
        {
            if (m_lpaPlotPointSequences.Count > 0)
            {
                try
                {
                    m_ptStartPoint = m_lpaPlotPointSequences[0][0];
                    Point[] paLast = m_lpaPlotPointSequences[m_lpaPlotPointSequences.Count - 1];
                    m_ptEndPoint = paLast[paLast.Length - 1];
                }
                catch (Exception)
                {
                    // Just quit
                    // This only seems to happen when on of the Point[] collections in m_lpaPlotPointSequences is empty
                }
            }
        }

        //public override Point GetStartPoint ()
        //{
        //    throw new NotImplementedException ("CComplexLinesShape.GetStartPoint not implemented");
        //}

        //public override Point GetEndPoint ()
        //{
        //    throw new NotImplementedException ("CComplexLinesShape.GetEndPoint not implemented");
        //}

        public override void RotatePoints (int iDegrees, Point pAxisPoint)
        {
            if (Math.Abs (iDegrees) > 359)
            {
                throw new Exception ("CComplexLinesShape.RotatePoints iDegress greater than 359 degrees");
            }

            if (pAxisPoint.X < 0 ||
                pAxisPoint.X > CHPGL.MAX_X_VALUE ||
                pAxisPoint.Y < 0 ||
                pAxisPoint.Y > CHPGL.MAX_Y_VALUE)
            {
                throw new Exception ("CComplexLinesShape.RotatePoints pAxixPoint out of range");
            }
        }

        public override void ResizeOrMove (Point ptLowerLeft, Point ptUpperRight)
        {
            throw new NotImplementedException ("CComplexLinesShape.ResizeOrMove not implemented");
        }

        public override bool IsEqualTo (CDrawingShapeElement that)
        {
            bool b1 = base.IsConfigEqualTo (that);
            bool b2 = this.m_lpaPlotPointSequences.Count == ((CComplexLinesShape)that).m_lpaPlotPointSequences.Count;

            if (b1 && b2)
            {
                for (int iIdxPa = 0; iIdxPa < this.m_lpaPlotPointSequences.Count; ++iIdxPa)
                {
                    Point[] paThis = this.m_lpaPlotPointSequences[iIdxPa];
                    Point[] paThat = ((CComplexLinesShape)that).m_lpaPlotPointSequences[iIdxPa];

                    for (int iIdxPt = 0; iIdxPt < paThis.Length; ++iIdxPt)
                    {
                        if (paThis[iIdxPt] != paThat[iIdxPt])
                        {
                            return false;
                        }
                    }
                }
            }

            return b1 && b2;
        }

        public override void LoadValuesFromString (string strShapeValues)
        {
            if (strShapeValues.Substring (0, 2) == "CL")
            {
                string strShapeString = strShapeValues.Substring (2);
                int iTildeIdx = strShapeString.IndexOf ('~');
                if (iTildeIdx >= 0)
                {
                    m_lpaPlotPointSequences.Clear ();
                    base.LoadValuesConfigFromString (strShapeString.Substring (0, iTildeIdx));
                    string strComplexLinesValues  = strShapeString.Substring (iTildeIdx + 1);
                    string[] straPointArrayValues = CGenericMethods.ParseDelimitedString (strComplexLinesValues, ';');
                    foreach (string strPointArray in straPointArrayValues)
                    {
                        string[] straPointValues = CGenericMethods.ParseDelimitedString (strPointArray);
                        List<Point> lptValues = new List<Point> ();
                        Point ptNew = new Point (-1, -1);

                        foreach (string str in straPointValues)
                        {
                            int iPointValue = CGenericMethods.SafeConvertToInt (str);
                            if (ptNew.X == -1)
                            {
                                ptNew.X = iPointValue;
                            }
                            else
                            {
                                ptNew.Y = iPointValue;
                                lptValues.Add (ptNew);
                                ptNew.X = ptNew.Y = -1;
                            }
                        }

                        m_lpaPlotPointSequences.Add (lptValues.ToArray ());
                    }
                }
            }
        }

        public override string SaveValuesToString ()
        {
            StringBuilder sbShapeValues = new StringBuilder ("CL");
            sbShapeValues.Append (base.SaveConfigValuesToString () + '~');
            bool bFirstArray = true;

            foreach (Point[] pa in m_lpaPlotPointSequences)
            {
                if (!bFirstArray)
                {
                    sbShapeValues.Append (';');
                }
                bFirstArray = false;

                bool bFirstPoint = true;
                foreach (Point pt in pa)
                {
                    if (!bFirstPoint)
                    {
                        sbShapeValues.Append (',');
                    }
                    bFirstPoint = false;
                    sbShapeValues.Append (pt.X.ToString ());
                    sbShapeValues.Append (',');
                    sbShapeValues.Append (pt.Y.ToString ());
                }
            }

            return sbShapeValues.ToString ();
        }

        public override string GetHPGL (bool bIncludeConfigValues = true, EPlotMode ePlotMode = EPlotMode.ePlotDefault)
        {
            bool bPlotAbsolute = ePlotMode == EPlotMode.ePlotDefault ? m_bPlotAbsolute :
                                 ePlotMode == EPlotMode.ePlotAbsolute;
            StringBuilder sbHPGL = new StringBuilder (bIncludeConfigValues ? base.GetConfigHPGL () : "");

            sbHPGL.Append (CHPGL.PenUp ());

            foreach (Point[] pa in m_lpaPlotPointSequences)
            {
                sbHPGL.Append (bPlotAbsolute ? CHPGL.PlotAbsolute (pa) : CHPGL.PlotRelative (pa));
            }

            return sbHPGL.ToString ();
        }
    }

    // TODO: Require all values in constructor for relevant config settings (including direction) and HPGL command argument and bool bPlotAbsolute
    public class CCircleShape : CDrawingShapeElement // CS
    {
        // CI radius [i/sd] (,chord angle [i])                                     Circle

        float m_fRadius     = 0.0F;
        int   m_iChordAngle = 0;

        public CCircleShape (int iStartX, int iStartY, float fRadius, int iChordAngle, EPenSelect ePenSelect)
        {
            m_ptStartPoint.X = iStartX;
            m_ptStartPoint.Y = iStartY;
            m_fRadius        = fRadius;
            m_iChordAngle    = iChordAngle;
            m_ePenSelect     = ePenSelect;
        }

        public CCircleShape (Point ptStartPoint, float fRadius, int iChordAngle, EPenSelect ePenSelect)
        {
            m_ptStartPoint = ptStartPoint;
            m_fRadius      = fRadius;
            m_iChordAngle  = iChordAngle;
            m_ePenSelect   = ePenSelect;
        }

        public void SetRadius     (float fRadius)   { m_fRadius     = fRadius; }
        public void SetChordAngle (int iChordAngle) { m_iChordAngle = iChordAngle; }

        public float GetRadius ()     { return m_fRadius; }
        public int   GetChordAngle () { return m_iChordAngle; }

        public void ResetRadius     () { m_fRadius = 0.0F; }
        public void ResetChordAngle () { m_iChordAngle = 0; }
        
        public override void ComputeStartAndEndPoints ()
        {
            m_ptEndPoint = m_ptStartPoint;
        }

        //public override Point GetStartPoint ()
        //{
        //    throw new NotImplementedException ("CCircleShape.GetStartPoint not implemented");
        //}

        //public override Point GetEndPoint ()
        //{
        //    throw new NotImplementedException ("CCircleShape.GetEndPoint not implemented");
        //}

        public override void RotatePoints (int iDegrees, Point pAxisPoint)
        {
            throw new NotImplementedException ("CCircleShape.RotatePoints not implemented");
        }

        public override void ResizeOrMove (Point ptLowerLeft, Point ptUpperRight)
        {
            throw new NotImplementedException ("CCircleShape.ResizeOrMove not implemented");
        }

        public override bool IsEqualTo (CDrawingShapeElement that)
        {
            return base.IsConfigEqualTo (that)                          &&
                   this.m_fRadius     == ((CCircleShape)that).m_fRadius &&
                   this.m_iChordAngle == ((CCircleShape)that).m_iChordAngle;
        }

        public override void LoadValuesFromString (string strShapeValues)
        {
            if (strShapeValues.Substring (0, 2) == "CS")
            {
                string strShapeString = strShapeValues.Substring (2);
                int iTildeIdx = strShapeString.IndexOf ('~');
                if (iTildeIdx >= 0)
                {
                    base.LoadValuesConfigFromString (strShapeString.Substring (0, iTildeIdx));
                    string strCircleShapeValues    = strShapeString.Substring (iTildeIdx + 1);
                    string[] straCircleShapeValues = CGenericMethods.ParseDelimitedString (strCircleShapeValues);
                    m_ptStartPoint.X               = CGenericMethods.SafeConvertToInt     (straCircleShapeValues[0]);
                    m_ptStartPoint.Y               = CGenericMethods.SafeConvertToInt     (straCircleShapeValues[1]);
                    m_fRadius                      = CGenericMethods.SafeConvertToFloat   (straCircleShapeValues[2]);
                    m_iChordAngle                  = CGenericMethods.SafeConvertToInt     (straCircleShapeValues[3]);
                }
            }
        }

        public override string SaveValuesToString ()
        {
            string strShapeValues = string.Format ("CS{0}~{1},{2},{3:#####0.####},{4}", base.SaveConfigValuesToString (),
                                                   m_ptStartPoint.X, m_ptStartPoint.Y, m_fRadius, m_iChordAngle);

            return strShapeValues;
        }

        public override string GetHPGL (bool bIncludeConfigValues = true, EPlotMode ePlotMode = EPlotMode.ePlotDefault)
        {
            StringBuilder sbHPGL = new StringBuilder (bIncludeConfigValues ? base.GetConfigHPGL () : "");
            sbHPGL.Append (CHPGL.PenUp () + CHPGL.PlotAbsolute (m_ptStartPoint.X, m_ptStartPoint.Y) + CHPGL.Circle (m_fRadius, m_iChordAngle)  + CHPGL.PenUp ());

            return sbHPGL.ToString ();
        }
    }

    public class CArcShape : CDrawingShapeElement // AS
    {
        // AA X [i/sd], Y [i/sd], arc angle [i] (,chord angle [i])                 Arc absolute
        // AR X [i/sd], Y [i/sd], arc angle [i] (,chord angle [i])                 Arc relative

        int   m_iArcAngle   = 0;
        int   m_iChordAngle = 0;
        Point m_ptAxis      = new Point ();

        public CArcShape (int iStartX, int iStartY, int iAxisX, int iAxisY, int iArcAngle, int iChordAngle, EPenSelect ePenSelect)
        {
            m_ptStartPoint.X = iStartX;
            m_ptStartPoint.Y = iStartY;
            m_ptAxis.X       = iAxisX;
            m_ptAxis.Y       = iAxisY;
            m_iArcAngle      = iArcAngle;
            m_iChordAngle    = iChordAngle;
            m_ePenSelect     = ePenSelect;
        }

        public CArcShape (Point ptStartPoint, Point ptAxis, int iArcAngle, int iChordAngle, EPenSelect ePenSelect)
        {
            m_ptStartPoint = ptStartPoint;
            m_ptAxis       = ptAxis;
            m_iArcAngle    = iArcAngle;
            m_iChordAngle  = iChordAngle;
            m_ePenSelect   = ePenSelect;
        }

        public void SetArcAngle   (int iArcAngle)   { m_iArcAngle   = iArcAngle; }
        public void SetChordAngle (int iChordAngle) { m_iChordAngle = iChordAngle; }
        public void SetAxisPoint  (Point ptAxis)    { m_ptAxis       = ptAxis; }

        public int   GetArcAngle ()   { return m_iArcAngle; }
        public int   GetChordAngle () { return m_iChordAngle; }
        public Point GetAxisPoint ()  { return m_ptAxis; }

        public void ResetArcAngle ()   { m_iArcAngle = 0; }
        public void ResetChordAngle () { m_iChordAngle = 0; }
        public void ResetAxisPoint ()
        {
            m_ptAxis.X = -1;
            m_ptAxis.Y = -1;
        }

        public override void ComputeStartAndEndPoints ()
        {
            m_ptEndPoint = CPlotterMath.RotatePoint (m_ptAxis, m_iArcAngle, m_ptStartPoint);
        }

        //public override Point GetStartPoint ()
        //{
        //    throw new NotImplementedException ("CArcShape.GetStartPoint not implemented");
        //}

        //public override Point GetEndPoint ()
        //{
        //    throw new NotImplementedException ("CArcShape.GetEndPoint not implemented");
        //}

        public override void RotatePoints (int iDegrees, Point pAxisPoint)
        {
            throw new NotImplementedException ("CArcShape.RotatePoints not implemented");
        }

        public override void ResizeOrMove (Point ptLowerLeft, Point ptUpperRight)
        {
            throw new NotImplementedException ("CArcShape.ResizeOrMove not implemented");
        }

        public override bool IsEqualTo (CDrawingShapeElement that)
        {
            return base.IsConfigEqualTo (that)                         &&
                   this.m_iArcAngle   == ((CArcShape)that).m_iArcAngle &&
                   this.m_iChordAngle == ((CArcShape)that).m_iChordAngle;
        }

        public override void LoadValuesFromString (string strShapeValues)
        {
            if (strShapeValues.Substring (0, 2) == "AS")
            {
                string strShapeString = strShapeValues.Substring (2);
                int iTildeIdx = strShapeString.IndexOf ('~');
                if (iTildeIdx >= 0)
                {
                    base.LoadValuesConfigFromString (strShapeString.Substring (0, iTildeIdx));
                    string strArcShapeValues    = strShapeString.Substring (iTildeIdx + 1);
                    string[] straArcShapeValues = CGenericMethods.ParseDelimitedString (strArcShapeValues);
                    m_iArcAngle                 = CGenericMethods.SafeConvertToInt     (straArcShapeValues[0]);
                    m_iChordAngle               = CGenericMethods.SafeConvertToInt     (straArcShapeValues[1]);
                    m_ptAxis.X                  = CGenericMethods.SafeConvertToInt     (straArcShapeValues[2]);
                    m_ptAxis.Y                  = CGenericMethods.SafeConvertToInt     (straArcShapeValues[3]);
                    m_ptStartPoint.X            = CGenericMethods.SafeConvertToInt     (straArcShapeValues[4]);
                    m_ptStartPoint.Y            = CGenericMethods.SafeConvertToInt     (straArcShapeValues[5]);
                }
            }
        }

        public override string SaveValuesToString ()
        {
            string strShapeValues = string.Format ("AS{0}~{1},{2},{3},{4},{5},{6}", base.SaveConfigValuesToString (), m_iArcAngle, m_iChordAngle,
                                                                                    m_ptAxis.X, m_ptAxis.Y, m_ptStartPoint.X, m_ptStartPoint.Y);

            return strShapeValues;
        }

        public override string GetHPGL (bool bIncludeConfigValues = true, EPlotMode ePlotMode = EPlotMode.ePlotDefault)
        {
            StringBuilder sbHPGL = new StringBuilder (bIncludeConfigValues ? base.GetConfigHPGL () : "");
            if ( ePlotMode == EPlotMode.ePlotAbsolute ||
                (ePlotMode == EPlotMode.ePlotDefault  && m_bPlotAbsolute))
            {
                sbHPGL.Append (CHPGL.PlotAbsolute (m_ptStartPoint.X, m_ptStartPoint.Y) +
                               CHPGL.PenDown ()                                        +
                               CHPGL.ArcAbsolute (m_ptAxis.X, m_ptAxis.Y, m_iArcAngle, m_iChordAngle));
            }
            else
            {
                sbHPGL.Append (CHPGL.PlotRelative (m_ptStartPoint.X, m_ptStartPoint.Y) +
                               CHPGL.PenDown ()                                        +
                               CHPGL.ArcRelative (m_ptAxis.X, m_ptAxis.Y, m_iArcAngle, m_iChordAngle));
            }

            return sbHPGL.ToString ();
        }
    }

    public class CRectangleShape : CDrawingShapeElement // RS
    {
        // EA X (i/sd], Y [i/sd]                                                   Edge rectangle absolute
        // ER X [i/sd], Y [i/sd]                                                   Edge rectangle relative
        // RA X [i/sd], Y [i/sd]                                                   Shade rectangle absolute
        // RR X [i/sd], Y [i/sd]                                                   Shade rectangle relative

        float m_fEndPointX       = -1.0F;
        float m_fEndPointY       = -1.0F;
        float m_fOppositeCornerX = -1.0F;
        float m_fOppositeCornerY = -1.0F;
        bool  m_bEdgeRectangle   = true;

        public CRectangleShape (int iStartX, int iStartY, float fOppositeCornerX, float fOppositeCornerY, EPenSelect ePenSelect, bool bEdgeRectangle = true)
        {
            m_ptEndPoint.X     = m_ptStartPoint.X = iStartX;
            m_ptEndPoint.Y     = m_ptStartPoint.Y = iStartY;
            m_ptEndPoint.X     = m_ptStartPoint.X = iStartX;
            m_ptEndPoint.Y     = m_ptStartPoint.Y = iStartY;
            m_fOppositeCornerX = fOppositeCornerX;
            m_fOppositeCornerY = fOppositeCornerY;
            m_bEdgeRectangle   = bEdgeRectangle;
            m_ePenSelect       = ePenSelect;
        }

        public CRectangleShape (Point ptStart, float fOppositeCornerX, float fOppositeCornerY, EPenSelect ePenSelect, bool bEdgeRectangle = true)
        {
            m_ptEndPoint       = m_ptStartPoint   = ptStart;
            m_fOppositeCornerX = fOppositeCornerX;
            m_fOppositeCornerY = fOppositeCornerY;
            m_bEdgeRectangle   = bEdgeRectangle;
            m_ePenSelect       = ePenSelect;
        }

        public void SetEndPointX (float fEndPointX) { m_fEndPointX = fEndPointX; }
        public void SetEndPointY (float fEndPointY) { m_fEndPointY = fEndPointY; }

        public float GetEndPointX () { return m_fEndPointX; }
        public float GetEndPointY () { return m_fEndPointY; }

        public void ResetEndPointX () { m_fEndPointX = -1.0F; }
        public void ResetEndPointY () { m_fEndPointY = -1.0F; }

        public override void ComputeStartAndEndPoints ()
        {
        }

        //public override Point GetStartPoint ()
        //{
        //    throw new NotImplementedException ("CRectangleShape.GetStartPoint not implemented");
        //}

        //public override Point GetEndPoint ()
        //{
        //    throw new NotImplementedException ("CRectangleShape.GetEndPoint not implemented");
        //}

        public override void RotatePoints (int iDegrees, Point pAxisPoint)
        {
            throw new NotImplementedException ("CRectangleShape.RotatePoints not implemented");
        }

        public override void ResizeOrMove (Point ptLowerLeft, Point ptUpperRight)
        {
            throw new NotImplementedException ("CRectangleShape.ResizeOrMove not implemented");
        }

        public override bool IsEqualTo (CDrawingShapeElement that)
        {
            return base.IsConfigEqualTo (that)                               &&
                   this.m_fEndPointX == ((CRectangleShape)that).m_fEndPointX &&
                   this.m_fEndPointY == ((CRectangleShape)that).m_fEndPointY;
        }

        public override void LoadValuesFromString (string strShapeValues)
        {
            if (strShapeValues.Substring (0, 2) == "RS")
            {
                string strShapeString = strShapeValues.Substring (2);
                int iTildeIdx = strShapeString.IndexOf ('~');
                if (iTildeIdx >= 0)
                {
                    base.LoadValuesConfigFromString (strShapeString.Substring (0, iTildeIdx));
                    string strRectangleShapeValues    = strShapeString.Substring (iTildeIdx + 1);
                    string[] straRectangleShapeValues = CGenericMethods.ParseDelimitedString (strRectangleShapeValues);
                    m_fEndPointX                      = CGenericMethods.SafeConvertToFloat   (straRectangleShapeValues[0]);
                    m_fEndPointY                      = CGenericMethods.SafeConvertToFloat   (straRectangleShapeValues[1]);
                }
            }
        }

        public override string SaveValuesToString ()
        {
            string strShapeValues = string.Format ("RS{0}~{1:#####0.####},{2:#####0.####}", base.SaveConfigValuesToString (), m_fEndPointX, m_fEndPointY);

            return strShapeValues;
        }

        public override string GetHPGL (bool bIncludeConfigValues = true, EPlotMode ePlotMode = EPlotMode.ePlotDefault)
        {
            StringBuilder sbHPGL = new StringBuilder (bIncludeConfigValues ? base.GetConfigHPGL () : "");
            if ( ePlotMode == EPlotMode.ePlotAbsolute ||
                (ePlotMode == EPlotMode.ePlotDefault  && m_bPlotAbsolute))
            {
                if (m_bEdgeRectangle)
                {
                    sbHPGL.Append (CHPGL.PlotAbsolute (m_ptStartPoint) + CHPGL.EdgeRectangleAbsolute (m_fOppositeCornerX, m_fOppositeCornerY));
                }
                else
                {
                    sbHPGL.Append (CHPGL.PlotAbsolute (m_ptStartPoint) + CHPGL.ShadeRectangleAbsolute (m_fOppositeCornerX, m_fOppositeCornerY));
                }
            }
            else
            {
                if (m_bEdgeRectangle)
                {
                    sbHPGL.Append (CHPGL.PlotAbsolute (m_ptStartPoint) + CHPGL.EdgeRectangleRelative (m_fOppositeCornerX, m_fOppositeCornerY));
                }
                else
                {
                    sbHPGL.Append (CHPGL.PlotAbsolute (m_ptStartPoint) + CHPGL.ShadeRectangleRelative (m_fOppositeCornerX, m_fOppositeCornerY));
                }
            }

            return sbHPGL.ToString ();
        }
    }

    public class CWedgeShape : CDrawingShapeElement // WS
    {
        // EW radius [i/sd], start angle [i], sweep angle [i] (,chord angle [i])   Edge wedge
        // WG radius [i/sd], start angle [i], sweep angle [i] (,chord angle [i])   Shade wedge

        float m_fRadius     = 0.0F;
        int   m_iStartAngle = 0;
        int   m_iSweepAngle = 0;
        int   m_iChordAngle = 0;
        Point m_ptAxis      = new Point ();
        bool  m_bEdgeWedge  = true;

        public CWedgeShape (int iStartX, int iStartY, float fRadius, int fStartAngle, int fSweepAngle, int iChordAngle, EPenSelect ePenSelect, bool bEdgeWedge = true)
        {
            m_ptAxis.X    = iStartX;
            m_ptAxis.Y    = iStartY;
            m_fRadius     = fRadius;
            m_iStartAngle = fStartAngle;
            m_iSweepAngle = fSweepAngle;
            m_iChordAngle = iChordAngle;
            m_ePenSelect  = ePenSelect;
            m_bEdgeWedge  = bEdgeWedge;
        }

        public CWedgeShape (Point ptStartPoint, float fRadius, int fStartAngle, int fSweepAngle, int iChordAngle, EPenSelect ePenSelect, bool bEdgeWedge = true)
        {
            m_ptAxis      = ptStartPoint;
            m_fRadius     = fRadius;
            m_iStartAngle = fStartAngle;
            m_iSweepAngle = fSweepAngle;
            m_iChordAngle = iChordAngle;
            m_ePenSelect  = ePenSelect;
            m_bEdgeWedge  = bEdgeWedge;
        }

        public void SetStartPoint (Point ptStartPoint) { m_ptStartPoint = ptStartPoint; }
        public void SetRadius     (float fRadius)      { m_fRadius      = fRadius; }
        public void SetStartAngle (int   fStartAngle ) { m_iStartAngle  = fStartAngle; }
        public void SetSweepAngle (int   fSweepAngle ) { m_iSweepAngle  = fSweepAngle; }
        public void SetChordAngle (int   iChordAngle ) { m_iChordAngle  = iChordAngle; }
        public void SetAxisPoint  (Point ptAxis)       { m_ptAxis       = ptAxis; }

        //public Point GetStartPoint () { return m_ptStartPoint; }
        public float GetRadius     () { return m_fRadius; }
        public int   GetStartAngle () { return m_iStartAngle; }
        public int   GetSweepAngle () { return m_iSweepAngle; }
        public int   GetChordAngle () { return m_iChordAngle; }
        public Point GetAxisPoint  () { return m_ptAxis; }

        public void ResetStartPoint ()
        {
            m_ptStartPoint.X = -1;
            m_ptStartPoint.Y = -1;
        }
        public void ResetAxisPoint ()
        {
            m_ptAxis.X = -1;
            m_ptAxis.Y = -1;
        }
        public void ResetRadius ()     { m_fRadius      = 0.0F; }
        public void ResetStartAngle () { m_iStartAngle  = 0; }
        public void ResetSweepAngle () { m_iSweepAngle  = 0; }
        public void ResetChordAngle () { m_iChordAngle  = 0; }

        public override void ComputeStartAndEndPoints ()
        {
            m_ptStartPoint = CPlotterMath.PolarToCartesian (m_ptAxis, m_iStartAngle, (int)m_fRadius);
            m_ptEndPoint   = CPlotterMath.PolarToCartesian (m_ptAxis, m_iStartAngle - m_iSweepAngle, (int)m_fRadius);
        }

        //public override Point GetStartPoint ()
        //{
        //    throw new NotImplementedException ("CWedgeShape.GetStartPoint not implemented");
        //}

        //public override Point GetEndPoint ()
        //{
        //    throw new NotImplementedException ("CWedgeShape.GetEndPoint not implemented");
        //}

        public override void RotatePoints (int iDegrees, Point pAxisPoint)
        {
            throw new NotImplementedException ("CWedgeShape.RotatePoints not implemented");
        }

        public override void ResizeOrMove (Point ptLowerLeft, Point ptUpperRight)
        {
            throw new NotImplementedException ("CWedgeShape.ResizeOrMove not implemented");
        }

        public override bool IsEqualTo (CDrawingShapeElement that)
        {
            return base.IsConfigEqualTo (that)                             &&
                   this.m_fRadius     == ((CWedgeShape)that).m_fRadius     &&
                   this.m_iStartAngle == ((CWedgeShape)that).m_iStartAngle &&
                   this.m_iSweepAngle == ((CWedgeShape)that).m_iSweepAngle &&
                   this.m_iChordAngle == ((CWedgeShape)that).m_iChordAngle;
        }

        public override void LoadValuesFromString (string strShapeValues)
        {
            if (strShapeValues.Substring (0, 2) == "WS")
            {
                string strShapeString = strShapeValues.Substring (2);
                int iTildeIdx = strShapeString.IndexOf ('~');
                if (iTildeIdx >= 0)
                {
                    base.LoadValuesConfigFromString (strShapeString.Substring (0, iTildeIdx));
                    string strWedgeShapeValues    = strShapeString.Substring (iTildeIdx + 1);
                    string[] straWedgeShapeValues = CGenericMethods.ParseDelimitedString (strWedgeShapeValues);
                    m_fRadius                     = CGenericMethods.SafeConvertToFloat   (straWedgeShapeValues[0]);
                    m_iStartAngle                 = CGenericMethods.SafeConvertToInt     (straWedgeShapeValues[1]);
                    m_iSweepAngle                 = CGenericMethods.SafeConvertToInt     (straWedgeShapeValues[2]);
                    m_iChordAngle                 = CGenericMethods.SafeConvertToInt     (straWedgeShapeValues[3]);
                }
            }
        }

        public override string SaveValuesToString ()
        {
            string strShapeValues = string.Format ("WS{0}~{1:#####0.####},{2},{3},{4}", base.SaveConfigValuesToString (),
                                                   m_fRadius, m_iStartAngle, m_iSweepAngle, m_iChordAngle);

            return strShapeValues;
        }

        public override string GetHPGL (bool bIncludeConfigValues = true, EPlotMode ePlotMode = EPlotMode.ePlotDefault)
        {
            StringBuilder sbHPGL = new StringBuilder (bIncludeConfigValues ? base.GetConfigHPGL () : "");
            if (m_bEdgeWedge)
            {
                sbHPGL.Append (CHPGL.PlotAbsolute (m_ptAxis) +
                               CHPGL.EdgeWedge (m_fRadius, m_iStartAngle, -m_iSweepAngle, m_iChordAngle));
            }
            else
            {
                sbHPGL.Append (CHPGL.PlotAbsolute (m_ptAxis) +
                               CHPGL.ShadeWedge (m_fRadius, m_iStartAngle, -m_iSweepAngle, m_iChordAngle));
            }

            return sbHPGL.ToString ();
        }
    }

    public class CTextLabel : CDrawingShapeElement // TL
    {
        // Starting point, text, all character settings (size, slant, angle, direction, ...)

        string m_strLabelText = "";

        public CTextLabel (int iStartX, int iStartY, string strLabelText, EPenSelect ePenSelect, float fCharacterPrintAngle = 0.0F)
        {
            m_ptStartPoint.X             = iStartX;
            m_ptStartPoint.Y             = iStartY;
            m_strLabelText               = strLabelText;
            m_ePenSelect                 = ePenSelect;
            m_fCharacterPrintAngle       = fCharacterPrintAngle;
            float fAngleInRadians        = (float)(fCharacterPrintAngle * Math.PI / 180.0);

            m_fAbsoluteCharDirectionRise = (float)Math.Sin (fAngleInRadians);
            m_fAbsoluteCharDirectionRun  = (float)Math.Cos (fAngleInRadians);
        }

        public CTextLabel (Point ptStartPoint, string strLabelText, EPenSelect ePenSelect, float fCharacterPrintAngle = 0.0F)
        {
            m_ptStartPoint               = ptStartPoint;
            m_strLabelText               = strLabelText;
            m_ePenSelect                 = ePenSelect;
            m_fCharacterPrintAngle       = fCharacterPrintAngle;
            float fAngleInRadians        = (float)(fCharacterPrintAngle * Math.PI / 180.0);

            m_fAbsoluteCharDirectionRise = (float)Math.Sin (fAngleInRadians);
            m_fAbsoluteCharDirectionRun  = (float)Math.Cos (fAngleInRadians);
        }

        public void SetText (string strText) { m_strLabelText = strText; }
        public string GetText () { return m_strLabelText; }

        public override void ComputeStartAndEndPoints ()
        {
            int iLength = (int)(m_strLabelText.Length * m_fAbsoluteCharacterSizeWidth * CHARACTER_SIZE_TO_SPACE_FACTOR);
            m_ptEndPoint.X = m_ptStartPoint.X + iLength;
            m_ptEndPoint.Y = m_ptStartPoint.Y;

            m_ptEndPoint = CPlotterMath.RotatePoint (m_ptStartPoint, (int)m_fCharacterPrintAngle, m_ptEndPoint);
        }

        //public override Point GetStartPoint ()
        //{
        //    throw new NotImplementedException ("CTextLabel.GetStartPoint not implemented");
        //}

        //public override Point GetEndPoint ()
        //{
        //    throw new NotImplementedException ("CTextLabel.GetEndPoint not implemented");
        //}

        public override void RotatePoints (int iDegrees, Point pAxisPoint)
        {
            throw new NotImplementedException ("CTextLabel.RotatePoints not implemented");
        }

        public override void ResizeOrMove (Point ptLowerLeft, Point ptUpperRight)
        {
            throw new NotImplementedException ("CTextLabel.ResizeOrMove not implemented");
        }

        public override bool IsEqualTo (CDrawingShapeElement that)
        {
            return base.IsConfigEqualTo (that) &&
                   this.m_strLabelText == ((CTextLabel)that).m_strLabelText;
        }

        public override void LoadValuesFromString (string strShapeValues)
        {
            if (strShapeValues.Substring (0, 2) == "TL")
            {
                string strShapeString = strShapeValues.Substring (2);
                int iTildeIdx = strShapeString.IndexOf ('~');
                if (iTildeIdx >= 0)
                {
                    base.LoadValuesConfigFromString (strShapeString.Substring (0, iTildeIdx));
                    string strTextLabelValues    = strShapeString.Substring (iTildeIdx + 1);
                    string[] straTextLabelValues = CGenericMethods.ParseDelimitedString (strTextLabelValues);
                    m_ptStartPoint.X             = CGenericMethods.SafeConvertToInt (straTextLabelValues[0]);
                    m_ptStartPoint.Y             = CGenericMethods.SafeConvertToInt (straTextLabelValues[1]);
                    m_strLabelText               = straTextLabelValues[2];
                }
            }
        }

        public override string SaveValuesToString ()
        {
            string strShapeValues = string.Format ("TL{0}~{1},{2},{3}", base.SaveConfigValuesToString (), m_ptStartPoint.X, m_ptStartPoint.Y, m_strLabelText);

            return strShapeValues;
        }

        // DI The Absolute Direction Instruction
        // SI The Absolute Character Size Instruction

        // LB The Label Instruction
        // CP The Character Plot Instruction
        // UC The User-defined Character Instruction
        public override string GetHPGL (bool bIncludeConfigValues = true, EPlotMode ePlotMode = EPlotMode.ePlotDefault)
        {
            if (m_strLabelText.Length > 0)
            {
                StringBuilder sbHPGL = new StringBuilder (bIncludeConfigValues ? base.GetConfigHPGL () : "");
                if ( ePlotMode == EPlotMode.ePlotAbsolute ||
                    (ePlotMode == EPlotMode.ePlotDefault && m_bPlotAbsolute))
                {
                    sbHPGL.Append (CHPGL.PlotAbsolute (m_ptStartPoint.X, m_ptStartPoint.Y) + CHPGL.Label (m_strLabelText));
                }
                else
                {
                    sbHPGL.Append (CHPGL.PlotRelative (m_ptStartPoint.X, m_ptStartPoint.Y) + CHPGL.Label (m_strLabelText));
                }

                return sbHPGL.ToString ();
            }
            else
            {
                return "";
            }
        }
    }

    public class CChartRulerFrameShape : CDrawingShapeElement
    {
        // TODO: Add options for numeric labels: Zero-point value
        //                                       Increment/decrement interval values
        //                                       Rotation angle for labels
        #region DATA DEFINITIONS
        public enum ERulerCrossPoint
        {
            ENoCross,
            ECrossStartPoint,
            ECrossMiddlePoint,
            ECrossEndPoint
        }

        public enum EXYPair
        {
            EUndefined,
            ELeftTop,
            ELeftMiddle,
            ELeftBottom,
            ECenterTop,
            ECenterMiddle,
            ECenterBottom,
            ERightTop,
            ERightMiddle,
            ERightBottom
        }

        int    m_iCrossAxisStartPosition    = 0;
        bool   m_bVertical                  = false;
        int    m_iRulerStartPosition        = 0;
        int    m_iRulerEndPosition          = 0;
        int    m_iTicksOnRuler              = 0;
        int    m_iTickInterval              = 0;
        int    m_iTickPositive              = 0;
        int    m_iTickNegative              = 0;
        bool   m_bDrawFirstTickMark         = true;
        bool   m_bDrawMiddleTickMark        = true;
        bool   m_bDrawLastTickMark          = true;
        // int    m_iLeadTickOffset            = 0;

        ERulerCrossPoint m_eRulerCrossPoint = ERulerCrossPoint.ENoCross;
        #endregion

        #region GET METHODS
        public int  GetOffset ()        { return m_iCrossAxisStartPosition; }
        public bool IsVerical ()        { return m_bVertical; }
        public int  GetStartPosition () { return m_iRulerStartPosition; }
        public int  GetEndPosition ()   { return m_iRulerEndPosition; }
        public int  GetTicksOnRuler ()  { return m_iTicksOnRuler; }
        public int  GetTickInterval ()  { return m_iTickInterval; }
        public int  GetTickPositive ()  { return m_iTickPositive; }
        public int  GetTickNegative ()  { return m_iTickNegative; }
        #endregion

        //#region SET METHODS
        //public void SetVerical       (bool iVerical)         { m_bVertical               = iVerical; }
        //public void SetStartPosition (int  iStartPosition)   { m_iRulerStartPosition     = iStartPosition; }
        //public void SetEndPosition   (int  iEndPosition)     { m_iRulerEndPosition       = iEndPosition; }
        //public void SetTicksOnRuler  (int  iTicksOnRuler)    { m_iTicksOnRuler           = iTicksOnRuler; }
        //public void SetTickPositive  (int  iTickPositive)    { m_iTickPositive           = iTickPositive; }
        //public void SetTickNegative  (int  iTickNegative)    { m_iTickNegative           = iTickNegative; }
        //public void SetOffset        (int  iCrossAxisOffset) { m_iCrossAxisStartPosition = iCrossAxisOffset; }
        //#endregion

        public class CChartStatistics
        {
            int m_iXStepSize = -1;
            int m_iYStepSize = -1;
            int m_iXPosTicks = -1;
            int m_iXNegTicks = -1;
            int m_iYPosTicks = -1;
            int m_iYNegTicks = -1;

            public void SetXStepSize (int iXStepSize) { if (m_iXStepSize == -1) m_iXStepSize = iXStepSize; }
            public void SetYStepSize (int iYStepSize) { if (m_iYStepSize == -1) m_iYStepSize = iYStepSize; }
            public void SetXPosTicks (int iXPosTicks) { if (m_iXPosTicks == -1) m_iXPosTicks = iXPosTicks; }
            public void SetXNegTicks (int iXNegTicks) { if (m_iXNegTicks == -1) m_iXNegTicks = iXNegTicks; }
            public void SetYPosTicks (int iYPosTicks) { if (m_iYPosTicks == -1) m_iYPosTicks = iYPosTicks; }
            public void SetYNegTicks (int iYNegTicks) { if (m_iYNegTicks == -1) m_iYNegTicks = iYNegTicks; }

            public int GetXStepSize () { return m_iXStepSize; }
            public int GetYStepSize () { return m_iYStepSize; }
            public int GetXPosTicks () { return m_iXPosTicks; }
            public int GetXNegTicks () { return m_iXNegTicks; }
            public int GetYPosTicks () { return m_iYPosTicks; }
            public int GetYNegTicks () { return m_iYNegTicks; }
        }

        public CChartRulerFrameShape (int iCrossAxisStartPosition,                                  // X-axis start point for vertical, Y-axis for horizontal
                                      EPenSelect ePenSelect,                                        // Plotter pen selection
                                      bool bVertical                    = false,                    // Horizontal (default) or vertical
                                      int iRulerStartPosition           = -1,                       // Y-axis start point for vertical, X-axis for horizontal
                                      int iRulerEndPosition             = -1,                       // Not used (Y-axis end point for vertical, X-axis for horizontal)
                                      int iTickCount                    = -1,                       // Number of tick marks to be drawn
                                      int iTickInterval                 = -1,                       // Plotter "pixels" between tick marks
                                      int iTickPositive                 = 0,                        // Length of tick mark above horizontal ruler, left for vertical
                                      int iTickNegative                 = 0,                        // Length of tick mark below horizontal ruler, right for vertical
                                      int iSortGroup                    = 0,
                                      bool bDrawFirstTickMark           = true,
                                      bool bDrawMiddleTickMark          = true,
                                      bool bDrawLastTickMark            = true,
                                      ERulerCrossPoint eRulerCrossPoint = ERulerCrossPoint.ENoCross)
        {
            if (eRulerCrossPoint == ERulerCrossPoint.ECrossStartPoint)
            {
                bDrawFirstTickMark = false;
            }
            else if (eRulerCrossPoint == ERulerCrossPoint.ECrossMiddlePoint)
            {
                bDrawMiddleTickMark = false;
            }
            else if (eRulerCrossPoint == ERulerCrossPoint.ECrossEndPoint)
            {
                bDrawLastTickMark = false;
            }

            // Sanity checks
            int iRulerDefValues = (iRulerStartPosition > -1 ? 1 : 0);
            iRulerDefValues    += (iRulerEndPosition   > -1 ? 1 : 0);
            iRulerDefValues    += (iTickCount          > -1 ? 1 : 0);
            iRulerDefValues    += (iTickInterval       > -1 ? 1 : 0);
            if (iRulerDefValues < 3)
            {
                throw new Exception ("Too few ruler / tick definition values in CChartRulerFrameShape ctor");
            }

            // Calculate End Position if -1; calculate iTickInterval if -1; calulate iTickCount if -1
            if (iRulerStartPosition > -1 &&
                iRulerEndPosition   > -1 &&
                iTickCount          > -1 &&
                iTickInterval       > -1)
            {
                // If all specified, ignore iTickInterval
                iTickInterval = -1;
            }

            if (iRulerStartPosition == -1)
            {
                iRulerStartPosition = iRulerEndPosition - (iTickCount * iTickInterval);
            }
            else if (iRulerEndPosition == -1)
            {
                iRulerEndPosition = iRulerStartPosition + (iTickCount * iTickInterval);
            }
            else if (iTickCount == -1 &&
                     iTickInterval > 0)
            {
                iTickCount = (iRulerEndPosition - iRulerStartPosition) / iTickInterval;
            }
            else if (iTickInterval == -1 &&
                     iTickCount > 1)
            {
                iTickInterval = (iRulerEndPosition - iRulerStartPosition) / (iTickCount - 1);
            }

            m_iCrossAxisStartPosition = iCrossAxisStartPosition;
            m_ePenSelect              = ePenSelect;
            m_bVertical               = bVertical;
            m_iRulerStartPosition     = iRulerStartPosition;
            m_iRulerEndPosition       = iRulerEndPosition;
            m_iTicksOnRuler           = iTickCount;
            m_iTickInterval           = iTickInterval;
            m_iTickPositive           = iTickPositive;
            m_iTickNegative           = iTickNegative;
            m_iSortGroup              = iSortGroup;
            m_bDrawFirstTickMark      = bDrawFirstTickMark;
            m_bDrawMiddleTickMark     = bDrawMiddleTickMark;
            m_bDrawLastTickMark       = bDrawLastTickMark;
            m_eRulerCrossPoint        = eRulerCrossPoint;
        }

        public override void ComputeStartAndEndPoints ()
        {
        }

        public override void RotatePoints (int iDegrees, Point pAxisPoint)
        {
            throw new NotImplementedException ("CTextLabel.RotatePoints not implemented");
        }

        public override void ResizeOrMove (Point ptLowerLeft, Point ptUpperRight)
        {
            throw new NotImplementedException ("CTextLabel.ResizeOrMove not implemented");
        }

        public override bool IsEqualTo (CDrawingShapeElement that)
        {
            if (this.m_iCrossAxisStartPosition != ((CChartRulerFrameShape)that).m_iCrossAxisStartPosition)
            {
                return false;
            }

            if (this.m_ePenSelect              != ((CChartRulerFrameShape)that).m_ePenSelect)
            {
                return false;
            }

            if (this.m_bVertical               != ((CChartRulerFrameShape)that).m_bVertical)
            {
                return false;
            }

            if (this.m_iRulerStartPosition     != ((CChartRulerFrameShape)that).m_iRulerStartPosition)
            {
                return false;
            }

            if (this.m_iRulerEndPosition       != ((CChartRulerFrameShape)that).m_iRulerEndPosition)
            {
                return false;
            }

            if (this.m_iTicksOnRuler           != ((CChartRulerFrameShape)that).m_iTicksOnRuler)
            {
                return false;
            }

            if (this.m_iTickInterval           != ((CChartRulerFrameShape)that).m_iTickInterval)
            {
                return false;
            }

            if (this.m_iTickPositive           != ((CChartRulerFrameShape)that).m_iTickPositive)
            {
                return false;
            }

            if (this.m_iTickNegative           != ((CChartRulerFrameShape)that).m_iTickNegative)
            {
                return false;
            }

            if (this.m_iSortGroup              != ((CChartRulerFrameShape)that).m_iSortGroup)
            {
                return false;
            }

            if (this.m_bDrawFirstTickMark      != ((CChartRulerFrameShape)that).m_bDrawFirstTickMark)
            {
                return false;
            }

            if (this.m_bDrawMiddleTickMark     != ((CChartRulerFrameShape)that).m_bDrawMiddleTickMark)
            {
                return false;
            }

            if (this.m_bDrawLastTickMark       != ((CChartRulerFrameShape)that).m_bDrawLastTickMark)
            {
                return false;
            }

            if (this.m_eRulerCrossPoint        != ((CChartRulerFrameShape)that).m_eRulerCrossPoint)
            {
                return false;
            }

            //if (this.m_iLeadTickOffset         != ((CChartRulerFrameShape)that).m_iLeadTickOffset)
            //{
            //    return false;
            //}

            return true;
        }

        public override void LoadValuesFromString (string strShapeValues)
        {
            if (strShapeValues.Substring (0, 2) == "CR")
            {
                string strShapeString = strShapeValues.Substring (2);
                int iTildeIdx = strShapeString.IndexOf ('~');
                if (iTildeIdx >= 0)
                {
                    base.LoadValuesConfigFromString (strShapeString.Substring (0, iTildeIdx));
                    string strRulerShapeValues    = strShapeString.Substring (iTildeIdx + 1);
                    string[] straRulerShapeValues = CGenericMethods.ParseDelimitedString (strRulerShapeValues);

                    m_iCrossAxisStartPosition = CGenericMethods.SafeConvertToInt (straRulerShapeValues[0]);
                    m_ePenSelect              = (EPenSelect)CGenericMethods.SafeConvertToInt (straRulerShapeValues[1]);
                    m_bVertical               = (straRulerShapeValues[2][0]) == 'T';
                    m_iRulerStartPosition     = CGenericMethods.SafeConvertToInt (straRulerShapeValues[3]);
                    m_iRulerEndPosition       = CGenericMethods.SafeConvertToInt (straRulerShapeValues[4]);
                    m_iTicksOnRuler           = CGenericMethods.SafeConvertToInt (straRulerShapeValues[5]);
                    m_iTickInterval           = CGenericMethods.SafeConvertToInt (straRulerShapeValues[6]);
                    m_iTickPositive           = CGenericMethods.SafeConvertToInt (straRulerShapeValues[7]);
                    m_iTickNegative           = CGenericMethods.SafeConvertToInt (straRulerShapeValues[8]);
                    m_iSortGroup              = CGenericMethods.SafeConvertToInt (straRulerShapeValues[9]);
                    m_bDrawFirstTickMark      = (straRulerShapeValues[10][0]) == 'T';
                    m_bDrawMiddleTickMark     = (straRulerShapeValues[11][0]) == 'T';
                    m_bDrawLastTickMark       = (straRulerShapeValues[12][0]) == 'T';
                    m_eRulerCrossPoint        = (ERulerCrossPoint)CGenericMethods.SafeConvertToInt (straRulerShapeValues[13]);
                    //m_iLeadTickOffset         = CGenericMethods.SafeConvertToInt (straRulerShapeValues[14]);
                }
            }
        }

        public override string SaveValuesToString ()
        {
            string strShapeValues = string.Format ("CR{0}~{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                                                    base.SaveConfigValuesToString (),  // 0
                                                    m_iCrossAxisStartPosition,         // 1
                                                    (int)m_ePenSelect,                 // 2
                                                    m_bVertical ? 'T' : 'F',           // 3
                                                    m_iRulerStartPosition,             // 4
                                                    m_iRulerEndPosition,               // 5
                                                    m_iTicksOnRuler,                   // 6
                                                    m_iTickInterval,                   // 7
                                                    m_iTickPositive,                   // 8
                                                    m_iTickNegative,                   // 9
                                                    m_iSortGroup,                      // 10
                                                    m_bDrawFirstTickMark  ? 'T' : 'F', // 11
                                                    m_bDrawMiddleTickMark ? 'T' : 'F', // 12
                                                    m_bDrawLastTickMark   ? 'T' : 'F', // 13
                                                    (int)m_eRulerCrossPoint);          // 14
                                                    //m_iLeadTickOffset);                // 15

            return strShapeValues;
        }

        public override string GetHPGL (bool bIncludeConfigValues = true, EPlotMode ePlotMode = EPlotMode.ePlotDefault)
        {
            // if ruler too close to edge of bounding rectangle for iTickPositive or iTickNegative, adjust ruler position to make room for tick mark
            // Calculate zero-point tick index on ruler (not coordinate value)

            StringBuilder sbHPGL = new StringBuilder (bIncludeConfigValues ? base.GetConfigHPGL () : "");
            sbHPGL.Append (CHPGL.SelectPen (m_ePenSelect));
            int iZeroPointTickIdx = m_iTicksOnRuler / 2;

            if (m_bVertical)
            {
                // First draw main ruler line from start position to end position, going up to down or right to left if EndPosition < StartPosition
                sbHPGL.Append (CHPGL.PlotAbsolute (m_iCrossAxisStartPosition, m_iRulerStartPosition));
                sbHPGL.Append (CHPGL.PenDown ());
                sbHPGL.Append (CHPGL.PlotAbsolute (m_iCrossAxisStartPosition, m_iRulerEndPosition));

                // Then draw tick marks using for loop
                if (m_iTickInterval > 0 &&
                    m_iTicksOnRuler > 0)
                {
                    for (int iIdx = m_iTicksOnRuler - 1; iIdx >= 0; --iIdx) // Draw in reverse order to minimize pen travel from end of ruler to first tick mark
                    {
                        if (!m_bDrawFirstTickMark  && iIdx == 0)
                            continue;
                        if (!m_bDrawMiddleTickMark && iIdx == iZeroPointTickIdx)
                            continue;
                        if (!m_bDrawLastTickMark   && iIdx == m_iTicksOnRuler - 1)
                            continue;

                        int iTickPosition = (iIdx == m_iTicksOnRuler - 1) ? m_iRulerEndPosition : m_iRulerStartPosition + (m_iTickInterval * iIdx);
                        sbHPGL.Append (CHPGL.PlotAbsolute (m_iCrossAxisStartPosition, iTickPosition));

                        // Replace XAxisTick ()
                        sbHPGL.Append (CHPGL.PlotXAxisTick (m_iTickPositive, m_iTickNegative));
                    }
                }
            }
            else
            {
                // First draw main ruler line from start position to end position, going up to down or right to left if EndPosition < StartPosition
                sbHPGL.Append (CHPGL.PlotAbsolute (m_iRulerStartPosition, m_iCrossAxisStartPosition));
                sbHPGL.Append (CHPGL.PenDown ());
                sbHPGL.Append (CHPGL.PlotAbsolute (m_iRulerEndPosition, m_iCrossAxisStartPosition));

                // Then draw tick marks using for loop
                if (m_iTickInterval > 0 &&
                    m_iTicksOnRuler > 0)
                {
                    for (int iIdx = m_iTicksOnRuler - 1; iIdx >= 0; --iIdx) // Draw in reverse order to minimize pen travel from end of ruler to first tick mark
                    {
                        if (!m_bDrawFirstTickMark  && iIdx == 0)
                            continue;
                        if (!m_bDrawMiddleTickMark && iIdx == iZeroPointTickIdx)
                            continue;
                        if (!m_bDrawLastTickMark   && iIdx == m_iTicksOnRuler - 1)
                            continue;

                        int iTickPosition = (iIdx == m_iTicksOnRuler - 1) ? m_iRulerEndPosition : m_iRulerStartPosition + (m_iTickInterval * iIdx);
                        sbHPGL.Append (CHPGL.PlotAbsolute (iTickPosition, m_iCrossAxisStartPosition));

                        // Replace XAxisTick ()
                        sbHPGL.Append (CHPGL.PlotYAxisTick (m_iTickPositive, m_iTickNegative));
                    }
                }
            }

            sbHPGL.Append (CHPGL.PenUp ());

            //Console.WriteLine (sbHPGL.ToString ());

            return sbHPGL.ToString ();
        }

        public static CChartRulerFrameShape[] CreateXYRulerPair (Point ptOuterRectLL,
                                                                 Point ptOuterRectTR,
                                                                 ref Point rptChartCenter,
                                                                 ref CChartStatistics robjStat,
                                                                 EXYPair eXYPair,
                                                                 EPenSelect ePenSelect,
                                                                 int iHorzTickCount    = 0,
                                                                 int iHorzTickPositive = 0,
                                                                 int iHorzTickNegative = 0,
                                                                 int iVertTickCount    = 0,
                                                                 int iVertTickPositive = 0,
                                                                 int iVertTickNegative = 0,
                                                                 int iSortGroup        = 0)
        {
            if (eXYPair == EXYPair.EUndefined)
            {
                throw new Exception ("Invalid EXYPair selection in CChartRulerFrameShape.CreateXYRulerPair");
            }

            List<CChartRulerFrameShape> lcrfXYPair = new List<CChartRulerFrameShape> ();
            ERulerCrossPoint eZeroCrossVertical   = ERulerCrossPoint.ENoCross;
            ERulerCrossPoint eZeroCrossHorizontal = ERulerCrossPoint.ENoCross;

            Point ptInnerRectLL = new Point (ptOuterRectLL.X + iVertTickNegative, ptOuterRectLL.Y + iHorzTickNegative);
            Point ptInnerRectTR = new Point (ptOuterRectTR.X - iVertTickPositive, ptOuterRectTR.Y - iHorzTickPositive);

            int iHorzRulerLength = ptInnerRectTR.X - ptInnerRectLL.X;
            int iVertRulerLength = ptInnerRectTR.Y - ptInnerRectLL.Y;

            int iHorzTickInterval = (iHorzTickCount > 1) ? iHorzRulerLength / (iHorzTickCount - 1) : iHorzRulerLength;
            int iVertTickInterval = (iVertTickCount > 1) ? iVertRulerLength / (iVertTickCount - 1) : iVertRulerLength;

            #region EZeroCrossPoint
            if (eXYPair == EXYPair.ELeftTop)
            {
                eZeroCrossVertical   = ERulerCrossPoint.ECrossEndPoint;
                eZeroCrossHorizontal = ERulerCrossPoint.ECrossStartPoint;
            }
            else if (eXYPair == EXYPair.ELeftMiddle)
            {
                eZeroCrossVertical   = ERulerCrossPoint.ECrossMiddlePoint;
                eZeroCrossHorizontal = ERulerCrossPoint.ECrossStartPoint;
            }
            else if (eXYPair == EXYPair.ELeftBottom)
            {
                eZeroCrossVertical   = ERulerCrossPoint.ECrossStartPoint;
                eZeroCrossHorizontal = ERulerCrossPoint.ECrossStartPoint;
            }
            else if (eXYPair == EXYPair.ECenterTop)
            {
                eZeroCrossVertical   = ERulerCrossPoint.ECrossEndPoint;
                eZeroCrossHorizontal = ERulerCrossPoint.ECrossMiddlePoint;
            }
            else if (eXYPair == EXYPair.ECenterMiddle)
            {
                eZeroCrossVertical   = ERulerCrossPoint.ECrossMiddlePoint;
                eZeroCrossHorizontal = ERulerCrossPoint.ECrossMiddlePoint;
            }
            else if (eXYPair == EXYPair.ECenterBottom)
            {
                eZeroCrossVertical   = ERulerCrossPoint.ECrossStartPoint;
                eZeroCrossHorizontal = ERulerCrossPoint.ECrossMiddlePoint;
            }
            else if (eXYPair == EXYPair.ERightTop)
            {
                eZeroCrossVertical   = ERulerCrossPoint.ECrossEndPoint;
                eZeroCrossHorizontal = ERulerCrossPoint.ECrossEndPoint;
            }
            else if (eXYPair == EXYPair.ERightMiddle)
            {
                eZeroCrossVertical   = ERulerCrossPoint.ECrossMiddlePoint;
                eZeroCrossHorizontal = ERulerCrossPoint.ECrossEndPoint;
            }
            else if (eXYPair == EXYPair.ERightBottom)
            {
                eZeroCrossVertical   = ERulerCrossPoint.ECrossStartPoint;
                eZeroCrossHorizontal = ERulerCrossPoint.ECrossEndPoint;
            }
            #endregion

            int iHorzZeroPointIdx = -1;
            int iVertZeroPointIdx = -1;
            int iHorzZeroPoint    = -1;
            int iVertZeroPoint    = -1;

            if (eZeroCrossHorizontal == ERulerCrossPoint.ECrossStartPoint)
            {
                iHorzZeroPointIdx = 0;
                iHorzZeroPoint    = ptInnerRectLL.X;
            }
            else if (eZeroCrossHorizontal == ERulerCrossPoint.ECrossMiddlePoint)
            {
                iHorzZeroPointIdx = iHorzTickCount / 2;
                iHorzZeroPoint    = ptInnerRectLL.X + (iHorzZeroPointIdx * iHorzTickInterval);
            }
            else if (eZeroCrossHorizontal == ERulerCrossPoint.ECrossEndPoint)
            {
                iHorzZeroPointIdx = iHorzTickCount - 1;
                iHorzZeroPoint    = ptInnerRectTR.X;
            }

            if (eZeroCrossVertical == ERulerCrossPoint.ECrossStartPoint)
            {
                iVertZeroPointIdx = 0;
                iVertZeroPoint    = ptInnerRectLL.Y;
            }
            else if (eZeroCrossVertical == ERulerCrossPoint.ECrossMiddlePoint)
            {
                iVertZeroPointIdx = iVertTickCount / 2;
                iVertZeroPoint    = ptInnerRectLL.Y + (iVertZeroPointIdx * iVertTickInterval);
            }
            else if (eZeroCrossVertical == ERulerCrossPoint.ECrossEndPoint)
            {
                iVertZeroPointIdx = iVertTickCount - 1;
                iVertZeroPoint    = ptInnerRectTR.Y;
            }

            if (rptChartCenter != null)
            {
                rptChartCenter.X = iHorzZeroPoint;
                rptChartCenter.Y = iVertZeroPoint;
            }

            if (robjStat != null)
            {
                robjStat.SetXStepSize (iHorzTickInterval);
                robjStat.SetYStepSize (iVertTickInterval);
                robjStat.SetXPosTicks (iHorzTickCount / 2);
                robjStat.SetXNegTicks ((iHorzTickCount % 2 == 0) ? robjStat.GetXPosTicks () - 1 : robjStat.GetXPosTicks ()); // 1 less than pos tick count if even
                robjStat.SetYPosTicks (iVertTickCount / 2);
                robjStat.SetYNegTicks ((iVertTickCount % 2 == 0) ? robjStat.GetYPosTicks () - 1 : robjStat.GetYPosTicks ()); // 1 less than pos tick count if even
            }

            CChartRulerFrameShape crfHorizontal = new CChartRulerFrameShape (iVertZeroPoint,     // int iCrossAxisStartPosition,       X-axis start point for vertical, Y-axis for horizontal
                                                                             ePenSelect,         // EPenSelect ePenSelect,             Plotter pen selection
                                                                             false,              // bool bVertical            = false  Horizontal (default) or vertical
                                                                             ptInnerRectLL.X,    // int iRulerStartPosition   = -1     Y-axis start point for vertical, X-axis for horizontal
                                                                             ptInnerRectTR.X,    // int iRulerEndPosition     = -1     Not used (Y-axis end point for vertical, X-axis for horizontal)
                                                                             iHorzTickCount,     // int iTickCount            = 0      Number of tick marks to be drawn
                                                                             iHorzTickInterval,  // int iTickInterval         = -1     Plotter "pixels" between tick marks
                                                                             iHorzTickPositive,  // int iTickPositive         = 0      Length of tick mark above horizontal ruler, left for vertical
                                                                             iHorzTickNegative,  // int iLeadTickOffset       = 0      Also not used
                                                                             iSortGroup);        // int iSortGroup            = 0      
            crfHorizontal.m_ptEndPoint.X = ptInnerRectTR.X;
            crfHorizontal.m_ptEndPoint.Y = ptInnerRectLL.Y;
            lcrfXYPair.Add (crfHorizontal);

            CChartRulerFrameShape crfVertical = new CChartRulerFrameShape (iHorzZeroPoint,       // int iCrossAxisStartPosition
                                                                           ePenSelect,           // EPenSelect ePenSelect
                                                                           true,                 // bool bVertical = false
                                                                           ptInnerRectLL.Y,      // int iRulerStartPosition = -1
                                                                           ptInnerRectTR.Y,      // int iRulerEndPosition = -1
                                                                           iVertTickCount,       // int iTickCount = 0
                                                                           iVertTickInterval,    // int iTickInterval = -1
                                                                           iVertTickPositive,    // int iTickPositive = 0
                                                                           iVertTickNegative,    // int iLeadTickOffset = 0
                                                                           iSortGroup);          // int iSortGroup = 0
            crfVertical.m_ptEndPoint.X = ptInnerRectLL.X;
            crfVertical.m_ptEndPoint.Y = ptInnerRectTR.Y;
            lcrfXYPair.Add (crfVertical);

            Console.WriteLine ("Chart center: " + rptChartCenter.ToString ());

            return lcrfXYPair.ToArray ();
        }

        // Unfinished method initially intended for use by PlotPolynomialChart().  It
        // was abandoned in favor of creating four rulers individually in the method.
        // Optionally import that ruler-creating code from PlotPolynomialChart().
        public CChartRulerFrameShape[] CreateXYRulerPairFromZeroPoint (Point ptOuterRectLL,
                                                                       Point ptOuterRectTR,
                                                                       Point ptChartCenter,
                                                                       int iTickInterval,
                                                                       EPenSelect ePenSelect,
                                                                       int iHorzTickCount    = 0,
                                                                       int iHorzTickPositive = 0,
                                                                       int iHorzTickNegative = 0,
                                                                       int iVertTickCount    = 0,
                                                                       int iVertTickPositive = 0,
                                                                       int iVertTickNegative = 0,
                                                                       int iSortGroup        = 0)
        {
            List<CChartRulerFrameShape> lcrfXYPair = new List<CChartRulerFrameShape> ();

            Point ptInnerRectLL = new Point (ptOuterRectLL.X + iVertTickNegative, ptOuterRectLL.Y + iHorzTickNegative);
            Point ptInnerRectTR = new Point (ptOuterRectTR.X - iVertTickPositive, ptOuterRectTR.Y - iHorzTickPositive);

            int iHorzRulerLength = ptInnerRectTR.X - ptInnerRectLL.X;
            int iVertRulerLength = ptInnerRectTR.Y - ptInnerRectLL.Y;

            CChartRulerFrameShape crfHorizontal = new CChartRulerFrameShape (ptChartCenter.Y,                // int iCrossAxisStartPosition
                                                                             ePenSelect,                     // EPenSelect ePenSelect
                                                                             false,                          // bool bVertical = false
                                                                             ptInnerRectLL.X,                // int iRulerStartPosition = -1
                                                                             ptInnerRectTR.X,                // int iRulerEndPosition = -1
                                                                             iHorzTickCount,                 // int iTickCount = 0
                                                                             iTickInterval,                  // int iTickInterval = -1
                                                                             iHorzTickPositive,              // int iTickPositive = 0
                                                                             iHorzTickNegative,              // int iLeadTickOffset = 0
                                                                             iSortGroup);                    // int iSortGroup = 0
            crfHorizontal.m_ptEndPoint.X = ptInnerRectTR.X;
            crfHorizontal.m_ptEndPoint.Y = ptInnerRectLL.Y;
            lcrfXYPair.Add (crfHorizontal);

            CChartRulerFrameShape crfVertical = new CChartRulerFrameShape (ptChartCenter.X,                // int iCrossAxisStartPosition
                                                                           ePenSelect,                     // EPenSelect ePenSelect
                                                                           true,                           // bool bVertical = false
                                                                           ptInnerRectLL.Y,                // int iRulerStartPosition = -1
                                                                           ptInnerRectTR.Y,                // int iRulerEndPosition = -1
                                                                           iVertTickCount,                 // int iTickCount = 0
                                                                           iTickInterval,                  // int iTickInterval = -1
                                                                           iVertTickPositive,              // int iTickPositive = 0
                                                                           iVertTickNegative,              // int iLeadTickOffset = 0
                                                                           iSortGroup);                    // int iSortGroup = 0
            crfVertical.m_ptEndPoint.X = ptInnerRectLL.X;
            crfVertical.m_ptEndPoint.Y = ptInnerRectTR.Y;
            lcrfXYPair.Add (crfVertical);

            Console.WriteLine ("Chart center: " + ptChartCenter.ToString ());

            return lcrfXYPair.ToArray ();
        }
    }
    #endregion

    public static class CPlotterStringArt
    {
        public static Point[] PlotSineWave (int iZeroLineY, int iWaveLength, int iAmplitude, int iStartX, int iEndX,
                                            int iStartDegree = 0, int iResolution = 10, bool bInvertPhase = false)
        {
            // Validate arguments
            if (iWaveLength < 1)
            {
                iWaveLength = 1;
            }

            if (iAmplitude < 1)
            {
                iAmplitude = 1000;
            }

            if (iResolution < 1)
            {
                iResolution = 1;
            }

            iAmplitude /= 2; // Convert values to peak-to-peak

            // Initialize
            List<Point> lptSinePoints = new List<Point> ();
            double dAngleStep = 360 / (double)iWaveLength;

            iStartDegree %= 360;
            if (iStartDegree < 0)
            {
                iStartDegree += 360;
            }

            for (int iXAxis = iStartX; iXAxis <= iEndX; iXAxis += iResolution)
            {
                double dAngle = (double)iXAxis * dAngleStep;
                dAngle += (double)iStartDegree;
                double dRadians = Math.PI * dAngle / 180.0;

                double dSine = Math.Sin (dRadians) * (bInvertPhase ? -1 : 1);
                dSine *= (double)iAmplitude;
                int iYAxis = (int)(dSine);
                iYAxis += iZeroLineY;

                //Console.WriteLine ("X: {0, 6:D} Y: {1, 6:D}", iXAxis, iYAxis);
                lptSinePoints.Add (new Point (iXAxis, iYAxis));
            }

            return lptSinePoints.ToArray ();
        }

        public static Point[] PlotLissajousCurve (int iWaveLengthX, int iWaveLengthY, int iAmplitudeX, int iAmplitudeY, bool bSwapXandY = false,
                                                  int iPhaseX = 0, int iPhaseY = 0, bool bInvertX = false, bool bInvertY = false)
        {
            if (iWaveLengthX > 100 ||
                iWaveLengthY > 100)
            {
                throw new Exception ("Wavelength values must not exceed 100");
            }

            int iResolution = 10;
            int iPlotPoints = (iWaveLengthX) * (iWaveLengthY) * 100;
            List<Point> lptLissajousPoints = new List<Point> ();
            Point[] aptSineWaveX = PlotSineWave (iAmplitudeX / 2, iWaveLengthX * 1000, iAmplitudeX, 0, iPlotPoints * iResolution, iPhaseX, iResolution, bInvertX);
            Point[] aptSineWaveY = PlotSineWave (iAmplitudeY / 2, iWaveLengthY * 1000, iAmplitudeY, 0, iPlotPoints * iResolution, iPhaseY, iResolution, bInvertY);

            if (aptSineWaveX.Length == aptSineWaveY.Length)
            {
                int iLowX  = 0,
                    iHighX = 0,
                    iLowY  = 0,
                    iHighY = 0,
                    iBiasX = 0,
                    iBiasY = 0;

                for (int iIdx = 0; iIdx < aptSineWaveX.Length; ++iIdx)
                {
                    if (iLowX  > aptSineWaveX[iIdx].Y)
                        iLowX  = aptSineWaveX[iIdx].Y;
                    if (iHighX < aptSineWaveX[iIdx].Y)
                        iHighX = aptSineWaveX[iIdx].Y;

                    if (iLowY  > aptSineWaveY[iIdx].Y)
                        iLowY  = aptSineWaveY[iIdx].Y;
                    if (iHighY < aptSineWaveY[iIdx].Y)
                        iHighY = aptSineWaveY[iIdx].Y;
                }

                if (iLowX < 0)
                {
                    iBiasX = -iLowX;
                }

                if (iLowY < 0)
                {
                    iBiasY = -iLowY;
                }

                for (int iIdx = 0; iIdx < aptSineWaveX.Length; ++iIdx)
                {
                    if (bSwapXandY)
                    {
                        lptLissajousPoints.Add (new Point (aptSineWaveY[iIdx].Y + iBiasX, aptSineWaveX[iIdx].Y + iBiasY));
                    }
                    else
                    {
                        lptLissajousPoints.Add (new Point (aptSineWaveX[iIdx].Y + iBiasX, aptSineWaveY[iIdx].Y + iBiasY));
                    }
                }
            }

            return lptLissajousPoints.ToArray ();
        }

        public static Point[] PlotEllipse (Point ptLL,
                                           Point ptUR,
                                           int iPlotPointCount)
        {
            if (ptLL.X >= ptUR.X ||
                ptLL.Y >= ptUR.Y)
            {
                throw new Exception ("Invalid values passed to CPlotterStringArt.PlotEllipse");
            }

            int iCenterX = ((ptUR.X - ptLL.X) / 2) + ptLL.X;
            int iCenterY = ((ptUR.Y - ptLL.Y) / 2) + ptLL.Y;
            int iRatioX = (ptUR.X - ptLL.X) / 2;
            int iRatioY = (ptUR.Y - ptLL.Y) / 2;

            return PlotEllipse ((float)iCenterX, (float)iCenterY, (float)iRatioX, (float)iRatioY, iPlotPointCount);
        }

        // Given a radius R and an angle θ, the equations x = R * cos(θ), y = R * sin(θ) give the coordinates of a point
        // on a circle of radius R centered at the origin. The picture on the right shows how the equations define the
        // point’s location. To trace out a circle, you can connect the points that you get when you let θ vary from 0 to
        // 2 π. If you scale the X and Y coordinates by different amounts, you can make an ellipse. Finally, if you add
        // offsets to the X and Y coordinates, you can move the result so it is centered somewhere other than at the origin.
        // http://csharphelper.com/blog/2019/03/use-sines-and-cosines-to-draw-circles-and-ellipses-in-c/
        // Another approach is documented in "Modern Algebra and Trigonometry" p 306, but it requires different algorithms
        // for each quadrant.  This method is simpler as it uses the same code for all quadrants.
        public static Point[] PlotEllipse (float fCenterX, 
                                           float fCenterY,
                                           float fRatioX,
                                           float fRatioY,
                                           int   iPlotPointCount)
        {
            List<Point> lptCircle = new List<Point> ();
            float fDTheta = (float)(2 * Math.PI / iPlotPointCount);
            float fTheta = 0;
            for (int i = 0; i < iPlotPointCount; i++)
            {
                int iX = (int)(fCenterX + fRatioX * Math.Cos (fTheta));
                int iY = (int)(fCenterY + fRatioY * Math.Sin (fTheta));
                lptCircle.Add (new Point (iX, iY));
                fTheta += fDTheta;
            }

            return lptCircle.ToArray ();
        }

        public static CDrawingShapeElement[] PlotPolynomialChart (double[] daFactors, double dLeftX, double dRightX, double dStep,
                                                                  ref int riZeroPointX, ref int riZeroPointY, ref int riIncrement,
                                                                  EPenSelect epsPlot   = EPenSelect.ESelectPen1,
                                                                  EPenSelect epsRulers = EPenSelect.ESelectNoPen,
                                                                  EPenSelect epsGrid   = EPenSelect.ESelectNoPen,
                                                                  EPenSelect epsFrame  = EPenSelect.ESelectNoPen,
                                                                  double dPlotScale    = 3000)
        {
            // First: call PlotPolynomialFuntion to get the plot dimensions, zero point, and increment size
            // Second: create rulers that intersect at the zero point
            //         Add rulers to the lclsPolynomialChart collection
            // Third: draw grid using plot dimensions, zero point, and increment size
            //        Add grid to the lclsPolynomialChart collection flagged as unsorted (1 group per chart quadrant)
            // Last: draw thick frame around entire chart

            List<CDrawingShapeElement> ldesPolynomialChart = new List<CDrawingShapeElement> ();
            if (epsPlot == EPenSelect.ESelectNoPen)
            {
                // If there's no pen selection for the polynomial plot, there's no point in going any further, is there?
                return ldesPolynomialChart.ToArray ();
            }

            const int FRAME_THICKNESS = 2;
            const int TICK_LENGTH     = 25;

            // Create and add the polynomial chart
            riIncrement = -1;
            Point ptLL = new Point ();
            Point ptUR = new Point ();
            Point[] paPolynomialPlot = PlotPolynomialFunction (daFactors, dLeftX, dRightX, dStep, ref riZeroPointX, ref riZeroPointY, ref riIncrement,
                                                               ref ptLL, ref ptUR, dPlotScale);
            CComplexLinesShape clsPlot = new CComplexLinesShape (paPolynomialPlot, epsPlot, true);
            ldesPolynomialChart.Add (clsPlot);

            // Get the bounding rectangle edges from the plot points
            int iLeftEdge   = ptLL.X;
            int iRightEdge  = ptUR.X;
            int iTopEdge    = ptUR.Y;
            int iBottomEdge = ptLL.Y;
            int iFrameEdgeBuffer = riIncrement / 2;

            // Create the rulers
            if (epsRulers != EPenSelect.ESelectNoPen)
            {
                // Create the Vertical Top ruler
                int iTickCount = (iTopEdge - riZeroPointY) / riIncrement + 1;
                CChartRulerFrameShape crfTop = new CChartRulerFrameShape (riZeroPointX,
                                                                          epsRulers,
                                                                          true,
                                                                          riZeroPointY,
                                                                          iTopEdge,
                                                                          iTickCount,
                                                                          riIncrement,
                                                                          TICK_LENGTH,
                                                                          TICK_LENGTH,
                                                                          0,
                                                                          false,
                                                                          true,
                                                                          false);
                ldesPolynomialChart.Add (crfTop);

                // Create the Vertical Bottom ruler
                iTickCount = (riZeroPointY - iBottomEdge) / riIncrement + 1;
                CChartRulerFrameShape crfBottom = new CChartRulerFrameShape (riZeroPointX,
                                                                             epsRulers,
                                                                             true,
                                                                             iBottomEdge,
                                                                             riZeroPointY,
                                                                             iTickCount,
                                                                             riIncrement,
                                                                             TICK_LENGTH,
                                                                             TICK_LENGTH,
                                                                             0,
                                                                             false,
                                                                             true,
                                                                             false);
                ldesPolynomialChart.Add (crfBottom);

                // Create the Horizontal Left ruler
                iTickCount = (riZeroPointX - iLeftEdge) / riIncrement + 1;
                CChartRulerFrameShape crfLeft = new CChartRulerFrameShape (riZeroPointY,
                                                                           epsRulers,
                                                                           false,
                                                                           iLeftEdge,
                                                                           riZeroPointX,
                                                                           iTickCount,
                                                                           riIncrement,
                                                                           TICK_LENGTH,
                                                                           TICK_LENGTH,
                                                                           0,
                                                                           false,
                                                                           true,
                                                                           false);
                ldesPolynomialChart.Add (crfLeft);

                // Create the Horizontal Right ruler
                iTickCount = (iRightEdge - riZeroPointX) / riIncrement + 1;
                CChartRulerFrameShape crfRight = new CChartRulerFrameShape (riZeroPointY,
                                                                            epsRulers,
                                                                            false,
                                                                            riZeroPointX,
                                                                            iRightEdge,
                                                                            iTickCount,
                                                                            riIncrement,
                                                                            TICK_LENGTH,
                                                                            TICK_LENGTH,
                                                                            0,
                                                                            false,
                                                                            true,
                                                                            false);
                ldesPolynomialChart.Add (crfRight);
            }

            // Create the grid
            if (epsGrid != EPenSelect.ESelectNoPen)
            {
                List<Point[]> lptaChartGrid = new List<Point[]> ();

                // Create top right grid (riZeroPointX -> iRightEdge, riZeroPointY -> iTopEdge)
                for (int iHorizontalPoint = riZeroPointX + riIncrement; iHorizontalPoint < (iRightEdge - iFrameEdgeBuffer); iHorizontalPoint += riIncrement)
                {
                    List<Point> lptGridBuffer = new List<Point> ();
                    lptGridBuffer.Add (new Point (iHorizontalPoint, riZeroPointY + TICK_LENGTH));
                    lptGridBuffer.Add (new Point (iHorizontalPoint, iTopEdge - FRAME_THICKNESS));
                    lptaChartGrid.Add (lptGridBuffer.ToArray ());
                }
                for (int iVerticalPoint = riZeroPointY + riIncrement; iVerticalPoint < (iTopEdge - iFrameEdgeBuffer); iVerticalPoint += riIncrement)
                {
                    List<Point> lptGridBuffer = new List<Point> ();
                    lptGridBuffer.Add (new Point (riZeroPointX + TICK_LENGTH, iVerticalPoint));
                    lptGridBuffer.Add (new Point (iRightEdge - FRAME_THICKNESS, iVerticalPoint));
                    lptaChartGrid.Add (lptGridBuffer.ToArray ());
                }

                // Create bottom right grid (riZeroPointX -> iRightEdge, iBottomEdge -> riZeroPointY)
                for (int iHorizontalPoint = riZeroPointX + riIncrement; iHorizontalPoint < (iRightEdge - iFrameEdgeBuffer); iHorizontalPoint += riIncrement)
                {
                    List<Point> lptGridBuffer = new List<Point> ();
                    lptGridBuffer.Add (new Point (iHorizontalPoint, riZeroPointY - TICK_LENGTH));
                    lptGridBuffer.Add (new Point (iHorizontalPoint, iBottomEdge + FRAME_THICKNESS));
                    lptaChartGrid.Add (lptGridBuffer.ToArray ());
                }
                for (int iVerticalPoint = iBottomEdge + riIncrement; iVerticalPoint < (riZeroPointY - iFrameEdgeBuffer); iVerticalPoint += riIncrement)
                {
                    List<Point> lptGridBuffer = new List<Point> ();
                    lptGridBuffer.Add (new Point (riZeroPointX + TICK_LENGTH, iVerticalPoint));
                    lptGridBuffer.Add (new Point (iRightEdge - FRAME_THICKNESS, iVerticalPoint));
                    lptaChartGrid.Add (lptGridBuffer.ToArray ());
                }

                // Create bottom left grid (iLeftEdge -> riZeroPointX, iBottomEdge -> riZeroPointY)
                for (int iHorizontalPoint = iLeftEdge + riIncrement; iHorizontalPoint < (riZeroPointX - iFrameEdgeBuffer); iHorizontalPoint += riIncrement)
                {
                    List<Point> lptGridBuffer = new List<Point> ();
                    lptGridBuffer.Add (new Point (iHorizontalPoint, riZeroPointY - TICK_LENGTH));
                    lptGridBuffer.Add (new Point (iHorizontalPoint, iBottomEdge + FRAME_THICKNESS));
                    lptaChartGrid.Add (lptGridBuffer.ToArray ());
                }
                for (int iVerticalPoint = iBottomEdge + riIncrement; iVerticalPoint < (riZeroPointY - iFrameEdgeBuffer); iVerticalPoint += riIncrement)
                {
                    List<Point> lptGridBuffer = new List<Point> ();
                    lptGridBuffer.Add (new Point (riZeroPointX - TICK_LENGTH, iVerticalPoint));
                    lptGridBuffer.Add (new Point (iLeftEdge + FRAME_THICKNESS, iVerticalPoint));
                    lptaChartGrid.Add (lptGridBuffer.ToArray ());
                }

                // Create top left grid (iLeftEdge -> riZeroPointX, riZeroPointY -> iTopEdge)
                for (int iHorizontalPoint = iLeftEdge + riIncrement; iHorizontalPoint < (riZeroPointX - iFrameEdgeBuffer); iHorizontalPoint += riIncrement)
                {
                    List<Point> lptGridBuffer = new List<Point> ();
                    lptGridBuffer.Add (new Point (iHorizontalPoint, riZeroPointY + TICK_LENGTH));
                    lptGridBuffer.Add (new Point (iHorizontalPoint, iTopEdge - FRAME_THICKNESS));
                    lptaChartGrid.Add (lptGridBuffer.ToArray ());
                }
                for (int iVerticalPoint = riZeroPointY + riIncrement; iVerticalPoint < (iTopEdge - iFrameEdgeBuffer); iVerticalPoint += riIncrement)
                {
                    List<Point> lptGridBuffer = new List<Point> ();
                    lptGridBuffer.Add (new Point (riZeroPointX - TICK_LENGTH, iVerticalPoint));
                    lptGridBuffer.Add (new Point (iLeftEdge + FRAME_THICKNESS, iVerticalPoint));
                    lptaChartGrid.Add (lptGridBuffer.ToArray ());
                }

                CComplexLinesShape clsGrid = new CComplexLinesShape (lptaChartGrid, epsGrid);
                ldesPolynomialChart.Add (clsGrid);
            }
                                                                      
            // Create a thick frame
            if (epsFrame != EPenSelect.ESelectNoPen)
            {
                CComplexLinesShape clsThickFrame = PlotThickFrame (iBottomEdge, iTopEdge, iLeftEdge, iRightEdge, FRAME_THICKNESS, epsFrame);
                ldesPolynomialChart.Add (clsThickFrame);
            }
                                                                      
            return ldesPolynomialChart.ToArray ();
        }

        private enum EPlotDirection
        {
            EUndetermined,
            EDownward,
            EUpward
        }

        public static Point[] PlotPolynomialFunction (double[] daFactors, double dLeftX, double dRightX, double dStep,
                                                      ref int riZeroPointX, ref int riZeroPointY, ref int riIncrement, 
                                                      ref Point ptLL, ref Point ptUR, double dPlotScale = 3000)
        {
            // "Modern Algebra and Trigonometry" p 519
            // https://www.shelovesmath.com/algebra/advanced-algebra/graphing-polynomials/

            #region Local data variables
            const double BOUNDARY_BUFFER_THICKNESS = 2.0;
            const double DEFAULT_TOP_FRAME_EDGE    = 6.0;
            const double DEFAULT_BOTTOM_FRAME_EDGE = -6.0;
            const double DEFAULT_LEFT_FRAME_EDGE   = -3.0;
            const double DEFAULT_RIGHT_FRAME_EDGE  = 3.0;

            riZeroPointX = -1;
            riZeroPointY = -1;
            riIncrement  = -1;

            List<PointF> lptPlotPointsFloat = new List<PointF> ();
            List<Point>  lptPlotPointsInt   = new List<Point> ();

            double dScaleX      = 1 / dStep;
            double dPrevX       = 0;
            double dPrevY       = 0;
            int   iFirstTurnIdx = -1;
            int   iLastTurnIdx  = -1;

            bool   bTopTurnFound = false;
            double dTopTurnX     = 0.0;
            double dTopTurnY     = 0.0;

            bool   bBottomTurnFound = false;
            double dBottomTurnX     = 0.0;
            double dBottomTurnY     = 0.0;

            bool   bTopFrameEdgeFound    = false;
            bool   bBottomFrameEdgeFound = false;
            double dTopFrameEdge            = DEFAULT_TOP_FRAME_EDGE;
            double dBottomFrameEdge         = DEFAULT_BOTTOM_FRAME_EDGE;
            double dLeftFrameEdge           = DEFAULT_LEFT_FRAME_EDGE;
            double dRightFrameEdge          = DEFAULT_RIGHT_FRAME_EDGE;
            double dTopPlottablePoint       = 0.0;
            double dBottomPlottablePoint    = 0.0;
            double dLeftMostPlottablePoint  = 0.0;
            double dRightMostPlottablePoint = 0.0;

            EPlotDirection ePlotDirection  = EPlotDirection.EUndetermined;
            EPlotDirection eStartDirection = EPlotDirection.EUndetermined;
            EPlotDirection eEndDirection   = EPlotDirection.EUndetermined;
            EPlotDirection ePrevDirection  = EPlotDirection.EUndetermined;
            #endregion

            #region First iteration pass
            for (double dCurrentX = dLeftX; dCurrentX <= dRightX; dCurrentX += dStep)
            {
                double dYNew = 0.0;
                for (int iIdx = 0; iIdx < daFactors.Count (); ++iIdx)
                {
                    double d1 = daFactors[daFactors.Count () - iIdx - 1];
                    double d2 = Math.Pow (dCurrentX, iIdx);
                    double d3 = d1 * d2;
                    dYNew += d3;
                }

                lptPlotPointsFloat.Add (new PointF ((float)dCurrentX, (float)dYNew));

                dTopPlottablePoint = Math.Max (dTopPlottablePoint, dYNew);
                dBottomPlottablePoint = Math.Min (dBottomPlottablePoint, dYNew);
                if (dYNew > DEFAULT_BOTTOM_FRAME_EDGE &&
                    dYNew < DEFAULT_TOP_FRAME_EDGE)
                {
                    dLeftMostPlottablePoint = Math.Min (dLeftMostPlottablePoint, dCurrentX);
                    dRightMostPlottablePoint = Math.Max (dRightMostPlottablePoint, dCurrentX);
                }

                if (dTopPlottablePoint > dTopFrameEdge)
                {
                    if (dTopFrameEdge < dTopTurnY)
                        dTopFrameEdge = Math.Truncate (dTopPlottablePoint + BOUNDARY_BUFFER_THICKNESS);
                }

                ePrevDirection = ePlotDirection;
                if (lptPlotPointsFloat.Count () > 1)
                {
                    ePlotDirection  = (dPrevY > dYNew) ? EPlotDirection.EDownward : EPlotDirection.EUpward;

                    if (lptPlotPointsFloat.Count () == 2)
                    {
                        eStartDirection = ePlotDirection;
                        ePrevDirection  = ePlotDirection;
                    }
                }

                //Console.Write (string.Format ("{0} [{1}]  X: {2}{3:0.0}   Y: {4}{5:0.00}", (ePlotDirection == EPlotDirection.EUpward ? "  Up" :
                //                                                                           (ePlotDirection == EPlotDirection.EDownward ? "Down" : "    ")),
                //                                                                           lptPlotPointsFloat.Count () - 1,
                //                                                                           (Math.Round (dCurrentX, 1) < 0.0 ? "" : " "), dCurrentX,
                //                                                                           (Math.Round (dYNew, 1) < 0.0 ? "" : " "), dYNew));

                if (ePrevDirection != ePlotDirection)
                {
                    iLastTurnIdx = lptPlotPointsFloat.Count () - 1;

                    if (ePlotDirection == EPlotDirection.EDownward)
                    {
                        if (!bTopTurnFound)
                        {
                            bTopTurnFound = true;
                            dTopTurnX = dPrevX;
                            dTopTurnY = dPrevY;
                            //Console.Write (string.Format ("   dTopTurnX: {0:0.00}", dTopTurnX));
                            //Console.Write (string.Format ("   dTopTurnY: {0:0.00}", dTopTurnY));

                            if (iFirstTurnIdx < 0)
                            {
                                iFirstTurnIdx = lptPlotPointsFloat.Count () - 1;
                                //Console.Write (string.Format ("   iFirstTurnIdx: {0}", iFirstTurnIdx));
                            }
                        }
                        else
                        {
                            if (dYNew > dTopTurnY)
                            {
                                dTopTurnX = dPrevX;
                                dTopTurnY = dPrevY;
                                //Console.Write (string.Format ("   dTopTurnX: {0:0.00}", dTopTurnX));
                                //Console.Write (string.Format ("   dTopTurnY: {0:0.00}", dTopTurnY));
                            }
                        }

                        if (!bTopFrameEdgeFound)
                        {
                            bTopFrameEdgeFound = true;
                            dTopFrameEdge = dPrevY;
                            //Console.Write (string.Format ("   dTopFrameEdge: {0:0.00}", dTopFrameEdge));
                        }
                        else
                        {
                            if (dYNew > dTopFrameEdge)
                            {
                                dTopFrameEdge = dPrevY;
                                //Console.Write (string.Format ("   dTopFrameEdge: {0:0.00}", dTopFrameEdge));
                            }
                        }
                    }
                    else // ePlotDirection == EPlotDirection.EUpWard
                    {
                        if (!bBottomTurnFound)
                        {
                            bBottomTurnFound = true;
                            dBottomTurnX     = dPrevX;
                            dBottomTurnY     = dPrevY;
                            //Console.Write (string.Format ("   dBottomTurnX: {0:0.00}", dBottomTurnX));
                            //Console.Write (string.Format ("   dBottomTurnY: {0:0.00}", dBottomTurnY));

                            if (iFirstTurnIdx < 0)
                            {
                                iFirstTurnIdx = lptPlotPointsFloat.Count () - 1;
                                //Console.Write (string.Format ("   iFirstTurnIdx: {0}", iFirstTurnIdx));
                            }
                        }
                        else
                        {
                            if (dYNew < dBottomTurnY)
                            {
                                dBottomTurnX = dPrevX;
                                dBottomTurnY = dPrevY;
                                //Console.Write (string.Format ("   dBottomTurnX: {0:0.00}", dBottomTurnX));
                                //Console.Write (string.Format ("   dBottomTurnY: {0:0.00}", dBottomTurnY));
                            }
                        }

                        if (!bBottomFrameEdgeFound)
                        {
                            bBottomFrameEdgeFound = true;
                            dBottomFrameEdge = dPrevY;
                            //Console.Write (string.Format ("   dBottomFrameEdge: {0:0.00}", dBottomFrameEdge));
                        }
                        else
                        {
                            if (dYNew < dBottomFrameEdge)
                            {
                                dBottomFrameEdge = dPrevY;
                                //Console.Write (string.Format ("   dBottomFrameEdge: {0:0.00}", dBottomFrameEdge));
                            }
                        }
                    }
                }

                //Console.WriteLine ();
                //Console.WriteLine (string.Format ("{0} [{1}]  X: {2}{3:0.0}   Y: {4}{5:0.00}", (ePlotDirection == EPlotDirection.EDownward ? "Down" : "  Up"),
                //                                                                                lptPlotPointsFloat.Count () - 1,
                //                                                                                (Math.Round (dPrevX, 1) < 0.0 ? "" : " "), dPrevX,
                //                                                                                (Math.Round (dYNew, 1) < 0.0 ? "" : " "), dYNew));

                dPrevX = dCurrentX;
                dPrevY = dYNew;
            }

            eEndDirection = ePlotDirection;
            #endregion

            #region Adjust top & bottom turn points
            if (bTopTurnFound)
            {
                if (dTopTurnY > 0.0)
                {
                    if (dTopPlottablePoint > dTopTurnY)
                    {
                        dTopFrameEdge = Math.Max (DEFAULT_TOP_FRAME_EDGE, Math.Truncate (dTopTurnY + BOUNDARY_BUFFER_THICKNESS));
                    }
                    else
                    {
                        dTopFrameEdge = Math.Truncate (dTopTurnY + BOUNDARY_BUFFER_THICKNESS);
                    }
                }
                else
                {
                    dTopFrameEdge = BOUNDARY_BUFFER_THICKNESS;
                }
            }
            else
            {
                dTopFrameEdge = (dTopPlottablePoint > 0.0) ? 6.0 : BOUNDARY_BUFFER_THICKNESS;
            }

            if (bBottomTurnFound)
            {
                if (dBottomTurnY < 0.0)
                {
                    if (dBottomPlottablePoint < dBottomTurnY)
                    {
                        dBottomFrameEdge = Math.Min (DEFAULT_BOTTOM_FRAME_EDGE, Math.Truncate (dBottomTurnY - BOUNDARY_BUFFER_THICKNESS));
                    }
                    else
                    {
                        dBottomFrameEdge = Math.Truncate (dBottomTurnY - BOUNDARY_BUFFER_THICKNESS);
                    }
                }
                else
                {
                    dBottomFrameEdge = -BOUNDARY_BUFFER_THICKNESS;
                }
            }
            else
            {
                dBottomFrameEdge = (dBottomPlottablePoint < 0.0) ? 6.0 : -BOUNDARY_BUFFER_THICKNESS;
            }

            //Console.WriteLine (string.Format ("   Rounded dTopFrameEdge: {0:0.00}", dTopFrameEdge));
            //Console.WriteLine (string.Format ("   Rounded dBottomFrameEdge: {0:0.00}", dBottomFrameEdge));
            #endregion

            #region Second iteration pass to determine left frame side edge value
            int    iLeftEdgeIdx                  = -1;
            PointF ptfFirstPointBeyondTurnLeft   = new PointF (0, 0);
            PointF ptfPrevPointOutsideFrameLeft  = new PointF (0, 0);
            PointF ptfFirstPointOutsideFrameLeft = new PointF (0, 0);
            for (int iIdxLeft = iFirstTurnIdx; iIdxLeft >= 0; --iIdxLeft)
            {
                PointF ptfThis = lptPlotPointsFloat[iIdxLeft];
                //Console.WriteLine (string.Format ("2nd iter: [{0}] X: {1}{2:0.0}   Y: {3}{4:0.00}", iIdxLeft, (Math.Round (ptfThis.X, 1) < 0.0 ? "" : " "), ptfThis.X,
                //                                                                                              (Math.Round (ptfThis.Y, 1) < 0.0 ? "" : " "), ptfThis.Y));

                if (eStartDirection == EPlotDirection.EDownward &&
                    ptfThis.Y >= dTopTurnY)
                {
                    if (dLeftFrameEdge == 0.0)
                    {
                        ptfFirstPointBeyondTurnLeft = ptfThis;
                        dLeftFrameEdge = Math.Min (dLeftFrameEdge, ptfThis.X - BOUNDARY_BUFFER_THICKNESS);
                        dLeftFrameEdge = Math.Truncate (dLeftFrameEdge);
                    }

                    ptfPrevPointOutsideFrameLeft  = ptfFirstPointOutsideFrameLeft;
                    ptfFirstPointOutsideFrameLeft = ptfThis;
                    iLeftEdgeIdx                  = iIdxLeft;

                    if (ptfThis.Y >= dTopFrameEdge)
                    {
                        break;
                    }
                }
                else if (eStartDirection == EPlotDirection.EUpward &&
                         ptfThis.Y <= dBottomTurnY)
                {
                    if (dLeftFrameEdge == 0.0)
                    {
                        ptfFirstPointBeyondTurnLeft = ptfThis;
                        dLeftFrameEdge = Math.Min (dLeftFrameEdge, ptfThis.X - BOUNDARY_BUFFER_THICKNESS);
                        dLeftFrameEdge = Math.Truncate (dLeftFrameEdge);
                    }

                    ptfPrevPointOutsideFrameLeft  = ptfFirstPointOutsideFrameLeft;
                    ptfFirstPointOutsideFrameLeft = ptfThis;
                    iLeftEdgeIdx                  = iIdxLeft;

                    if (ptfThis.Y <= dBottomFrameEdge)
                    {
                        break;
                    }
                }
            }

            //Console.WriteLine (string.Format ("   Rounded dLeftFrameEdge: {0:0.00}", dLeftFrameEdge));
            #endregion

            #region Third iteration pass to determine right frame side edge value
            int    iRightEdgeIdx                  = -1;
            PointF ptfFirstPointBeyondTurnRight   = new PointF (0, 0);
            PointF ptfPrevPointOutsideFrameRight  = new PointF (0, 0);
            PointF ptfFirstPointOutsideFrameRight = new PointF (0, 0);
            for (int iIdxRight = iLastTurnIdx; iIdxRight < lptPlotPointsFloat.Count; ++iIdxRight)
            {
                PointF ptfThis = lptPlotPointsFloat[iIdxRight];
                //Console.WriteLine (string.Format ("3rd iter: [{0}] X: {1}{2:0.0}   Y: {3}{4:0.00}", iIdxRight, (Math.Round (ptfThis.X, 1) < 0.0 ? "" : " "), ptfThis.X,
                //                                                                                               (Math.Round (ptfThis.Y, 1) < 0.0 ? "" : " "), ptfThis.Y));

                if (eEndDirection == EPlotDirection.EDownward &&
                    ptfThis.Y <= dBottomTurnY)
                {
                    if (dRightFrameEdge == 0.0)
                    {
                        ptfFirstPointBeyondTurnRight = ptfThis;
                        dRightFrameEdge = Math.Max (dRightFrameEdge, ptfThis.X + BOUNDARY_BUFFER_THICKNESS);
                        dRightFrameEdge = Math.Truncate (dRightFrameEdge);
                    }

                    ptfPrevPointOutsideFrameRight  = ptfFirstPointOutsideFrameRight;
                    ptfFirstPointOutsideFrameRight = ptfThis;
                    iRightEdgeIdx                  = iIdxRight;

                    if (ptfThis.Y <= dBottomFrameEdge)
                    {
                        if (ptfPrevPointOutsideFrameLeft.X == 0.0 &&
                            ptfPrevPointOutsideFrameLeft.Y == 0.0)
                        {
                            if (iLeftEdgeIdx > 0)
                            {
                                ptfPrevPointOutsideFrameLeft = lptPlotPointsFloat[iLeftEdgeIdx + 1];
                            }
                        }
                        break;
                    }
                }
                else if (eEndDirection == EPlotDirection.EUpward &&
                         ptfThis.Y >= dTopTurnY)
                {
                    if (dRightFrameEdge == 0.0)
                    {
                        ptfFirstPointBeyondTurnRight = ptfThis;
                        dRightFrameEdge = Math.Max (dRightFrameEdge, ptfThis.X + BOUNDARY_BUFFER_THICKNESS);
                        dRightFrameEdge = Math.Truncate (dRightFrameEdge);
                    }

                    ptfPrevPointOutsideFrameRight  = ptfFirstPointOutsideFrameRight;
                    ptfFirstPointOutsideFrameRight = ptfThis;
                    iRightEdgeIdx                  = iIdxRight;

                    if (ptfThis.Y >= dTopFrameEdge)
                    {
                        if (ptfPrevPointOutsideFrameRight.X == 0.0 &&
                            ptfPrevPointOutsideFrameRight.Y == 0.0)
                        {
                            if (iRightEdgeIdx > 0)
                            {
                                ptfPrevPointOutsideFrameRight = lptPlotPointsFloat[iRightEdgeIdx - 1];
                            }
                        }
                        break;
                    }
                }
            }

            //Console.WriteLine (string.Format ("   Rounded dRightFrameEdge: {0:0.00}", dRightFrameEdge));
            #endregion

            #region Adjust plot edges for optimal edge spacing
            dBottomFrameEdge = Math.Max (dBottomFrameEdge, Math.Truncate (dBottomPlottablePoint - BOUNDARY_BUFFER_THICKNESS));

            dTopFrameEdge = Math.Min (dTopFrameEdge, Math.Truncate (dTopPlottablePoint + BOUNDARY_BUFFER_THICKNESS));

            dLeftFrameEdge = Math.Max (dLeftFrameEdge, DEFAULT_LEFT_FRAME_EDGE);
            dLeftFrameEdge = Math.Min (dLeftFrameEdge, Math.Truncate (dLeftMostPlottablePoint - BOUNDARY_BUFFER_THICKNESS));

            dRightFrameEdge = Math.Min (dRightFrameEdge, DEFAULT_TOP_FRAME_EDGE);
            dRightFrameEdge = Math.Max (dRightFrameEdge, Math.Truncate (dRightMostPlottablePoint + BOUNDARY_BUFFER_THICKNESS));

            // Change the edge values of the plot
            // Replace the Y-value of the point at the indices of the first points found outside of the frame:
            // 1. Proportion the X-value with the ratio of the 2 Y-values to maintain the slope of the line
            // 2. Set the Y-value to the top or bottom edge
            if (iRightEdgeIdx >= 0)
            {
                if (ptfFirstPointOutsideFrameRight.Y < dBottomFrameEdge)
                {
                    double dDeltaBeyondFrame  = dBottomFrameEdge - ptfPrevPointOutsideFrameRight.Y;
                    double dDeltaBothPointsX = ptfFirstPointOutsideFrameRight.X - ptfPrevPointOutsideFrameRight.X;
                    double dDeltaBothPointsY = ptfFirstPointOutsideFrameRight.Y - ptfPrevPointOutsideFrameRight.Y;
                    double dRatioY  = Math.Abs (dDeltaBeyondFrame / dDeltaBothPointsY);
                    double dOffsetX = dDeltaBothPointsX * dRatioY;
                    double dNewX    = ptfPrevPointOutsideFrameRight.X + dOffsetX;
                    //Console.WriteLine (string.Format ("BR [{0:00}] X: {1:0.000} -> {2:0.000}  Y: {3:0.000} -> {4:0.000}",
                    //                                  iRightEdgeIdx, ptfPrevPointOutsideFrameRight.X, dNewX,
                    //                                                 ptfPrevPointOutsideFrameRight.Y, dBottomFrameEdge));
                    lptPlotPointsFloat[iRightEdgeIdx] = new PointF ((float)dNewX, (float)dBottomFrameEdge);
                }
                else if (ptfFirstPointOutsideFrameRight.Y > dTopFrameEdge)
                {
                    double dDeltaBeyondFrame  = ptfPrevPointOutsideFrameRight.Y - dTopFrameEdge;
                    double dDeltaBothPointsX = ptfFirstPointOutsideFrameRight.X - ptfPrevPointOutsideFrameRight.X;
                    double dDeltaBothPointsY = ptfFirstPointOutsideFrameRight.Y - ptfPrevPointOutsideFrameRight.Y;
                    double dRatioY  = Math.Abs (dDeltaBeyondFrame / dDeltaBothPointsY);
                    double dOffsetX = dDeltaBothPointsX * dRatioY;
                    double dNewX    = ptfPrevPointOutsideFrameRight.X + dOffsetX;
                    lptPlotPointsFloat[iRightEdgeIdx] = new PointF ((float)dNewX, (float)dTopFrameEdge);
                    //Console.WriteLine (string.Format ("TR [{0:00}] X: {1:0.000} -> {2:0.000}  Y: {3:0.000} -> {4:0.000}",
                    //                                  iRightEdgeIdx, ptfPrevPointOutsideFrameRight.X, dNewX,
                    //                                                 ptfPrevPointOutsideFrameRight.Y, dTopFrameEdge));
                }
            }

            if (iLeftEdgeIdx >= 0)
            {
                if (ptfFirstPointOutsideFrameLeft.Y < dBottomFrameEdge)
                {
                    double dDeltaBeyondFrame = dBottomFrameEdge - ptfPrevPointOutsideFrameLeft.Y;
                    double dDeltaBothPointsX = ptfFirstPointOutsideFrameLeft.X - ptfPrevPointOutsideFrameLeft.X;
                    double dDeltaBothPointsY = ptfFirstPointOutsideFrameLeft.Y - ptfPrevPointOutsideFrameLeft.Y;
                    double dRatioY  = Math.Abs (dDeltaBeyondFrame / dDeltaBothPointsY);
                    double dOffsetX = dDeltaBothPointsX * dRatioY;
                    double dNewX    = ptfPrevPointOutsideFrameLeft.X + dOffsetX;
                    lptPlotPointsFloat[iLeftEdgeIdx] = new PointF ((float)dNewX, (float)dBottomFrameEdge);
                    //Console.WriteLine (string.Format ("BL [{0:00}] X: {1:0.000} -> {2:0.000}  Y: {3:0.000} -> {4:0.000}",
                    //                                  iLeftEdgeIdx, ptfPrevPointOutsideFrameLeft.X, dNewX,
                    //                                                ptfPrevPointOutsideFrameLeft.Y, dBottomFrameEdge));
                }
                else if (ptfFirstPointOutsideFrameLeft.Y > dTopFrameEdge)
                {
                    double dDeltaBeyondFrame = ptfPrevPointOutsideFrameLeft.Y - dTopFrameEdge;
                    double dDeltaBothPointsX = ptfFirstPointOutsideFrameLeft.X - ptfPrevPointOutsideFrameLeft.X;
                    double dDeltaBothPointsY = ptfFirstPointOutsideFrameLeft.Y - ptfPrevPointOutsideFrameLeft.Y;
                    double dRatioY  = Math.Abs (dDeltaBeyondFrame / dDeltaBothPointsY);
                    double dOffsetX = dDeltaBothPointsX * dRatioY;
                    double dNewX    = ptfPrevPointOutsideFrameLeft.X + dOffsetX;
                    lptPlotPointsFloat[iLeftEdgeIdx] = new PointF ((float)dNewX, (float)dTopFrameEdge);
                    //Console.WriteLine (string.Format ("TL [{0:00}] X: {1:0.000} -> {2:0.000}  Y: {3:0.000} -> {4:0.000}",
                    //                                  iLeftEdgeIdx, ptfPrevPointOutsideFrameLeft.X, dNewX,
                    //                                                ptfPrevPointOutsideFrameLeft.Y, dTopFrameEdge));
                }
            }
            #endregion

            #region Create values for moving and scaling
            // The plot has been created as a series of PointF (floating-point) values with some negative
            // values that must be converted to non-negative integer plottable points for the plotter.
            // These values are needed for that conversion process.
            float fRangeX = (float)(dRightFrameEdge - dLeftFrameEdge);
            float fRangeY = (float)(dTopFrameEdge - dBottomFrameEdge);
            float fScaleX = (float)(dPlotScale / fRangeX);
            float fScaleY = (float)(dPlotScale / fRangeY);
            float fScale  = (float)((fScaleX < fScaleY) ? fScaleX : fScaleY);
            riIncrement   = (int)fScale;
            float fBiasX  = (float)(dLeftFrameEdge   < 0.0 ? (float)Math.Abs (dLeftFrameEdge)   : dLeftFrameEdge);
            float fBiasY  = (float)(dBottomFrameEdge < 0.0 ? (float)Math.Abs (dBottomFrameEdge) : dBottomFrameEdge);
            riZeroPointX  = (int)(fBiasX * fScale);
            riZeroPointY  = (int)(fBiasY * fScale);
            Point ptPlot  = new Point (0, 0);

            int iTopFrameEdge    = (int)((dTopFrameEdge    + fBiasY) * fScale);
            int iBottomFrameEdge = (int)((dBottomFrameEdge + fBiasY) * fScale);
            int iLeftFrameEdge   = (int)((dLeftFrameEdge   + fBiasX) * fScale);
            int iRightFrameEdge  = (int)((dRightFrameEdge  + fBiasX) * fScale);
            ptLL.X = iLeftFrameEdge;
            ptLL.Y = iBottomFrameEdge;
            ptUR.X = iRightFrameEdge;
            ptUR.Y = iTopFrameEdge;
            #endregion

            #region Iterate through all the PointF values and convert them to plottable Point values
            //Console.WriteLine ("Begin conversion to integers for plotter ...");
            for (int iIdx2 = 0; iIdx2 < lptPlotPointsFloat.Count; ++iIdx2)
            {
                PointF ptfCalc = lptPlotPointsFloat[iIdx2];
                //Console.Write (string.Format ("PointF X: [{0:00}] {1}{2:0.0000}  Y:{3}{4:0000.00}", iIdx2, (ptfCalc.X > -0.1 ? " " : ""), ptfCalc.X,
                //                                                                                           (ptfCalc.Y >  0.0 ? " " : ""), ptfCalc.Y));

                if (ptfCalc.X >= (float)dLeftFrameEdge   &&
                    ptfCalc.X <= (float)dRightFrameEdge  &&
                    ptfCalc.Y >= (float)dBottomFrameEdge &&
                    ptfCalc.Y <= (float)dTopFrameEdge)
                {
                    // Is within the frame edge values ?
                    ptPlot.X = (int)((ptfCalc.X + fBiasX) * fScale);
                    ptPlot.Y = (int)((ptfCalc.Y + fBiasY) * fScale);
                    //Console.Write (string.Format (" --> [{0:00}] X: {1:0000}  Y: {2:0000}", lptPlotPointsInt.Count, ptPlot.X, ptPlot.Y));
                    lptPlotPointsInt.Add (new Point (ptPlot.X, ptPlot.Y));
                }
                else
                {
                    //Console.Write ("  outside frame");
                }

                //Console.WriteLine ();
            }
            #endregion

            return lptPlotPointsInt.ToArray ();
        }

        public static List<Point[]> PlotSteppedLines (Point ptLine1Start, Point ptLine1End, Point ptLine2Start, Point ptLine2End,
                                                      int iStepCount, bool bDrawGuideLines = false)
        {
            return PlotSteppedLines (ptLine1Start.X, ptLine1Start.Y, ptLine1End.X, ptLine1End.Y,
                                     ptLine2Start.X, ptLine2Start.Y, ptLine2End.X, ptLine2End.Y,
                                     iStepCount, bDrawGuideLines);
        }

        public static List<Point[]> PlotSteppedLines (int iLine1StartX, int iLine1StartY, int iLine1EndX, int iLine1EndY,
                                                      int iLine2StartX, int iLine2StartY, int iLine2EndX, int iLine2EndY,
                                                      int iStepCount, bool bDrawGuideLines = false, bool bSkipFirstLine = false)
        {
            List<Point[]> lptaSteppedLines = new List<Point[]> ();
            List<Point> lptLinePoints = new List<Point> ();

            int iCurrentStep = 0;

            // Limit coordinates to 0 through max for page
            iLine1StartX = CHPGL.ValidateX (iLine1StartX);
            iLine1EndX   = CHPGL.ValidateX (iLine1EndX);
            iLine2StartX = CHPGL.ValidateX (iLine2StartX);
            iLine2EndX   = CHPGL.ValidateX (iLine2EndX);

            iLine1StartY = CHPGL.ValidateY (iLine1StartY);
            iLine1EndY   = CHPGL.ValidateY (iLine1EndY);
            iLine2StartY = CHPGL.ValidateY (iLine2StartY);
            iLine2EndY   = CHPGL.ValidateY (iLine2EndY);

            // Only where the start point of one = end point of the other, for crossing connecting lines
            bool bGuideLinesJoined = (iLine1StartX == iLine2EndX   && iLine1StartY == iLine2EndY) ||
                                     (iLine1EndX   == iLine2StartX && iLine1EndY   == iLine2StartY);

            // Determine upper limit on # staps
            int iDistanceX1 = iLine1EndX - iLine1StartX;
            int iDistanceX2 = iLine2EndX - iLine2StartX;
            int iDistanceY1 = iLine1EndY - iLine1StartY;
            int iDistanceY2 = iLine2EndY - iLine2StartY;

            int iStepLimit1 = (Math.Abs (iDistanceX1) > Math.Abs (iDistanceY1)) ? Math.Abs (iDistanceX1) : Math.Abs (iDistanceY1);
            int iStepLimit2 = (Math.Abs (iDistanceX2) > Math.Abs (iDistanceY2)) ? Math.Abs (iDistanceX2) : Math.Abs (iDistanceY2);
            int iStepLimit  = (iStepLimit1 > iStepLimit2) ? iStepLimit1 : iStepLimit2;

            // Limit iSteps to 2 througn ?
            if (iStepCount < 2)
                iStepCount = 2;
            else if (iStepCount > iStepLimit)
                iStepCount = iStepLimit;

            if (bGuideLinesJoined)
            #region Joined Guide Lines
            {
                int iJoinedPointX = 0;
                int iJoinedPointY = 0;

                int iEndPoint1X = 0;
                int iEndPoint1Y = 0;
                int iEndPoint2X = 0;
                int iEndPoint2Y = 0;

                if (iLine1StartX == iLine2EndX && iLine1StartY == iLine2EndY)
                {
                    iJoinedPointX = iLine1StartX;
                    iJoinedPointY = iLine1StartY;

                    iEndPoint1X = iLine1EndX;
                    iEndPoint1Y = iLine1EndY;
                    iEndPoint2X = iLine2StartX;
                    iEndPoint2Y = iLine2StartY;
                }
                else if (iLine1EndX == iLine2StartX && iLine1EndY == iLine2StartY)
                {
                    iJoinedPointX = iLine2StartX;
                    iJoinedPointY = iLine2StartY;

                    iEndPoint1X = iLine1StartX;
                    iEndPoint1Y = iLine1StartY;
                    iEndPoint2X = iLine2EndX;
                    iEndPoint2Y = iLine2EndY;
                }

                // Compute delta for each line end point
                float fDeltaX1 = ((float)iEndPoint1X - (float)iJoinedPointX) / (float)iStepCount;
                float fDeltaY1 = ((float)iEndPoint1Y - (float)iJoinedPointY) / (float)iStepCount;
                float fDeltaX2 = ((float)iJoinedPointX - (float)iEndPoint2X) / (float)iStepCount;
                float fDeltaY2 = ((float)iJoinedPointY - (float)iEndPoint2Y) / (float)iStepCount;

                if (bDrawGuideLines)
                {
                    lptLinePoints.Add (new Point (iEndPoint1X, iEndPoint1Y));
                    lptLinePoints.Add (new Point (iJoinedPointX, iJoinedPointY));
                    lptLinePoints.Add (new Point (iEndPoint2X, iEndPoint2Y));
                    lptaSteppedLines.Add (lptLinePoints.ToArray ());
                    lptLinePoints.Clear ();
                }

                for (int iLineStep = 0; iLineStep < iStepCount; ++iLineStep)
                {
                    if (bSkipFirstLine)
                    {
                        bSkipFirstLine = false;
                        continue;
                    }

                    // From joined point out to endpoint 1
                    int iPoint1X = (int)((float)iJoinedPointX + (fDeltaX1 * (iLineStep + 1)));
                    int iPoint1Y = (int)((float)iJoinedPointY + (fDeltaY1 * (iLineStep + 1)));

                    // From endpoint 2 in to joined point
                    int iPoint2X = (int)((float)iEndPoint2X + (fDeltaX2 * iLineStep));
                    int iPoint2Y = (int)((float)iEndPoint2Y + (fDeltaY2 * iLineStep));

                    if (iLineStep % 2 > 0)
                    {
                        lptLinePoints.Add (new Point (iPoint1X, iPoint1Y));
                        lptLinePoints.Add (new Point (iPoint2X, iPoint2Y));
                        lptaSteppedLines.Add (lptLinePoints.ToArray ());
                        lptLinePoints.Clear ();
                    }
                    else
                    {
                        lptLinePoints.Add (new Point (iPoint2X, iPoint2Y));
                        lptLinePoints.Add (new Point (iPoint1X, iPoint1Y));
                        lptaSteppedLines.Add (lptLinePoints.ToArray ());
                        lptLinePoints.Clear ();
                    }
                }
            }
            #endregion
            else
            {
                // Compute delta for each line end point
                float fDeltaX1 = iDistanceX1 / (iStepCount - 1);
                float fDeltaX2 = iDistanceX2 / (iStepCount - 1);
                float fDeltaY1 = iDistanceY1 / (iStepCount - 1);
                float fDeltaY2 = iDistanceY2 / (iStepCount - 1);
                int   iLastX   = -1;
                int   iLastY   = -1;

                if (bDrawGuideLines)
                {
                    lptLinePoints.Add (new Point (iLine1EndX, iLine1EndY));
                    lptLinePoints.Add (new Point (iLine1StartX, iLine1StartY));
                    lptLinePoints.Add (new Point (iLine2StartX, iLine2StartY));
                    lptLinePoints.Add (new Point (iLine2EndX, iLine2EndY));
                    lptLinePoints.Add (new Point (iLine1EndX, iLine1EndY));
                    lptaSteppedLines.Add (lptLinePoints.ToArray ());
                    lptLinePoints.Clear ();

                    ++iCurrentStep;
                    --iStepCount;
                }

                for (int iStep = iCurrentStep; iStep < iStepCount; ++iStep)
                {
                    if (bSkipFirstLine)
                    {
                        bSkipFirstLine = false;
                        continue;
                    }

                    int iStartX = (int)((float)iLine1EndX - (fDeltaX1 * iStep));
                    int iStartY = (int)((float)iLine1EndY - (fDeltaY1 * iStep));
                    int iEndX   = (int)((float)iLine2EndX - (fDeltaX2 * iStep));
                    int iEndY   = (int)((float)iLine2EndY - (fDeltaY2 * iStep));
                    //Console.WriteLine (string.Format ("CurrentStep: {0}, StepCount: {1}", iStep, iStepCount));
                    //Console.WriteLine (string.Format ("From {0} x {1} -> {2} x {3}", iStartX, iStartY, iEndX, iEndY));

                    // Draw next connecting lines
                    if (Math.Abs (iStartX - iEndX) > 1 || Math.Abs (iStartY - iEndY) > 1)
                    {
                        //Console.WriteLine (string.Format ("{0} != {1} || {2} != {3}", iStartX, iEndX, iStartY, iEndY));
                        if (iStep % 2 > 0)
                        {
                            if (iLastX != iStartX ||
                                iLastY != iStartY)
                            {
                                if (lptLinePoints.Count > 0)
                                {
                                    lptaSteppedLines.Add (lptLinePoints.ToArray ());
                                    lptLinePoints.Clear ();
                                }
                            }

                            lptLinePoints.Add (new Point (iStartX, iStartY));
                            lptLinePoints.Add (new Point (iEndX, iEndY));

                            iLastX = iEndX;
                            iLastY = iEndY;
                        }
                        else
                        {
                            if (iLastX != iEndX ||
                                iLastY != iEndY)
                            {
                                if (lptLinePoints.Count > 0)
                                {
                                    lptaSteppedLines.Add (lptLinePoints.ToArray ());
                                    lptLinePoints.Clear ();
                                }
                            }

                            lptLinePoints.Add (new Point (iEndX, iEndY));
                            lptLinePoints.Add (new Point (iStartX, iStartY));
                            lptaSteppedLines.Add (lptLinePoints.ToArray ());
                            lptLinePoints.Clear ();

                            iLastX = iStartX;
                            iLastY = iStartY;
                        }
                    }
                }

                if (lptLinePoints.Count > 0)
                {
                    lptaSteppedLines.Add (lptLinePoints.ToArray ());
                    lptLinePoints.Clear ();
                }
            }

            return lptaSteppedLines;
        }

        public static CComplexLinesShape[] PlotFourQuadrants (int iBottomLeftX, int iBottomLeftY, int iTopRightX, int iTopRightY, int iStepCount,
                                                              EPenSelect eOuterCirclePen, bool bDrawOuterCircleGuideLines,
                                                              EPenSelect eInnerSpikePen, bool bDrawInnerSpikeGuideLines,
                                                              EPenSelect eOuterSpikePen, bool bDrawOuterSpikeGuideLines)
        {
            List<CComplexLinesShape> lclsFourQuadrants = new List<CComplexLinesShape> ();

            // If either iTopRightX or iTopRightY are negative, their absolute values
            // are used to determine height and width instead of plot coordinates
            if (iTopRightX < 0)
            {
                iTopRightX = iTopRightX * -1;
                iTopRightX += iBottomLeftX;
            }

            if (iTopRightY < 0)
            {
                iTopRightY = iTopRightY * -1;
                iTopRightY += iBottomLeftY;
            }

            if (iBottomLeftX < iTopRightX &&
                iBottomLeftY < iTopRightY &&
                (eOuterCirclePen != EPenSelect.ESelectNoPen ||
                 eInnerSpikePen  != EPenSelect.ESelectNoPen ||
                 eOuterSpikePen  != EPenSelect.ESelectNoPen))
            {
                int iTop          = iTopRightY;
                int iBottom       = iBottomLeftY;
                int iLeft         = iBottomLeftX;
                int iRight        = iTopRightX;
                int iWidth        = (iTopRightX - iBottomLeftX);
                int iHeight       = (iTopRightY - iBottomLeftY);
                int iCenterWidth  = iBottomLeftX + (iWidth / 2);
                int iCenterHeight = iBottomLeftY + (iHeight / 2);

                Point ptTopLeft      = new Point (iLeft, iTop);
                Point ptTopCenter    = new Point (iCenterWidth, iTop);
                Point ptTopRight     = new Point (iRight, iTop);
                Point ptLeftCenter   = new Point (iLeft, iCenterHeight);
                Point ptCenterXY     = new Point (iCenterWidth, iCenterHeight);
                Point ptRightCenter  = new Point (iRight, iCenterHeight);
                Point ptBottomLeft   = new Point (iLeft, iBottom);
                Point ptBottomCenter = new Point (iCenterWidth, iBottom);
                Point ptBottomRight  = new Point (iRight, iBottom);

                if (eOuterCirclePen != EPenSelect.ESelectNoPen)
                {
                    // Draw outer circle 1
                    EPenSelect ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer circle 1: " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsCircle1 = new CComplexLinesShape (PlotSteppedLines (ptTopCenter, ptTopLeft, ptTopLeft, ptLeftCenter,
                                                                            iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsCircle1);

                    // Draw outer circle 2
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer circle 2: " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsCircle2 = new CComplexLinesShape (PlotSteppedLines (ptTopCenter, ptTopRight, ptTopRight, ptRightCenter,
                                                                            iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsCircle2);

                    // Draw outer circle 3
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer circle 3: " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsCircle3 = new CComplexLinesShape (PlotSteppedLines (ptRightCenter, ptBottomRight, ptBottomRight, ptBottomCenter,
                                                                            iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsCircle3);

                    // Draw outer circle 4
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer circle 4: " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsCircle4 = new CComplexLinesShape (PlotSteppedLines (ptBottomCenter, ptBottomLeft, ptBottomLeft, ptLeftCenter,
                                                                            iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsCircle4);
                }

                if (eInnerSpikePen != EPenSelect.ESelectNoPen)
                {
                    // Draw inner spike 1
                    EPenSelect ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw inner spike 1: " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsInnerSpike1 = new CComplexLinesShape (PlotSteppedLines (ptLeftCenter, ptCenterXY, ptCenterXY, ptTopCenter,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsInnerSpike1);

                    // Draw inner spike 2
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw inner spike 2: " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsInnerSpike2 = new CComplexLinesShape (PlotSteppedLines (ptTopCenter, ptCenterXY, ptCenterXY, ptRightCenter,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsInnerSpike2);

                    // Draw inner spike 3
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw inner spike 3: " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsInnerSpike3 = new CComplexLinesShape (PlotSteppedLines (ptRightCenter, ptCenterXY, ptCenterXY, ptBottomCenter,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsInnerSpike3);

                    // Draw inner spike 4
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw inner spike 4: " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsInnerSpike4 = new CComplexLinesShape (PlotSteppedLines (ptBottomCenter, ptCenterXY, ptCenterXY, ptLeftCenter,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsInnerSpike4);
                }

                if (eOuterSpikePen != EPenSelect.ESelectNoPen)
                {
                    // Draw outer spike 1 (top left)
                    EPenSelect ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer spike 1 (top left): " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsOuterSpike1 = new CComplexLinesShape (PlotSteppedLines (ptTopLeft, ptTopCenter, ptTopCenter, ptCenterXY,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsOuterSpike1);

                    // Draw outer spike 2 (top right)
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer spike 2 (top right): " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsOuterSpike2 = new CComplexLinesShape (PlotSteppedLines (ptCenterXY, ptTopCenter, ptTopCenter, ptTopRight,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsOuterSpike2);

                    // Draw outer spike 3 (right center top)
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer spike 3 (right center top): " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsOuterSpike3 = new CComplexLinesShape (PlotSteppedLines (ptTopRight, ptRightCenter, ptRightCenter, ptCenterXY,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsOuterSpike3);

                    // Draw outer spike 4 (right center bottom)
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer spike 4 (right center bottom): " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsOuterSpike4 = new CComplexLinesShape (PlotSteppedLines (ptCenterXY, ptRightCenter, ptRightCenter, ptBottomRight,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsOuterSpike4);

                    // Draw outer spike 5 (bottom right)
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer spike 5 (bottom right): " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsOuterSpike5 = new CComplexLinesShape (PlotSteppedLines (ptBottomRight, ptBottomCenter, ptBottomCenter, ptCenterXY,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsOuterSpike5);

                    // Draw outer spike 6 (bottom left)
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer spike 6 (bottom left): " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsOuterSpike6 = new CComplexLinesShape (PlotSteppedLines (ptCenterXY, ptBottomCenter, ptBottomCenter, ptBottomLeft,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsOuterSpike6);

                    // Draw outer spike 7 (left center bottom)
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer spike 7 (left center bottom): " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsOuterSpike7 = new CComplexLinesShape (PlotSteppedLines (ptBottomLeft, ptLeftCenter, ptLeftCenter, ptCenterXY,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsOuterSpike7);

                    // Draw outer spike 8 (left center top)
                    ePenSelect = CPlotterEngine.GetNextPen (eOuterCirclePen);
                    //Console.WriteLine ("Draw outer spike 8 (left center top): " + ((int)ePenSelect).ToString ());
                    CComplexLinesShape clsOuterSpike8 = new CComplexLinesShape (PlotSteppedLines (ptCenterXY, ptLeftCenter, ptLeftCenter, ptTopLeft,
                                                                                iStepCount, bDrawOuterCircleGuideLines), ePenSelect, true);
                    lclsFourQuadrants.Add (clsOuterSpike8);
                }
            }

            return lclsFourQuadrants.ToArray ();
        }

        public static CComplexLinesShape[] PlotStackedSineWaves (int iBottomLeftX, int iBottomLeftY, int iTopRightX, int iTopRightY, int iWaveCount,
                                                                 int iWaveOverlap = 0, int iFrameThickness = 0, int iMinAmplitude = 0, int iMaxAmplitude = 0,
                                                                 bool bInvertAlternateWaves = false)
        {
            List<CComplexLinesShape> lclsSineWaves = new List<CComplexLinesShape> ();

            return lclsSineWaves.ToArray ();
        }

        public static CComplexLinesShape PlotThickFrame (int iBottom, int iTop, int iLeft, int iRight, int iThickness = 5, EPenSelect ePenSelection = EPenSelect.ESelectPen1)
        {
            List<Point[]> lptaFrame = new List<Point[]> ();
            List<Point> lptPointSequence = new List<Point> ();

            if (CHPGL.AreBoxCoordinatesValid (iBottom, iTop, iLeft, iRight) &&
                iThickness > 0 &&
                iTop - iBottom > iThickness &&
                iRight - iLeft > iThickness &&
                ePenSelection != EPenSelect.ESelectNoPen)
            {
                int iDelta = 7;

                while (iThickness-- > 0)
                {
                    lptPointSequence.Add (new Point (iLeft, iBottom));
                    lptPointSequence.Add (new Point (iLeft, iTop));
                    lptPointSequence.Add (new Point (iRight, iTop));
                    lptPointSequence.Add (new Point (iRight, iBottom));
                    lptPointSequence.Add (new Point (iLeft, iBottom));

                    iBottom += iDelta;
                    iTop    -= iDelta;
                    iLeft   += iDelta;
                    iRight  -= iDelta;
                }

                lptaFrame.Add (lptPointSequence.ToArray ());
                lptPointSequence.Clear ();
            }

            ePenSelection = CPlotterEngine.GetNextPen (ePenSelection);
            return new CComplexLinesShape (lptaFrame, ePenSelection, true);
        }

        public static CComplexLinesShape[] PlotRadialLines (int iBottom, int iTop, int iLeft, int iRight, int iStepCount, int iThickness, EPenSelect ePenSelection)
        {
            List<CComplexLinesShape> lclsRadialLines = new List<CComplexLinesShape> ();

            if (CHPGL.AreBoxCoordinatesValid (iBottom, iTop, iLeft, iRight))
            {
                int iWidth        = iRight - iLeft;
                int iHeight       = iTop - iBottom;
                int iCenterWidth  = iWidth / 2;
                int iCenterHeight = iHeight / 2;

                if (iThickness > 0)
                {
                    ePenSelection = CPlotterEngine.GetNextPen (ePenSelection);
                    lclsRadialLines.Add (PlotThickFrame (iBottom, iTop, iLeft, iRight, iThickness, ePenSelection));
                }

                ePenSelection = CPlotterEngine.GetNextPen (ePenSelection);
                lclsRadialLines.Add (new CComplexLinesShape (PlotSteppedLines (iLeft, iTop, iRight, iTop, iCenterWidth, iCenterHeight, iCenterWidth, iCenterHeight,
                                                             iStepCount, false, false), ePenSelection, true));

                ePenSelection = CPlotterEngine.GetNextPen (ePenSelection);
                lclsRadialLines.Add (new CComplexLinesShape (PlotSteppedLines (iLeft, iBottom, iLeft, iTop, iCenterWidth, iCenterHeight, iCenterWidth, iCenterHeight,
                                                             iStepCount, false, true), ePenSelection, true));

                ePenSelection = CPlotterEngine.GetNextPen (ePenSelection);
                lclsRadialLines.Add (new CComplexLinesShape (PlotSteppedLines (iLeft, iBottom, iRight, iBottom, iCenterWidth, iCenterHeight, iCenterWidth, iCenterHeight,
                                                             iStepCount, false, true), ePenSelection, true));

                ePenSelection = CPlotterEngine.GetNextPen (ePenSelection);
                lclsRadialLines.Add (new CComplexLinesShape (PlotSteppedLines (iRight, iBottom, iRight, iTop, iCenterWidth, iCenterHeight, iCenterWidth, iCenterHeight,
                                                             iStepCount, false, true), ePenSelection, true));
            }

            return lclsRadialLines.ToArray ();
        }

        public static CComplexLinesShape[] PlotTriangle (int iPoint1X, int iPoint1Y, int iPoint2X, int iPoint2Y, int iPoint3X, int iPoint3Y,
                                                         int iStepCount, EPenSelect ePenSelection = EPenSelect.ESelectPen1)
        {
            List<CComplexLinesShape> lclsTriangle = new List<CComplexLinesShape> ();

            if (iPoint1X  <= CHPGL.MAX_X_VALUE                 &&
                iPoint1Y  <= CHPGL.MAX_Y_VALUE                 &&
                iPoint2X  <= CHPGL.MAX_X_VALUE                 &&
                iPoint2Y  <= CHPGL.MAX_Y_VALUE                 &&
                iPoint3X  <= CHPGL.MAX_X_VALUE                 &&
                iPoint3Y  <= CHPGL.MAX_Y_VALUE                 &&
                (iPoint1X != iPoint2X || iPoint1Y != iPoint2Y) &&
                (iPoint1X != iPoint3X || iPoint1Y != iPoint3Y))
            {
                EPenSelect ePenSelect1 = EPenSelect.ESelectPen1;
                EPenSelect ePenSelect2 = EPenSelect.ESelectPen1;
                EPenSelect ePenSelect3 = EPenSelect.ESelectPen1;

                if (ePenSelection == EPenSelect.ESelectAllPens)
                {
                    ePenSelect1 = CPlotterEngine.GetNextPen (EPenSelect.ESelectPen1);
                    ePenSelect2 = CPlotterEngine.GetNextPen (EPenSelect.ESelectPen2);
                    ePenSelect3 = CPlotterEngine.GetNextPen (EPenSelect.ESelectPen3);
                }
                else
                {
                    ePenSelect1 = CPlotterEngine.GetNextPen (ePenSelect1);
                    ePenSelect2 = CPlotterEngine.GetNextPen (ePenSelect2);
                    ePenSelect3 = CPlotterEngine.GetNextPen (ePenSelect3);
                }

                lclsTriangle.Add (new CComplexLinesShape (PlotSteppedLines (iPoint1X, iPoint1Y, iPoint2X, iPoint2Y, iPoint2X, iPoint2Y, iPoint3X, iPoint3Y, iStepCount),
                                                          ePenSelect1, true));

                lclsTriangle.Add (new CComplexLinesShape (PlotSteppedLines (iPoint2X, iPoint2Y, iPoint3X, iPoint3Y, iPoint3X, iPoint3Y, iPoint1X, iPoint1Y, iStepCount),
                                                          ePenSelect2, true));

                lclsTriangle.Add (new CComplexLinesShape (PlotSteppedLines (iPoint3X, iPoint3Y, iPoint1X, iPoint1Y, iPoint1X, iPoint1Y, iPoint2X, iPoint2Y, iStepCount),
                                                          ePenSelect3, true));
            }

            return lclsTriangle.ToArray ();
        }
    }
}
