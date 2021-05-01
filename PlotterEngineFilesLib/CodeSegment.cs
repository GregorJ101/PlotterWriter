            string[] astrListOfDocuments = m_objPlotterEngine.GetListOfDocuments ();

            if (m_objPlotterEngine.GetPlotterPort () == CPlotterEngine.EPlotterPort.ESerialPort)
            {
                SPlotEntry sPlotEntry           = new SPlotEntry ();
                sPlotEntry.slPrintQueueEntries  = new SortedList<string, int> ();
                sPlotEntry.lstrDuplicateEntries = new List<string> ();
                m_lstPlotEntries.Add (sPlotEntry);

                //int iTestQueueLength = 0;
                int iTestQueueSize   = 0;

                foreach (string str in astrListOfDocuments)
                {
                    if (sPlotEntry.slPrintQueueEntries.ContainsKey (str))
                    {
                        sPlotEntry.lstrDuplicateEntries.Add (str);
                    }
                    else
                    {
                        int iEntryLength = 0;
                        int iLastUnderscoreIdx = str.LastIndexOf ('_');
                        if (iLastUnderscoreIdx > 0 &&
                            iLastUnderscoreIdx < str.Length)
                        {
                            string strEntryLength = str.Substring (iLastUnderscoreIdx);
                            iEntryLength          = CGenericMethods.SafeConvertToInt (strEntryLength);
                            iTestQueueSize       += iEntryLength;
                        }

                        sPlotEntry.slPrintQueueEntries.Add (str, iEntryLength);
                    }
                }

                sPlotEntry.strPlotName  = strPlotName;
                //sPlotEntry.iQueueSize  = m_iTestQueueLength /*m_objPlotterEngine.GetQueueLength ()*/ - iOldQueueLength;
                sPlotEntry.iQueueLength = m_objPlotterEngine.GetQueueLength () - iQueueLengthBefore;
                sPlotEntry.iQueueSize   = m_objPlotterEngine.GetQueueSize () - iQueueSizeBefore;
                Debug.Assert (sPlotEntry.iQueueLength == sPlotEntry.slPrintQueueEntries.Count);
                Debug.Assert (sPlotEntry.iQueueSize == iTestQueueSize);

                if (bShowNewEntry)
                {
                    //Console.WriteLine ("  New: " + strPlotName + " [" + m_iTestQueueLength + ']');
                    Console.WriteLine (strPlotName                                      + " ["  +
                                       m_objPlotterEngine.GetQueueLength ().ToString () + "] [" +
                                       m_objPlotterEngine.GetQueueSize ().ToString ()   + ']');
                    ShowQueueContents ();
                }
