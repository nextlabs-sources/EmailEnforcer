using CSBase.Common;

using DocumentFormat.OpenXml.InkML;
using Microsoft.Exchange.Data.Transport;
using Microsoft.Exchange.Data.Transport.Email;
using Newtonsoft.Json;
using Nextlabs.TDFFileAnalyser;

using CSBase.Diagnose;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using System.Xml;
using System.Xml.Serialization;

namespace SFBEnforcerPlugin
{
    public class SFBEnforcerEEPlugin : /*RouteAgent.Plugin.INLUserParser,*/ RouteAgent.Plugin.INLEmailParser, RouteAgent.Plugin.INLAttachmentParser
    {
        #region Const/Read only Values: Common
        private const string strDenyResult = "Enforce_Deny";
        private const string strSubjectType = "Email Subject";
        private const string strBodyType = "Email Body";
        private const string strDenyBehaviro = "deny";

        private const string strLackOfInfo = "There is some necessary info were lacked,we don not protect this email!";
        private const string strTimeOutInfo = "Server is busy,the Exchange Enforcer was time out!";
        private const string strResourceKey = "jsonheader";

        private const string g_kstrSepInvitee = ",";
        private const string kstrUserAgent_Plugin = "SFBEnforcerPlugin";
        #endregion

        #region Const/Read only values: Web method, WMQueryPolicyForMeetingInvite
        private const string kstrMethodName_WMQueryPolicyForMeetingInvite = "WMQueryPolicyForMeetingInviteEx";

        private const string kstrMethodParam_StrMeetingIdentify = "strMeetingIdentify";
        private const string kstrMethodParam_StrInviter = "strInviter";
        private const string kstrMethodParam_StrInvitees = "strInvitees";
        private const string kstrMethodParam_StrSepInvitee = "strSepInvitee";
        private const string kstrMethodParam_StrNeedDoObligations = "strNeedDoObligations";
        #endregion

        #region Const/Read only values: Web method, WMGetQueryPolicyForMeetingResult
        private const string kstrMethodName_WMGetQueryPolicyForMeetingResult = "WMGetQueryPolicyForMeetingResult";

        private const string kstrMethodParam_strQueryIdentify = "strQueryIdentify";
        #endregion

        #region Constructor
        static SFBEnforcerEEPlugin()
        {
            //get current Exchange Enforcer install path
            string strEEInstallPath = RouteAgent.Common.Function.GetExchangeEnforcerInstallPath();
            strEEInstallPath = FileTools.GetStandardFolderPath(strEEInstallPath);
            LoadConfig(strEEInstallPath);
        }
        #endregion

        #region Implement interface: INPluginRoot
        public void Init(Type tyInterface)
        {
        }
        public void Uninit(Type tyInterface)
        {
        }
        #endregion

        #region Implement interface: INLEmailParser
        #region Events
        public void PreEvaluation(MailItem obMailItem, RouteAgent.Common.EmailEvalInfoManage obEmailEvalInfoMgr, SmtpServer obSmtpServer)
        {
            // Parameters check
            if ((null == obMailItem) ||
                (null == obEmailEvalInfoMgr) || (null == obEmailEvalInfoMgr.Sender) || (null == obEmailEvalInfoMgr.EmailInfos) || (0 == obEmailEvalInfoMgr.EmailInfos.Count) ||
                (null == obSmtpServer)
              )
            {
                CSLogger.OutputLog(LogLevel.Debug, "The pre-evaluation parameters error, the mail object, mail manager, or SMTP server is invalid\n");

            }
            else
            {
                HashSet<string> setMeetingUrl = GetMeetingUrl(obEmailEvalInfoMgr, SFBEnforcerPluginConfig.GetInstance().IsSupportEvaMulMeetingInAnEmail);
                if (null == setMeetingUrl)
                {
                    CSLogger.OutputLog(LogLevel.Debug, "There is no meeting entry info in the email, ignore meeting invite evaluation\n");
                }
                else
                {
                    EmailRecipient obSender = obEmailEvalInfoMgr.Sender;
                    foreach (string strMeetingUrl in setMeetingUrl)
                    {
                        List<string> lsRecipients = RouteAgent.Common.Function.GetAllRecipientsToStr(obMailItem);
                        if ((null == lsRecipients) || (0 >= lsRecipients.Count))
                        {
                            CSLogger.OutputLog(LogLevel.Info, String.Format("The email recipients is empyty during check email-meeting:[{0}], maybe no recipients need continue to do evaluation, they are have beny denied\n", strMeetingUrl));
                        }
                        else
                        {
                            DoEvaluationForEmailMeetingInvite(obMailItem, obSender, lsRecipients, strMeetingUrl, obSmtpServer);
                        }
                    }
                }
            }
        }
        public void AdjustClassificationInfo(List<KeyValuePair<string, string>> lsHeaders, MailItem obMailItem, ref List<KeyValuePair<string, string>> lsClassificatioInfo)
        {
            SFBEnforcerPluginConfig obSFBEnforcerPluginConfigIns = SFBEnforcerPluginConfig.GetInstance();
            if (obSFBEnforcerPluginConfigIns.NeedCombineTDFAttachmentXHeader)
            {
                foreach (var attachFile in obMailItem.Message.Attachments)
                {
                    string strJsonHeader = GetTDFHeaderInfoFromAttachment(attachFile);
                    if (!string.IsNullOrEmpty(strJsonHeader))
                    {
                        // Note: make sure all json info as lower case
                        var dic = new KeyValuePair<string, string>(strResourceKey, strJsonHeader.ToLower());

                        if (null == lsClassificatioInfo)
                        {
                            lsClassificatioInfo = new List<KeyValuePair<string, string>>();
                        }
                        lsClassificatioInfo.Add(dic);
                    }
                }
            }
            else
            {
                CSLogger.OutputLog(LogLevel.Debug, "Current no need combine TDF attachment xheader info as an email attribute, this will be handle by each attachment\n");
            }
        }
        #endregion
        #endregion

