using Microsoft.Exchange.Data.Transport;
using Microsoft.Exchange.Data.Transport.Email;

using CSBase.Diagnose;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RouteAgent.Common
{
    public class NotifyObligation
    {
        public enum SendToTarget
        {
            sender,
            receiver,
            other
        }

        private EmailRecipient m_emailPeciSender = null;
        private List<EmailRecipient> m_lisEmailPeciReceives = null;
        private string m_strSubject = string.Empty;
        private string m_strBody = string.Empty;
        private string m_AttachOrigEmail;
        private EmailMessage m_emailMessageorigMsg = null;
        private List<string> m_lisDenyRecipients = null;

        private bool m_bException = false;
        private string m_strDenyedSubject;
        public string DenyedSubject
        {
            get
            {
                return m_strDenyedSubject;
            }
        }

        public List<string> DenyRecipients
        {
            get
            {
                return m_lisDenyRecipients;
            }
        }

        public EmailMessage OriginEmailMessage
        {
            get
            {
                return m_emailMessageorigMsg;
            }
            set
            {
                m_emailMessageorigMsg = value;
            }
        }
        public string NotifyFormat
        {
            get
            {
                return m_strBody;
            }
            set
            {
                m_strBody = value;
            }
        }
        public string AttachOrIgEmail
        {
            get
            {
                return m_AttachOrigEmail;
            }
            set
            {
                m_AttachOrigEmail = value;
            }
        }
        public string Subject
        {
            get
            {
                return m_strSubject;
            }
            set
            {
                m_strSubject = value;
            }
        }
        public EmailRecipient Sender
        {
            get
            {
                return m_emailPeciSender;
            }
            set
            {
                m_emailPeciSender = value;
            }
        }
        public List<EmailRecipient> Recipients
        {
            get
            {
                return m_lisEmailPeciReceives;
            }
            set
            {
                m_lisEmailPeciReceives = value;
            }
        }

        public NotifyObligation(EmailMessage emailMessageOrig, List<EmailRecipient> lisemailReciRecipients, string strSubject, string strBody, string strAttachorigEmail,bool bException=false)
        {
            this.OriginEmailMessage = emailMessageOrig;
            this.Sender = new EmailRecipient(RouteAgent.Common.Config.EmailNotifyObligatinSenderName, RouteAgent.Common.Config.EmailNotifyObligatinSenderEmailAddress);
            this.Recipients = lisemailReciRecipients;
            this.Subject = strSubject;
            this.NotifyFormat = strBody;
            this.AttachOrIgEmail = strAttachorigEmail;
            m_lisDenyRecipients = new List<string>();
            m_strDenyedSubject = emailMessageOrig.Subject;
            m_bException = bException;
        }
        public static bool ObligationExits(NotifyObligation obligation, List<NotifyObligation> lisObligations)
        {
            CSLogger.OutputLog(LogLevel.Debug, "ObligationExits Start");
            bool result = false;
            if (lisObligations != null)
            {
                foreach (NotifyObligation item in lisObligations)
                {
                    if (item.Sender.SmtpAddress.Equals(obligation.Sender.SmtpAddress, StringComparison.OrdinalIgnoreCase) &&
                        item.Subject.Equals(obligation.Subject, StringComparison.OrdinalIgnoreCase) &&
                        item.NotifyFormat.Equals(obligation.NotifyFormat, StringComparison.OrdinalIgnoreCase) &&
                        item.AttachOrIgEmail.Equals(obligation.AttachOrIgEmail))
                    {

                        foreach (EmailRecipient recipient in item.Recipients)
                        {
                            if (!obligation.Recipients.Exists(dir => { return dir.SmtpAddress.Equals(recipient.SmtpAddress, StringComparison.OrdinalIgnoreCase); }))
                            {
                                return false;
                            }
                        }

                        result = true;

                    }
                }
            }
            CSLogger.OutputLog(LogLevel.Debug, "ObligationExits End result is :" + result);
            return result;
        }

        public static NotifyObligation GetExitsObligation(NotifyObligation obligation, List<NotifyObligation> lisObligations)
        {
            CSLogger.OutputLog(LogLevel.Debug, "ObligationExits Start");
            NotifyObligation notify = null;
            if (lisObligations != null)
            {
                foreach (NotifyObligation item in lisObligations)
                {
                    if (item.Sender.SmtpAddress.Equals(obligation.Sender.SmtpAddress, StringComparison.OrdinalIgnoreCase) &&
                        item.Subject.Equals(obligation.Subject, StringComparison.OrdinalIgnoreCase) &&
                        item.NotifyFormat.Equals(obligation.NotifyFormat, StringComparison.OrdinalIgnoreCase) &&
                        item.AttachOrIgEmail.Equals(obligation.AttachOrIgEmail))
                    {

                        foreach (EmailRecipient recipient in item.Recipients)
                        {
                            if (!obligation.Recipients.Exists(dir => { return dir.SmtpAddress.Equals(recipient.SmtpAddress, StringComparison.OrdinalIgnoreCase); }))
                            {
                                return notify;
                            }
                        }

                        notify = item;

                    }
                }
            }
            return notify;
        }

        public void DoObligation(SmtpServer smtpServer)
        {
            CSLogger.OutputLog(LogLevel.Debug, "DoObligation Start");
            EmailMessage newMsg = EmailMessage.Create(BodyFormat.Html);

            //sender
            newMsg.From = this.Sender;
            //receiver
            foreach (EmailRecipient emailRecipient in this.Recipients)
            {
                if (!newMsg.To.Contains(emailRecipient))
                {
                    newMsg.To.Add(new EmailRecipient(emailRecipient.DisplayName,emailRecipient.SmtpAddress));
                }
            }

            //subject
            newMsg.Subject = this.Subject;

            //body
            string strBodyFormat = this.NotifyFormat;
            if (!m_bException)
            {

                string[] strArry = strBodyFormat.Split('%');

                StringBuilder sbBody = new StringBuilder();
                for (int i = 0; i < strArry.Length; i++)
                {
                    if (i == 0)
                    {
                        sbBody.Append(strArry[0]);
                    }
                    else
                    {
                        if (strArry[i].Length > 0)
                        {
                            if (char.ToLower(strArry[i][0]).CompareTo(ConstVariable.Char_Email_Subject_Split) == 0)
                            {
                                sbBody.Append(OriginEmailMessage.Subject);
                                sbBody.Append(strArry[i].Substring(1, strArry[i].Length - 1));
                            }
                            else if (char.ToLower(strArry[i][0]).CompareTo(ConstVariable.Char_Email_From_Split) == 0)
                            {
                                sbBody.Append(OriginEmailMessage.From.SmtpAddress);
                                sbBody.Append(strArry[i].Substring(1, strArry[i].Length - 1));
                            }
                            else if (char.ToLower(strArry[i][0]).CompareTo(ConstVariable.Char_Email_Recipients_Split) == 0)
                            {
                                string strRecipient = "</br>";
                                foreach (var recipient in DenyRecipients)
                                {
                                    strRecipient += recipient;
                                    strRecipient += "</br>";
                                }
                                sbBody.Append(strRecipient);
                                sbBody.Append(strArry[i].Substring(1, strArry[i].Length - 1));
                            }
                            else
                            {
                                sbBody.Append("%" + strArry[i]);
                            }
                        }
                    }
                }
                strBodyFormat = sbBody.ToString();
            }
            //
            Stream streamBody = newMsg.Body.GetContentWriteStream();
            byte[] byteBody = System.Text.Encoding.ASCII.GetBytes(strBodyFormat);
            streamBody.Write(byteBody, 0, byteBody.Length);
            streamBody.Flush();
            streamBody.Close();

            //attachment
            string strAttachOrigEmail = this.AttachOrIgEmail;

            if (strAttachOrigEmail.Equals(RouteAgent.Common.Policy.m_strObEmailNotifyAttachOrigEmailValueY,StringComparison.OrdinalIgnoreCase))
            {
                Attachment attach = newMsg.Attachments.Add("OriginalMessage", "message/rfc822");
                attach.EmbeddedMessage = this.OriginEmailMessage;
            }

            //add special header, so that this email can by ignored by our agent
            RouteAgent.Common.Function.AddEmailHeader(ConstVariable.Str_NextlabsHeader_Key, ConstVariable.Str_MailClassify_DenyNotiy, newMsg);

            //submit
            smtpServer.SubmitMessage(newMsg);

            CSLogger.OutputLog(LogLevel.Debug, "DoObligation End");
        }
    }
}
