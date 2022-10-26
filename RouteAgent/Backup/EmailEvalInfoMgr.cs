using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Exchange.Data.Mime;
using Microsoft.Exchange.Data.Transport;
using Microsoft.Exchange.Data.Transport.Email;
using SDKWrapperLib;

namespace Common
{
    public class EmailEvalInfoManage
    {
        public EmailEvalInfoManage()
        {

        }

        protected static CLog theLog = CLog.GetLogger(typeof(EmailEvalInfoManage));

        private string m_strClientType = string.Empty;
        public string ClientType
        {
            get { return m_strClientType; }
            set { m_strClientType = value; }
        }
        private List<Common.EmailInfo> m_listEmailInfo = null;
        public List<Common.EmailInfo> EmailInfos
        {
            get { return m_listEmailInfo; }
        }


        private string m_strAction = string.Empty;
        public string Action
        {
            get
            {
                return m_strAction;
            }
        }

        private string m_strMailClassification = Common.ConstVariable.Str_MailClassify_MAPIMsgClsSubmitLamProbe;
        public string MailClassification
        {
            get
            {
                return m_strMailClassification;
            }
            set
            {
                m_strMailClassification = value;
            }
        }

        private List<Common.GroupInfo> m_lisGropuInfo = null;
        public List<Common.GroupInfo> GroupInfos
        {
            get { return m_lisGropuInfo; }
        }

        private List<KeyValuePair<string, string>> m_lisHeaders = null;
        public List<KeyValuePair<string, string>> Headers
        {
            get { return m_lisHeaders; }
        }

        private List<KeyValuePair<string, string>> m_lisPairClassification = null;
        public List<KeyValuePair<string, string>> Classifications
        {
            get { return m_lisPairClassification; }
        }

        private List<KeyValuePair<string, string>> m_lisPairHeader = null;
        public List<KeyValuePair<string, string>> PairHeaders
        {
            get { return m_lisPairHeader; }
        }

        private EmailRecipient m_Sender;
        public EmailRecipient Sender
        {
            get
            {
                return m_Sender;
            }
        }

        private string m_strSendSid;
        public string SendSid
        {
            get
            {
                return m_strSendSid;
            }
        }


        private bool m_bNeedProcess = true;
        public bool NeedProcess
        {
            get { return m_bNeedProcess; }
        }

        private string m_MapiClass;
        public string MapiMessageClass
        {
            get
            {
                return m_MapiClass;
            }
        }

        private string FormatHeader(string strKey)
        {
            string strEmailHeaderFormat = (Config.EmailHeaderFormat == null) ? "" : Config.EmailHeaderFormat;
            return strEmailHeaderFormat.Replace(ConstVariable.Str_Email_Header_Format_Split, strKey);
        }