        #region Implement interface: INLAttachmentParser
        public bool IsSupportParseClassificationInfo(Attachment obAttachment, out bool bIsNeedSaveAttachmentAsLocalFileOut)
        {
            // Init out parameters
            bIsNeedSaveAttachmentAsLocalFileOut = false;

            bool bRetSupport = false;
            SFBEnforcerPluginConfig obSFBEnforcerPluginConfigIns = SFBEnforcerPluginConfig.GetInstance();
            if (obSFBEnforcerPluginConfigIns.NeedCombineTDFAttachmentXHeader)
            {
                // Combine TDF xheader, no need process for each attachment
                // This case will be handled by INLEmailParser in AdjustClassificationInfo method
                bRetSupport = false;
            }
            else
            {
                if (null != obAttachment)
                {
                    TDFXHeaderExtralConfig obTDFXHeaderExtralConfig = TDFXHeaderExtralConfig.GetInstance();
                    bRetSupport = obTDFXHeaderExtralConfig.IsSupportFile(obAttachment.FileName);
                }
            }
            return bRetSupport;
        }
        /// <summary>
        /// Get the attachment classification info
        /// </summary>
        /// <param name="obAttachment">the attachment object</param>
        /// <param name="strAttachmentLocalFilePath">if this file path is not empty, it specify a local file path which contains the attachment content</param>
        /// <returns></returns>
        public List<KeyValuePair<string, string>> GetAttachmentClassificationInfo(Attachment obAttachment, string strAttachmentLocalFilePath)
        {
            List<KeyValuePair<string, string>> lsClassificatioInfoRet = null;
            string strJsonHeader = GetTDFHeaderInfoFromAttachment(obAttachment);
            if (!string.IsNullOrEmpty(strJsonHeader))
            {
                lsClassificatioInfoRet = new List<KeyValuePair<string, string>>();

                // Note: make sure all json info as lower case
                KeyValuePair<string, string> pairJsonHeader = new KeyValuePair<string, string>(strResourceKey, strJsonHeader.ToLower());
                lsClassificatioInfoRet.Add(pairJsonHeader);
            }
            return lsClassificatioInfoRet;
        }
        #endregion

        #region Load config info
        private static void LoadConfig(string strEEInstallPath)
        {
            try
            {
                string configPath = strEEInstallPath + @"config\SFBEnforcerPluginConfig.xml";
                CSLogger.OutputLog(LogLevel.Debug, "configPath:" + configPath);

                SFBEnforcerPluginConfig.GetInstance().InitFromFile(configPath);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during LoadConfig info from file:[{0}]", new object[] { strEEInstallPath }, ex);
            }
        }

