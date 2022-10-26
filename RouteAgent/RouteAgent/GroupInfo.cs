using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Exchange.Data.Transport;

namespace RouteAgent.Common
{
    public class GroupInfo
    {
        private EnvelopeRecipient m_Recipient;
        public EnvelopeRecipient Recipient
        {
            get { return m_Recipient; }
        }
        private RecipientType m_Type;
        public RecipientType Type
        {
            get { return m_Type; }
        }

        public GroupInfo(EnvelopeRecipient recipient, RecipientType recipientType,RoutingAddress address)
        {
            m_Recipient = recipient;
            m_Type = recipientType;
            m_Address = address;
        }

        private List<PolicyResult> m_lisPolicyResult;
        public List<PolicyResult> PolicyResults
        {
            get { return m_lisPolicyResult; }
            set { m_lisPolicyResult = value; }
        }

        //some times , we get routing address from ENVELOPRECIPIEN WILL show error  Object name: 'This EnvelopRecipient has already been disposed.'. StackTrace:   at Microsoft.Exchange.Transport.MailRecipientWrapper.DisposeValidation() 

        private RoutingAddress m_Address;
        public RoutingAddress Address
        {
            get { return m_Address; }
        }
    }
}
