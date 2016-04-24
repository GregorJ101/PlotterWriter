using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WrapperBaseClass;
using ParallelPortWriter;
using SerialPortWriter;

namespace PlotterBuffer
{
    public class CPlotterTester
    {
        public void TestPlotter (bool bSerial)
        {
            try
            {
                WrapperBase wb = null;

                if (bSerial)
                {
                    wb = new SerialWrapper ();
                    string s = wb.ToString ();
                    s = ((SerialWrapper)wb).ToString ();
                    //((SerialWrapper)wb).OpenComPort (123);
                    //SerialWrapper sw = new SerialWrapper ();
                    //sw.OpenComPort (123);
                    //PlotTestSerial ();
                }
                else
                {
                    wb = new ParallelWrapper ();
                    string s = wb.ToString ();
                    s = ((ParallelWrapper)wb).ToString ();
                    //PlotTestParallel ();
                }

                String strPortName = wb.WriteTextString ("IN; SP1;");
                strPortName = wb.WriteTextString ("PA600,4000; CI400;");
                strPortName = wb.WriteTextString ("SP0; PA0,0");

                // Select pen, set starting point
                strPortName = wb.WriteTextString ("IN; SP1;");

                // Draw circles
                strPortName = wb.WriteTextString ("PA400,400; CI400;");
                strPortName = wb.WriteTextString ("PA400,1200; CI400;");
                strPortName = wb.WriteTextString ("PA400,2000; CI400;");
                strPortName = wb.WriteTextString ("PA400,2800; CI400;");

                // Put pen back & close up
                strPortName = wb.WriteTextString ("SP0; PA0,0");
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
            String strPortName = pw.WriteTextString ("IN; SP1;");
            strPortName = pw.WriteTextString ("PA600,4000; CI400;");
            strPortName = pw.WriteTextString ("SP0; PA0,0");

            // Select pen, set starting point
            strPortName = pw.WriteTextString ("IN; SP1;");

            // Draw circles
            strPortName = pw.WriteTextString ("PA400,400; CI400;");
            strPortName = pw.WriteTextString ("PA400,1200; CI400;");
            strPortName = pw.WriteTextString ("PA400,2000; CI400;");
            strPortName = pw.WriteTextString ("PA400,2800; CI400;");

            // Put pen back & close up
            strPortName = pw.WriteTextString ("SP0; PA0,0");
        }

        static void PlotTestSerial ()
        {
            SerialWrapper sw = new SerialWrapper ();
            //string s = sw.OpenComPort ();
            sw.SetOutputTrace (true);
            String strPortName = sw.WriteTextString ("IN; SP1; PA600,4000; CI400; SP0; PA0,0");
            sw.SetOutputTrace (false);
            //strPortName = sw.OpenNamedPort (9);
            //strPortName = sw.WriteTextString ("IN; SP1; PA600,2000; CI400; SP0; PA0,0");

            // Select pen, set starting point
            strPortName = sw.WriteTextString ("IN; SP1;");

            // Draw circles
            strPortName = sw.WriteTextString ("PA400,400; CI400;");
            strPortName = sw.WriteTextString ("PA400,1200; CI400;");
            strPortName = sw.WriteTextString ("PA400,2000; CI400;");
            strPortName = sw.WriteTextString ("PA400,2800; CI400;");

            // Put pen back & close up
            strPortName = sw.WriteTextString ("SP0; PA0,0");
            bool b = sw.CloseOutputPort ();
        }
    }

    public class CPlotterBuffer
    {
        public int BufferEnqueue (String strCommand)
        {
            return 0;
        }

        public void BufferSort ()
        {
        }

        public String BufferPrint (bool bSerial)
        {
            return "";
        }
        public int GetLineCount ()
        {
            return 0;
        }

        public int GetBufferSize ()
        {
            return 0;
        }

        public void ClearBuffer ()
        {
        }
    }
}
