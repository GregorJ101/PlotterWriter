using System;

using WrapperBaseClass;
using ParallelPortWriter;
using SerialPortWriter;
using PlotterBuffer;
using PlotterTester;

namespace PlotterWritterDLLTester
{
    class CPlotterTesterApp
    {
        const bool SERIAL      = true;
        const bool PARALLEL    = false;

        static void Main (string[] args)
        {
            CPlotterTester pt = new CPlotterTester ();
            pt.TestPlotter (SERIAL, CPlotterTester.TEST_PROGRAMS_2);
        }
    }
}
