using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

using CSBase.Diagnose.Tools;
using System.Threading;
using System.Threading.Tasks;

namespace CSBase.Diagnose
{
    public class MemoryDump
    {
        #region Public flags
        // Taken almost verbatim from http://blog.kalmbach-software.de/2008/12/13/writing-minidumps-in-c/
        [Flags]
        public enum Option : uint
        {
            // From dbghelp.h:
            Normal = 0x00000000,
            WithDataSegs = 0x00000001,
            WithFullMemory = 0x00000002,
            WithHandleData = 0x00000004,
            FilterMemory = 0x00000008,
            ScanMemory = 0x00000010,
            WithUnloadedModules = 0x00000020,
            WithIndirectlyReferencedMemory = 0x00000040,
            FilterModulePaths = 0x00000080,
            WithProcessThreadData = 0x00000100,
            WithPrivateReadWriteMemory = 0x00000200,
            WithoutOptionalData = 0x00000400,
            WithFullMemoryInfo = 0x00000800,
            WithThreadInfo = 0x00001000,
            WithCodeSegs = 0x00002000,
            WithoutAuxiliaryState = 0x00004000,
            WithFullAuxiliaryState = 0x00008000,
            WithPrivateWriteCopyMemory = 0x00010000,
            IgnoreInaccessibleMemory = 0x00020000,
            ValidTypeFlags = 0x0003ffff,
        };
        public enum ExceptionInfo
        {
            None,
            Present
        }
        #endregion

        #region Public static methods
        public static bool WriteCurrentMemoryDump()
        {
            MemoryDump obMemoryDumpIns = MemoryDump.GetInstance();
            return obMemoryDumpIns.WriteDumpFile();
        }
        #endregion

        #region Sigeton
        static private object s_obLockForInstance = new object();
        static private MemoryDump s_obMemoryDumpIns = null;
        static private MemoryDump GetInstance()
        {
            if (null == s_obMemoryDumpIns)
            {
                lock (s_obLockForInstance)
                {
                    if (null == s_obMemoryDumpIns)
                    {
                        s_obMemoryDumpIns = new MemoryDump();
                    }
                }
            }
            return s_obMemoryDumpIns;
        }
        private MemoryDump()
        {

        }
        #endregion

