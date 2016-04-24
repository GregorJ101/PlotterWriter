using System;
//using System.Linq;
//using System.Activities;
//using System.Activities.Statements;

using WrapperBaseClass;
using ParallelPortWriter;
using SerialPortWriter;
using PlotterBuffer;

namespace PlotterWritterDLLTester
{
    class Program
    {
        static void Main (string[] args)
        {
            //Activity workflow1 = new Workflow1 ();
            //WorkflowInvoker.Invoke (workflow1);

            CPlotterBuffer pb = new CPlotterBuffer ();
            int i = pb.BufferEnqueue ("");
            pb.BufferSort ();
            pb.GetLineCount ();
            pb.GetBufferSize ();
            pb.ClearBuffer ();
            String s = pb.BufferPrint (true);

            CPlotterTester pt = new CPlotterTester ();
            pt.TestPlotter (true);
        }
    }
}
