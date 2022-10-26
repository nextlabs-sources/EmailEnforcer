using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Exchange.Data.Transport;

namespace RouteAgent.Common
{
    public class ForkItem
    {

        private string m_strPolicyName;
        public string PolicyName
        {
            get
            {
                return m_strPolicyName;
            }
        }

        private List<EnvelopeRecipient> m_lisRecipients;
        public List<EnvelopeRecipient> Recipients
        {
            get
            {
                return m_lisRecipients;
            }
        }

        private List<PolicyResult> m_lisPolicyResult;

        public List<PolicyResult> LisPolicyResult
        {
            get
            {
                return m_lisPolicyResult;
            }
        }

        public ForkItem(string strPolicyName,List<PolicyResult> lisPolicyResult)
        {
            m_strPolicyName = strPolicyName;
            m_lisRecipients = new List<EnvelopeRecipient>();
            m_lisPolicyResult = lisPolicyResult;
        }

        public static void Add(string strPolicyName, EnvelopeRecipient recipient, List<ForkItem> forkLists, List<PolicyResult> lisPolicyResult)
        {
            bool bFind = false;
            foreach(ForkItem p in forkLists)
            {
                if (p.PolicyName.Equals(strPolicyName, StringComparison.OrdinalIgnoreCase))
                {
                    bFind = true;
                    p.Recipients.Add(recipient);
                    break;
                }
            }
            if(!bFind)
            {
                ForkItem forkList = new ForkItem(strPolicyName, lisPolicyResult);
                forkList.Recipients.Add(recipient);
                forkLists.Add(forkList);
            }
        }
    }
}
