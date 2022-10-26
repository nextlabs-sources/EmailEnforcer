using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net.Repository.Hierarchy;
using Microsoft.Exchange.Data.Mime;
using Microsoft.Exchange.Data.Transport;
using Microsoft.Exchange.Data.Transport.Email;
using SDKWrapperLib;

using RouteAgent.Plugin;
using CSBase.Diagnose;
using System.DirectoryServices;

namespace RouteAgent.Common
{
    public class EmailEvalInfoManage
    {
        public EmailEvalInfoManage()
        {

        }

        private string m_strClientType = string.Empty;
        public string ClientType
        {
            get { return m_strClientType; }
            set { m_strClientType = value; }
        }
        private List<RouteAgent.Common.EmailInfo> m_listEmailInfo = null;
        public List<RouteAgent.Common.EmailInfo> EmailInfos
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

        private string m_strMailClassification = RouteAgent.Common.ConstVariable.Str_MailClassify_MAPIMsgClsSubmitLamProbe;
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

        private List<RouteAgent.Common.GroupInfo> m_lisGropuInfo = null;
        public List<RouteAgent.Common.GroupInfo> GroupInfos
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
            string[] arryStrNoProcessMsgCls = { RouteAgent.Common.ConstVariable.Str_MailClassify_MAPIMsgClsSubmitLamProbe, RouteAgent.Common.ConstVariable.Str_MailClassify_MAPIMsgClsReportNoteNDR };
            m_MapiClass = mailItem.Message.MapiMessageClass;

