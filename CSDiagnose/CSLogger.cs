using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;

using CSBase.Diagnose.Tools;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CSBase.Diagnose
{
    public enum LogLevel
    {
        Fatal = 0,
        Error = 1,
        Warn = 2,
        Info = 3,
        Debug = 4
    }

    // Logger name: NLLogger
    // Adapper name: RollingFileAppender
    public class CSLogger
    {
        #region Const/Readonly values
        private const string g_kstrLoggerName = "NLLogger";
        private const string g_kstrFileAppenderName = "RollingFileAppender";
        #endregion

        #region Sigeton
        static private object s_obLockForInstance = new object();
        static private CSLogger s_obNLLoggerIns = null;
        static private CSLogger GetInstance()
        {
            if (null == s_obNLLoggerIns)
            {
                lock (s_obLockForInstance)
                {
                    if (null == s_obNLLoggerIns)
                    {
                        s_obNLLoggerIns = new CSLogger();
                    }
                }
            }
            return s_obNLLoggerIns;
        }
        private CSLogger()
        {
            InitedField = false;
            LogOutputStandardFolderField = "";
            LogConfigFilePathField = "";

            CheckAndInit();
        }
        #endregion

        #region Init
        private bool CheckAndInit()
        {
            bool bInited = InitedField;
            if (!bInited)
            {
                DiagnoseGlobalInfo obDiagnoseGlobalInfoIns = DiagnoseGlobalInfo.GetInstance();
                if (obDiagnoseGlobalInfoIns.InitedField)
                {
                    bInited = InnerInit(obDiagnoseGlobalInfoIns.LogOutputFolderField, obDiagnoseGlobalInfoIns.LogConfigFileFullPathField, obDiagnoseGlobalInfoIns.ProductNameField);
                }
            }
            return bInited;
        }
        private bool InnerInit(string strLogOutputFolder, string strLogConfigFileFullPath, string strProductName)
        {
            bool bRet = false;
            try
            {
                if (InitedField)
                {
                    // Already inited
                    bRet = true;
                }
                else
                {
                    if (String.IsNullOrEmpty(strLogOutputFolder) || String.IsNullOrEmpty(strLogConfigFileFullPath))
                    {
                        bRet = false;
                    }
                    else
                    {
                        CommonTools.MakeStandardFolderPath(ref strLogOutputFolder);
                        LogOutputStandardFolderField = strLogOutputFolder;
                        LogConfigFilePathField = strLogConfigFileFullPath;

                        // Init log config file and no need care exceptions
                        try
                        {
                            FileInfo obLogConfigFileInfo = new FileInfo(LogConfigFilePathField);
                            log4net.Config.XmlConfigurator.ConfigureAndWatch(obLogConfigFileInfo);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            MyOutputDebugString("Exception during NLLogger ctor:{0}", ex.Message + ex.StackTrace);
                        }

                        m_obl4nlog = LogManager.GetLogger(g_kstrLoggerName);
                        InitLoggerInfo(m_obl4nlog, g_kstrFileAppenderName, LogOutputStandardFolderField, ProductNameField);

                        Task obTaskForCleanLogFiles = new Task(ThreadCleanLogFiles);
                        obTaskForCleanLogFiles.Start();

                        bRet = InitedField = true;

                        ForceTrimLogFiles();
                    }
                }
            }
            catch (Exception ex)
            {
                MyOutputDebugString("Exception during NLLogger instance init:{{0}}, stack:[{1}]", ex.Message, ex.StackTrace);
            }
            return bRet;
        }
        #endregion

        #region Public log methods
        public static void OutputLog(LogLevel emLevel, string strFormat, object[] szArgs = null, Exception obExceptionInfo = null, bool bOutputStackTrace = true, bool bOutputInnerExceptionInfo = true, [CallerFilePath] string strCallerFileName = null, [CallerLineNumber] int nCallerFileNumber = 0, [CallerMemberName] string strCallerName = null)
        {
            CSLogger obNLLoggerIns = CSLogger.GetInstance();
            try
            {
                bool bInited = obNLLoggerIns.CheckAndInit();
                if (bInited)
                {
                    if (obNLLoggerIns.IsLogLevelSupport(emLevel))
                    {
                        string strLogerInfo = obNLLoggerIns.EstablishLogInfo(strFormat, szArgs, obExceptionInfo, bOutputStackTrace, bOutputInnerExceptionInfo, strCallerFileName, nCallerFileNumber, strCallerName);
                        obNLLoggerIns.InnerLog(emLevel, strLogerInfo);
                    }
                    else
                    {
                        // ignore
                    }
                }
                else
                {
#if DEBUG
                    string strLogerInfo = obNLLoggerIns.EstablishLogInfo(strFormat, szArgs, obExceptionInfo, bOutputStackTrace, bOutputInnerExceptionInfo, strCallerFileName, nCallerFileNumber, strCallerName);
                    MyOutputDebugString("Current the logger do not inited, debug message:[{0}]", strLogerInfo);
#endif
                }
            }
            catch (Exception ex)
            {
                LogLevel emLogLeve = LogLevel.Debug;
#if DEBUG
                emLogLeve = LogLevel.Fatal;
#endif
                obNLLoggerIns.InnerLog(emLogLeve, String.Format("Exception during write logs, log code place:[{0}, {1}, {2}], ExceptionMessage:[{3}]", strCallerFileName, nCallerFileNumber, strCallerName, ex.Message));
            }
        }
        #endregion

        #region Inner log methods
        private bool IsLogLevelSupport(LogLevel emLevel)
        {
            bool bRet = false;
            if (null == m_obl4nlog)
            {
                // do not support
            }
            else
            {
                switch (emLevel)
                {
                    case LogLevel.Fatal:
                        {
                            bRet = m_obl4nlog.IsFatalEnabled;
                            break;
                        }
                    case LogLevel.Error:
                        {
                            bRet = m_obl4nlog.IsErrorEnabled;
                            break;
                        }
                    case LogLevel.Warn:
                        {
                            bRet = m_obl4nlog.IsWarnEnabled;
                            break;
                        }
                    case LogLevel.Info:
                        {
                            bRet = m_obl4nlog.IsInfoEnabled;
                            break;
                        }
                    case LogLevel.Debug:
                        {
                            bRet = m_obl4nlog.IsDebugEnabled;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            return bRet;
        }
        private void InnerLog(LogLevel emLevel, string strMsg)
        {
            if (null == m_obl4nlog)
            {
                // ignore
            }
            else
            {
                switch (emLevel)
                {
                    case LogLevel.Fatal:
                        {
                            m_obl4nlog.Fatal(strMsg);
                            break;
                        }
                    case LogLevel.Error:
                        {
                            m_obl4nlog.Error(strMsg);
                            break;
                        }
                    case LogLevel.Warn:
                        {
                            m_obl4nlog.Warn(strMsg);
                            break;
                        }
                    case LogLevel.Info:
                        {
                            m_obl4nlog.Info(strMsg);
                            break;
                        }
                    case LogLevel.Debug:
                        {
                            m_obl4nlog.Debug(strMsg);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }
        private string EstablishLogInfo(string strFormat, object[] szArgs, Exception ex, bool bOutputStackTrace, bool bOutputInnerExceptionInfo, string strCallerFileName, int nCallerFileNumber, string strCallerName)
        {
            string strCallerInfo = "";
            if (String.IsNullOrEmpty(strCallerFileName))
            {
                // Stack frame: index 0, current
                // Stack frame: index 1, caller
                StackFrame obCallerStackFrame = GetCallerStackFrame(new StackTrace(true), 2);
                strCallerInfo = GetCallerInfo(obCallerStackFrame, "[", "] ");
            }
            else
            {
                strCallerInfo = String.Format("[{0}:{1}:{2}] ", Path.GetFileName(strCallerFileName), nCallerFileNumber, strCallerName);
            }

            string strMessageInfo = "";
            if (null == szArgs)
            {
                strMessageInfo = (null == strFormat) ? "" : strFormat;
            }
            else
            {
                strMessageInfo = String.Format(strFormat, szArgs);
            }

            string strExceptionInfo = "";
            if (null == ex)
            {
                // Empty
            }
            else
            {
                if (bOutputStackTrace)
                {
                    strExceptionInfo = String.Format("\nExceptionMessage:[{0}]\n\tStackTrace:[{1}]\n", ex.Message, ex.StackTrace);
                }
                else
                {
                    strExceptionInfo = String.Format("\nExceptionMessage:[{0}]\n", ex.Message);
                }

                if (bOutputInnerExceptionInfo)
                {
                    if (null == ex.InnerException)
                    {
                        // Empty
                    }
                    else
                    {
                        if (bOutputStackTrace)
                        {
                            strExceptionInfo += String.Format("InnerExceptionMessage:[{0}]\n\tInnerExceptionStackTrace:[{1}]\n", ex.InnerException.Message, ex.InnerException.StackTrace);
                        }
                        else
                        {
                            strExceptionInfo += String.Format("InnerExceptionMessage:[{0}]\n", ex.InnerException.Message);
                        }
                    }
                }
            }

            return strCallerInfo + strMessageInfo + strExceptionInfo;
        }
        private void ForceTrimLogFiles()
        {
            m_obEventForCleanLogFiles.Set();
        }
        private void ThreadCleanLogFiles()
        {
            const int knCleanIntervalMs = 12 * 60 * 1000;
            const int knMaxLogFiles = 10;
            // Need update make the condition more exactly
            const string kstrLogFilePatten = "*.log*";  // *.log *.log.1

            CSLogger obNLLoggerIns = CSLogger.GetInstance();
            CSLogger.OutputLog(LogLevel.Info, "The log file clean thread start");
            bool bContinue = true;

            do
            {
                try
                {
                    CSLogger.OutputLog(LogLevel.Info, "Begin wait clean log file event, timeout setting:[{0}]", new object[] { knCleanIntervalMs });
                    bool bWaitRet = obNLLoggerIns.m_obEventForCleanLogFiles.WaitOne(knCleanIntervalMs);
                    CSLogger.OutputLog(LogLevel.Info, "End wait clean log file event with result:[{0}] and begin to do clean", new object[] { bWaitRet ? "Singled" : "Timeout" });

                    CommonTools.TrimSpecifyTopFolderFiles(obNLLoggerIns.LogOutputStandardFolderField, knMaxLogFiles, kstrLogFilePatten);
                    CSLogger.OutputLog(LogLevel.Info, "End clean log folder:[{0}] with max files:[{1}] in patten:[{2}]", new object[] { obNLLoggerIns.LogOutputStandardFolderField, knMaxLogFiles, kstrLogFilePatten });

                    bContinue = true;
                }
                catch (Exception ex)
                {
                    // Exception, exit
                    CSLogger.OutputLog(LogLevel.Error, "Exception during clean log file, please check", null, ex);
                    bContinue = true;
                }
            } while (bContinue);
            CSLogger.OutputLog(LogLevel.Info, "The log file clean thread stop");
        }
        #endregion

        #region Inner independence tools
        private static void InitLoggerInfo(ILog iLog, string strAppenderName, string strStandardLogFolderPath, string strProductName)
        {
            try
            {
                bool bFind = false;
                AppenderCollection ac = ((Logger)iLog.Logger).Appenders;
                RollingFileAppender rfa = null;
                for (int i = 0; i < ac.Count; i++)
                {
                    rfa = ac[i] as RollingFileAppender;

                    if (rfa != null && rfa.Name.Equals(strAppenderName))
                    {
                        bFind = true;
                        break;
                    }
                }

                if (bFind && !string.IsNullOrEmpty(strStandardLogFolderPath))
                {
                    Process obCurProcess = Process.GetCurrentProcess();
                    rfa.File = strStandardLogFolderPath + strProductName + obCurProcess.ProcessName + "_" + obCurProcess.Id.ToString() + ".log";
                    rfa.ActivateOptions();
                }
            }
            catch (Exception ex)
            {
                MyOutputDebugString("Exception during SetCELogPath:{0}", ex.Message + ex.StackTrace);
            }
        }
        private static string GetCallerInfo(StackFrame obCallerStackFrame, string strPrefix, string strPostfix)
        {
            string strCallerInfoRet = "";
            if (null == obCallerStackFrame)
            {
                strCallerInfoRet = "";
            }
            else
            {
                string strFileFullName = obCallerStackFrame.GetFileName();
                if (String.IsNullOrEmpty(strFileFullName))
                {
                    // Crash module invoke, the file info will be empty
                    strCallerInfoRet = obCallerStackFrame.GetMethod().Name;
                }
                else
                {
                    string strFileName = Path.GetFileName(strFileFullName);
                    strCallerInfoRet = String.Format("{0}{1}:{2}:{3}{4}", strPrefix, strFileName, obCallerStackFrame.GetFileLineNumber(), obCallerStackFrame.GetMethod().Name, strPostfix);
                }
            }
            return strCallerInfoRet;
        }
        private static StackFrame GetCallerStackFrame(StackTrace obStackTrace, int nCallerIndex)
        {
            StackFrame obStackFrameRet = null;
            try
            {
                if ((null != obStackFrameRet) && (0 <= nCallerIndex))
                {
                    if (nCallerIndex < obStackTrace.FrameCount)
                    {
                        obStackFrameRet = obStackTrace.GetFrame(nCallerIndex);
                    }
                    else
                    {
                        obStackFrameRet = null;
                    }
                }
                else
                {
                    // Parameters error
                    MyOutputDebugString("Parameters error when we try to get caller stack frame, coding error, please check\n");
                }
            }
            catch (Exception ex)
            {
                MyOutputDebugString("Exception during GetCallerStackFrame:{0}, {1}\n", ex.Message, ex.StackTrace);
            }
            return obStackFrameRet;
        }
        private static void MyOutputDebugString(string message)
        {
            MyOutputDebugString("{0}", message);
        }
        private static void MyOutputDebugString(string format, params string[] szArgs)
        {
            try
            {
                Trace.TraceInformation(format, szArgs);
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region Fields
        private bool InitedField { get; set; }
        private string LogOutputStandardFolderField { get; set; }
        private string LogConfigFilePathField { get; set; }
        private string ProductNameField { get; set; }
        #endregion

        #region Members
        private AutoResetEvent m_obEventForCleanLogFiles = new AutoResetEvent(false);
        private ILog m_obl4nlog = null;
        #endregion
    }
}
