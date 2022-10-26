using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using theLog = Nextlabs.TDFFileAnalyser.MyLog;

namespace Nextlabs.TDFFileAnalyser
{
    /*
     TDF Simple match steps::
    1. Config info:
        1. Max x-header length
            1. Header start length, MaxXHeaderEffectiveStartLength, ex: 2M
            2. Header end length, MaxXHeaderEffectiveEndLength, ex: 1K
        2. Ignore nodes: name and type
    2. Read x-header
        1. If file size not above than MaxXHeaderEffectiveStartLength + MaxXHeaderEffectiveEndLength (byte size), read whole file directly.
        2. If file size above than MaxXHeaderEffectiveStartLength + MaxXHeaderEffectiveEndLength (byte size)
            1. Read start header, length MaxXHeaderEffectiveStartLength
            2. Read end header, length MaxXHeaderEffectiveEndLength
            3. Connect these two parts as the x-header original content.
    3. Refining x-header
        1. Search the fist "<", mark the index as XML start index
        2. Search the last ">", mark the index as XML end index
        3. Extract the content from  start and end index as the x-header real content
    4. Trim ignore nodes
        1. Using regular to match the ignore node info
        2. According config to remove the node's value, attribute or the whole node
     */

    static public class TDFXHeaderExtral
    {
        static void OutputLog(string strLogInfo)
        {
            // CSLogger.OutputLog(LogLevel.Debug, strLogInfo);
        }
        static void OutputLog(string strFormat, params object[] szLogInfo)
        {
            // CSLogger.OutputLog(LogLevel.Debug, String.Format(strFormat, szLogInfo));
        }

        static public string GetXmlHeaderFromTDFFileByFilePath(string strTDFFilePath, bool bConvertToJson)
        {
            TDFXHeaderExtralConfig obTDFXHeaderExtralConfig = TDFXHeaderExtralConfig.GetInstance();

            string strTDFFileContent = "";
            bool bSupport = obTDFXHeaderExtralConfig.IsSupportFile(strTDFFilePath);
            if (bSupport)
            {
                strTDFFileContent = GetTDFFileContentByFilePath(strTDFFilePath, obTDFXHeaderExtralConfig.MaxStartEffectiveXHeaderLength, obTDFXHeaderExtralConfig.MaxEndEffectiveXHeaderLength);
                if (String.IsNullOrEmpty(strTDFFileContent))
                {
                    OutputLog("Get empty TDF x-header info from file:[{0}]\n", strTDFFilePath);
                }
                else
                {
                    strTDFFileContent = TrimTDFContent(strTDFFileContent, obTDFXHeaderExtralConfig.IgnoreNodes, true, bConvertToJson);
                }
            }
            else
            {
                strTDFFileContent = "";
            }
            return strTDFFileContent;
        }

        static public string GetXmlHeaderFromTDFFileByStreamReader(StreamReader obStreamReader, string strFileName, bool bConvertToJson)
        {
            TDFXHeaderExtralConfig obTDFXHeaderExtralConfig = TDFXHeaderExtralConfig.GetInstance();

            string strTDFFileContent = "";
            bool bSupport = obTDFXHeaderExtralConfig.IsSupportFile(strFileName);
            if (bSupport)
            {
                if (null == obStreamReader)
                {
                    MyLog.OutputLog(LogLevel.Error, "The stream reader for file:[{0}] is empty\n", strFileName);
                }
                else
                {
                    long nOrgPosition = obStreamReader.BaseStream.Position;
                    strTDFFileContent = GetTDFFileContentFromStreamReader(obStreamReader, obTDFXHeaderExtralConfig.MaxStartEffectiveXHeaderLength, obTDFXHeaderExtralConfig.MaxEndEffectiveXHeaderLength, strFileName);
                    if (String.IsNullOrEmpty(strTDFFileContent))
                    {
                        OutputLog("Get empty TDF x-header info from file:[{0}]\n", strFileName);
                    }
                    else
                    {
                        strTDFFileContent = TrimTDFContent(strTDFFileContent, obTDFXHeaderExtralConfig.IgnoreNodes, true, bConvertToJson);
                    }
                    obStreamReader.BaseStream.Seek(nOrgPosition, SeekOrigin.Begin);
                }
            }
            else
            {
                strTDFFileContent = "";
            }
            return strTDFFileContent;
        }

