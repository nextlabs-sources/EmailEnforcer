using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Exchange.Data.Transport;
using Microsoft.Exchange.Data.Transport.Email;
using System.IO;
using Microsoft.Exchange.Data.Mime;
using CSBase.Diagnose;

namespace RouteAgent.Common
{
    public class ApprovalObligation
    {
        private EmailRecipient emailApprover=null;
        private EmailRecipient Approver
        {
            get
            {
                return emailApprover;
            }
            set
            {
                emailApprover = value;
            }
        }
        private EmailMessage emailMessage = null;
        private EmailMessage Message
        {
            get
            {
                return emailMessage;
            }
            set
            {
                emailMessage = value;
            }
        }
        private SmtpServer smtpServer = null;
        private SmtpServer SMTPServer
        {
            get
            {
                return smtpServer;
            }
            set
            {
                smtpServer = value;
            }
        }
        public ApprovalObligation(SmtpServer server, EmailMessage message,EmailRecipient emailApproverSmtpAddress)
        {
            this.Approver = emailApproverSmtpAddress;
            this.Message = message;
            this.SMTPServer = server;
        }
        public void DoObligation()
        {
            CSLogger.OutputLog(LogLevel.Debug, "DoObligation Start");
            SaveTolocal(Message);
            EmailRecipient recpSender=new EmailRecipient(RouteAgent.Common.Config.EmailNotifyObligatinSenderName, RouteAgent.Common.Config.EmailNotifyObligatinSenderEmailAddress);
            List<EmailRecipient> lisRecipi = new List<EmailRecipient>()
            {
                this.Approver
            };
            List<Model.EmailAttachment> lisModelEmailAttachments = null;
            if (Message.Attachments.Count > 0)
            {
                 lisModelEmailAttachments = new List<Model.EmailAttachment>();
                foreach(Attachment attch in Message.Attachments)
                {
                    Model.EmailAttachment modelAttachment = new Model.EmailAttachment();
                    modelAttachment.FileName = attch.FileName;
                    modelAttachment.ContentType = attch.ContentType;
                    using (Stream streamAttach = attch.GetContentReadStream())
                    {
                        modelAttachment.FileContent = Function.GetByteFormStream(streamAttach);
                    }
                    lisModelEmailAttachments.Add(modelAttachment);
                }
            }
            EmailMessage emailNewMessage = CreateEmailMessage(recpSender, lisRecipi, Policy.m_strObsApprovalMailSubject, BodyFormat.Html, Policy.GetObsApprovalMailHtmlBody(Message), lisModelEmailAttachments);
            SMTPServer.SubmitMessage(emailNewMessage);
            CSLogger.OutputLog(LogLevel.Debug, "DoObligation End");
        }

