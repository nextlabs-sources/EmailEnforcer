using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSBase.Diagnose.Tools
{
    static class CommonTools
    {
        #region File and path
        public static void MakeStandardFolderPath(ref string strFolderPathRef)
        {
            strFolderPathRef = GetStandardFolderPath(strFolderPathRef);
        }
        public static string GetStandardFolderPath(string strFolderPathIn)
        {
            if (String.IsNullOrEmpty(strFolderPathIn))
            {
                strFolderPathIn = "";
            }
            else
            {
                if ('\\' == strFolderPathIn[strFolderPathIn.Length - 1])
                {
                    // OK
                }
                else
                {
                    strFolderPathIn += '\\';
                }
            }
            return strFolderPathIn;
        }
        public static string ConnectFolderRelativePath(string strFolderPath, string strRelativePath, string strDefaultRelativePath, bool bReturnFolderPathIfRelativeIsEmpty)
        {
            MakeStandardFolderPath(ref strFolderPath);

            // Get right relative path
            if (String.IsNullOrEmpty(strRelativePath))
            {
                strRelativePath = strDefaultRelativePath;
            }

            if (!String.IsNullOrEmpty(strRelativePath))
            {
                if ('\\' == strRelativePath[0])
                {
                    strRelativePath = strRelativePath.Substring(1);
                }
            }

            if (String.IsNullOrEmpty(strRelativePath))
            {
                return bReturnFolderPathIfRelativeIsEmpty ? strFolderPath : "";
            }
            else
            {
                return strFolderPath + strRelativePath;
            }
        }
        public static bool TrimSpecifyTopFolderFiles(string strFolderPath, int nMaxFileCount, string searchPattern)
        {
            bool bRet = false;
            try
            {
                if ((String.IsNullOrEmpty(strFolderPath)) || (0 > nMaxFileCount))
                {
                    CSLogger.OutputLog(LogLevel.Debug, "Parameters error during trim directory:[{0}] with max file count:[{1}]", new object[] { strFolderPath, nMaxFileCount });
                }
                else
                {
                    if (Directory.Exists(strFolderPath))
                    {
                        string[] szFiles = Directory.GetFiles(strFolderPath, searchPattern);
                        if (nMaxFileCount >= szFiles.Length)
                        {
                            // Not overflow, no need trimming
                        }
                        else
                        {
                            if (0 == nMaxFileCount)
                            {
                                // Delete all files
                                foreach (string strFilePath in szFiles)
                                {
                                    MyDeleteFile(strFilePath);
                                }
                            }
                            else
                            {
                                // Record file last write time and index
                                List<KeyValuePair<long, int>> lsFileLastWriteTimes = new List<KeyValuePair<long, int>>();
                                for (int i = 0; i < szFiles.Length; ++i)
                                {
                                    string strFilePath = szFiles[i];
                                    DateTime dtFile = File.GetLastWriteTime(strFilePath);

                                    lsFileLastWriteTimes.Add(new KeyValuePair<long, int>(dtFile.Ticks, i));
                                }
                                // Sort files with last write time
                                lsFileLastWriteTimes.Sort((first, second) => { return (first.Key == second.Key) ? 0 : ((first.Key > second.Key) ? 1 : -1); });

                                // Delete early files
                                int nNeedTrimmedFileCount = lsFileLastWriteTimes.Count - nMaxFileCount;
                                CSLogger.OutputLog(LogLevel.Debug, "Log folder:[{0}], current log files count:[{1}], max log files count:[{2}], need trimed files count:[{3}]", new object[] { strFolderPath, lsFileLastWriteTimes.Count, nMaxFileCount, nNeedTrimmedFileCount });

                                int nDeleteCount = 0;
                                foreach (KeyValuePair<long, int> pariItem in lsFileLastWriteTimes)
                                {
                                    if (nDeleteCount < nNeedTrimmedFileCount)
                                    {
                                        string strFilePath = szFiles[pariItem.Value];
                                        bool bDelRet = MyDeleteFile(strFilePath);
                                        CSLogger.OutputLog(LogLevel.Debug, "Remove log file:[{0}] with result:[{1}]", new object[] { strFilePath, bDelRet });
                                    }
                                    else
                                    {
                                        // Enough
                                        break;
                                    }
                                    ++nDeleteCount;
                                }
                            }
                        }
                        bRet = true;
                    }
                    else
                    {
                        bRet = false;
                        CSLogger.OutputLog(LogLevel.Debug, "Trim directory:[{0}] with max file count:[{1}] failed, the folder do not exist", new object[] { strFolderPath, nMaxFileCount });
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during trim directory:[{0}] with max file count:[{1}], Message:[{2}]", new object[] { strFolderPath, nMaxFileCount, ex.Message });
            }
            return bRet;
        }
        public static bool MyDeleteFile(string strFilePath)
        {
            bool bRet = true;
            try
            {
                File.Delete(strFilePath);
            }
            catch (Exception ex)
            {
                bRet = false;
                CSLogger.OutputLog(LogLevel.Debug, "Try to delete file:[{0}] but exception with message:[{1}]", new object[] { strFilePath, ex.Message });
            }
            return bRet;
        }
        #endregion

        #region Register
        public static string GetRegStringValue(RegistryKey obRegRootKey, string strRegKey, string strRegItemKey, string strDefaultValue)
        {
            string strValueRet = strDefaultValue;
            RegistryKey obRegItemKey = null;
            try
            {
                obRegItemKey = obRegRootKey.OpenSubKey(strRegKey, false);
                if (null == obRegItemKey)
                {
                    // Using default value
                }
                else
                {
                    object ReglogInstallDir = obRegItemKey.GetValue(strRegItemKey);
                    if (ReglogInstallDir == null)
                    {
                        // Using default value
                    }
                    else
                    {
                        strValueRet = Convert.ToString(ReglogInstallDir);
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during read reg string value from:[{0}, {1}, {2}], default value:[{3}]\n", new object[] { obRegRootKey, strRegKey, strRegItemKey, strDefaultValue }, ex);
            }
            finally
            {
                if (null != obRegItemKey)
                {
                    obRegItemKey.Close();
                }
            }
            return strValueRet;
        }
        #endregion
    }
}
