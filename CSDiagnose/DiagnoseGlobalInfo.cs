using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CSBase.Diagnose.Tools;
using System.IO;

namespace CSBase.Diagnose
{
    public class DiagnoseGlobalInfo
    {
        #region Sigeton
        static private object s_obLockForInstance = new object();
        static private DiagnoseGlobalInfo s_obDiagnoseGlobalInfoIns = null;
        static public DiagnoseGlobalInfo GetInstance()
        {
            if (null == s_obDiagnoseGlobalInfoIns)
            {
                lock (s_obLockForInstance)
                {
                    if (null == s_obDiagnoseGlobalInfoIns)
                    {
                        s_obDiagnoseGlobalInfoIns = new DiagnoseGlobalInfo();
                    }
                }
            }
            return s_obDiagnoseGlobalInfoIns;
        }
        private DiagnoseGlobalInfo()
        {
            InitedField = false;
            ProductNameField = "";
            AssemblyResolverStandardFolderField = "";
            LogOutputFolderField = "";
            LogConfigFileFullPathField = "";
            DumpOutputFolderField = "";
        }
        #endregion

        #region Init
        public bool Init(string strProductName, RegistryKey obRegRootKey, string strRegKey, string strRegIntallItemKey,
            string strRelativeLogOutputFolder, string strRelativeLogConfigFilePath, string strRelativeDumpOutputFolder, string strRelativeAssemblyResolverFolder)
        {
            bool bRet = false;
            if (InitedField)
            {
                // Already inited
                bRet = true;
            }
            else
            {
                if (String.IsNullOrEmpty(strProductName) || (null == obRegRootKey) || String.IsNullOrEmpty(strRegKey) || String.IsNullOrEmpty(strRegIntallItemKey))
                {
                    bRet = false;
                }
                else
                {
                    // Get install folder path from the pass in register key
                    string strInstallFolder = CommonTools.GetRegStringValue(obRegRootKey, strRegKey, strRegIntallItemKey, "");
                    if (String.IsNullOrEmpty(strInstallFolder))
                    {
                        bRet = false;
                    }
                    else
                    {
                        CommonTools.MakeStandardFolderPath(ref strInstallFolder);

                        bRet = InitedField = Init(strProductName,
                            CommonTools.ConnectFolderRelativePath(strInstallFolder, strRelativeLogOutputFolder, "Logs\\", false),
                            CommonTools.ConnectFolderRelativePath(strInstallFolder, strRelativeLogConfigFilePath, "Config\\Log.config", false),
                            CommonTools.ConnectFolderRelativePath(strInstallFolder, strRelativeDumpOutputFolder, "Logs\\", false),
                            CommonTools.ConnectFolderRelativePath(strInstallFolder, strRelativeAssemblyResolverFolder, "Bin\\", true)
                            );
                    }
                }
            }
            return bRet;
        }
        public bool Init(string strProductName, string strLogOutputFolder, string strLogConfigFileFullPath, string strDumpOutputFolder, string strAssemblyResolverFolder)
        {
            bool bRet = false;
            if (InitedField)
            {
                // Already inited
                bRet = true;
            }
            else
            {
                if (String.IsNullOrEmpty(strProductName))
                {
                    bRet = false;
                }
                else
                {
                    ProductNameField = strProductName;

                    LogOutputFolderField = CommonTools.GetStandardFolderPath(strLogOutputFolder);
                    LogConfigFileFullPathField = strLogConfigFileFullPath;

                    DumpOutputFolderField = CommonTools.GetStandardFolderPath(strDumpOutputFolder);
                    AssemblyResolverStandardFolderField = CommonTools.GetStandardFolderPath(strAssemblyResolverFolder);

                    bRet = InitedField = true;

                    InitAssemblyLoaderHelper();
                }
            }
            return bRet;
        }
        #endregion

        #region Assembly load resolver
        private bool InitAssemblyLoaderHelper()
        {
            bool bRet = Directory.Exists(AssemblyResolverStandardFolderField);
            if (bRet)
            {
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyLoadResolver;
            }
            CSLogger.OutputLog(LogLevel.Debug, "Add loader helper:{0}, reload dir:{1}\n", new object[] { bRet.ToString(), AssemblyResolverStandardFolderField });
            return bRet;
        }
        private System.Reflection.Assembly AssemblyLoadResolver(object sender, ResolveEventArgs args)
        {
            string[] szAssemblyInfo = args.Name.Split(',');
            if ((null != szAssemblyInfo) && (0 < szAssemblyInfo.Length))
            {
                string strCurAssemblyFullName = szAssemblyInfo[0] + ".dll";
                string strSFBAssemblyFullPathName = AssemblyResolverStandardFolderField + strCurAssemblyFullName;
                CSLogger.OutputLog(LogLevel.Debug, "AssemblyLoadResolver {0},{1},{2}:\n", new Object[] { args.Name, strSFBAssemblyFullPathName, strCurAssemblyFullName });
                if (File.Exists(strSFBAssemblyFullPathName))
                {
                    return System.Reflection.Assembly.LoadFrom(strSFBAssemblyFullPathName);
                }
            }
            return null;
        }
        #endregion

        #region public Fields
        public bool InitedField { get; private set; }
        public string ProductNameField { get; private set; }
        public string AssemblyResolverStandardFolderField { get; private set; }
        public string LogOutputFolderField { get; private set; }
        public string LogConfigFileFullPathField { get; private set; }
        public string DumpOutputFolderField { get; private set; }
        #endregion
    }
}
