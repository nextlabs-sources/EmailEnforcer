using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CSBase.Diagnose;
using System.Reflection;



namespace CSBase.Common
{
    public static class FileTools
    {
        #region Application and Assembly
        public static string GetApplicationFile()
        {
            System.Reflection.Assembly exeAssembly = System.Reflection.Assembly.GetEntryAssembly();
            if (null != exeAssembly)
            {
                string codeBase = exeAssembly.CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return path;
            }
            else
            {
                return string.Empty;
            }
        }
        public static string GetApplicationPath()
        {
            string strAppfile = GetApplicationFile();
            if (!strAppfile.Equals(string.Empty))
            {
                return Path.GetDirectoryName(strAppfile) + "\\";
            }
            return string.Empty;
        }

        public static Assembly LoadAssemble(string strAssemble)
        {
            Assembly resultAssemble = null;
            try
            {
                resultAssemble = Assembly.Load(strAssemble);
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                CSLogger.OutputLog(LogLevel.Error, errorMessage);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during load assembly:[{0}]", new object[] { strAssemble }, ex);
            }
            return resultAssemble;
        }

        public static Type[] GetTypesFromAssemble(Assembly assemble)
        {
            Type[] types = null;
            try
            {
                types = assemble.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                CSLogger.OutputLog(LogLevel.Error, errorMessage);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during get types from assembly:[{0}]", new object[] { assemble }, ex);
            }
            return types;
        }
        #endregion

        #region File path
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
        #endregion

        #region File operation
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

        public static bool SaveToFile(string strFilePath, FileMode emFileMode, string strFileContent)
        {
            bool bRet = false;
            FileStream fs = null;
            StreamWriter sw = null;
            try
            {
                fs = new FileStream(strFilePath, emFileMode);
                sw = new StreamWriter(fs);
                sw.Write(strFileContent);
                sw.Flush();
                bRet = true;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception in SaveObjectInfoIntoFile for file:[{0}], mode:[{1}]\n", new object[] { strFilePath, emFileMode }, ex);
            }
            finally
            {
                if (null != sw)
                {
                    sw.Close();
                }
                if (null != fs)
                {
                    fs.Close();
                }
            }
            return bRet;
        }
        public static string ReadAllFileContent(string strFilePath, FileMode emFileMode)
        {
            string strFileContent = "";
            FileStream fs = null;
            StreamReader sr = null;
            try
            {
                fs = new FileStream(strFilePath, emFileMode);
                sr = new StreamReader(fs);
                strFileContent = sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception in SaveObjectInfoIntoFile, file:[{0}], mode:[{1}]\n", new object[] { strFilePath, emFileMode }, ex);
            }
            finally
            {
                if (null != sr)
                {
                    sr.Close();
                }
                if (null != fs)
                {
                    fs.Close();
                }
            }
            return strFileContent;
        }

        public static Encoding GetEncoding(string filename, System.Text.Encoding DefaultEncoding)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return DefaultEncoding;
        }
        #endregion
    }
}