        static private string TrimTDFContent(string strTDFFileContent, Dictionary<string, EMNode_Element> dicIgnoreNode, bool bNeedRefiningTDFContent, bool bConvertToJson)
        {
            if (!String.IsNullOrEmpty(strTDFFileContent))
            {
                if (bNeedRefiningTDFContent)
                {
                    strTDFFileContent = InnerRefiningTDFFileContent(strTDFFileContent);
                }
                if (!String.IsNullOrEmpty(strTDFFileContent))
                {
                    strTDFFileContent = InnerTrimXHeaderNodes(strTDFFileContent, dicIgnoreNode);
                }
                if (!String.IsNullOrEmpty(strTDFFileContent))
                {
                     if (bConvertToJson)
                     {
                         strTDFFileContent = ConvertTDFXHeaderFromXmlToJSON(strTDFFileContent);
                     }
                }
            }
            return strTDFFileContent;
        }

        static private string InnerTrimXHeaderNodes(string strTDFXHeaderContent, Dictionary<string, EMNode_Element> dicIgnoreNode)
        {
            if (null != dicIgnoreNode)
            {
                foreach (var pairIgnoreNode in dicIgnoreNode)
                {
                    List<Group> lsMatchedGroup = MatchXmlNode(strTDFXHeaderContent, pairIgnoreNode);
                    if (null == lsMatchedGroup)
                    {
                        // No matched
                    }
                    else
                    {
                        int nRemovedCount = 0;
                        foreach (Group obCurMatchGroup in lsMatchedGroup)
                        {
                            strTDFXHeaderContent = RemoveDataContentByRegGroup(strTDFXHeaderContent, obCurMatchGroup, ref nRemovedCount);
                        }
                    }
                }
            }
            return strTDFXHeaderContent;
        }
        static private string InnerRefiningTDFFileContent(string strTDFFileContent)
        {
            // Remove xml comments
            strTDFFileContent = RemoveXMLComments(strTDFFileContent);

            // Find Xml start place
            int nStartXmlIndex = strTDFFileContent.IndexOf('<');
            if (-1 != nStartXmlIndex)
            {
                if (0 != nStartXmlIndex)
                {
                    strTDFFileContent = strTDFFileContent.Remove(0, nStartXmlIndex);
                }

                // Find root node
                string strRootNodeName = GetXmlRootNodeName(strTDFFileContent);
                if (!String.IsNullOrEmpty(strRootNodeName))
                {
                    // Find root node end space
                    int nRootNodeEndIndex = strTDFFileContent.LastIndexOf(strRootNodeName);
                    if (-1 != nRootNodeEndIndex)
                    {
                        int nEndXmlIndex = strTDFFileContent.IndexOf('>', nRootNodeEndIndex + strRootNodeName.Length);
                        if (-1 != nEndXmlIndex)
                        {
                            return strTDFFileContent.Substring(0, nEndXmlIndex + 1);
                        }
                    }
                }
            }
            return "";
        }
        static private string GetTDFFileContentByFilePath(string strTDFFilePath, int nMaxStartEffectiveXHeaderLength, int nMaxEndEffectiveXHeaderLength)
        {
            string strTDFXHeaderContentRet = "";
            if (String.IsNullOrEmpty(strTDFFilePath))
            {
                OutputLog("The pass in TDF file path:[{0}] is empty\n", strTDFFilePath);
            }
            else
            {
                Encoding obEncoding = CommonHelper.GetEncoding(strTDFFilePath, System.Text.Encoding.UTF8);
                using (FileStream obFileStream = new FileStream(strTDFFilePath, FileMode.Open))
                {
                    strTDFXHeaderContentRet = GetTDFFileContentFronFileStream(obFileStream, nMaxStartEffectiveXHeaderLength, nMaxEndEffectiveXHeaderLength, obEncoding, strTDFFilePath);
                }
            }
            return strTDFXHeaderContentRet;
        }

