using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Exchange.Data.Transport;

namespace RouteAgent.Common
{
    public class QueryPolicyResult
    {
        /// <summary>
        /// 
        /// </summary>
        private Microsoft.Exchange.Data.Transport.EnvelopeRecipient m_Recipient;
        public Microsoft.Exchange.Data.Transport.EnvelopeRecipient Recipient
        {
            get
            {
                return m_Recipient;
            }
        }
        private List<PolicyResult> m_LisPolicyReslts;
        public List<PolicyResult> LisPolicyReslts
        {
            get
            {
                return m_LisPolicyReslts;
            }
        }
        public bool Vaild
        {
            get;
            set;
        }
        private RoutingAddress m_Address;
        public RoutingAddress Address
        {
            get
            {
                return m_Address;
            }
        }
        
        

        public QueryPolicyResult(Microsoft.Exchange.Data.Transport.EnvelopeRecipient recipient,List<PolicyResult> lisPolicyReslts)
        {
            m_Recipient = recipient;
            m_LisPolicyReslts = lisPolicyReslts;
            m_Address = recipient.Address;
            Vaild = true;
                
        }
    }
}
