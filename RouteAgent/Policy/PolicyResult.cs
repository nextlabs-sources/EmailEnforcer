using System;
using System.Collections.Generic;
using System.Text;

namespace RouteAgent.Common
{
    public class PolicyResult
    {
        public EmailInfo emailInfo;
        public bool bDeny = false;
        private string m_strPolicyName = string.Empty;
        public string MatchedPolicyName
        {
            get
            {
                return m_strPolicyName;
            }
            set
            {
                if(m_strPolicyName.IndexOf(value)<0)
                {
                    m_strPolicyName = m_strPolicyName + " " + value;
                }
            }
        }
        public List<ExchangeObligation> lstExchangeObligations;

        public static PolicyResult GetDenyPolicyResult(List<PolicyResult> lstPolicyResult)
        {
            foreach (PolicyResult pr in lstPolicyResult)
            {
                if (pr.bDeny)
                {
                    return pr;
                }
            }

            return null;
        }
        public static bool HavePolicyNeedProcess(List<PolicyResult> lstPolicyResult)
        {
            bool result = false;
            foreach (PolicyResult pr in lstPolicyResult)
            {
                if (pr.bDeny)
                {
                    result = true;
                    break;
                }
                else if (pr.lstExchangeObligations != null)
                {
                    if (pr.lstExchangeObligations.Count > 0)
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }
        public static List<ExchangeObligation> GetExchangeObligation(PolicyResult pr, string strContentType, string strObligationName)
        {
            List<ExchangeObligation> lisResultObs = new List<ExchangeObligation>();
            if (pr.emailInfo.strContentType.Equals(strContentType) && (null != pr.lstExchangeObligations))
            {
                foreach (RouteAgent.Common.ExchangeObligation exOb in pr.lstExchangeObligations)
                {
                    if (exOb.ObligationName.Equals(strObligationName, StringComparison.OrdinalIgnoreCase))
                    {
                        lisResultObs.Add(exOb);
                    }
                }

            }
            return lisResultObs;
        }
        public static List<ExchangeObligation> GetExchangeObligation(PolicyResult pr, string strObligationName)
        {
           if((null==pr.lstExchangeObligations) || pr.lstExchangeObligations.Count==0)
           {
               return null;
           }

            List<ExchangeObligation> lisResultObs = new List<ExchangeObligation>();

            foreach (RouteAgent.Common.ExchangeObligation exOb in pr.lstExchangeObligations)
            {
                if (exOb.ObligationName.Equals(strObligationName, StringComparison.OrdinalIgnoreCase))
                {
                    lisResultObs.Add(exOb);
                }
            }

            return lisResultObs;
        }
        
    }
}
