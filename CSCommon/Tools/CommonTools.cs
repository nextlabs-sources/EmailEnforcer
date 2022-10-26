using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CSBase.Diagnose;
using System.Web;
using System.Text.RegularExpressions;
using System.Reflection;

namespace CSBase.Common
{
    public static class CommonTools
    {
        #region null object helper
        // Get solid string, avoid null object. If the string(strIn) is null, it will return empty string("").
        static public string GetSolidString(string strIn)
        {
            return (null == strIn) ? "" : strIn;
        }
        // Get object string value, avoid null object
        static public string GetObjectStringValue<T>(T obT)
        {
            return (null != obT) ? obT.ToString() : "";
        }
        #endregion

        #region Convert helper
        static public T ConvertStringToEnum<T>(string strValue, bool bIgnoreCase, T emDefault)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), strValue, bIgnoreCase);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Convert:[{0}] to enum:[{1}] failed, please check.\n", new object[] { strValue, typeof(T).ToString() }, ex);
            }
            return emDefault;
        }
        static public int ConvertStringToInt(string strValue, int nDefault)
        {
            try
            {
                return Int32.Parse(strValue);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Convert:[{0}] to int failed, use default value:[{1}], please check\n", new object[] { strValue, nDefault }, ex);
            }
            return nDefault;
        }
        static public bool ConvertStringToBoolean(string strValue, bool bDefault)
        {
            try
            {
                if ((strValue.Equals("0", StringComparison.OrdinalIgnoreCase)) ||
                    (strValue.Equals("no", StringComparison.OrdinalIgnoreCase)) ||
                    (strValue.Equals("false", StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
                else if ((strValue.Equals("1", StringComparison.OrdinalIgnoreCase)) ||
                         (strValue.Equals("yes", StringComparison.OrdinalIgnoreCase)) ||
                         (strValue.Equals("true", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
                else
                {
                    return bDefault;
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Convert:[{0}] to boolean failed, use default value:[{1}], please check.\n", new object[] { strValue, bDefault }, ex);
            }
            return bDefault;
        }
        static public string[] ConvertStringToArray(string strValue, string strSep, bool bIgnoreCase, StringSplitOptions emStringSplitOption)
        {
            string[] szValueRet = null;
            if (!String.IsNullOrEmpty(strValue))
            {
                if (String.IsNullOrEmpty(strSep))
                {
                    szValueRet = new string[] { strValue };
                }
                else
                {
                    if (bIgnoreCase)
                    {
                        szValueRet = strValue.ToLower().Split(new string[] { strSep.ToLower() }, emStringSplitOption);
                    }
                    else
                    {
                        szValueRet = strValue.Split(new string[] { strSep }, emStringSplitOption);
                    }
                }
            }
            return szValueRet;
        }
        static public void ConvertArrayToCollection<TCollection, TItem>(TItem[] szValue, ref TCollection setValueRefRet) where TCollection : ICollection<TItem>
        {
            // HashSet<string>
            if (null != szValue)
            {
                if (null == setValueRefRet)
                {
                    setValueRefRet = default(TCollection);
                }
                foreach (TItem strItem in szValue)
                {
                    setValueRefRet.Add(strItem);
                }
            }
        }
        static public string CovertComapreOpFromEnumToString(EMCompareOp emLogicOp)
        {
            return CommonTools.GetValueByKeyFromDic(CommonValues.g_kdicLogicOpEnumAndString, emLogicOp, "");
        }
        static public EMCompareOp CovertComapreOpFromStringToEnum(string strLogicOp)
        {
            return CommonTools.GetValueByKeyFromDic(CommonValues.g_kdicLogicOpStringAndEnum, strLogicOp, EMCompareOp.emUnknown);
        }
        #endregion

        #region Algorothm
        static public string[] SplitStringByLength(string strIn, int nSepCharNum)
        {
            List<string> lsSnippets = new List<string>();
            if (!(string.IsNullOrEmpty(strIn)) && (0 < nSepCharNum))
            {
                int nSnippets = (strIn.Length / (nSepCharNum)) + 1;
                for (int i = 0; i < nSnippets; ++i)
                {
                    string strCurSnippet = "";
                    int nCurStartIndex = i * nSepCharNum;
                    if (i == (nSnippets - 1))
                    {
                        strCurSnippet = strIn.Substring(nCurStartIndex); // the last one
                    }
                    else
                    {
                        strCurSnippet = strIn.Substring(nCurStartIndex, nSepCharNum);
                    }

                    lsSnippets.Add(strCurSnippet);
                }
            }
            return lsSnippets.ToArray();
        }
        static public string ReplaceWildcards(string strIn, Dictionary<string, string> dicWildcards, string strWildcardStartFlag, string strWildcardEndFlag, bool bNeedEncodeWildcardValue)
        {
            if (null != dicWildcards)
            {
                {
                    // Debug
                    CSLogger.OutputLog(LogLevel.Debug, "Start output wildcards, Org:[{0}]\n", new object[] { strIn });
                    foreach (KeyValuePair<string, string> pairItem in dicWildcards)
                    {
                        CSLogger.OutputLog(LogLevel.Debug, "Wildcards=>Key:[{0}],Value:[{1}],EncodeValue:[{2}]\n", new object[] { pairItem.Key, pairItem.Value, (bNeedEncodeWildcardValue ? HttpUtility.HtmlEncode(pairItem.Value) : pairItem.Value) });
                    }
                    CSLogger.OutputLog(LogLevel.Debug, "End output wildcards.\n");
                }
                foreach (KeyValuePair<string, string> pairWildcardItem in dicWildcards)
                {
                    string strRegPattern = MakeAsStandardRegularPattern(strWildcardStartFlag + pairWildcardItem.Key + strWildcardEndFlag);
                    Regex regex = new Regex(strRegPattern);
                    if (regex.IsMatch(strIn))
                    {
                        strIn = regex.Replace(strIn, (bNeedEncodeWildcardValue ? HttpUtility.HtmlEncode(pairWildcardItem.Value) : pairWildcardItem.Value));
                    }
                }
            }
            return strIn;
        }
        static public void SubStringBuilder(ref StringBuilder strBuilder, int nSubLength)
        {
            if (null != strBuilder)
            {
                if (strBuilder.Length >= nSubLength)
                {
                    strBuilder.Length -= nSubLength;
                }
                else
                {
                    strBuilder.Length = 0;
                }
            }
        }
        static public bool ContainsOneOfChars(string strIn, params char[] szChars)
        {
            if ((!string.IsNullOrEmpty(strIn)) && ((null != szChars)))
            {
                for (int i = 0; i < szChars.Length; ++i)
                {
                    if (strIn.Contains(szChars[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        static public string MakeAsStandardRegularPattern(string strInRegularFlag)
        {
            return strInRegularFlag.Replace("\\", "\\\\");
        }
        #endregion

        #region List helper
        static public string ConvertListToString(IList<string> lsMembers, string strSeparator, bool bEndWithSeparator)
        {
            string strOut = "";
            if (null != lsMembers)
            {
                int nCount = lsMembers.Count;
                if (0 < nCount)
                {
                    strOut = lsMembers[0];
                    for (int i = 1; i < nCount; ++i)
                    {
                        strOut += strSeparator + lsMembers[i];
                    }
                    if (bEndWithSeparator)
                    {
                        strOut += strSeparator;
                    }
                }
            }
            return strOut;
        }
        static public string JoinList<T>(List<T> lsIn, string strSepJoin)
        {
            return string.Join(strSepJoin, lsIn);
        }
        static public List<string> GetStandardStringList(List<string> lsStrIn, bool bNeedTrim, bool bConverNullToEmpty, bool bRemovedNullOrWitheSpaceItem)
        {
            List<string> lsOut = null;
            if (null != lsStrIn)
            {
                lsOut = new List<string>();
                foreach (string strItem in lsStrIn)
                {
                    if (null == strItem)
                    {
                        lsOut.Add(bConverNullToEmpty ? "" : null);
                    }
                    else
                    {
                        lsOut.Add((bNeedTrim ? strItem.Trim() : strItem));
                    }
                }
                if (bRemovedNullOrWitheSpaceItem)
                {
                    lsOut.RemoveAll(new Predicate<string>(string.IsNullOrWhiteSpace));
                }
            }
            return lsOut;
        }
        static public bool ListStringContains(List<string> lstSrc, string strDest, StringComparison emStringComparison = StringComparison.OrdinalIgnoreCase)
        {
            foreach (string strSrc in lstSrc)
            {
                if (strSrc.Equals(strDest, emStringComparison))
                {
                    return true;
                }
            }
            return false;
        }
        static public void ListStringRemove(List<string> lstSrc, string strDest, StringComparison emStringComparison = StringComparison.OrdinalIgnoreCase)
        {
            foreach (string strSrc in lstSrc)
            {
                if (strSrc.Equals(strDest, emStringComparison))
                {
                    lstSrc.Remove(strSrc);
                    break;
                }
            }
        }
        #endregion

        #region Array helper
        static public T GetArrayValueByIndex<T>(T[] szTIn, int nIndex, T tDefaultValue)
        {
            if ((0 <= nIndex) && (szTIn.Length > nIndex))
            {
                return szTIn[nIndex];
            }
            return tDefaultValue;
        }
        #endregion

        #region Dictionary helper
        static public TVALUE GetValueByKeyFromDic<TKEY, TVALUE>(Dictionary<TKEY, TVALUE> dirMaps, TKEY tKey, TVALUE tDefaultValue)
        {
            if (null != dirMaps)
            {
                if (dirMaps.ContainsKey(tKey))
                {
                    return dirMaps[tKey];
                }
            }
            return tDefaultValue;
        }
        static public bool AddKeyValuesToDic<TKEY, TVALUE>(Dictionary<TKEY, TVALUE> dirMaps, TKEY tKey, TVALUE tValue, bool bFailedIfExist)
        {
            bool bRet = false;
            if (null == dirMaps)
            {
                bRet = false;
            }
            else
            {
                if (dirMaps.ContainsKey(tKey))
                {
                    if (bFailedIfExist)
                    {
                        bRet = false;
                    }
                    else
                    {
                        dirMaps[tKey] = tValue;
                        bRet = true;
                    }
                }
                else
                {
                    dirMaps.Add(tKey, tValue);
                    bRet = true;
                }
            }
            return bRet;
        }
        static public void RemoveKeyValuesFromDic<TKEY, TVALUE>(Dictionary<TKEY, TVALUE> dicMaps, TKEY tKey)
        {
            if (null != dicMaps)
            {
                if (dicMaps.ContainsKey(tKey))
                {
                    dicMaps.Remove(tKey);
                }
            }
        }
        static public string ConnectionDicKeyAndValues<TKEY, TVALUE>(Dictionary<TKEY, TVALUE> dicMaps, bool bRemoveEmptyItem, bool bEndWithKeySep, string strSepKeys, string strSepKeyAndValues)
        {
            if (null != dicMaps)
            {
                StringBuilder strOut = new StringBuilder();
                foreach (KeyValuePair<TKEY, TVALUE> pairItem in dicMaps)
                {
                    if ((!bRemoveEmptyItem) || (!string.IsNullOrEmpty(pairItem.Key.ToString()) && (!string.IsNullOrEmpty(pairItem.Value.ToString()))))
                    {
                        strOut.Append(pairItem.Key.ToString() + strSepKeyAndValues + pairItem.Value.ToString() + strSepKeys);
                    }
                }
                if (!bEndWithKeySep)
                {
                    SubStringBuilder(ref strOut, strSepKeys.Length);
                }
                return strOut.ToString();
            }
            return null;
        }
        static public Dictionary<string, TVALUE> DistinctDictionaryIgnoreKeyCase<TVALUE>(Dictionary<string, TVALUE> dicMaps)
        {
            Dictionary<string, TVALUE> dicCheckedMaps = new Dictionary<string, TVALUE>();
            foreach (KeyValuePair<string, TVALUE> pairItem in dicMaps)
            {
                AddKeyValuesToDic(dicCheckedMaps, pairItem.Key.ToLower(), pairItem.Value, false);
            }
            return dicCheckedMaps;
        }
        #endregion

        #region Others
        static public string GetTabsStrByTabNum(int nTabNumbers)
        {
            string strTabs = "";
            for (int i = 0; i < nTabNumbers; ++i)
            {
                strTabs += "\t";
            }
            return strTabs;
        }
        #endregion
    }
}