        private string SaveTolocal(EmailMessage msg)
        {

            string strFilePath =Function.GetMsgFilePath(msg.MessageId);
            CSLogger.OutputLog(LogLevel.Debug, "SaveTolocal Start ,Save file to local:" + strFilePath);
            if (!File.Exists(strFilePath))
            {
                Model.EmailModel emailModel = GetEmailModel(msg);
                string strJsonEmailModel = Function.JsonSerializer.SaveToJson(emailModel);
                string strEncodeJsonEmailModel = Function.Encode(strJsonEmailModel);
                FileStream fs = null;
                try
                {
                    fs = new FileStream(strFilePath, FileMode.CreateNew, FileAccess.Write);
                    using (StreamWriter sr = new StreamWriter(fs))
                    {
                        fs = null;
                        sr.Write(strEncodeJsonEmailModel);
                    }

                }
                finally
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                }
            }
            CSLogger.OutputLog(LogLevel.Debug, "SaveTolocal End");
            return strFilePath;
        }
        private Model.EmailModel GetEmailModel(EmailMessage msg)
        {
            Model.EmailModel model = new Model.EmailModel();
            //form
            model.From = new Model.EmailAddress();
            model.From.DisplayName = msg.From.DisplayName;
            model.From.SmtpAddress = msg.From.SmtpAddress;
            //to
            if(msg.To.Count>0)
            {
                model.To = new List<Model.EmailAddress>();
                foreach(EmailRecipient item in msg.To)
                {
                    Model.EmailAddress address = new Model.EmailAddress();
                    address.DisplayName = item.DisplayName;
                    address.SmtpAddress = item.SmtpAddress;
                    model.To.Add(address);
                }
            }
            //cc
            if (msg.Cc.Count > 0)
            {
                model.Cc = new List<Model.EmailAddress>();
                foreach (EmailRecipient item in msg.Cc)
                {
                    Model.EmailAddress address = new Model.EmailAddress();
                    address.DisplayName = item.DisplayName;
                    address.SmtpAddress = item.SmtpAddress;
                    model.Cc.Add(address);
                }
            }
            //subject
            model.Subject = msg.Subject;
            //body
            model.Body = new Model.EmailBody();
            model.Body.BodyFormat = msg.Body.BodyFormat.ToString();
            using(Stream streamBodyReader = msg.Body.GetContentReadStream())
            {
                byte[] byteBody = new byte[streamBodyReader.Length];
                streamBodyReader.Read(byteBody, 0, byteBody.Length);
                model.Body.BodyContent = byteBody;
            }

            //attachment
            if(msg.Attachments.Count>0)
            {
                model.EmailAttachments = new List<Model.EmailAttachment>();
                foreach(Attachment item in msg.Attachments)
                {
                    Model.EmailAttachment attachment = new Model.EmailAttachment();
                    attachment.FileName = item.FileName;
                    attachment.ContentType = item.ContentType;

                    using (Stream streamAttachment = item.GetContentReadStream())
                    {
                        byte[] byteAttachment = new byte[streamAttachment.Length];
                        streamAttachment.Read(byteAttachment, 0, byteAttachment.Length);
                        attachment.FileContent = byteAttachment;
                    }
                    model.EmailAttachments.Add(attachment);
                }
            }
            //headers
            if(msg.MimeDocument.RootPart.Headers!=null)
            {
                model.Headers = new List<Model.EmailHeader>();
                MimeDocument mdMimeDoc = msg.MimeDocument;
                foreach(var item in mdMimeDoc.RootPart.Headers)
                {
                    Model.EmailHeader header=new Model.EmailHeader();
                    header.HeaderName = item.Name;
                    header.HeaderValue = item.Value;
                    model.Headers.Add(header);
                }

            }
            return model;
        }
        private EmailMessage CreateEmailMessage(EmailRecipient Sender, List<EmailRecipient> lisRecipients, string strSubject, BodyFormat bodyFormat, string strbody,List<Model.EmailAttachment> lisAttahments)
        {
            CSLogger.OutputLog(LogLevel.Debug, "CreateEmailMessage Start");
            EmailMessage message = EmailMessage.Create(bodyFormat);
            message.Subject = strSubject;

            using (Stream streamBody = message.Body.GetContentWriteStream())
            {
                byte[] byteBody = System.Text.Encoding.ASCII.GetBytes(strbody);
                streamBody.Write(byteBody, 0, byteBody.Length);
            }
            message.From = Sender;
            foreach (EmailRecipient emailRecipient in lisRecipients)
            {
                message.To.Add(emailRecipient);
            }
            if(lisAttahments!=null)
            {
                foreach(Model.EmailAttachment modelAttach in lisAttahments)
                {
                    Attachment attch= message.Attachments.Add(modelAttach.FileName, modelAttach.ContentType);
                    using(Stream streamAttachWrite= attch.GetContentWriteStream())
                    {
                        streamAttachWrite.Write(modelAttach.FileContent, 0, modelAttach.FileContent.Length);
                    }
                }
            }
            //add special header, so that this email can by ignored by our agent
            RouteAgent.Common.Function.AddEmailHeader(ConstVariable.Str_NextlabsHeader_Key, ConstVariable.Str_MailClassify_ApprovalMail, message);
            CSLogger.OutputLog(LogLevel.Debug, "CreateEmailMessage End");
            return message;
        }

        public static bool ObligationExits(ApprovalObligation obligation, List<ApprovalObligation> lisObligations)
        {
            CSLogger.OutputLog(LogLevel.Debug, "ObligationExits Start");
            bool result = false;
            if (lisObligations != null)
            {
                foreach (ApprovalObligation item in lisObligations)
                {
                    if(item.Message.Equals(obligation.Message))
                    {
                        if (item.Approver.SmtpAddress.Equals(obligation.Approver.SmtpAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
            CSLogger.OutputLog(LogLevel.Debug, "ObligationExits End result is :" + result);
            return result;
        }

    }
}
