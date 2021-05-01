//#define OLD_VERSION
//#define HPGL_COMMENTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.Win32;

namespace PlotterEngine
{
    public enum EPlotMode
    {
        ePlotDefault,
        ePlotAbsolute,
        ePoltRelative
    }

    //public class CRectangle
    //{
    //    Point m_ptLowerLeft  = new Point (-1, -1);
    //    Point m_ptUpperRight = new Point (-1, -1);

    //    public CRectangle () { }
    //    public CRectangle (Point ptLL, Point ptUR)
    //    {
    //        m_ptLowerLeft  = ptLL;
    //        m_ptUpperRight = ptUR;
    //    }

    //    public bool IsPointInRect (Point ptTest)
    //    {
    //        return (ptTest.X >= m_ptLowerLeft.X  &&
    //                ptTest.X <= m_ptUpperRight.X &&
    //                ptTest.Y >= m_ptLowerLeft.Y  &&
    //                ptTest.Y <= m_ptUpperRight.Y);
    //    }
    //}

    public static class CGenericMethods
    {
        // From PlotterDriver.CSerialPortDriver:
        //private const int EMPTY_READ_DATA_STRING           = -1;
        //private const int PLOTTER_BUSY_ERROR_CODE          = -2;

        public const int FORMAT_EXCEPTION_ERROR_CODE         = -3;
        public const int STACK_OVERFLOW_EXCEPTION_ERROR_CODE = -4;
        public const int GENERAL_EXCEPTION_ERROR_CODE        = -5;

        private const string STRING_VERSION = "Version=";
        private const string HPGL_EXTENSION = ".hpgl";
        private const string COMMENT_PREFIX = ";//";
        private const string COMMENT_SUFFIX = "\\";
        private const string SELECT_PEN_ZERO = ";SP0;";
        private const string SELECT_NO_PEN   = ";SP;";
        private const string SELECT_PEN      = ";SP";

        private static string s_strLastDocName = "";
        private static int s_iDocumentSequence = 0;
        private static int s_iDocumentCount    = 0;

        public static string FillStringLength (string strIn, int iStrLength, char cFillChar = '0', bool bFillLead = true)
        {
            if (iStrLength > strIn.Length)
            {
                int iFillLength = iStrLength - strIn.Length;
                return bFillLead ? new string (cFillChar, iFillLength) + strIn :
                                   strIn + new string (cFillChar, iFillLength);
            }

            return strIn;
        }

        public static int SafeConvertToInt (string strValue)
        {
            if (strValue.Length == 0)
            {
                return 0;
            }

            try
            {
                int iValue = (int)Convert.ToInt32 (strValue);
                return iValue;
            }
            catch (System.FormatException)
            {
                return FORMAT_EXCEPTION_ERROR_CODE;
            }
            catch (System.StackOverflowException)
            {
                return STACK_OVERFLOW_EXCEPTION_ERROR_CODE;
            }
            catch (Exception)
            {
                return GENERAL_EXCEPTION_ERROR_CODE;
            }
        }

        public static float SafeConvertToFloat (string strValue)
        {
            try
            {
                float fValue = (float)Convert.ToDouble (strValue);
                return fValue;
            }
            catch (System.FormatException)
            {
                return FORMAT_EXCEPTION_ERROR_CODE;
            }
            catch (System.StackOverflowException)
            {
                return STACK_OVERFLOW_EXCEPTION_ERROR_CODE;
            }
            catch (Exception)
            {
                return GENERAL_EXCEPTION_ERROR_CODE;
            }
        }

        public static Byte[] SqueeezByteArray (Byte[] yaPlotterBufferUTF16)
        {
            List<Byte> lyUTF8 = new List<byte> ();

            for (int iIdx = 0; iIdx < yaPlotterBufferUTF16.Length; ++iIdx)
            {
                Byte y2 = (Byte)Convert.ToUInt16 (yaPlotterBufferUTF16[iIdx]);
                if (iIdx % 2 == 0)
                {
                    lyUTF8.Add (y2);
                }
            }

            return lyUTF8.ToArray ();
        }

        public static string[] ParseDelimitedString (string strCSVString, char cDelimiter = ',')
        {
            List<string> lstrSubstrings = new List<string> ();

            int iLastCommaIdx = -1;
            int iCommaIdx = strCSVString.IndexOf (cDelimiter);
            while (iCommaIdx > 0)
            {
                string strParam = strCSVString.Substring (iLastCommaIdx >= 0 ? iLastCommaIdx + 1 : 0, iCommaIdx - iLastCommaIdx - 1);
                lstrSubstrings.Add (strParam);
                iLastCommaIdx = iCommaIdx;
                iCommaIdx = strCSVString.IndexOf (cDelimiter, iLastCommaIdx + 1);
            }
            lstrSubstrings.Add (strCSVString.Substring (iLastCommaIdx + 1));

            return lstrSubstrings.ToArray ();
        }

        public static string ExtractStringInQuotes (string strIn, bool bFirstString = true)
        {
            int iFirstQuote = bFirstString ? strIn.IndexOf ('\"') : strIn.LastIndexOf ('\"');
            int iLastQuote = -1;
            if (iFirstQuote >= 0)
            {
                if (bFirstString)
                {
                    if (iFirstQuote < strIn.Length - 1)
                    {
                        iLastQuote = strIn.IndexOf ('\"', iFirstQuote + 1);
                    }
                }
                else
                {
                    if (iFirstQuote > 0 &&
                        iFirstQuote <= strIn.Length - 1)
                    {
                        iLastQuote = strIn.LastIndexOf ('\"', iFirstQuote - 1);
                    }
                }
            }

            if (iFirstQuote > 0)
            {
                if (bFirstString)
                {
                    if (iLastQuote > iFirstQuote)
                    {
                        return strIn.Substring (iFirstQuote + 1, iLastQuote - iFirstQuote - 1);
                    }
                }
                else
                {
                    if (iLastQuote < iFirstQuote)
                    {
                        return strIn.Substring (iLastQuote + 1, iFirstQuote - iLastQuote - 1);
                    }
                }
            }

            return "";
        }

