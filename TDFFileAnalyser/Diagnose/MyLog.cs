using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Nextlabs.TDFFileAnalyser
{
	enum LogLevel
	{
		Fatal = 0,
		Error = 1,
		Warn = 2,
		Info = 3,
		Debug = 4
	}

	// Singleton
	class MyLog
    {
        #region Public log tools

        public static void OutputLog(LogLevel emLogLevel, string strFormat, params object[] szArgs)
        {
            GetInstance().ImpOutputDebugString(strFormat, szArgs);
        }
        #endregion

        #region Singleton
        static private object s_obLockForMyLogInstance = new object();
        static private MyLog s_obMylogIns = null;
        private const string s_kstrFilePath = "TDFFileAnalyser.log";
        static private MyLog GetInstance()
        {
            if (null == s_obMylogIns)
            {
                lock (s_obLockForMyLogInstance)
                    if (null == s_obMylogIns)
                    {
                        s_obMylogIns = new MyLog();
                    }
            }
            return s_obMylogIns;
        }

        private MyLog()
        {

        }
        ~MyLog() { }
        #endregion

        private void ImpOutputDebugString(string strFormat, params object[] szArgs)
        {
            try
            {
                // Empty
            }
            catch (Exception)
            {

            }
        }
    }
}
