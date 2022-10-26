using CSBase.Diagnose;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSBase.Common
{
    public class DomainHelper : IDisposable
    {
        public delegate int /*EMlogicControl*/ ProcessWithSpeicfyDomain<TyRetOut>(Domain obDoaminIn, ref TyRetOut tyRetOut);

        #region Const/Readonly values
        public const string g_kstrADKey_ObjectClass = "objectClass";
        public const string g_kstrADValue_ObjectClassUser = "user";
        public const string g_kstrADValue_ObjectClassGroup = "group";

        public const string g_kstrADKey_Mail = "mail";
        public const string g_kstrADKey_UserPrincipalName = "userprincipalname";
        public const string g_kstrADKey_ObjectSID = "objectsid";
        #endregion

        #region Static methods
        #region Public domain tools
        public static void LoopForestDomainProperties(Forest obDomainForest, string strObjectKeyIn, string strObjectValueIn, string strPropertyKey, string strPropertyValue)
        {
            try
            {
                if (null == obDomainForest)
                {
                    CSLogger.OutputLog(LogLevel.Debug, "Try to loop forest with key:[{0}], value:[{1}], but the forest object is null [{2}]\n", new object[] { strPropertyKey, strPropertyValue, obDomainForest });
                }
                else
                {
                    DomainCollection obDomainCollection = obDomainForest.Domains;
                    if (null == obDomainCollection)
                    {
                        CSLogger.OutputLog(LogLevel.Debug, "Try to loop forest:[0] with key:[{1}], value:[{2}], but the domain collection object is null\n", new object[] { obDomainForest.Name, strPropertyKey, strPropertyValue });
                    }
                    else
                    {
                        CSLogger.OutputLog(LogLevel.Debug, "Begin Loop forest:[0] with key:[{1}], value:[{2}], inner domain count:[{3}]\n", new object[] { obDomainForest.Name, strPropertyKey, strPropertyValue, obDomainCollection.Count });
                        foreach (Domain obDomainItem in obDomainCollection)
                        {
                            using (obDomainItem)
                            {
                                try
                                {
                                    using (DomainHelper obCommonADHelper = new DomainHelper(obDomainItem))
                                    {
                                        obCommonADHelper.LoopDomainProperties(strObjectKeyIn, strObjectValueIn, strPropertyKey, strPropertyValue);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "Exception durring loop domain:[{0}] by key:[{1}] value:[{2}]\n", new object[] { obDomainItem.Name, strPropertyKey, strPropertyValue }, ex);
                                }
                            }
                        }
                        CSLogger.OutputLog(LogLevel.Debug, "End Loop forest:[0] with key:[{1}], value:[{2}], inner domain count:[{3}]\n", new object[] { obDomainForest.Name, strPropertyKey, strPropertyValue, obDomainCollection.Count });
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during get user SID from forest by key:[{0}] value:[{1}]", new object[] { strPropertyKey, strPropertyValue }, ex);
            }
        }
        #endregion
        #region Public domain tools: user related
        public static string GetUserSecurityIDByEmailAddress(string strUserEmailAddress, bool bAutoCheckWithLogonName, string strDefaultSID, bool bDeepSearchForest, bool bIncludeGroup)
        {
            // Both login name and email address can be used to send email
            // User login name and user email address can be different. BUG 37482
            // Both AD and Exchange ecp can specify a different email address
            // New email address is called email alias
            // The alias must be unique with others login name and alias
            // If the alias conflict, email address will be auto add an index number, but alias do not changed
            string strSIDRet = strDefaultSID;
            try
            {
                strSIDRet = StandardCurrentDomainSearchInvoker(
                    bDeepSearchForest,
                    true,
                    delegate(Domain obDomainItem, ref string strProcessResult)
                    {
                        using (DomainHelper obCurDomainHelper = new DomainHelper(obDomainItem))
                        {
                            strProcessResult = obCurDomainHelper.GetUserSecurityIDByEmailAddress(strUserEmailAddress, bAutoCheckWithLogonName, "", bIncludeGroup);
                            if (String.IsNullOrEmpty(strProcessResult))
                            {
                                return (int)(EMLogicControl.emLogicFailed | EMLogicControl.emLogicContine);
                            }
                            else
                            {
                                return (int)(EMLogicControl.emLogicSuccess | EMLogicControl.emLogicBreak);
                            }
                        }                        
                    },
                    strDefaultSID);
                CSLogger.OutputLog(LogLevel.Debug, "Get user:[{0}] SID with result:[{1}], AutoCheckLogonNameFlag:[{2}] default SID:[{3}]", new object[] { strUserEmailAddress, strSIDRet, bAutoCheckWithLogonName, strDefaultSID });
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during get user SID by email address:[{0}], auto check with logon name flag:[{1}], defaultSID:[{2}]", new object[] { strUserEmailAddress, bAutoCheckWithLogonName, strDefaultSID }, ex);
            }

            if (String.IsNullOrEmpty(strSIDRet))
            {
                strSIDRet = strDefaultSID;
            }
            return strSIDRet;
        }
        public static string GetStandardEmailAddressFromAD(string strUserEmailAddressOrg, bool bFailedReturnOriginalAddr, bool bDeepSearchForest, bool bIncludeGroup)
        {
            // Both login name and email address can be used to send email
            // User login name and user email address can be different. BUG 37482
            // Both AD and Exchange ecp can specify a different email address
            // New email address is called email alias
            // The alias must be unique with others login name and alias
            // If the alias conflict, email address will be auto add an index number, but alias do not changed
            string strStandardEamilOut = "";
            try
            {
                strStandardEamilOut = StandardCurrentDomainSearchInvoker(
                    bDeepSearchForest,
                    true,
                    delegate(Domain obDomainItem, ref string strProcessResult)
                    {
                        using (DomainHelper obCurDomainHelper = new DomainHelper(obDomainItem))
                        {
                            strProcessResult = obCurDomainHelper.GetStandardEmailAddressFromAD(strUserEmailAddressOrg, false, bIncludeGroup);
                            if (String.IsNullOrEmpty(strProcessResult))
                            {
                                return (int)(EMLogicControl.emLogicFailed | EMLogicControl.emLogicContine);
                            }
                            else
                            {
                                return (int)(EMLogicControl.emLogicSuccess | EMLogicControl.emLogicBreak);
                            }
                        }
                    },
                    bFailedReturnOriginalAddr ? strUserEmailAddressOrg : "");
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during get user standard eamil address by email address:[{0}] from domain:[{1}], bFailedReturnOriginalAddr:[{2}], bDeepSearchForest:[{3}]", new object[] { strUserEmailAddressOrg, strStandardEamilOut, bFailedReturnOriginalAddr, bDeepSearchForest }, ex);
            }

            if (String.IsNullOrEmpty(strStandardEamilOut) && bFailedReturnOriginalAddr)
            {
                strStandardEamilOut = strUserEmailAddressOrg;
            }
            CSLogger.OutputLog(LogLevel.Debug, "Get user:[{0}] standard eamil address with result:[{1}], bFailedReturnOriginalAddr:[{2}], bDeepSearchForest:[{3}]", new object[] { strUserEmailAddressOrg, strStandardEamilOut, bFailedReturnOriginalAddr, bDeepSearchForest });
            return strStandardEamilOut;
        }
        public static bool GetUserBaseInfoFromAD(string strUserEmailAddress, bool bAutoCheckWithLogonName, ref string strStandardEmailAddrRef, ref string strUserPrincipalNameRef, ref string strSIDRef, bool bDeepSearchForest, bool bIncludeGroup)
        {
            bool bRet = false;
            try
            {
                Dictionary<string, string> dicUserBaseInfo = StandardCurrentDomainSearchInvoker(
                    bDeepSearchForest,
                    true,
                    delegate(Domain obDomainItem, ref Dictionary<string, string> dicUserBaseInfoRef)
                    {
                        string strStandardEmailAddr = "";
                        string strUserPrincipalName = "";
                        string strSID = "";

                        using (DomainHelper obCurDomainHelper = new DomainHelper(obDomainItem))
                        {
                            bool bSuccessFind = obCurDomainHelper.GetUserBaseInfoFromAD(strUserEmailAddress, bAutoCheckWithLogonName, bIncludeGroup, ref strStandardEmailAddr, ref strUserPrincipalName, ref strSID);
                            if (bSuccessFind)
                            {
                                if (null == dicUserBaseInfoRef)
                                {
                                    dicUserBaseInfoRef = new Dictionary<string, string>();
                                }

                                CommonTools.AddKeyValuesToDic(dicUserBaseInfoRef, g_kstrADKey_Mail, strStandardEmailAddr, false);
                                CommonTools.AddKeyValuesToDic(dicUserBaseInfoRef, g_kstrADKey_UserPrincipalName, strUserPrincipalName, false);
                                CommonTools.AddKeyValuesToDic(dicUserBaseInfoRef, g_kstrADKey_ObjectSID, strSID, false);

                                return (int)(EMLogicControl.emLogicSuccess | EMLogicControl.emLogicBreak);
                            }
                            else
                            {
                                return (int)(EMLogicControl.emLogicFailed | EMLogicControl.emLogicContine);
                            }
                        }
                    },
                    null);

                if ((null != dicUserBaseInfo) && (3 == dicUserBaseInfo.Count))
                {
                    strStandardEmailAddrRef = CommonTools.GetValueByKeyFromDic(dicUserBaseInfo, g_kstrADKey_Mail, strStandardEmailAddrRef);
                    strUserPrincipalNameRef = CommonTools.GetValueByKeyFromDic(dicUserBaseInfo, g_kstrADKey_UserPrincipalName, strUserPrincipalNameRef);
                    strSIDRef = CommonTools.GetValueByKeyFromDic(dicUserBaseInfo, g_kstrADKey_ObjectSID, strSIDRef);

                    bRet = true;
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during Get user:[{0}] base info, bDeepSearchForest:[{1}]", new object[] { strUserEmailAddress, bDeepSearchForest }, ex);
            }

            CSLogger.OutputLog(LogLevel.Debug, "Get user:[{0}] base info with result:[{1}], standard eamil:[{2}], UPN:[{3}], SID:[{4}], bDeepSearchForest:[{5}]", new object[] { strUserEmailAddress, bRet, strStandardEmailAddrRef, strUserPrincipalNameRef, strSIDRef, bDeepSearchForest });
            return bRet;
        }
        #endregion

        #region Public independence tools
        public static string GetStringSIDFromByteArray(byte[] szByteSid, string strDefaultSID)
        {
            string strSIDRet = strDefaultSID;
            try
            {
                if (null == szByteSid)
                {
                    CSLogger.OutputLog(LogLevel.Warn, "Failed to get user string SID, the SID item object:[{0}] is null", new object[] { szByteSid });
                }
                else
                {
                    System.Security.Principal.SecurityIdentifier obSecurityIdentifier = new System.Security.Principal.SecurityIdentifier(szByteSid, 0);
                    strSIDRet = obSecurityIdentifier.ToString();
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Warn, "Exception during get user string SID from byte array:[{0}]", new object[] { szByteSid }, ex);
            }
            return strSIDRet;
        }
        #endregion

        #region Inner tools
        private static TyRetOut StandardCurrentDomainSearchInvoker<TyRetOut>(bool bNeedDoDeepSearch, bool bContinueLoopIfException, ProcessWithSpeicfyDomain<TyRetOut> pFuncProcessWithSpeicfyDomain, TyRetOut tyDefaultRet)
        {
            TyRetOut tyRetOut = tyDefaultRet;
            try
            {
                Domain obCurDomain = Domain.GetCurrentDomain();
                tyRetOut = StandardDomainSearchInvoker<TyRetOut>(obCurDomain, bNeedDoDeepSearch, bContinueLoopIfException, pFuncProcessWithSpeicfyDomain, tyDefaultRet);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during standard search from current domain:[{0}], bNeedDoDeepSearch:[{1}], bContinueLoopIfException:[{2}], pFuncProcessWithSpeicfyDomain:[{3}], tyDefaultRet:[{4}]", new object[] { Domain.GetCurrentDomain(), bNeedDoDeepSearch, bContinueLoopIfException, pFuncProcessWithSpeicfyDomain, tyDefaultRet }, ex);
            }
            return tyRetOut;
        }
        private static TyRetOut StandardDomainSearchInvoker<TyRetOut>(Domain obCurDomainIn, bool bNeedDoDeepSearch, bool bContinueLoopIfException, ProcessWithSpeicfyDomain<TyRetOut> pFuncProcessWithSpeicfyDomain, TyRetOut tyDefaultRet)
        {
            TyRetOut tyRetOut = tyDefaultRet;
            try
            {
                if ((null == obCurDomainIn) || (null == pFuncProcessWithSpeicfyDomain))
                {
                    CSLogger.OutputLog(LogLevel.Info, "Try to loop the forest but the pass in forest object or the process delegate functions is null\n");
                }
                else
                {
                    int nLogicControl = pFuncProcessWithSpeicfyDomain(obCurDomainIn, ref tyRetOut);
                    if (0 == (nLogicControl & ((int)(EMLogicControl.emLogicContine))))
                    {
                        // exit
                    }
                    else
                    {
                        if (bNeedDoDeepSearch)
                        {
                            tyRetOut = StandardForestLoopInvoker(
                                obCurDomainIn.Forest,
                                bContinueLoopIfException,
                                delegate(Domain obDomainItem, ref TyRetOut tyProcessResult)
                                {
                                    if (String.Equals(obDomainItem.Name, obCurDomainIn.Name))
                                    {
                                        nLogicControl = (int)(EMLogicControl.emLogicSuccess | EMLogicControl.emLogicContine);
                                    }
                                    else
                                    {
                                        nLogicControl = pFuncProcessWithSpeicfyDomain(obDomainItem, ref tyProcessResult);
                                    }
                                    return nLogicControl;
                                },
                                tyDefaultRet
                                );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during standard search from domain:[{0}], bNeedDoDeepSearch:[{1}] bContinueLoopIfException:[{2}], pFuncProcessWithSpeicfyDomain:[{3}], tyDefaultRet:[{4}]", new object[] { obCurDomainIn, bNeedDoDeepSearch, bContinueLoopIfException, pFuncProcessWithSpeicfyDomain, tyDefaultRet }, ex);
            }
            return tyRetOut;
        }
        private static TyRetOut StandardForestLoopInvoker<TyRetOut>(Forest obForest, bool bContinueLoopIfException, ProcessWithSpeicfyDomain<TyRetOut> pFuncProcessWithSpeicfyDomain, TyRetOut tyDefaultRet)
        {
            TyRetOut tyRetOut = tyDefaultRet;
            try
            {
                if ((null == obForest) || (null == pFuncProcessWithSpeicfyDomain))
                {
                    CSLogger.OutputLog(LogLevel.Info, "Try to loop the forest but the pass in forest object or the process delegate functions is null\n");
                }
                else
                {
                    DomainCollection obDomainCollection = obForest.Domains;
                    if (null == obDomainCollection)
                    {
                        CSLogger.OutputLog(LogLevel.Info, "Try to loop the forest:[{0}] but the the domain collection object is null\n", new object[] { obForest.Name });
                    }
                    else
                    {
                        bool bNeedBreak = (!bContinueLoopIfException);
                        int nLogicControl = (int)(EMLogicControl.emLogicSuccess | EMLogicControl.emLogicContine);
                        foreach (Domain obDomainItem in obDomainCollection)
                        {
                            using (obDomainItem)
                            {
                                try
                                {
                                    nLogicControl = pFuncProcessWithSpeicfyDomain(obDomainItem, ref tyRetOut);
                                    bNeedBreak = (0 == (nLogicControl & ((int)(EMLogicControl.emLogicContine))));
                                }
                                catch (Exception ex)
                                {
                                    bNeedBreak = (!bContinueLoopIfException);
                                    CSLogger.OutputLog(LogLevel.Debug, "Exception during process with domain:[{0}], bContinueLoopIfException:[{1}]\n", new object[] { obDomainItem.Name, bContinueLoopIfException }, ex);
                                }
                            }

                            if (bNeedBreak)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during standard loop from forest:[{0}], bContinueLoopIfException:[{1}], pFuncProcessWithSpeicfyDomain:[{2}], tyDefaultRet:[{3}]", new object[] { obForest, bContinueLoopIfException, pFuncProcessWithSpeicfyDomain, tyDefaultRet }, ex);
            }
            return tyRetOut;
        }
        #endregion
        #endregion

        public DomainHelper(Domain obDomainIn)
        {
            if (null == obDomainIn)
            {
                obDomainIn = Domain.GetCurrentDomain();
            }
            m_obDirectoryEntry = GetDirectoryEntry(obDomainIn);

            if (null == m_obDirectoryEntry)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Current domain entry is empty, maybe the domain:[{0}] is broken or some setting error\n", new object[] { m_strDomainName });
                throw new Exception(String.Format("Cannot get the domain entry:[{0}], maybe the domain is broken", m_strDomainName));
            }
            else
            {
                m_strDomainName = m_obDirectoryEntry.Name;
            }
        }

        #region Interface implement : IDisposable
        public void Dispose()
        {
            try
            {
                if (null != m_obDirectoryEntry)
                {
                    m_obDirectoryEntry.Dispose();
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during dispose domain helper object", null, ex);
            }
        }
        #endregion

        #region Public methods
        public void LoopDomainProperties(string strObjectKeyIn, string strObjectValueIn, string strPropertyKey, string strPropertyValue)
        {
            try
            {
                using (DomainSearchManager obDomainSearchManager = new DomainSearchManager(m_obDirectoryEntry, null, true, EMCompareOp.emEqualSingle, strObjectKeyIn, strObjectValueIn, strPropertyKey, strPropertyValue))
                {
                    SearchResultCollection obSearchResultCollection = obDomainSearchManager.GetSearchResultCollection();
                    if (null == obSearchResultCollection)
                    {
                        CSLogger.OutputLog(LogLevel.Error, "Failed to get user AD properties by key:[{0}] value:[{1}], cannot find any item", new object[] { strPropertyKey, strPropertyValue });
                    }
                    else
                    {
                        CSLogger.OutputLog(LogLevel.Debug, "Begin loop domain:[{0}], count:[{1}]: \n", new object[] { m_strDomainName, obSearchResultCollection.Count });
                        foreach (SearchResult obSearchResult in obSearchResultCollection)
                        {
                            if ((null == obSearchResult) || (null == obSearchResult.Properties))
                            {
                                CSLogger.OutputLog(LogLevel.Warn, "Failed to get AD properties SID by key:[{0}] value:[{1}], cannot find this item", new object[] { strPropertyKey, strPropertyValue });
                            }
                            else
                            {
                                ResultPropertyCollection obResultPropertyCollection = obSearchResult.Properties;
                                CSLogger.OutputLog(LogLevel.Debug, "Begin output item info: \n");
                                foreach (string strNames in obResultPropertyCollection.PropertyNames)
                                {
                                    ResultPropertyValueCollection obResultPropertyValueCollection = obSearchResult.Properties[strNames];
                                    foreach (object obItemValue in obResultPropertyValueCollection)
                                    {
                                        if (strNames.Equals(g_kstrADKey_ObjectSID, StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (obItemValue is byte[])
                                            {
                                                byte[] byItemValue = obItemValue as byte[];
                                                string strSID = GetStringSIDFromByteArray(byItemValue, "");
                                                CSLogger.OutputLog(LogLevel.Debug, "\tName:[{0}], value:[{1}]", new object[] { strNames, strSID });
                                            }
                                            else
                                            {
                                                CSLogger.OutputLog(LogLevel.Debug, "\tName:[{0}], value:[{1}]", new object[] { strNames, obItemValue.ToString() });
                                            }
                                        }
                                        else
                                        {
                                            CSLogger.OutputLog(LogLevel.Debug, "\tName:[{0}], value:[{1}]", new object[] { strNames, obItemValue.ToString() });
                                        }
                                    }
                                }
                                CSLogger.OutputLog(LogLevel.Debug, "End output item info \n");
                            }
                        }
                        CSLogger.OutputLog(LogLevel.Debug, "End loop domain:[{0}] \n", new object[] { m_strDomainName });
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during get user SID by key:[{0}] value:[{1}]", new object[] { strPropertyKey, strPropertyValue }, ex);
            }
        }
        #endregion

        #region Public methods: user related
        public string GetUserSecurityIDByEmailAddress(string strUserEmailAddress, bool bAutoCheckWithLogonName, string strDefaultSID, bool bIncludeGroup)
        {
            // Both login name and email address can be used to send email
            // User login name and user email address can be different. BUG 37482
            // Both AD and Exchange ecp can specify a different email address
            // New email address is called email alias
            // The alias must be unique with others login name and alias
            // If the alias conflict, email address will be auto add an index number, but alias do not changed
            string strSIDRet = strDefaultSID;
            try
            {
                string strStandardEmailAddr = "";
                string strUserPrincipalName = "";
                string strSID = "";

                bool bSuccessFind = GetUserBaseInfoFromAD(strUserEmailAddress, bAutoCheckWithLogonName, bIncludeGroup, ref strStandardEmailAddr, ref strUserPrincipalName, ref strSID);
                if (bSuccessFind)
                {
                    strSIDRet = strSID;
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during get user SID by email address:[{0}], auto check with logon name flag:[{1}], defaultSID:[{2}]", new object[] { strUserEmailAddress, bAutoCheckWithLogonName, strDefaultSID }, ex);
            }
            CSLogger.OutputLog(LogLevel.Debug, "Get user:[{0}] SID with result:[{1}], AutoCheckLogonNameFlag:[{2}] default SID:[{3}]", new object[] { strUserEmailAddress, strSIDRet, bAutoCheckWithLogonName, strDefaultSID });
            return strSIDRet;
        }
        public string GetStandardEmailAddressFromAD(string strUserEmailAddressOrg, bool bFailedReturnOriginalAddr, bool bIncludeGroup)
        {
            string strStandardUserEmailAddressOut = bFailedReturnOriginalAddr ? strUserEmailAddressOrg : "";
            try
            {
                string strStandardEmailAddr = "";
                string strUserPrincipalName = "";
                string strSID = "";
                
                bool bSuccessFind = GetUserBaseInfoFromAD(strUserEmailAddressOrg, true, bIncludeGroup, ref strStandardEmailAddr, ref strUserPrincipalName, ref strSID);
                if (bSuccessFind)
                {
                    strStandardUserEmailAddressOut = strStandardEmailAddr;
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during get standard email address from AD, user email adderss:[{0}], standard email address:[{1}], FailedReturnOriginalAddr:[{2}]", new object[] { strUserEmailAddressOrg, strStandardUserEmailAddressOut, bFailedReturnOriginalAddr }, ex);
            }
            CSLogger.OutputLog(LogLevel.Debug, "Passin user email adderss:[{0}], standard email address:[{1}], FailedReturnOriginalAddr:[{2}]", new object[] { strUserEmailAddressOrg, strStandardUserEmailAddressOut, bFailedReturnOriginalAddr });
            return strStandardUserEmailAddressOut;
        }

        public bool GetUserBaseInfoFromAD(string strUserEmailAddress, bool bAutoCheckWithLogonName, bool bIncludeGroup, ref string strStandardEmailAddrRef, ref string strUserPrincipalNameRef, ref string strSIDRef)
        {
            bool bRet = false;
            try
            {
                STUCondition<string, string> stuSearchConditons = EstablsihUserEmailADSearchConditions(strUserEmailAddress, bAutoCheckWithLogonName, bIncludeGroup);
                if (null == stuSearchConditons)
                {
                    CSLogger.OutputLog(LogLevel.Error, "Code logic error, try to get user base info but the check conditions is invalid. Info: email:[{0}], bAutoCheckWithLogonName:[{1}], bIncludeGroup:[{2}]", new object[] { strUserEmailAddress, bAutoCheckWithLogonName, bIncludeGroup });
                }
                else
                {
                    List<string> lsSpecifyAttrNames = new List<string>() { g_kstrADKey_ObjectSID, g_kstrADKey_Mail, g_kstrADKey_UserPrincipalName };
                    using (DomainSearchManager obDomainSearchManager = new DomainSearchManager(m_obDirectoryEntry, lsSpecifyAttrNames, stuSearchConditons))
                    {
                        SearchResult obSearchResult = obDomainSearchManager.GetFirstSearchResult();
                        if (null == obSearchResult)
                        {
                            CSLogger.OutputLog(LogLevel.Error, "Cannot get user base info from AD by email:[{0}] bAutoCheckWithLogonName:[{1}]", new object[] { strUserEmailAddress, bAutoCheckWithLogonName });
                        }
                        else
                        {
                            strStandardEmailAddrRef = obDomainSearchManager.GetAttributeValueByNameFromSearchResult(obSearchResult, g_kstrADKey_Mail, strStandardEmailAddrRef);
                            strUserPrincipalNameRef = obDomainSearchManager.GetAttributeValueByNameFromSearchResult(obSearchResult, g_kstrADKey_UserPrincipalName, strUserPrincipalNameRef);

                            byte[] szByteSID = obDomainSearchManager.GetAttributeValueByNameFromSearchResult<byte[]>(obSearchResult, g_kstrADKey_ObjectSID, null);
                            strSIDRef = GetStringSIDFromByteArray(szByteSID, strSIDRef);

                            bRet = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during get user base info from AD by email:[{0}] bAutoCheckWithLogonName:[{1}]", new object[] { strUserEmailAddress, bAutoCheckWithLogonName }, ex);
            }
            return bRet;
        }

        public bool IsTheUserADAttributeExist(bool bIncludeGroup, bool bUserConditionLogicAnd, EMCompareOp emUserConditionCmpOp, params string[] szConditionKeyValues)
        {
            bool bRet = false;
            try
            {
                STUCondition<string, string> stuSearchConditons = EstablsihUserEmailADSearchConditionsEx(bIncludeGroup, bUserConditionLogicAnd, emUserConditionCmpOp, szConditionKeyValues);
                if (null == stuSearchConditons)
                {
                    CSLogger.OutputLog(LogLevel.Error, "Code logic error, try to check user attributes but the check conditions is invalid. Info: bIncludeGroup:[{0}], bUserConditionLogicAnd:[{1}], emUserConditionCmpOp:[{2}], Conditions:[{3}]", new object[] { bIncludeGroup, bUserConditionLogicAnd, emUserConditionCmpOp, (null == szConditionKeyValues) ? "" : String.Join(",", szConditionKeyValues) });
                }
                else
                {
                    List<string> lsSpecifyAttrNames = new List<string>() { g_kstrADKey_ObjectSID };
                    using (DomainSearchManager obDomainSearchManager = new DomainSearchManager(m_obDirectoryEntry, lsSpecifyAttrNames, stuSearchConditons))
                    {
                        SearchResult obSearchResult = obDomainSearchManager.GetFirstSearchResult();
                        bRet = (null != obSearchResult);
                    }
                }               
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during check user AD attribute bIncludeGroup:[{0}], bUserConditionLogicAnd:[{1}], emUserConditionCmpOp:[{2}], Conditions:[{3}]", new object[] { bIncludeGroup, bUserConditionLogicAnd, emUserConditionCmpOp, (null == szConditionKeyValues) ? "" : String.Join(",", szConditionKeyValues) }, ex);
            }
            return bRet;
        }

        public string GetUserSecurityIDByUniqueKey(string strPropertyKey, string strPropertyValue, string strDefaultSid, bool bIncludeGroup)
        {
            string strSIDRet = strDefaultSid;
            try
            {
                List<string> lsSpecifyAttrNames = new List<string>() { g_kstrADKey_ObjectSID };
                using (DomainSearchManager obDomainSearchManager = new DomainSearchManager(m_obDirectoryEntry, lsSpecifyAttrNames, true, EMCompareOp.emEqualSingle, g_kstrADKey_ObjectClass, g_kstrADValue_ObjectClassUser, strPropertyKey, strPropertyValue))
                {
                    byte[] szByteSid = obDomainSearchManager.GetFirstAttributeSearchValue<byte[]>(null);
                    if (null == szByteSid)
                    {
                        CSLogger.OutputLog(LogLevel.Error, "Failed to get user SID attribute by key:[{0}] value:[{1}]", new object[] { strPropertyKey, strPropertyValue });
                    }
                    else
                    {
                        strSIDRet = GetStringSIDFromByteArray(szByteSid, strDefaultSid);
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during get user SID by key:[{0}] value:[{1}]", new object[] { strPropertyKey, strPropertyValue }, ex);
            }
            return strSIDRet;
        }
        #endregion

        #region Innre tools
        public STUCondition<string, string> EstablsihUserEmailADSearchConditions(string strUserEmailAddress, bool bAutoCheckWithLogonName, bool bIncludeGroup)
        {
            if (bAutoCheckWithLogonName)
            {
                return EstablsihUserEmailADSearchConditionsEx(bIncludeGroup, false, EMCompareOp.emEqualSingle, g_kstrADKey_Mail, strUserEmailAddress, g_kstrADKey_UserPrincipalName, strUserEmailAddress);
            }
            else
            {
                return EstablsihUserEmailADSearchConditionsEx(bIncludeGroup, false, EMCompareOp.emEqualSingle, g_kstrADKey_Mail, strUserEmailAddress);
            }
        }

        public STUCondition<string, string> EstablsihUserEmailADSearchConditionsEx(bool bIncludeGroup, bool bUserConditionLogicAnd, EMCompareOp emUserConditionCmpOp, params string[] szConditionKeyValues)
        {
            STUCondition<string, string> stuSearchConditons = null;
            try
            {
                STUConditionGroup<string, string> stuClassConditionGroup = new STUConditionGroup<string, string>(new List<STUConditionUnit<string, string>>(), EMLogicOp.emOr);
                stuClassConditionGroup.lsConditionGroup.Add(new STUConditionUnit<string, string>(g_kstrADKey_ObjectClass, g_kstrADValue_ObjectClassUser, EMCompareOp.emEqualSingle));
                if (bIncludeGroup)
                {
                    stuClassConditionGroup.lsConditionGroup.Add(new STUConditionUnit<string, string>(g_kstrADKey_ObjectClass, g_kstrADValue_ObjectClassGroup, EMCompareOp.emEqualSingle));
                }

                STUConditionGroup<string, string> stuPropertyAddrConditionGroup = DomainSearchManager.ConvertConditionFromArrayToConditionGroup(bUserConditionLogicAnd, emUserConditionCmpOp, szConditionKeyValues);

                stuSearchConditons = new STUCondition<string, string>(new List<STUConditionGroup<string, string>>(), EMLogicOp.emAnd);
                stuSearchConditons.lsConditionGroups.Add(stuClassConditionGroup);

                if (null != stuPropertyAddrConditionGroup)
                {
                    stuSearchConditons.lsConditionGroups.Add(stuPropertyAddrConditionGroup);
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during establsih user email AD search conditions by bIncludeGroup:[{0}] bUserConditionLogicAnd:[{1}], emUserConditionCmpOp:[{2}], conditons:[{3}]", new object[] { bIncludeGroup,  bUserConditionLogicAnd, emUserConditionCmpOp, (null == szConditionKeyValues) ? "" : String.Join(", ", szConditionKeyValues) }, ex);
            }
            return stuSearchConditons;
        }

        private DirectoryEntry GetDirectoryEntry(Domain obDomain)
        {
            DirectoryEntry obDirectoryEntry = null;
            try
            {
                if (null != obDomain)
                {
                    obDirectoryEntry = obDomain.GetDirectoryEntry();
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during domain:[{0}] entry info failed", new object[] { m_strDomainName }, ex);
            }
            return obDirectoryEntry;
        }
        #endregion

        #region Members
        private readonly string m_strDomainName = "";
        private readonly DirectoryEntry m_obDirectoryEntry = null;
        #endregion
    }
}