        public static string FormatPlotDocName (string strPlotDocName, /*int iDocSeq, int iDocCount,*/ int iDocLength)
        {
            if (strPlotDocName == null ||
                strPlotDocName.Length == 0)
            {
                return "";
            }

            if (strPlotDocName.ToLower ().Contains (HPGL_EXTENSION))
            {
                int iHpglIdx = strPlotDocName.ToLower ().IndexOf (HPGL_EXTENSION);
                strPlotDocName = strPlotDocName.Substring (0, iHpglIdx);
            }

            if (s_strLastDocName == "" ||
                s_strLastDocName != strPlotDocName)
            {
                s_iDocumentCount = 0;
                s_strLastDocName = strPlotDocName;
            }

            StringBuilder sbDocName = new StringBuilder ();
            DateTime dtNow = DateTime.Now;

            sbDocName.Append (CGenericMethods.FillStringLength ((++s_iDocumentSequence).ToString (), 3));
            sbDocName.Append ('_');
            sbDocName.Append (strPlotDocName);

            sbDocName.Append ('_');
            sbDocName.Append (dtNow.Year.ToString () + '-' +
                              CGenericMethods.FillStringLength (dtNow.Month.ToString (), 2) + '-' +
                              CGenericMethods.FillStringLength (dtNow.Day.ToString (), 2));

            sbDocName.Append ('_');
            sbDocName.Append (CGenericMethods.FillStringLength (dtNow.Hour.ToString (), 2) + ':' +
                              CGenericMethods.FillStringLength (dtNow.Minute.ToString (), 2) + ':' +
                              CGenericMethods.FillStringLength (dtNow.Second.ToString (), 2));

            sbDocName.Append ('_');
            sbDocName.Append (CGenericMethods.FillStringLength ((++s_iDocumentCount).ToString (), 3));

            sbDocName.Append ('_');
            sbDocName.Append (CGenericMethods.FillStringLength (iDocLength.ToString (), 5));

            return sbDocName.ToString ();
        }
        
        public static string StripFileExtension (string strFilename)
        {
            int iLastDot = strFilename.LastIndexOf ('.');
            if (iLastDot > 0)
            {
                return strFilename.Substring (0, iLastDot);
            }

            return strFilename;
        }

        public static string PrependCommentHPGL (string strHPGL, string strComment)
        {
#if HPGL_COMMENTS
            return COMMENT_PREFIX + strComment + COMMENT_SUFFIX + strHPGL;
#else
            return strHPGL;
#endif
        }

        public static string ExtractCommentHPGL (string strHPGL)
        {
#if HPGL_COMMENTS
            if (strHPGL.Contains (COMMENT_PREFIX) &&
                strHPGL.Contains (COMMENT_SUFFIX))
            {
                int iCommentLength = strHPGL.IndexOf (COMMENT_SUFFIX) - COMMENT_PREFIX.Length - COMMENT_SUFFIX.Length + 1;
                string strComment  = strHPGL.Substring (COMMENT_PREFIX.Length, iCommentLength);
                return strComment;
            }

            return "";
#else
            return strHPGL;
#endif
        }

        public static string RemoveCommentHPGL (string strHPGL)
        {
#if HPGL_COMMENTS
            if (strHPGL.Contains (COMMENT_PREFIX) &&
                strHPGL.Contains (COMMENT_SUFFIX))
            {
                int iCommentEndIdx = strHPGL.IndexOf (COMMENT_SUFFIX) + COMMENT_SUFFIX.Length;
                strHPGL = strHPGL.Substring (iCommentEndIdx);
            }

            return strHPGL;
#else
            return strHPGL;
#endif
        }

        public static int GetWindowDeltaDiagonal (Point ptP1, Point ptP2)
        {
            int iDeltaX = Math.Abs (ptP2.X - ptP1.X),
                iDeltaY = Math.Abs (ptP2.Y - ptP1.Y);
            return (int)Math.Sqrt ((iDeltaX * iDeltaX) + (iDeltaY * iDeltaY));
        }

        private static List<HPGL.EPenSelect> CreatePenList (bool bRandomize = false, int iMaxPenCount = 8)
        {
            List<HPGL.EPenSelect> liOrderedPens = new List<HPGL.EPenSelect> ();
            List<HPGL.EPenSelect> liRandomPens = new List<HPGL.EPenSelect> ();

            for (int iIdx = 1; iIdx <= iMaxPenCount; ++iIdx)
            {
                liOrderedPens.Add ((HPGL.EPenSelect)iIdx);
            }

            if (bRandomize)
            {
                DateTime dtNow = DateTime.Now;
                Random rand = new Random (dtNow.Millisecond);

                while (liOrderedPens.Count > 1)
                {
                    int iIdx = rand.Next (liOrderedPens.Count - 1);
                    liRandomPens.Add (liOrderedPens[iIdx]);
                    liOrderedPens.RemoveAt (iIdx);
                }
                liRandomPens.Add (liOrderedPens[0]);
                liOrderedPens.RemoveAt (0);

                return liRandomPens;
            }

            return liOrderedPens;
        }

        public static bool IsSelectPenCommandInHPGL (string strHPGL)
        {
            return strHPGL.Contains  (SELECT_PEN)      &&
                   !strHPGL.Contains (SELECT_PEN_ZERO) &&
                   !strHPGL.Contains (SELECT_NO_PEN);
        }