        public void Init(Microsoft.Exchange.Data.Transport.MailItem mailItem, Microsoft.Exchange.Data.Transport.SmtpServer server, string emailOutputTempDir)
        {
            // 不需要处理的类型，比如一些回执，或者退信等
            string[] arryStrNoProcessMsgCls = { Common.ConstVariable.Str_MailClassify_MAPIMsgClsSubmitLamProbe, Common.ConstVariable.Str_MailClassify_MAPIMsgClsReportNoteNDR };
            m_MapiClass = mailItem.Message.MapiMessageClass;

            if (!arryStrNoProcessMsgCls.Contains<string>(m_MapiClass, StringComparer.OrdinalIgnoreCase))
            {
                // 将邮件里面的发送信息解析出来，放入一个新生成的EmailRecipient对象
                m_Sender = GetSender(mailItem);
                if (m_Sender != null)
                {
                    //  去exchangepep.xml 找到白名单
                    whiteListSection whiteList = Common.Config.GetSection<whiteListSection>(Common.ConstVariable.Str_Configuration_Section_Name_WhiteList);
                    if (whiteList != null)
                    {
                        if (!CheckInSenderList(m_Sender.SmtpAddress, whiteList.SenderListSetting))
                        {
                            // 不在白名单内，需要处理
                            m_lisHeaders = GetHeaders(mailItem);
                            if (!CheckInHeaderList(m_lisHeaders, whiteList.HeaderListSetting))
                            {
                                // 在服务器的addressbook上面查找是否存在收件人，如果存在就把它放入成员变量中
                                m_lisGropuInfo = GetGroupInfo(mailItem.Recipients, server);

                                // 把邮件的各部分保持在临时文件中
                                m_listEmailInfo = SaveEmailInfo(mailItem.Message, emailOutputTempDir);

                                // PlugIn 会用C#的反射技术加载外部的程序块，即插件， 在Factory中实现加载
                                m_lisPairClassification = GetClassification(Common.PlugIn.Instances, m_lisHeaders, mailItem);

                                // 通过server的AcceptedDomains找Sender的域名，如果找到，这个邮件是OutMail， 如果找不到是InMail
                                m_strAction = GetEvaluationAction(server, mailItem);

                                m_strSendSid = GetUserSessionID(Sender.SmtpAddress);

                                m_strClientType = GetCilentType(mailItem.Message.MessageId);

                                //从配置文件中读取PairHeader 的Key信息
                               m_lisPairHeader = GetPairHeader(m_lisHeaders);

                            }
                            else
                            {
                                // 在白名单内，不需要处理
                                m_bNeedProcess = false;
                                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Ignore this mail , because this mail's header in whiteList ", mailItem.Message.Subject, mailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                            }

                        }
                        else
                        {
                            m_bNeedProcess = false;
                            CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Ignore this mail , because this mail's Sender's address in whiteList ", mailItem.Message.Subject, mailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                        }
                    }
                }
                else
                {
                    m_bNeedProcess = false;
                    CSLogger.OutputLog(LogLevel.Error, string.Format("Subject:{1} | MessageId:{2} | Message:{0}", "Can't do enforcer to this mail , because this mail's Sender is empty", mailItem.Message.Subject, mailItem.Message.MessageId));
                }
            }
            else
            {
                m_bNeedProcess = false;
                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Ignore this mail , because this mail's mapiclass is " + m_MapiClass, mailItem.Message.Subject, mailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
            }
        }


        private string GetMailClassify(Microsoft.Exchange.Data.Transport.Email.EmailMessage message)
        {
            string strMailClassify = string.Empty;
            try
            {
                //no need process mapi message type
                string[] strNoProcessMsgCls = { Common.ConstVariable.Str_MailClassify_MAPIMsgClsSubmitLamProbe, "Report.IPM.Note.NDR" };
                foreach (string strMsgCls in strNoProcessMsgCls)
                {
                    if (strMsgCls.Equals(message.MapiMessageClass, StringComparison.OrdinalIgnoreCase))
                    {
                        strMailClassify = Common.ConstVariable.Str_MailClassify_MAPIMsgClsSubmitLamProbe;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(strMailClassify))
                {
                    List<string> lisNextlabsHeaderVals = new List<string>()
                    {
                        Common.ConstVariable.Str_MailClassify_DenyNotiy
                    };

                    foreach (var header in message.MimeDocument.RootPart.Headers)
                    {
                        if (header.Name.Equals(Common.ConstVariable.Str_NextlabsHeader_Key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (lisNextlabsHeaderVals.Exists(dir => { return dir.Equals(header.Value, StringComparison.OrdinalIgnoreCase); }))
                            {
                                strMailClassify = header.Value;
                                break;
                            }
                        }
                    }
                }
                return strMailClassify;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, ex);
                return strMailClassify;
            }
            finally
            {
            }
        }

        private string GetEvaluationAction(Microsoft.Exchange.Data.Transport.SmtpServer server, Microsoft.Exchange.Data.Transport.MailItem mailItem)
        {
            string rcptDomain = mailItem.FromAddress.DomainPart;
            if (string.IsNullOrEmpty(rcptDomain))
            {
                EmailRecipient sender = GetSender(mailItem);
                if (sender != null)
                {
                    string strSendrSmtp = sender.SmtpAddress;
                    if (strSendrSmtp.Contains("@"))
                    {
                        rcptDomain = strSendrSmtp.Split('@')[strSendrSmtp.Split('@').Length - 1];
                    }
                }
                else
                {
                    return Policy.m_strEmailActionInBound;
                }

            }
            Microsoft.Exchange.Data.Transport.AcceptedDomain acceptedDomain = server.AcceptedDomains.Find(rcptDomain);
            if (acceptedDomain == null)
            {
                return Policy.m_strEmailActionInBound;
            }
            else
            {
                return Policy.m_strEmailActionOutBound;
            }

        }

        private List<GroupInfo> GetGroupInfo(EnvelopeRecipientCollection recipients, SmtpServer server)
        {
            List<GroupInfo> lisGroupInfo = new List<GroupInfo>();
            if (recipients != null)
            {
                foreach (var recipient in recipients)
                {
                    AddressBookEntry book = server.AddressBook.Find(recipient.Address);
                    if (book != null)
                    {
                        if (book.RecipientType.Equals(RecipientType.DistributionList))
                        {
                            Common.GroupInfo groupInfo = new GroupInfo(recipient, book.RecipientType, recipient.Address);
                            lisGroupInfo.Add(groupInfo);
                        }
                    }
                }
            }
            return lisGroupInfo;
        }

        private List<EmailInfo> SaveEmailInfo(EmailMessage message, string strOutDir)
        {
            try
            {
                EmailMessage emailMessage;

                // 如果是Task， 把第0个Attachment当作邮件（为什么呢？）
                if (this.MapiMessageClass.Contains(ConstVariable.Str_MailClassify_MAPITASK))
                {
                    emailMessage = message.Attachments[0].EmbeddedMessage;
                }
                else
                {
                    emailMessage = message;
                }

                List<EmailInfo> lstEmailInfo = new List<EmailInfo>(emailMessage.Attachments.Count + 2);

                //save subject
                string strSubjectFile = strOutDir + ConstVariable.Str_EmailFile_Prefix_Sunbject + Guid.NewGuid().ToString() + ConstVariable.Str_EmailFile_Extension;
                FileStream file = null;
                try
                {
                    file = new FileStream(strSubjectFile, FileMode.Create);
                    file.Seek(0, SeekOrigin.Begin);


                    using (StreamWriter streamWrite = new StreamWriter(file))
                    {
                        file = null;
                        streamWrite.Write(emailMessage.Subject);
                        //streamWrite.Flush();
                        //streamWrite.Close();
                        //file.Close();
                    }
                }
                finally
                {
                    if (file != null)
                    {
                        file.Dispose();
                    }
                }
                EmailInfo emailInfoSubject = new EmailInfo();
                emailInfoSubject.strContentType = Common.Policy.m_strContentTypeEmailSubject;
                emailInfoSubject.strSavedPath = strSubjectFile;

                lstEmailInfo.Add(emailInfoSubject);

                //save body
                Stream streamBodyReader = emailMessage.Body.GetContentReadStream();

                string strBodyFile = strOutDir + ConstVariable.Str_EmailFile_Prefix_Body + Guid.NewGuid().ToString() + ConstVariable.Str_EmailFile_Extension;
                using (FileStream fileBody = new FileStream(strBodyFile, FileMode.Create))
                {

                    byte[] byteBody = new byte[ConstVariable.Int_ReadLenOneTimeBody];
                    while (true)
                    {
                        int nReadLen = streamBodyReader.Read(byteBody, 0, ConstVariable.Int_ReadLenOneTimeBody);

                        fileBody.Write(byteBody, 0, nReadLen);

                        if (nReadLen < ConstVariable.Int_ReadLenOneTimeBody)
                        {
                            break;
                        }

                    }
                }
                EmailInfo emailInfoBody = new EmailInfo()
                {
                    strContentType = Common.Policy.m_strContentTypeEmailBody,
                    strSavedPath = strBodyFile
                };

                lstEmailInfo.Add(emailInfoBody);


                //save attachment

                AttachmentCollection attachments = emailMessage.Attachments;
                if (attachments.Count > 0)
                {
                    int indexAttach = -1;
                    foreach (Attachment attachment in attachments)
                    {
                        indexAttach++;
                        string strExtension = GetFileSuffix(attachment.FileName);
                        if (IsSupportExtension(strExtension))
                        {
                            Stream streamAttachment = attachment.GetContentReadStream();

                            string strAttachmentFile = strOutDir + ConstVariable.Str_EmailFile_Prefix_Attach + attachment.FileName;
                            using (FileStream fileAttachment = new FileStream(strAttachmentFile, FileMode.Create))
                            {
                                byte[] byteAttachment = new byte[ConstVariable.Int_ReadLenOneTimeAttach];
                                while (true)
                                {
                                    int nReadLen = streamAttachment.Read(byteAttachment, 0, ConstVariable.Int_ReadLenOneTimeAttach);

                                    fileAttachment.Write(byteAttachment, 0, nReadLen);

                                    if (nReadLen < ConstVariable.Int_ReadLenOneTimeAttach)//means the file is end
                                    {
                                        break;
                                    }
                                }

                            }
                            EmailInfo emailInfoAttachment = new EmailInfo()
                            {
                                strContentType = Policy.m_strContentTypeEmailAttachment,
                                strSavedPath = strAttachmentFile,
                                strName = attachment.FileName,
                                attachInfo = new AttachInfo()
                                {
                                    attach = attachment,
                                    lisObligationFiles = new List<ObligationFile>(),
                                    strAttrachContentType = attachment.ContentType,
                                    strAttachName = attachment.FileName,
                                    embeddedMessage = attachment.EmbeddedMessage,
                                    index = indexAttach,
                                    listTags = new List<KeyValuePair<string,string>>()
                                }
                            };

                            //read file tag.
                            try
                            {
                                string strAttachSavedFileName = emailInfoAttachment.strSavedPath;
                                FileType ftAttachmentFile = Common.Function.GetFileType(strAttachSavedFileName);
                                if (ftAttachmentFile != FileType.Nextlabs)
                                {
                                    FileTagManager tagMgr = new FileTagManager();
                                    int lCount = 0;
                                    tagMgr.GetTagsCount(strAttachSavedFileName, out lCount);
                                    CSLogger.OutputLog(LogLevel.Info, string.Format("Read file tag: {0} count:{1}", emailInfoAttachment.attachInfo.strAttachName, lCount));
                                    for (int i = 0; i < lCount; i++)
                                    {
                                        string strName, strValue;
                                        tagMgr.GetTagByIndex(strAttachSavedFileName, i, out strName, out strValue);
                                        CSLogger.OutputLog(LogLevel.Info, string.Format("Read file tag:{0}, value: {1}={2}", strAttachSavedFileName, strName, strValue));
                                        if ((!string.IsNullOrWhiteSpace(strName)) && (!string.IsNullOrWhiteSpace(strValue)))
                                        {
                                            emailInfoAttachment.attachInfo.listTags.Add(new KeyValuePair<string, string>(strName, strValue));
                                        }
                                        else
                                        {
                                            CSLogger.OutputLog(LogLevel.Info, "ignore this tag");
                                        }
                                    }
                                }
                                else
                                {//read .nxl file tag
                                    INLRightsManager nlRManager = new NLRightsManager();
                                    int lCount = 0;
                                    nlRManager.NLGetTagsCount(strAttachSavedFileName, out lCount);
                                    CSLogger.OutputLog(LogLevel.Info, string.Format("NLGetTagsCount: {0} count:{1}", emailInfoAttachment.attachInfo.strAttachName, lCount));
                                    for (int i = 0; i < lCount; i++)
                                    {
                                        string strName, strValue;
                                        nlRManager.NLReadTags(strAttachSavedFileName, i, out strName, out strValue);
                                        CSLogger.OutputLog(LogLevel.Info, string.Format("Read file tag:{0}, value: {1}={2}", emailInfoAttachment.attachInfo.strAttachName, strName, strValue));
                                        if ((!string.IsNullOrWhiteSpace(strName)) && (!string.IsNullOrWhiteSpace(strValue)))
                                        {
                                            emailInfoAttachment.attachInfo.listTags.Add(new KeyValuePair<string, string>(strName, strValue));
                                        }
                                        else
                                        {
                                            CSLogger.OutputLog(LogLevel.Info, "ignore this nxl tag");
                                        }
                                    }
                                }

                            }
                            catch(Exception ex)
                            {
                                CSLogger.OutputLog(LogLevel.Error, string.Format("Read file tag exception, file:{0}, message:{1}", emailInfoAttachment.strSavedPath, ex.Message));
                            }


                            ObligationFile obligationFile = new ObligationFile()
                            {
                                strFilePullPath = emailInfoAttachment.strSavedPath,
                                bEncrypt = false,
                                dirNormalTags = null,
                                dirRmsTags = null
                            };
                            emailInfoAttachment.attachInfo.lisObligationFiles.Add(obligationFile);
                            lstEmailInfo.Add(emailInfoAttachment);
                        }
                    }

                }
                return lstEmailInfo;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, ex);
                return null;
            }
        }

        private List<KeyValuePair<string, string>> GetHeaders(MailItem mailitem)
        {
            List<KeyValuePair<string, string>> lisHeaders = new List<KeyValuePair<string, string>>();
            HeaderList headers = mailitem.Message.MimeDocument.RootPart.Headers;
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if((!string.IsNullOrWhiteSpace(header.Name)) && (!string.IsNullOrWhiteSpace(header.Value)))
                    {
                        lisHeaders.Add(new KeyValuePair<string, string>(header.Name, header.Value));
                    }
                }
            }
            return lisHeaders;
        }

