using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Threading;
using CSBase.Diagnose;

namespace RouteAgent.Common
{
    public class Config
    {
        #region Const/Read only value
        static public readonly string g_kstrValue_Star = "*";
        #endregion

        protected static Configuration configuration = null;

        private static ReaderWriterLock rwlock = new ReaderWriterLock();
        public static bool Init(string strCfgFile)
        {
            CSLogger.OutputLog(LogLevel.Info, "Read Config file from:" + strCfgFile);
            try
            {
                rwlock.AcquireWriterLock(60 * 1000);
                if (!rwlock.IsWriterLockHeld)
                {
                    CSLogger.OutputLog(LogLevel.Warn, "Get Writer Lock failed.");
                    return false;
                }

                var configMap = new ExeConfigurationFileMap { ExeConfigFilename = strCfgFile.ToString() };
                configuration = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
                CSLogger.OutputLog(LogLevel.Info, "Read Config Success");
                return true;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Warn, "Read Config file Exception:" + ex.ToString());
                CSLogger.OutputLog(LogLevel.Warn, "Exchange Enforcer will try Read it after 60 second");
                System.Timers.Timer timer = new System.Timers.Timer(60 * 1000);
                timer.AutoReset = false;
                timer.Elapsed += (sender, e) =>
                {
                    Init(strCfgFile);
                };
                timer.Start();
                return false;
            }
            finally
            {
                rwlock.ReleaseWriterLock();
            }
        }

        public static T GetSection<T>(string strSectionName) where T : ConfigurationSection
        {
            T t = null;
            rwlock.AcquireReaderLock(60 * 1000);
            if (!rwlock.IsReaderLockHeld)
            {
                CSLogger.OutputLog(LogLevel.Warn, "Get Reader Lock failed.");
                return null;
            }

            try
            {
                t = configuration.GetSection(strSectionName) as T;
                if (t == null)
                {
                    t = default(T);
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during get session by name:[{0}]", new object[] { strSectionName }, ex);
            }
            finally
            {
                rwlock.ReleaseReaderLock();
            }
            return t;
        }

        protected static string GetConfigValue(string strKey)
        {
            rwlock.AcquireReaderLock(60 * 1000);
            if (!rwlock.IsReaderLockHeld)
            {
                CSLogger.OutputLog(LogLevel.Warn, "Get Reader Lock failed.");
                return string.Empty;
            }

            try
            {
                return configuration.AppSettings.Settings[strKey].Value;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Warn, "exception on ReadConfig,Key=" + strKey + " exception:" + ex.ToString());
                return string.Empty;
            }
            finally
            {
                rwlock.ReleaseReaderLock();
            }
        }

        public static bool IsNeedSearchUserAttrInForest()
        {
            bool bRet = true;
            string strSearchUserAttrInForest = Config.SearchUserAttrInForest;
            if (!string.IsNullOrEmpty(strSearchUserAttrInForest))
            {
                if (strSearchUserAttrInForest.Equals(Common.ConstVariable.Str_NO, StringComparison.OrdinalIgnoreCase))
                {
                    bRet = false;
                }

            }
           return bRet;
        }
        private static string SearchUserAttrInForest { get { return GetConfigValue("SearchUserAttrInForest"); } }

