using CSBase.Diagnose;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RouteAgent.Common
{
    public class ObligationFile
    {
        public string strFilePullPath;
        public bool bEncrypt;
        public Dictionary<string, string> dirNormalTags;
        public Dictionary<string, string> dirRmsTags;
        public static void RecordFileTagOperate(List<RouteAgent.Common.ObligationFile> lisObligationFiles,string strEmailOutputTempDir, string strFileName, Dictionary<string, string> dirNormalTags, Dictionary<string, string> dirRMSTags, bool bEncrypt)
        {
            string strDestDirectory = strEmailOutputTempDir + lisObligationFiles.Count;
            string strDestFileFullPath = strDestDirectory + "\\" + strFileName;
            if (System.IO.File.Exists(strDestFileFullPath))
            {
                RouteAgent.Common.ObligationFile ObligationFile = new ObligationFile();
                ObligationFile.bEncrypt = bEncrypt;
                ObligationFile.dirNormalTags = dirNormalTags;
                ObligationFile.dirRmsTags = dirRMSTags;
                ObligationFile.strFilePullPath = strDestFileFullPath;
                lisObligationFiles.Add(ObligationFile);
            }
        }

        public static RouteAgent.Common.ObligationFile GetRecordFileObligation(List<RouteAgent.Common.ObligationFile> lisObligationFiles, Dictionary<string, string> dirNormalTags, Dictionary<string, string> dirRMSTags, bool bEncrypt)
        {
            CSLogger.OutputLog(LogLevel.Debug, "GetRecordFileObligation Start lisObligationFiles Count:" + lisObligationFiles.Count + " bEncrypt:" + bEncrypt);

            RouteAgent.Common.ObligationFile obligationFileResult = null;

            foreach (RouteAgent.Common.ObligationFile obligationFile in lisObligationFiles)
            {
                if (!bEncrypt)
                {
                    if (obligationFile.bEncrypt.Equals(false))
                    {
                        if (RouteAgent.Common.Function.EqualTag(obligationFile.dirNormalTags, dirNormalTags))
                        {
                            obligationFileResult = obligationFile;
                            CSLogger.OutputLog(LogLevel.Debug, "Find obligation file from cache, with no encrypt.");
                            break;
                        }

                    }
                }
                else
                {
                    if (obligationFile.bEncrypt.Equals(true))
                    {
                        if (RouteAgent.Common.Function.EqualTag(obligationFile.dirNormalTags, dirNormalTags) && RouteAgent.Common.Function.EqualTag(obligationFile.dirRmsTags,dirRMSTags))
                        {
                            obligationFileResult = obligationFile;
                            CSLogger.OutputLog(LogLevel.Debug, "Find obligation file from cache, with encrypt.");
                            break;
                        }

                    }
                }

            }
            return obligationFileResult;
        }

    }
}