        //added email header value, and split it if needed.
        void AddEmailHeader( List<KeyValuePair<string, string>> lisResult, KeyValuePair<string, string> pairHeader)
        {
            string strPairKey = FormatHeader(pairHeader.Key);

            if(!string.IsNullOrWhiteSpace(Config.EmailHeaderMultiValueSplit))
            {
                //split
                string[] splits = new string[1];
                splits[0] = Config.EmailHeaderMultiValueSplit;
                string[] strValues = pairHeader.Value.Split(splits, StringSplitOptions.RemoveEmptyEntries);
                if(strValues!=null)
                {
                    foreach (string strValue in strValues)
                    {
                         try
                         {
                             lisResult.Add(new KeyValuePair<string, string>(strPairKey, strValue.Trim()));
                         }
                        catch(Exception ex)
                         {
                             CSLogger.OutputLog(LogLevel.Error, "Exception on AddEmailHeader:", ex);
                         }

                    }
                }


            }
            else
            {
                lisResult.Add(new KeyValuePair<string, string>(strPairKey, pairHeader.Value.Trim()));
            }

        }

        private List<KeyValuePair<string, string>> GetPairHeader(List<KeyValuePair<string, string>> listHeaders)
        {
            CSLogger.OutputLog(LogLevel.Debug, "Enter GetPairHeader...");
            List<KeyValuePair<string, string>> lisResult = new List<KeyValuePair<string, string>>();

            string strSupportHeader = (Config.SupportHeaderKey == null) ? "" : Config.SupportHeaderKey;
            CSLogger.OutputLog(LogLevel.Debug, "SupportHeaderKey = " + strSupportHeader);

            if (strSupportHeader.Equals("*"))
            {
                foreach (KeyValuePair<string, string> pairHeader in listHeaders)
                {
                    AddEmailHeader(lisResult, pairHeader);
                }
            }
            else
            {
                string[] strKeys = strSupportHeader.Split(ConstVariable.Char_Support_Header_Key_Split);
                CSLogger.OutputLog(LogLevel.Debug, "length of strKeys = " + strKeys.Length);

                foreach (KeyValuePair<string, string> pairHeader in listHeaders)
                {
                    foreach (string strKey in strKeys)
                    {
                         if (strKey.Equals(pairHeader.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            AddEmailHeader(lisResult, pairHeader);
                            break;
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, string> pairHeader in lisResult)
            {
                CSLogger.OutputLog(LogLevel.Debug, "pairHeader {Key=[" + pairHeader.Key + "], Value=[" + pairHeader.Value + "]}");
            }

            CSLogger.OutputLog(LogLevel.Debug, "Leave GetPairHeader...");
            return lisResult;
        }

        private List<KeyValuePair<string, string>> GetClassification(List<Nextlabs.RouteAgent.PlugIn.INLClearance> lisInstances, List<KeyValuePair<string, string>> lisHeaders, MailItem mailItem)
        {
            CSLogger.OutputLog(LogLevel.Debug, "GetClassification Start");
            List<KeyValuePair<string, string>> lisResult = new List<KeyValuePair<string, string>>();

            if (lisInstances != null)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Instances Count="+lisInstances.Count);
                foreach (Nextlabs.RouteAgent.PlugIn.INLClearance instance in lisInstances)
                {
                    List<KeyValuePair<string, string>> lisClearance=null;
                    Nextlabs.RouteAgent.PlugIn.ObJectType supportedOb;
                    try
                    {
                        Nextlabs.RouteAgent.PlugIn.HRESULT hr = instance.GetSupportObject(out supportedOb);
                        if (hr.Equals(Nextlabs.RouteAgent.PlugIn.HRESULT.NO_ERROR))
                        {
                            CSLogger.OutputLog(LogLevel.Debug, "Call Function GetSupportObject Rerturn Result " + hr);
                            switch (supportedOb)
                            {
                                case Nextlabs.RouteAgent.PlugIn.ObJectType.HeadersKeyValuePair:
                                    {
                                        CSLogger.OutputLog(LogLevel.Debug, "Object Type Is HeadersKeyValuePair");
                                        CSLogger.OutputLog(LogLevel.Debug, "Start Print Input Param");
                                        foreach(var pair in lisHeaders)
                                        {
                                            CSLogger.OutputLog(LogLevel.Debug, "Key=[" + pair .Key+ "] Value="+pair.Value);
                                        }
                                        CSLogger.OutputLog(LogLevel.Debug, "End Paint Input Param");
                                        hr = instance.GetEmailClearance(lisHeaders, out lisClearance);
                                    }
                                    break;
                                case Nextlabs.RouteAgent.PlugIn.ObJectType.MailItem:
                                    {
                                        CSLogger.OutputLog(LogLevel.Debug, "Object Type Is MailItem");
                                        CSLogger.OutputLog(LogLevel.Debug, "This Is Exchange Object, Can Not Print");
                                        hr = instance.GetEmailClearance(mailItem, out lisClearance);
                                    }
                                    break;
                                case Nextlabs.RouteAgent.PlugIn.ObJectType.XHeaderByteArry:
                                    {
                                        CSLogger.OutputLog(LogLevel.Debug, "Object Type Is XHeaderByteArry");
                                        using (MemoryStream mstream = new MemoryStream())
                                        {
                                            long count = mailItem.Message.MimeDocument.RootPart.Headers.WriteTo(mstream);
                                            mstream.Position = 0;
                                            byte[] bytes = new byte[count];
                                            mstream.Read(bytes, 0, bytes.Length);
                                            CSLogger.OutputLog(LogLevel.Debug, "Start Print Input Param");
                                            CSLogger.OutputLog(LogLevel.Debug, string.Join(",", bytes));
                                            CSLogger.OutputLog(LogLevel.Debug, "End Print Param");
                                            hr = instance.GetEmailClearance(bytes, out lisClearance);
                                        }
                                    }
                                    break;
                            }
                            if (hr.Equals(Nextlabs.RouteAgent.PlugIn.HRESULT.NO_ERROR))
                            {
                                CSLogger.OutputLog(LogLevel.Debug, "Call Function GetEmailClearance Rerturn Result " + hr);
                                if (lisClearance != null)
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "Start Print Clearance");
                                    foreach (var pair in lisClearance)
                                    {
                                        string strKey = pair.Key;
                                        string strValue = pair.Value;
                                        if (strKey != null)
                                        {
                                            strKey = FormatHeader(strKey);
                                            lisResult.Add(new KeyValuePair<string, string>(strKey, strValue));
                                            CSLogger.OutputLog(LogLevel.Debug, "key=[" + pair.Key + "] Value=[" + pair.Value + "]");
                                        }
                                    }
                                    CSLogger.OutputLog(LogLevel.Debug, "End Print Clearance");
                                }
                                else
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "Object lisClearance Is NULL , please check plugin function GetEmailClearance out parm");
                                }
                            }
                            else
                            {
                                CSLogger.OutputLog(LogLevel.Debug, "Call Function GetEmailClearance Rerturn Exception Result " + hr);
                            }
                        }
                        else
                        {
                            CSLogger.OutputLog(LogLevel.Debug, "Call Function GetSupportObject Rerturn Exception Result "+hr);
                        }
                    }
                    catch (Exception ex)
                    {
                        CSLogger.OutputLog(LogLevel.Error, ex);
                    }
                }
            }
            else
            {
                CSLogger.OutputLog(LogLevel.Debug, "Can Not Get Any Instances From PlugIn");
            }
            CSLogger.OutputLog(LogLevel.Debug, "GetClassification End");
            return lisResult;
        }

