using CSBase.Diagnose;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace RouteAgent.Common
{
    public class FileModifyTimeComparer : IComparer
    {
        int IComparer.Compare(Object SourceFile, Object TagerFile)
        {

            FileInfo fiSource = SourceFile as FileInfo;
            FileInfo fiTager = TagerFile as FileInfo;
            return fiSource.CreationTime.CompareTo(fiTager.CreationTime);
        }
    }
    public class TransportMessageTraceLogReader
    {
        #region struts

        struct Log
        {
            public string DateTime;
            public string ClientIP;
            public string ClientHostName;
            public string ServerIP;
            public string ServerHostName;
            public string SourceContext;
            public string ConnectorId;
            public string Source;
            public string EventId;
            public string InternalMessageId;
            public string MessageId;
            public string NetworkMessageId;
            public string RecipientAddress;
            public string RecipientStatus;
            public string TotalBytes;
            public string RecipientCount;
            public string RelatedRecipientAddress;
            public string Reference;
            public string MessageSubject;
            public string SenderAddress;
            public string ReturnPath;
            public string MessageInfo;
            public string Directionality;
            public string TenantId;
            public string OriginalClientIp;
            public string OriginalServerIp;
            public string CustomData;
        }
        #endregion

        private static List<Log> m_log = new List<Log>();
        private static EventWaitHandle m_updateEvent = new ManualResetEvent(false);
        private static ReaderWriterLock m_rwlock = new ReaderWriterLock();
        private static DateTime m_lastLogDataTime= new DateTime(1990, 1,1);

        #region varable
        private const int I_FIELD_COUNT = 27;
        private const char C_FIELD_SPLIT = ',';
        #endregion
        /// <summary>
        /// Get User Client Type
        /// </summary>
        /// <param name="strMessageId"></param>
        /// <returns>OWA,AirSync</returns>
        public string GetClientType(string strMessageId)
        {
            int times = 0;
            string strClientType = string.Empty;

            for (; ;)
            {
                m_updateEvent.WaitOne(300);
                times++;

                string strSourceContent = string.Empty;
                try
                {
                    m_rwlock.AcquireReaderLock(Timeout.Infinite);
                    if(m_log!=null && m_log.Count>0)
                    {
                        for (int i = m_log.Count - 1; i > -1; i--)
                        {
                            if (m_log[i].MessageId.Equals(strMessageId))
                            {
                                strSourceContent = m_log[i].SourceContext;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    m_rwlock.ReleaseReaderLock();
                }

                if (!string.IsNullOrEmpty(strSourceContent))
                {
                    string[] arryProperties = strSourceContent.Split(C_FIELD_SPLIT);
                    for (int i = 0; i < arryProperties.Length; i++)
                    {
                        if (arryProperties[i].Contains("ClientType:"))
                        {
                            strClientType = arryProperties[i].Split(':')[1];
                            break;
                        }
                    }
                }
                if (strClientType != string.Empty || times > 3)
                {
                    break;
                }
                else
                {
                    m_updateEvent.Reset();
                }
            }
            if (strClientType == string.Empty)
            {
                CSLogger.OutputLog(LogLevel.Warn, "ReadLogHelp: Get Client Type Empty: " + strMessageId);
            }

            return strClientType;
        }

        private bool IsLastLogDataInited()
        {
            return m_lastLogDataTime != new DateTime(1990, 1, 1);
        }

        /*
        public string GetClientType(string strMessageId)
        {
            string strClientType = string.Empty;
            try
            {
                string strLogFolderPath = Config.MessageTracingLogPath;
                //CSLogger.OutputLog(LogLevel.Debug, "GetClientType Start MessageId:" + strMessageId + "  strLogFolderPath" + strLogFolderPath);
                foreach (string strCurrentLogFullPath in GetCurrentLogFullPath(strLogFolderPath))
                {
                    List<Log> lisCurrentLogs = GetLogList(strCurrentLogFullPath);

                    string strSourceContent = string.Empty;
                    for (int i = lisCurrentLogs.Count - 1; i > -1; i--)
                    {
                        if (lisCurrentLogs[i].MessageId.Equals(strMessageId) && lisCurrentLogs[i].EventId.Equals("SUBMIT"))
                        {
                            strSourceContent = lisCurrentLogs[i].SourceContext;
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(strSourceContent))
                    {
                        string[] arryProperties = strSourceContent.Split(C_FIELD_SPLIT);
                        for (int i = 0; i < arryProperties.Length; i++)
                        {
                            if (arryProperties[i].Contains("ClientType:"))
                            {
                                strClientType = arryProperties[i].Split(':')[1];
                                break;
                            }
                        }
                    }
                    if (strClientType != string.Empty)
                    {
                        break;
                    }
                }

                return strClientType;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, ex);
                return strClientType;
            }
            finally
            {
                //CSLogger.OutputLog(LogLevel.Debug, "GetClientType End Client Type:" + strClientType);

            }
        }
        */

        /********************
         * method to analysis log:
         * 1. get log file from NowDate+1(consider UTC time and local time) until we get the two log files.
         * 2. update the LoastLogDate, then next time we just scan log file from  NowDate+1 to LoastLogDate
        ********************/
        public void ThreadEntry()
        {
            int iWarnCount = 0;

            for (; ; )
            {
                try
                {
                    DirectoryInfo dirLogFolder = new DirectoryInfo(Config.MessageTracingLogPath);

                    if (dirLogFolder.Exists)  // @ fix bug 42408 by ray 2017/06/22
                    {
                        //find collection contains latest log
                        FileInfo[] fiLogs = null;
                        if (!IsLastLogDataInited())
                        {
                            fiLogs = dirLogFolder.GetFiles("MSGTRKMS*");
                            //  CSLogger.OutputLog(LogLevel.Debug, string.Format("ReadLogHelp::ThreadEntry IsLastLogDataInited false, get all logfile,count:{0}", fiLogs.Length));
                        }
                        else
                        {
                            List<FileInfo> lstFileInfo = new List<FileInfo>();
                            DateTime itTime = DateTime.Now;
                            itTime = itTime.AddDays(+1);

                            DateTime dtLastLog = new DateTime(m_lastLogDataTime.Year, m_lastLogDataTime.Month, m_lastLogDataTime.Day);
                            while (itTime >= dtLastLog)
                            {
                                String strTime = String.Format("MSGTRKMS{0:d4}{1:d2}{2:d2}*", itTime.Year, itTime.Month, itTime.Day);
                                FileInfo[] fiLogsLocal = dirLogFolder.GetFiles(strTime);

                                if (fiLogsLocal.Length > 0)
                                {
                                    lstFileInfo.AddRange(fiLogsLocal);
                                }

                                if (lstFileInfo.Count == 1)
                                {
                                    m_lastLogDataTime = itTime;
                                }
                                else if (lstFileInfo.Count >= 2)
                                {
                                    m_lastLogDataTime = itTime;
                                    //  CSLogger.OutputLog(LogLevel.Debug, string.Format("ReadLogHelp::ThreadEntry update lastlogtime to:{0:d4}{1:d2}{2:d2}", m_lastLogDataTime.Year,  m_lastLogDataTime.Month, m_lastLogDataTime.Day));
                                    break;
                                }

                                itTime = itTime.AddDays(-1);
                            }

                            //
                            fiLogs = lstFileInfo.ToArray();
                        }

                        //find the latest two logs
                        if ((fiLogs != null) && (fiLogs.Length > 0))  // @ by ray 2017/06/22
                        {
                            string strFileLittle = "";
                            DateTime dtLittle = new DateTime(1989, 1, 1);

                            string strFileLarge = "";
                            DateTime dtLarge = new DateTime(1990, 1, 1);

                            foreach (FileInfo temp in fiLogs)
                            {
                                if (temp.CreationTime.CompareTo(dtLarge) > 0)
                                {
                                    strFileLittle = strFileLarge;
                                    dtLittle = dtLarge;

                                    strFileLarge = temp.FullName;
                                    dtLarge = temp.CreationTime;
                                }
                                else if (temp.CreationTime.CompareTo(dtLittle) > 0)
                                {
                                    strFileLittle = temp.FullName;
                                    dtLittle = temp.CreationTime;
                                }
                            }

                            //update lastlog date
                            if (!IsLastLogDataInited())
                            {
                                m_lastLogDataTime = new DateTime(dtLittle.Year, dtLittle.Month, dtLittle.Day);
                                //CSLogger.OutputLog(LogLevel.Debug, string.Format("ReadLogHelp::ThreadEntry update lastlogtime for IsLastLogDataInited false to:{0:d4}{1:d2}{2:d2}", m_lastLogDataTime.Year, m_lastLogDataTime.Month, m_lastLogDataTime.Day));
                            }

                            try
                            {
                                List<Log> logLarger = null;
                                List<Log> logLittle = null;

                                if(!string.IsNullOrWhiteSpace(strFileLarge))
                                {
                                    logLarger = GetLogList(strFileLarge);
                                }

                                // CSLogger.OutputLog(LogLevel.Debug, string.Format("ReadLogHelp::ThreadEntry Read large time log:{1}, count:{0}", logLarger.Count,strFileLarge));

                                if ((logLarger.Count < 10) && (!string.IsNullOrWhiteSpace(strFileLittle)) )
                                {
                                    logLittle = GetLogList(strFileLittle);
                                    //  CSLogger.OutputLog(LogLevel.Debug, string.Format("ReadLogHelp::ThreadEntry Read little time log:{1}, count:{0}", logLittle.Count, strFileLittle));
                                }

                                m_rwlock.AcquireWriterLock(Timeout.Infinite);
                                m_log.Clear();
                                if (logLarger != null)
                                {
                                    m_log.AddRange(logLarger);
                                }
                                if (logLittle != null)
                                {
                                    m_log.AddRange(logLittle);
                                }

                            }
                            finally
                            {
                                m_rwlock.ReleaseWriterLock();
                            }
                        }
                        else
                        {
                            if ((iWarnCount = (++iWarnCount % 3600)) == 1) // print log every 30 minutes
                            {
                                CSLogger.OutputLog(LogLevel.Warn, "Not matched Format log file found in dirctory.");
                            }
                        }
                    }
                    else
                    {
                        if ((iWarnCount = (++iWarnCount % 3600)) == 1) // print log every 30 minutes
                        {
                            CSLogger.OutputLog(LogLevel.Warn, "Path \"" + Config.MessageTracingLogPath + "\" is not Exist.");
                        }
                    }
                }
                catch(Exception ex)
                {
                    CSLogger.OutputLog(LogLevel.Warn, string.Format("ThreadEntry exception:{0}", ex.ToString()));
                }
                finally
                {
                    m_updateEvent.Set();
                    Thread.Sleep(500);
                    m_updateEvent.Reset();
                }
            }
        }

        private List<string> GetCurrentLogFullPath(string strLogFolderPath)
        {
            List<string> lisCurrentLogFullPath = new List<string>();

            DateTime dt = DateTime.Now;
            String strTime = String.Format("MSGTRKMS{0:d4}{1:d2}{2:d2}*", dt.Year, dt.Month, dt.Day);
            DirectoryInfo dirLogFolder = new DirectoryInfo(strLogFolderPath);

            if (dirLogFolder.Exists)  // @ fix bug 42408 by ray 2017/06/22
            {
                FileInfo[] fiLogs = dirLogFolder.GetFiles(strTime);
                if (fiLogs.Length < 1 || fiLogs.Length > 23)
                {
                    strTime = String.Format("MSGTRKMS{0:d4}{1:d2}*", dt.Year, dt.Month);
                    fiLogs = dirLogFolder.GetFiles(strTime);
                }
                //CSLogger.OutputLog(LogLevel.Debug, "ReadLogHelp File Name: " + strTime);

                if (fiLogs.Length > 0)   // @ by ray 2017/06/22
                {
                    string str;
                    str = fiLogs[0].FullName;
                    dt = fiLogs[0].CreationTime;

                    foreach (FileInfo temp in fiLogs)
                    {
                        if (temp.CreationTime.CompareTo(dt) > 0)
                        {
                            str = temp.FullName;
                            dt = temp.CreationTime;
                        }
                    }
                    lisCurrentLogFullPath.Add(str);
                }
            }

            return lisCurrentLogFullPath;
        }
        private List<Log> GetLogList(string strFilePath)
        {
            List<Log> lisLog = new List<Log>();
            //string[] arryAllLine = File.ReadAllLines(strFilePath);

            List<string> lisArrLine = new List<string>();
            FileStream fs = null;
            try
            {
                fs = new FileStream(strFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        fs = null;
                        while (true)
                        {
                            string strReadLine = sr.ReadLine();
                            if (strReadLine != null)
                            {
                                lisArrLine.Add(strReadLine);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during try to read log list", null, ex);
            }
            finally
            {
                if(fs!=null)
                {
                    fs.Dispose();
                }
            }
            string[] arryAllLine = lisArrLine.ToArray<string>();
            for (int i = 0; i < arryAllLine.Length; i++)
            {
                string strTempLine = arryAllLine[i];
                if (strTempLine != null)
                {
                    string[] arryLine = GetSingLine(strTempLine);
                    if (arryLine.Length == I_FIELD_COUNT)
                    {
                        if (arryLine[8].Equals("SUBMIT") == false)
                        {
                            continue;
                        }
                        Log tempLog;
                        tempLog.DateTime = arryLine[0];
                        tempLog.ClientIP = arryLine[1];
                        tempLog.ClientHostName = arryLine[2];
                        tempLog.ServerIP = arryLine[3];
                        tempLog.ServerHostName = arryLine[4];
                        tempLog.SourceContext = arryLine[5];
                        tempLog.ConnectorId = arryLine[6];
                        tempLog.Source = arryLine[7];
                        tempLog.EventId = arryLine[8];
                        tempLog.InternalMessageId = arryLine[9];
                        tempLog.MessageId = arryLine[10];
                        tempLog.NetworkMessageId = arryLine[11];
                        tempLog.RecipientAddress = arryLine[12];
                        tempLog.RecipientStatus = arryLine[13];
                        tempLog.TotalBytes = arryLine[14];
                        tempLog.RecipientCount = arryLine[15];
                        tempLog.RelatedRecipientAddress = arryLine[16];
                        tempLog.Reference = arryLine[17];
                        tempLog.MessageSubject = arryLine[18];
                        tempLog.SenderAddress = arryLine[19];
                        tempLog.ReturnPath = arryLine[20];
                        tempLog.MessageInfo = arryLine[21];
                        tempLog.Directionality = arryLine[22];
                        tempLog.TenantId = arryLine[23];
                        tempLog.OriginalClientIp = arryLine[24];
                        tempLog.OriginalServerIp = arryLine[25];
                        tempLog.CustomData = arryLine[26];
                        lisLog.Add(tempLog);
                    }
                }
            }
            return lisLog;
        }
        private string[] GetSingLine(string strLine)
        {
            List<string> lisField = new List<string>();
            if (!strLine.StartsWith("#"))
            {
                for (int i = 0; i < I_FIELD_COUNT; i++)
                {
                    if (!strLine.StartsWith("\""))
                    {
                        int iIndex = strLine.IndexOf(",");
                        if (iIndex > 0)
                        {
                            lisField.Add(strLine.Substring(0, iIndex));
                            strLine = strLine.Substring(iIndex + 1, strLine.Length - iIndex - 1);
                        }
                        else
                        {
                            lisField.Add(string.Empty);
                            strLine = strLine.Substring(1, strLine.Length - 1);
                        }

                    }
                    else
                    {
                        int iIndex = strLine.IndexOf("\"", 1);
                        if (iIndex > 0)
                        {
                            lisField.Add(strLine.Substring(1, iIndex - 1));
                            if (iIndex + 2 < strLine.Length)
                            {
                                strLine = strLine.Substring(iIndex + 2, strLine.Length - iIndex - 2);
                            }
                        }
                    }
                }

            }
            return lisField.ToArray<string>();
        }
    }
}
