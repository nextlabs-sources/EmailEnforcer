using CSBase.Diagnose;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSBase.Common
{
    public class DomainSearchManager : IDisposable
    {
        public string DirectoryNameField { get; private set; }
        public List<List<STUConditionUnit<string, string>>> SearchConditionsField { get; private set; } // In group logic and, between group logic or
        public string SearchFilterField { get; private set; }
        public List<string> SpecifyAttrNamesField { get; private set; }
        public string CurSearchDebugInfo { get; private set; }
        private DirectorySearcher DirectorySearcherField { get; set; }

        // Inner group logic is And, between group logic is Or
        public DomainSearchManager(DirectoryEntry obDirectoryEntryIn, List<string> lsSpecifyAttrNamesIn, List<List<STUConditionUnit<string, string>>> lsSearchConditonsIn)
        {
            if (null != obDirectoryEntryIn)
            {
                DirectoryNameField = obDirectoryEntryIn.Name;
                SearchConditionsField = lsSearchConditonsIn;
                SpecifyAttrNamesField = lsSpecifyAttrNamesIn;

                CurSearchDebugInfo = GetCurrentSearchDebugInfo();
                SearchFilterField = EstablishDomainSearchFilter(SearchConditionsField);

                DirectorySearcherField = EstablishDirectorySearcher(obDirectoryEntryIn);
            }
        }
        public DomainSearchManager(DirectoryEntry obDirectoryEntryIn, List<string> lsSpecifyAttrNamesIn, bool bLogicAndIn, params STUConditionUnit<string, string>[] szStuPropertyFilterIn)
            : this(obDirectoryEntryIn, lsSpecifyAttrNamesIn, ConvertConditionFromArrayToList(bLogicAndIn, szStuPropertyFilterIn))
        {
        }
        public DomainSearchManager(DirectoryEntry obDirectoryEntryIn, List<string> lsSpecifyAttrNamesIn, bool bLogicAndIn, EMCompareOp emLogicOpIn, params string[] szConditionKeyValuesIn)
            : this(obDirectoryEntryIn, lsSpecifyAttrNamesIn, ConvertConditionFromArrayToList(bLogicAndIn, emLogicOpIn, szConditionKeyValuesIn))
        {
        }

        #region Interface implement : IDisposable
        public void Dispose()
        {
            try
            {
                if (null != DirectorySearcherField)
                {
                    DirectorySearcherField.Dispose();
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during dispose directory search field object with search info:[{0}]", new object[] { CurSearchDebugInfo }, ex);
            }
        }
        #endregion

        #region Domain tools
        public TyAttr GetFirstAttributeSearchValue<TyAttr>(TyAttr tyAttrDefaultValue) where TyAttr : class
        {
            TyAttr strPropertyValueRet = tyAttrDefaultValue;
            try
            {
                SearchResult obSearchResult = GetFirstSearchResult();
                if (null == obSearchResult)
                {
                    CSLogger.OutputLog(LogLevel.Error, "Failed to get first search result with search info:[{0}]", new object[] { CurSearchDebugInfo });
                }
                else
                {
                    strPropertyValueRet = GetAttributeSearchValue<TyAttr>(obSearchResult, SpecifyAttrNamesField.First(), tyAttrDefaultValue);
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during get domain info with search info:[{0}]", new object[] { CurSearchDebugInfo }, ex);
            }
            return strPropertyValueRet;
        }
        public TyAttrValue GetAttributeSearchValue<TyAttrValue>(SearchResult obSearchResult, string strAttrName, TyAttrValue tyAttrDefaultValue) where TyAttrValue : class
        {
            TyAttrValue strPropertyValueRet = tyAttrDefaultValue;
            try
            {
                if (null == obSearchResult)
                {
                    CSLogger.OutputLog(LogLevel.Error, "Failed to get first search result with search info:[{0}]", new object[] { CurSearchDebugInfo });
                }
                else
                {
                    if ((null != SpecifyAttrNamesField) && (0 < SpecifyAttrNamesField.Count))
                    {
                        strPropertyValueRet = GetAttributeValueByNameFromSearchResult<TyAttrValue>(obSearchResult, strAttrName, tyAttrDefaultValue);
                    }
                    else
                    {
                        CSLogger.OutputLog(LogLevel.Error, "Logic error, the specify attribute names is error, please check, search info:[{0}]", new object[] { CurSearchDebugInfo });
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during get domain info with search info:[{0}]", new object[] { CurSearchDebugInfo }, ex);
            }
            return strPropertyValueRet;
        }
        public SearchResultCollection GetSearchResultCollection()
        {
            SearchResultCollection obSearchResultCollection = null;
            try
            {
                DirectorySearcher dirSearcher = DirectorySearcherField;
                if (null == dirSearcher)
                {
                    CSLogger.OutputLog(LogLevel.Error, "Failed to estabblish directory search object for get user AD attribute collection with search info:[{0}]", new object[] { CurSearchDebugInfo });
                }
                else
                {
                    obSearchResultCollection = dirSearcher.FindAll();
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during search user AD attribute collection with search info:[{0}]", new object[] { CurSearchDebugInfo }, ex);
            }
            return obSearchResultCollection;
        }
        public SearchResult GetFirstSearchResult()
        {
            SearchResult obSearchResult = null;
            try
            {
                DirectorySearcher dirSearcher = DirectorySearcherField;
                if (null == dirSearcher)
                {
                    CSLogger.OutputLog(LogLevel.Error, "Failed to estabblish directory search object for get user AD attribute with search info:[{0}]", new object[] { CurSearchDebugInfo });
                }
                else
                {
                    obSearchResult = dirSearcher.FindOne();
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during search user first AD attribute with search info:[{0}]", new object[] { CurSearchDebugInfo }, ex);
            }
            return obSearchResult;
        }
        public TyAttr GetAttributeValueByNameFromSearchResult<TyAttr>(SearchResult obSearchResult, string strAttrName, TyAttr tyAttrDefaultValue) where TyAttr : class
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
        #endregion

        #region Inner tools
        private DirectorySearcher EstablishDirectorySearcher(DirectoryEntry obDirectoryEntry)
        {
            DirectorySearcher dirSearcher = null;
            try
            {
                dirSearcher = new DirectorySearcher(obDirectoryEntry);
                dirSearcher.Filter = SearchFilterField;
                SetAttrNamesInotPropertieToLoad(dirSearcher.PropertiesToLoad, SpecifyAttrNamesField);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during search user AD attribute collection with search info:[{0}]", new object[] { CurSearchDebugInfo }, ex);
            }
            return dirSearcher;
        }
        private string GetCurrentSearchDebugInfo()
        {
            try
            {
                return String.Format("DomainName:[{0}], SearchFilter:[{1}], SpecifyAttrName:[{2}]",
                    DirectoryNameField, SearchFilterField, (null == SpecifyAttrNamesField ? "" : String.Join(", ", SpecifyAttrNamesField)));
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during get current domain:[{0}] search debug info", new object[] { DirectoryNameField }, ex);
            }
            return "";
        }
        private static List<List<STUConditionUnit<string, string>>> ConvertConditionFromArrayToList(bool bLogicAnd, EMCompareOp emLogicOp, params string[] szConditionKeyValues)
        {
            List<List<STUConditionUnit<string, string>>> lsSearchConditionsRet = null;
            if ((null != szConditionKeyValues) && (0 < szConditionKeyValues.Length))
            {
                if (0 == ((szConditionKeyValues.Length) % 2))
                {
                    lsSearchConditionsRet = new List<List<STUConditionUnit<string, string>>>();
                    if (bLogicAnd)
                    {
                        List<STUConditionUnit<string, string>> lsAndGroupConditions = new List<STUConditionUnit<string, string>>();
                        for (int i = 1; i < szConditionKeyValues.Length; i += 2)
                        {
                            lsAndGroupConditions.Add(new STUConditionUnit<string, string>(szConditionKeyValues[i-1], szConditionKeyValues[i], emLogicOp));
                        }
                        lsSearchConditionsRet.Add(lsAndGroupConditions);
                    }
                    else
                    {
                        for (int i = 1; i < szConditionKeyValues.Length; i += 2)
                        {
                            lsSearchConditionsRet.Add(new List<STUConditionUnit<string, string>>() { new STUConditionUnit<string, string>(szConditionKeyValues[i - 1], szConditionKeyValues[i], emLogicOp) });
                        }
                    }
                }
                else
                {
                    CSLogger.OutputLog(LogLevel.Debug, "Code logic error, please check. Using a string array to establish condition string, but the array length:[{0}] is not even number, ", new object[] { szConditionKeyValues.Length });
                }
            }
            return lsSearchConditionsRet;
        }
        private static List<List<STUConditionUnit<string, string>>> ConvertConditionFromArrayToList(bool bLogicAnd, params STUConditionUnit<string, string>[] szStuPropertyFilter)
        {
            List<List<STUConditionUnit<string, string>>> lsSearchConditionsRet = null;
            if ((null != szStuPropertyFilter) && (0 < szStuPropertyFilter.Length))
            {
                lsSearchConditionsRet = new List<List<STUConditionUnit<string, string>>>();
                if (bLogicAnd)
                {
                    List<STUConditionUnit<string, string>> lsAndGroupConditions = new List<STUConditionUnit<string, string>>();
                    foreach (STUConditionUnit<string, string> stuLogicUnitItem in szStuPropertyFilter)
                    {
                        lsAndGroupConditions.Add(stuLogicUnitItem);
                    }
                    lsSearchConditionsRet.Add(lsAndGroupConditions);
                }
                else
                {
                    foreach (STUConditionUnit<string, string> stuLogicUnitItem in szStuPropertyFilter)
                    {
                        lsSearchConditionsRet.Add(new List<STUConditionUnit<string, string>>() { stuLogicUnitItem });
                    }
                }
            }
            return lsSearchConditionsRet;
        }
        private static string EstablishDomainSearchFilter(List<List<STUConditionUnit<string, string>>> lsSearchConditions)
        {
            string strDomainSearchFilterRet = "";

            StringBuilder sbDomainSearchFilter = new StringBuilder();
            if (null != lsSearchConditions)
            {
                foreach (List<STUConditionUnit<string, string>> lsLogicGroup in lsSearchConditions)
                {
                    StringBuilder sbLogicGroup = new StringBuilder();
                    foreach (STUConditionUnit<string, string> stuLogicItem in lsLogicGroup)
                    {
                        sbLogicGroup.Append(String.Format("{0}{1}{2}", stuLogicItem.tyKey, CommonTools.CovertLogicOpFromEnumToString(stuLogicItem.emLogicOp), stuLogicItem.tyValue));
                    }
                    if (0 < sbLogicGroup.Length)
                    {
                        // Inner group logic is And
                        sbLogicGroup.Insert(0, "(&(");
                        sbLogicGroup.Append("))");

                        sbDomainSearchFilter.Append(sbLogicGroup.ToString());
                    }
                    else
                    {
                        // Ignore
                    }
                }
            }

            if (0 < sbDomainSearchFilter.Length)
            {
                // Between group logic is or
                sbDomainSearchFilter.Insert(0, "(|(");
                sbDomainSearchFilter.Append("))");

                strDomainSearchFilterRet = sbDomainSearchFilter.ToString();
            }
            else
            {
                strDomainSearchFilterRet = "";
            }
            CSLogger.OutputLog(LogLevel.Debug, "Domain search filter:[{0}]", new object[] { strDomainSearchFilterRet });
            return strDomainSearchFilterRet;
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