        private bool CheckAndInit()
        {
            bool bInited = InitedField;
            if (!bInited)
            {
                DiagnoseGlobalInfo obDiagnoseGlobalInfoIns = DiagnoseGlobalInfo.GetInstance();
                if (obDiagnoseGlobalInfoIns.InitedField)
                {
                    bInited = InnerInit(obDiagnoseGlobalInfoIns.ProductNameField, obDiagnoseGlobalInfoIns.DumpOutputFolderField);
                }
            }
            return bInited;
        }
        private bool InnerInit(string strProductName, string strDumpOutputFolder)
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
                    if (String.IsNullOrEmpty(strDumpOutputFolder))
                    {
                        bRet = false;
                    }
                    else
                    {
                        ProductNameField = strProductName;

                        CommonTools.MakeStandardFolderPath(ref strDumpOutputFolder);
                        DumpOutputStandardFolderField = strDumpOutputFolder;


                        Task obTaskForCleanLogFiles = new Task(ThreadCleanDumpFiles);
                        obTaskForCleanLogFiles.Start();

                        bRet = InitedField = true;

                        ForceTrimDumpFiles();
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Fatal, "Exception during Memory dump instance init, parameters:[{0}, {1}]", new object[] { strProductName, strDumpOutputFolder }, ex);
            }
            return bRet;
        }

        #region Write dumps
        private bool WriteDumpFile()
        {
            bool bRet = false;
            try
            {
                bool bInited = CheckAndInit();
                if (bInited)
                {

                    DateTime dtNow = DateTime.Now;
                    string strTimeNow = String.Format("{0:d4}{1:d2}{2:d2}{3:d2}{4:d2}{5:d2}{6:d4}", dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, dtNow.Minute, dtNow.Second, dtNow.Millisecond);

                    string strDumpFile = DumpOutputStandardFolderField + ProductNameField + strTimeNow + ".dmp";
                    using (FileStream fs = new FileStream(strDumpFile, FileMode.Create, FileAccess.Write, FileShare.Write))
                    {
                        bRet = WriteDumpFile(fs.SafeFileHandle, Option.WithFullMemory);
                    }
                }
                else
                {
                    CSLogger.OutputLog(LogLevel.Error, "Try to write dump file but the init flag is false, code error, please check");
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Fatal, "Exception during write dump file for:[{0}] in [1{}]", new object[] { ProductNameField, DumpOutputStandardFolderField }, ex);
            }
            return bRet;
        }
        private bool WriteDumpFile(SafeHandle fileHandle, Option dumpType)
        {
            return WriteDumpFile(fileHandle, dumpType, ExceptionInfo.Present);
        }
        private bool WriteDumpFile(SafeHandle fileHandle, Option options, ExceptionInfo exceptionInfo)
        {
            bool bRet = false;
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                IntPtr currentProcessHandle = currentProcess.Handle;
                uint currentProcessId = (uint)currentProcess.Id;

                NativeMethods.MiniDumpExceptionInformation exp;
                exp.ThreadId = NativeMethods.GetCurrentThreadId();
                exp.ClientPointers = false;
                exp.ExceptionPointers = IntPtr.Zero;
                if (exceptionInfo == ExceptionInfo.Present)
                {
                    exp.ExceptionPointers = System.Runtime.InteropServices.Marshal.GetExceptionPointers();
                }

                if (exp.ExceptionPointers == IntPtr.Zero)
                {
                    bRet = NativeMethods.MiniDumpWriteDump(currentProcessHandle, currentProcessId, fileHandle, (uint)options, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    bRet = NativeMethods.MiniDumpWriteDump(currentProcessHandle, currentProcessId, fileHandle, (uint)options, ref exp, IntPtr.Zero, IntPtr.Zero);
                }
                return bRet;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Fatal, "Exception during write dump file with parameters:[{0}, {1}, {2}]", new object[] { fileHandle, options, exceptionInfo }, ex);
            }
            return bRet;
        }
        #endregion

        #region Dump files manager
        private void ForceTrimDumpFiles()
        {
            m_obEventForCleanDumpFiles.Set();
        }
        private void ThreadCleanDumpFiles()
        {
            const int knCleanIntervalMs = 12 * 60 * 1000;
            const int knMaxDumpFiles = 3;
            const string kstrDumpFilePatten = "*.dump";

            MemoryDump obMemoryDumpIns = MemoryDump.GetInstance();
            CSLogger.OutputLog(LogLevel.Info, "The dump file clean thread start");
            bool bContinue = true;

            do
            {
                try
                {
                    CSLogger.OutputLog(LogLevel.Info, "Begin wait clean dump file event, timeout setting:[{0}]", new object[] { knCleanIntervalMs });
                    bool bWaitRet = obMemoryDumpIns.m_obEventForCleanDumpFiles.WaitOne(knCleanIntervalMs);

                    CSLogger.OutputLog(LogLevel.Info, "End wait clean dump file event with result:[{0}] and begin to do clean", new object[] { bWaitRet ? "Singled" : "Timeout" });

                    CommonTools.TrimSpecifyTopFolderFiles(obMemoryDumpIns.DumpOutputStandardFolderField, knMaxDumpFiles, kstrDumpFilePatten);
                    CSLogger.OutputLog(LogLevel.Info, "End clean log folder:[{0}] with max files:[{1}] in patten:[{2}]", new object[] { obMemoryDumpIns.DumpOutputStandardFolderField, knMaxDumpFiles, kstrDumpFilePatten });

                    bContinue = true;
                }
                catch (Exception ex)
                {
                    // Exception, exit
                    CSLogger.OutputLog(LogLevel.Error, "Exception during clean dump file, please check", null, ex);
                    bContinue = true;
                }
            } while (bContinue);
            CSLogger.OutputLog(LogLevel.Info, "The dump file clean thread stop");
        }
        #endregion


        #region Fields
        private bool InitedField { get; set; }
        private string ProductNameField { get; set; }
        private string DumpOutputStandardFolderField { get; set; }
        #endregion

        #region Members
        private AutoResetEvent m_obEventForCleanDumpFiles = new AutoResetEvent(false);
        #endregion
    }
}