        public static string MessageTracingLogPath { get { return GetConfigValue("MessageTracingLogPath"); } }
        public static string DenyOnException { get { return GetConfigValue("DenyOnException"); } }
        public static string EmailEnforceInstalledPath
        {
            get
            {
                string strValue = GetConfigValue("EmailEnforceInstalledPath");
                if (!strValue.EndsWith("\\"))
                {
                    strValue += "\\";
                }
                return strValue;
            }
        }
        public static string ApprovalService
        {
            get
            {
                string strValue = GetConfigValue("ApprovalService");
                if (!strValue.EndsWith("/"))
                {
                    strValue += "/";
                }
                return strValue;

            }
        }
        public static HashSet<string> supportExtensionNames
        {
            get
            {
                HashSet<string> setSupportExtension = new HashSet<string>();
                rwlock.AcquireReaderLock(60 * 1000);
                if (!rwlock.IsReaderLockHeld)
                {
                    CSLogger.OutputLog(LogLevel.Warn, "Get Reader Lock failed.");
                    return setSupportExtension;
                }

                try
                {
                    SupportExtensionNamesSection exten = (SupportExtensionNamesSection)configuration.GetSection(RouteAgent.Common.ConstVariable.Str_Configuration_Section_Name_SupportExtensionNames);
                    if (exten != null)
                    {
                        if (exten.ExtensionNames != null)
                        {
                            foreach (ExtensionNameElement p in exten.ExtensionNames)
                            {
                                setSupportExtension.Add(p.value);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    CSLogger.OutputLog(LogLevel.Error, "Exception during get support extension names", null, ex);
                }
                finally
                {
                    rwlock.ReleaseReaderLock();
                }
                return setSupportExtension;
            }
        }
        public static string EmailNotifyObligatinSenderName { get { return GetConfigValue("EmailNotifyObligatinSenderName"); } }
        public static string EmailNotifyObligatinSenderEmailAddress { get { return GetConfigValue("EmailNotifyObligatinSenderEmailAddress"); } }
        public static string ExceptionNotifyTo { get { return GetConfigValue("ExceptionNotifyTo"); } }

        public static string ExceptionNotifySubject { get { return GetConfigValue("ExceptionNotifySubject"); } }
        public static string ExceptionNotifyBody{get{return GetConfigValue("ExceptionNotifyBody");}}
        public static string ExceptionNotifyAttachOriginEmail { get { return GetConfigValue("ExceptionNotifyAttachOriginEmail"); } }
        public static string NotifyWhenException { get { return GetConfigValue("NotifyWhenException"); } }
        public static string EmailHeaderFormat { get { return GetConfigValue("EmailHeaderFormat"); } }

        public static string SupportClientType { get { return GetConfigValue("SupportClientType"); } }

        //key that will do header check
        public static string SupportHeaderKey { get { return GetConfigValue("SupportHeaderKey"); } }
        public static string EmailHeaderMultiValueSplit { get {  return GetConfigValue("EmailHeaderMultiValueSplit"); } }


        public static int MultipleQueryLimite
        {
            get
            {
                int iDefault = ConstVariable.Int_MultipleQueryLimite_Default;
                string strReeult = GetConfigValue("MultipleQueryLimite");
                if (!string.IsNullOrEmpty(strReeult))
                {
                    if (!Int32.TryParse(strReeult, out iDefault))
                    {
                        iDefault = ConstVariable.Int_MultipleQueryLimite_Default;
                    }
                    else
                    {
                        if(iDefault==0)
                        {
                            iDefault = ConstVariable.Int_MultipleQueryLimite_Default;
                        }
                    }
                }
                return iDefault;
            }
        }

        public static bool RemoveRecipients
        {
            get
            {
                bool bresult = false;
                string strRemoveRecipients = GetConfigValue("RemoveRecipients");
                if (strRemoveRecipients != null)
                {
                    if (strRemoveRecipients.Equals(RouteAgent.Common.ConstVariable.Str_YES, StringComparison.OrdinalIgnoreCase))
                    {
                        bresult = true;
                    }
                }
                return bresult;
            }
        }
        public static bool EnableEnforce
        {
            get
            {
                bool bresult = false;
                string strEnableEnforce = GetConfigValue("EnableEnforce");
                if (strEnableEnforce != null)
                {
                    if (strEnableEnforce.Equals(RouteAgent.Common.ConstVariable.Str_YES, StringComparison.OrdinalIgnoreCase))
                    {
                        bresult = true;
                    }
                }
                return bresult;
            }
        }
        public static int RecipientsLimited
        {
            get
            {
                int iResult = 0;
                string strRecipientsLimited = GetConfigValue("RecipientsLimited");
                if (strRecipientsLimited != null)
                {
                    if (!Int32.TryParse(strRecipientsLimited, out iResult))
                    {
                        iResult = RouteAgent.Common.ConstVariable.Int_RecipientsLimited_Default;
                    }
                    else
                    {
                        if(iResult==0)
                        {
                            iResult = RouteAgent.Common.ConstVariable.Int_RecipientsLimited_Default;
                        }
                    }
                }
                return iResult;
            }
        }
    }

    public class ExtractGroup : ConfigurationSection
    {
        [ConfigurationProperty("value", IsRequired = true)]
        public string Value
        {
            get { return this["value"].ToString(); }
            set { this["value"] = value; }
        }
    }

    public class Email_Header_Format : ConfigurationSection
    {
        [ConfigurationProperty("value", IsRequired = true)]
        public string Value
        {
            get { return this["value"].ToString(); }
            set { this["value"] = value; }
        }
    }

    #region classificationMap
    public class classificationMap : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public classificationCollection classifications
        {
            get
            {
                return (classificationCollection)base[string.Empty];
            }
        }
    }

    public class classificationCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new Classification();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Classification)element);
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }
        protected override string ElementName
        {
            get
            {
                return "classification";
            }
        }

        public Classification this[int index]
        {
            get
            {
                return (Classification)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }
    }


    public class Classification : ConfigurationElement
    {
        private static readonly ConfigurationProperty s_property = new ConfigurationProperty(string.Empty, typeof(MyKeyValueCollection), null, ConfigurationPropertyOptions.IsDefaultCollection);
        [ConfigurationProperty("", Options = ConfigurationPropertyOptions.IsDefaultCollection)]
        public MyKeyValueCollection KeyValues
        {
            get
            {
                return (MyKeyValueCollection)base[s_property];
            }
        }

        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return this["name"].ToString(); }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("default-value", IsRequired = true)]
        public string Default_Value
        {
            get { return this["default-value"].ToString(); }
            set { this["default-value"] = value; }
        }
    }


    [ConfigurationCollection(typeof(MyKeyValueSetting))]
    public class MyKeyValueCollection : ConfigurationElementCollection        // 自定义一个集合
    {
        // 基本上，所有的方法都只要简单地调用基类的实现就可以了。

        public MyKeyValueCollection()
            : base(StringComparer.OrdinalIgnoreCase)    // 忽略大小写
        {
        }

        // 其实关键就是这个索引器。但它也是调用基类的实现，只是做下类型转就行了。
        new public MyKeyValueSetting this[string key]
        {
            get
            {
                return (MyKeyValueSetting)base.BaseGet(key);
            }
        }
        public MyKeyValueSetting this[int index]
        {
            get
            {
                return (MyKeyValueSetting)base.BaseGet(index);
            }
        }



        // 下面二个方法中抽象类中必须要实现的。
        protected override ConfigurationElement CreateNewElement()
        {
            return new MyKeyValueSetting();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MyKeyValueSetting)element).key;
        }
    }

    public class MyKeyValueSetting : ConfigurationElement    // 集合中的每个元素
    {
        [ConfigurationProperty("value", IsRequired = true)]
        public string key
        {
            get { return this["value"].ToString(); }
            set { this["value"] = value; }
        }

        [ConfigurationProperty("Clearance", IsRequired = true)]
        public string Clearance
        {
            get { return this["Clearance"].ToString(); }
            set { this["Clearance"] = value; }
        }
    }
    #endregion
    //Read white list section
    #region whiteListSection
    public class whiteListSection : ConfigurationSection
    {
        [ConfigurationProperty("senderList")]
        public senderListSection SenderListSetting { get { return (senderListSection)base["senderList"]; } }

        [ConfigurationProperty("headerList")]
        public headerListSection HeaderListSetting { get { return (headerListSection)base["headerList"]; } }
    }

    [ConfigurationCollection(typeof(addressSection), AddItemName = "address")]
    public class senderListSection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new addressSection();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((addressSection)element).Value;
        }

        public addressSection this[int index]
        {
            get { return (addressSection)base.BaseGet(index); }
        }

        new public addressSection this[string name]
        {
            get { return (addressSection)base.BaseGet(name); }
        }
    }
    [ConfigurationCollection(typeof(headerSection), AddItemName = "header")]
    public class headerListSection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new headerSection();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((headerSection)element).Name;
        }

        public headerSection this[int index]
        {
            get { return (headerSection)base.BaseGet(index); }
        }

        new public headerSection this[string name]
        {
            get { return (headerSection)base.BaseGet(name); }
        }
    }

    public class headerSection : ConfigurationElement
    {

        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name { get { return (string)this["name"]; } set { Name = value; } }

        [ConfigurationProperty("value", IsRequired = true)]
        public string Value { get { return (string)this["value"]; } set { Value = value; } }

    }
    public class addressSection : ConfigurationElement
    {
        [ConfigurationProperty("value", IsRequired = true)]
        public string Value { get { return (string)this["value"]; } set { Value = value; } }

    }
    #endregion

    #region SupportExtensionNamesSection

    public class SupportExtensionNamesSection : ConfigurationSection
    {
        [ConfigurationProperty("supportExtensionNames", IsRequired = true)]
        public string Category
        {

            get
            {
                return (string)base["Category"];
            }

            set
            {
                base["Category"] = value;
            }

        }
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public ExtensionNameCollection ExtensionNames
        {

            get
            {
                return (ExtensionNameCollection)base[""];
            }

        }

    }

    public class ExtensionNameCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ExtensionNameElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ExtensionNameElement)element).value;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        protected override string ElementName
        {
            get
            {
                return "ExtensionName";
            }
        }
        public ExtensionNameElement this[int index]
        {

            get
            {
                return (ExtensionNameElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }

        }
    }
    public class ExtensionNameElement : ConfigurationElement
    {
        [ConfigurationProperty("value", IsRequired = true)]
        public string value
        {
            get
            {
                return (string)base["value"];
            }
            set
            {
                base["value"] = value;
            }
        }
    }
    #endregion
}