        private EmailRecipient GetSender(MailItem mailItem)
        {
            if (mailItem.Message.Sender == null && mailItem.FromAddress != null)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Mail FromAddress: " + mailItem.FromAddress.ToString());
                EmailRecipient sender = new EmailRecipient(mailItem.FromAddress.LocalPart, mailItem.FromAddress.ToString());
                return sender;
            }

            return mailItem.Message.Sender;
        }
        private bool IsSupportExtension(string strExtension)
        {
            bool bresult = false;
            List<string> lisSupport = Config.supportExtensionNames;
            if (lisSupport.Exists((dir) => { return dir.Equals(strExtension, StringComparison.OrdinalIgnoreCase); }))
            {
                bresult = true;
            }
            return bresult;

        }
        public string GetFileSuffix(string strFileName)
        {
            string strSuffix = string.Empty;
            int nPos = strFileName.LastIndexOf('.');
            if (nPos >= 0)
            {
                strSuffix = strFileName.Substring(nPos + 1);
            }
            return strSuffix;
        }

        public string GetUserSessionID(string strUserLogonName)
        {
            string strResult="s-1100";
            try
            {
                //extract domain and username
                string strDomain;
                string strUserName;
                int nPos = -1;
                if ((nPos = strUserLogonName.IndexOf('@')) > 0)
                {
                    strDomain = strUserLogonName.Substring(nPos + 1);
                    strUserName = strUserLogonName.Substring(0, nPos);
                }
                else if ((nPos = strUserLogonName.IndexOf('\\')) > 0)
                {
                    strDomain = strUserLogonName.Substring(0, nPos);
                    strUserName = strUserLogonName.Substring(nPos);
                }
                else
                {
                    CSLogger.OutputLog(LogLevel.Error, "GetUserSessionID failed with error loginname:" + strUserLogonName);
                    return string.Empty;
                }

                //get session id

                System.DirectoryServices.DirectoryEntry obDirEntry = new System.DirectoryServices.DirectoryEntry("WinNT://" + strDomain + "/" + strUserName);
                System.DirectoryServices.PropertyCollection coll = obDirEntry.Properties;
                if (coll != null)
                {
                    byte[] sidVal = coll["objectSid"].Value as byte[];
                    if (sidVal != null)
                    {
                        System.Security.Principal.SecurityIdentifier si = new System.Security.Principal.SecurityIdentifier(sidVal, 0);
                        strResult= si.ToString();
                    }
                    else
                    {
                        CSLogger.OutputLog(LogLevel.Warn, "GetUserSessionID Faild on Login Name:" + strUserLogonName + "objectSid is null");
                    }
                }
                else
                {
                    CSLogger.OutputLog(LogLevel.Warn, "GetUserSessionID Faild on Login Name:" + strUserLogonName + " PropertyCollection is null ");
                }
                return strResult;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Warn, "exception on GetUserSessionID on Login Name:" + strUserLogonName);
                return strResult;
            }
        }
        public static string GetCilentType(string strMsgId)
        {
            if (Common.Config.SupportClientType.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ReadLogHelp rl = new ReadLogHelp();
                    return rl.GetClientType(strMsgId);
                }
                catch(Exception ex)
                {
                    CSLogger.OutputLog(LogLevel.Warn, "exception on GetCilentType:" + strMsgId);
                    return "";
                }

            }
            else
            {
                return "";
            }
        }
        private bool CheckWhiteList(string strFromAddress, List<KeyValuePair<string, string>> lisHeaders)
        {
            bool bresult = false;

            whiteListSection whiteList = Common.Config.GetSection<whiteListSection>(Common.ConstVariable.Str_Configuration_Section_Name_WhiteList);
            if (whiteList != null)
            {
                if (CheckInSenderList(strFromAddress, whiteList.SenderListSetting))
                {
                    bresult = true;
                }
                else
                {
                    if (CheckInHeaderList(lisHeaders, whiteList.HeaderListSetting))
                    {
                        bresult = true;
                    }
                }
            }

            return bresult;
        }

        private bool CheckInSenderList(string strFromAddesss, senderListSection senderList)
        {
            bool bresult = false;
            if (senderList != null)
            {
                foreach (var p in senderList)
                {
                    addressSection address = (addressSection)p;
                    if (address != null)
                    {
                        if (strFromAddesss.Equals(address.Value,StringComparison.OrdinalIgnoreCase))
                        {
                            bresult = true;
                            break;
                        }
                    }
                }
            }
            return bresult;
        }

        private bool CheckInHeaderList(List<KeyValuePair<string, string>> lisHeader, headerListSection headerList)
        {
            string[] whiteHeaderValue = {Common.ConstVariable.Str_MailClassify_DenyNotiy, Common.ConstVariable.Str_MailClassify_Enforced};
            bool bresult = false;
            foreach (KeyValuePair<string, string> keyValyeHeader in lisHeader)
            {
                if (keyValyeHeader.Key.Equals(Common.ConstVariable.Str_NextlabsHeader_Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (whiteHeaderValue.Contains<string>(keyValyeHeader.Value, StringComparer.OrdinalIgnoreCase))
                    {
                        bresult = true;
                    }
                }
            }
            if (!bresult)
            {
                if (headerList != null)
                {
                    foreach (var p in headerList)
                    {
                        headerSection sectionHeader = (headerSection)p;
                        foreach (KeyValuePair<string, string> keyValyeHeader in lisHeader)
                        {
                            if (sectionHeader.Name.Equals(keyValyeHeader.Key, StringComparison.OrdinalIgnoreCase))
                            {
                                if (sectionHeader.Value.Equals(keyValyeHeader.Value, StringComparison.OrdinalIgnoreCase))
                                {
                                    bresult = true;
                                    break;
                                }
                            }
                        }
                        if (bresult)
                        {
                            break;
                        }
                    }
                }
            }
            return bresult;
        }

    }
}