        static private string GetTDFFileContentFronFileStream(FileStream obFileStream, int nMaxStartEffectiveXHeaderLength, int nMaxEndEffectiveXHeaderLength, Encoding obEncoding, string strFileName)
        {
            if (null == obFileStream)
            {
                return "";
            }

            string strTDFXHeaderContentRet = "";
            int nFileSize = (int)obFileStream.Length;
            if ((nMaxStartEffectiveXHeaderLength < 0) && (nMaxEndEffectiveXHeaderLength < 0))
            {
                strTDFXHeaderContentRet = MyReadFile(obFileStream, obEncoding, 0, nFileSize);
            }
            else
            {
                // +4 , BOM length
                if (nFileSize > (nMaxStartEffectiveXHeaderLength + nMaxEndEffectiveXHeaderLength + 4))
                {
                    bool bSuccess = true;
                    if (0 < nMaxStartEffectiveXHeaderLength)
                    {
                        // Read start block
                        obFileStream.Seek(0, SeekOrigin.Begin);
                        string strStartBlock = MyReadFile(obFileStream, obEncoding, 0, nMaxStartEffectiveXHeaderLength);
                        if ((!String.IsNullOrEmpty(strStartBlock)) && (strStartBlock.Length == nMaxStartEffectiveXHeaderLength))
                        {
                            bSuccess = true;
                            strTDFXHeaderContentRet += strStartBlock;
                        }
                        else
                        {
                            bSuccess = false;
                            OutputLog("Read start TDF content from file:[{0}] failed, the read count is not right, please check\n", strFileName);
                        }
                    }
                    else
                    {
                        bSuccess = true;
                        OutputLog("Read start TDF content from file:[{0}] ignore, the specify start max lenght is:[{1}] less than zero\n", strFileName, nMaxStartEffectiveXHeaderLength);
                    }

                    if (bSuccess)
                    {
                        if (0 < nMaxEndEffectiveXHeaderLength)
                        {
                            // Read end block
                            obFileStream.Seek(0 - nMaxEndEffectiveXHeaderLength, SeekOrigin.End);
                            string strEndBlock = MyReadFile(obFileStream, obEncoding, 0, nMaxEndEffectiveXHeaderLength);
                            if ((!String.IsNullOrEmpty(strEndBlock)) && (strEndBlock.Length == nMaxEndEffectiveXHeaderLength))
                            {
                                bSuccess = true;
                                strTDFXHeaderContentRet += strEndBlock;
                            }
                            else
                            {
                                bSuccess = false;
                                strTDFXHeaderContentRet = "";
                                OutputLog("Read end TDF content from file:[{0}] failed, the read count is not right, please check\n", strFileName);
                            }
                        }
                        else
                        {
                            OutputLog("Read end TDF content from file:[{0}] ignore, the specify end max lenght is:[{1}] less than zero\n", strFileName, nMaxEndEffectiveXHeaderLength);
                        }
                    }
                    else
                    {
                        strTDFXHeaderContentRet = "";
                    }
                }
                else
                {
                    strTDFXHeaderContentRet = MyReadFile(obFileStream, obEncoding, 0, nFileSize);
                }
            }

            return strTDFXHeaderContentRet;
        }
        static private string GetTDFFileContentFromStreamReader(StreamReader obFileReader, int nMaxStartEffectiveXHeaderLength, int nMaxEndEffectiveXHeaderLength, string strFileName)
        {
            if (null == obFileReader)
            {
                return "";
            }

            string strTDFXHeaderContentRet = "";
            if ((nMaxStartEffectiveXHeaderLength < 0) && (nMaxEndEffectiveXHeaderLength < 0))
            {
                strTDFXHeaderContentRet = obFileReader.ReadToEnd();
            }
            else
            {
                // +4 , BOM length
                long nFileSize = obFileReader.BaseStream.Length;
                if (nFileSize > (nMaxStartEffectiveXHeaderLength + nMaxEndEffectiveXHeaderLength + 4))
                {
                    bool bSuccess = true;
                    StringBuilder obStringBuilder = new StringBuilder();
                    if (0 < nMaxStartEffectiveXHeaderLength)
                    {
                        // Read start block
                        obFileReader.BaseStream.Seek(0, SeekOrigin.Begin);
                        char[] szStartBlock = new char[nMaxStartEffectiveXHeaderLength];
                        int nRealReadCount = obFileReader.ReadBlock(szStartBlock, 0, nMaxStartEffectiveXHeaderLength);
                        if (nRealReadCount == nMaxStartEffectiveXHeaderLength)
                        {
                            bSuccess = true;
                            obStringBuilder.Append(szStartBlock, 0, nRealReadCount);
                        }
                        else
                        {
                            bSuccess = false;
                            OutputLog("Read start TDF content from file:[{0}] failed, the read count is not right, please check\n", strFileName);
                        }
                    }
                    else
                    {
                        bSuccess = true;
                        OutputLog("Read start TDF content from file:[{0}] ignore, the specify start max lenght is:[{1}] less than zero\n", strFileName, nMaxStartEffectiveXHeaderLength);
                    }

                    if (bSuccess)
                    {
                        if (0 < nMaxEndEffectiveXHeaderLength)
                        {
                            // Read end block
                            obFileReader.BaseStream.Seek(0 - nMaxEndEffectiveXHeaderLength, SeekOrigin.End);
                            char[] szEndBlock = new char[nMaxEndEffectiveXHeaderLength];
                            int nRealReadCount = obFileReader.ReadBlock(szEndBlock, 0, nMaxEndEffectiveXHeaderLength);
                            if (nRealReadCount == nMaxEndEffectiveXHeaderLength)
                            {
                                bSuccess = true;
                                obStringBuilder.Append(szEndBlock, 0, nRealReadCount);
                            }
                            else
                            {
                                bSuccess = false;
                                strTDFXHeaderContentRet = "";
                                OutputLog("Read end TDF content from file:[{0}] failed, the read count is not right, please check\n", strFileName);
                            }
                        }
                        else
                        {
                            OutputLog("Read end TDF content from file:[{0}] ignore, the specify end max lenght is:[{1}] less than zero\n", strFileName, nMaxEndEffectiveXHeaderLength);
                        }
                    }
                    else
                    {
                        strTDFXHeaderContentRet = "";
                    }

                    if (bSuccess)
                    {
                        strTDFXHeaderContentRet = obStringBuilder.ToString();
                    }
                }
                else
                {
                    strTDFXHeaderContentRet = obFileReader.ReadToEnd();
                }
            }
            return strTDFXHeaderContentRet;
        }