        /// <summary>
        /// onece config file changed,we update the Configuration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                string strEEInstallPath = RouteAgent.Common.Function.GetExchangeEnforcerInstallPath();
                LoadConfig(strEEInstallPath);
            }
        }
        #endregion

        #region SFBMeeting Policy and Obligations
        private void DoEvaluationForEmailMeetingInvite(MailItem obMailItem, EmailRecipient obSender, List<string> lsRecipients, string strMeetingUrl, SmtpServer obSmtpServer)
        {
            SFBServiceQueryResult obQueryResult = null;
            try
            {
                if ((null == obSender) || (null == lsRecipients) || (0 >= lsRecipients.Count) || String.IsNullOrEmpty(strMeetingUrl))
                {
                    CSLogger.OutputLog(LogLevel.Debug, String.Format("One of the query necessary info is empty, ignore query. sender:[{0}], recipients:{1}, meetingUrl:[{2}]\n", obSender, lsRecipients, strMeetingUrl));
                }
                else
                {
                    obQueryResult = CheckPolicyForEmailMeetingInvite(strMeetingUrl, obSender, lsRecipients);
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, String.Format("Exception during check policy for email meeting invite, message:[{0}]\n\tstack:[{1}]\n", ex.Message, ex.StackTrace));
                RouteAgent.Common.Function.DoExceptionNotify(strLackOfInfo, obSmtpServer, obMailItem);

                obQueryResult = new SFBServiceQueryResult(SFBServiceQueryResult.OPERATION_FAILED, null, null);
            }
            if (null != obQueryResult)
            {
                ProcessQueryResults(obQueryResult, obMailItem, lsRecipients, obSender, strMeetingUrl, obSmtpServer);
            }
        }
        private SFBServiceQueryResult CheckPolicyForEmailMeetingInvite(string strMeetingUrl, EmailRecipient Sender, List<string> lisRecipients)
        {
            SFBServiceQueryResult obQueryResult = InvokeWMQueryPolicyForMeetingInvite(strMeetingUrl, Sender.NativeAddress, lisRecipients, false);
            if (null == obQueryResult)
            {
                CSLogger.OutputLog(LogLevel.Debug, "The response query result object of query policy for meeting invite is null, please check\n");
                obQueryResult = new SFBServiceQueryResult(SFBServiceQueryResult.OPERATION_FAILED, null, null);
            }
            else
            {
                if (obQueryResult.ResultCode == SFBServiceQueryResult.OPERATION_PROCESSING)
                {
                    obQueryResult = LoopWaitForQueryResponse(obQueryResult.QueryIdentify, lisRecipients.Count);
                }
            }
            return obQueryResult;

        }
        private bool ProcessQueryResults(SFBServiceQueryResult obQueryResult, MailItem obMailItem, List<string> lisRecipients, EmailRecipient Sender, string strMeetingUrl, SmtpServer Server)
        {
            // Parameters check
            if (null == obQueryResult)
            {
                CSLogger.OutputLog(LogLevel.Debug, "The policy result is null, no need process, ignore\n");
                return false;
            }

            bool bRet = true;
            switch (obQueryResult.ResultCode)
            {
                case SFBServiceQueryResult.OPERATION_SUCCEED:
                    {
                        DenyRecipients(obMailItem, obQueryResult, Server);
                        break;
                    }
                case SFBServiceQueryResult.OPERATION_FAILED:
                    {
                        // Default policy config
                        ExecuteDefaultBehavior(obMailItem);
                        break;
                    }
                case SFBServiceQueryResult.OPERATION_NoManualClassify:
                    {
                        EmailRecipient emailRecipients = new EmailRecipient(Sender.NativeAddress, Sender.SmtpAddress);
                        string notifyBody = SFBEnforcerPluginConfig.GetInstance().DenyNotifyBodyWithNoClassification + "\nEmail subject:" + obMailItem.Message.Subject + "\nmeeting urL:" + strMeetingUrl;
                        RouteAgent.Common.Function.SendEmail(obMailItem, Sender, new List<EmailRecipient> { emailRecipients }, SFBEnforcerPluginConfig.GetInstance().DenyNotifySubject, notifyBody, SFBEnforcerPluginConfig.GetInstance().DenyNotifyAttachOriginEmail, Server);
                        DenyAllRecipients(obMailItem);
                        break;
                    }
                case SFBServiceQueryResult.OPERATION_MeetingNotExist:
                    {
                        CSLogger.OutputLog(LogLevel.Error, String.Format("The meeting:[{0}] not exist, maybe the email content extract not correct, please check\n", strMeetingUrl));
                        break;
                    }
                default:
                    {
                        CSLogger.OutputLog(LogLevel.Error, "The policy result code:[%d] and do not support in current context, ignore\n");
                        bRet = false;
                        break;
                    }
            }
            return bRet;
        }
        private SFBServiceQueryResult LoopWaitForQueryResponse(string strQueryIdentify, int nRecipientsCount)
        {
            SFBServiceQueryResult obQueryResult = null;

            SFBEnforcerPluginConfig obSFBEnforcerPluginConfigIns = SFBEnforcerPluginConfig.GetInstance();

            int nHeartBeat = obSFBEnforcerPluginConfigIns.ResponseCheckHeartBeatMs;
            int nMaxWaitTime = obSFBEnforcerPluginConfigIns.SingleRequestMaxTimeMs;
            DateTime dtStartTime = DateTime.Now;

            for (int i = 0; true; ++i)
            {
                double nAlreadyCostTime = DateTime.Now.Subtract(dtStartTime).TotalMilliseconds;
                if (nAlreadyCostTime > nMaxWaitTime)
                {
                    CSLogger.OutputLog(LogLevel.Fatal, "Break wait query:[{0}] response. Cost too much time to wait the QueryPolicyForMeetingResult response", new object[] { strQueryIdentify });
                    obQueryResult = new SFBServiceQueryResult(SFBServiceQueryResult.OPERATION_FAILED, null, null);
                    break;
                }
                else
                {
                    CSLogger.OutputLog(LogLevel.Debug, "Wait:[{0}:[{1}]] for quest:[{2}] query result.\n", new object[] { i, nHeartBeat, strQueryIdentify });
                    System.Threading.Thread.Sleep(nHeartBeat);

                    obQueryResult = GetQueryResponse(strQueryIdentify);
                    if (null == obQueryResult)
                    {
                        CSLogger.OutputLog(LogLevel.Debug, "Break wait query:[{0}:{1}] response, failed to wait the response, please check\n", new object[] { i, strQueryIdentify });
                        break;
                    }
                    else
                    {
                        if (obQueryResult.ResultCode == SFBServiceQueryResult.OPERATION_PROCESSING)
                        {
                            CSLogger.OutputLog(LogLevel.Debug, "Contiune wait query:[{0}:{1}] response, success wait the response, result code:[{2}\n", new object[] { i, strQueryIdentify, obQueryResult.ResultCode });
                            break;
                        }
                        else
                        {
                            CSLogger.OutputLog(LogLevel.Debug, "Break wait query:[{0}:{1}] response, success wait the response, result code:[{2}\n", new object[] { i, strQueryIdentify, obQueryResult.ResultCode });
                        }
                    }
                }
            }
            return obQueryResult;
        }
        private SFBServiceQueryResult GetQueryResponse(string strQueryIndetify)
        {
            SFBServiceQueryResult obPolicyResults = null;
            if (!String.IsNullOrEmpty(strQueryIndetify))
            {
                obPolicyResults = InvokeWMGetQueryPolicyForMeetingResult(strQueryIndetify);
            }
            return obPolicyResults;
        }
        #endregion

        #region Webmethod invoker
        private SFBServiceQueryResult InvokeWMQueryPolicyForMeetingInvite(string strMeetingIdentify, string strInviter, List<string> lsInvitees, bool bNeedDoObligations)
        {
            SFBServiceQueryResult obSFBServiceQueryResult = null;
            try
            {
                SFBEnforcerPluginConfig obSFBEnforcerPluginConfigIns = SFBEnforcerPluginConfig.GetInstance();
                if (String.IsNullOrEmpty(obSFBEnforcerPluginConfigIns.SFBPolicyAssistantWebServiceUrl))
                {
                    CSLogger.OutputLog(LogLevel.Error, "Config file error, the SFB policy assistant web service url:[{0}] is empty\n", new object[] { obSFBEnforcerPluginConfigIns.SFBPolicyAssistantWebServiceUrl });
                }
                else
                {
                    Dictionary<string, string> dicParams = new Dictionary<string, string>() {
                        {kstrMethodParam_StrMeetingIdentify, strMeetingIdentify},
                        {kstrMethodParam_StrInviter, strInviter},
                        {kstrMethodParam_StrInvitees, String.Join(g_kstrSepInvitee, lsInvitees)},
                        {kstrMethodParam_StrSepInvitee, g_kstrSepInvitee},
                        {kstrMethodParam_StrNeedDoObligations, bNeedDoObligations.ToString()},
                    };

                    XmlDocument obXmlResponse = WebServiceTools.QueryPostWebService(obSFBEnforcerPluginConfigIns.SFBPolicyAssistantWebServiceUrl, kstrMethodName_WMQueryPolicyForMeetingInvite, dicParams, kstrUserAgent_Plugin);
                    if (null == obXmlResponse)
                    {
                        CSLogger.OutputLog(LogLevel.Error, "Response Post xml info is null, code error, please check\n");
                    }
                    else
                    {
                        CSLogger.OutputLog(LogLevel.Debug, "Response Post:[{0}]\n", new object[] { obXmlResponse.OuterXml });

                        obSFBServiceQueryResult = new SFBServiceQueryResult(obXmlResponse.InnerText);
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception in GetClassifyMeetingUrlFromResponse, [{0}]\n", new object[] { ex.Message });
            }
            return obSFBServiceQueryResult;
        }
        private SFBServiceQueryResult InvokeWMGetQueryPolicyForMeetingResult(string stQueryIdentify)
        {
            SFBServiceQueryResult obSFBServiceQueryResult = null;
            try
            {
                SFBEnforcerPluginConfig obSFBEnforcerPluginConfigIns = SFBEnforcerPluginConfig.GetInstance();
                if (String.IsNullOrEmpty(obSFBEnforcerPluginConfigIns.SFBPolicyAssistantWebServiceUrl))
                {
                    CSLogger.OutputLog(LogLevel.Error, "Config file error, the SFB policy assistant web service url:[{0}] is empty\n", new object[] { obSFBEnforcerPluginConfigIns.SFBPolicyAssistantWebServiceUrl });
                }
                else
                {
                    Dictionary<string, string> dicParams = new Dictionary<string, string>() {
                        {kstrMethodParam_strQueryIdentify, stQueryIdentify}
                    };

                    XmlDocument obXmlResponse = WebServiceTools.QueryPostWebService(obSFBEnforcerPluginConfigIns.SFBPolicyAssistantWebServiceUrl, kstrMethodName_WMGetQueryPolicyForMeetingResult, dicParams, kstrUserAgent_Plugin);
                    if (null == obXmlResponse)
                    {
                        CSLogger.OutputLog(LogLevel.Error, "Response Post xml info is null, code error, please check\n");
                    }
                    else
                    {
                        CSLogger.OutputLog(LogLevel.Debug, "Response Post:[{0}]\n", new object[] { obXmlResponse.OuterXml });

                        obSFBServiceQueryResult = new SFBServiceQueryResult(obXmlResponse.InnerText);
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception in GetClassifyMeetingUrlFromResponse, [{0}]\n", new object[] { ex.Message });
            }
            return obSFBServiceQueryResult;
        }
        #endregion

        #region TDF header
        public string GetTDFHeaderInfoFromAttachment(Attachment attachFile)
        {
            string strJsonHeader = null;
            if (null == attachFile)
            {
                CSLogger.OutputLog(LogLevel.Error, "Try to convert xml to json from attachment, but the attachment file object is null");
            }
            else
            {
                using (StreamReader reader = new StreamReader(attachFile.GetContentReadStream()))
                {
                    try
                    {
                        strJsonHeader = Nextlabs.TDFFileAnalyser.TDFXHeaderExtral.GetXmlHeaderFromTDFFileByStreamReader(reader, attachFile.FileName, true);
                    }
                    catch (Exception ex)
                    {
                        CSLogger.OutputLog(LogLevel.Error, "Exception during convert xml to json from attachment file:[{0}]", new object[] { attachFile.FileName }, ex);
                    }
                }
            }
            return strJsonHeader;
        }
        #endregion

        #region Inner independence tools
        private void ExecuteDefaultBehavior(MailItem obMailItem)
        {
            if (SFBEnforcerPluginConfig.GetInstance().IsDefaultBehaviorAllow)
            {
                CSLogger.OutputLog(LogLevel.Warn, "Default behavior is allow, nothing to do\n");
            }
            else
            {
                DenyAllRecipients(obMailItem);
            }
        }
        private void DenyAllRecipients(MailItem obMailItem)
        {
            if (null != obMailItem)
            {
                obMailItem.Recipients.Clear();
                RemoveStationeryRecipients(obMailItem);
            }
        }
        private void RemoveStationeryRecipients(MailItem mailItem, string address)
        {
            for (int i = mailItem.Message.To.Count; i > 0; i--)
            {
                if (mailItem.Message.To[i - 1].SmtpAddress.Equals(address, StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.To.Remove(mailItem.Message.To[i - 1]);
                }
            }
            for (int i = mailItem.Message.Cc.Count; i > 0; i--)
            {
                if (mailItem.Message.Cc[i - 1].SmtpAddress.Equals(address, StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.Cc.Remove(mailItem.Message.Cc[i - 1]);
                }
            }
            for (int i = mailItem.Message.Bcc.Count; i > 0; i--)
            {
                if (mailItem.Message.Bcc[i - 1].SmtpAddress.Equals(address, StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.Bcc.Remove(mailItem.Message.Bcc[i - 1]);
                }
            }
        }
        private void RemoveStationeryRecipients(MailItem mailItem)
        {
            mailItem.Message.To.Clear();
            mailItem.Message.Cc.Clear();
            mailItem.Message.Bcc.Clear();
        }
        private void DenyRecipients(MailItem obMailItem, SFBServiceQueryResult result, SmtpServer Server)
        {
            List<EmailRecipient> denyRecipient = new List<EmailRecipient>();
            EmailRecipient senderRecipient = new EmailRecipient(obMailItem.FromAddress.ToString(), obMailItem.FromAddress.ToString());
            foreach (var obPolicyResult in result.PolicyResults)
            {
                if (obPolicyResult.Enforcement.Equals(strDenyResult, StringComparison.OrdinalIgnoreCase))
                {
                    //remove user from recipient
                    var needRemoveRecipient = obMailItem.Recipients.FirstOrDefault(d => d.Address.ToString().Equals(obPolicyResult.Participant, StringComparison.OrdinalIgnoreCase));
                    //add need notify recipients to list
                    EmailRecipient emailRecipients = new EmailRecipient(needRemoveRecipient.Address.ToString(), needRemoveRecipient.Address.ToString());
                    denyRecipient.Add(emailRecipients);
                    obMailItem.Recipients.Remove(needRemoveRecipient);
                    //remove user form to cc bcc header
                    if (RouteAgent.Common.Config.RemoveRecipients)
                    {
                        RemoveStationeryRecipients(obMailItem, obPolicyResult.Participant);
                    }
                }
            }
            if (0 < denyRecipient.Count)
            {
                RouteAgent.Common.Function.SendEmail(obMailItem, senderRecipient, denyRecipient, SFBEnforcerPluginConfig.GetInstance().DenyNotifySubject, SFBEnforcerPluginConfig.GetInstance().DenyNotifyBody, SFBEnforcerPluginConfig.GetInstance().DenyNotifyAttachOriginEmail, Server);
            }
            else
            {
                CSLogger.OutputLog(LogLevel.Debug, "No recipient denied by policy\n");
            }
        }
        private HashSet<string> GetMeetingUrl(RouteAgent.Common.EmailEvalInfoManage obEmailEvalInfoMgr, bool bSupportMutipleMeetings)
        {
            HashSet<string> setMeetingUrl = new HashSet<string>();
            try
            {
                string strBodyPath = null;
                foreach (RouteAgent.Common.EmailInfo emailInfo in obEmailEvalInfoMgr.EmailInfos)
                {
                    if (emailInfo.strContentType.Equals(strBodyType, StringComparison.OrdinalIgnoreCase))
                    {
                        strBodyPath = emailInfo.strSavedPath;
                        break;
                    }
                }
                Encoding obEncoding = FileTools.GetEncoding(strBodyPath, System.Text.Encoding.ASCII);

                using (var sr = new StreamReader(strBodyPath, obEncoding))
                {
                    // Regex rg = new Regex("(https://meet.[0-9a-zA-z.-]+?/[^\\s/]+?/[0-9a-zA-z]{8})(?:[^0-9a-zA-z]|$)");
                    Regex rg = new Regex("(https://meet.[0-9a-zA-z.-]+?/[^\\s/]+?/[0-9a-zA-z]{8})(?:[^0-9a-zA-z]|$)");

                    string strBodyContent = sr.ReadToEnd();
                    if (bSupportMutipleMeetings)
                    {
                        MatchCollection obMatchCollection = rg.Matches(strBodyContent);
                        foreach (Match obMatch in obMatchCollection)
                        {
                            if (obMatch.Groups.Count == 2)
                            {
                                setMeetingUrl.Add(obMatch.Groups[1].Value);
                            }
                        }
                    }
                    else
                    {
                        Match obMatch = rg.Match(strBodyContent);
                        if (obMatch.Groups.Count == 2)
                        {
                            setMeetingUrl.Add(obMatch.Groups[1].Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during GetMeetingUrl:" + ex.Message + ex.ToString());
            }
            return setMeetingUrl;
        }
        #endregion
    }
}
