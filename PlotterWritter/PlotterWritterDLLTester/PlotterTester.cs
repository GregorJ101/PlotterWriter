using System;
using System.Drawing;

using WrapperBaseClass;
using ParallelPortWriter;
using SerialPortWriter;
using PlotterBuffer;
using PlotterTester;
using HPGL;

namespace PlotterWritterDLLTester
{
    class CPlotterTesterApp
    {
        static void Main (string[] args)
        {
            Point ptStart = new Point (0, 0);

            //int iTestAngle = 0;
            //int iTestRadius = 0;
            //for (int iAngle = 0; iAngle < 360; iAngle += 15)
            //{
            //    Point ptEnd = CPlotterMath.PolarToCartesian (ptStart, iAngle, 100);
            //    CPlotterMath.CartesianToPolar (ptStart, ptEnd, ref iTestAngle, ref iTestRadius);
            //    Point ptTest = CPlotterMath.PolarToCartesian (ptStart, iTestAngle, iTestRadius);
            //    Console.WriteLine (string.Format ("{0, 4:D} degrees  X: {1, 4:D}  Y: {2, 4:D} -> {3, 4:D} {4, 4:D} ( {5, 4:D} {6, 4:D})",
            //                                      iAngle, ptEnd.X, ptEnd.Y, ptTest.X, ptTest.Y, ptEnd.X - ptTest.X, ptEnd.Y - ptTest.Y));
            //    //Console.WriteLine (string.Format ("{0, 4:D} degrees  X: {1, 4:D}  Y: {2, 4:D} -> {3, 4:D} {4, 4:D}", iAngle, ptEnd.X, ptEnd.Y, iTestAngle, iTestRadius));
            //    //Console.WriteLine (string.Format ("{0, 4:D} degrees  {3, 4:D} {4, 4:D}", iAngle, ptEnd.X, ptEnd.Y, iTestAngle, iAngle - iTestAngle));
            //}

            bool bOutputMode = CPlotterTester.PARALLEL;
            CPlotterTester pt = new CPlotterTester (bOutputMode);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LINE_ART_1);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LINE_ART_2);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LINE_ART_3);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LINE_ART_4);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LINE_ART_5);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LINE_ART_6);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LINE_ART_7);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LINE_ART_8);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LINE_ART_9);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LINE_ART_10);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LINE_ART_11);
            pt.TestPlotter (bOutputMode, CPlotterTester.ROTATE_SQUARE);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_SINE_WAVE);
            pt.TestPlotter (bOutputMode, CPlotterTester.DRAW_LISSAJOUS);

            CPlotterShapes.DrawFourQuadrants (0, 1300, -7500, -7500, 16,
                                              EPenSelect.ESelectPenRandom, false,
                                              EPenSelect.ESelectPenRandom, false,
                                              EPenSelect.ESelectPenRandom, false, bOutputMode);
            CPlotterShapes.DrawThickFrame (0,    10408, 0,    7500,  5, EPenSelect.ESelectPen1, false);
            CPlotterShapes.DrawThickFrame (0,    10408, 0,    16640, 1, EPenSelect.ESelectPen1, false);
            CPlotterShapes.DrawThickFrame (6000, 8000,  6000, 7500,  5, EPenSelect.ESelectPen1, false);
            CPlotterShapes.DrawRadialLines (0, 5000, 0, 5000, 10, 5, EPenSelect.ESelectPen1, false);
            CPlotterShapes.DrawTriangle (0, 0, 0, 5000, 4000, 2500, 15, EPenSelect.ESelectPen1, false);
        }
    }
}
