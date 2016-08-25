using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using WrapperBaseClass;
using ParallelPortWriter;
using SerialPortWriter;
using PlotterBuffer;
using HPGL;

namespace PlotterTester
{
    public class CPlotterTester
    {
        public const int CIRCLE_TEST = 1;  // Test CPlotterBuffer sorting and printing
        public const int STRESS_TEST = 2;  // Test CPlotterBuffer w/more data than fits in plotter 1024-byte buffer
        public const int ESC_TEST = 3;  // Test HPGL ESC commands
        public const int HPGL_TEST_1 = 4;  // Test annotated HPGL Query instructions
        public const int HPGL_TEST_2 = 5;  // Test raw HPGL Query instructions
        public const int HPGL_TEST_3 = 6;  // Test Boundaries and Units instructions
        public const int HPGL_TEST_4 = 7;  // Test Pen and Plotting instructions 1
        public const int HPGL_TEST_5 = 8;  // Test Pen and Plotting instructions 2
        public const int HPGL_TEST_6 = 9;  // Test Pen and Plotting instructions 3
        public const int HPGL_TEST_7 = 10; // Test Plot Enhancing instructions
        public const int HPGL_TEST_8 = 11; // Test Labeling instructions 1
        public const int HPGL_TEST_9 = 12; // Test Labeling instructions 2
        public const int HPGL_TEST_10 = 13; // Test Digitizing instructions
        public const int TEST_PROGRAMS_1 = 14; // Test programs in BASIC
        public const int TEST_PROGRAMS_2 = 15; // Test programs in HPGL 1
        public const int TEST_PROGRAMS_3 = 16; // Test programs in HPGL 2
        const int DEFAULT_VALUE = -1;
        const char LF = '\x0A';
        const char CR = '\x0D';
        const char ETX = '\x03';

        public void TestPlotter (bool bSerial, int iTestMode)
        {
            try
            {
                if (ESC_TEST == iTestMode)
                #region Test HPGL ESC commands
                {
                    if (bSerial)
                    {
                        WrapperBase wb = new SerialWrapper ();

                        wb.WriteTextString ("asdf"); // General HPGL error
                        wb.WriteTextString (CHPGL.EscReset ()); // Fix it?
                        wb.WriteTextString (CHPGL.Initialize ()); // Fix it?
                        Console.WriteLine (wb.GetPlotterBufferSize ());
                        Console.WriteLine (wb.GetPlotterBufferSpace ());

                        string strTest = CHPGL.EscOutputBufferSpace ();
                        Console.WriteLine (strTest);
                        Console.WriteLine (wb.QueryPlotter (strTest));
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscOutputExtendedError ();
                        Console.WriteLine (strTest);
                        Console.WriteLine (wb.QueryPlotter (strTest));
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscOutputBufferSize ();
                        Console.WriteLine (strTest);
                        Console.WriteLine (wb.QueryPlotter (strTest));
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscOutputExtendedStatus ();
                        Console.WriteLine (strTest);
                        Console.WriteLine (wb.QueryPlotter (strTest));
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscReset ();
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscPlotterConfiguration (500);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscPlotterConfiguration (DEFAULT_VALUE, DEFAULT_VALUE);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscPlotterConfiguration (400, DEFAULT_VALUE);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscPlotterConfiguration (DEFAULT_VALUE, 14);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscPlotterConfiguration (300, 13);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscPlotterConfiguration ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscSetHandshakeMode1 ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscSetHandshakeMode2 ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscAbortDeviceControl ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscAbortGraphicControl ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscSetOutputMode ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.SetExtendedOutputAndHandshakeMode ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());

                        strTest = CHPGL.EscReset ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryExtendedErrorText ());
                    }
                }
                #endregion
                else if (HPGL_TEST_1 == iTestMode)
                #region Test annotated HPGL Query instructions
                {
                    if (bSerial)
                    {
                        WrapperBase wb = new SerialWrapper ();

                        Console.WriteLine (wb.QueryIdentification ());
                        Console.WriteLine (wb.QueryStatus ());
                        Console.WriteLine (wb.QueryStatusText ());
                        Console.WriteLine (wb.QueryFactors ());
                        Console.WriteLine (wb.QueryFactorsText ());
                        Console.WriteLine (wb.QueryError ());
                        Console.WriteLine (wb.QueryErrorText ());
                        Console.WriteLine (wb.QueryActualPosition ());
                        Console.WriteLine (wb.QueryActualPositionText ());
                        Console.WriteLine (wb.QueryCommandedPosition ());
                        Console.WriteLine (wb.QueryCommandedPositionText ());
                        Console.WriteLine (wb.QueryOptions ());
                        Console.WriteLine (wb.QueryOptionsText ());
                        Console.WriteLine (wb.QueryHardClipLimits ());
                        Console.WriteLine (wb.QueryHardClipLimitsText ());
                    }
                }
                #endregion
                else if (HPGL_TEST_2 == iTestMode)
                #region Test raw HPGL Query instructions
                {
                    if (bSerial)
                    {
                        WrapperBase wb = new SerialWrapper ();

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
                }
                #endregion
                else if (HPGL_TEST_3 == iTestMode)
                #region Test Boundaries and Units instructions
                {
                    if (bSerial)
                    {
                        WrapperBase wb = new SerialWrapper ();

                        // Test SetDefaultValues ()
                        string strTest = CHPGL.SetDefaultValues ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);

                        // Test Initialize ()
                        strTest = CHPGL.Initialize ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);

                        // Test InputMask ()
                        strTest = CHPGL.InputMask (-1);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.InputMask (300);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.InputMask (125);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);

                        strTest = CHPGL.InputMask ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);

                        // Test PaperSize ()
                        strTest = CHPGL.PaperSize (-1);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.PaperSize (128);
                        Console.WriteLine (strTest);

