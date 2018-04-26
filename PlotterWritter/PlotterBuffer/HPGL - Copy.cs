#define OLD_VERSION
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

using WrapperBaseClass;
using SerialPortWriter;

namespace HPGL
{
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
                fY <  99.0F)
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

    public static class CHPGL
    {
        //const int  TOGGLE_PEN = -99;
        const char LF         = '\x0A';
        const char CR         = '\x0D';
        const char ETX        = '\x03';

        private static char s_cLabelTerminator = ETX; // ETX

        #region VALIDATION METHODS:
        public static int ValidateX (int iX, int iXLimit = 16640)
        {
            if (iX < 0)
                iX = 0;
            else if (iX > iXLimit)
                iX      = iXLimit;

            return iX;
        }

        public static int ValidateY (int iY, int iYLimit = 11040)
        {
            if (iY < 0)
                iY = 0;
            else if (iY > iYLimit)
                iY      = iYLimit;

            return iY;
        }
        #endregion

        #region STATIC CONFIGURATION COMMANDS:
        public static string EscPlotterConfiguration () // See Paragraph 6.4.6.
        {
            return string.Format ("\x1B.@:");
        }

        public static string EscPlotterConfiguration (int iBuffer) // See Paragraph 6.4.6.
        {
            return string.Format ("\x1B.@{0}:", iBuffer);
        }

        public static string EscPlotterConfiguration (int iBuffer, int iMode) // See Paragraph 6.4.6.
        {
            if (iBuffer < 0 && iMode < 0)
            {
                return string.Format ("\x1B.@:");
            }
            else if (iBuffer > -1 && iMode < 0)
            {
                return string.Format ("\x1B.@{0}:", iBuffer);
            }
            else if (iBuffer < 0 && iMode > -1)
            {
                return string.Format ("\x1B.@;{0}:", iMode);
            }
            else if (iBuffer > -1 && iMode > -1)
            {
                return string.Format ("\x1B.@{0};{1}:", iBuffer, iMode);
            }

            return string.Format ("\x1B.@:"); // redundant default case to keep the compiler happy
        }

        public static string EscSetHandshakeMode1 () // Only default version implemented
        {
            return "\x1B.H:";
        }

        public static string EscSetHandshakeMode2 () // Only default version implemented
        {
            return "\x1B.I:";
        }

        public static string EscReset () // Full support.
        {
            return "\x1B.R";
        }

        public static string EscSetOutputMode () // Full support.
        {
            return string.Format ( "\x1B.M:");
        }

        //public static string EscSetOutputMode (int iTurnaround, int iTrigger, int iEcho, int iTerminator1, int iTerminator2, int iInitiator) // Full support.
        //{
        //    return string.Format ( "\x1B.M{0};{1};{2};{3};{4};{5}:", iTurnaround, iTrigger, iEcho, iTerminator1, iTerminator2, iInitiator);
        //}

        public static string SetExtendedOutputAndHandshakeMode () // Only default version implemented
        {
            return "\x1B.N:";
        }
        #endregion

        #region STATIC OUTPUT FUNCTIONS:
        public static string EscOutputBufferSpace () // Full support.
        {
            return "\x1B.B";
        }

        public static string EscOutputExtendedError () // Full support.
        {
            return "\x1B.E";
        }

        public static string EscOutputBufferSize () // See Paragraph 6.4.6.
        {
            return "\x1B.L";
        }

        public static string EscOutputExtendedStatus () // Full support.
        {
            return "\x1B.O";
        }
        #endregion

        #region ABORT FUNCTIONS:
        public static string EscAbortDeviceControl () //  Full support.
        {
            return "\x1B.J";
        }

        public static string EscAbortGraphicControl () //  Full support.
        {
            return "\x1B.K";
        }
        #endregion

        #region STATIC CONFIGURATION INSTRUCTIONS:
        // Paper Size  Pl        P2            Format
        // ----------  --------  ------------  ------
        // A           250, 596  10250,  7796  US 8 1/2" x 11"
        // A4          603, 52I  10603,  7721  Metric
        // B           522, 259  15722, 10259  US 11" x 17"
        // A3          170, 602  15370, 10602  Metric

        public static string Initialize () // See Paragraph. 6.4.4.
        {
            return "IN;";
        }

        public static string InputMask () // See Paragraph 6.4.2.
        {
            return "IM;";
        }

        public static string InputMask (int iMask) // See Paragraph 6.4.2.
        {
            if (iMask < 0)
                iMask = 0;
            else if (iMask > 255)
                iMask = 223; // default value if no argument specified

            return string.Format ("IM{0};", iMask);
        }

        public static string SetDefaultValues () // See Paragraph 6.4.4.
        {
            return "DF;";
        }

        public static string InputWindow () // Full support.
        {
            return string.Format ("IW;");
        }

        public static string InputWindow (int iXll, int iYll, int iXur, int iYur) // Full support.
        {
            iXll = ValidateX (iXll);
            iYll = ValidateY (iYll);

            iXur = ValidateX (iXur);
            iYur = ValidateY (iYur);

            return string.Format ("IW{0},{1},{2},{3};", iXll, iYll, iXur, iYur);
        }

        public static string OutputWindow () // Full support.
        {
            return "OW;";
        }

        public static string InputP1andP2 () // Full support.
        {
            return string.Format ("IP;");
        }

        public static string InputP1andP2 (int iP1x, int iP1y) // Full support.
        {
            iP1x = ValidateX (iP1x);
            iP1y = ValidateY (iP1y);

            return string.Format ("IP{0},{1};", iP1x, iP1y);
        }

        public static string InputP1andP2 (int iP1x, int iP1y, int iP2x, int iP2y) // Full support.
        {
            iP1x = ValidateX (iP1x);
            iP1y = ValidateY (iP1y);

            iP2x = ValidateX (iP2x);
            iP2y = ValidateY (iP2y);

            return string.Format ("IP{0},{1},{2},{3};", iP1x, iP1y, iP2x, iP2y);
        }

        public static string OutputP1andP2 () // Full support.
        {
            return "OP;";
        }

        public static string Scale () // Full support.
        {
            return "SC;";
        }

        public static string Scale (int iX1, int iX2, int iY1, int iY2) // Full support.
        {
            return string.Format ("SC{0},{1},{2},{3};", iX1, iX2, iY1, iY2);
        }

        public static string RotateCoordinateSystem () // Full support.
        {
            return "RO;";
        }

        public static string RotateCoordinateSystem (int iAngle) // Full support.
        {
            if (iAngle != 0 &&
                iAngle != 90)
            {
                iAngle = (iAngle > 45) ? 90 : 0;
            }

            return string.Format ("RO{0};", iAngle);
        }

        public static string PaperSize (int iSize) // Full support.
        {
            if (iSize < 0)
                iSize = 0;
            else if (iSize > 127)
                iSize = 127;

            return string.Format ("PS{0};", iSize);
        }
        #endregion

        #region STATIC PEN INSTRUCTIONS:
        public static string PenDown () // Full support.
        {
            return "PD;";
        }

        public static string PenDown (int iX, int iY) // Full support.
        {
            return string.Format ("PD{0},{1};", iX, iY);
        }

        public static string PenDown (int iX1, int iY1, int iX2, int iY2) // Full support.
        {
            return string.Format ("PD{0},{1},{2},{3};", iX1, iY1, iX2, iY2);
        }

        public static string PenDown (int iX1, int iY1, int iX2, int iY2, int iX3, int iY3) // Full support.
        {
            return string.Format ("PD{0},{1},{2},{3},{4},{5};", iX1, iY1, iX2, iY2, iX3, iY3);
        }

        public static string PenDown (int iX1, int iY1, int iX2, int iY2, int iX3, int iY3, int iX4, int iY4) // Full support.
        {
            return string.Format ("PD{0},{1},{2},{3},{4},{5},{6},{7};", iX1, iY1, iX2, iY2, iX3, iY3, iX4, iY4);
        }

        public static string PenDown (int iX1, int iY1, int iX2, int iY2, int iX3, int iY3, int iX4, int iY4, int iX5, int iY5) // Full support.
        {
            return string.Format ("PD{0},{1},{2},{3},{4},{5},{6},{7},{8},{9};", iX1, iY1, iX2, iY2, iX3, iY3, iX4, iY4, iX5, iY5);
        }

        public static string PenDown (float fX, float fY) // Full support.
        {
            return string.Format ("PD{0:#####0.####},{1:#####0.####};", fX, fY);
        }

        public static string PenDown (float fX1, float fY1, float fX2, float fY2) // Full support.
        {
            return string.Format ("PD{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####};", fX1, fY1, fX2, fY2);
        }

        public static string PenDown (float fX1, float fY1, float fX2, float fY2, float fX3, float fY3) // Full support.
        {
            return string.Format ("PD{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####},{4:#####0.####},{5:#####0.####};", fX1, fY1, fX2, fY2, fX3, fY3);
        }

        public static string PenDown (float fX1, float fY1, float fX2, float fY2, float fX3, float fY3, float fX4, float fY4) // Full support.
        {
            return string.Format ("PD{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####},{4:#####0.####},{5:#####0.####},{6:#####0.####},{7:#####0.####};", fX1, fY1, fX2, fY2, fX3, fY3, fX4, fY4);
        }

        public static string PenDown (float fX1, float fY1, float fX2, float fY2, float fX3, float fY3, float fX4, float fY4, float fX5, float fY5) // Full support.
        {
            return string.Format ("PD{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####},{4:#####0.####},{5:#####0.####},{6:#####0.####},{7:#####0.####},{8:#####0.####},{9:#####0.####};", fX1, fY1, fX2, fY2, fX3, fY3, fX4, fY4, fX5, fY5);
        }

        public static string PenDown (List<KeyValuePair<int, int>> lkvpInt) // Full support.
        {
            return FormatIntPairList ("PD", lkvpInt);
        }

        public static string PenDown (List<KeyValuePair<float, float>> lkvpFloat) // Full support.
        {
            return FormatFloatPairList ("PD", lkvpFloat);
        }

        public static string PenUp () // Full support.
        {
            return "PU;";
        }

        public static string PenUp (int iX, int iY) // Full support.
        {
            return string.Format ("PU{0},{1};", iX, iY);
        }

        public static string PenUp (float fX, float fY) // Full support.
        {
            return string.Format ("PU{0:#####0.####},{1:#####0.####};", fX, fY);
        }

        public static string PenUp (List<KeyValuePair<int, int>> lkvpInt) // Full support.
        {
            return FormatIntPairList ("PU", lkvpInt);
        }

        public static string PenUp (List<KeyValuePair<float, float>> lkvpFloat) // Full support.
        {
            return FormatFloatPairList ("PU", lkvpFloat);
        }

        public static string SelectPen () // See Paragraph 6.4.3.
        {
            return "SP;";
        }

        public static string SelectPen (int iPen) // See Paragraph 6.4.3.
        {
            if (iPen < 0)
                iPen = 0;
            else if (iPen > 8)
                iPen = 8;

            return string.Format ("SP{0};", iPen);
        }

        public static string PlotAbsolute () // Full support.
        {
            return "PA;";
        }

        public static string PlotAbsolute (int iX, int iY) // Full support.
        {
            return string.Format ("PA{0},{1};", iX, iY);
        }

        public static string PlotAbsolute (int iX1, int iY1, int iX2, int iY2) // Full support.
        {
            return string.Format ("PA{0},{1},{2},{3};", iX1, iY1, iX2, iY2);
        }

        public static string PlotAbsolute (int iX1, int iY1, int iX2, int iY2, int iX3, int iY3) // Full support.
        {
            return string.Format ("PA{0},{1},{2},{3},{4},{5};", iX1, iY1, iX2, iY2, iX3, iY3);
        }

        public static string PlotAbsolute (int iX1, int iY1, int iX2, int iY2, int iX3, int iY3, int iX4, int iY4) // Full support.
        {
            return string.Format ("PA{0},{1},{2},{3},{4},{5},{6},{7};", iX1, iY1, iX2, iY2, iX3, iY3, iX4, iY4);
        }

        public static string PlotAbsolute (int iX1, int iY1, int iX2, int iY2, int iX3, int iY3, int iX4, int iY4, int iX5, int iY5) // Full support.
        {
            return string.Format ("PA{0},{1},{2},{3},{4},{5},{6},{7},{8},{9};", iX1, iY1, iX2, iY2, iX3, iY3, iX4, iY4, iX5, iY5);
        }

        public static string PlotAbsolute (float fX, float fY) // Full support.
        {
            return string.Format ("PA{0:#####0.####},{1:#####0.####};", fX, fY);
        }

        public static string PlotAbsolute (float fX1, float fY1, float fX2, float fY2) // Full support.
        {
            return string.Format ("PA{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####};", fX1, fY1, fX2, fY2);
        }

        public static string PlotAbsolute (float fX1, float fY1, float fX2, float fY2, float fX3, float fY3) // Full support.
        {
            return string.Format ("PA{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####},{4:#####0.####},{5:#####0.####};", fX1, fY1, fX2, fY2, fX3, fY3);
        }

        public static string PlotAbsolute (float fX1, float fY1, float fX2, float fY2, float fX3, float fY3, float fX4, float fY4) // Full support.
        {
            return string.Format ("PA{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####},{4:#####0.####},{5:#####0.####},{6:#####0.####},{7:#####0.####};", fX1, fY1, fX2, fY2, fX3, fY3, fX4, fY4);
        }

        public static string PlotAbsolute (float fX1, float fY1, float fX2, float fY2, float fX3, float fY3, float fX4, float fY4, float fX5, float fY5) // Full support.
        {
            return string.Format ("PA{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####},{4:#####0.####},{5:#####0.####},{6:#####0.####},{7:#####0.####},{8:#####0.####},{9:#####0.####};", fX1, fY1, fX2, fY2, fX3, fY3, fX4, fY4, fX5, fY5);
        }

        public static string PlotAbsolute (List<KeyValuePair<int, int>> lkvpInt) // Full support.
        {
            return FormatIntPairList ("PA", lkvpInt);
        }

        public static string PlotAbsolute (List<KeyValuePair<float, float>> lkvpFloat) // Full support.
        {
            return FormatFloatPairList ("PA", lkvpFloat);
        }

        public static string PlotRelative () // Full support.
        {
            return "PR;";
        }

        public static string PlotRelative (int iX, int iY) // Full support.
        {
            return string.Format ("PR{0},{1};", iX, iY);
        }

        public static string PlotRelative (int iX1, int iY1, int iX2, int iY2) // Full support.
        {
            return string.Format ("PR{0},{1},{2},{3};", iX1, iY1, iX2, iY2);
        }

        public static string PlotRelative (int iX1, int iY1, int iX2, int iY2, int iX3, int iY3) // Full support.
        {
            return string.Format ("PR{0},{1},{2},{3},{4},{5};", iX1, iY1, iX2, iY2, iX3, iY3);
        }

        public static string PlotRelative (int iX1, int iY1, int iX2, int iY2, int iX3, int iY3, int iX4, int iY4) // Full support.
        {
            return string.Format ("PR{0},{1},{2},{3},{4},{5},{6},{7};", iX1, iY1, iX2, iY2, iX3, iY3, iX4, iY4);
        }

        public static string PlotRelative (int iX1, int iY1, int iX2, int iY2, int iX3, int iY3, int iX4, int iY4, int iX5, int iY5) // Full support.
        {
            return string.Format ("PR{0},{1},{2},{3},{4},{5},{6},{7},{8},{9};", iX1, iY1, iX2, iY2, iX3, iY3, iX4, iY4, iX5, iY5);
        }

        public static string PlotRelative (float fX, float fY) // Full support.
        {
            return string.Format ("PR{0:#####0.####},{1:#####0.####};", fX, fY);
        }

        public static string PlotRelative (float fX1, float fY1, float fX2, float fY2) // Full support.
        {
            return string.Format ("PR{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####};", fX1, fY1, fX2, fY2);
        }

        public static string PlotRelative (float fX1, float fY1, float fX2, float fY2, float fX3, float fY3) // Full support.
        {
            return string.Format ("PR{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####},{4:#####0.####},{5:#####0.####};", fX1, fY1, fX2, fY2, fX3, fY3);
        }

        public static string PlotRelative (float fX1, float fY1, float fX2, float fY2, float fX3, float fY3, float fX4, float fY4) // Full support.
        {
            return string.Format ("PR{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####},{4:#####0.####},{5:#####0.####},{6:#####0.####},{7:#####0.####};", fX1, fY1, fX2, fY2, fX3, fY3, fX4, fY4);
        }

        public static string PlotRelative (float fX1, float fY1, float fX2, float fY2, float fX3, float fY3, float fX4, float fY4, float fX5, float fY5) // Full support.
        {
            return string.Format ("PR{0:#####0.####},{1:#####0.####},{2:#####0.####},{3:#####0.####},{4:#####0.####},{5:#####0.####},{6:#####0.####},{7:#####0.####},{8:#####0.####},{9:#####0.####};", fX1, fY1, fX2, fY2, fX3, fY3, fX4, fY4, fX5, fY5);
        }

        public static string PlotRelative (List<KeyValuePair<int, int>> lkvpInt) // Full support.
        {
            return FormatIntPairList ("PR", lkvpInt);
        }

        public static string PlotRelative (List<KeyValuePair<float, float>> lkvpFloat) // Full support.
        {
            return FormatFloatPairList ("PR", lkvpFloat);
        }

        public static string VelocitySelect () // Full support.
        {
            return "VS;";
        }

        public static string VelocitySelect (float fSpeed) // Full support.
        {
            return string.Format ("VS{0:#####0.####};", fSpeed);
        }

        //public static string VelocityAdaptive VA; NOP, // nothing happens.
        //public static string VelocityNormal VN; NOP, // nothing happens.
        //public static string AutomaticPenPickup AP; NOP, // nothing happens.
        #endregion

        #region STATIC LINE INSTRUCTIONS:
        public static string DesignateLine () // Full support.
        {
            return "LT;";
        }

        public static string DesignateLine (int iPatternNumber) // Full support.
        {
            if (iPatternNumber < 0)
                iPatternNumber = 0;
            else if (iPatternNumber > 6)
                iPatternNumber = 6;

            return string.Format ("LT{0};", iPatternNumber);
        }

        public static string DesignateLine (int iPatternNumber, float fLength) // Full support.
        {
            if (iPatternNumber < 0)
                iPatternNumber = 0;
            else if (iPatternNumber > 6)
                iPatternNumber = 6;

            if (fLength < 0.0F)
                fLength = 0.0F;
            else if (fLength > 127.9999F)
                fLength = 127.9999F;

            return string.Format ("LT{0},{1:#####0.####};", iPatternNumber, fLength);
        }

        public static string TickLength (int iPositive) // Full support.
        {
            if (iPositive < -128)
                iPositive = -128;
            else if (iPositive > 127)
                iPositive = 127;

            return string.Format ("TL{0};", iPositive);
        }

        public static string TickLength (float fPositive) // Full support.
        {
            if (fPositive < -128.0F)
                fPositive = -128.0F;
            else if (fPositive > 127.9999F)
                fPositive = 127.9999F;

            return string.Format ("TL{0:#####0.####};", fPositive);
        }

        public static string TickLength (int iPositive, int iNegative) // Full support.
        {
            if (iPositive < -128)
                iPositive = -128;
            else if (iPositive > 127)
                iPositive = 127;

            if (iNegative < -128)
                iNegative = -128;
            else if (iNegative > 127)
                iNegative = 127;

            return string.Format ("TL{0},{1};", iPositive, iNegative);
        }

        public static string TickLength (float fPositive, float fNegative) // Full support.
        {
            if (fPositive < -128.0F)
                fPositive = -128.0F;
            else if (fPositive > 127.9999F)
                fPositive = 127.9999F;

            if (fNegative < -128.0F)
                fNegative = -128.0F;
            else if (fNegative > 127.9999F)
                fNegative = 127.9999F;

            return string.Format ("TL{0:#####0.####},{1:#####0.####};", fPositive, fNegative);
        }

        public static string XAxisTick () // Full support.
        {
            return "XT;";
        }

        public static string YAxisTick () // Full support.
        {
            return "YT;";
        }
        #endregion

        #region STATIC CURVE INSTRUCTIONS:
        public static string Circle (int iRadius) // Full support.
        {
            return string.Format ("CI{0};", iRadius);
        }

        public static string Circle (float fRadius) // Full support.
        {
            return string.Format ("CI{0:#####0.####};", fRadius);
        }

        public static string Circle (int iRadius, int iChord) // Full support.
        {
            return string.Format ("CI{0},{1};", iRadius, iChord);
        }

        public static string Circle (float fRadius, int iChord) // Full support.
        {
            return string.Format ("CI{0:#####0.####},{1};", fRadius, iChord);
        }

        public static string ArcAbsolute (int iX, int iY, int iAngle) // Full support.
        {
            return string.Format ("AA{0},{1},{2};", iX, iY, iAngle);
        }

        public static string ArcAbsolute (int iX, float fY, int iAngle) // Full support.
        {
            return string.Format ("AA{0},{1:#####0.####},{2};", iX, fY, iAngle);
        }

        public static string ArcAbsolute (float fX, int iY, int iAngle) // Full support.
        {
            return string.Format ("AA{0:#####0.####},{1},{2};", fX, iY, iAngle);
        }

        public static string ArcAbsolute (float fX, float fY, int iAngle) // Full support.
        {
            return string.Format ("AA{0:#####0.####},{1:#####0.####},{2};", fX, fY, iAngle);
        }

        public static string ArcAbsolute (int iX, int iY, int iAngle, int iChord) // Full support.
        {
            return string.Format ("AA{0},{1},{2},{3};", iX, iY, iAngle, iChord);
        }

        public static string ArcAbsolute (int iX, float fY, int iAngle, int iChord) // Full support.
        {
            return string.Format ("AA{0},{1:#####0.####},{2},{3};", iX, fY, iAngle, iChord);
        }

        public static string ArcAbsolute (float fX, int iY, int iAngle, int iChord) // Full support.
        {
            return string.Format ("AA{0:#####0.####},{1},{2},{3};", fX, iY, iAngle, iChord);
        }

        public static string ArcAbsolute (float fX, float fY, int iAngle, int iChord) // Full support.
        {
            return string.Format ("AA{0:#####0.####},{1:#####0.####},{2},{3};", fX, fY, iAngle, iChord);
        }

        public static string ArcRelative (int iX, int iY, int iAngle) // Full support.
        {
            return string.Format ("AR{0},{1},{2};", iX, iY, iAngle);
        }

        public static string ArcRelative (int iX, float fY, int iAngle) // Full support.
        {
            return string.Format ("AR{0},{1:#####0.####},{2};", iX, fY, iAngle);
        }

        public static string ArcRelative (float fX, int iY, int iAngle) // Full support.
        {
            return string.Format ("AR{0:#####0.####},{1},{2};", fX, iY, iAngle);
        }

        public static string ArcRelative (float fX, float fY, int iAngle) // Full support.
        {
            return string.Format ("AR{0:#####0.####},{1:#####0.####},{2};", fX, fY, iAngle);
        }

        public static string ArcRelative (int iX, int iY, int iAngle, int iChord) // Full support.
        {
            return string.Format ("AR{0},{1},{2},{3};", iX, iY, iAngle, iChord);
        }

        public static string ArcRelative (int iX, float fY, int iAngle, int iChord) // Full support.
        {
            return string.Format ("AR{0},{1:#####0.####},{2},{3};", iX, fY, iAngle, iChord);
        }

        public static string ArcRelative (float fX, int iY, int iAngle, int iChord) // Full support.
        {
            return string.Format ("AR{0:#####0.####},{1},{2},{3};", fX, iY, iAngle, iChord);
        }

        public static string ArcRelative (float fX, float fY, int iAngle, int iChord) // Full support.
        {
            return string.Format ("AR{0:#####0.####},{1:#####0.####},{2},{3};", fX, fY, iAngle, iChord);
        }
        #endregion

        #region STATIC CHARACTER INSTRUCTIONS:
        // Set No.  Description                                ISO Registration Number
        // -------  -----------------------------------------  -----------------------
        // Set O    ANSI ASCII                                 006
        // Set 1    9825 Character Set
        // Set 2    French/German
        // Set 3    Scandinavian
        // Set 4    Spanish/Latin American
        // Set 6    JIS ASCII                                  014
        // Set 7    Roman Extensions
        // Set 8    Katakana                                   013
        // Set 9    ISO IRV (International Reference Version)  002
        // Set 30   ISO Swedish                                010
        // Set 31   ISO Swedish For Names                      011
        // Set 32   ISO Norway, Version 1                      060
        // Set 33   ISO German                                 021
        // Set 34   ISO French                                 025
        // Set 35   ISO United Kingdom                         004
        // Set 36   ISO Italian                                015
        // Set 37   ISO Spanish                                017
        // Set 38   ISO Portuguese                             016
        // Set 39   ISO Norway, Version 2                      061

        public static string DesignateStandardCharacterSet () // See Paragraph 6.4.4.
        {
            return "CS;";
        }
                
        public static string DesignateStandardCharacterSet (int iCharSet) // See Paragraph 6.4.4.
        {
            if (iCharSet < 0)
                iCharSet = 0;
            else if (iCharSet > 39)
                iCharSet = 39;
            else if (iCharSet > 4 && iCharSet < 6)
                iCharSet = 4;
            else if (iCharSet > 9 && iCharSet < 30)
                iCharSet = 9;

            return string.Format ("CS{0};", iCharSet);
        }
                
        public static string DesignateAlternateCharacterSet () // See Paragraph 6.4.4.
        {
            return "CA;";
        }
                
        public static string DesignateAlternateCharacterSet (int iCharSet) // See Paragraph 6.4.4.
        {
            if (iCharSet < 0)
                iCharSet = 0;
            else if (iCharSet > 39)
                iCharSet = 39;
            else if (iCharSet > 4 && iCharSet < 6)
                iCharSet = 4;
            else if (iCharSet > 9 && iCharSet < 30)
                iCharSet = 9;

            return string.Format ("CA{0};", iCharSet);
        }
                
        public static string SelectStandardCharacterSet () // Full support.
        {
            return "SS;";
        }

        public static string SelectAlternateCharacterSet () // Full support.
        {
            return "SA;";
        }
                
        //   7475 ASCII Code Definitions
        //   Decimal  Hexadecimal    ASCII
        //    Value      Value     Character  All Sets
        //   -------  -----------  ---------  ------------------------------
        //    0       00           NULL       No Operation (NOP)
        //    1       01           SOH        NOP
        //    2       02           STX        NOP
        //    3       03           ETX        End Label Instruction
        //    4       04           ETO        NOP
        //    5       05           ENQ        NOP
        //    6       06           ACK        NOP
        //    7       07           BEL        NOP
        //    8       08           BS         Backspace
        // *  9       09           HT         Horizontal Tab (1f2 backspace)
        //   10       0A           LF         Line Feed
        //   11       0B           VT         Inverse Line Feed
        //   12       0C           FF         NOP
        //   13       0D           CR         Carriage Return
        //   14       0E           SO         Select Alternate Character Set
        //   15       0F           SI         Select Standard Character Set
        //   16       10           DLE        NOP
        //   17       11           DC1        NOP
        //   18       12           DC2        NOP
        //   19       13           DC3        NOP
        //   20       14           DC4        NOP
        //   21       15           NAK        NOP
        //   22       16           SYN        NOP
        //   23       17           ETB        NOP
        //   24       18           CAN        NOP
        //   25       19           EM         NOP
        //   26       1A           SUB        NOP
        //   27       1B           ESC        NOP
        //   28       1C           FS         NOP
        //   29       1D           GS         NOP
        //   30       1E           RS         NOP
        //   31       1F           US         NOP
        //   32       20           SP         Space
        //   *Using control character horizontal tab (decimal 9) inside a label string
        //   moves the pen one-half character space back (equivalent to a CP -.5,0).
        //   Use this tab with character set 8, Katakana, where spacing between symbols
        //   can alter the meaning of the symbol and hence the word or phrase.                
        public static string DefineLabelTerminator () // Full support.
        {
            s_cLabelTerminator = ETX;
            return string.Format ("DT{0};", s_cLabelTerminator); // Set ETX, the default/initial value
        }

        public static string DefineLabelTerminator (char cTerminator) // Full support.
        {
            s_cLabelTerminator = cTerminator;
            return string.Format ("DT{0};", cTerminator);
        }
                
        public static string UserDefinedCharacter () // Full support.
        {
            return "UC;";
        }

        public static string UserDefinedCharacter (List<float> lfUdcPoints) // Full support.
        {
            return UserDefinedCharacter (lfUdcPoints, true);
        }

        public static string UserDefinedCharacter (List<float> lfUdcPoints, bool bClearPoints) // Full support.
        {
            StringBuilder sbldUdcInstruction = new StringBuilder ();
            sbldUdcInstruction.Append ("UC");
    
            foreach (float fl in lfUdcPoints)
            {
                if (sbldUdcInstruction.Length > 2)
                    sbldUdcInstruction.Append (',');
                sbldUdcInstruction.Append (string.Format ("{0:#####0.####}", fl));
            }

            sbldUdcInstruction.Append (';');

            if (bClearPoints)
                lfUdcPoints.Clear ();

            return sbldUdcInstruction.ToString ();
        }

        public static string SymbolMode () // Full support.
        {
            return "SM;";
        }
                
        public static string SymbolMode (char cSymbol) // Full support.
        {
            return string.Format ("SM{0};", cSymbol);
        }
                
        public static string Label (string strLabelText) // Full support.
        {
            if (strLabelText[strLabelText.Length - 1] == s_cLabelTerminator)
                return string.Format ("LB{0};", strLabelText);
            else
                return string.Format ("LB{0}{1};", strLabelText, s_cLabelTerminator);
        }

        public static string AbsoluteCharacterSize () // Full support.
        {
            return "SI;";
        }

        public static string AbsoluteCharacterSize (float fWidth, float fHeight) // Full support.
        {
            if (fWidth < -128.0F)
                fWidth = -128.0F;
            else if (fWidth > 127.9999F)
                fWidth = 127.9999F;

            if (fHeight < -128.0F)
                fHeight = -128.0F;
            else if (fHeight > 127.9999F)
                fHeight = 127.9999F;

            return string.Format ("SI{0:#####0.####},{1:#####0.####};", fWidth, fHeight);
        }

        public static string RelativeCharacterSize () // Full support.
        {
            return "SR;";
        }

        public static string RelativeCharacterSize (float fWidth, float fHeight) // Full support.
        {
            if (fWidth < -128.0F)
                fWidth = -128.0F;
            else if (fWidth > 127.9999F)
                fWidth = 127.9999F;

            if (fHeight < -128.0F)
                fHeight = -128.0F;
            else if (fHeight > 127.9999F)
                fHeight = 127.9999F;

            return string.Format ("SR{0:#####0.####},{1:#####0.####};", fWidth, fHeight);
        }

        public static string AbsoluteCharacterSlant () // Full support.
        {
            return "SL;";
        }

        public static string AbsoluteCharacterSlant (float fAngle) // Full support.
        {
            if (fAngle < -128.0F)
                fAngle = -128.0F;
            else if (fAngle > 127.9999F)
                fAngle = 127.9999F;

            return string.Format ("SL{0:#####0.####};", fAngle);
        }

        public static string CharacterPlot () // Full support.
        {
            return "CP;";
        }

        public static string CharacterPlot (float fSpaces, float fLines) // Full support.
        {
            if (fSpaces < -128.0F)
                fSpaces = -128.0F;
            else if (fSpaces > 127.9999F)
                fSpaces = 127.9999F;

            if (fLines < -128.0F)
                fLines = -128.0F;
            else if (fLines > 127.9999F)
                fLines = 127.9999F;

            return string.Format ("CP{0:#####0.####},{1:#####0.####};", fSpaces, fLines);
        }

        public static string AbsoluteDirection () // Full support.
        {
            return "DI;";
        }

        public static string AbsoluteDirection (float fRun, float fRise) // Full support.
        {
            if (fRun < -128.0F)
                fRun = -128.0F;
            else if (fRun > 127.9999F)
                fRun = 127.9999F;

            if (fRise < -128.0F)
                fRise = -128.0F;
            else if (fRise > 127.9999F)
                fRise = 127.9999F;

            return string.Format ("DI{0:#####0.####},{1:#####0.####};", fRun, fRise);
        }

        public static string RelativeDirection () // Full support.
        {
            return "DR;";
        }

        public static string RelativeDirection (float fRun, float fRise) // Full support.
        {
            if (fRun < -128.0F)
                fRun = -128.0F;
            else if (fRun > 127.9999F)
                fRun = 127.9999F;

            if (fRise < -128.0F)
                fRise = -128.0F;
            else if (fRise > 127.9999F)
                fRise = 127.9999F;

            return string.Format ("DR{0:#####0.####},{1:#####0.####};", fRun, fRise);
        }
        #endregion

        #region STATIC DIGITIZING INSTRUCTIONS:
        public static string DigitizePoint () // Full support.
        {
            return "DP;";
        }

        public static string OutputDigitizedPoint () // Full support.
        {
            return "OD;";
        }

        public static string DigitizeClear () // Full support.
        {
            return "DC;";
        }
        #endregion

        #region STATIC STATUS INSTRUCTIONS:
        public static string OutputIdentification () // See Paragraph 6.4.1.
        {
            return "OI;";
        }

        public static string OutputStatus () // Full support.
        {
            return "OS;";
        }

        public static string OutputFactors () // Full support.
        {
            return "OF;";
        }

        public static string OutputError () // Full support.
        {
            return "OE;";
        }

        public static string OutputActualPosition () // Full support.
        {
            return "OA;";
        }

        public static string OutputCommandedPosition () // Full support.
        {
            return "OC;";
        }

        public static string OutputOptions () // Full support.
        {
            return "OO;";
        }

        public static string OutputHardClipLimits () // Full support.
        {
            return "OH;";
        }
        #endregion

        #region STATIC CHART INSTRUCTIONS:
        //public static string AdvanceFullPage () { return "AF;"; } // AF or PG; or PGI; - NOP, nothing happens.
        //public static string EnableCutLine () { return "EC;"; } // EC or ECI; - NOP, nothing happens.
        #endregion

        #region STATIC POLYGON INSTRUCTIONS:
        public static string EdgeRectangleAbsolute (int iXur, int iYur) // Full support.
        {
            return string.Format ("EA{0},{1};", iXur, iYur);
        }

        public static string EdgeRectangleAbsolute (float fXur, float fYur) // Full support.
        {
            return string.Format ("EA{0:#####0.####},{1:#####0.####};", fXur, fYur);
        }

        public static string EdgeRectangleRelative (int iXur, int iYur) // Full support.
        {
            return string.Format ("ER{0},{1};", iXur, iYur);
        }

        public static string EdgeRectangleRelative (float fXur, float fYur) // Full support.
        {
            return string.Format ("ER{0:#####0.####},{1:#####0.####};", fXur, fYur);
        }

        public static string EdgeWedge (int iRadius, int iAngle, int iSweep) // Full support.
        {
            return string.Format ("EW{0},{1},{2};", iRadius, iAngle, iSweep);
        }

        public static string EdgeWedge (float fRadius, int iAngle, int iSweep) // Full support.
        {
            return string.Format ("EW{0:#####0.####},{1},{2};", fRadius, iAngle, iSweep);
        }

        public static string EdgeWedge (int iRadius, int iAngle, int iSweep, int iChord) // Full support.
        {
            if (iChord < 1)
                iChord = 1;
            else if (iChord > 120)
                iChord = 120;

            return string.Format ("EW{0},{1},{2},{3};", iRadius, iAngle, iSweep, iChord);
        }

        public static string EdgeWedge (float fRadius, int iAngle, int iSweep, int iChord) // Full support.
        {
            if (iChord < 1)
                iChord = 1;
            else if (iChord > 120)
                iChord = 120;

            return string.Format ("EW{0:#####0.####},{1},{2},{3};", fRadius, iAngle, iSweep, iChord);
        }

        public static string FillType (int iFillType) // Full support.
        {
            // 1. solid (lines with spacing as defined in the PT instruction; bidirectional shading)
            // 2. solid (lines with spacing as defined in the PT instruction; unidirectional shading)*
            // 3. parallellines
            // 4. cross-hatch
            // 5. ignored
            // *For the highest quality transparencies, use fill type 2.

            if (iFillType < 0)
                iFillType = 0;
            else if (iFillType > 5)
                iFillType = 5;

            return string.Format ("FT{0};", iFillType);
        }

        public static string FillType (int iFillType, float fSpacing) // Full support.
        {
            if (iFillType < 0)
                iFillType = 0;
            else if (iFillType > 5)
                iFillType = 5;

            if (fSpacing < 0.0F)
                fSpacing = 0.0F;

            return string.Format ("FT{0},{1:#####0.####};", iFillType, fSpacing);
        }

        public static string FillType (int iFillType, float fSpacing, int iAngle) // Full support.
        {
            if (iFillType < 0)
                iFillType = 0;
            else if (iFillType > 5)
                iFillType = 5;

            if (fSpacing < 0.0F)
                fSpacing = 0.0F;

            return string.Format ("FT{0},{1:#####0.####},{2};", iFillType, fSpacing, iAngle);
        }

        public static string PenThickness () // Full support.
        {
            return "PT;";
        }

        public static string PenThickness (float fWidth) // Full support.
        {
            if (fWidth < 0.1F)
                fWidth = 0.1F;
            else if (fWidth > 5.0F)
                fWidth = 5.0F;

            return string.Format ("PT{0:#####0.####};", fWidth);
        }

        public static string ShadeRectangleAbsolute (int iXur, int iYur) // Full support.
        {
            return string.Format ("RA{0},{1};", iXur, iYur);
        }

        public static string ShadeRectangleAbsolute (float fXur, float fYur) // Full support.
        {
            return string.Format ("RA{0:#####0.####},{1:#####0.####};", fXur, fYur);
        }

        public static string ShadeRectangleRelative (int iXur, int iYur) // Full support.
        {
            return string.Format ("RR{0},{1};", iXur, iYur);
        }

        public static string ShadeRectangleRelative (float fXur, float fYur) // Full support.
        {
            return string.Format ("RR{0:#####0.####},{1:#####0.####};", fXur, fYur);
        }

        public static string ShadeWedge (int iRadius, int iAngle, int iSweep) // Full support.
        {
            return string.Format ("WG{0},{1},{2};", iRadius, iAngle, iSweep);
        }

        public static string ShadeWedge (float fRadius, int iAngle, int iSweep) // Full support.
        {
            return string.Format ("WG{0:#####0.####},{1},{2};", fRadius, iAngle, iSweep);
        }

        public static string ShadeWedge (int iRadius, int iAngle, int iSweep, int iChord) // Full support.
        {
            if (iChord < 1)
                iChord = 1;
            else if (iChord > 120)
                iChord = 120;

            return string.Format ("WG{0},{1},{2},{3};", iRadius, iAngle, iSweep, iChord);
        }

        public static string ShadeWedge (float fRadius, int iAngle, int iSweep, int iChord) // Full support.
        {
            if (iChord < 1)
                iChord = 1;
            else if (iChord > 120)
                iChord = 120;

            return string.Format ("WG{0:#####0.####},{1},{2},{3};", fRadius, iAngle, iSweep, iChord);
        }
        #endregion

        #region HELPER METHODS
        public static List<KeyValuePair<int, int>> MakeIntPairList (int iXval, int iYval)
        {
            List<KeyValuePair<int, int>> lkvpInt = new List<KeyValuePair<int, int>> ();
            return MakeIntPairList (iXval, iYval, lkvpInt);
        }

        public static List<KeyValuePair<int, int>> MakeIntPairList (int iXval, int iYval, List<KeyValuePair<int, int>> lkvpInt)
        {
            KeyValuePair<int, int> kvpInt = new KeyValuePair<int, int> (iXval, iYval);
            lkvpInt.Add (kvpInt);
            return lkvpInt;
        }

        public static List<KeyValuePair<float, float>> MakeFloatPairList (float fXval, float fYval)
        {
            List<KeyValuePair<float, float>> lkvpFloat = new List<KeyValuePair<float, float>> ();
            return MakeFloatPairList (fXval, fYval, lkvpFloat);
        }

        public static List<KeyValuePair<float, float>> MakeFloatPairList (float fXval, float fYval, List<KeyValuePair<float, float>> lkvpFloat)
        {
            KeyValuePair<float, float> kvpFloat = new KeyValuePair<float, float> (fXval, fYval);
            lkvpFloat.Add (kvpFloat);
            return lkvpFloat;
        }

        public static string FormatIntPairList (string strInstruction, List<KeyValuePair<int, int>> lkvpInt)
        {
            StringBuilder sbldIntPairList = new StringBuilder ();

            sbldIntPairList.Append (strInstruction);

            foreach (KeyValuePair<int, int> kvp in lkvpInt)
            {
                if (sbldIntPairList.Length > 2)
                    sbldIntPairList.Append (',');
                sbldIntPairList.Append (string.Format ("{0},{1}", kvp.Key, kvp.Value));
            }

            sbldIntPairList.Append (";");

            return sbldIntPairList.ToString ();
        }

        public static string FormatFloatPairList (string strInstruction, List<KeyValuePair<float, float>> lkvpFloat)
        {
            StringBuilder sbldFloatPairList = new StringBuilder ();

            sbldFloatPairList.Append (strInstruction);

            foreach (KeyValuePair<float, float> kvp in lkvpFloat)
            {
                if (sbldFloatPairList.Length > 2)
                    sbldFloatPairList.Append (',');
                sbldFloatPairList.Append (string.Format ("{0:#####0.####},{1:#####0.####}", kvp.Key, kvp.Value));
            }

            sbldFloatPairList.Append (";");

            return sbldFloatPairList.ToString ();
        }

        public static int SafeConvertToInt (string strInput)
        {
            StringBuilder sbldSafeString = new StringBuilder ();

            for (int iIdx = 0; iIdx < strInput.Length; ++iIdx)
            {
                char cTest = strInput[iIdx];
                if (cTest >= '0' && cTest <= '9')
                    sbldSafeString.Append (cTest);
            }

            if (sbldSafeString.Length > 0)
            {
                return Convert.ToInt16 (sbldSafeString.ToString ());
            }
            else
                return 0;
        }

        private static void OutputString (string strCommand, ref StringBuilder sbldBuffer, WrapperBase wb = null,
                                          bool bOutputToConsole = false, string strConsoleMessage = "")
        {
            if (wb == null)
            {
                sbldBuffer.Append (strCommand);
            }
            else
            {
                wb.WriteTextString (strCommand);
            }

            if (bOutputToConsole)
            {
                if (strConsoleMessage.Length > 0)
                {
                    Console.WriteLine (strConsoleMessage);
                }
                Console.WriteLine (strCommand);
            }
        }
        #endregion

        #region DIGITIZE POINTS
        public static List<int> ReadDigitizedPoints (WrapperBase wb)
        {
            wb.WriteTextString (Initialize () + SelectPen () + PlotAbsolute (0, 0) + DigitizePoint ());

            string strTest = "";
            //Console.WriteLine (wb.QueryErrorText ());

            bool bFinished = false;
            int iLastXPos = -1,
                iLastYPos = -1,
                iLastPen  = -1;
            List<int> liPoints = new List<int> ();

            while (!bFinished)
            {
                int iPositionX = -1,
                    iPositionY = -1,
                    iPenPos    = -1;

                Thread.Sleep (100);
                string strStatus = wb.QueryStatus ();
                int iLen = strStatus.Length;
                if (iLen > 0 && iLen < 5)
                {
                    int iStatus = CHPGL.SafeConvertToInt (strStatus);
                    if ((iStatus & 4) > 0)
                    {
                        strTest = wb.QueryDigitizedPoint ();
                        Console.WriteLine (strTest);
                        int iComma1 = strTest.IndexOf (','),
                            iComma2 = strTest.IndexOf (',', iComma1 + 1);
                        string strX = strTest.Substring (0, iComma1),
                               strY = strTest.Substring (iComma1 + 1, iComma2 - iComma1 - 1);
                        Console.WriteLine ("X: {0}  Y: {1}:  Pen: {2}", strX, strY, strTest[iComma2 + 1]);
                        iPositionX = Convert.ToInt16 (strX);
                        iPositionY = Convert.ToInt16 (strY);
                        iPenPos = Convert.ToInt16 (strTest[iComma2 + 1]) - (int)'0';
                        //iPositionX = Convert.ToInt16 (strTest.Substring (0, iComma1));
                        //iPositionY = Convert.ToInt16 (strTest.Substring (iComma1 + 1, iComma2 - iComma1 - 1));

                        if (iLastXPos == iPositionX &&
                            iLastYPos == iPositionY)
                        {
                            Console.WriteLine ("** Exit loop on same point entered twice.");
                            bFinished = true;
                            wb.WriteTextString (DigitizeClear ());
                            //Console.WriteLine (wb.QueryErrorText ());
                            break;
                        }
                        else
                        {
                            liPoints.Add (iPositionX);
                            liPoints.Add (iPositionY);
                            liPoints.Add (iPenPos);
                            Console.WriteLine ("Adding point: X {0}  Y {1}  Pen {2}", iPositionX, iPositionY, iPenPos);
                        }

                        iLastXPos = iPositionX;
                        iLastYPos = iPositionY;
                        iLastPen  = iPenPos;

                        wb.WriteTextString (CHPGL.DigitizePoint ());
                    }
                }
            }

            return liPoints;
        }

        public static string PlotDigitizedPoints (List<int> liPoints)
        {
            return PlotDigitizedPoints (liPoints, true);
        }

        public static string PlotDigitizedPoints (List<int> liPoints, bool bAddCleanupInstructions)
        {
            if (liPoints.Count < 2)
                return "";

            bool bFreshPenDown = false;
            StringBuilder sbldPlot = new StringBuilder ();
            int iPosX     = -1,
                iPosY     = -1,
                iPen      = -1;

            for (int iIdx = 0; iIdx < liPoints.Count - 2; ++iIdx)
            {
                iPosX = liPoints[iIdx];
                iPosY = liPoints[++iIdx];
                iPen  = liPoints[++iIdx];

                //Console.WriteLine ("This X: {0}  Y: {1}  Pen: {2}", iPosX, iPosY, iPen);

                if (sbldPlot.Length == 0)
                {
                    // Allow for drawing from 0, 0 which defaults to pen up
                    if (iPosX == 0 && iPosY == 0)
                        sbldPlot.Append (string.Format ("PD{0},{1}", iPosX, iPosY));
                    else
                    {
                        sbldPlot.Append (string.Format ("PU{0},{1};PD", iPosX, iPosY));
                        bFreshPenDown = true;
                    }

                    continue;
                }

                if (iPen == 1)
                {
                    sbldPlot.Append (string.Format (";PU{0},{1};PD", iPosX, iPosY));
                    bFreshPenDown = true; // Put pen down after moving to drawing coordinates
                }
                else
                {
                    if (!bFreshPenDown)
                    {
                        sbldPlot.Append (',');
                    }
                    sbldPlot.Append (string.Format ("{0},{1}", iPosX, iPosY));
                    bFreshPenDown = false;
                }
            }

            if (sbldPlot.Length > 2)
            {
                sbldPlot.Append (";PU;");
                if (bAddCleanupInstructions)
                    sbldPlot.Append ("SP;PA0,0;");
            }

            return sbldPlot.ToString ();
        }
        #endregion

        #region COMPLEX SHAPES
#if OLD_VERSION
        public static string DrawSteppedLines (Point ptLine1Start, Point ptLine1End, Point ptLine2Start, Point ptLine2End,
                                               int iStepCount, bool bDrawGuideLines = false)
        {
            return DrawSteppedLines (ptLine1Start.X, ptLine1Start.Y, ptLine1End.X, ptLine1End.Y,
                                    ptLine2Start.X, ptLine2Start.Y, ptLine2End.X, ptLine2End.Y,
                                    iStepCount, bDrawGuideLines);
        }

        public static string DrawSteppedLines (int iLine1StartX, int iLine1StartY, int iLine1EndX, int iLine1EndY,
                                               int iLine2StartX, int iLine2StartY, int iLine2EndX, int iLine2EndY,
                                               int iStepCount, bool bDrawGuideLines = false, WrapperBase wb = null)
        {
            StringBuilder sbldDrawSteppedLines = new StringBuilder ();

            // Limit coordinates to 0 through max for page
            iLine1StartX = ValidateX (iLine1StartX);
            iLine1EndX   = ValidateX (iLine1EndX);
            iLine2StartX = ValidateX (iLine2StartX);
            iLine2EndX   = ValidateX (iLine2EndX);

            iLine1StartY = ValidateY (iLine1StartY);
            iLine1EndY   = ValidateY (iLine1EndY);
            iLine2StartY = ValidateY (iLine2StartY);
            iLine2EndY   = ValidateY (iLine2EndY);

            int iDistanceX1 = Math.Abs (iLine1EndX - iLine1StartX);
            int iDistanceX2 = Math.Abs (iLine2EndX - iLine2StartX);
            int iDistanceY1 = Math.Abs (iLine1EndY - iLine1StartY);
            int iDistanceY2 = Math.Abs (iLine2EndY - iLine2StartY);

            // Only where the start point of one = end point of the other, for crossing connecting lines
            bool bGuideLinesJoined = //(iLine1StartX == iLine2StartX && iLine1StartY == iLine2StartY) ||
                                     (iLine1StartX == iLine2EndX   && iLine1StartY == iLine2EndY)   ||
                                     (iLine1EndX   == iLine2StartX && iLine1EndY   == iLine2StartY);// ||
                                     //(iLine1EndX   == iLine2EndX   && iLine1EndY   == iLine2EndY);

            // Determine upper limit on # staps
            int iStepLimit1 = (Math.Abs (iDistanceX1) > Math.Abs (iDistanceY1)) ? Math.Abs (iDistanceX1) : Math.Abs (iDistanceY1);
            int iStepLimit2 = (Math.Abs (iDistanceX2) > Math.Abs (iDistanceY2)) ? Math.Abs (iDistanceX2) : Math.Abs (iDistanceY2);
            int iStepLimit  = (iStepLimit1 > iStepLimit2) ? iStepLimit1 : iStepLimit2;

            // Limit iSteps to 2 througn ?
            if (iStepCount < 2)
                iStepCount = 2;
            else if (iStepCount > iStepLimit)
                iStepCount      = iStepLimit;

            int iCurrentStep = 0;

            if (bGuideLinesJoined)
            {
                // Compute delta for each line end point
                float fDeltaX1 = iDistanceX1 / iStepCount;
                float fDeltaX2 = iDistanceX2 / iStepCount;
                float fDeltaY1 = iDistanceY1 / iStepCount;
                float fDeltaY2 = iDistanceY2 / iStepCount;

                bool bLine1IncreasesX = (iLine1EndX > iLine1StartX);
                bool bLine1DecreasesX = (iLine1EndX < iLine1StartX);
                bool bLine1IncreasesY = (iLine1EndY > iLine1StartY);
                bool bLine1DecreasesY = (iLine1EndY < iLine1StartY);

                bool bLine2IncreasesX = (iLine2EndX > iLine2StartX);
                bool bLine2DecreasesX = (iLine2EndX < iLine2StartX);
                bool bLine2IncreasesY = (iLine2EndY > iLine2StartY);
                bool bLine2DecreasesY = (iLine2EndY < iLine2StartY);

                if (bDrawGuideLines) // Guide lines joined start of one to end of the other
                {
                    // Draw line 1 from endpoint to starting point (do not lift pen)                   iLine1EndX / iLine1EndY     -> iLine1StartX / iLine1StartY
                    wb.WriteTextString (PlotAbsolute (iLine1EndX, iLine1EndY));     // Move pen to end of guide line 1
                    wb.WriteTextString (PenDown ());                                // Pen down                           Start drawing
                    wb.WriteTextString (PlotAbsolute (iLine1StartX, iLine1StartY)); // Move pen to start of guide line 1    Draw guide line 1

                    // Draw first connecting line from line 1 start point to line 2 (do not lift pen)  iLine1StartX / iLine1StartY -> iLine2StartX / iLine2StartY
                    wb.WriteTextString (PlotAbsolute (iLine2StartX, iLine2StartY)); // Move pen to start of guide line 2  Draw first connecting line

                    // Draw line 2 from end of first connecting line to other end (do not lift pen)    iLine2StartX / iLine2StartY -> iLine2EndX / iLine2EndY
                    //if (!bGuideLinesJoined)
                    //{
                    //    sbldDrawSteppedLines.Append (PlotAbsolute (iLine2EndX, iLine2EndY)); // Move pen to end of guide line 2    Draw guide line 2

                    //    // Draw last connecting line from current pen location to other end (now lift pen) iLine2EndX / iLine2EndY -> iLine1EndX / iLine1EndY
                    //    sbldDrawSteppedLines.Append (PlotAbsolute (iLine1EndX, iLine1EndY)); // Move pen to end of guide line 1    Draw last connecting line
                    //}

                    wb.WriteTextString (PenUp ());                                  // Pen up                             Drawing finished, get ready for next connecting line

                    // Set status: first and last connecting lines drawn
                    //++iCurrentStep;
                    //--iStepCount;
                }

                for (int iLineStep = 0; iLineStep < iStepCount; ++iStepCount)
                {
                    // 0, 1000 -> 3000, 0
                    // 0, 2000 -> 2000, 0
                    // 0, 3000 -> 1000, 0
                    int iStartX = 0;
                    int iStartY = 0;
                    int iEndX   = 0;
                    int iEndY   = 0;

                    ////////////////////////////////
                    // Both X and Y of both lines go from start values to end values, increasing or decreasing !!!
                    ////////////////////////////////
                    if (bLine1IncreasesX)
                    {
                        iStartX = (int)((float)iLine1StartX + (fDeltaX1 * iLineStep));
                    }
                    else if (bLine1DecreasesX)
                    {
                        iStartX = (int)((float)iLine1EndX - (fDeltaX1 * (iLineStep + 1)));
                    }

                    if (bLine1IncreasesY)
                    {
                        iStartY = (int)((float)iLine1EndY + (fDeltaY1 * iLineStep));
                    }
                    else if (bLine1DecreasesY)
                    {
                        iStartY = (int)((float)iLine1StartY - (fDeltaY1 * (iLineStep)));
                    }

                    if (bLine2IncreasesX)
                    {
                        iStartX = (int)((float)iLine2StartX + (fDeltaX2 * (iLineStep + 1)));
                    }
                    else if (bLine2DecreasesX)
                    {
                        iStartX = (int)((float)iLine2EndX - (fDeltaX2 * (iLineStep + 1)));
                    }

                    if (bLine2IncreasesY)
                    {
                        iStartY = (int)((float)iLine2EndY + (fDeltaY2 * iLineStep + 1));
                    }
                    else if (bLine2DecreasesY)
                    {
                        iStartY = (int)((float)iLine2StartY - (fDeltaY2 * (iLineStep + 1)));
                    }

                    //wb.WriteTextString (PlotAbsolute (iStartX, iStartY));           // Move pen to start of next connecting line
                    //wb.WriteTextString (PenDown ());                                // Pen down
                    //wb.WriteTextString (PlotAbsolute (iEndX, iEndY));               // Draw next connecting line
                    //wb.WriteTextString (PenUp ());                                  // Pen up
                    Console.WriteLine (string.Format ("CurrentStep: {0}, StepCount: {1}", iCurrentStep, iStepCount));
                    Console.WriteLine (string.Format ("From {0} x {1}  To {2} x {3}", iStartX, iStartY, iEndX, iEndY));
                }

                while (iCurrentStep < iStepCount)
                {
                    // Calculate starting and ending points of next connecting line
                    //int iStepFactor = iStepCount - 1;
                    int iStartX = (int)((float)iLine1EndX - (fDeltaX1 * (iStepCount - 1)));
                    int iStartY = (int)((float)iLine1EndY - (fDeltaY1 * (iStepCount - 1)));
                    int iEndX   = (int)((float)iLine2EndX - (fDeltaX2 * (iStepCount - 1)));
                    int iEndY   = (int)((float)iLine2EndY - (fDeltaY2 * (iStepCount - 1)));
                    // 0, 1000 -> 3000, 0
                    // 0, 2000 -> 2000, 0
                    // 0, 3000 -> 1000, 0

                    // Draw next connecting lines
                    wb.WriteTextString (PlotAbsolute (iStartX, iStartY));           // Move pen to start of next connecting line
                    wb.WriteTextString (PenDown ());                                // Pen down
                    wb.WriteTextString (PlotAbsolute (iEndX, iEndY));               // Draw next connecting line
                    wb.WriteTextString (PenUp ());                                  // Pen up
                    Console.WriteLine (string.Format ("CurrentStep: {0}, StepCount: {1}", iCurrentStep, iStepCount));
                    Console.WriteLine (string.Format ("From {0} x {1}  To {2} x {3}", iStartX, iStartY, iEndX, iEndY));
                    --iStepCount;                                                            // Decrement step count

                    if (iCurrentStep < iStepCount)
                    {
                        // Calculate starting and ending points of next connecting line
                        iStartX = (int)((float)iLine2EndX - (fDeltaX2 * (iStepCount - 1)));
                        iStartY = (int)((float)iLine2EndY - (fDeltaY2 * (iStepCount - 1)));
                        iEndX   = (int)((float)iLine1EndX - (fDeltaX1 * (iStepCount - 1)));
                        iEndY   = (int)((float)iLine1EndY - (fDeltaY1 * (iStepCount - 1)));

                        // Draw next connecting lines
                        wb.WriteTextString (PlotAbsolute (iStartX, iStartY));           // Move pen to start of next connecting line
                        wb.WriteTextString (PenDown ());                                // Pen down
                        wb.WriteTextString (PlotAbsolute (iEndX, iEndY));               // Draw next connecting line
                        wb.WriteTextString (PenUp ());                                  // Pen up
                        Console.WriteLine (string.Format ("CurrentStep: {0}, StepCount: {1}", iCurrentStep, iStepCount));
                        Console.WriteLine (string.Format ("From {0} x {1}  To {2} x {3}", iStartX, iStartY, iEndX, iEndY));
                        --iStepCount;                                                            // Decrement step count
                    }
                }
            }
            else // Guide lines not joined start of one to end of the other
            {
                // Compute delta for each line end point
                float fDeltaX1 = iDistanceX1 / (iStepCount - 1);
                float fDeltaX2 = iDistanceX2 / (iStepCount - 1);
                float fDeltaY1 = iDistanceY1 / (iStepCount - 1);
                float fDeltaY2 = iDistanceY2 / (iStepCount - 1);

                if (bDrawGuideLines)
                {
                    // Draw line 1 from endpoint to starting point (do not lift pen)                   iLine1EndX / iLine1EndY     -> iLine1StartX / iLine1StartY
                    sbldDrawSteppedLines.Append (PlotAbsolute (iLine1EndX, iLine1EndY));     // Move pen to end of guide line 1
                    sbldDrawSteppedLines.Append (PenDown ());                                // Pen down                           Start drawing
                    sbldDrawSteppedLines.Append (PlotAbsolute (iLine1StartX, iLine1StartY)); // Move pen to start of guide line 1    Draw guide line 1

                    // Draw first connecting line from line 1 start point to line 2 (do not lift pen)  iLine1StartX / iLine1StartY -> iLine2StartX / iLine2StartY
                    sbldDrawSteppedLines.Append (PlotAbsolute (iLine2StartX, iLine2StartY)); // Move pen to start of guide line 2  Draw first connecting line

                    // Draw line 2 from end of first connecting line to other end (do not lift pen)    iLine2StartX / iLine2StartY -> iLine2EndX / iLine2EndY
                    //if (!bGuideLinesJoined)
                    //{
                        sbldDrawSteppedLines.Append (PlotAbsolute (iLine2EndX, iLine2EndY)); // Move pen to end of guide line 2    Draw guide line 2

                        // Draw last connecting line from current pen location to other end (now lift pen) iLine2EndX / iLine2EndY -> iLine1EndX / iLine1EndY
                        sbldDrawSteppedLines.Append (PlotAbsolute (iLine1EndX, iLine1EndY)); // Move pen to end of guide line 1    Draw last connecting line
                    //}

                    sbldDrawSteppedLines.Append (PenUp ());                                  // Pen up                             Drawing finished, get ready for next connecting line

                    // Set status: first and last connecting lines drawn
                    ++iCurrentStep;
                    --iStepCount;
                }

                //for (int iLineStep = 0; iLineStep >= iStepCount; ++iStepCount)
                while (iCurrentStep < iStepCount)
                {
                    // Calculate starting and ending points of next connecting line
                    //int iStepFactor = iStepCount - 1;
                    int iStartX = (int)((float)iLine1EndX - (fDeltaX1 * (iStepCount - 1))); //iCurrentStep));
                    int iStartY = (int)((float)iLine1EndY - (fDeltaY1 * (iStepCount - 1))); //iCurrentStep));
                    int iEndX = (int)((float)iLine2EndX - (fDeltaX2 * (iStepCount - 1))); //iCurrentStep));
                    int iEndY = (int)((float)iLine2EndY - (fDeltaY2 * (iStepCount - 1))); //iCurrentStep));

                    // Draw next connecting lines
                    sbldDrawSteppedLines.Append (PlotAbsolute (iStartX, iStartY));           // Move pen to start of next connecting line
                    sbldDrawSteppedLines.Append (PenDown ());                                // Pen down
                    sbldDrawSteppedLines.Append (PlotAbsolute (iEndX, iEndY));               // Draw next connecting line
                    sbldDrawSteppedLines.Append (PenUp ());                                  // Pen up
                    Console.WriteLine (string.Format ("CurrentStep: {0}, StepCount: {1}", iCurrentStep, iStepCount));
                    Console.WriteLine (string.Format ("From {0} x {1}  To {2} x {3}", iStartX, iStartY, iEndX, iEndY));
                    --iStepCount;                                                            // Decrement step count

                    if (iCurrentStep < iStepCount)
                    {
                        // Calculate starting and ending points of next connecting line
                        iStartX = (int)((float)iLine2EndX - (fDeltaX2 * (iStepCount - 1))); //iCurrentStep));
                        iStartY = (int)((float)iLine2EndY - (fDeltaY2 * (iStepCount - 1))); //iCurrentStep));
                        iEndX = (int)((float)iLine1EndX - (fDeltaX1 * (iStepCount - 1))); //iCurrentStep));
                        iEndY = (int)((float)iLine1EndY - (fDeltaY1 * (iStepCount - 1))); //iCurrentStep));

                        // Draw next connecting lines
                        sbldDrawSteppedLines.Append (PlotAbsolute (iStartX, iStartY));           // Move pen to start of next connecting line
                        sbldDrawSteppedLines.Append (PenDown ());                                // Pen down
                        sbldDrawSteppedLines.Append (PlotAbsolute (iEndX, iEndY));               // Draw next connecting line
                        sbldDrawSteppedLines.Append (PenUp ());                                  // Pen up
                        Console.WriteLine (string.Format ("CurrentStep: {0}, StepCount: {1}", iCurrentStep, iStepCount));
                        Console.WriteLine (string.Format ("From {0} x {1}  To {2} x {3}", iStartX, iStartY, iEndX, iEndY));
                        --iStepCount;                                                            // Decrement step count
                    }
                }
            }

            return sbldDrawSteppedLines.ToString ();
        }
#else
        public static string DrawSteppedLines (int iLine1StartX, int iLine1StartY, int iLine1EndX, int iLine1EndY,
                                               int iLine2StartX, int iLine2StartY, int iLine2EndX, int iLine2EndY,
                                               int iStepCount, bool bDrawGuideLines = false)
        {
            StringBuilder sbldDrawSteppedLines = new StringBuilder ();

            // Limit coordinates to 0 through max for page
            iLine1StartX = ValidateX (iLine1StartX);
            iLine1EndX   = ValidateX (iLine1EndX);
            iLine2StartX = ValidateX (iLine2StartX);
            iLine2EndX   = ValidateX (iLine2EndX);

            iLine1StartY = ValidateY (iLine1StartY);
            iLine1EndY   = ValidateY (iLine1EndY);
            iLine2StartY = ValidateY (iLine2StartY);
            iLine2EndY   = ValidateY (iLine2EndY);

            int iDistanceX1 = iLine1EndX - iLine1StartX;
            int iDistanceX2 = iLine2EndX - iLine2StartX;
            int iDistanceY1 = iLine1EndY - iLine1StartY;
            int iDistanceY2 = iLine2EndY - iLine2StartY;

            bool bGuideLinesJoined = //(iLine1StartX == iLine2StartX && iLine1StartY == iLine2StartY) ||
                                     (iLine1StartX == iLine2EndX   && iLine1StartY == iLine2EndY)   ||
                                     (iLine1EndX   == iLine2StartX && iLine1EndY   == iLine2StartY);// ||
                                     //(iLine1EndX   == iLine2EndX   && iLine1EndY   == iLine2EndY);

            // Determine upper limit on # staps
            int iStepLimit1 = (Math.Abs (iDistanceX1) > Math.Abs (iDistanceY1)) ? Math.Abs (iDistanceX1) : Math.Abs (iDistanceY1);
            int iStepLimit2 = (Math.Abs (iDistanceX2) > Math.Abs (iDistanceY2)) ? Math.Abs (iDistanceX2) : Math.Abs (iDistanceY2);
            int iStepLimit  = (iStepLimit1 > iStepLimit2) ? iStepLimit1 : iStepLimit2;

            // Limit iSteps to 2 througn ?
            if (iStepCount < 2)
                iStepCount = 2;
            else if (iStepCount > iStepLimit)
                iStepCount      = iStepLimit;

            int iCurrentStep = 0;

            // Compute delta for each line end point
            float fDeltaX1 = iDistanceX1 / (iStepCount - 1);
            float fDeltaX2 = iDistanceX2 / (iStepCount - 1);
            float fDeltaY1 = iDistanceY1 / (iStepCount - 1);
            float fDeltaY2 = iDistanceY2 / (iStepCount - 1);
            //float fDeltaX1 = iDistanceX1 / (iStepCount + (bGuideLinesJoined ? 1 : 0));
            //float fDeltaX2 = iDistanceX2 / (iStepCount + (bGuideLinesJoined ? 1 : 0));
            //float fDeltaY1 = iDistanceY1 / (iStepCount + (bGuideLinesJoined ? 1 : 0));
            //float fDeltaY2 = iDistanceY2 / (iStepCount + (bGuideLinesJoined ? 1 : 0));

            if (bDrawGuideLines)
            {
                // Draw line 1 from endpoint to starting point (do not lift pen)                   iLine1EndX / iLine1EndY     -> iLine1StartX / iLine1StartY
                sbldDrawSteppedLines.Append (PlotAbsolute (iLine1EndX, iLine1EndY));     // Move pen to end of guide line 1
                sbldDrawSteppedLines.Append (PenDown ());                                // Pen down                           Start drawing
                sbldDrawSteppedLines.Append (PlotAbsolute (iLine1StartX, iLine1StartY)); // Move pen to start of guide line 1    Draw guide line 1

                // Draw first connecting line from line 1 start point to line 2 (do not lift pen)  iLine1StartX / iLine1StartY -> iLine2StartX / iLine2StartY
                sbldDrawSteppedLines.Append (PlotAbsolute (iLine2StartX, iLine2StartY)); // Move pen to start of guide line 2  Draw first connecting line

                // Draw line 2 from end of first connecting line to other end (do not lift pen)    iLine2StartX / iLine2StartY -> iLine2EndX / iLine2EndY
                //if (!bGuideLinesJoined)
                //{
                    sbldDrawSteppedLines.Append (PlotAbsolute (iLine2EndX, iLine2EndY)); // Move pen to end of guide line 2    Draw guide line 2

                    // Draw last connecting line from current pen location to other end (now lift pen) iLine2EndX / iLine2EndY -> iLine1EndX / iLine1EndY
                    sbldDrawSteppedLines.Append (PlotAbsolute (iLine1EndX, iLine1EndY)); // Move pen to end of guide line 1    Draw last connecting line
                //}
                sbldDrawSteppedLines.Append (PenUp ());                                  // Pen up                             Drawing finished, get ready for next connecting line

                // Set status: first and last connecting lines drawn
                //if (!bGuideLinesJoined)
                //{
                    ++iCurrentStep;
                    --iStepCount;
                //}
            }

            //for (int iLineStep = 0; iLineStep >= iStepCount; ++iStepCount)
            while (iCurrentStep < iStepCount)
            {
                // Calculate starting and ending points of next connecting line
                int iStartX = (int)((float)iLine1EndX - (fDeltaX1 * iStepCount - (bGuideLinesJoined ? 1 : 0)));
                int iStartY = (int)((float)iLine1EndY - (fDeltaY1 * iStepCount - (bGuideLinesJoined ? 1 : 0)));
                int iEndX   = (int)((float)iLine2EndX - (fDeltaX2 * iStepCount - (bGuideLinesJoined ? 1 : 0)));
                int iEndY   = (int)((float)iLine2EndY - (fDeltaY2 * iStepCount - (bGuideLinesJoined ? 1 : 0)));

                // Draw next connecting lines
                sbldDrawSteppedLines.Append (PlotAbsolute (iStartX, iStartY));           // Move pen to start of next connecting line
                sbldDrawSteppedLines.Append (PenDown ());                                // Pen down
                sbldDrawSteppedLines.Append (PlotAbsolute (iEndX, iEndY));               // Draw next connecting line
                sbldDrawSteppedLines.Append (PenUp ());                                  // Pen up
                //Console.WriteLine (string.Format ("CurrentStep: {0}, StepCount: {1}", iCurrentStep, iStepCount));
                //Console.WriteLine (string.Format ("From {0} x {1}  To {2} x {3}", iStartX, iStartY, iEndX, iEndY));
                --iStepCount;                                                            // Decrement step count

                if (iCurrentStep < iStepCount)
                {
                    // Calculate starting and ending points of next connecting line
                    iStartX = (int)((float)iLine2EndX - (fDeltaX2 * iStepCount - (bGuideLinesJoined ? 1 : 0)));
                    iStartY = (int)((float)iLine2EndY - (fDeltaY2 * iStepCount - (bGuideLinesJoined ? 1 : 0)));
                    iEndX   = (int)((float)iLine1EndX - (fDeltaX1 * iStepCount - (bGuideLinesJoined ? 1 : 0)));
                    iEndY   = (int)((float)iLine1EndY - (fDeltaY1 * iStepCount - (bGuideLinesJoined ? 1 : 0)));

                    // Draw next connecting lines
                    sbldDrawSteppedLines.Append (PlotAbsolute (iStartX, iStartY));           // Move pen to start of next connecting line
                    sbldDrawSteppedLines.Append (PenDown ());                                // Pen down
                    sbldDrawSteppedLines.Append (PlotAbsolute (iEndX, iEndY));               // Draw next connecting line
                    sbldDrawSteppedLines.Append (PenUp ());                                  // Pen up
                    //Console.WriteLine (string.Format ("CurrentStep: {0}, StepCount: {1}", iCurrentStep, iStepCount));
                    //Console.WriteLine (string.Format ("From {0} x {1}  To {2} x {3}", iStartX, iStartY, iEndX, iEndY));
                    --iStepCount;                                                            // Decrement step count
                }
            }

            return sbldDrawSteppedLines.ToString ();
        }
#endif
        #endregion
    }

    public static class CPlotterShapes
    {
    }
}
