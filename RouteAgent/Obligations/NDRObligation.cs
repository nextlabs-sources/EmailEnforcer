using Microsoft.Exchange.Data.Transport;
using Microsoft.Exchange.Data.Transport.Email;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Exchange.Data.Transport.Smtp;

namespace RouteAgent.Common
{
    public class NDRObligation
    {     
        private SmtpResponse m_smtpResponse;
        private MailItem m_mailItem;
        
        public NDRObligation(MailItem mailItem,string errorCode,string errorMsg)
        {
            MakeStandardSMTPErrorCode(ref errorCode);
            m_smtpResponse = new SmtpResponse(errorCode, string.Empty, errorMsg);
            m_mailItem=mailItem;
        }

        public void DoObligation(EnvelopeRecipient routingAddress)
        {
            m_mailItem.Recipients.Remove(routingAddress, DsnType.Failure, m_smtpResponse);

            for (int i = m_mailItem.Message.To.Count; i > 0; i--)
            {
                if (m_mailItem.Message.To[i - 1].SmtpAddress.Equals(routingAddress.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    m_mailItem.Message.To.Remove(m_mailItem.Message.To[i - 1]);
                }
            }

            for (int i = m_mailItem.Message.Cc.Count; i > 0; i--)
            {
                if (m_mailItem.Message.Cc[i - 1].SmtpAddress.Equals(routingAddress.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    m_mailItem.Message.Cc.Remove(m_mailItem.Message.Cc[i - 1]);
                }
            }

            for (int i = m_mailItem.Message.Bcc.Count; i > 0; i--)
            {
                if (m_mailItem.Message.Bcc[i - 1].SmtpAddress.Equals(routingAddress.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    m_mailItem.Message.Bcc.Remove(m_mailItem.Message.Bcc[i - 1]);
                }
            }
        }
        

        public void MakeStandardSMTPErrorCode(ref string strErrorCode)
        {
            // The error code must be three digits
            if (String.IsNullOrEmpty(strErrorCode))
            {
                strErrorCode = "100";
            }
            else if (1 == strErrorCode.Length)
            {
                strErrorCode += "00";
            }
            else if (2 == strErrorCode.Length)
            {
                strErrorCode += "0";
            }
            else if (3 == strErrorCode.Length)
            {
                // OK
            }
            else
            {
                strErrorCode = strErrorCode.Substring(0, 3);
            }

            try
            {
                Int32.Parse(strErrorCode);
            }
            catch (Exception)
            {
                strErrorCode = "100";
            }
        }       
    }
}
