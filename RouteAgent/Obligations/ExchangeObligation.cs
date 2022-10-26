using System;
using System.Collections.Generic;
using System.Text;

namespace RouteAgent.Common
{
    public class ExchangeObligation
    {
        private string m_strPolicyName;
        public string PolicyName
        {
            get { return m_strPolicyName; }
        }
        private string m_strObligationName;
        public string ObligationName
        {
            get { return m_strObligationName; }
        }
        public List<KeyValuePair<string, string>> lstAttribute;

        public ExchangeObligation(SDKWrapperLib.Obligation ob)
        {
            ob.get_policyname(out m_strPolicyName);
            ob.get_name(ref m_strObligationName);

            //get attribute
            SDKWrapperLib.CEAttres ceAttributes = new SDKWrapperLib.CEAttres();
            ob.get_attres(out ceAttributes);

            int lAttributeCount = 0;
            ceAttributes.get_count(out lAttributeCount);

            if (lAttributeCount > 0)
            {
                lstAttribute = new List<KeyValuePair<string, string>>(lAttributeCount);

                for (int iAttribute = 0; iAttribute < lAttributeCount; iAttribute++)
                {
                    string strAttrName = null;
                    string strAttrValue = null;
                    ceAttributes.get_attre(iAttribute, out strAttrName, out strAttrValue);

                    KeyValuePair<string, string> ObAttribute = new KeyValuePair<string, string>(strAttrName, strAttrValue);
                    lstAttribute.Add(ObAttribute);
                }
            }
        }

        public ExchangeObligation(JsonHelperDataModule.ObligationsNode obligation)
        {
            m_strPolicyName = "";
            m_strObligationName = obligation.Id;

            int attrCount = 0;
            if (obligation.AttributeAssignment != null)
            {
                attrCount = obligation.AttributeAssignment.Count;
            }
            if (attrCount > 1)
            {
                lstAttribute = new List<KeyValuePair<string, string>>(attrCount - 2);
            }

            string strAttrValue;
            for (int i = 0; i < attrCount; ++i)
            {
                string strAttrName = obligation.AttributeAssignment[i].AttributeId;
                if (obligation.AttributeAssignment[i].AttributeId != null)
                {
                    if (obligation.AttributeAssignment[i].Value != null)
                    {
                        if (obligation.AttributeAssignment[i].Value.Count > 0)
                        {
                            if (strAttrName.Equals("POLICY", StringComparison.OrdinalIgnoreCase))
                            {
                                m_strPolicyName = obligation.AttributeAssignment[i].Value[0];
                                continue;
                            }
                            strAttrValue = obligation.AttributeAssignment[i].Value[0];

                            KeyValuePair<string, string> ObAttribute = new KeyValuePair<string, string>(strAttrName, strAttrValue);
                            if (attrCount > 1)
                            {
                                lstAttribute.Add(ObAttribute);
                            }
                        }
                    }
                }
            }
        }

        public ExchangeObligation(string strPolicyName, string strObligationName)
        {
            m_strPolicyName = strPolicyName;
            m_strObligationName = strObligationName;
        }

        public string GetAttribute(string strName)
        {
            foreach (KeyValuePair<string, string> attr in lstAttribute)
            {
                if (attr.Key.Equals(strName, StringComparison.OrdinalIgnoreCase))
                {
                    return attr.Value;
                }
            }

            return string.Empty;
        }

    }
}
