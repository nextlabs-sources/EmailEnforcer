using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;

namespace RouteAgent.Common
{
    public enum EMLOG_LEVEL
    {
        emLogLevelDebug,
        emLogLevelInfo,
        emLogLevelWarn,
        emLogLevelError,
        emLogLevelFatal
    }

    public class CLog
    {
        protected static List<KeyValuePair<log4net.ILog,CLog>> m_lstLogger = new List<KeyValuePair<ILog,CLog>>();
        private static Object logLocker = new Object();

        protected log4net.ILog m_log = null;

        public static void Init(string strCfgFileName)
        {
            Trace.WriteLine("Load Log config file from:" + strCfgFileName);
            
            //read config file
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(strCfgFileName));
        }


        /*construct, all is Protected, CLog can't be create directory, and must be get by GetLogger.*/
        protected CLog() { }
        protected CLog(CLog log) { }
        protected CLog(log4net.ILog log)
        {
            m_log = log;
        }

        public static CLog GetLogger(string strName)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(strName);
            return GetWrapperLog(log);
        }
        public static CLog GetLogger(Type typeName)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(typeName);
            
            return GetWrapperLog(log);
        }

        /* Test if a level is enabled for logging */
        public bool IsDebugEnabled { get { return m_log.IsDebugEnabled; } }
        public bool IsInfoEnabled  { get { return m_log.IsInfoEnabled; } }
        public bool IsWarnEnabled  { get { return m_log.IsWarnEnabled; } }
        public bool IsErrorEnabled { get { return m_log.IsErrorEnabled; } }
        public bool IsFatalEnabled { get { return m_log.IsFatalEnabled; } }

        public void OutputLog(EMLOG_LEVEL emLogLevel, string strMessage)
        {
            OutputLog(emLogLevel, "{0}", strMessage);
        }
        public void OutputLog(EMLOG_LEVEL emLogLevel, string strFormat, params object[] szArgs)
        {
            try
            {
                switch (emLogLevel)
                {
                    case EMLOG_LEVEL.emLogLevelDebug:
                        {
                            if (IsDebugEnabled)
                            {
                                m_log.DebugFormat(strFormat, szArgs);
                            }
                            break;
                        }
                    case EMLOG_LEVEL.emLogLevelInfo:
                        {
                            if (m_log.IsInfoEnabled)
                            {
                                m_log.InfoFormat(strFormat, szArgs);
                            }
                            break;
                        }
                    case EMLOG_LEVEL.emLogLevelWarn:
                        {
                            if (IsWarnEnabled)
                            {
                                m_log.WarnFormat(strFormat, szArgs);
                            }
                            break;
                        }
                    case EMLOG_LEVEL.emLogLevelError:
                        {
                            if (IsErrorEnabled)
                            {
                                m_log.ErrorFormat(strFormat, szArgs);
                            }
                            break;
                        }
                    case EMLOG_LEVEL.emLogLevelFatal:
                        {
                            if (IsFatalEnabled)
                            {
                                m_log.FatalFormat(strFormat, szArgs);
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            catch (Exception /*ex*/)
            {
                // OutputTraceLog("Exception happened in CLog::OutputLog,[{0}]\n", ex.Message);
            }
        }

        /* Log a message object */
        public void Debug(object message) { m_log.Debug(message); }
        public void Info(object message)  { m_log.Info(message); }
        public void Warn(object message)  { m_log.Warn(message); }
        public void Error(object message) { m_log.Error(message); }
        public void Fatal(object message) { m_log.Fatal(message); }

        /* Log a message object and exception */
        public void Debug(object message, Exception t) { m_log.Debug(message, t); }
        public void Info(object message, Exception t)  { m_log.Info(message, t); }
        public void Warn(object message, Exception t)  { m_log.Warn(message, t); }
        public void Error(object message, Exception t) { m_log.Error(message, t); }
        public void Fatal(object message, Exception t) { m_log.Fatal(message, t); }


        protected static CLog GetWrapperLog(log4net.ILog log)
        {
            CLog existWrapperLog = FindExistWrapperLog(log);

            if(null==existWrapperLog)
            {
                return CreateWrapperLog(log);
            }
            else
            {
                return existWrapperLog;
            }
        }
        protected static CLog FindExistWrapperLog(log4net.ILog log)
        {
            CLog wrapperLog = null;

            lock(logLocker)
            {
                foreach (KeyValuePair<log4net.ILog, CLog> wrapperLogInfo in m_lstLogger)
                {
                    if (wrapperLogInfo.Key.Equals(log))
                    {
                        wrapperLog = wrapperLogInfo.Value;
                        break;
                    }
                }
            }

            return wrapperLog;
        }
        protected static CLog CreateWrapperLog(log4net.ILog log)
        {
            CLog wrapperLog = new CLog(log);

            lock (logLocker)
            {
                m_lstLogger.Add(new KeyValuePair<ILog, CLog>(log, wrapperLog));
            }

            return wrapperLog;
        }

		private static void InitLogInfo(ILog iLog, string strAppenderName, string strStandardLogFolderPath)
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
					rfa.File = strStandardLogFolderPath + "SharepointEnforcer_" + obCurProcess.ProcessName + "_" + obCurProcess.Id.ToString() + ".log";
					rfa.ActivateOptions();
				}
			}
			catch (Exception ex)
			{
				MyOutputDebugString("Exception during SetCELogPath:{0}", ex.Message + ex.StackTrace);
			}
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

	}
}
