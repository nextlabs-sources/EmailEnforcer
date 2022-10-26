using DocumentFormat.OpenXml.InkML;
using Microsoft.Exchange.Data.Transport;
using Microsoft.Exchange.Data.Transport.Email;
using Newtonsoft.Json;
using SFBEnforcerPlugin.PolicyAssistant;
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
    public class SFBEnforcerEEPlugin : RouteAgent.Plugin.INLRouteAgentPluginEntry
    {
        private const string strDenyResult = "Enforce_Deny";
        private const string strSubjectType = "Email Subject";
        private const string strBodyType = "Email Body";
        private const string strDenyBehaviro = "deny";

        private const string strLackOfInfo = "There is some necessary info were lacked,we don not protect this email!";
        private const string strTimeOutInfo = "Server is busy,the Exchange Enforcer was time out!";
        private const string strResourceKey = "jsonheader";
        private static Configuration Configuration;
        #region Logger
        protected static RouteAgent.Common.CLog theLog = RouteAgent.Common.CLog.GetLogger(typeof(SFBEnforcerEEPlugin));
        #endregion

        #region Constructor
        static SFBEnforcerEEPlugin()
        {
            //get current Exchange Enforcer install path
            string strEEInstallPath = RouteAgent.Common.Function.GetExchangeEnforcerInstallPath();
            FileSystemWatcher configFileWatcher = new FileSystemWatcher();
            configFileWatcher.Path = strEEInstallPath + "config";
            configFileWatcher.Filter = "PluginConfig.xml";
            configFileWatcher.Changed += new FileSystemEventHandler(OnConfigFileChanged);
            configFileWatcher.EnableRaisingEvents = true;
            configFileWatcher.NotifyFilter = NotifyFilters.LastWrite;

            LoadConfig(strEEInstallPath);
        }
        #endregion

        #region Implement interface: INLRouteAgentPluginEntry
        #region Init
        public void Init()
        {
        }
        public void Uninit()
        {
        }
        #endregion

        #region Events
        public void PreEvaluation(MailItem obMailItem, RouteAgent.Common.EmailEvalInfoManage obEmailEvalInfoMgr, SmtpServer Server)
        {
            if(obEmailEvalInfoMgr == null || obEmailEvalInfoMgr.EmailInfos == null || obEmailEvalInfoMgr.EmailInfos.Count == 0)
            {
                CSLogger.OutputLog(LogLevel.Debug, "maile info is null,we return the code,do exceptior behavior");
                //notify admin
                RouteAgent.Common.Function.DoExceptionNotify(strLackOfInfo, Server, obMailItem);
                DoDenyAllRecipients(obMailItem);
                return;
            }

            string meetUrl = GetMeetingUrl(obEmailEvalInfoMgr);
            if (string.IsNullOrEmpty(meetUrl))
            {
                CSLogger.OutputLog(LogLevel.Warn, "meeting url is null or empty");
                return;
            }

            EmailRecipient Sender = obEmailEvalInfoMgr.Sender;
            List<string> lisRecipients = RouteAgent.Common.Function.GetAllRecipientsToStr(obMailItem);
            try
            {
                InnerDoEvaluationForEmailMeetingInvite(obMailItem, lisRecipients, Sender, meetUrl, Server);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during GetQueryResult,do exceptior behavior:", ex);
                //notify admin
                RouteAgent.Common.Function.DoExceptionNotify(ex, Server, obMailItem);
                DoDenyAllRecipients(obMailItem);
            }
        }
        public void AdjustClassificationInfo(List<KeyValuePair<string, string>> lsHeaders, MailItem obMailItem, ref List<KeyValuePair<string, string>> lsClassificatioInfo)
        {
            foreach(var attachFile in obMailItem.Message.Attachments)
            {
                string strJsonHeader = GetTDFHeaderInfoFromAttachment(attachFile);
                if (!string.IsNullOrEmpty(strJsonHeader))
                {
                    // Note: make sure all json info as lower case
                    var dic = new KeyValuePair<string, string>(strResourceKey, strJsonHeader.ToLower());
                    lsClassificatioInfo.Add(dic);
                }
            }
        }
        #endregion
        #endregion

        #region Load config info
        private static Configuration LoadConfig(string strEEInstallPath)
        {
            try
            {
                string configPath = strEEInstallPath + @"config\PluginConfig.xml";
                CSLogger.OutputLog(LogLevel.Debug, "configPath:" + configPath);
                Encoding e = GetEncoding(configPath);
                XmlSerializer xs = new XmlSerializer(typeof(Configuration));
                using (var sr = new StreamReader(configPath, e))
                {
                    Configuration = xs.Deserialize(sr) as Configuration;
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during LoadConfig:", ex);
            }
            return Configuration;
        }
        private string converToJSON(XmlDocument doc)
        {
            String json = null;
            try
            {
                json = JsonConvert.SerializeXmlNode(doc);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "JSONRAP: Json Conversion Error:", ex);
            }
            return json;
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
                var newConfiguration = LoadConfig(strEEInstallPath);
                System.Threading.Interlocked.Exchange(ref newConfiguration, Configuration);
            }
        }
        #endregion

        #region Inner independence tools
        private void DoDenyAllRecipients(MailItem obMailItem)
        {
            if (Configuration.ExceptionBehavior.Equals(strDenyBehaviro, StringComparison.OrdinalIgnoreCase))
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
        private void DenyRecipients(MailItem obMailItem, PolicyResults result, SmtpServer Server)
        {
            List<EmailRecipient> denyRecipient = new List<EmailRecipient>();
            EmailRecipient senderRecipient = new EmailRecipient(obMailItem.FromAddress.ToString(), obMailItem.FromAddress.ToString());
            foreach (var joinResult in result.JoinResult)
            {
                if (joinResult.Enforcement.Equals(strDenyResult, StringComparison.OrdinalIgnoreCase))
                {
                    //remove user from recipient
                    var needRemoveRecipient = obMailItem.Recipients.FirstOrDefault(d => d.Address.ToString().Equals(joinResult.Participant, StringComparison.OrdinalIgnoreCase));
                    //add need notify recipients to list
                    EmailRecipient emailRecipients = new EmailRecipient(needRemoveRecipient.Address.ToString(), needRemoveRecipient.Address.ToString());
                    denyRecipient.Add(emailRecipients);
                    obMailItem.Recipients.Remove(needRemoveRecipient);
                    //remove user form to cc bcc header
                    if (RouteAgent.Common.Config.RemoveRecipients)
                    {
                        RemoveStationeryRecipients(obMailItem, joinResult.Participant);
                    }
                }
            }
            RouteAgent.Common.Function.SendEmail(obMailItem, senderRecipient, denyRecipient, Configuration.DenyNotifySubject, Configuration.DenyNotifyBody, Configuration.DenyNotifyAttachOriginEmail, Server);
        }
        private static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.ASCII;
        }
        private string GetMeetingUrl(RouteAgent.Common.EmailEvalInfoManage obEmailEvalInfoMgr)
        {
            string subjectPath = null;
            string bodyPath = null;
            string meetUrl = null;
            try
            {
                foreach (RouteAgent.Common.EmailInfo emailInfo in obEmailEvalInfoMgr.EmailInfos)
                {
                    if (emailInfo.strContentType.Equals(strSubjectType, StringComparison.OrdinalIgnoreCase))
                    {
                        subjectPath = emailInfo.strSavedPath;
                    }
                    else if (emailInfo.strContentType.Equals(strBodyType, StringComparison.OrdinalIgnoreCase))
                    {
                        bodyPath = emailInfo.strSavedPath;
                    }
                }
                Encoding e = GetEncoding(bodyPath);

                using (var sr = new StreamReader(bodyPath, e))
                {
                    string bodyContent = sr.ReadToEnd();
                    //Regex rg = new Regex("(https://meet.office.dev/[^\\s/]+?/[0-9a-zA-z]{8})(?:[\"\\s$])");
                    Regex rg = new Regex("(https://meet.office.dev/[^\\s/]+?/[0-9a-zA-z]{8})(?:[\"\\s]|$)");
                    var groupValue = rg.Match(bodyContent);
                    if (groupValue.Groups.Count == 2)
                    {
                        meetUrl = groupValue.Groups[1].Value;
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during GetMeetingUrl:" + ex.Message + ex.ToString());
            }
            return meetUrl;
        }

        #endregion

        #region SFBMeeting Policy and Obligations
        private void DoEvaluationForEmailMeetingInvite(MailItem obMailItem, List<string> lisRecipients, EmailRecipient Sender, string strMeetingUrl, SmtpServer Server)
        {
            PolicyResults obPolicyResult = null;
            Exception exInfo = null;
            try
            {
                obPolicyResult = CheckPolicyForEmailMeetingInvite(strMeetingUrl, Sender, lisRecipients);
            }
            catch (Exception ex)
            {
                exInfo = ex;
                CSLogger.OutputLog(LogLevel.Debug, String.Format("Exception during check policy for email meeting invite, message:[{0}]\n", ex.Message));
                if (null == obPolicyResult)
                {
                    obPolicyResult = new PolicyResults();
                }
                obPolicyResult.ResultCode = PolicyResults.OPERATION_FAILED;
            }
            ProcessPolicyResults(obPolicyResult, obMailItem, lisRecipients, Sender, strMeetingUrl, Server, exInfo);
        }
        private PolicyResults CheckPolicyForEmailMeetingInvite(string strMeetingUrl, EmailRecipient Sender, List<string> lisRecipients)
        {
            PolicyResults result = null;
            PolicyAssistantSoapClient policyAssistantSoapClient = new PolicyAssistantSoapClient();
            string response = policyAssistantSoapClient.WMQueryPolicyForMeetingInvite(strMeetingUrl, Sender.NativeAddress, lisRecipients.ToArray(), false);
            XmlSerializer xs = new XmlSerializer(typeof(PolicyResults));
            using (StringReader reader = new StringReader(response))
            {
                result = xs.Deserialize(reader) as PolicyResults;
            }
            if (null == result)
            {
                result = new PolicyResults();
                result.ResultCode = PolicyResults.OPERATION_FAILED;
            }
            else
            {
                if (result.ResultCode == PolicyResults.OPERATION_PROCESSING)
                {
                    // send data again
                    int waitTime = Configuration.SingleRequestTime;
                    int processTime = 1;
                    int maxWaitTime = waitTime * lisRecipients.Count;
                    DateTime startTime = DateTime.Now;
                    result = LoopWaitForQueryResponse(policyAssistantSoapClient, result.QueryIndetify, waitTime, processTime, maxWaitTime, startTime);
                }
            }
            return result;

        }
        private bool ProcessPolicyResults(PolicyResults obPolicyResult, MailItem obMailItem, List<string> lisRecipients, EmailRecipient Sender, string strMeetingUrl, SmtpServer Server, Exception ex)
        {
            // Parameters check
            if (null == obPolicyResult)
            {
                CSLogger.OutputLog(LogLevel.Debug, "The policy result is null, no need process, ignore\n");
                return false;
            }

            bool bRet = false;
            switch (obPolicyResult.ResultCode)
            {
            case PolicyResults.OPERATION_SUCCEED:
            {
                DenyRecipients(obMailItem, obPolicyResult, Server);
                break;
            }
            case PolicyResults.OPERATION_FAILED:
            {
                if (null != ex)
                {
                    RouteAgent.Common.Function.DoExceptionNotify(ex, Server, obMailItem);
                }

                // Default policy config
                DoDenyAllRecipients(obMailItem);
                break;
            }
            case PolicyResults.OPERATION_NoManualClassify:
            {
               EmailRecipient emailRecipients = new EmailRecipient(Sender.NativeAddress, Sender.SmtpAddress);
                string notifyBody = Configuration.DenyNotifyBodyWithNoClassification + "Email subject:" + obMailItem.Message.Subject + ",meeting urL:" + strMeetingUrl;
                RouteAgent.Common.Function.SendEmail(obMailItem, Sender, new List<EmailRecipient> { emailRecipients }, Configuration.DenyNotifySubject, notifyBody, Configuration.DenyNotifyAttachOriginEmail, Server);
                DoDenyAllRecipients(obMailItem);
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
        private PolicyResults LoopWaitForQueryResponse(PolicyAssistantSoapClient policyAssistantSoapClient, string strQueryIndetify, int waitTime, int processTime, int maxWaitTime, DateTime startTime)
        {
            PolicyResults result = new PolicyResults();
            var alreadyCostTime = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            if (alreadyCostTime > maxWaitTime && alreadyCostTime > Configuration.SingleRequestMinTime)
            {
                CSLogger.OutputLog(LogLevel.Fatal, "QueryPolicyForMeetingResult cost time too long, we wont continue query, return and do exception behavior");
                result.ResultCode = PolicyResults.OPERATION_FAILED;
            }
            else
            {
                waitTime *= processTime;
                //if wait time is larger than SingleRequestMaxTime,we keep the old wait time
                waitTime = waitTime > Configuration.SingleRequestMaxTime ? Configuration.SingleRequestMaxTime : waitTime;
                System.Threading.Thread.Sleep(waitTime);

                result = GetQueryResponse(policyAssistantSoapClient, strQueryIndetify);
                processTime++;

                // Loop query
                if (result.ResultCode == PolicyResults.OPERATION_PROCESSING)
                {
                    LoopWaitForQueryResponse(policyAssistantSoapClient, result.QueryIndetify, waitTime, processTime, maxWaitTime, startTime);
                }
            }
            return result;
        }
        private PolicyResults GetQueryResponse(PolicyAssistantSoapClient policyAssistantSoapClient, string queryIndetify)
        {
            PolicyResults obPolicyResults = null;
            string strResponse = policyAssistantSoapClient.WMGetQueryPolicyForMeetingResult(queryIndetify);
            if (!String.IsNullOrEmpty(strResponse))
            {
                XmlSerializer xs = new XmlSerializer(typeof(PolicyResults));
                using (StringReader reader = new StringReader(strResponse))
                {
                    obPolicyResults = xs.Deserialize(reader) as PolicyResults;
                }
            }
            return obPolicyResults;
        }
        #endregion

        #region TDF header
        public string GetTDFHeaderInfoFromAttachment(Attachment attachFile)
        {
            string strJsonHeader = null;
            string strExtension = RouteAgent.Common.Function.GetFileSuffix(attachFile.FileName);
            if (strExtension.Equals("xml", StringComparison.OrdinalIgnoreCase))
            {
                using (StreamReader reader = new StreamReader(attachFile.GetContentReadStream()))
                {
                    string xmlString = reader.ReadToEnd();
                    try
                    {
                        string fileName = attachFile.FileName;
                        var fileNameIndex = fileName.LastIndexOf(".xml");
                        fileName = fileName.Remove(fileNameIndex);
                        var index = xmlString.LastIndexOf(fileName);
                        System.Diagnostics.Trace.WriteLine("index:" + index);
                        if (index > -1)
                        {
                            xmlString = xmlString.Remove(index);
                        }
                    }
                    catch (Exception ex)
                    {
                        CSLogger.OutputLog(LogLevel.Error, "Exception during get correct xml format string:", ex);
                    }

                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlString);
                        strJsonHeader = converToJSON(doc);
                    }
                    catch (Exception ex)
                    {
                        CSLogger.OutputLog(LogLevel.Error, "Exception during convert xml to json:", ex);
                    }
                }
            }
            return strJsonHeader;
        }
        #endregion

        #region Backup code
        private void InnerDoEvaluationForEmailMeetingInvite(MailItem obMailItem, List<string> lisRecipients, EmailRecipient Sender, string meetUrl, SmtpServer Server)
        {
            int waitTime = Configuration.SingleRequestTime;
            int processTime = 1;
            int maxWaitTime = waitTime * lisRecipients.Count;
            PolicyAssistantSoapClient policyAssistantSoapClient = new PolicyAssistantSoapClient();
            string response;
            PolicyResults result = null;
            try
            {
                response = policyAssistantSoapClient.WMQueryPolicyForMeetingInvite(meetUrl, Sender.NativeAddress, lisRecipients.ToArray(), false);
                XmlSerializer xs = new XmlSerializer(typeof(PolicyResults));
                using (StringReader reader = new StringReader(response))
                {
                    result = xs.Deserialize(reader) as PolicyResults;
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during call services:" + ex.Message);
                if (ex is System.Net.WebException || result == null)
                {
                    CSLogger.OutputLog(LogLevel.Error, "Error occured,we return the code,do exceptior behavior");
                    //notify admin
                    RouteAgent.Common.Function.DoExceptionNotify(ex, Server, obMailItem);
                    DoDenyAllRecipients(obMailItem);
                    return;
                }
            }

            if (result.ResultCode == PolicyResults.OPERATION_SUCCEED)
            {
                DenyRecipients(obMailItem, result, Server);
            }
            else if (result.ResultCode == PolicyResults.OPERATION_NoManualClassify)
            {
                EmailRecipient emailRecipients = new EmailRecipient(Sender.NativeAddress, Sender.SmtpAddress);
                string notifyBody = Configuration.DenyNotifyBodyWithNoClassification + "Email subject:" + obMailItem.Message.Subject + ",meeting urL:" + meetUrl;
                RouteAgent.Common.Function.SendEmail(obMailItem, Sender, new List<EmailRecipient> { emailRecipients }, Configuration.DenyNotifySubject, notifyBody, Configuration.DenyNotifyAttachOriginEmail, Server);
                DoDenyAllRecipients(obMailItem);
            }
            else if (result.ResultCode == PolicyResults.OPERATION_PROCESSING)
            {
                // send data again
                DateTime startTime = DateTime.Now;
                SendDataForResponse(policyAssistantSoapClient, obMailItem, result.QueryIndetify, waitTime, processTime, maxWaitTime, startTime, Server);
            }
        }
        private void SendDataForResponse(PolicyAssistantSoapClient policyAssistantSoapClient, MailItem obMailItem, string queryIndetify, int waitTime, int processTime, int maxWaitTime, DateTime startTime, SmtpServer Server)
        {
            var alreadyCostTime = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            if (alreadyCostTime > maxWaitTime && alreadyCostTime > Configuration.SingleRequestMinTime)
            {
                CSLogger.OutputLog(LogLevel.Fatal, "QueryPolicyForMeetingResult cost time too long,we wont contine query,code return,do exceptior behavior");
                //notify admin
                RouteAgent.Common.Function.DoExceptionNotify(strTimeOutInfo, Server, obMailItem);
                DoDenyAllRecipients(obMailItem);
                return;
            }
            waitTime *= processTime;
            //if wait time is larger than SingleRequestMaxTime,we keep the old wait time
            waitTime = waitTime > Configuration.SingleRequestMaxTime ? Configuration.SingleRequestMaxTime : waitTime;
            System.Threading.Thread.Sleep(waitTime);
            string response;
            PolicyResults result = null;
            try
            {
                response = policyAssistantSoapClient.WMGetQueryPolicyForMeetingResult(queryIndetify);
                XmlSerializer xs = new XmlSerializer(typeof(PolicyResults));
                using (StringReader reader = new StringReader(response))
                {
                    result = xs.Deserialize(reader) as PolicyResults;
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during call service:" + ex.Message);
                if (ex is System.Net.WebException || result == null)
                {
                    CSLogger.OutputLog(LogLevel.Error, "Error occured,we return the code,do exceptior behavior");
                    //notify admin
                    RouteAgent.Common.Function.DoExceptionNotify(ex, Server, obMailItem);
                    DoDenyAllRecipients(obMailItem);
                    return;
                }
            }
            processTime++;
            if (result.ResultCode == PolicyResults.OPERATION_SUCCEED)
            {
                DenyRecipients(obMailItem, result, Server);
            }
            else if (result.ResultCode == PolicyResults.OPERATION_PROCESSING)
            {
                SendDataForResponse(policyAssistantSoapClient, obMailItem, result.QueryIndetify, waitTime, processTime, maxWaitTime, startTime, Server);
            }
        }
        #endregion
    }
}
