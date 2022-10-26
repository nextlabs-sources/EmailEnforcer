using NextLabs.Diagnostic;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteAgent.Common
{
	public static class CommonADHelper
	{
		#region Const/Readonly values
		public const string kstrADKey_Mail = "mail";
		public const string kstrADKey_UserPrincipalName = "userprincipalname";
		public const string kstrADKey_ObjectSID = "objectSid";
		#endregion

		#region Public methods
		public static void LoopAllADUserProperties(string strKeyIn, string strValueIn)
		{
			try
			{
				DirectorySearcher dirSearcher = new DirectorySearcher();
				dirSearcher.Filter = String.Format("(&(objectClass=user)({0}={1}))", strKeyIn, strValueIn);

				SearchResultCollection obSearchResultCollection = GetUserADAttributeSearchResultCollection(strKeyIn, strValueIn, null);
				if (null == obSearchResultCollection)
				{
					CSLogger.OutputLog(LogLevel.Error, "Failed to get user AD properties by key:[{0}] value:[{1}], cannot find any item", new object[] { strKeyIn, strValueIn });
				}
				else
				{
					CSLogger.OutputLog(LogLevel.Debug, "\n\nLoop begin: \n");
					foreach (SearchResult obSearchResult in obSearchResultCollection)
					{
						if ((null == obSearchResult) || (null == obSearchResult.Properties))
						{
							CSLogger.OutputLog(LogLevel.Warn, "Failed to get AD properties SID by key:[{0}] value:[{1}], cannot find this item", new object[] { strKeyIn, strValueIn });
						}
						else
						{
							ResultPropertyCollection obResultPropertyCollection = obSearchResult.Properties;
							CSLogger.OutputLog(LogLevel.Debug, "\n\nBegin output item info: \n");
							foreach (string strNames in obResultPropertyCollection.PropertyNames)
							{
								ResultPropertyValueCollection obResultPropertyValueCollection = obSearchResult.Properties[strNames];
								foreach (object obItemValue in obResultPropertyValueCollection)
								{
									CSLogger.OutputLog(LogLevel.Debug, "\tName:[{0}], value:[{1}]", new object[] { strNames, obItemValue.ToString() });
								}
							}
							CSLogger.OutputLog(LogLevel.Debug, "\n\nEnd output item info \n");
						}
					}
					CSLogger.OutputLog(LogLevel.Debug, "\n\nLoop End \n");
				}
			}
			catch (Exception ex)
			{
				CSLogger.OutputLog(LogLevel.Error, "Exception during get user SID by key:[{0}] value:[{1}]", new object[] { strKeyIn, strValueIn }, ex);
			}
		}
		public static string GetUserSessionIDByEmailAddress(string strUserEmailAddress, bool bAutoCheckWithLogonName, string strDefaultSID)
		{
			// Both login name and eamil address can be used to send email
			// User login name and user email address can be different. BUG 37482
			// Both AD and Exchange ecp can specify a different email address
			// New email address is called email alias
			// The alias must be unique with others login name and alias
			// If the alias conflict, email address will be auto add an index number, but alias do not changed

			string strSIDRet = GetUserSessionIDByUniqueKey(kstrADKey_Mail, strUserEmailAddress, "");
			if (String.IsNullOrEmpty(strSIDRet))
			{
				if (bAutoCheckWithLogonName)
				{
					strSIDRet = GetUserSessionIDByUniqueKey(kstrADKey_UserPrincipalName, strUserEmailAddress, "");
					if (String.IsNullOrEmpty(strSIDRet))
					{
						strSIDRet = strDefaultSID;
					}
				}
				else
				{
					strSIDRet = strDefaultSID;
				}
			}
			CSLogger.OutputLog(LogLevel.Debug, "Get user:[{0}] SID with result:[{1}], AutoCheckLogonNameFlag:[{2}] default SID:[{3}]", new object[] { strUserEmailAddress, strSIDRet, bAutoCheckWithLogonName, strDefaultSID });
			return strSIDRet;
		}
		public static string GetStandardEmailAddressFromAD(string strUserEmailAddressOrg, bool bFailedReturnOriginalAddr)
		{
			string strStandardUserEmailAddressOut = bFailedReturnOriginalAddr ? strUserEmailAddressOrg : "";
			try
			{
				bool bExist = IsTheUserADAttributeExist(kstrADKey_Mail, strUserEmailAddressOrg);
				if (bExist)
				{
					// Current email address is OK
					strStandardUserEmailAddressOut = strUserEmailAddressOrg;
				}
				else
				{
					string strUserEmailAddressNew = GetUniquePropertyValueByUniqueKey<string>(kstrADKey_UserPrincipalName, strUserEmailAddressOrg, kstrADKey_Mail, "");
					if (String.IsNullOrEmpty(strUserEmailAddressNew))
					{
						CSLogger.OutputLog(LogLevel.Debug, "The eaml address:[{0}] is not AD eamil address or logon name", new object[] { strUserEmailAddressOrg });
					}
					else
					{
						strStandardUserEmailAddressOut = strUserEmailAddressNew;
					}
				}
			}
			catch (Exception ex)
			{
				CSLogger.OutputLog(LogLevel.Debug, "Exception during get standard email address from AD, user email adderss:[{0}], standard email address:[{1}], FailedReturnOriginalAddr:[{2}]", new object[] { strUserEmailAddressOrg, strStandardUserEmailAddressOut, bFailedReturnOriginalAddr }, ex);
			}
			CSLogger.OutputLog(LogLevel.Debug, "Passin user email adderss:[{0}], standard email address:[{1}], FailedReturnOriginalAddr:[{2}]", new object[] { strUserEmailAddressOrg, strStandardUserEmailAddressOut, bFailedReturnOriginalAddr });
			return strStandardUserEmailAddressOut;
		}

		public static bool IsTheUserADAttributeExist(string strUniqueKey, string strUniqueValue)
		{
			bool bRet = false;
			try
			{
				SearchResult obSearchResult = GetUserADAttributeFirstSearchResult(strUniqueKey, strUniqueValue, new List<string>() { kstrADKey_ObjectSID });

				bRet = (null != obSearchResult);
			}
			catch (Exception ex)
			{
				CSLogger.OutputLog(LogLevel.Error, "Exception during check user AD attribute by key:[{0}] value:[{1}]", new object[] { strUniqueKey, strUniqueValue }, ex);
			}
			return bRet;
		}

		public static string GetUserSessionIDByUniqueKey(string strUniqueKey, string strUniqueValue, string strDefaultSid)
		{
			string strSIDRet = strDefaultSid;
			try
			{
				byte[] szByteSid = GetUniquePropertyValueByUniqueKey<byte[]>(strUniqueKey, strUniqueValue, kstrADKey_ObjectSID, null);
				if (null == szByteSid)
				{
					CSLogger.OutputLog(LogLevel.Error, "Failed to get user SID attribute by key:[{0}] value:[{1}]", new object[] { strUniqueKey, strUniqueValue });
				}
				else
				{
					strSIDRet = GetStringSIDFromByteArray(szByteSid);
				}
			}
			catch (Exception ex)
			{
				CSLogger.OutputLog(LogLevel.Error, "Exception during get user SID by key:[{0}] value:[{1}]", new object[] { strUniqueKey, strUniqueValue }, ex);
			}
			return strSIDRet;
		}
		#endregion

		#region Inner tools
		private static string GetStringSIDFromByteArray(byte[] szByteSid)
		{
			string strSIDRet = "";
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
		private static SearchResultCollection GetUserADAttributeSearchResultCollection(string strUniqueKey, string strUniqueValue, List<string> lsSpecifyAttrNames)
		{
			SearchResultCollection obSearchResultCollection = null;
			try
			{
				DirectorySearcher dirSearcher = new DirectorySearcher();
				dirSearcher.Filter = String.Format("(&(objectClass=user)({0}={1}))", strUniqueKey, strUniqueValue);
				SetAttrNamesInotPropertieToLoad(dirSearcher.PropertiesToLoad, lsSpecifyAttrNames);

				obSearchResultCollection = dirSearcher.FindAll();
			}
			catch (Exception ex)
			{
				CSLogger.OutputLog(LogLevel.Error, "Exception during search user AD attribute collection by key:[{0}] value:[{1}]", new object[] { strUniqueKey, strUniqueValue }, ex);
			}
			return obSearchResultCollection;
		}
		private static SearchResult GetUserADAttributeFirstSearchResult(string strUniqueKey, string strUniqueValue, List<string> lsSpecifyAttrNames)
		{
			SearchResult obSearchResult = null;
			try
			{
				DirectorySearcher dirSearcher = new DirectorySearcher();
				dirSearcher.Filter = String.Format("(&(objectClass=user)({0}={1}))", strUniqueKey, strUniqueValue);
				SetAttrNamesInotPropertieToLoad(dirSearcher.PropertiesToLoad, lsSpecifyAttrNames);

				obSearchResult = dirSearcher.FindOne();
			}
			catch (Exception ex)
			{
				CSLogger.OutputLog(LogLevel.Error, "Exception during search user first AD attribute by key:[{0}] value:[{1}], SpecifyAttrName", new object[] { strUniqueKey, strUniqueValue }, ex);
			}
			return obSearchResult;
		}

		private static TyAttr GetUniquePropertyValueByUniqueKey<TyAttr>(string strUniqueKey, string strUniqueValue, string strAttrName, TyAttr tyAttrDefaultValue) where TyAttr : class
		{
			TyAttr strPropertyValueRet = tyAttrDefaultValue;
			try
			{
				SearchResult obSearchResult = GetUserADAttributeFirstSearchResult(strUniqueKey, strUniqueValue, new List<string>() { strAttrName });
				strPropertyValueRet = GetUniquePropertyValueFromSearchResult<TyAttr>(obSearchResult, strAttrName, tyAttrDefaultValue);
			}
			catch (Exception ex)
			{
				CSLogger.OutputLog(LogLevel.Error, "Exception during get user SID by key:[{0}] value:[{1}] strAttrName:[{2}]", new object[] { strUniqueKey, strUniqueValue, strAttrName }, ex);
			}
			return strPropertyValueRet;
		}
		private static TyAttr GetUniquePropertyValueFromSearchResult<TyAttr>(SearchResult obSearchResult, string strAttrName, TyAttr tyAttrDefaultValue) where TyAttr : class
		{
			TyAttr obPropertyValueRet = tyAttrDefaultValue;
			try
			{
				if ((null == obSearchResult) || (null == obSearchResult.Properties))
				{
					CSLogger.OutputLog(LogLevel.Warn, "Failed to get unique property string value by attr name:[{0}]", new object[] { strAttrName });
				}
				else
				{
					ResultPropertyValueCollection obResultPropertyValueCollection = obSearchResult.Properties[strAttrName];
					if (null == obResultPropertyValueCollection)
					{
						CSLogger.OutputLog(LogLevel.Warn, "Failed to get unique property string value by attr name:[{0}], cannot find item", new object[] { strAttrName });
					}
					else
					{
						if (0 < obResultPropertyValueCollection.Count)
						{
							obPropertyValueRet = obResultPropertyValueCollection[0] as TyAttr;
						}
					}
				}
			}
			catch (Exception ex)
			{
				CSLogger.OutputLog(LogLevel.Warn, "Exception to get unique property string value by key:[{0}]", new object[] { strAttrName }, ex);
			}
			return obPropertyValueRet;
		}

		private static bool SetAttrNamesInotPropertieToLoad(StringCollection obPropertiesToLoad, List<string> lsSpecifyAttrNames)
		{
			bool bRet = false;
			try
			{
				if ((null != obPropertiesToLoad) && (null != lsSpecifyAttrNames))
				{
					foreach (string strSpecifyAttrName in lsSpecifyAttrNames)
					{
						if (!String.IsNullOrEmpty(strSpecifyAttrName))
						{
							obPropertiesToLoad.Add(strSpecifyAttrName);
						}
					}
				}
				bRet = true;
			}
			catch (Exception ex)
			{
				CSLogger.OutputLog(LogLevel.Debug, "Exception during set attribute names into properties to load objects", null, ex);
			}
			return bRet;
		}
		#endregion
	}
}