        public static bool IsNumeric (string strTest)
        {
            foreach (char c in strTest)
            {
                if (!IsNumeric (c))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsNumeric (char cTest)
        {
            return (cTest >= '0' &&
                    cTest <= '9') ;
        }

        public static bool PointsAreInRect (List<Point[]> lptTest, Rectangle rcTest)
        {
            foreach (Point[] pa in lptTest)
            {
                if (!PointsAreInRect (pa, rcTest))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool PointsAreInRect (Point[] ptaTest, Rectangle rcTest)
        {
            foreach (Point pt in ptaTest)
            {
                if (!PointIsInRect (pt, rcTest))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool PointIsInRect (Point ptTest, Rectangle rcTest)
        {
            if (rcTest.IsEmpty)
            {
                return false;
            }

            return (ptTest.X >= rcTest.Left   &&
                    ptTest.X <= rcTest.Right  &&
                    ptTest.Y <= rcTest.Bottom &&
                    ptTest.Y >= rcTest.Top);
        }

        public static Rectangle GetBoundingRectangle (List<Point[]> lptaPattern)
        {
            List<Point> lptAll = new List<Point> ();

            foreach (Point[] pta in lptaPattern)
            {
                foreach (Point pt in pta)
                {
                    lptAll.Add (pt);
                }
            }

            return GetBoundingRectangle (lptAll.ToArray ());
        }

        public static Rectangle GetBoundingRectangle (Point[] ptaPattern)
        {
            int  iRectLeft   = 0;
            int  iRectTop    = 0;
            int  iRectRight  = 0;
            int  iRectBottom = 0;

            foreach (Point pt in ptaPattern)
            {
                if (iRectLeft   == 0 &&
                    iRectRight  == 0 &&
                    iRectBottom == 0 &&
                    iRectTop    == 0)
                {
                    iRectLeft   = iRectRight = pt.X;
                    iRectBottom = iRectTop   = pt.Y;
                }
                else
                {
                    if (iRectLeft > pt.X)
                    {
                        iRectLeft = pt.X;
                    }
                    if (iRectRight < pt.X)
                    {
                        iRectRight = pt.X;
                    }
                    if (iRectBottom > pt.Y)
                    {
                        iRectBottom = pt.Y;
                    }
                    if (iRectTop < pt.Y)
                    {
                        iRectTop = pt.Y;
                    }
                }
            }

            return new Rectangle (iRectLeft, iRectTop, iRectRight - iRectLeft, iRectTop - iRectBottom);
        }

        public static string LoadSetting (string strRegistryPath, string strValueName)
        {
            RegistryKey rkPathKey = Registry.LocalMachine.OpenSubKey (strRegistryPath, false);
            if (rkPathKey == null)
            {
                return "";
            }

            object objRegValue = rkPathKey.GetValue (strValueName);
            try
            {
                // Treat all types as string
                return objRegValue.ToString ();
            }
            catch (Exception)
            {
                // Fail silently
            }

            return "";
        }

        public static string GetCurrentVersions (ref string rstrShortName, ref string rstrFullName, ref string rstrCSharpVer, ref string rstrDotNetVer, ref string rstrWpfVer)
        {
            //_MSC_VER  <--------------- VS Version ---------------->   C#    .NET   WPF
            //  1200    VS 6.0         Visual Studio 6.0
            //  1300    VS2002         Visual Studio .NET 2002 (7.0)    1.0   1
            //  1310    VS2003         Visual Studio .NET 2003 (7.1)    1.0   1.1
            //  1400    VS2005         Visual Studio 2005 (8.0)         2.0   2
            //  1500    VS2008         Visual Studio 2008 (9.0)         3.0   3.5    3.5
            //  1600    VS2010         Visual Studio 2010 (10.0)        4.0   4      4
            //  1700    VS2012         Visual Studio 2012 (11.0)        5.0   4.5    4.5
            //  1800    VS2013         Visual Studio 2013 (12.0)        5.0   4.5.1  4.5.1
            //  1900    VS2015         Visual Studio 2015 (14.0)        6.0   4.6    4.6
            //  1910    VS2017 15.0    Visual Studio 2017 RTW (15.0)    7.0   4.6.1
            //  1911    VS2017 15.3    Visual Studio 2017 version 15.3  7.0   4.6.2
            //  1912    VS2017 15.5    Visual Studio 2017 version 15.5  7.0
            //  1913    VS2017 15.6    Visual Studio 2017 version 15.6
            //  1914    VS2017 15.7    Visual Studio 2017 version 15.7
            //  1915    VS2017 15.8    Visual Studio 2017 version 15.8
            //  1916    VS2017 15.9    Visual Studio 2017 version 15.9
            //  1920    VS2019 16.0    Visual Studio 2019 RTW (16.0)
            //  1921    VS2019 16.1    Visual Studio 2019 version 16.1
            //  1922    VS2019 16.2    Visual Studio 2019 version 16.2
            //  1923    VS2019 16.3    Visual Studio 2019 version 16.3
            //  1924    VS2019 16.4    Visual Studio 2019 version 16.4
            //  1925    VS2019 16.5    Visual Studio 2019 version 16.5
            //  1926    VS2019 16.6    Visual Studio 2019 version 16.6
            //  1927    VS2019 16.7    Visual Studio 2019 version 16.7
            //  1928    VS2019 16.8    Visual Studio 2019 version 16.8

            StringBuilder sbCurrentVersions = new StringBuilder (); // Ver: 1900 (VS2015)  C# 6.0  .Net 4.6  WPF  4.6
            Int32 i32CompilerVersion        = CompilerVersion.CCompilerVersion.GetMscVer ();
            Int32 i32CompilerFullVersion    = CompilerVersion.CCompilerVersion.GetMscFullVer ();
            string strCompilerFullVersion   = string.Format ("{0:##-#-00000-0}", i32CompilerFullVersion).Replace ('-', '.');

            //Console.WriteLine ("CompilerVersion: " + i32CompilerVersion.ToString ());
            //Console.WriteLine ("CompilerFullVersion: " + i32CompilerFullVersion.ToString ());

            //  1928    VS2019 16.8    Visual Studio 2019 version 16.8
            if (i32CompilerVersion >= 1928)
            {
                rstrShortName = "VS2019 16.8";
                rstrFullName  = "Visual Studio 2019 version 16.8";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1927    VS2019 16.7    Visual Studio 2019 version 16.7
            else if (i32CompilerVersion >= 1927)
            {
                rstrShortName = "VS2019 16.7";
                rstrFullName  = "Visual Studio 2019 version 16.7";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1926    VS2019 16.6    Visual Studio 2019 version 16.6
            else if (i32CompilerVersion >= 1926)
            {
                rstrShortName = "VS2019 16.6";
                rstrFullName  = "Visual Studio 2019 version 16.6";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1925    VS2019 16.5    Visual Studio 2019 version 16.5
            else if (i32CompilerVersion >= 1925)
            {
                rstrShortName = "VS2019 16.5";
                rstrFullName  = "Visual Studio 2019 version 16.5";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1924    VS2019 16.4    Visual Studio 2019 version 16.4
            else if (i32CompilerVersion >= 1924)
            {
                rstrShortName = "VS2019 16.4";
                rstrFullName  = "Visual Studio 2019 version 16.4";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1923    VS2019 16.3    Visual Studio 2019 version 16.3
            else if (i32CompilerVersion >= 1923)
            {
                rstrShortName = "VS2019 16.3";
                rstrFullName  = "Visual Studio 2019 version 16.3";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1922    VS2019 16.2    Visual Studio 2019 version 16.2
            else if (i32CompilerVersion >= 1922)
            {
                rstrShortName = "VS2019 16.2";
                rstrFullName  = "Visual Studio 2019 version 16.2";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1921    VS2019 16.1    Visual Studio 2019 version 16.1
            else if (i32CompilerVersion >= 1921)
            {
                rstrShortName = "VS2019 16.1";
                rstrFullName  = "Visual Studio 2019 version 16.1";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1920    VS2019 16.0    Visual Studio 2019 RTW (16.0)
            else if (i32CompilerVersion >= 1920)
            {
                rstrShortName = "VS2019 16.0";
                rstrFullName  = "Visual Studio 2019 RTW (16.0)";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1916    VS2017 15.9    Visual Studio 2017 version 15.9
            else if (i32CompilerVersion >= 1916)
            {
                rstrShortName = "VS2017 15.9";
                rstrFullName  = "Visual Studio 2017 version 15.9";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1915    VS2017 15.8    Visual Studio 2017 version 15.8
            else if (i32CompilerVersion >= 1915)
            {
                rstrShortName = "VS2017 15.8";
                rstrFullName  = "Visual Studio 2017 version 15.8";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1914    VS2017 15.7    Visual Studio 2017 version 15.7
            else if (i32CompilerVersion >= 1914)
            {
                rstrShortName = "VS2017 15.7";
                rstrFullName  = "Visual Studio 2017 version 15.7";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1913    VS2017 15.6    Visual Studio 2017 version 15.6
            else if (i32CompilerVersion >= 1913)
            {
                rstrShortName = "VS2017 15.6";
                rstrFullName  = "Visual Studio 2017 version 15.6";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1912    VS2017 15.5    Visual Studio 2017 version 15.5  7.0
            else if (i32CompilerVersion >= 1912)
            {
                rstrShortName = "VS2017 15.5";
                rstrFullName  = "Visual Studio 2017 version 15.5";
                rstrCSharpVer = "7.0";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1911    VS2017 15.3    Visual Studio 2017 version 15.3  7.0   4.6.2
            else if (i32CompilerVersion >= 1911)
            {
                rstrShortName = "VS2017 15.3";
                rstrFullName  = "Visual Studio 2017 version 15.3";
                rstrCSharpVer = "7.0";
                rstrDotNetVer = "4.6.2";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1910    VS2017 15.0    Visual Studio 2017 RTW (15.0)    7.0   4.6.1
            else if (i32CompilerVersion >= 1910)
            {
                rstrShortName = "VS2017 15.0";
                rstrFullName  = "Visual Studio 2017 RTW (15.0)";
                rstrCSharpVer = "7.0";
                rstrDotNetVer = "4.6.1";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1900    VS2015         Visual Studio 2015 (14.0)        6.0   4.6    4.6
            else if (i32CompilerVersion >= 1900)
            {
                rstrShortName = "VS2015";
                rstrFullName  = "Visual Studio 2015 (14.0)";
                rstrCSharpVer = "6.0";
                rstrDotNetVer = "4.6";
                rstrWpfVer    = "4.6";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1800    VS2013         Visual Studio 2013 (12.0)        5.0   4.5.1  4.5.1
            else if (i32CompilerVersion >= 1800)
            {
                rstrShortName = "VS2013";
                rstrFullName  = "Visual Studio 2013 (12.0)";
                rstrCSharpVer = "5.0";
                rstrDotNetVer = "4.5.1";
                rstrWpfVer    = "4.5.1";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1700    VS2012         Visual Studio 2012 (11.0)        5.0   4.5    4.5
            else if (i32CompilerVersion >= 1700)
            {
                rstrShortName = "VS2012";
                rstrFullName  = "Visual Studio 2012 (11.0)";
                rstrCSharpVer = "5.0";
                rstrDotNetVer = "4.5";
                rstrWpfVer    = "4.5";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1600    VS2010         Visual Studio 2010 (10.0)        4.0   4.0    4.0
            else if (i32CompilerVersion >= 1600)
            {
                rstrShortName = "VS2010";
                rstrFullName  = "Visual Studio 2010 (10.0)";
                rstrCSharpVer = "4.0";
                rstrDotNetVer = "4.0";
                rstrWpfVer    = "4.0";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1500    VS2008         Visual Studio 2008 (9.0)         3.0   3.5    3.5
            else if (i32CompilerVersion >= 1500)
            {
                rstrShortName = "VS2008";
                rstrFullName  = "Visual Studio 2008 (9.0)";
                rstrCSharpVer = "3.0";
                rstrDotNetVer = "3.5";
                rstrWpfVer    = "3.5";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1400    VS2005         Visual Studio 2005 (8.0)         2.0   2.0
            else if (i32CompilerVersion >= 1400)
            {
                rstrShortName = "VS2005";
                rstrFullName  = "Visual Studio 2005 (8.0)";
                rstrCSharpVer = "2.0";
                rstrDotNetVer = "2.0";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1310    VS2003         Visual Studio .NET 2003 (7.1)    1.0   1.1
            else if (i32CompilerVersion >= 1310)
            {
                rstrShortName = "VS2003";
                rstrFullName  = "Visual Studio .NET 2003 (7.1)";
                rstrCSharpVer = "1.0";
                rstrDotNetVer = "1.1";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1300    VS2002         Visual Studio .NET 2002 (7.0)    1.0   1.0
            else if (i32CompilerVersion >= 1300)
            {
                rstrShortName = "VS2002";
                rstrFullName  = "Visual Studio .NET 2002 (7.0)";
                rstrCSharpVer = "1.0";
                rstrDotNetVer = "1.0";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
                if (rstrCSharpVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  C# " + rstrCSharpVer);
                }
                if (rstrDotNetVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  .Net " + rstrDotNetVer);
                }
                if (rstrWpfVer.Length > 0)
                {
                    sbCurrentVersions.Append ("  WPF " + rstrWpfVer);
                }
            }
            //  1200    VS 6.0         Visual Studio 6.0
            else if (i32CompilerVersion == 1200)
            {
                rstrShortName = "VS 6.0";
                rstrFullName  = "Visual Studio 6.0";
                rstrCSharpVer = "";
                rstrDotNetVer = "";
                rstrWpfVer    = "";
                sbCurrentVersions.Append ("MSC Ver: " + strCompilerFullVersion + " (" + rstrShortName + ")");
            }

            return sbCurrentVersions.ToString ();
        }

        public static void LoadVersionData (System.Reflection.Assembly asm, ref string rstrAssemblyTitle, ref string rstrAssemblyVersion, ref string rstrAssemblyFileVersion,
                                            ref string rstrCompanyName, ref string rstrConfiguration, ref string rstrTargetFramework)
        {
            // Get the AssemblyVersion from the Fullname string
            // "PlotterWriterWpfUI, Version=0.2.1.5, Culture=neutral, PublicKeyToken=null"
            string strAsmFullname = asm.FullName;
            int iVersionStart = strAsmFullname.IndexOf (STRING_VERSION) + STRING_VERSION.Length;
            int iVersionLength = (iVersionStart > 0) ? strAsmFullname.IndexOf (',', iVersionStart) - iVersionStart : -1;
            if (iVersionStart > 0 &&
                iVersionLength > 0)
            {
                rstrAssemblyVersion = strAsmFullname.Substring (iVersionStart, iVersionLength);
            }

            IEnumerable<System.Reflection.CustomAttributeData> ienumCustAttribData = asm.CustomAttributes;
            foreach (System.Reflection.CustomAttributeData cad in ienumCustAttribData)
            {
                string strCad = cad.ToString ();
                if (strCad.IndexOf ("AssemblyTitle") >= 0)
                {
                    rstrAssemblyTitle = CGenericMethods.ExtractStringInQuotes (strCad);
                }
                else if (strCad.IndexOf ("AssemblyFileVersion") >= 0)
                {
                    rstrAssemblyFileVersion = CGenericMethods.ExtractStringInQuotes (strCad);
                }
                else if (strCad.IndexOf ("AssemblyCompany") >= 0)
                {
                    rstrCompanyName = CGenericMethods.ExtractStringInQuotes (strCad);
                }
                else if (strCad.IndexOf ("AssemblyConfiguration") >= 0)
                {
                    rstrConfiguration = CGenericMethods.ExtractStringInQuotes (strCad);
                }
                else if (strCad.IndexOf ("TargetFramework") >= 0)
                {
                    //"[System.Runtime.Versioning.TargetFrameworkAttribute(\".NETFramework,Version=v4.5\", FrameworkDisplayName = \".NET Framework 4.5\")]"
                    rstrTargetFramework = CGenericMethods.ExtractStringInQuotes (strCad, false);
                }

                if (rstrAssemblyTitle.Length > 0 &&
                    rstrAssemblyFileVersion.Length > 0 &&
                    rstrTargetFramework.Length > 0 &&
                    rstrCompanyName.Length > 0)
                {
                    break;
                }
            }
        }

        public static void ShowVersionInfo (ref string rstrAssemblyTitle, ref string rstrAssemblyVersion, ref string rstrCompanyName,
                                            ref string rstrConfiguration, ref string rstrTargetFramework)
        {
            string strShortName = "";
            string strFullName = "";
            string strCSharpVer = "";
            string strDotNetVer = "";
            string strWpfVer = "";
            string strAssemblyFileVersion = "";
            string strCurrentVersions = CGenericMethods.GetCurrentVersions (ref strShortName, ref strFullName, ref strCSharpVer, ref strDotNetVer, ref strWpfVer);
            CGenericMethods.LoadVersionData (System.Reflection.Assembly.GetExecutingAssembly (), ref rstrAssemblyTitle, ref rstrAssemblyVersion,
                                             ref strAssemblyFileVersion, ref rstrCompanyName, ref rstrConfiguration, ref rstrTargetFramework);
            DateTime dtBuildDate = new System.IO.FileInfo (System.Reflection.Assembly.GetExecutingAssembly ().Location).LastWriteTime;

            // PlotterTestApp 0.2.1.18 (32-bit) (Debug x86)
            // Copyright 2021 (C) Sacred Cat Software
            // Build Timestamp: 3/11/2021 10:23:23 AM
            // Target .Net Version: .NET Framework 4.5  C#5.0
            // .Net Version: 4.7.03062  Visual Studio 2012 (11.0)
            Console.WriteLine (rstrAssemblyTitle + " " + rstrAssemblyVersion + ((IntPtr.Size == 8) ? " (64-bit)" : " (32-bit) (" + rstrConfiguration + ")"));
            Console.WriteLine ("Copyright " + dtBuildDate.Year.ToString () + " (C) " + rstrCompanyName);
            Console.WriteLine ("Build Timestamp: " + dtBuildDate.ToString ());
            Console.WriteLine ("Target .Net Version: " + rstrTargetFramework + "  C#" + strCSharpVer);
            Console.WriteLine (".Net Version: " + CGenericMethods.LoadSetting (@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\", "Version") +
                               "  " + strFullName);
            Console.WriteLine ();
        }

        public static bool DotNet45Found ()
        {
            int iKeyreleaseKey = 0;

            RegistryKey regKey = RegistryKey.OpenBaseKey (RegistryHive.LocalMachine,
                                                          RegistryView.Registry32).OpenSubKey ("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\");

            if (regKey == null)
            {
                return false;
            }

            try
            {
                iKeyreleaseKey = Convert.ToInt32 (regKey.GetValue ("Release"));
            }
            catch (Exception)
            {
                // Fail silently
                return false;
            }

            if (iKeyreleaseKey >= 393273)
            {
                return true; // 4.6 RC or later
            }

            if (iKeyreleaseKey >= 379893)
            {
                return true; // 4.5.2 or later
            }

            if (iKeyreleaseKey >= 378675)
            {
                return true; // 4.5.1 or later
            }

            if (iKeyreleaseKey >= 378389)
            {
                return true; // 4.5 or later
            }

            // This line should never execute. A non-null release key should mean that 4.5 or later is installed. 
            return false; // No 4.5 or later version detected
        }
    }

    public static class CPlotterMath
    {
        // Center = start point = x1, y2
        // End point = x2, y2
        //
        //   0    x1 < x2   y1 == y2
        //  90    x1 == x2  y1 < y2
        // 180    x1 > x2   y1 == y2
        // 270    x1 == x2  y1 > y2
        //
        // Q I    x1 > x2   y1 > y2
        // Q II   x1 < x2   y1 > y2
        // Q III  x1 < x2   y1 < y2
        // Q IV   x1 > x2   y1 < y2
        //
        //                90
        //                |
        //        Q II    |    Q I
        //                |
        //                |
        //  180 ----------+---------- 0    
        //                |
        //                |
        //        Q III   |    Q IV
        //                |
        //               270
        //

        public static Point PolarToCartesian (Point ptCenter, int iAngle, int iRadius)
        {
            Point ptEnd = new Point (ptCenter.X, ptCenter.Y);

            iAngle %= 360;
            if (iAngle < 0)
            {
                iAngle += 360;
            }

            double dRadians = Math.PI * (double)iAngle / 180.0;

            ptEnd.X = (int)(Math.Cos (dRadians) * (double)iRadius) + ptCenter.X;
            ptEnd.Y = (int)(Math.Sin (dRadians) * (double)iRadius) + ptCenter.Y;

            return ptEnd;
        }

        public static void CartesianToPolar (Point ptStart, Point ptEnd, ref int riAngle, ref int riRadius)
        {
            double dX = ptEnd.X - ptStart.X;
            double dY = ptEnd.Y - ptStart.Y;

            double dSquareX = Math.Pow (dX, 2.0);
            double dSquareY = Math.Pow (dY, 2.0);
            double dHypotenuse = Math.Sqrt (dSquareX + dSquareY);
            double dHypotenuseRound = Math.Round (dHypotenuse);
            double dError = dHypotenuse - dHypotenuseRound;
            bool bRoundUp = (dError > 0.0);

            riRadius = (int)dHypotenuse;
            if (bRoundUp)
            {
                ++riRadius;
            }

            double dRadians = Math.Atan2 (dY, dX);
            double dAngle = dRadians * (180 / Math.PI);
            riAngle = (int)Math.Round (dAngle);

            if (dX < 0 && dY < 0) // X < 0 && Y < 0
            {
                // Quadrant II & III
                riAngle += 360;
            }
            else if (dX >= 0 && dY < 0)
            {
                // Quadrant IV
                riAngle += 360;
            }
        }

        public static Point RotatePoint (Point ptCenter, int iRotationAngle, Point ptRotatePoint)
        {

            int iAngle = 0;
            int iRadius = 0;
            CPlotterMath.CartesianToPolar (ptCenter, ptRotatePoint, ref iAngle, ref iRadius);

            // Add rotation angle
            int iRotateAngle = iAngle + iRotationAngle;

            // Get new rotated point
            Point ptRotatedPoint = PolarToCartesian (ptCenter, iRotateAngle, iRadius);
            //Console.WriteLine (string.Format ("X: {0,5:D} Y: {1,5:D} -> X: {2,5:D} Y: {3,5:D}  Angle: {4,5:D} Radius: {5,5:D}",
            //                   ptRotatePoint.X, ptRotatePoint.Y, ptRotatedPoint.X, ptRotatedPoint.Y, iAngle, iRadius));

            return ptRotatedPoint;
        }

        public static Point[] RotatePoints (Point ptCenter, int iRotationAngle, Point[] aptRotatePoints)
        {
            List<Point> lptRotatedPoints = new List<Point> ();

            foreach (Point pt in aptRotatePoints)
            {
                lptRotatedPoints.Add (RotatePoint (ptCenter, iRotationAngle, pt));
            }

            return lptRotatedPoints.ToArray ();
        }
    }
}

namespace HPGL
{
    public enum EPenSelect
    {
        ESelectNoPen     = 0,
        ESelectPen1      = 1,
        ESelectPen2      = 2,
        ESelectPen3      = 3,
        ESelectPen4      = 4,
        ESelectPen5      = 5,
        ESelectPen6      = 6,
        ESelectPen7      = 7,
        ESelectPen8      = 8,
        ESelectAllPens   = 9,
        ESelectPenRandom = 10
    };

    public static class CHPGL
    {
        #region DATA DEFINITIONS
        public struct SPrintQueueEntry
        {
            public string strDocumentName;
            public int    iDocumentLength;
        }
        
        public const int ENGINEERING_A_NORMAL_P1_X = 250;
        public const int  ENGINEERING_A_NORMAL_P1_Y  = 596;
        public const int  ENGINEERING_A_NORMAL_P2_X  = 10250;
        public const int  ENGINEERING_A_NORMAL_P2_Y  = 7796;

        public const int  ENGINEERING_A_ROTATED_P1_X = 154;
        public const int  ENGINEERING_A_ROTATED_P1_Y = 244;
        public const int  ENGINEERING_A_ROTATED_P2_X = 7354;
        public const int  ENGINEERING_A_ROTATED_P2_Y = 10244;

        public const int  ENGINEERING_B_NORMAL_P1_X  = 522;
        public const int  ENGINEERING_B_NORMAL_P1_Y  = 259;
        public const int  ENGINEERING_B_NORMAL_P2_X  = 15722;
        public const int  ENGINEERING_B_NORMAL_P2_Y  = 10259;

        public const int  ENGINEERING_B_ROTATED_P1_X = 283;
        public const int  ENGINEERING_B_ROTATED_P1_Y = 934;
        public const int  ENGINEERING_B_ROTATED_P2_X = 10283;
        public const int  ENGINEERING_B_ROTATED_P2_Y = 16134;

        public const int  MAX_X_VALUE                = 16640; // Returned by CHPGL.OutputHardClipLimits ()
        public const int  MAX_Y_VALUE                = 10408; // Returned by CHPGL.OutputHardClipLimits ()

        public const int  SP0_Y_VALUE                = 0000;
        public const int  SP1_Y_VALUE                = 0220;
        public const int  SP8_Y_VALUE                = 9800;
        public const int  SP_Y_DELTA                 = (SP8_Y_VALUE - SP1_Y_VALUE) / 7;
        //public const int  SP2_Y_VALUE                = SP1_Y_VALUE + (SP_Y_DELTA * 1); // 0220 + (1369 * 1); // 1589  1600;
        //public const int  SP3_Y_VALUE                = SP1_Y_VALUE + (SP_Y_DELTA * 2); // 0220 + (1369 * 2); // 2958  3100;
        //public const int  SP4_Y_VALUE                = SP1_Y_VALUE + (SP_Y_DELTA * 3); // 0220 + (1369 * 3); // 4327  4400;
        //public const int  SP5_Y_VALUE                = SP1_Y_VALUE + (SP_Y_DELTA * 4); // 0220 + (1369 * 4); // 4327  5800;
        //public const int  SP6_Y_VALUE                = SP1_Y_VALUE + (SP_Y_DELTA * 5); // 0220 + (1369 * 5); // 7065  7100;
        //public const int  SP7_Y_VALUE                = SP1_Y_VALUE + (SP_Y_DELTA * 6); // 0220 + (1369 * 6); // 8434  8550;

        public const char NUL                        = '\x00';
        public const char ETX                        = '\x03';
        public const char LF                         = '\x0A';
        public const char CR                         = '\x0D';

        //private const int  TOGGLE_PEN = -99;

        private static char s_cLabelTerminator = ETX; // ETX

        //private static WrapperBase s_wbPlotterPort = null;
        #endregion

        public static Point GenPenHolderPoint (EPenSelect ePenSelect)
        {
            return new Point (0, GetPenYAxisValue (ePenSelect));
        }

        public static Point GenPenHolderPoint (int iPen)
        {
            return new Point (0, GetPenYAxisValue (iPen));
        }

        public static int GetPenYAxisValue (EPenSelect ePenSelect)
        {
            return GetPenYAxisValue ((int)ePenSelect);
        }

        public static int GetPenYAxisValue (int iPen)
        {
            if (iPen < 1)
            {
                return SP0_Y_VALUE;
            }
            else if (iPen == 1)
            {
                return SP1_Y_VALUE;
            }
            else if (iPen == 8)
            {
                return SP8_Y_VALUE;
            }
            else if (iPen > 1 &&
                     iPen < 8)
            {
                return SP1_Y_VALUE + (SP_Y_DELTA * (iPen - 1));
            }
            else
            {
                throw new Exception ("CHPGL.GetPenYAxisValue pen value out of range");
            }
        }

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

            return string.Format ("\x1B.@:"); // Redundant default case to keep the compiler happy
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
        // Paper Size  P1        P2            Format
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

        public static string InputWindow (Point ptP1, Point ptP2) // Full support.
        {
            int iXll = ValidateX (ptP1.X);
            int iYll = ValidateY (ptP1.Y);

            int iXur = ValidateX (ptP2.X);
            int iYur = ValidateY (ptP2.Y);

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

        public static string Scale (Point ptP1, Point ptP2) // Full support.
        {
            return string.Format ("SC{0},{1},{2},{3};", ptP1.X, ptP2.X, ptP1.Y, ptP2.Y);
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
            // the range of 0-3 selects either B or A3 size paper
            // the range of 4-127 selects either A or A4 size paper
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

        public static string SelectPen (EPenSelect ePenSelect) // See Paragraph 6.4.3.
        {
            return SelectPen ((int)ePenSelect);
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

        public static string PlotAbsolute (Point pt) // Full support.
        {
            return string.Format ("PA{0},{1};", pt.X, pt.Y);
        }

        public static string PlotAbsolute (Point[] paPoints, bool bAddPenSteps = true) // Full support.
        {
            return PlotPoints (paPoints, true, bAddPenSteps);
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

        public static string PlotRelative (Point pt) // Full support.
        {
            return string.Format ("PR{0},{1};", pt.X, pt.Y);
        }

        public static string PlotRelative (Point[] paPoints, bool bAddPenSteps = true) // Full support.
        {
            return PlotPoints (paPoints, false, bAddPenSteps);
        }

        private static string PlotPoints (Point[] paPoints, bool bPlotAbsolue, bool bAddPenSteps = true) // Full support.
        {
            string strCommandCode = bPlotAbsolue ? "PA" : "PR";
            StringBuilder sbPlotPoints = new StringBuilder (strCommandCode);
            bool bPenDownAdded = false;
            bool bLastCharNumeric = false;

            foreach (Point pt in paPoints)
            {
                if (sbPlotPoints.Length > 2 &&
                    bLastCharNumeric)
                {
                    sbPlotPoints.Append (',');
                }

                sbPlotPoints.Append (pt.X.ToString () + ',' +
                                     pt.Y.ToString ());
                bLastCharNumeric = true;

                if (bAddPenSteps &&
                    !bPenDownAdded)
                {
                    sbPlotPoints.Append (";PD;" + strCommandCode);
                    bLastCharNumeric = false;
                    bPenDownAdded = true;
                }
            }
            sbPlotPoints.Append (';');

            if (bAddPenSteps)
            {
                sbPlotPoints.Append ("PU;");
            }

            return sbPlotPoints.ToString ();
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

        public static string PlotXAxisTick (int iXTickPositive, int iXTickNegative) // Full support.
        {
            if (iXTickNegative <= 0 &&
                iXTickPositive <= 0)
            {
                return "";
            }

            return PlotRelative (iXTickPositive, 0) + PlotRelative (-(iXTickPositive + iXTickNegative), 0) + PlotRelative (iXTickNegative, 0);
        }

        public static string PlotYAxisTick (int iXTickPositive, int iXTickNegative) // Full support.
        {
            if (iXTickNegative <= 0 &&
                iXTickPositive <= 0)
            {
                return "";
            }

            return PlotRelative (0, iXTickPositive) + PlotRelative (0, -(iXTickPositive + iXTickNegative)) + PlotRelative (0, iXTickNegative);
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

        //private static void OutputString (string strCommand, ref StringBuilder sbldBuffer, WrapperBase wb = null,
        //                                  bool bOutputToConsole = false, string strConsoleMessage = "")
        //{
        //    if (wb == null)
        //    {
        //        sbldBuffer.Append (strCommand);
        //    }
        //    else
        //    {
        //        wb.WriteTextString (strCommand);
        //    }

        //    if (bOutputToConsole)
        //    {
        //        if (strConsoleMessage.Length > 0)
        //        {
        //            Console.WriteLine (strConsoleMessage);
        //        }
        //        Console.WriteLine (strCommand);
        //    }
        //}

        public static bool AreBoxCoordinatesValid (int iBottom, int iTop, int iLeft, int iRight)
        {
            return (iTop   > iBottom      &&
                    iLeft  < iRight       &&
                    iTop   <= MAX_Y_VALUE &&
                    iRight <= MAX_X_VALUE);
        }
        #endregion

        #region DIGITIZE POINTS
        //public static List<int> ReadDigitizedPoints (WrapperBase wb)
        //{
        //    wb.WriteTextString (Initialize () + SelectPen () + PlotAbsolute (0, 0) + DigitizePoint ());

        //    string strTest = "";
        //    //Console.WriteLine (wb.QueryErrorText ());

        //    bool bFinished = false;
        //    int iLastXPos = -1,
        //        iLastYPos = -1,
        //        iLastPen  = -1;
        //    List<int> liPoints = new List<int> ();

        //    while (!bFinished)
        //    {
        //        int iPositionX = -1,
        //            iPositionY = -1,
        //            iPenPos    = -1;

        //        Thread.Sleep (100);
        //        string strStatus = wb.QueryStatus ();
        //        int iLen = strStatus.Length;
        //        if (iLen > 0 && iLen < 5)
        //        {
        //            int iStatus = CHPGL.SafeConvertToInt (strStatus);
        //            if ((iStatus & 4) > 0)
        //            {
        //                strTest = wb.QueryDigitizedPoint ();
        //                Console.WriteLine (strTest);
        //                int iComma1 = strTest.IndexOf (','),
        //                    iComma2 = strTest.IndexOf (',', iComma1 + 1);
        //                string strX = strTest.Substring (0, iComma1),
        //                       strY = strTest.Substring (iComma1 + 1, iComma2 - iComma1 - 1);
        //                Console.WriteLine ("X: {0}  Y: {1}:  Pen: {2}", strX, strY, strTest[iComma2 + 1]);
        //                iPositionX = Convert.ToInt16 (strX);
        //                iPositionY = Convert.ToInt16 (strY);
        //                iPenPos = Convert.ToInt16 (strTest[iComma2 + 1]) - (int)'0';
        //                //iPositionX = Convert.ToInt16 (strTest.Substring (0, iComma1));
        //                //iPositionY = Convert.ToInt16 (strTest.Substring (iComma1 + 1, iComma2 - iComma1 - 1));

        //                if (iLastXPos == iPositionX &&
        //                    iLastYPos == iPositionY)
        //                {
        //                    Console.WriteLine ("** Exit loop on same point entered twice.");
        //                    bFinished = true;
        //                    wb.WriteTextString (DigitizeClear ());
        //                    //Console.WriteLine (wb.QueryErrorText ());
        //                    break;
        //                }
        //                else
        //                {
        //                    liPoints.Add (iPositionX);
        //                    liPoints.Add (iPositionY);
        //                    liPoints.Add (iPenPos);
        //                    Console.WriteLine ("Adding point: X {0}  Y {1}  Pen {2}", iPositionX, iPositionY, iPenPos);
        //                }

        //                iLastXPos = iPositionX;
        //                iLastYPos = iPositionY;
        //                iLastPen  = iPenPos;

        //                wb.WriteTextString (CHPGL.DigitizePoint ());
        //            }
        //        }
        //    }

        //    return liPoints;
        //}

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
    }
}