        static private string RemoveXMLComments(string strXmlData)
        {
            MatchCollection obMatchCollection = MatchXmlComments(strXmlData);
            if (null == obMatchCollection)
            {
                // No matched
            }
            else
            {
                int nRemovedCount = 0;
                foreach (Match obMatch in obMatchCollection)
                {
                    if (obMatch.Success && 1 == obMatch.Groups.Count)
                    {
                        Group obCurMatchGroup = obMatch.Groups[0];
                        strXmlData = RemoveDataContentByRegGroup(strXmlData, obCurMatchGroup, ref nRemovedCount);
                    }
                }
            }
            return strXmlData;
        }
        static private string GetXmlRootNodeName(string strXmlData)
        {
            const string strXmlNodePatten = "<\\s?([^?!\\s<>]+?)(?:\\s|>)";

            string strRootNodeName = "";
            Regex obRegex = new Regex(strXmlNodePatten, RegexOptions.Multiline);
            Match obMatch = obRegex.Match(strXmlData);
            if (null == obMatch)
            {
                // Failed, root node name is empty
            }
            else
            {
                if (obMatch.Success && (2 == obMatch.Groups.Count))
                {
                    strRootNodeName = obMatch.Groups[1].Value;
                }
                else
                {
                    // match failed, ignore
                }
            }
            return strRootNodeName;
        }
        static private MatchCollection MatchXmlComments(string strXmlData)
        {
            const string strXmlCommentsPatten = "<!--[\\s\\S]*?-->";

            Regex obRegex = new Regex(strXmlCommentsPatten, RegexOptions.Multiline);
            return obRegex.Matches(strXmlData);
        }
        static private string MyReadFile(FileStream obFileStream, Encoding obEncoding, int nOffset, int nReadCount)
        {
            if (0 >= nReadCount)
            {
                nReadCount = (int)obFileStream.Length;
            }
            byte[] szReadByte = new byte[nReadCount];
            int r = obFileStream.Read(szReadByte, nOffset, szReadByte.Length);
            return obEncoding.GetString(szReadByte);
        }
        static private string RemoveDataContentByRegGroup(string strData, Group obCurMatchGroup, ref int nAlreadynRemovedCount)
        {
            if (null == obCurMatchGroup)
            {
                OutputLog("Exception during remove ignore value");
            }
            else
            {
                int nIndex = obCurMatchGroup.Index - nAlreadynRemovedCount;
                if (0 <= nIndex)
                {
                    int nMatchLength = obCurMatchGroup.Length;
                    strData = strData.Remove(nIndex, nMatchLength);
                    nAlreadynRemovedCount += nMatchLength;
                }
                else
                {
                    OutputLog("Exception during remove data by RegGroup\n");
                }
            }
            return strData;
        }
        static private List<Group> MatchXmlNode(string strXmlData, KeyValuePair<string, EMNode_Element> pairNodeInfo)
        {
            const string strNodePattenFormat = "<\\s?{0}((?:\\s[^<>]*?)*?)>([\\s\\S]*?)</\\s?{0}\\s?>";

            List<Group> lsGroupRet = null;

            string strCurNodePatten = String.Format(strNodePattenFormat, pairNodeInfo.Key);
            Regex obRegex = new Regex(strCurNodePatten, RegexOptions.Multiline);
            MatchCollection obMatchCollection = obRegex.Matches(strXmlData);
            if (null == obMatchCollection)
            {
                // No matched
            }
            else
            {
                foreach (Match obMatch in obMatchCollection)
                {
                    if (obMatch.Success && 3 == obMatch.Groups.Count)
                    {
                        Group obCurMatchGroup = null;
                        switch (pairNodeInfo.Value)
                        {
                            case EMNode_Element.Value:
                                {
                                    obCurMatchGroup = obMatch.Groups[2];
                                    break;
                                }
                            case EMNode_Element.Attribute:
                                {
                                    obCurMatchGroup = obMatch.Groups[1];
                                    break;
                                }
                            case EMNode_Element.WholeNode:
                                {
                                    obCurMatchGroup = obMatch.Groups[0];
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                        if (null == lsGroupRet)
                        {
                            lsGroupRet = new List<Group>();
                        }
                        lsGroupRet.Add(obCurMatchGroup);
                    }
                }
            }
            return lsGroupRet;
        }

        static private string ConvertTDFXHeaderFromXmlToJSON(string strTDFXHeader)
        {
            String strJson = null;
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(strTDFXHeader);
                strJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(xmlDoc);
            }
            catch (Exception ex)
            {
                theLog.OutputLog(LogLevel.Error, "JSONRAP: Json Conversion Error:{0}", ex.Message);
            }
            return strJson;
        }

        #region Backup code
        static private string TrimXHeaderNodes_backup(string strTDFXHeaderContent, Dictionary<string, EMNode_Element> dicIgnoreNode)
        {
            if (null != dicIgnoreNode)
            {
                const string strNodePattenFormat = "<\\s?{0}((?:\\s[^<>]*?)*?)>([\\s\\S]*?)</\\s?{0}\\s?>";
                foreach (var pairIgnoreNode in dicIgnoreNode)
                {
                    string strCurNodePatten = String.Format(strNodePattenFormat, pairIgnoreNode.Key);
                    Regex obRegex = new Regex(strCurNodePatten, RegexOptions.Multiline);
                    MatchCollection obMatchCollection = obRegex.Matches(strTDFXHeaderContent);
                    if (null == obMatchCollection)
                    {
                        // No matched
                    }
                    else
                    {
                        int nRemovedCount = 0;
                        foreach (Match obMatch in obMatchCollection)
                        {
                            if (obMatch.Success && 3 == obMatch.Groups.Count)
                            {
                                Group obCurMatchGroup = null;
                                switch (pairIgnoreNode.Value)
                                {
                                    case EMNode_Element.Value:
                                        {
                                            obCurMatchGroup = obMatch.Groups[2];
                                            break;
                                        }
                                    case EMNode_Element.Attribute:
                                        {
                                            obCurMatchGroup = obMatch.Groups[1];
                                            break;
                                        }
                                    case EMNode_Element.WholeNode:
                                        {
                                            obCurMatchGroup = obMatch.Groups[0];
                                            break;
                                        }
                                    default:
                                        {
                                            break;
                                        }
                                }
                                strTDFXHeaderContent = RemoveDataContentByRegGroup(strTDFXHeaderContent, obCurMatchGroup, ref nRemovedCount);
                            }
                            else
                            {
                                OutputLog("Match ignore node:[{0}] error\n", pairIgnoreNode.Key);
                            }
                        }
                    }
                }
            }
            return strTDFXHeaderContent;
        }
        #endregion
    }
}
