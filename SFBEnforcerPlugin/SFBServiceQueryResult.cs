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
    class SFBServiceQueryResult
    {
        #region Const values
        // Result codes
        public const int OPERATION_SUCCEED = 0;
        public const int OPERATION_FAILED = 1;
        public const int OPERATION_PROCESSING = 4;
        public const int OPERATION_NoManualClassify = 5;
        public const int OPERATION_MeetingNotExist = 6;
        #endregion

        #region Query result XML Node & Attr
        /*
            <?xml version="1.0" encoding="utf-8" ?>
            <PolicyResults>
              <ResultCode>0</ResultCode>
              <QueryIdentify>Guid</QueryIdentify>>
              <JoinResult>
                <Result Enforcement="Enforce_DontCare" Participant="satoshi.xiao@lync.nextlabs.solutions"></Result>
                <Result Enforcement="Enforce_Allow" Participant="joe.xu@lync.nextlabs.solutions"></Result>
                <Result Enforcement="Enforce_Deny" Participant="jimmy.carter@lync.nextlabs.solutions"></Result>
                <Result Enforcement="Enforce_Unknow" Participant="john.tyler@lync.nextlabs.solutions"></Result>
              </JoinResult>
            </PolicyResults>
         */
        private const string kstrXmlNode_PolicyResults = "PolicyResults";
        private const string kstrXmlNode_ResultCode = "ResultCode";
        private const string kstrXmlNode_QueryIdentify = "QueryIdentify";
        private const string kstrXmlNode_Results = "JoinResult";
        private const string kstrXmlNode_Result = "Result";

        private const string kstrXmlAttr_Enforcement = "Enforcement";
        private const string kstrXmlAttr_Participant = "Participant";
        #endregion

        #region Members
        private int m_nRresultCode = OPERATION_FAILED;
        private string m_strQueryIdentify = null;
        private List<PolicyResult> m_lsPolicyResults = null;
        #endregion

        public int ResultCode { get { return this.m_nRresultCode; } }

        /// <remarks/>
        public string QueryIdentify { get { return this.m_strQueryIdentify; } }

        /// <remarks/>
        public List<PolicyResult> PolicyResults { get { return this.m_lsPolicyResults; } }

        #region Constructors
        public SFBServiceQueryResult(int nRresultCodeIn, string nQueryIdentifyIn, List<PolicyResult> lsPolicyResultsIn)
        {
            m_nRresultCode = nRresultCodeIn;
            m_strQueryIdentify = nQueryIdentifyIn;
            m_lsPolicyResults = lsPolicyResultsIn;
        }
        public SFBServiceQueryResult(string strXMLResultInfo)
        {
            if (!String.IsNullOrEmpty(strXMLResultInfo))
            {
                InitFromXMLString(strXMLResultInfo);
            }
        }
        #endregion

        #region Inner init
        private bool InitFromXMLString(string strXMLResultInfo)
        {
            bool bRet = false;

            m_nRresultCode = OPERATION_FAILED;
            m_strQueryIdentify = null;
            m_lsPolicyResults = null;

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(strXMLResultInfo);

                bRet = InitFromXMLDoc(xmlDoc);
                CSLogger.OutputLog(LogLevel.Debug, "Analysis XML Result info:[{0}], ResultInfo:[{1}]\n", new object[] { bRet, strXMLResultInfo });
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception in InitFromXMLString, [{0}]\n[{1}]\n", new object[] { ex.Message, strXMLResultInfo });
            }
            return bRet;
        }
        private bool InitFromXMLDoc(XmlDocument xmlDoc)
        {
            bool bRet = false;

            m_nRresultCode = OPERATION_FAILED;
            m_strQueryIdentify = null;
            m_lsPolicyResults = null;

            try
            {
                // Select notify
                XmlNode nodeQueryResult = xmlDoc.SelectSingleNode(kstrXmlNode_PolicyResults);
                if (null != nodeQueryResult)
                {
                    string strResultCode = XMLTools.GetXMLNodeText(nodeQueryResult.SelectSingleNode(kstrXmlNode_ResultCode));
                    if (!String.IsNullOrEmpty(strResultCode))
                    {
                        m_nRresultCode = Int32.Parse(strResultCode);
                    }

                    m_strQueryIdentify = XMLTools.GetXMLNodeText(nodeQueryResult.SelectSingleNode(kstrXmlNode_QueryIdentify));

                    m_lsPolicyResults = new List<PolicyResult>();

                    XmlNode nodeResults = nodeQueryResult.SelectSingleNode(kstrXmlNode_Results);
                    if (null != nodeResults)
                    {
                        XmlNodeList nodeListResult = nodeResults.SelectNodes(kstrXmlNode_Result);
                        foreach (XmlNode nodeResult in nodeListResult)
                        {
                            string strEnforcement = XMLTools.GetAttributeValue(nodeResult.Attributes, kstrXmlAttr_Enforcement);
                            string strParticipant = XMLTools.GetAttributeValue(nodeResult.Attributes, kstrXmlAttr_Participant);

                            m_lsPolicyResults.Add(new PolicyResult(strEnforcement, strParticipant));
                        }
                    }
                    bRet = true;
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception in InitFromXMLDoc, [{0}]\n", new object[] { ex.Message });
            }
            return bRet;
        }
        #endregion
    }

    class PolicyResult
    {
        private string enforcementField = null;
        private string participantField = null;

        public PolicyResult(string strEnforcementIn, string strParticipantIn)
        {
            Enforcement = strEnforcementIn;
            Participant = strParticipantIn;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Enforcement
        {
            get
            {
                return this.enforcementField;
            }
            set
            {
                this.enforcementField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Participant
        {
            get
            {
                return this.participantField;
            }
            set
            {
                this.participantField = value;
            }
        }
    }

}
