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
        public STUCondition<string, string> SearchConditionsField { get; private set; } // In group logic and, between group logic or
        public string SearchFilterField { get; private set; }
        public List<string> SpecifyAttrNamesField { get; private set; }
        public string CurSearchDebugInfo { get; private set; }
        private DirectorySearcher DirectorySearcherField { get; set; }

        // Inner group logic is And, between group logic is Or
        public DomainSearchManager(DirectoryEntry obDirectoryEntryIn, List<string> lsSpecifyAttrNamesIn, STUCondition<string, string> stuSearchConditonsIn)
        {
            if (null != obDirectoryEntryIn)
            {
                DirectoryNameField = obDirectoryEntryIn.Name;
                SearchConditionsField = stuSearchConditonsIn;
                SpecifyAttrNamesField = lsSpecifyAttrNamesIn;

                CurSearchDebugInfo = GetCurrentSearchDebugInfo();
                SearchFilterField = EstablishDomainSearchFilterCondition(SearchConditionsField);

                DirectorySearcherField = EstablishDirectorySearcher(obDirectoryEntryIn);
            }
        }
        public DomainSearchManager(DirectoryEntry obDirectoryEntryIn, List<string> lsSpecifyAttrNamesIn, bool bLogicAndIn, params STUConditionUnit<string, string>[] szStuPropertyFilterIn)
            : this(obDirectoryEntryIn, lsSpecifyAttrNamesIn, ConvertConditionFromArrayToConditionStructure(bLogicAndIn, szStuPropertyFilterIn))
        {
        }
        public DomainSearchManager(DirectoryEntry obDirectoryEntryIn, List<string> lsSpecifyAttrNamesIn, bool bLogicAndIn, EMCompareOp emLogicOpIn, params string[] szConditionKeyValuesIn)
            : this(obDirectoryEntryIn, lsSpecifyAttrNamesIn, ConvertConditionFromArrayToConditionStructure(bLogicAndIn, emLogicOpIn, szConditionKeyValuesIn))
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

        #region Independence public tools
        public static STUConditionGroup<string, string> ConvertConditionFromArrayToConditionGroup(bool bLogicAnd, EMCompareOp emLogicOp, params string[] szConditionKeyValues)
        {
            STUConditionGroup<string, string> stuConditionGroup = null;
            if ((null != szConditionKeyValues) && (0 < szConditionKeyValues.Length))
            {
                if (0 == ((szConditionKeyValues.Length) % 2))
                {
                    stuConditionGroup = new STUConditionGroup<string, string>(new List<STUConditionUnit<string, string>>(), bLogicAnd ? EMLogicOp.emAnd : EMLogicOp.emOr);
                    for (int i = 1; i < szConditionKeyValues.Length; i += 2)
                    {
                        stuConditionGroup.lsConditionGroup.Add(new STUConditionUnit<string, string>(szConditionKeyValues[i - 1], szConditionKeyValues[i], emLogicOp));
                    }
                }
                else
                {
                    CSLogger.OutputLog(LogLevel.Debug, "Code logic error, please check. Using a string array to establish condition string, but the array length:[{0}] is not even number, ", new object[] { szConditionKeyValues.Length });
                }
            }
            return stuConditionGroup;
        }
        public static STUConditionGroup<string, string> ConvertConditionFromArrayToConditionGroup(bool bLogicAnd, params STUConditionUnit<string, string>[] szStuPropertyFilter)
        {
            STUConditionGroup<string, string> stuConditionGroup = null;
            if ((null != szStuPropertyFilter) && (0 < szStuPropertyFilter.Length))
            {
                stuConditionGroup = new STUConditionGroup<string, string>(new List<STUConditionUnit<string, string>>(), bLogicAnd ? EMLogicOp.emAnd : EMLogicOp.emOr);
                foreach (STUConditionUnit<string, string> stuLogicUnitItem in szStuPropertyFilter)
                {
                    stuConditionGroup.lsConditionGroup.Add(stuLogicUnitItem);
                }
            }
            return stuConditionGroup;
        }

        public static STUCondition<string, string> ConvertConditionFromArrayToConditionStructure(bool bLogicAnd, EMCompareOp emLogicOp, params string[] szConditionKeyValues)
        {
            STUCondition<string, string> stuConditionsRet = null;
            STUConditionGroup<string, string> stuConditionGroup = ConvertConditionFromArrayToConditionGroup(bLogicAnd, emLogicOp, szConditionKeyValues);
            if (null != stuConditionGroup)
            {
                stuConditionsRet = new STUCondition<string, string>(new List<STUConditionGroup<string, string>>() { stuConditionGroup }, EMLogicOp.emOr);
            }
            return stuConditionsRet;
        }
        public static STUCondition<string, string> ConvertConditionFromArrayToConditionStructure(bool bLogicAnd, params STUConditionUnit<string, string>[] szStuPropertyFilter)
        {
            STUCondition<string, string> stuConditionsRet = null;
            STUConditionGroup<string, string> stuConditionGroup = ConvertConditionFromArrayToConditionGroup(bLogicAnd, szStuPropertyFilter);
            if (null != stuConditionGroup)
            {
                stuConditionsRet = new STUCondition<string, string>(new List<STUConditionGroup<string, string>>() { stuConditionGroup }, EMLogicOp.emOr);
            }
            return stuConditionsRet;
        }
        #endregion

        #region Independence inner tools
        private static string EstablishDomainSearchFilterCondition(STUCondition<string, string> stuSearchConditions)
        {
            StringBuilder sbDomainSearchFilter = new StringBuilder();
            if (null != stuSearchConditions.lsConditionGroups)
            {
                foreach (STUConditionGroup<string, string> stuLogicGroup in stuSearchConditions.lsConditionGroups)
                {
                    string strGroupConditon = EstablishDomainSearchFilterConditionGroup(stuLogicGroup);
                    sbDomainSearchFilter.Append(strGroupConditon);
                }

                if ((0 < sbDomainSearchFilter.Length) && (1 < stuSearchConditions.lsConditionGroups.Count))
                {
                    if (stuSearchConditions.emLogicBetweenGroup == EMLogicOp.emAnd)
                    {
                        sbDomainSearchFilter.Insert(0, "(&(");
                        sbDomainSearchFilter.Append("))");
                    }
                    else
                    {
                        // Default between group logic is or
                        sbDomainSearchFilter.Insert(0, "(|(");
                        sbDomainSearchFilter.Append("))");
                    }
                }
            }

            CSLogger.OutputLog(LogLevel.Debug, "Domain search filter:[{0}]", new object[] { sbDomainSearchFilter.ToString() });
            return sbDomainSearchFilter.ToString();
        }
        private static string EstablishDomainSearchFilterConditionGroup(STUConditionGroup<string, string> stuSearchConditionGroup)
        {
            StringBuilder sbLogicGroup = new StringBuilder();
            if (null != stuSearchConditionGroup.lsConditionGroup)
            {
                foreach (STUConditionUnit<string, string> stuLogicItem in stuSearchConditionGroup.lsConditionGroup)
                {
                    string strConditons = EstablishDomainSearchFilterConditionUnit(stuLogicItem, true);
                    sbLogicGroup.Append(strConditons);
                }

                if ((0 < sbLogicGroup.Length) && (1 < stuSearchConditionGroup.lsConditionGroup.Count))
                {
                    if (stuSearchConditionGroup.emLogicInGroup == EMLogicOp.emOr)
                    {
                        sbLogicGroup.Insert(0, "(|(");
                        sbLogicGroup.Append("))");
                    }
                    else
                    {
                        // Default inner group logic is And
                        sbLogicGroup.Insert(0, "(&(");
                        sbLogicGroup.Append("))");
                    }
                }
            }
            return sbLogicGroup.ToString();
        }
        private static string EstablishDomainSearchFilterConditionUnit(STUConditionUnit<string, string> stuSearchConditionUnit, bool bIncludeParenthesis)
        {
            string strLogicUnitRet = "";
            if (String.IsNullOrEmpty(stuSearchConditionUnit.tyKey) || String.IsNullOrEmpty(stuSearchConditionUnit.tyValue) || (EMCompareOp.emUnknown == stuSearchConditionUnit.emLogicOp))
            {
                CSLogger.OutputLog(LogLevel.Fatal, "Code error, please check. Current logic unit is error, this maybe a code error, Key:[{0}], Op:[{1}], value:[{2}]", new object[] { stuSearchConditionUnit.tyKey, stuSearchConditionUnit.tyValue, stuSearchConditionUnit.emLogicOp });
            }
            else
            {
                if (bIncludeParenthesis)
                {
                    strLogicUnitRet = String.Format("({0}{1}{2})", stuSearchConditionUnit.tyKey, CommonTools.CovertComapreOpFromEnumToString(stuSearchConditionUnit.emLogicOp), stuSearchConditionUnit.tyValue);
                }
                else
                {
                    strLogicUnitRet = String.Format("{0}{1}{2}", stuSearchConditionUnit.tyKey, CommonTools.CovertComapreOpFromEnumToString(stuSearchConditionUnit.emLogicOp), stuSearchConditionUnit.tyValue);
                }
            }
            return strLogicUnitRet;
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
        #endregion
    }
}