                        Console.WriteLine (wb.QueryHardClipLimitsText ());
                        strTest = CHPGL.PaperSize (0);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryHardClipLimitsText ());

                        strTest = CHPGL.PaperSize (3);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryHardClipLimitsText ());

                        strTest = CHPGL.PaperSize (4);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryHardClipLimitsText ());

                        strTest = CHPGL.PaperSize (127);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryHardClipLimitsText ());

                        // Test InputP1andP2 () and OutputP1andP2 ()
                        Console.WriteLine (wb.QueryPlotter (CHPGL.OutputP1andP2 ()));

                        strTest = CHPGL.InputP1andP2 (-1, -1);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.InputP1andP2 (17000, 17000);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.InputP1andP2 (300, 13);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryPlotter (CHPGL.OutputP1andP2 ()));

                        strTest = CHPGL.InputP1andP2 (-1, -1, -1, -1);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.InputP1andP2 (17000, 17000, 17000, 17000);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.InputP1andP2 (400, 500, 10000, 7000);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryPlotter (CHPGL.OutputP1andP2 ()));

                        strTest = CHPGL.InputP1andP2 ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryPlotter (CHPGL.OutputP1andP2 ()));

                        // Test Scale ()
                        strTest = CHPGL.Scale (101, 102, 10003, 10004);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);

                        strTest = CHPGL.Scale ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);

                        // Test InputWindow () and OutputWindow ()
                        strTest = CHPGL.InputWindow (-1, -1, -1, -1);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.InputWindow (17000, 17000, 17000, 17000);
                        Console.WriteLine (strTest);

                        Console.WriteLine (wb.QueryPlotter (CHPGL.OutputWindow ()));
                        Console.WriteLine (wb.QueryOutputWindowText ());
                        strTest = CHPGL.InputWindow (101, 102, 10003, 10004);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryOutputWindowText ());

                        strTest = CHPGL.InputWindow ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                        Console.WriteLine (wb.QueryOutputWindowText ());

                        // Test RotateCoordinateSystem ()
                        strTest = CHPGL.RotateCoordinateSystem (-1);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.RotateCoordinateSystem (44);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.RotateCoordinateSystem (45);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.RotateCoordinateSystem (89);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.RotateCoordinateSystem (100);
                        Console.WriteLine (strTest);

                        strTest = CHPGL.RotateCoordinateSystem (0);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);

                        strTest = CHPGL.RotateCoordinateSystem (90);
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);

                        strTest = CHPGL.RotateCoordinateSystem ();
                        Console.WriteLine (strTest);
                        wb.WriteTextString (strTest);
                    }
                }
                #endregion
                else if (HPGL_TEST_4 == iTestMode)
                #region Test Pen and Plotting instructions 1
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

                    // Exercise the variable-argument list generating code
                    List<KeyValuePair<int, int>> lkvpInt = CHPGL.MakeIntPairList (101, 202);
                    lkvpInt = CHPGL.MakeIntPairList (303, 404, lkvpInt);
                    string strTest = CHPGL.FormatIntPairList ("AB", lkvpInt);
                    Console.WriteLine (strTest);

                    List<KeyValuePair<float, float>> lkvpFloat = CHPGL.MakeFloatPairList (1.01F, 2.02F);
                    lkvpFloat = CHPGL.MakeFloatPairList (3.03F, 4.04F, lkvpFloat);
                    strTest = CHPGL.FormatFloatPairList ("CD", lkvpFloat);
                    Console.WriteLine (strTest);

                    // PU/PD The Pen Up/Down Instructions
                    strTest = CHPGL.PenDown ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (100, 200);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (101, 101, 202, 202);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (101, 101, 202, 202, 303, 303);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (101, 101, 202, 202, 303, 303, 404, 404);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (101, 101, 202, 202, 303, 303, 404, 404, 505, 505);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (101.0F, 101.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (101.0F, 101.0F, 202.0F, 202.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (101.0F, 101.0F, 202.0F, 202.0F, 303.0F, 303.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (101.0F, 101.0F, 202.0F, 202.0F, 303.0F, 303.0F, 404.0F, 404.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (101.0F, 101.0F, 202.0F, 202.0F, 303.0F, 303.0F, 404.0F, 404.0F, 505.0F, 505.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (lkvpInt);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenDown (lkvpFloat);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.PenDown ());
                    strTest = CHPGL.PenUp ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.PenDown ());
                    strTest = CHPGL.PenUp (606, 606);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.PenDown ());
                    strTest = CHPGL.PenUp (101.0F, 101.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.PenDown ());
                    strTest = CHPGL.PenUp (lkvpInt);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.PenDown ());
                    strTest = CHPGL.PenUp (lkvpFloat);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // PA    The Plot Absolute Instruction
                    strTest = CHPGL.PlotAbsolute ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (101, 101);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (101, 101, 202, 202);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (101, 101, 202, 202, 303, 303);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (101, 101, 202, 202, 303, 303, 404, 404);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (101, 101, 202, 202, 303, 303, 404, 404, 505, 505);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (101.0F, 101.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (101.0F, 101.0F, 202.0F, 202.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (101.0F, 101.0F, 202.0F, 202.0F, 303.0F, 303.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (101.0F, 101.0F, 202.0F, 202.0F, 303.0F, 303.0F, 404.0F, 404.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (101.0F, 101.0F, 202.0F, 202.0F, 303.0F, 303.0F, 404.0F, 404.0F, 505.0F, 505.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (lkvpInt);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotAbsolute (lkvpFloat);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // PR    The Plot Relative Instruction
                    strTest = CHPGL.PlotRelative ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (101, 101);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (-101, -101, 202, 202);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (101, 101, -202, -202, 303, 303);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (101, 101, 202, 202, -303, -303, 404, 404);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (101, 101, 202, 202, 303, 303, -404, -404, 505, 505);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (101.0F, 101.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (-101.0F, -101.0F, 202.0F, 202.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (101.0F, 101.0F, -202.0F, -202.0F, 303.0F, 303.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (101.0F, 101.0F, 202.0F, 202.0F, -303.0F, -303.0F, 404.0F, 404.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (101.0F, 101.0F, 202.0F, 202.0F, 303.0F, 303.0F, -404.0F, -404.0F, 505.0F, 505.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (lkvpInt);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PlotRelative (lkvpFloat);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // VS    The Velocity Select Instruction
                    strTest = CHPGL.PlotAbsolute ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    strTest = CHPGL.VelocitySelect (1.5F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    strTest = CHPGL.PenDown (101, 101, 5050, 5050);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.VelocitySelect ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    strTest = CHPGL.PenDown (101, 101, 5050, 5050);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                }
                #endregion
                else if (HPGL_TEST_5 == iTestMode)
                #region Test Pen and Plotting instructions 2
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

                    // CI    The Circle Instruction
                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.PlotAbsolute (2000, 2000) + CHPGL.SelectPen (1));
                    string strTest = CHPGL.Circle (400);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.Circle (500.5F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.Circle (600, 45);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.Circle (500.5F, 60);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // AA    The Arc Absolute Instruction
                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.PenUp () + CHPGL.PlotAbsolute (2000, 2000) + CHPGL.SelectPen (1) + CHPGL.PenDown ());
                    strTest = CHPGL.ArcAbsolute (100, 100, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.PenUp () + CHPGL.PlotAbsolute (2000, 2000) + CHPGL.SelectPen (1) + CHPGL.PenDown ());
                    strTest = CHPGL.ArcAbsolute (200, 200.5F, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.PenUp () + CHPGL.PlotAbsolute (2000, 2000) + CHPGL.SelectPen (1) + CHPGL.PenDown ());
                    strTest = CHPGL.ArcAbsolute (300.5F, 300, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.PenUp () + CHPGL.PlotAbsolute (2000, 2000) + CHPGL.SelectPen (1) + CHPGL.PenDown ());
                    strTest = CHPGL.ArcAbsolute (405.5F, 405.5F, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.PenUp () + CHPGL.PlotAbsolute (2000, 2000) + CHPGL.SelectPen (1) + CHPGL.PenDown ());
                    strTest = CHPGL.ArcAbsolute (100, 100, 90, 30);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.PenUp () + CHPGL.PlotAbsolute (2000, 2000) + CHPGL.SelectPen (1) + CHPGL.PenDown ());
                    strTest = CHPGL.ArcAbsolute (200, 200.5F, 90, 30);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.PenUp () + CHPGL.PlotAbsolute (2000, 2000) + CHPGL.SelectPen (1) + CHPGL.PenDown ());
                    strTest = CHPGL.ArcAbsolute (300.5F, 300, 90, 30);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.PenUp () + CHPGL.PlotAbsolute (2000, 2000) + CHPGL.SelectPen (1) + CHPGL.PenDown ());
                    strTest = CHPGL.ArcAbsolute (405.5F, 405.5F, 90, 30);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // AR    The Arc Relative Instruction
                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.PenUp () + CHPGL.PlotAbsolute (2000, 2000) + CHPGL.SelectPen (1) + CHPGL.PenDown ());
                    strTest = CHPGL.ArcRelative (100, 100, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ArcRelative (200, 200.5F, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ArcRelative (300.5F, 300, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ArcRelative (405.5F, 405.5F, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ArcRelative (100, 100, 90, 30);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ArcRelative (200, 200.5F, 90, 30);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ArcRelative (300.5F, 300, 90, 30);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ArcRelative (405.5F, 405.5F, 90, 30);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                }
                #endregion
                else if (HPGL_TEST_6 == iTestMode)
                #region Test Pen and Plotting instructions 3
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

                    // FT    The Fill Type Instruction
                    string strTest = CHPGL.FillType (-1);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.FillType (6);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.FillType (3);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.FillType (-1, -10.0F);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.FillType (6, 25.0F);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.FillType (3, 10.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.FillType (-1, -10.0F, 45);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.FillType (6, 25.0F, 45);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.FillType (4, 15.5F, 45);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // PT    The Pen Thickness Instruction
                    strTest = CHPGL.PenThickness (0.0F);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.PenThickness (6.0F);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.PenThickness (3.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.PenThickness ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // RA    The Shade Rectangle Absolute Instruction
                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1) + CHPGL.PlotAbsolute (0, 0));
                    strTest = CHPGL.ShadeRectangleAbsolute (100, 100);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ShadeRectangleAbsolute (101.1F, 102.2F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // EA    The Edge Rectangle Absolute Instruction
                    strTest = CHPGL.EdgeRectangleAbsolute (101, 102);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.EdgeRectangleAbsolute (103.3F, 104.4F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // RR    The Shade Rectangle Relative Instruction
                    strTest = CHPGL.ShadeRectangleRelative (100, 100);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ShadeRectangleRelative (103.3F, 104.4F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // ER    The Edge Rectangle Relative Instruction
                    strTest = CHPGL.EdgeRectangleRelative (105, 106);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.EdgeRectangleRelative (107.7F, 108.8F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // WG    The Shade Wedge Instruction
                    strTest = CHPGL.ShadeWedge (400, 360, 180);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ShadeWedge (404.4F, 240, 270);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ShadeWedge (400, 360, 170, 0);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.ShadeWedge (400, 360, 170, 150);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.ShadeWedge (400, 360, 170, 100);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.ShadeWedge (404.4F, 360, 125, 0);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.ShadeWedge (404.4F, 360, 125, 150);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.ShadeWedge (404.4F, 360, 125, 100);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // EW    The Edge Wedge Instruction
                    strTest = CHPGL.EdgeWedge (400, 360, 170);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.EdgeWedge (404.4F, 360, 125);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.EdgeWedge (400, 360, 125, 0);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.EdgeWedge (400, 360, 125, 150);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.EdgeWedge (400, 360, 125, 100);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.EdgeWedge (404.4F, 360, 125, 0);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.EdgeWedge (404.4F, 360, 125, 150);
                    Console.WriteLine (strTest);

                    strTest = CHPGL.EdgeWedge (404.4F, 360, 125, 100);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                }
                #endregion
                else if (HPGL_TEST_7 == iTestMode)
                #region Test Plot Enhancing instructions
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

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1) + CHPGL.PlotAbsolute (0, 0));

                    // XT The X-Tick Instruction
                    string strTest = CHPGL.XAxisTick ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // YT The Y-Tick Instruction
                    strTest = CHPGL.YAxisTick ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // TL The Tick Length Instruction
                    strTest = CHPGL.TickLength (-129);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.TickLength (129);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.TickLength (90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.TickLength (-129.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.TickLength (129.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.TickLength (90.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.TickLength (-129, -129);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.TickLength (129, 129);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.TickLength (90, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.TickLength (-129.0F, -129.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.TickLength (129.0F, 129.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.TickLength (90.0F, 90.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // SM The Symbol Mode Instruction
                    strTest = CHPGL.SymbolMode ('S');
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.SymbolMode ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    // LT The Line Type Instruction
                    strTest = CHPGL.DesignateLine (-1);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateLine (9);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateLine (3);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateLine (-1, -100);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateLine (9, 130);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateLine (4, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateLine ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                }
                #endregion
                else if (HPGL_TEST_8 == iTestMode)
                #region Test Labeling instructions 1
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

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1) + CHPGL.PlotAbsolute (0, 0));

                    //CS The Designate Standard Character Set Instruction
                    string strTest = CHPGL.DesignateStandardCharacterSet (-1);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateStandardCharacterSet (5);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateStandardCharacterSet (10);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateStandardCharacterSet (29);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateStandardCharacterSet (40);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateStandardCharacterSet (3);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateStandardCharacterSet ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    //CA The Designate Alternate Character Set Instruction
                    strTest = CHPGL.DesignateAlternateCharacterSet (-1);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateAlternateCharacterSet (5);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateAlternateCharacterSet (10);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateAlternateCharacterSet (29);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateAlternateCharacterSet (40);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateAlternateCharacterSet (3);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DesignateAlternateCharacterSet ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    //SS The Select Standard Character Set Instruction
                    strTest = CHPGL.SelectStandardCharacterSet ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    //SA The Select Alternate Character Set Instruction
                    strTest = CHPGL.SelectAlternateCharacterSet ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    //DT The Define Terminator Instruction
                    strTest = CHPGL.DefineLabelTerminator ('\x14');
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.DefineLabelTerminator ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    //LB The Label Instruction
                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1) + CHPGL.PlotAbsolute (0, 0));

                    strTest = CHPGL.Label ("No terminator");
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.Label ("With terminator" + ETX);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.SelectPen () + CHPGL.PlotAbsolute (0, 0));

                    //DI The Absolute Direction Instruction
                    strTest = CHPGL.AbsoluteDirection (-129, -129);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.AbsoluteDirection (129, 129);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.AbsoluteDirection (90, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.AbsoluteDirection ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    //DR The Relative Direction Instruction
                    strTest = CHPGL.RelativeDirection (-129, -129);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.RelativeDirection (129, 129);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.RelativeDirection (90, 90);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    strTest = CHPGL.RelativeDirection ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                }
                #endregion
                else if (HPGL_TEST_9 == iTestMode)
                #region Test Labeling instructions
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

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1) + CHPGL.PlotAbsolute (0, 0));

                    //CP The Character Plot Instruction
                    wb.WriteTextString (CHPGL.PlotAbsolute (1000, 1000));
                    string strTest = CHPGL.CharacterPlot (130, 130);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.CharacterPlot (-130, -130);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    wb.WriteTextString ("CP5,.35;");
                    strTest = CHPGL.CharacterPlot (5.0F, 0.35F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    wb.WriteTextString ("CP0,-.95;");
                    strTest = CHPGL.CharacterPlot (0.0F, -0.95F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.CharacterPlot ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    //SI The Absolute Character Size Instruction
                    strTest = CHPGL.AbsoluteCharacterSize (130, 130);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.AbsoluteCharacterSize (-130, -130);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.AbsoluteCharacterSize (0, 0);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.AbsoluteCharacterSize ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    //SR The Relative Character Size Instruction
                    strTest = CHPGL.RelativeCharacterSize (130, 130);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.RelativeCharacterSize (-130, -130);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.RelativeCharacterSize (0, 0);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.RelativeCharacterSize ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    //SL The Character Slant Instruction
                    strTest = CHPGL.AbsoluteCharacterSlant (130.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.AbsoluteCharacterSlant (-130.0F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.AbsoluteCharacterSlant (0.5F);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.AbsoluteCharacterSlant ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    //UC The User-defined Character Instruction
                    CUDCPoints udc = new CUDCPoints ();
                    int iCount = udc.AddPenDownStep ();
                    iCount = udc.AddPenUpStep ();

                    try
                    {
                        iCount = udc.AddPointPair (100F, 100F);
                    }
                    catch (Exception e)
                    {
                        string strError = e.Message;
                        strError = e.Source;
                        strError = e.ToString ();
                    }

                    try
                    {
                        iCount = udc.AddPointPair (-100F, -100F);
                    }
                    catch (Exception e)
                    {
                        string strError = e.Message;
                        strError = e.Source;
                        strError = e.ToString ();
                    }

                    try
                    {
                        iCount = udc.AddPointPair (0F, 100F);
                    }
                    catch (Exception e)
                    {
                        string strError = e.Message;
                        strError = e.Source;
                        strError = e.ToString ();
                    }

                    try
                    {
                        iCount = udc.AddPointPair (0F, -100F);
                    }
                    catch (Exception e)
                    {
                        string strError = e.Message;
                        strError = e.Source;
                        strError = e.ToString ();
                    }

                    udc.ClearPoints ();
                    iCount = udc.AddPointPair (0F, 4F);
                    iCount = udc.AddPenDownStep ();
                    iCount = udc.AddPointPair (1.75F, 0F);
                    iCount = udc.AddPointPair (1.5F, 4F);
                    iCount = udc.AddPointPair (3F, -8F);
                    iCount = udc.AddPointPair (3F, 8F);
                    iCount = udc.AddPointPair (3F, -8F);
                    iCount = udc.AddPointPair (3F, 8F);
                    iCount = udc.AddPointPair (3F, -8F);
                    iCount = udc.AddPointPair (1.5F, 4F);
                    iCount = udc.AddPointPair (1.75F, 0F);
                    iCount = udc.AddPenUpStep ();
                    List<float> lfPoints = udc.GetPointsList ();
                    //"UC0,4,99,1.75,0,1.5,4,3,-8,3,8,3,-8,3,8,3,-8,1.5,4,175,0;"

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1) + CHPGL.PlotAbsolute (0, 0));

                    strTest = CHPGL.UserDefinedCharacter (lfPoints);
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    Console.WriteLine (wb.QueryErrorText ());

                    strTest = CHPGL.UserDefinedCharacter ();
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);

                    wb.WriteTextString (CHPGL.SelectPen () + CHPGL.PlotAbsolute (0, 0));
                }
                #endregion
                else if (HPGL_TEST_10 == iTestMode)
                #region Test Digitizing instructions
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

                    List<int> liPoints = CHPGL.ReadDigitizedPoints (wb);

                    //for (int iIdx = 0; iIdx < liPoints.Count; ++iIdx)
                    //{
                    //    Console.WriteLine ("X: {0}  Y: {1}  Pen: {2}", liPoints[iIdx], liPoints[++iIdx], liPoints[++iIdx]);
                    //}

                    string strPoints = CHPGL.PlotDigitizedPoints (liPoints);
                    Console.WriteLine (strPoints);

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1) + CHPGL.PlotAbsolute (0, 0));
                    wb.WriteTextString (strPoints);
                }
                #endregion
                else if (TEST_PROGRAMS_1 == iTestMode)
                #region Test programs in BASIC
                {
                    // HP 7475A manual page 2-11
                    // This example scales a square plotting area from 0 to 1 in each axis and
                    // draws a unit circle. This program should run on most BASIC systems.
                    // Change line 10 as necessary for your computer to define the plotter as
                    // the system printer. Also, if PI is not a function recognized by your
                    // computer, add a line before line 30 to define PI as a variable (PI
                    // = 3.1416). Lines 60 and 65 are necessary to limit the number of digits
                    // in the X- and Y-coordinates. This prevents the possibility of
                    // coordinates being sent to the plotter in scientific notation, which
                    // sets an error in the plotter.

                    // 10 PRINTER IS 705,80
                    // 20 PRINT "IN;IP4000,3000,5000,4000;SP1;SC0,1,0,1;"
                    // 30 FOR T=0 TO 2*PI+PI/20 STEP PI/20
                    // 40 X=COS(T)
                    // 50 Y=SIN(T)
                    // 60 PRINT USING 65;"PA",X,Y,"PD;"
                    // 65 IMAGE 2A,2(MD.DDDD),3A
                    // 70 NEXT T
                    // 80 PRINT "PU;SPO;"
                    // 90 END

                    // HP 7475A manual page 3-14
                    // 10  PRINTER IS 10
                    // 20  PRINT "IN;SP1;IP2650,1325,7650,6325;"
                    // 30  PRINT "SC-1000,1000,-1000,1000;"
                    // 40  PRINT "PA-800,800;"
                    // 50  GOSUB 130
                    // 60  PRINT "PA200,800;"
                    // 70  GOSUB 130
                    // 80  PRINT "PA-800,-200;"
                    // 90  GOSUB 130
                    // 100 PRINT "PA200,-200;"
                    // 110 GOSUB 130
                    // 120 END
                    // 130 PRINT "CI50;PR600,0;CI50;PR-300,-300;CI250;"
                    // 140 PRINT "PR-300,-300;CI50;PR600,0;CI50;"
                    // 150 RETURN

                    // HP 7475A manual page 4-4
                    // 1 PRINTER IS 705,80
                    // 10 PRINT "IN;PR300,279;SP2;PD;TL100;XT;"
                    // 20 FOR I=1 TO 10
                    // 30 PRINT "PR1000,0;XT;"
                    // 40 NEXT I
                    // 50 PRINT "TL;PU;PA300,279;PD"
                    // 60 GOSUB 1000
                    // 70 PRINT "TL1,0;PU;PA1300,279;PD;"
                    // 80 GOSUB 1000
                    // 90 PRINT "TL0,5;PU;PA2300,279;"
                    // 100 GOSUB 1000
                    // 110 PRHH "PA300,7479;TL100;YT;PU;SP0;"
                    // 120 STOP
                    // 1000 ! SUBROUTINE TO DRAW TICKS
                    // 1010 FOR J=l TO 9
                    // 1020 PRINT "PR0,720;YT;"
                    // 1030 NEXT J
                    // 1040 RETURN
                    // 1050 END                }

                    // HP 7475A manual page 5-22
                    // PRINT "IN;SP2;PA1000,1000;"
                    // FOR A=.19 TO .89 STEP .1
                    // PRINT "SI",A,A*1.4
                    // PRINT "UC4,7,99,0,1,-4,0,2,-4,-2,-4,4,0,0,1;"
                    // NEXT A
                    // PRINT "PA1000,1750;"
                    // FOR B=.19 TO .89 STEP .1
                    // PRINT "SI",B,B*1.4
                    // PRINT "LBE" + ETX
                    // NEXT B

                    // HP 7475A manual page 5-29
                    //  10 DIM A$[40],B$[40],C$[40]
                    //  20 A$="THIS LABEL IS RIGHT JUSTIFIED"
                    //  30 PRINT "SP1;SM*;PA6000,5500;PDPU;"
                    //  40 PRINT "CP";-LEN(A$);"0;LB";A$;CHR$(3)
                    //  50 B$="THIS LABEL IS CENTERED BELOW THE POINT"
                    //  60 PRINT "PA4500,5000;PDPU;"
                    //  70 PRINT "CP";-LEN(B$)/2;"-.5;LB";B$;CHR$(3)
                    //  80 C$ =" VERTICALLY CENTERED LABEL"
                    //  90 PRINT "PA2750,4500;PDPU;"
                    // 100 PRINT "CP0,-.25;LB";C$;CHR$(3)
                    // 110 END
                }
                #endregion
                else if (TEST_PROGRAMS_2 == iTestMode)
                #region Test programs in HPGL 1
                {
                    WrapperBase wb = null;

                    if (bSerial)
                    {
                        wb = new SerialWrapper ();
                        ((SerialWrapper)wb).SetConsoleOutputTrace (true);
                    }
                    else
                    {
                        wb = new ParallelWrapper ();
                    }

                    StringBuilder sbldHPGL = new StringBuilder ();

                    // HP 7475A manual 3-7
                    Console.WriteLine ("HP 7475A manual 3-7");
                    wb.WriteTextString ("IN;SP1;");
                    //                   IN;SP1;
                    sbldHPGL.Append (CHPGL.Initialize () + CHPGL.SelectPen (1));

                    wb.WriteTextString ("PA2000,1500,PD,0,1500,2000,3500,2000,1500,PU,2500,1500;");
                    //                   PA2000,1500;PD0,1500,2000,3500,2000,1500;PU2500,1500;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (2000, 1500) +
                                     CHPGL.PenDown (0, 1500, 2000, 3500, 2000, 1500) +
                                     CHPGL.PenUp (2500, 1500));

                    wb.WriteTextString ("PAPD4500,1500,2500,3500,2500,1500,PU,10365,7721;SP0;PA0,0;");
                    //                   PA;PD4500,1500,2500,3500,2500,1500;PU10365,7721;SP;PA0,0;
                    sbldHPGL.Append (CHPGL.PlotAbsolute () +
                                     CHPGL.PenDown (4500, 1500, 2500, 3500, 2500, 1500) +
                                     CHPGL.PenUp (10365, 7721) +
                                     CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual 3-8
                    Console.WriteLine ("HP 7475A manual 3-8");
                    wb.WriteTextString ("IN;SP1;SC0,100,0,100;");
                    //                   IN;SP1;SC0,100,0,100;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.Scale (0, 100, 0, 100));

                    wb.WriteTextString ("PA20,15,PD,0,15,20,35,20,15,PU,25,15;");
                    //                   PA20,15;PD0,15,20,35,20,15;PU25,15;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (20, 15) +
                                     CHPGL.PenDown (0, 15, 20, 35, 20, 15) +
                                     CHPGL.PenUp (25, 15));

                    wb.WriteTextString ("PAPD45,15,25,35,25,15,PU;SP;PA0,0;");
                    //                   PA;PD45,15,25,35,25,15;PU;SP;PA0,0;
                    sbldHPGL.Append (CHPGL.PlotAbsolute () +
                                     CHPGL.PenDown (45, 15, 25, 35, 25, 15) +
                                     CHPGL.PenUp () +
                                     CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual 3-10
                    Console.WriteLine ("HP 7475A manual 3-10");
                    wb.WriteTextString ("IN;SP1;");
                    //                   IN;SP1;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1));

                    wb.WriteTextString ("PA2000,1500,PD,PR-2000,0,2000,2000,0,-2000,PU,500,0;");
                    //                   PA2000,1500;PD;PR-2000,0,2000,2000,0,-2000;PU500,0;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (2000, 1500) +
                                     CHPGL.PenDown () +
                                     CHPGL.PlotRelative (-2000, 0, 2000, 2000, 0, -2000) +
                                     CHPGL.PenUp (500, 0));

                    wb.WriteTextString ("PD2000,0,-2000,2000,0,-2000,PU;SP;PA0,0;");
                    //                   PD2000,0,-2000,2000,0,-2000;PU;SP;PA0,0;
                    sbldHPGL.Append (CHPGL.PenDown (2000, 0, -2000, 2000, 0, -2000) +
                                     CHPGL.PenUp () +
                                     CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual 3-12
                    Console.WriteLine ("HP 7475A manual 3-12");
                    wb.WriteTextString ("IN;SP1;IP2650,1325,7650,6325;");
                    //                   IN;SP1;IP2650,1325,7650,6325;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.InputP1andP2 (2650, 1325, 7650, 6325));

                    wb.WriteTextString ("SC-100,100,-100,100;");
                    //                   SC-100,100,-100,100;
                    sbldHPGL.Append (CHPGL.Scale (-100, 100, -100, 100));

                    wb.WriteTextString ("PA-50,40;CI30,45;");
                    //                   PA-50,40;CI30,45;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (-50, 40) +
                                     CHPGL.Circle (30, 45));

                    wb.WriteTextString ("PA50,40;CI30,30;");
                    //                   PA50,40;CI30,30;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (50, 40) +
                                     CHPGL.Circle (30, 30));

                    wb.WriteTextString ("PA-50,-40;CI30,15;");
                    //                   PA-50,-40;CI30,30;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (-50, -40) +
                                     CHPGL.Circle (30, 30));
                    
                    wb.WriteTextString ("PA50,-40;CI130,5;SP;PA0,0;");
                    //                   PA50,-40;CI130,5;SP;PA0,0;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (50, -40) +
                                     CHPGL.Circle (130, 5) +
                                     CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));
                    
                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();

                    //wb.WriteTextString (CHPGL.EscAbortDeviceControl ());
                    Console.WriteLine (wb.QueryPlotter (CHPGL.EscOutputBufferSpace ()));
                    wb.WriteTextString (CHPGL.EscAbortGraphicControl ());
                    Console.WriteLine (wb.QueryPlotter (CHPGL.EscOutputBufferSpace ()));


                    // HP 7475A manual 3-13
                    Console.WriteLine ("HP 7475A manual 3-13");
                    wb.WriteTextString ("IN;SP1;IP2650,1325,7650,6325;");
                    //                   IN;SP1;IP2650,1325,7650,6325;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.InputP1andP2 (2650, 1325, 7650, 6325));
                    
                    wb.WriteTextString ("SC-100,100,-100,100;");
                    //                   SC-100,100,-100,100;
                    sbldHPGL.Append (CHPGL.Scale (-100, 100, -100, 100));
                    
                    wb.WriteTextString ("PA0,0;LT;CI10,5;LT0;CI-20,5;LT1;CI30,5;");
                    //                   PA0,0;LT;CI10,5;LT0;CI-20,5;LT1;CI30,5;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (0, 0) +
                                     CHPGL.DesignateLine () +
                                     CHPGL.Circle (10, 5) +
                                     CHPGL.DesignateLine (0) +
                                     CHPGL.Circle (-20, 5) +
                                     CHPGL.DesignateLine (1) +
                                     CHPGL.Circle (30, 5));
                    
                    wb.WriteTextString ("LT2;CI-40,5;LT3;CI50,5;LT4;CI-60,5;LT5;");
                    //                   LT2;CI-40,5;LT3;CI50,5;LT4;CI-60,5;LT5;
                    sbldHPGL.Append (CHPGL.DesignateLine (2) +
                                     CHPGL.Circle (-40, 5) +
                                     CHPGL.DesignateLine (3) +
                                     CHPGL.Circle (50, 5) +
                                     CHPGL.DesignateLine (4) +
                                     CHPGL.Circle (-60, 5) +
                                     CHPGL.DesignateLine (5));
                    
                    wb.WriteTextString ("CI170,5;LT6;CI80,5;SP;PA0,0;");
                    //                   CI170,5;LT6;CI80,5;SP;PA0,0;
                    sbldHPGL.Append (CHPGL.Circle (170, 5) +
                                     CHPGL.DesignateLine (6) +
                                     CHPGL.Circle (80, 5) +
                                     CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 3-17
                    Console.WriteLine ("HP 7475A manual page 3-17");
                    wb.WriteTextString ("IN;SP1;IP2650,1325,7650,6325;");
                    //                   IN;SP1;IP2650,1325,7650,6325;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.InputP1andP2 (2650, 1325, 7650, 6325));

                    wb.WriteTextString ("SC0,100,0,100;");
                    //                   SC0,100,0,100;
                    sbldHPGL.Append (CHPGL.Scale (0, 100, 0, 100));

                    wb.WriteTextString ("PA0,20;");
                    //                   PA0,20;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (0, 20));

                    wb.WriteTextString ("PD;PA0,40;AA0,50,180;PA0,80;");
                    //                   PD;PA0,40;AA0,50,180;PA0,80;
                    sbldHPGL.Append (CHPGL.PenDown () +
                                     CHPGL.PlotAbsolute (0, 40) +
                                     CHPGL.ArcAbsolute (0, 50, 180) +
                                     CHPGL.PlotAbsolute (0, 80));

                    wb.WriteTextString ("AA0,100,90;PA40,100;AA50,100,180;PA80,100;");
                    //                   AA0,100,90;PA40,100;AA50,100,180;PA80,100;
                    sbldHPGL.Append (CHPGL.ArcAbsolute (0, 100, 90) +
                                     CHPGL.PlotAbsolute (40, 100) +
                                     CHPGL.ArcAbsolute (50, 100, 180) +
                                     CHPGL.PlotAbsolute (80, 100));

                    wb.WriteTextString ("AA100,100,90;PA100,60;AA100,50,180;PA100,20;");
                    //                   AA100,100,90;PA100,60;AA100,50,180;PA100,20;
                    sbldHPGL.Append (CHPGL.ArcAbsolute (100, 100, 90) +
                                     CHPGL.PlotAbsolute (100, 60) +
                                     CHPGL.ArcAbsolute (100, 50, 180) +
                                     CHPGL.PlotAbsolute (100, 20));

                    wb.WriteTextString ("AA100,0,90;PA60,0;AA50,0,180;PA20,0;AA0,0,90;");
                    //                   AA100,0,90;PA60,0;AA50,0,180;PA20,0;AA0,0,90;
                    sbldHPGL.Append (CHPGL.ArcAbsolute (100, 0, 90) +
                                     CHPGL.PlotAbsolute (60, 0) +
                                     CHPGL.ArcAbsolute (50, 0, 180) +
                                     CHPGL.PlotAbsolute (20, 0) +
                                     CHPGL.ArcAbsolute (0, 0, 90));

                    wb.WriteTextString ("PU;PA50,50;CI30;SP;PA0,0;");
                    //                   PU;PA50,50;CI30;SP;PA0,0;
                    sbldHPGL.Append (CHPGL.PenUp () +
                                     CHPGL.PlotAbsolute (50, 50) +
                                     CHPGL.Circle (30) +
                                     CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 3-19
                    Console.WriteLine ("HP 7475A manual page 3-19");
                    wb.WriteTextString ("IN;SP1;IP2650,1325,7650,6325;");
                    //                   IN;SP1;IP2650,1325,7650,6325;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.InputP1andP2 (2650, 1325, 7650, 6325));

                    wb.WriteTextString ("SC-100,100,-100,100;");
                    //                   SC-100,100,-100,100;
                    sbldHPGL.Append (CHPGL.Scale (-100, 100, -100, 100));

                    wb.WriteTextString ("PA-80,-50;PD;AR0,80,90;AR80,0,90;PU;SP;PA0,0;");
                    //                   PA-80,-50;PD;AR0,80,90;AR80,0,90;PU;SP;PA0,0;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (-80, -50) +
                                     CHPGL.PenDown () +
                                     CHPGL.ArcRelative (0, 80, 90) +
                                     CHPGL.ArcRelative (80, 0, 90) +
                                     CHPGL.PenUp () +
                                     CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 3-20
                    Console.WriteLine ("HP 7475A manual page 3-20");
                    wb.WriteTextString ("IN;SP1;IP2650,1325,7650,5325;");
                    //                   IN;SP1;IP2650,1325,7650,5325;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.InputP1andP2 (2650, 1325, 7650, 5325));

                    wb.WriteTextString ("SC-100,100,-100,100;");
                    //                   SC-100,100,-100,100;
                    sbldHPGL.Append (CHPGL.Scale (-100, 100, -100, 100));
                    
                    wb.WriteTextString ("PA-100,40;PD;PR60,0;AR0,-40,-90;AR40,0,90;PR60,0;PU;SP;PA0,0;");
                    //                   PA-100,40;PD;PR60,0;AR0,-40,-90;AR40,0,90;PR60,0;PU;SP;PA0,0;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (-100, 40) +
                                     CHPGL.PenDown () +
                                     CHPGL.PlotRelative (60, 0) +
                                     CHPGL.ArcRelative (0, -40, -90) +
                                     CHPGL.ArcRelative (40, 0, 90) +
                                     CHPGL.PlotRelative (60, 0) +
                                     CHPGL.PenUp () +
                                     CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 3-24
                    Console.WriteLine ("HP 7475A manual page 3-24");
                    wb.WriteTextString ("IN;SP1;PA5000,4000;");
                    //                   IN;SP1;PA5000,4000;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.PlotAbsolute (5000, 4000));

                    wb.WriteTextString ("PT.3;FT1;RA4000,3000;");
                    //                   PT.3;FT1;RA4000,3000;
                    sbldHPGL.Append (CHPGL.PenThickness (.3F) +
                                     CHPGL.FillType (1) +
                                     CHPGL.ShadeRectangleAbsolute (4000, 3000));

                    wb.WriteTextString ("FT3,100;RA6000,3000;");
                    //                   FT3,100;RA6000,3000;
                    sbldHPGL.Append (CHPGL.FillType (3, 100) +
                                     CHPGL.ShadeRectangleAbsolute (6000, 3000));

                    wb.WriteTextString ("FT2;RA6000,5000;");
                    //                   FT2;RA6000,5000;
                    sbldHPGL.Append (CHPGL.FillType (2) +
                                     CHPGL.ShadeRectangleAbsolute (6000, 5000));

                    wb.WriteTextString ("FT4,100,45;RA4000,5000;");
                    //                   FT4,100,45;RA4000,5000;
                    sbldHPGL.Append (CHPGL.FillType (4, 100, 45) +
                                     CHPGL.ShadeRectangleAbsolute (4000, 5000));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 3-25
                    Console.WriteLine ("HP 7475A manual page 3-25");
                    wb.WriteTextString ("IN;SP1;PA5000,4000;");
                    //                   IN;SP1;PA5000,4000;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.PlotAbsolute (5000, 4000));

                    wb.WriteTextString ("PT.3;FT1;RA4000,3000;");
                    //                   PT.3;FT1;RA4000,3000;
                    sbldHPGL.Append (CHPGL.PenThickness (.3F) +
                                     CHPGL.FillType (1) +
                                     CHPGL.ShadeRectangleAbsolute (4000, 3000));

                    wb.WriteTextString ("SP3;EA4000,3000;");
                    //                   SP3;EA4000,3000;
                    sbldHPGL.Append (CHPGL.SelectPen (3) +
                                     CHPGL.EdgeRectangleAbsolute (4000, 3000));

                    wb.WriteTextString ("SP4;FT3;RA6000,3000;");
                    //                   SP4;FT3;RA6000,3000;
                    sbldHPGL.Append (CHPGL.SelectPen (4) +
                                     CHPGL.FillType (3) +
                                     CHPGL.ShadeRectangleAbsolute (6000, 3000));

                    wb.WriteTextString ("SP3;EA6000,3000;");
                    //                   SP3;EA6000,3000;
                    sbldHPGL.Append (CHPGL.SelectPen (3) +
                                     CHPGL.EdgeRectangleAbsolute (6000, 3000));

                    wb.WriteTextString ("SP5;FT2;RA6000,5000;");
                    //                   SP5;FT2;RA6000,5000;
                    sbldHPGL.Append (CHPGL.SelectPen (5) +
                                     CHPGL.FillType (2) +
                                     CHPGL.ShadeRectangleAbsolute (6000, 5000));

                    wb.WriteTextString ("SP3;EA6000,5000;");
                    //                   SP3;EA6000,5000;
                    sbldHPGL.Append (CHPGL.SelectPen (3) +
                                     CHPGL.EdgeRectangleAbsolute (6000, 5000));

                    wb.WriteTextString ("SP6;FT4,100,45;RA4000,5000;");
                    //                   SP6;FT4,100,45;RA4000,5000;
                    sbldHPGL.Append (CHPGL.SelectPen (6) +
                                     CHPGL.FillType (4, 100, 45) +
                                     CHPGL.ShadeRectangleAbsolute (4000, 5000));

                    wb.WriteTextString ("SP3;EA4000,5000;");
                    //                   SP3;EA4000,5000;
                    sbldHPGL.Append (CHPGL.SelectPen (3) +
                                     CHPGL.EdgeRectangleAbsolute (4000, 5000));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 3-27
                    Console.WriteLine ("HP 7475A manual page 3-27");
                    wb.WriteTextString ("IN;SP1;PR5000,5000;");
                    //                   IN;SP1;PR5000,5000;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.PlotRelative (5000, 5000));

                    wb.WriteTextString ("PT.3;FT1;RR1000,1000;");
                    //                   PT.3;FT1;RR1000,1000;
                    sbldHPGL.Append (CHPGL.PenThickness (.3F) +
                                     CHPGL.FillType (1) +
                                     CHPGL.ShadeRectangleRelative (1000, 1000));

                    wb.WriteTextString ("PR1000,0;");
                    //                   PR1000,0;
                    sbldHPGL.Append (CHPGL.PlotRelative (1000, 0));

                    wb.WriteTextString ("FT3,100;RR1000,1000;");
                    //                   FT3,100;RR1000,1000;
                    sbldHPGL.Append (CHPGL.FillType (3, 100) +
                                     CHPGL.ShadeRectangleRelative (1000, 1000));

                    wb.WriteTextString ("PR0,1000;");
                    //                   PR0,1000;
                    sbldHPGL.Append (CHPGL.PlotRelative (0, 1000));

                    wb.WriteTextString ("FT2;RR1000,1000;");
                    //                   FT2;RR1000,1000;
                    sbldHPGL.Append (CHPGL.FillType (2) +
                                     CHPGL.ShadeRectangleRelative (1000, 1000));

                    wb.WriteTextString ("FT4,100,45;RR-1000,1000;");
                    //                   FT4,100,45;RR-1000,1000;
                    sbldHPGL.Append (CHPGL.FillType (4, 100, 45) +
                                     CHPGL.ShadeRectangleRelative (-1000, 1000));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 3-29
                    Console.WriteLine ("HP 7475A manual page 3-29");
                    wb.WriteTextString ("IN;SP1;PA5000,5000;");
                    //                   IN;SP1;PA5000,5000;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.PlotAbsolute (5000, 5000));

                    wb.WriteTextString ("PT.3;FT1;RR1000,1000;");
                    //                   PT.3;FT1;RR1000,1000;
                    sbldHPGL.Append (CHPGL.PenThickness (.3F) +
                                     CHPGL.FillType (1) +
                                     CHPGL.ShadeRectangleRelative (1000, 1000));

                    wb.WriteTextString ("SP3;ER1000,1000;");
                    //                   SP3;ER1000,1000;
                    sbldHPGL.Append (CHPGL.SelectPen (3) +
                                     CHPGL.EdgeRectangleRelative (1000, 1000));

                    wb.WriteTextString ("PR1000,0;");
                    //                   PR1000,0;
                    sbldHPGL.Append (CHPGL.PlotRelative (1000, 0));

                    wb.WriteTextString ("SP4;FT3;RR1000,1000;");
                    //                   SP4;FT3;RR1000,1000;
                    sbldHPGL.Append (CHPGL.SelectPen (4) +
                                     CHPGL.FillType (3) +
                                     CHPGL.ShadeRectangleRelative (1000, 1000));

                    wb.WriteTextString ("SP3;ER1000,1000;");
                    //                   SP3;ER1000,1000;
                    sbldHPGL.Append (CHPGL.SelectPen (3) +
                                     CHPGL.EdgeRectangleRelative (1000, 1000));

                    wb.WriteTextString ("PR0,1000;");
                    //                   PR0,1000;
                    sbldHPGL.Append (CHPGL.PlotRelative (0, 1000));

                    wb.WriteTextString ("SP5;FT2;RR1000,1000;");
                    //                   SP5;FT2;RR1000,1000;
                    sbldHPGL.Append (CHPGL.SelectPen (5) +
                                     CHPGL.FillType (2) +
                                     CHPGL.ShadeRectangleRelative (1000, 1000));

                    wb.WriteTextString ("SP3;ER1000,1000;");
                    //                   SP3;ER1000,1000;
                    sbldHPGL.Append (CHPGL.SelectPen (3) +
                                     CHPGL.EdgeRectangleRelative (1000, 1000));

                    wb.WriteTextString ("SP6;FT4,100,45;RR-1000,1000;");
                    //                   SP6;FT4,100,45;RR-1000,1000;
                    sbldHPGL.Append (CHPGL.SelectPen (6) +
                                     CHPGL.FillType (4, 100, 45) +
                                     CHPGL.ShadeRectangleRelative (-1000, 1000));

                    wb.WriteTextString ("SP3;ER-1000,1000;");
                    //                   SP3;ER-1000,1000;
                    sbldHPGL.Append (CHPGL.SelectPen (3) +
                                     CHPGL.EdgeRectangleRelative (-1000, 1000));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 3-33
                    Console.WriteLine ("HP 7475A manual page 3-33");
                    wb.WriteTextString ("IN;SP2;FT3,100;");
                    //                   IN;SP2;FT3,100;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (2) +
                                     CHPGL.FillType (3, 100));

                    wb.WriteTextString ("PA5000,5000;");
                    //                   PA5000,5000;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (5000, 5000));

                    wb.WriteTextString ("WG1000,90,180,5;");
                    //                   WG1000,90,180,5;
                    sbldHPGL.Append (CHPGL.ShadeWedge (1000, 90, 180, 5));

                    wb.WriteTextString ("SP4;FT4,100,45;");
                    //                   SP4;FT4,100,45;
                    sbldHPGL.Append (CHPGL.SelectPen (4) +
                                     CHPGL.FillType (4, 100, 45));

                    wb.WriteTextString ("WG1000,270,120;");
                    //                   WG1000,270,120;
                    sbldHPGL.Append (CHPGL.ShadeWedge (1000, 270, 120));

                    wb.WriteTextString ("SP1;FT1;");
                    //                   SP1;FT1;
                    sbldHPGL.Append (CHPGL.SelectPen (1) +
                                     CHPGL.FillType (1));

                    wb.WriteTextString ("WG1000,30,60;");
                    //                   WG1000,30,60;
                    sbldHPGL.Append (CHPGL.ShadeWedge (1000, 30, 60));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 3-36
                    Console.WriteLine ("HP 7475A manual page 3-36");
                    wb.WriteTextString ("IN;SP1;FT3,100;");
                    //                   IN;SP1;FT3,100;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.FillType (3, 100));

                    wb.WriteTextString ("PA5000,5000;");
                    //                   PA5000,5000;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (5000, 5000));

                    wb.WriteTextString ("WG1000,90,180,5;");
                    //                   WG1000,90,180,5;
                    sbldHPGL.Append (CHPGL.ShadeWedge (1000, 90, 180, 5));

                    wb.WriteTextString ("SP3;EW1000,90,180,5;");
                    //                   SP3;EW1000,90,180,5;
                    sbldHPGL.Append (CHPGL.SelectPen (3) +
                                     CHPGL.EdgeWedge (1000, 90, 180, 5));

                    wb.WriteTextString ("SP4;FT4,100,45;");
                    //                   SP4;FT4,100,45;
                    sbldHPGL.Append (CHPGL.SelectPen (4) +
                                     CHPGL.FillType (4, 100, 45));

                    wb.WriteTextString ("WG1000,270,120;");
                    //                   WG1000,270,120;
                    sbldHPGL.Append (CHPGL.ShadeWedge (1000, 270, 120));

                    wb.WriteTextString ("SP3;EW1000,270,120;");
                    //                   SP3;EW1000,270,120;
                    sbldHPGL.Append (CHPGL.SelectPen (3) +
                                     CHPGL.EdgeWedge (1000, 270, 120));

                    wb.WriteTextString ("SP1,FT1;");
                    //                   SP1;FT1;
                    sbldHPGL.Append (CHPGL.SelectPen (1) +
                                     CHPGL.FillType (1));

                    wb.WriteTextString ("WG1000,30,60;");
                    //                   WG1000,30,60;
                    sbldHPGL.Append (CHPGL.ShadeWedge (1000, 30, 60));

                    wb.WriteTextString ("SP3;EW1000,30,60;");
                    //                   SP3;EW1000,30,60;
                    sbldHPGL.Append (CHPGL.SelectPen (3) +
                                     CHPGL.EdgeWedge (1000, 30, 60));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();
                }
                #endregion
                else if (TEST_PROGRAMS_3 == iTestMode)
                #region Test programs in HPGL 2
                {
                    float f = 1.2F;
                    string s = string.Format ("{0:###.####}", f);

                    WrapperBase wb = null;

                    if (bSerial)
                    {
                        wb = new SerialWrapper ();
                    }
                    else
                    {
                        wb = new ParallelWrapper ();
                    }

                    StringBuilder sbldHPGL = new StringBuilder ();

                    wb.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1) + CHPGL.PlotAbsolute (100, 5000));

                    // HP 7475A manual page 4-2
                    Console.WriteLine ("HP 7475A manual page 4-2");
                    wb.WriteTextString ("IN;SP2;PA200,500;PD;XT;PR1000,0;XT;");
                    //                   IN;SP2;PA200,500;PD;XT;PR1000,0;XT;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (2) +
                                     CHPGL.PlotAbsolute (200, 500) +
                                     CHPGL.PenDown () +
                                     CHPGL.XAxisTick () +
                                     CHPGL.PlotRelative (1000, 0) +
                                     CHPGL.XAxisTick ());

                    wb.WriteTextString ("PR1000,0;XT;PR1000,0;XT;PU;SP0;");
                    //                   PR1000,0;XT;PR1000,0;XT;PU;SP0;SP;PA0,0;
                    sbldHPGL.Append (CHPGL.PlotRelative (1000, 0) +
                                     CHPGL.XAxisTick () +
                                     CHPGL.PlotRelative (1000, 0) +
                                     CHPGL.XAxisTick () +
                                     CHPGL.PenUp () +
                                     CHPGL.SelectPen (0));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 4-5
                    Console.WriteLine ("HP 7475A manual page 4-5");
                    wb.WriteTextString ("IN;SP1;");
                    //                   IN;SP1;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1));

                    wb.WriteTextString ("PA200,500;SM*;PR200,1000;");
                    //                   PA200,500;SM*;PR200,1000;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (200, 500) +
                                     CHPGL.SymbolMode ('*') +
                                     CHPGL.PlotRelative (200, 1000));

                    wb.WriteTextString ("PD400,1230,600,1560,900,1670,1500,1600,2000,2000;");
                    //                   PD400,1230,600,1560,900,1670,1500,1600,2000,2000;
                    sbldHPGL.Append (CHPGL.PenDown (400, 1230, 600, 1560, 900, 1670, 1500, 1600, 2000, 2000));

                    wb.WriteTextString ("PU;SM;PA100,300;SM3;");
                    //                   PU;SM;PA100,300;SM3;
                    sbldHPGL.Append (CHPGL.PenUp () +
                                     CHPGL.SymbolMode () +
                                     CHPGL.PlotAbsolute (100, 300) +
                                     CHPGL.SymbolMode ('3'));

                    wb.WriteTextString ("PA300,500,500,450,900,850,1350,1300,2100,1350PU;");
                    //                   PA300,500,500,450,900,850,1350,1300,2100,1350;PU;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (300, 500, 500, 450, 900, 850, 1350, 1300, 2100, 1350) +
                                     CHPGL.PenUp ());

                    wb.WriteTextString ("SM;PA1900,560;PD;SMY;PA3300,1250;");
                    //                   SM;PA1900,560;PD;SMY;PA3300,1250;
                    sbldHPGL.Append (CHPGL.SymbolMode () +
                                     CHPGL.PlotAbsolute (1900, 560) +
                                     CHPGL.PenDown () +
                                     CHPGL.SymbolMode ('Y') +
                                     CHPGL.PlotAbsolute (3300, 1250));

                    wb.WriteTextString ("SMZ;PA3500,950;SMX;PA1900,560;PU;SP0;");
                    //                   SMZ;PA3500,950;SMX;PA1900,560;PU;SP0;
                    sbldHPGL.Append (CHPGL.SymbolMode ('Z') +
                                     CHPGL.PlotAbsolute (3500, 950) +
                                     CHPGL.SymbolMode ('X') +
                                     CHPGL.PlotAbsolute (1900, 560) +
                                     CHPGL.PenUp () +
                                     CHPGL.SelectPen (0));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 5-2
                    Console.WriteLine ("HP 7475A manual page 5-2");
                    wb.WriteTextString ("IN;PA500,800;SP2;");
                    //                   IN;PA500,800;SP2;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.PlotAbsolute (500, 800) +
                                     CHPGL.SelectPen (2));

                    wb.WriteTextString ("CS33;LB60 & DR" + '\x5D' + "BER" + ETX);
                    //                   CS33;LB60 & DR     ]        BER    ♥;
                    sbldHPGL.Append (CHPGL.DesignateStandardCharacterSet (33) +
                                     CHPGL.Label ("60 & DR" + '\x5D' + "BER" + ETX));

                    wb.WriteTextString ("CS4;LB#su compan" + '\x7C' + "ia?" + ETX);
                    //                   CS4;LB#su compan    |         ia?    ♥;
                    sbldHPGL.Append (CHPGL.DesignateStandardCharacterSet (4) +
                                     CHPGL.Label ("#su compan" + '\x7C' + "ia?" + ETX));

                    wb.WriteTextString ("PA500,500;SP2;");
                    //                   PA500,500;SP2;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (500, 500) +
                                     CHPGL.SelectPen (2));

                    wb.WriteTextString ("CS30;LB35-50 " + '\x5D' + "R" + ETX);
                    //                   CS30;LB35-50     ]         R    ♥;
                    sbldHPGL.Append (CHPGL.DesignateStandardCharacterSet (30) +
                                     CHPGL.Label ("35-50 " + '\x5D' + "R" + ETX));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 5-5
                    Console.WriteLine ("HP 7475A manual page 5-5");
                    wb.WriteTextString ("IN;SP1;");
                    //                   IN;SP1;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1));

                    wb.WriteTextString ("PR500,1100;");
                    //                   PR500,1100;
                    sbldHPGL.Append (CHPGL.PlotRelative (500, 1100));

                    wb.WriteTextString ("SP2;CS0;CA4;SS;LBS_E_T_0_" + '\x0E' + "S_E_T_4_" + ETX);
                    //                   SP2;CS0;CA4;SS;LBS_E_T_0_    ♫         S_E_T_4_    ♥;
                    sbldHPGL.Append (CHPGL.SelectPen (2) +
                                     CHPGL.DesignateStandardCharacterSet (0) +
                                     CHPGL.DesignateAlternateCharacterSet (4) +
                                     CHPGL.SelectStandardCharacterSet () +
                                     CHPGL.Label ("S_E_T_0_" + '\x0E' + "S_E_T_4_" + ETX));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 5-6
                    Console.WriteLine ("HP 7475A manual page 5-6");
                    // Default control character ETX
                    // terminates by performing end-
                    // of-text function.
                    //
                    // Printing characters terminate,
                    // #but are also printed.#
                    //
                    // Control characters terminate
                    // and perform their function.
                    wb.WriteTextString ("IN;SP3;"); // ("IN;SP3;SC0,500,5000;PA0,0;");
                    //                   IN;SP3;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (3));

                    wb.WriteTextString ("PA0,9000;LBDefault control character ETX" + CR + LF + ETX);
                    //                   PA0,9000;LBDefault control character ETX              ♥;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (0, 9000) +
                                     CHPGL.Label ("Default control character ETX" + CR + LF + ETX));

                    wb.WriteTextString ("LBterminates by performing end-" + CR + LF + ETX);
                    //                   LBterminates by performing end-              ♥;
                    sbldHPGL.Append (CHPGL.Label ("terminates by performing end-" + CR + LF + ETX));

                    wb.WriteTextString ("LBof-text function." + ETX);
                    //                   LBof-text function.    ♥;
                    sbldHPGL.Append (CHPGL.Label ("of-text function." + ETX));

                    wb.WriteTextString ("PA0,3900;DT#;LBPrinting characters terminate," + CR + LF + '#');
                    //                   PA0,3900;DT#;LBPrinting characters terminate,;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (0, 3900) +
                                     CHPGL.DefineLabelTerminator ('#') +
                                     CHPGL.Label ("Printing characters terminate," + CR + LF + '#'));

                    wb.WriteTextString ("LBbut are also printed.#");
                    //                   
                    sbldHPGL.Append (CHPGL.Label ("but are also printed.#"));

                    wb.WriteTextString ("PA0,3400;DT" + CR + ";LBControl characters terminate" + LF + CR);
                    //                       3400;DT;          LBControl characters terminate
                    sbldHPGL.Append (CHPGL.PlotAbsolute (0, 3400) +
                                     CHPGL.DefineLabelTerminator (CR) +
                                     CHPGL.Label ("Control characters terminate" + LF + CR));

                    wb.WriteTextString ("LBand perform their functions." + CR);
                    //                            form their functions.
                    sbldHPGL.Append (CHPGL.Label ("and perform their functions." + CR));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 5-11
                    Console.WriteLine ("HP 7475A manual page 5-11");
                    wb.WriteTextString ("IN;SP1;PA2000,2000;");
                    //                   IN;SP1;PA2000,2000;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.PlotAbsolute (2000, 2000));

                    wb.WriteTextString ("DI0,1;LB_*_1984" + ETX + "DI1,1;LB_*_1985" + ETX);
                    //                   DI0,1;LB_*_1984    ♥;     DI1,1;LB_*_1985    ♥;
                    sbldHPGL.Append (CHPGL.AbsoluteDirection (0, 1) +
                                     CHPGL.Label ("_*_1984" + ETX) +
                                     CHPGL.AbsoluteDirection (1, 1) +
                                     CHPGL.Label ("_*_1985" + ETX));

                    wb.WriteTextString ("DI1,0;LB_*_1986" + ETX + "DI1,-1;LB_*_1987" + ETX);
                    //                   DI1,0;LB_*_1986    ♥;     DI1,-1;LB_*_1987    ♥;
                    sbldHPGL.Append (CHPGL.AbsoluteDirection (1, 0) +
                                     CHPGL.Label ("_*_1986" + ETX) +
                                     CHPGL.AbsoluteDirection (1, -1) +
                                     CHPGL.Label ("_*_1987" + ETX));

                    wb.WriteTextString ("DI0,-1;LB_*_1988" + ETX + "DI-1,-1;LB_*_1989" + ETX);
                    //                   DI0,-1;LB_*_1988    ♥;     DI-1,-1;LB_*_1989    ♥;
                    sbldHPGL.Append (CHPGL.AbsoluteDirection (0, -1) +
                                     CHPGL.Label ("_*_1988" + ETX) +
                                     CHPGL.AbsoluteDirection (-1, -1) +
                                     CHPGL.Label ("_*_1989" + ETX));

                    wb.WriteTextString ("DI-1,0;LB_*_1990" + ETX + "DI-1,1;LB_*_1991" + ETX);
                    //                   DI-1,0;LB_*_1990    ♥;     DI-1,1;LB_*_1991    ♥;
                    sbldHPGL.Append (CHPGL.AbsoluteDirection (-1, 0) +
                                     CHPGL.Label ("_*_1990" + ETX) +
                                     CHPGL.AbsoluteDirection (-1, 1) +
                                     CHPGL.Label ("_*_1991" + ETX));

                    wb.WriteTextString ("PA2450,2900;DI1,0;LB_*_2000" + CR + LF + ETX);
                    //                   PA2450,2900;DI1,0;LB_*_2000              ♥;
                    sbldHPGL.Append (CHPGL.PlotAbsolute (2450, 2900) +
                                     CHPGL.AbsoluteDirection (1, 0) +
                                     CHPGL.Label ("_*_2000" + CR + LF + ETX));

                    wb.WriteTextString ("DI0.7071,-0.7071;LB_RETURN POINT" + ETX);
                    //                   DI0.7071,-0.7071;LB_RETURN POINT    ♥;
                    sbldHPGL.Append (CHPGL.AbsoluteDirection (0.7071F, -0.7071F) +
                                     CHPGL.Label ("_RETURN POINT" + ETX));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 5-15
                    Console.WriteLine ("HP 7475A manual page 5-15");
                    wb.WriteTextString ("DF;SP1;PA1000,1000;PD;PR3000,0;PU;PR-3000,0;");
                    //                   DF;SP1;PA1000,1000;PD;PR3000,0;PU;PR-3000,0;
                    sbldHPGL.Append (CHPGL.SetDefaultValues () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.PlotAbsolute (1000, 1000) +
                                     CHPGL.PenDown () +
                                     CHPGL.PlotRelative (3000, 0) +
                                     CHPGL.PenUp () +
                                     CHPGL.PlotRelative (-3000, 0));

                    //wb.WriteTextString ("CP5,.35;LBABOVE THE LINE" + ETX + "PA2000,1000;");
                    //                     CP5,0.35;LBABOVE THE LINE   ♥;     PA2000,1000;
                    string strTest = CHPGL.CharacterPlot (5.0F, 0.35F) + "LBABOVE THE LINE" + ETX + "PA2000,1000;";
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    sbldHPGL.Append (CHPGL.CharacterPlot (5.0F, 0.35F) +
                                     CHPGL.Label ("ABOVE THE LINE" + ETX) +
                                     CHPGL.PlotAbsolute (2000, 1000));

                    //wb.WriteTextString ("XT;CP0,-.95;LBBELOW THE LINE" + CR + LF + "AND WITH A NEAT" + ETX);
                    //                     XT;CP0,-0.95;LBBELOW THE LINE              AND WITH A NEAT    ♥;
                    strTest = "XT;" + CHPGL.CharacterPlot (0.0F, -.95F) + "LBBELOW THE LINE" + CR + LF + "AND WITH A NEAT" + ETX;
                    Console.WriteLine (strTest);
                    wb.WriteTextString (strTest);
                    sbldHPGL.Append (CHPGL.XAxisTick () +
                                     CHPGL.CharacterPlot (0.0F, -.95F) +
                                     CHPGL.Label ("BELOW THE LINE" + CR + LF + "AND WITH A NEAT" + ETX));

                    wb.WriteTextString ("CP;LBMARGIN" + ETX);
                    //                   CP;LBMARGIN    ♥;
                    sbldHPGL.Append (CHPGL.CharacterPlot () +
                                     CHPGL.Label ("MARGIN" + ETX));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 5-16
                    Console.WriteLine ("HP 7475A manual page 5-16");
                    wb.WriteTextString ("IN;SP1;");
                    //                   IN;SP1;
                    sbldHPGL.Append (CHPGL.Initialize () +
                                     CHPGL.SelectPen (1));

                    wb.WriteTextString ("SI1,1.5;LB7475A" + ETX);
                    //                   SI1,1.5;LB7475A    ♥;
                    sbldHPGL.Append (CHPGL.AbsoluteCharacterSize (1, 1.5F) +
                                     CHPGL.Label ("7475A" + ETX));

                    wb.WriteTextString ("SI-.35,.6;LBHP" + ETX);
                    //                   SI-0.35,0.6;LBHP  ♥;
                    sbldHPGL.Append (CHPGL.AbsoluteCharacterSize (-.35F, .6F) +
                                     CHPGL.Label ("HP" + ETX));


                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 5-19
                    Console.WriteLine ("HP 7475A manual page 5-19");
                    wb.WriteTextString ("DF;SP1;SI1.3,1.8;PA3000,6000;");
                    //                   DF;SP1;SI1.3,1.8;PA3000,6000;
                    sbldHPGL.Append (CHPGL.SetDefaultValues () +
                                     CHPGL.SelectPen (1) +
                                     CHPGL.AbsoluteCharacterSize (1.3F, 1.8F) +
                                     CHPGL.PlotAbsolute (3000, 6000));

                    wb.WriteTextString ("SL1;LBHP" + ETX);
                    //                   SL1;LBHP    ♥;
                    sbldHPGL.Append (CHPGL.AbsoluteCharacterSlant (1) +
                                     CHPGL.Label ("HP" + ETX));

                    wb.WriteTextString ("SL-1;PR1300,0;LBHP" + ETX);
                    //                   SL-1;PR1300,0;LBHP    ♥;
                    sbldHPGL.Append (CHPGL.AbsoluteCharacterSlant (-1) +
                                     CHPGL.PlotRelative (1300, 0) +
                                     CHPGL.Label ("HP" + ETX));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    // HP 7475A manual page 5-22
                    Console.WriteLine ("HP 7475A manual page 5-22");
                    wb.WriteTextString ("SP1;PA1000,5000;SI.25,.4");
                    //                   SP1;PA1000,5000;SI0.25,0.4;
                    sbldHPGL.Append (CHPGL.SelectPen (1) +
                                     CHPGL.PlotAbsolute (1000, 5000) +
                                     CHPGL.AbsoluteCharacterSize (.25F, .4F));

                    wb.WriteTextString ("UC0,4,99,1.75,0,1.5,4,3,-8,3,8,3,-8,3,8,3,-8,1.5,4,1.75,0;");
                    //                   UC0,4,99,1.75,0,1.5,4,3,-8,3,8,3,-8,3,8,3,-8,1.5,4,1.75,0;
                    CUDCPoints udc = new CUDCPoints ();
                    udc.AddPointPair (0, 4);
                    udc.AddPenDownStep ();
                    udc.AddPointPair (1.75F, 0);
                    udc.AddPointPair (1.5F, 4);
                    udc.AddPointPair (3, -8);
                    udc.AddPointPair (3, 8);
                    udc.AddPointPair (3, -8);
                    udc.AddPointPair (3, 8);
                    udc.AddPointPair (3, -8);
                    udc.AddPointPair (1.5F, 4);
                    udc.AddPointPair (1.75F, 0);
                    sbldHPGL.Append (CHPGL.UserDefinedCharacter (udc.GetPointsList ()));

                    wb.WriteTextString ("CP3.25,0;LB1000 ohms" + ETX);
                    //                   CP3.25,0;LB1000 ohms    ♥;
                    sbldHPGL.Append (CHPGL.CharacterPlot (3.25F, 0) +
                                     CHPGL.Label ("1000 ohms" + ETX));

                    wb.WriteTextString ("SP;PA0,0;");
                    //                   SP;PA0,0;
                    sbldHPGL.Append (CHPGL.SelectPen () +
                                     CHPGL.PlotAbsolute (0, 0));

                    Console.WriteLine (sbldHPGL.ToString ());
                    wb.WriteTextString (sbldHPGL.ToString ());
                    sbldHPGL.Clear ();


                    //// HP 7475A manual page 8-3
                    //wb.WriteTextString ("IN;SP1;IP1250,750,9250,6250;");
                    //wb.WriteTextString ("SC1,12,0,150;");
                    //wb.WriteTextString ("PU1;OPD12,0,12,150,1,150,1,0;PU");
                    //wb.WriteTextString ("SP;PA0,0;");
                }
                #endregion
                else
                #region Test CPlotterBuffer
                {
                    CPlotterBuffer pb = new CPlotterBuffer ();
                    pb.BufferEnqueue (CHPGL.Initialize () + CHPGL.SelectPen (-1) + CHPGL.SelectPen (21) + CHPGL.SelectPen (1));
                    if (STRESS_TEST == iTestMode)
                    {
                        StringBuilder sbldLongCommand = new StringBuilder ();
                        int iCount = 0;
                        while (sbldLongCommand.Length < 1200)
                        {
                            sbldLongCommand.Append (CHPGL.PlotAbsolute (600, 1000) + CHPGL.Circle (400));
                            iCount++;
                        }
                        pb.BufferEnqueue (sbldLongCommand.ToString ());
                    }
                    else
                    {
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 1000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 1000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 1000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 1000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 1000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 1000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 1000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 1000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 1000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 1000) + CHPGL.Circle (400));
                    }
                    pb.BufferEnqueue (CHPGL.SelectPen (4));
                    pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                    if (STRESS_TEST == iTestMode)
                    {
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                        pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                    }
                    pb.BufferEnqueue (CHPGL.SelectPen (2));
                    pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 2000) + CHPGL.Circle (400));
                    pb.BufferEnqueue (CHPGL.SelectPen (8));
                    pb.BufferEnqueue (CHPGL.SelectPen (7));
                    pb.BufferEnqueue (CHPGL.SelectPen (6));
                    pb.BufferEnqueue (CHPGL.SelectPen (4));
                    pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 5000) + CHPGL.Circle (400));
                    pb.BufferEnqueue (CHPGL.SelectPen (3));
                    pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 3000) + CHPGL.Circle (400));
                    pb.BufferEnqueue (CHPGL.SelectPen () + CHPGL.PlotAbsolute (0, 0));
                    int iInstructionCount = pb.GetInstructionCount ();
                    int iLineCount = pb.GetLineCount ();
                    pb.BufferSort ();
                    pb.BufferEnqueue (CHPGL.SelectPen (2));
                    pb.BufferEnqueue (CHPGL.PlotAbsolute (600, 6000) + CHPGL.Circle (400));
                    iLineCount = pb.GetLineCount ();
                    iInstructionCount = pb.GetInstructionCount ();
                    pb.BufferSort ();
                    iInstructionCount = pb.GetInstructionCount ();
                    iLineCount = pb.GetLineCount ();
                    pb.BufferPrint (bSerial);
                    pb.ClearBuffer ();
                }
                #endregion

                #region Obsolete test code
                //WrapperBase wb = null;

                //if (bSerial)
                //{
                //    wb = new SerialWrapper ();
                //    string s = wb.ToString ();
                //    s = ((SerialWrapper)wb).ToString ();
                //    //((SerialWrapper)wb).OpenComPort (123);
                //    //SerialWrapper sw = new SerialWrapper ();
                //    //sw.OpenComPort (123);
                //    //PlotTestSerial ();
                //}
                //else
                //{
                //    wb = new ParallelWrapper ();
                //    string s = wb.ToString ();
                //    s = ((ParallelWrapper)wb).ToString ();
                //    //PlotTestParallel ();
                //}

                //if (bSerial)
                //{
                //    wb.WriteTextString (CHPGL.EscReset ());
                //    string strResponse = wb.QueryPlotter (CHPGL.EscOutputBufferSpace ());
                //    Console.WriteLine ("EscOutputBufferSpace: " + strResponse);
                //    strResponse = wb.QueryPlotter (CHPGL.EscOutputExtendedError ());
                //    Console.WriteLine ("EscOutputExtendedError: " + strResponse);
                //    strResponse = wb.QueryPlotter (CHPGL.EscOutputBufferSize ());
                //    Console.WriteLine ("EscOutputBufferSize: " + strResponse);
                //    strResponse = wb.QueryPlotter (CHPGL.EscOutputExtendedStatus ());
                //    Console.WriteLine ("EscOutputExtendedStatus: " + strResponse);
                //}

                //String strPortName = wb.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1));
                //strPortName = wb.WriteTextString (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (0) + CHPGL.PlotAbsolute (0, 0));

                //// Select pen, set starting point
                //strPortName = wb.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (2));

                //// Draw circles, lots of them
                //strPortName = wb.WriteTextString (CHPGL.PlotAbsolute (400, 400) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (3) + CHPGL.PlotAbsolute (400, 1200) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (4) + CHPGL.PlotAbsolute (400, 2000) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (5) + CHPGL.PlotAbsolute (400, 2800) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (6) + CHPGL.PlotAbsolute (400, 1200) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (7) + CHPGL.PlotAbsolute (400, 2000) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (8) + CHPGL.PlotAbsolute (400, 2800) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (3) + CHPGL.PlotAbsolute (400, 1200) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (4) + CHPGL.PlotAbsolute (400, 2000) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (5) + CHPGL.PlotAbsolute (400, 2800) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (6) + CHPGL.PlotAbsolute (400, 1200) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (7) + CHPGL.PlotAbsolute (400, 2000) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (8) + CHPGL.PlotAbsolute (400, 2800) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (3) + CHPGL.PlotAbsolute (400, 1200) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (4) + CHPGL.PlotAbsolute (400, 2000) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (5) + CHPGL.PlotAbsolute (400, 2800) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (6) + CHPGL.PlotAbsolute (400, 1200) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (7) + CHPGL.PlotAbsolute (400, 2000) + CHPGL.Circle (400));
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (8) + CHPGL.PlotAbsolute (400, 2800) + CHPGL.Circle (400));

                //// Put pen back & close up
                //strPortName = wb.WriteTextString (CHPGL.SelectPen (0) + CHPGL.PlotAbsolute (0, 0) + CHPGL.Initialize ());

                //if (bSerial)
                //{
                //    if (wb.WaitForPlotter (1000, true))
                //    {
                //        string strStatus = wb.QueryPlotter (CHPGL.OutputIdentification ());
                //        Console.WriteLine ("OutputIdentification: " + strStatus);
                //        strStatus = wb.QueryPlotter (CHPGL.OutputStatus ());
                //        Console.WriteLine ("OutputStatus: " + strStatus);
                //        strStatus = wb.QueryPlotter (CHPGL.OutputFactors ());
                //        Console.WriteLine ("OutputFactors: " + strStatus);
                //        strStatus = wb.QueryPlotter (CHPGL.OutputError ());
                //        Console.WriteLine ("OutputError: " + strStatus);
                //        strStatus = wb.QueryPlotter (CHPGL.OutputActualPosition ());
                //        Console.WriteLine ("OutputActualPosition: " + strStatus);
                //        strStatus = wb.QueryPlotter (CHPGL.OutputCommandedPosition ());
                //        Console.WriteLine ("OutputCommandedPosition: " + strStatus);
                //        strStatus = wb.QueryPlotter (CHPGL.OutputOptions ());
                //        Console.WriteLine ("OutputOptions: " + strStatus);
                //        strStatus = wb.QueryPlotter (CHPGL.OutputHardClipLimits ());
                //        Console.WriteLine ("OutputHardClipLimits: " + strStatus);
                //    }
                //    else
                //    {
                //        Console.WriteLine ("WaitForPlotter () timed out.");
                //    }
                //}
                #endregion
            }
            catch (Exception e)
            {
                Console.WriteLine (e.ToString ());
                System.Threading.Thread.Sleep (5000);
            }
        }

        static void PlotTestParallel ()
        {
            ParallelWrapper pw = new ParallelWrapper ();

            String strPortName = pw.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1));
            strPortName = pw.WriteTextString (CHPGL.PlotAbsolute (600, 4000) + CHPGL.Circle (400));
            strPortName = pw.WriteTextString (CHPGL.SelectPen (0) + CHPGL.PlotAbsolute (0, 0));

            // Select pen, set starting point
            strPortName = pw.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1));

            // Draw circles
            strPortName = pw.WriteTextString (CHPGL.PlotAbsolute (400, 400) + CHPGL.Circle (400));
            strPortName = pw.WriteTextString (CHPGL.PlotAbsolute (400, 1200) + CHPGL.Circle (400));
            strPortName = pw.WriteTextString (CHPGL.PlotAbsolute (400, 2000) + CHPGL.Circle (400));
            strPortName = pw.WriteTextString (CHPGL.PlotAbsolute (400, 2800) + CHPGL.Circle (400));

            // Put pen back & close up
            strPortName = pw.WriteTextString (CHPGL.SelectPen (0) + CHPGL.PlotAbsolute (0, 0));
        }

        static void PlotTestSerial ()
        {
            SerialWrapper sw = new SerialWrapper ();
            sw.SetOutputTrace (true);
            String strPortName = sw.WriteTextString (CHPGL.Initialize () +
                                                     CHPGL.SelectPen (1) +
                                                     CHPGL.PlotAbsolute (600, 4000) +
                                                     CHPGL.Circle (400) +
                                                     CHPGL.SelectPen (0) +
                                                     CHPGL.PlotAbsolute (0, 0));
            sw.SetOutputTrace (false);

            // Select pen, set starting point
            strPortName = sw.WriteTextString (CHPGL.Initialize () + CHPGL.SelectPen (1));

            // Draw circles
            strPortName = sw.WriteTextString (CHPGL.PlotAbsolute (400, 400) + CHPGL.Circle (400));
            strPortName = sw.WriteTextString (CHPGL.PlotAbsolute (400, 1200) + CHPGL.Circle (400));
            strPortName = sw.WriteTextString (CHPGL.PlotAbsolute (400, 2000) + CHPGL.Circle (400));
            strPortName = sw.WriteTextString (CHPGL.PlotAbsolute (400, 2800) + CHPGL.Circle (400));

            // Put pen back & close up
            strPortName = sw.WriteTextString (CHPGL.SelectPen (0) + CHPGL.PlotAbsolute (0, 0));
            bool b = sw.CloseOutputPort ();
        }
    }
}