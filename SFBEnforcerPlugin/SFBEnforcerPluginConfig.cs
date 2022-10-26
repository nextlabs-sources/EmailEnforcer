using CSBase.Common;
using CSBase.Diagnose;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SFBEnforcerPlugin
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    class SFBEnforcerPluginConfig
    {
        #region XML values
        // EnforceConfig
        static private readonly string kstrXmlNode_PluginConfig = "PluginConfig";
        static private readonly string kstrXmlNode_EnforcerConfig = "EnforcerConfig";
        static private readonly string kstrXmlNode_ResponseCheckHeartBeatMs = "ResponseCheckHeartBeatMs";
        static private readonly string kstrXmlNode_SingleRequestMaxTimeMs = "SingleRequestMaxTimeMs";
        static private readonly string kstrXmlNode_IsDefaultBehaviorAllow = "IsDefaultBehaviorAllow";
        static private readonly string kstrXmlNode_IsSupportEvaMulMeetingInAnEmail = "IsSupportEvaMulMeetingInAnEmail";
        static private readonly string kstrXmlNode_SFBPolicyAssistantWebService = "SFBPolicyAssistantWebService";
        static private readonly string kstrXmlNode_NotifyWhenDeny = "NotifyWhenDeny";
        static private readonly string kstrXmlNode_DenyNotifySubject = "DenyNotifySubject";
        static private readonly string kstrXmlNode_DenyNotifyBody = "DenyNotifyBody";
        static private readonly string kstrXmlNode_DenyNotifyBodyWithException = "DenyNotifyBodyWithException";
        static private readonly string kstrXmlNode_DenyNotifyBodyWithNoClassification = "DenyNotifyBodyWithNoClassification";
        static private readonly string kstrXmlNode_DenyNotifyAttachOriginEmail = "DenyNotifyAttachOriginEmail";
        static private readonly string kstrXmlNode_NeedCombineTDFAttachmentXHeader = "NeedCombineTDFAttachmentXHeader";
        #endregion

        #region members
        private int ResponseCheckHeartBeatMsField = 100;
        private int singleRequestMaxTimeMsField = 60 * 1000;
        private bool isDefaultBehaviorAllowField = false;
        private bool isSupportEvaMulMeetingInAnEmailField = false;

        private string m_strSFBPolicyAssistantWebServiceUrl = "";

        private string notifyWhenDenyField = "Yes";
        private string denyNotifySubjectField = "You have one email was denied by Exchange Enforcer";
        private string denyNotifyBodyField = "You have one email was denied by Exchange Enforcer,when mail's Subject:%s;/br&gt;Recipient:%t&lt;/br";
        private string denyNotifyBodyWithExceptionField = "An exception had happen on Exchange Enforcer &lt;/br&gt;An exception is thrown , when mail's Subject:%s &lt;/br&gt;Sender: %f&lt;/br&gt;Recipient:%t&lt;/br&gt;ErrorMessage:%e";
        private string denyNotifyBodyWithNoClassificationField = "You have not done the SFB meeting classification action, all recipients cannot receive the email! You can get the classify uri from SFB assistant in SFB.";
        private string denyNotifyAttachOriginEmailField = "Yes";
        private bool needCombineTDFAttachmentXHeaderField = false;
        #endregion

        #region Singleton
        static private object s_obLockForInstance = new object();
        static private SFBEnforcerPluginConfig s_obSFBEnforcerPluginConfigIns = null;
        static public SFBEnforcerPluginConfig GetInstance()
        {
            if (null == s_obSFBEnforcerPluginConfigIns)
            {
                lock (s_obLockForInstance)
                    if (null == s_obSFBEnforcerPluginConfigIns)
                    {
                        s_obSFBEnforcerPluginConfigIns = new SFBEnforcerPluginConfig();
                    }
            }
            return s_obSFBEnforcerPluginConfigIns;
        }
        private SFBEnforcerPluginConfig()
        {
        }
        #endregion

        #region Fields
        public int ResponseCheckHeartBeatMs
        {
            get
            {
                return this.ResponseCheckHeartBeatMsField;
            }
            set
            {
                this.ResponseCheckHeartBeatMsField = value;
            }
        }

        /// <remarks/>
        public int SingleRequestMaxTimeMs
        {
            get
            {
                return this.singleRequestMaxTimeMsField;
            }
            set
            {
                this.singleRequestMaxTimeMsField = value;
            }
        }

        /// <remarks/>
        /// false: deny, true: allow
        public bool IsDefaultBehaviorAllow
        {
            get
            {
                return this.isDefaultBehaviorAllowField;
            }
            set
            {
                this.isDefaultBehaviorAllowField = value;
            }
        }
        public bool IsSupportEvaMulMeetingInAnEmail
        {
            get
            {
                return this.isSupportEvaMulMeetingInAnEmailField;
            }
            set
            {
                this.isSupportEvaMulMeetingInAnEmailField = value;
            }
        }

        public string SFBPolicyAssistantWebServiceUrl
        {
            get { return this.m_strSFBPolicyAssistantWebServiceUrl; }
            set { this.m_strSFBPolicyAssistantWebServiceUrl = value; }
        }

        /// <remarks/>
        public string NotifyWhenDeny
        {
            get
            {
                return this.notifyWhenDenyField;
            }
            set
            {
                this.notifyWhenDenyField = value;
            }
        }

        /// <remarks/>
        public string DenyNotifySubject
        {
            get
            {
                return this.denyNotifySubjectField;
            }
            set
            {
                this.denyNotifySubjectField = value;
            }
        }
        /// <remarks/>
        public string DenyNotifyBody
        {
            get
            {
                return this.denyNotifyBodyField;
            }
            set
            {
                this.denyNotifyBodyField = value;
            }
        }

        /// <remarks/>
        public string DenyNotifyBodyWithException
        {
            get
            {
                return this.denyNotifyBodyWithExceptionField;
            }
            set
            {
                this.denyNotifyBodyWithExceptionField = value;
            }
        }
        /// <remarks/>
        public string DenyNotifyBodyWithNoClassification
        {
            get
            {
                return this.denyNotifyBodyWithNoClassificationField;
            }
            set
            {
                this.denyNotifyBodyWithNoClassificationField = value;
            }
        }
        /// <remarks/>
        public string DenyNotifyAttachOriginEmail
        {
            get
            {
                return this.denyNotifyAttachOriginEmailField;
            }
            set
            {
                this.denyNotifyAttachOriginEmailField = value;
            }
        }

        public bool NeedCombineTDFAttachmentXHeader
        {
            get
            {
                return this.needCombineTDFAttachmentXHeaderField;
            }
            set
            {
                this.needCombineTDFAttachmentXHeaderField = value;
            }
        }
        #endregion

        #region Init
        public void InitFromFile(string strConfigFilePath)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(strConfigFilePath);

                // Select TDF Config
                InnerInitFromXmlDocument(xmlDoc);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Analysis SFB plugin config file:[{0}] exception, message:[{1}]\n", new object[] { strConfigFilePath, ex.Message });
            }

        }
        public void InitFromXmlData(string strConfigXmlData)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(strConfigXmlData);

                // Select TDF Config
                InnerInitFromXmlDocument(xmlDoc);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Analysis SFB plugin config data:[{0}] exception, message:[{1}]\n", new object[] { strConfigXmlData, ex.Message });
            }

        }
        private void InnerInitFromXmlDocument(XmlDocument xmlDoc)
        {
            XmlNode nodePluginConfig = xmlDoc.SelectSingleNode(kstrXmlNode_PluginConfig);
            if (null != nodePluginConfig)
            {
                XmlNode nodeEnforcerConfig = nodePluginConfig.SelectSingleNode(kstrXmlNode_EnforcerConfig);
                if (null != nodeEnforcerConfig)
                {
                    InitEnforcerConfig(nodeEnforcerConfig);
                }

                XmlNode nodeTDFConfig = nodePluginConfig.SelectSingleNode(Nextlabs.TDFFileAnalyser.TDFXHeaderExtralConfig.kstrXmlNode_TDFConfig);
                if (null != nodeTDFConfig)
                {
                    Nextlabs.TDFFileAnalyser.TDFXHeaderExtralConfig.GetInstance().Init(nodeTDFConfig);
                }
            }
        }
        private void InitEnforcerConfig(XmlNode nodeEnforcerConfig)
        {
            if (null != nodeEnforcerConfig)
            {
                ResponseCheckHeartBeatMs = CommonTools.ConvertStringToInt(XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_ResponseCheckHeartBeatMs), 100);
                SingleRequestMaxTimeMs = CommonTools.ConvertStringToInt(XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_SingleRequestMaxTimeMs), 60 * 1000);
                IsDefaultBehaviorAllow = CommonTools.ConvertStringToBoolean(XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_IsDefaultBehaviorAllow), false);
                IsSupportEvaMulMeetingInAnEmail = CommonTools.ConvertStringToBoolean(XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_IsSupportEvaMulMeetingInAnEmail), true);
                SFBPolicyAssistantWebServiceUrl = XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_SFBPolicyAssistantWebService);

                NotifyWhenDeny = XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_NotifyWhenDeny);
                DenyNotifySubject = XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_DenyNotifySubject);
                DenyNotifyBody = XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_DenyNotifyBody);
                DenyNotifyBodyWithException = XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_DenyNotifyBodyWithException);
                DenyNotifyBodyWithNoClassification = XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_DenyNotifyBodyWithNoClassification);
                DenyNotifyAttachOriginEmail = XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_DenyNotifyAttachOriginEmail);

                NeedCombineTDFAttachmentXHeader = CommonTools.ConvertStringToBoolean(XMLTools.GetXMLSingleNodeText(nodeEnforcerConfig, kstrXmlNode_NeedCombineTDFAttachmentXHeader), false);
            }
        }
        #endregion
    }
}