            if (!arryStrNoProcessMsgCls.Contains<string>(m_MapiClass, StringComparer.OrdinalIgnoreCase))
            {
                // 将邮件里面的发送信息解析出来，放入一个新生成的EmailRecipient对象
                m_Sender = GetSender(mailItem);
                if (m_Sender != null)
                {
                    //  去exchangepep.xml 找到白名单
                    whiteListSection whiteList = RouteAgent.Common.Config.GetSection<whiteListSection>(RouteAgent.Common.ConstVariable.Str_Configuration_Section_Name_WhiteList);
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
                                PlugInManager obPlugInManagerIns = PlugInManager.GetInstance();
                                List<INLEmailParser> lsEmailParserPlugin = obPlugInManagerIns.GetPlugins<INLEmailParser>();
                                m_lisPairClassification = GetClassification(lsEmailParserPlugin, m_lisHeaders, mailItem);

                                // 通过server的AcceptedDomains找Sender的域名，如果找到，这个邮件是OutMail， 如果找不到是InMail
                                m_strAction = GetEvaluationAction(server, mailItem);

                                List<INLUserParser> lsUserParserPlugin = obPlugInManagerIns.GetPlugins<INLUserParser>();
                                m_strSendSid = CommonPluginTools.GetUserSecurityIDByEmailAddressByUserParserPlugin(lsUserParserPlugin, Sender.SmtpAddress, true, "s-1100");

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
                if (null == message)
				{
                    CSLogger.OutputLog(LogLevel.Error, "Try to get mail classify info but the email message object is null", null);
                }
                else
				{
					//no need process mapi message type
					string[] strNoProcessMsgCls = { RouteAgent.Common.ConstVariable.Str_MailClassify_MAPIMsgClsSubmitLamProbe, "Report.IPM.Note.NDR" };
					foreach (string strMsgCls in strNoProcessMsgCls)
					{
						if (strMsgCls.Equals(message.MapiMessageClass, StringComparison.OrdinalIgnoreCase))
						{
							strMailClassify = RouteAgent.Common.ConstVariable.Str_MailClassify_MAPIMsgClsSubmitLamProbe;
							break;
						}
					}

					if (string.IsNullOrEmpty(strMailClassify))
					{
						List<string> lisNextlabsHeaderVals = new List<string>()
					{
						RouteAgent.Common.ConstVariable.Str_MailClassify_DenyNotiy
					};

						foreach (var header in message.MimeDocument.RootPart.Headers)
						{
							if (header.Name.Equals(RouteAgent.Common.ConstVariable.Str_NextlabsHeader_Key, StringComparison.OrdinalIgnoreCase))
							{
								if (lisNextlabsHeaderVals.Exists(dir => { return dir.Equals(header.Value, StringComparison.OrdinalIgnoreCase); }))
								{
									strMailClassify = header.Value;
									break;
								}
							}
						}
					}
				}
                return strMailClassify;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during get mail classify info, messageID:[{0}]", new object[] { message.MessageId }, ex);
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
                            RouteAgent.Common.GroupInfo groupInfo = new GroupInfo(recipient, book.RecipientType, recipient.Address);
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
                if (null == message)
				{
					CSLogger.OutputLog(LogLevel.Error, "Try to save mail info but the email message object is null", null);
					return null;
				}

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
                emailInfoSubject.strContentType = RouteAgent.Common.Policy.m_strContentTypeEmailSubject;
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
                    strContentType = RouteAgent.Common.Policy.m_strContentTypeEmailBody,
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
                        ++indexAttach;

                        AttachInfo obCurAttachInfo = EstablishAttachmentInfo(attachment, indexAttach, strOutDir);
                        if (null == obCurAttachInfo)
                        {
                            CSLogger.OutputLog(LogLevel.Debug, "Establish an empty attachment info for acttachment:[{0}], if this attachment need support, please check the code\n", new object[] { attachment.FileName });
                        }
                        else
                        {
                            EmailInfo emailInfoAttachment = new EmailInfo()
                            {
                                strContentType = Policy.m_strContentTypeEmailAttachment,
                                strSavedPath = obCurAttachInfo.strAttachSavedFileFullPath,
                                strName = attachment.FileName,
                                attachInfo = obCurAttachInfo
                            };
                            lstEmailInfo.Add(emailInfoAttachment);
                        }
                    }
                }
                return lstEmailInfo;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during save mail info, messageID:[{0}], out dir:[{1}]", new object[] { message.MessageId, strOutDir }, ex);
            }
            return null;
        }

        private AttachInfo EstablishAttachmentInfo(Attachment obAttachment, int nAttachIndex, string strOutDir)
        {
            if (null == obAttachment)
            {
                CSLogger.OutputLog(LogLevel.Error, "The pass in attachment object is null when we try to establish the attachment info, please check, this maybe a code error\n");
                return null;
            }

            string strAttachmentFileFullPath = strOutDir + ConstVariable.Str_EmailFile_Prefix_Attach + obAttachment.FileName;
            AttachInfo obAttachInfoRet = EstablishBaseAttachementInfo(obAttachment, nAttachIndex, strAttachmentFileFullPath);

            bool bAttachmentAlreadySavedToLocalDrive = false;

            // Plugin attachment parser
            PlugInManager obPlugInManagerIns = PlugInManager.GetInstance();
            List<INLAttachmentParser> lsAttachmentParser = obPlugInManagerIns.GetPlugins<INLAttachmentParser>();
            if (null != lsAttachmentParser)
            {
                foreach (INLAttachmentParser obNLAttachmentParser in lsAttachmentParser)
                {
                    bool bNeedSaveToLoaclDrive = false;
                    bool bSupport = obNLAttachmentParser.IsSupportParseClassificationInfo(obAttachment, out bNeedSaveToLoaclDrive);
                    if (bSupport)
                    {
                        if (bNeedSaveToLoaclDrive)
                        {
                            if (!bAttachmentAlreadySavedToLocalDrive)
                            {
                                bAttachmentAlreadySavedToLocalDrive = SaveAttachmentToSpecifyFile(obAttachment, strAttachmentFileFullPath);
                                if (!bAttachmentAlreadySavedToLocalDrive)
                                {
                                    CSLogger.OutputLog(LogLevel.Error, "Attachment:[{0}] save to local driver:[{1}] failed\n", new object[] { obAttachment.FileName, strAttachmentFileFullPath });
                                }
                            }
                        }
                        if (bNeedSaveToLoaclDrive && (!bAttachmentAlreadySavedToLocalDrive))
                        {
                            CSLogger.OutputLog(LogLevel.Error, "Can not continue to invoke the attachment parser:[{0}]\n", new object[] { obNLAttachmentParser });
                        }
                        else
                        {
                            List<KeyValuePair<string, string>> lsFileTags = obNLAttachmentParser.GetAttachmentClassificationInfo(obAttachment, bAttachmentAlreadySavedToLocalDrive ? strAttachmentFileFullPath : "");
                            obAttachInfoRet.AddClassificationInfo(lsFileTags);
                        }
                    }
                }
            }

            // Common parser
            // In fact the pluging attachment parser and the common need manager unified
            string strExtension = GetFileSuffix(strAttachmentFileFullPath);
            if (IsSupportExtension(strExtension))
            {
                if (!bAttachmentAlreadySavedToLocalDrive)
                {
                    bAttachmentAlreadySavedToLocalDrive = SaveAttachmentToSpecifyFile(obAttachment, strAttachmentFileFullPath);
                }
                if (bAttachmentAlreadySavedToLocalDrive)
                {
                    List<KeyValuePair<string, string>> lsFileTags = GetCommonAttachmentClassificationInfo(strAttachmentFileFullPath);
                    obAttachInfoRet.AddClassificationInfo(lsFileTags);
                }
            }
            else
            {
                CSLogger.OutputLog(LogLevel.Debug, "Current attachment named:[{0},{1}] do not support by support extensions config\n", new object[] { obAttachment.FileName, strExtension });
            }
            return obAttachInfoRet;
        }
        private List<KeyValuePair<string, string>> GetCommonAttachmentClassificationInfo(string strAttachmentFileFullPath)
        {
            //read file tag.
            FileType emFileType = RouteAgent.Common.Function.GetFileType(strAttachmentFileFullPath);
            List<KeyValuePair<string, string>> lsFileTags = ReadFileTags(strAttachmentFileFullPath, (FileType.Nextlabs == emFileType));
            return lsFileTags;
        }
        private AttachInfo EstablishBaseAttachementInfo(Attachment obAttachment, int nAttachIndex, string strAttachmentFileFullPath)
        {
            return new AttachInfo()
            {
                attach = obAttachment,
                lisObligationFiles = new List<ObligationFile>()
                    {
                        new ObligationFile()
                        {
                            strFilePullPath = strAttachmentFileFullPath,
                            bEncrypt = false,
                            dirNormalTags = null,
                            dirRmsTags = null
                        }
                    },
                strAttrachContentType = obAttachment.ContentType,
                strAttachName = obAttachment.FileName,
                strAttachSavedFileFullPath = strAttachmentFileFullPath,
                embeddedMessage = obAttachment.EmbeddedMessage,
                index = nAttachIndex,
                listTags = new List<KeyValuePair<string, string>>()
            };
        }

        static public bool SaveAttachmentToSpecifyFile(Attachment obAttachment, string strAttachmentFileFullPath)
        {
            bool bRet = false;
            try
            {
                bRet = SaveStreamToSpecifyFile(obAttachment.GetContentReadStream(), strAttachmentFileFullPath);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Save file stream:[{0}] to specify file:[{1}] failed with exception:[{2}]\n",
                    new object[] { obAttachment, strAttachmentFileFullPath, ex.Message });
            }
            return bRet;
        }
        static private bool SaveStreamToSpecifyFile(Stream obStreamAttachment, string strAttachmentFileFullPath)
        {
            bool bRet = false;
            try
            {
                obStreamAttachment.Seek(0, SeekOrigin.Begin);
                using (FileStream fileAttachment = new FileStream(strAttachmentFileFullPath, FileMode.Create))
                {
                    byte[] byteAttachment = new byte[ConstVariable.Int_ReadLenOneTimeAttach];
                    while (true)
                    {
                        int nReadLen = obStreamAttachment.Read(byteAttachment, 0, ConstVariable.Int_ReadLenOneTimeAttach);

                        fileAttachment.Write(byteAttachment, 0, nReadLen);

                        if (nReadLen < ConstVariable.Int_ReadLenOneTimeAttach)//means the file is end
                        {
                            break;
                        }
                    }
                }
                bRet = true;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Save file stream:[{0}] to specify file:[{1}] failed with exception:[{2}]\n",
                    new object[] { obStreamAttachment, strAttachmentFileFullPath, ex.Message });
            }
            return bRet;
        }
        static private List<KeyValuePair<string,string>> ReadFileTags(string strAttachmentFileFullPath, bool bIsNxlFile)
        {
            List<KeyValuePair<string, string>> lsTagRet = new List<KeyValuePair<string, string>>();
            if (bIsNxlFile)
            {
                lsTagRet = ReadNxlFileTags(strAttachmentFileFullPath);
            }
            else
            {
                lsTagRet = ReadNoramlFileTags(strAttachmentFileFullPath);
            }
            return lsTagRet;
        }
        static private List<KeyValuePair<string, string>> ReadNxlFileTags(string strAttachmentFileFullPath)
        {
            List<KeyValuePair<string, string>> lsTagRet = new List<KeyValuePair<string, string>>();
            try
            {
                INLRightsManager nlRManager = new NLRightsManager();
                int lCount = 0;
                nlRManager.NLGetTagsCount(strAttachmentFileFullPath, out lCount);
                CSLogger.OutputLog(LogLevel.Info, string.Format("NLGetTagsCount: {0} count:{1}", strAttachmentFileFullPath, lCount));
                for (int i = 0; i < lCount; i++)
                {
                    string strName = "";
                    string strValue = "";
                    nlRManager.NLReadTags(strAttachmentFileFullPath, i, out strName, out strValue);
                    CSLogger.OutputLog(LogLevel.Info, string.Format("Read file tag:{0}, value: {1}={2}", strAttachmentFileFullPath, strName, strValue));
                    if ((!string.IsNullOrWhiteSpace(strName)) && (!string.IsNullOrWhiteSpace(strValue)))
                    {
                        lsTagRet.Add(new KeyValuePair<string, string>(strName, strValue));
                    }
                    else
                    {
                        CSLogger.OutputLog(LogLevel.Info, "ignore this nxl tag");
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, string.Format("Read file tag exception, file:{0}, message:{1}", strAttachmentFileFullPath, ex.Message));
            }
            return lsTagRet;
        }
        static private List<KeyValuePair<string, string>> ReadNoramlFileTags(string strAttachmentFileFullPath)
        {
            List<KeyValuePair<string, string>> lsTagRet = new List<KeyValuePair<string, string>>();
            try
            {
                FileTagManager tagMgr = new FileTagManager();
                int lCount = 0;
                tagMgr.GetTagsCount(strAttachmentFileFullPath, out lCount);
                CSLogger.OutputLog(LogLevel.Info, string.Format("Read file tag: {0} count:{1}", strAttachmentFileFullPath, lCount));
                for (int i = 0; i < lCount; i++)
                {
                    string strName = "";
                    string strValue = "";
                    tagMgr.GetTagByIndex(strAttachmentFileFullPath, i, out strName, out strValue);
                    CSLogger.OutputLog(LogLevel.Info, string.Format("Read file tag:{0}, value: {1}={2}", strAttachmentFileFullPath, strName, strValue));
                    if ((!string.IsNullOrWhiteSpace(strName)) && (!string.IsNullOrWhiteSpace(strValue)))
                    {
                        lsTagRet.Add(new KeyValuePair<string, string>(strName, strValue));
                    }
                    else
                    {
                        CSLogger.OutputLog(LogLevel.Info, "ignore this tag");
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, string.Format("Read file tag exception, file:{0}, message:{1}", strAttachmentFileFullPath, ex.Message));
            }
            return lsTagRet;
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
                             CSLogger.OutputLog(LogLevel.Error, "Exception on AddEmailHeader:", null, ex);
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

        private List<KeyValuePair<string, string>> GetClassification(List<RouteAgent.Plugin.INLEmailParser> lsRouteAgentPlugins, List<KeyValuePair<string, string>> lisHeaders, MailItem mailItem)
        {
            CSLogger.OutputLog(LogLevel.Debug, "GetClassification Start");
            List<KeyValuePair<string, string>> lsClassifyInfo = new List<KeyValuePair<string, string>>();

            if (lsRouteAgentPlugins != null)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Instances Count="+ lsRouteAgentPlugins.Count);
                foreach (RouteAgent.Plugin.INLEmailParser obNLRouteAgentPluginEntry in lsRouteAgentPlugins)
                {
                    try
                    {
                        obNLRouteAgentPluginEntry.AdjustClassificationInfo(lisHeaders, mailItem, ref lsClassifyInfo);
                    }
                    catch (Exception ex)
                    {
                        CSLogger.OutputLog(LogLevel.Error, "Exception during get classification from plugin module", null, ex);
                    }
                }
            }
            else
            {
                CSLogger.OutputLog(LogLevel.Debug, "Can Not Get Any Instances From PlugIn");
            }
            CSLogger.OutputLog(LogLevel.Debug, "GetClassification End");
            return lsClassifyInfo;
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
            bool bResult = false;
            if (!String.IsNullOrEmpty(strExtension))
            {
                HashSet<string> setSupport = Config.supportExtensionNames;
                if (null == setSupport)
                {
                    bResult = false;
                }
                else if ((setSupport.Contains(Config.g_kstrValue_Star)) || (setSupport.Contains(strExtension)))
                {
                    bResult = true;
                }
            }
            return bResult;

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

        public string GetUserSessionIDByEamilAddress(string strUserEmailAddress)
        {
            // User login name 和 user email address 可以不一样, login name 和 eamil address 都可以用于收发邮件. BUG 37482
            // AD 和 Exchange ecp 中都可以指定 不同的 email address
            // 新的 email address 相较于 login name 称为 别名 alias
            // 当 alias 同 其他的 login name 或者 alias 相冲突时, email address 中自动添加序号, 但 alias 不变

            string strSIDRet = "s-1100";
            try
            {
                DirectorySearcher dirSearcher = new DirectorySearcher();
				dirSearcher.Filter = String.Format("(&(objectClass=user)(mail={0}))", strUserEmailAddress);
                dirSearcher.PropertiesToLoad.Add("objectSid");

                SearchResultCollection obSearchCollection = dirSearcher.FindAll();
				foreach (SearchResult obSearchResult in obSearchCollection)
				{
                    CSLogger.OutputLog(LogLevel.Debug, "Search result:[{0}]\n", new object[] { obSearchResult.Path });
				}
			}
            catch (Exception ex)
			{
                CSLogger.OutputLog(LogLevel.Error, "Exception on GetUserSessionID on Login Name:{0}", new object[] { strUserEmailAddress }, ex);
            }
            return strSIDRet;
        }

        public static string GetCilentType(string strMsgId)
        {
            if (RouteAgent.Common.Config.SupportClientType.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    TransportMessageTraceLogReader rl = new TransportMessageTraceLogReader();
                    return rl.GetClientType(strMsgId);
                }
                catch(Exception ex)
                {
                    CSLogger.OutputLog(LogLevel.Warn, String.Format("exception on GetCilentType:{0}, ExceptionInfo:{1}", strMsgId, ex.Message));
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

            whiteListSection whiteList = RouteAgent.Common.Config.GetSection<whiteListSection>(RouteAgent.Common.ConstVariable.Str_Configuration_Section_Name_WhiteList);
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
            string[] whiteHeaderValue = {RouteAgent.Common.ConstVariable.Str_MailClassify_DenyNotiy, RouteAgent.Common.ConstVariable.Str_MailClassify_Enforced};
            bool bresult = false;
            foreach (KeyValuePair<string, string> keyValyeHeader in lisHeader)
            {
                if (keyValyeHeader.Key.Equals(RouteAgent.Common.ConstVariable.Str_NextlabsHeader_Key, StringComparison.OrdinalIgnoreCase))
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
