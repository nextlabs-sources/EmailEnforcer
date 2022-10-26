using System;
using System.Collections.Generic;
using System.Text;
using SDKWrapperLib;
using System.Diagnostics;
using Microsoft.Exchange.Data.Transport.Email;
using System.IO;
using CSBase.Diagnose;
using Microsoft.Exchange.Data.Transport;
using RouteAgent.Plugin;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace RouteAgent.Common
{
    public class Policy
    {
        public const string m_strContentTypeEmailSubject = "Email Subject";
        public const string m_strContentTypeEmailBody = "Email Body";
        public const string m_strContentTypeEmailAttachment = "Email Attachment";
        public const string m_strEmailAction = "EMAIL";
        public const string m_strEmailActionInBound = "INBOUND";
        public const string m_strEmailActionOutBound = "OUTBOUND";
        public const string m_strContentTypeKey = "contenttype";

        public const string m_strObNameEmailNDR = "EXCHANGE_NDR";
        public const string m_strObEmailNDRErrorKey = "Error Code";
        public const string m_strObEmailNDRErrorMsg = "Error Message";
        public const string m_strObNameEmailNotify = "EXCHANGE_EMAIL_NOTIFICATION";
        public const string m_strObEmailNotifyTargetKey = "Target";
        public const string m_strObEmailNotifyTargetValueSender = "Sender";
        public const string m_strObEmailNotifyTargetValueReceive = "Recipients";
        public const string m_strObEmailNotifyTargetValueBoth = "Both";
        public const string m_strObEmailNotifyAttachOrigEmailKey = "AttachOrginalEmail";
        public const string m_strObEmailNotifyAttachOrigEmailValueY = "Yes";
        public const string m_strObEmailNotifyAttachOrigEmailValueN = "No";
        public const string m_strObEmailNotifySubjectKey = "Subject";
        public const string m_strObEmailNotifyBodyKey = "Body";

        public const string m_strObNameRMS = "AttachmentRMS";
        public const string m_strObRMSTagNameKey = "Classify Name ";
        public const string m_strObRMSTagValueKey = "Classify Value ";

        public const string m_strObNameNormalTag = "SP_File_Tag";
        public const string m_strObsNormalTagNameKey = "Tag Name";
        public const string m_strObsNormalTagValueKey = "Tag Value";

        public const string m_strObNameAppendMessage = "APPEND_MESSAGE";
        public const string m_strObsAppendMessageValue = "Append Message";
        public const string m_strObsAppendMessagePosition = "Append position";
        public const string m_strObsAppendMessagePart = "Append parts";

        public const string m_strObsAppendPositionPrefix = "Prefix";
        public const string m_strObsAppendPositionSuffix = "Suffix";

        public const string m_strObsAppendPartEmailBody = "Email Body";
        public const string m_strObsAppendPartEmailSubject = "Email Subject";

        public const string m_strObNameApproval = "MAIL_APPROVAL";
        public const string m_strObsApprovalApprover = "Approver";
        public const string m_strObsApprovalMailSubject = "You Have An Email Need To Review";

        public const string m_strObNameMailClassify = "ExMailTag";
        public const string m_strObMailClsNameFmt = "Classify Name ";
        public const string m_strObMailClsValueFmt = "Classify Value ";
        public const string m_strObMailClsMode = "Classify Mode";
        public const string m_strObMailClsModeOverWrite = "OverWrite";
        public const string m_strObMailClsModeAppend = "Append";


        public enum CEResult
        {
            Allow = 0,
            Deny = 1,
            Unknow = 2
        }

        public static string GetObsApprovalMailHtmlBody(EmailMessage message)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<div>");
            sb.Append("<p style=\"text-indent:0em;\">Dear Approver:</p>");
            sb.Append("<p style=\"text-indent:2em;\">" + message.Sender.DisplayName + "(" + message.Sender.SmtpAddress + ") had send a mail</p>");
            if (message.To.Count > 0)
            {
                sb.Append("<p style=\"text-indent:2em;\">TO:</p>");
            }
            foreach (EmailRecipient item in message.To)
            {
                sb.Append("<p style=\"text-indent:4em;\">" + item.DisplayName + "(" + item.SmtpAddress + ")</p>");

            }
            if (message.Cc.Count > 0)
            {
                sb.Append("<p style=\"text-indent:2em;\">CC:</p>");
            }
            foreach (EmailRecipient item in message.Cc)
            {
                sb.Append("<p style=\"text-indent:4em;\">" + item.DisplayName + "(" + item.SmtpAddress + ")</p>");
            }
            sb.Append("<p style=\"text-indent:2em;\">SUBJECT:" + message.Subject + "</p>");
            sb.Append("<p style=\"text-indent:2em\">Body:</p>");
            string strBody = string.Empty;
            Stream streamBodyRead = null;
            try
            {
                streamBodyRead = message.Body.GetContentReadStream();
                {
                    using (StreamReader sr = new StreamReader(streamBodyRead))
                    {
                        streamBodyRead = null;
                        strBody = sr.ReadToEnd();
                    }
                }
            }
            finally
            {
                if (streamBodyRead != null)
                {
                    streamBodyRead.Dispose();
                }
            }
            sb.Append("<div style=\"text-indent:4em\">" + strBody + "</div>");
            if (message.Attachments.Count > 0)
            {
                sb.Append("<p style=\"text-indent:2em\">Attachment File:</p>");
            }
            foreach (Attachment item in message.Attachments)
            {
                sb.Append("<p style=\"text-indent:4em;\">" + item.FileName + "</p>");

            }
            sb.Append("</div>");

            sb.Append("<div>");
            sb.Append("<a href=\"" + Function.GetApprovalInfoUrl(message.MessageId, CEResult.Allow) + "\">Click Here Approval This Mail</a>");
            sb.Append("&nbsp;&nbsp;&nbsp;");
            sb.Append("<a href=\"" + Function.GetApprovalInfoUrl(message.MessageId, CEResult.Deny) + "\">Click Here NOT Approval This Mail</a>");
            sb.Append("</div>");

            sb.Append("</p>");
            sb.Append("</p>");
            sb.Append("</p>");
            sb.Append("</p>");
            sb.Append("</p>");
            sb.Append("<div>");
            sb.Append("<p>If you can not see link , please copy flower hypelink to you web brower:</p>");
            sb.Append("</p>");
            sb.Append(Function.GetApprovalInfoUrl(message.MessageId, CEResult.Unknow));
            sb.Append("</div>");
            return sb.ToString();
        }

        public static PolicyResult QueryPolicy(string strClientType, string strSender, string strSendSid, List<Microsoft.Exchange.Data.Transport.EnvelopeRecipient> listRecipients, string strLocalFile, string strContentType, List<KeyValuePair<string, string>> lisPairClassification, string strAction, List<KeyValuePair<string, string>> lisPairHeader)
        {
            PlugInManager obPlugInManagerIns = PlugInManager.GetInstance();
            List<INLUserParser> lsUserParserPlugin = obPlugInManagerIns.GetPlugins<INLUserParser>();

            Request pReq = new Request();
            pReq.set_action(strAction);
            string strAppPath = Function.GetApplicationFilePath();
            pReq.set_app(System.IO.Path.GetFileName(strAppPath), strAppPath, string.Empty, null);
            pReq.set_noiseLevel(2);

            strSender = CommonPluginTools.GetStandardEmailAddressFromADByUserParserPlugin(lsUserParserPlugin, strSender, true);

            CEAttres theUserAttres = new CEAttres();
            theUserAttres.add_attre(RouteAgent.Common.ConstVariable.Str_Attribute_Name_ClientType, strClientType);
            List<KeyValuePair<string, string>> lsUserAttrRet = CommonPluginTools.GetUserAttributesInfo(lsUserParserPlugin, strSender);
            AddPairToSrcAttres(theUserAttres, lsUserAttrRet);

            pReq.set_user(strSendSid, strSender, theUserAttres);
            pReq.set_performObligation(1);

            CEAttres theSrcAttres = new CEAttres();
            theSrcAttres.add_attre(m_strContentTypeKey, strContentType);

            theSrcAttres.add_attre(RouteAgent.Common.ConstVariable.Str_Attribute_Name_NoCache, RouteAgent.Common.ConstVariable.Str_YES);
            theSrcAttres.add_attre(RouteAgent.Common.ConstVariable.Str_Attribute_Name_FileSystemCheck, RouteAgent.Common.ConstVariable.Str_YES);

            AddPairToSrcAttres(theSrcAttres, lisPairClassification);
            AddPairToSrcAttres(theSrcAttres, lisPairHeader);

            pReq.set_param(strLocalFile, RouteAgent.Common.ConstVariable.Str_Attribute_Nmae_FSO, theSrcAttres, 0);

            foreach (EnvelopeRecipient obRecipientItem in listRecipients)
            {
                string strStandardRecipientEamilAddr = CommonPluginTools.GetStandardEmailAddressFromADByUserParserPlugin(lsUserParserPlugin, obRecipientItem.Address.ToString(), true);
                pReq.set_recipient(strStandardRecipientEamilAddr);

                // Here need set extral recipient attributes but no public interface
                // List<KeyValuePair<string, string>> lsUserAttrRet = CommonPluginTools.GetUserAttributesInfo(lsUserParserPlugin, strStandardRecipientEamilAddr);
            }

            QueryPC thePc = new QueryPC();
            int lCookie = 0;
            thePc.get_cookie(out lCookie);

            thePc.set_request(pReq, lCookie);

            int lResult = 0;
            try
            {
                thePc.check_resource(lCookie, 1000 * 60, 1, out lResult);
            }
            catch (Exception ex)
            {
                throw new QueryPCException("In QueryPolicyEx, check_resourceex failed", ex);
            }
            if (lResult != 0)
            {
                throw new QueryPCException("In QueryPolicyEx, check_resourceex failed");
            }

            //get policy result
            PolicyResult policyResult = new PolicyResult();

            int iObNum = 0;
            thePc.get_result(lCookie, 0, out lResult, out iObNum);

            policyResult.bDeny = (lResult == 0) ? true : false;

            if (iObNum > 0)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Obligation count > 0 Start Get Obligation");

                policyResult.lstExchangeObligations = new List<ExchangeObligation>();
                string[] arryStrObName = { m_strObNameEmailNDR, m_strObNameEmailNotify, m_strObNameNormalTag, m_strObNameRMS, m_strObNameAppendMessage, m_strObNameApproval };
                foreach (string strObName in arryStrObName)
                {
                    for (int i = 0; i < iObNum; i++)
                    {
                        Obligation ob = new Obligation();
                        thePc.get_obligation(lCookie, strObName, 0, i, out ob);
                        if (ob != null)
                        {
                            ExchangeObligation exchangeObligation = new ExchangeObligation(ob);
                            policyResult.lstExchangeObligations.Add(exchangeObligation);

                            CSLogger.OutputLog(LogLevel.Debug, "get obligation:" + strObName);
                        }
                        else
                        {
                        }
                    }
                }
            }
            else
            {
                CSLogger.OutputLog(LogLevel.Debug, "Obligation count = 0");
            }

            return policyResult;
        }

        public static List<QueryPolicyResult> QueryPolicyEx(List<EmailInfo> lisEmailInfos, string strAction, string strClientType, string strSender, string strSendSid,
                                                                            List<KeyValuePair<string, string>> lisPairClassification, List<Microsoft.Exchange.Data.Transport.EnvelopeRecipient> lisRecipients,
                                                                            List<KeyValuePair<string, string>> lisPairHeader, List<GroupInfo> lisGroupInfos)
        {
            PlugInManager obPlugInManagerIns = PlugInManager.GetInstance();
            List<INLUserParser> lsUserParserPlugin = obPlugInManagerIns.GetPlugins<INLUserParser>();

            strSender = CommonPluginTools.GetStandardEmailAddressFromADByUserParserPlugin(lsUserParserPlugin, strSender, true);
            CSLogger.OutputLog(LogLevel.Debug, "sender address : " + strSender);

            CSLogger.OutputLog(LogLevel.Debug, "QueryPolicyEx Start");
            List<QueryPolicyResult> lisPolicyCache = new List<QueryPolicyResult>();
            QueryPC thePC = new QueryPC();
            CSLogger.OutputLog(LogLevel.Debug, "Create QueryPC instance");

            JsonHelperDataModule.JsonHelperRequest jsonHelperRequest = new JsonHelperDataModule.JsonHelperRequest();
            JsonHelperDataModule.RequestNode requestNode = new JsonHelperDataModule.RequestNode();

            requestNode.CombinedDecision = false;
            requestNode.ReturnPolicyIdList = false;
            requestNode.XPathVersion = "http://www.w3.org/TR/1999/REC-xpath-19991116";

            JsonHelperDataModule.CategoryNode categoryNode = new JsonHelperDataModule.CategoryNode();
            categoryNode.CategoryId = "attribute-category:application";
            categoryNode.Id = "application1";

            string strAppPath = Function.GetApplicationFilePath();
            JsonHelperDataModule.AttributeNode attributeNodeAppId = new JsonHelperDataModule.AttributeNode();
            attributeNodeAppId.AttributeId = "application:application-name";
            attributeNodeAppId.Value = System.IO.Path.GetFileName(strAppPath);
            attributeNodeAppId.DataType = "http://www.w3.org/2001/XMLSchema#string";
            attributeNodeAppId.IncludeInResult = false;

            JsonHelperDataModule.AttributeNode attributeNodeAppPath = new JsonHelperDataModule.AttributeNode();
            attributeNodeAppPath.AttributeId = "application:application-path";
            attributeNodeAppPath.Value = strAppPath;
            attributeNodeAppPath.DataType = "http://www.w3.org/2001/XMLSchema#string";
            attributeNodeAppPath.IncludeInResult = false;

            categoryNode.Attribute = new List<JsonHelperDataModule.AttributeNode>();
            categoryNode.Attribute.Add(attributeNodeAppId);
            categoryNode.Attribute.Add(attributeNodeAppPath);

            requestNode.Category = new List<JsonHelperDataModule.CategoryNode>();
            requestNode.Category.Add(categoryNode);

            JsonHelperDataModule.SubjectNode subjectNode = new JsonHelperDataModule.SubjectNode();
            subjectNode.CategoryId = "access-subject";
            subjectNode.Id = "subject1";
            subjectNode.Attribute = new List<JsonHelperDataModule.AttributeNode>();

            JsonHelperDataModule.AttributeNode attributeNodeSubjectName = new JsonHelperDataModule.AttributeNode();
            attributeNodeSubjectName.AttributeId = "subject:name";
            attributeNodeSubjectName.DataType = "http://www.w3.org/2001/XMLSchema#string";
            attributeNodeSubjectName.IncludeInResult = false;
            attributeNodeSubjectName.Value = strSender;
            subjectNode.Attribute.Add(attributeNodeSubjectName);

            JsonHelperDataModule.AttributeNode attributeNodeSubjectSid = new JsonHelperDataModule.AttributeNode();
            attributeNodeSubjectSid.AttributeId = "subject:sid";
            attributeNodeSubjectSid.DataType = "http://www.w3.org/2001/XMLSchema#string";
            attributeNodeSubjectSid.IncludeInResult = false;
            attributeNodeSubjectSid.Value = !string.IsNullOrWhiteSpace(strSendSid) ? strSendSid : strSender; // if sid is empty string, use email address as user id
            subjectNode.Attribute.Add(attributeNodeSubjectSid);

            List<KeyValuePair<string, string>> lsSenderAttr = CommonPluginTools.GetUserAttributesInfo(lsUserParserPlugin, strSender);
            AddPairToSubjectNode(subjectNode, lsSenderAttr);

            if (!string.IsNullOrWhiteSpace(strClientType))
            {
                JsonHelperDataModule.AttributeNode attributeNodeSubjectclientType = new JsonHelperDataModule.AttributeNode();
                attributeNodeSubjectclientType.AttributeId = "subject:" + RouteAgent.Common.ConstVariable.Str_Attribute_Name_ClientType;
                attributeNodeSubjectclientType.DataType = "http://www.w3.org/2001/XMLSchema#string";
                attributeNodeSubjectclientType.IncludeInResult = false;
                attributeNodeSubjectclientType.Value = strClientType;
                subjectNode.Attribute.Add(attributeNodeSubjectclientType);
            }


            requestNode.Subject = new List<JsonHelperDataModule.SubjectNode>();
            requestNode.Subject.Add(subjectNode);

            JsonHelperDataModule.ActionNode actionNode = new JsonHelperDataModule.ActionNode();
            actionNode.CategoryId = "attribute-category:action";
            actionNode.Id = "action1";

            JsonHelperDataModule.AttributeNode attributeNodeAction = new JsonHelperDataModule.AttributeNode();
            attributeNodeAction.AttributeId = "action:action-id";
            attributeNodeAction.DataType = "http://www.w3.org/2001/XMLSchema#string";
            attributeNodeAction.IncludeInResult = false;
            attributeNodeAction.Value = strAction;

            actionNode.Attribute = new List<JsonHelperDataModule.AttributeNode>();
            actionNode.Attribute.Add(attributeNodeAction);

            requestNode.Action = new List<JsonHelperDataModule.ActionNode>();
            requestNode.Action.Add(actionNode);

            requestNode.Resource = new List<JsonHelperDataModule.ResourceNode>();
            requestNode.Recipient = new List<JsonHelperDataModule.RecipientNode>();

            int iresourceId = 0;
            int iRecipientId = 0;
            //////////////////////////

            JsonHelperDataModule.AttributeNode attributeSourceNoCache = new JsonHelperDataModule.AttributeNode();
            attributeSourceNoCache.AttributeId = "resource:" + RouteAgent.Common.ConstVariable.Str_Attribute_Name_NoCache;
            attributeSourceNoCache.DataType = "http://www.w3.org/2001/XMLSchema#string";
            attributeSourceNoCache.IncludeInResult = false;
            attributeSourceNoCache.Value = RouteAgent.Common.ConstVariable.Str_YES;

            JsonHelperDataModule.AttributeNode attributeSourceFileSystemCheck = new JsonHelperDataModule.AttributeNode();
            attributeSourceFileSystemCheck.AttributeId = "resource:" + RouteAgent.Common.ConstVariable.Str_Attribute_Name_FileSystemCheck;
            attributeSourceFileSystemCheck.DataType = "http://www.w3.org/2001/XMLSchema#string";
            attributeSourceFileSystemCheck.IncludeInResult = false;
            attributeSourceFileSystemCheck.Value = RouteAgent.Common.ConstVariable.Str_YES;
            /////////////////////////

            foreach (var recipient in lisRecipients)
            {
                iRecipientId++;

                JsonHelperDataModule.AttributeNode attributeNodeRecipient = new JsonHelperDataModule.AttributeNode();
                attributeNodeRecipient.DataType = "http://www.w3.org/2001/XMLSchema#string";
                attributeNodeRecipient.AttributeId = "recipient:email";
                attributeNodeRecipient.IncludeInResult = false;

                string strRecipientAddress = CommonPluginTools.GetStandardEmailAddressFromADByUserParserPlugin(lsUserParserPlugin, recipient.Address.ToString(), true);
                attributeNodeRecipient.Value = strRecipientAddress;
                CSLogger.OutputLog(LogLevel.Debug, "recipient address : " + strRecipientAddress);

                JsonHelperDataModule.RecipientNode recipientNode = new JsonHelperDataModule.RecipientNode();
                recipientNode.CategoryId = "recipient-subject";
                recipientNode.Id = "recipient" + iRecipientId;
                recipientNode.Attribute = new List<JsonHelperDataModule.AttributeNode>();
                recipientNode.Attribute.Add(attributeNodeRecipient);

                List<KeyValuePair<string, string>> lsRecipientAttr = CommonPluginTools.GetUserAttributesInfo(lsUserParserPlugin, strRecipientAddress);
                AddPairToRecipientNode(recipientNode, lsRecipientAttr);

#if ENABLEGROUPINFO
                bool bIsGroup = false;
                if (lisGroupInfos != null)
                {
                    foreach (GroupInfo groupInfo in lisGroupInfos)
                    {
                        if (strRecipientAddress.Equals(groupInfo.Address.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            bIsGroup = true;
                            break;
                        }
                    }
                }
                JsonHelperDataModule.AttributeNode attributeNodeRecipientType = new JsonHelperDataModule.AttributeNode();
                attributeNodeRecipientType.DataType = "http://www.w3.org/2001/XMLSchema#string";
                attributeNodeRecipientType.AttributeId = "recipient:recipient-type";
                attributeNodeRecipientType.IncludeInResult = false;
                attributeNodeRecipientType.Value = bIsGroup ? "group" : "user";
                recipientNode.Attribute.Add(attributeNodeRecipientType);
#endif

                foreach (EmailInfo emailInfo in lisEmailInfos)
                {
                    iresourceId++;
                    JsonHelperDataModule.ResourceNode resourceNode = new JsonHelperDataModule.ResourceNode();
                    resourceNode.CategoryId = "attribute-category:resource";

                    resourceNode.Id = "resource" + iresourceId;

                    resourceNode.Attribute = new List<JsonHelperDataModule.AttributeNode>();

                    JsonHelperDataModule.AttributeNode attributeNodeSourceContentType = new JsonHelperDataModule.AttributeNode();
                    attributeNodeSourceContentType.AttributeId = "resource:" + m_strContentTypeKey;
                    attributeNodeSourceContentType.DataType = "http://www.w3.org/2001/XMLSchema#string";
                    attributeNodeSourceContentType.IncludeInResult = false;
                    attributeNodeSourceContentType.Value = emailInfo.strContentType;
                    resourceNode.Attribute.Add(attributeNodeSourceContentType);

                    JsonHelperDataModule.AttributeNode attributeNodeSourceId = new JsonHelperDataModule.AttributeNode();
                    attributeNodeSourceId.AttributeId = "resource:resource-id";
                    attributeNodeSourceId.DataType = "http://www.w3.org/2001/XMLSchema#string";
                    attributeNodeSourceId.IncludeInResult = false;
                    attributeNodeSourceId.Value = emailInfo.strSavedPath;
                    resourceNode.Attribute.Add(attributeNodeSourceId);

                    JsonHelperDataModule.AttributeNode attributeNodeSourceType = new JsonHelperDataModule.AttributeNode();
                    attributeNodeSourceType.AttributeId = "resource:resource-type";
                    attributeNodeSourceType.DataType = "http://www.w3.org/2001/XMLSchema#string";
                    attributeNodeSourceType.IncludeInResult = false;
                    attributeNodeSourceType.Value = "ee";
                    resourceNode.Attribute.Add(attributeNodeSourceType);

                    {
                        //added resource:ce::nativeresname for CA
                        JsonHelperDataModule.AttributeNode attributeNodeNativeResName = new JsonHelperDataModule.AttributeNode();
                        attributeNodeNativeResName.AttributeId = "resource:ce::nativeresname";
                        attributeNodeNativeResName.DataType = "http://www.w3.org/2001/XMLSchema#string";
                        attributeNodeNativeResName.IncludeInResult = false;
                        attributeNodeNativeResName.Value = emailInfo.strSavedPath;
                        resourceNode.Attribute.Add(attributeNodeNativeResName);
                    }

                    //added tag

                    if (emailInfo.attachInfo != null && emailInfo.attachInfo.listTags != null)
                    {
                        List<KeyValuePair<string, string>> lstTag = emailInfo.attachInfo.listTags;
                        foreach (KeyValuePair<string, string> pairTag in lstTag)
                        {
                            if (string.IsNullOrWhiteSpace(pairTag.Key) || string.IsNullOrWhiteSpace(pairTag.Value))
                            {
                                continue;
                            }

                            JsonHelperDataModule.AttributeNode attributeTag = new JsonHelperDataModule.AttributeNode();
                            attributeTag.AttributeId = "resource:" + pairTag.Key;  //must begin with "resource:"
                            attributeTag.DataType = "http://www.w3.org/2001/XMLSchema#string";
                            attributeTag.IncludeInResult = false;
                            attributeTag.Value = pairTag.Value;
                            resourceNode.Attribute.Add(attributeTag);
                        }
                    }



                    resourceNode.Attribute.Add(attributeSourceNoCache);
                    resourceNode.Attribute.Add(attributeSourceFileSystemCheck);

                    AddPairToResourceNode(resourceNode, lisPairClassification);
                    AddPairToResourceNode(resourceNode, lisPairHeader);

                    requestNode.Resource.Add(resourceNode);
                }
                requestNode.Recipient.Add(recipientNode);
            }
            JsonHelperDataModule.MultiRequestsNode muitiRequestNode = new JsonHelperDataModule.MultiRequestsNode();
            muitiRequestNode.RequestReference = new List<JsonHelperDataModule.RequestReferenceNode>();

            iresourceId = 0;
            for (int i = 1; i <= lisRecipients.Count; i++)
            {
                for (int j = 1; j <= lisEmailInfos.Count; j++)
                {
                    iresourceId++;
                    JsonHelperDataModule.RequestReferenceNode requestReferenceNode = new JsonHelperDataModule.RequestReferenceNode();
                    requestReferenceNode.ReferenceId = new List<string>();
                    requestReferenceNode.ReferenceId.Add("subject1");
                    requestReferenceNode.ReferenceId.Add("action1");
                    requestReferenceNode.ReferenceId.Add("application1");
                    requestReferenceNode.ReferenceId.Add("resource" + iresourceId);
                    requestReferenceNode.ReferenceId.Add("recipient" + i);
                    muitiRequestNode.RequestReference.Add(requestReferenceNode);
                }
            }

            int callResult = -1;
            requestNode.MultiRequests = muitiRequestNode;
            jsonHelperRequest.Request = requestNode;
            JsonHelperDataModule.JsonHelperResponse jsonHelperResponse = null;
            string strJson = null;
            try
            {
                strJson = Function.JsonSerializer.SaveToJson(jsonHelperRequest);
                CSLogger.OutputLog(LogLevel.Debug, "Create Query Json Success");
                //  CSLogger.OutputLog(LogLevel.Debug, string.Format("strJson = {0}", strJson==null?"":strJson));
                string strResponse = "";

                Stopwatch tempWatch = new Stopwatch();
                tempWatch.Start();
                thePC.check_resourceex_json(strJson, out strResponse, ref callResult);
                tempWatch.Stop();

                CSLogger.OutputLog(LogLevel.Debug, "Call Com Interface TimeSpan=" + tempWatch.ElapsedMilliseconds);
                try
                {
                    jsonHelperResponse = Function.JsonSerializer.LoadFromJson<JsonHelperDataModule.JsonHelperResponse>(strResponse);
                }
                catch (Exception ex)
                {
                    CSLogger.OutputLog(LogLevel.Fatal, string.Format("LoadFromJson exception:{0}, json_response={1}", ex.Message, strResponse == null ? "null" : strResponse));
                }

            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Fatal, string.Format("In QueryPolicyEx exception,check_resourceex_json callresult:{0}, json_request:{1}", callResult, strJson == null ? "NULL" : strJson));
                throw new QueryPCException("In QueryPolicyEx, check_resourceex_json failed", ex);
            }
            if (callResult == 0 && jsonHelperResponse != null)
            {
                int iResultIndex = -1;
                for (int iRecipientIndex = 0; iRecipientIndex < lisRecipients.Count; iRecipientIndex++)
                {
                    List<PolicyResult> lisPolicyResult = new List<PolicyResult>();
                    for (int iMailInfoIndex = 0; iMailInfoIndex < lisEmailInfos.Count; iMailInfoIndex++)
                    {
                        iResultIndex++;
                        //get policy result
                        PolicyResult policyResult = new PolicyResult();
                        policyResult.emailInfo = lisEmailInfos[iMailInfoIndex];

                        if (jsonHelperResponse.Response != null)
                        {
                            if (jsonHelperResponse.Response.Result != null)
                            {
                                if (jsonHelperResponse.Response.Result.Count >= iResultIndex)
                                {
                                    JsonHelperDataModule.ResultNode resultNode = jsonHelperResponse.Response.Result[iResultIndex];
                                    if (resultNode.Decision != null)
                                    {
                                        policyResult.bDeny = (resultNode.Decision.Equals("deny", StringComparison.OrdinalIgnoreCase)) ? true : false;
                                        int iObNum = 0;
                                        if (resultNode.Obligations != null)
                                        {
                                            iObNum = resultNode.Obligations.Count;
                                        }

                                        if (iObNum > 0)
                                        {
                                            policyResult.lstExchangeObligations = new List<ExchangeObligation>();
                                            string[] arryStrObName = { m_strObNameEmailNDR, m_strObNameEmailNotify,
                                                                         m_strObNameNormalTag, m_strObNameRMS,
                                                                         m_strObNameAppendMessage, m_strObNameApproval,
                                                                         m_strObNameMailClassify};
                                            foreach (string strObName in arryStrObName)
                                            {
                                                for (int i = 0; i < iObNum; i++)
                                                {
                                                    if (resultNode.Obligations[i] != null)
                                                    {
                                                        if (resultNode.Obligations[i].Id != null)
                                                        {
                                                            if (resultNode.Obligations[i].Id.Equals(strObName, StringComparison.OrdinalIgnoreCase))
                                                            {
                                                                ExchangeObligation exchangeObligation = new ExchangeObligation(resultNode.Obligations[i]);
                                                                policyResult.lstExchangeObligations.Add(exchangeObligation);
                                                                policyResult.MatchedPolicyName = exchangeObligation.PolicyName;

                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            CSLogger.OutputLog(LogLevel.Debug, "Obligation count = 0");
                                        }
                                    }
                                }
                                else
                                {
                                    CSLogger.OutputLog(LogLevel.Error, "Call Com Interface return Result Node Count Less Than Request");
                                }
                            }
                            else
                            {
                                CSLogger.OutputLog(LogLevel.Error, "Call Com Interface return empty Result Node");
                            }
                        }
                        else
                        {
                            CSLogger.OutputLog(LogLevel.Error, "Call Com Interface return empty Response Node");
                        }
                        lisPolicyResult.Add(policyResult);
                    }
                    QueryPolicyResult queryResult = new QueryPolicyResult(lisRecipients[iRecipientIndex], lisPolicyResult);
                    lisPolicyCache.Add(queryResult);
                }
            }
            else
            {
                throw new QueryPCException("In QueryPolicyEx, check_resourceex failed.");
            }
            return lisPolicyCache;
        }

        private static JsonHelperDataModule.JsonHelperResponse GetTestResult()
        {
            JsonHelperDataModule.JsonHelperResponse jsonHelperResponse = new JsonHelperDataModule.JsonHelperResponse();
            JsonHelperDataModule.ResponseNode responseNode = new JsonHelperDataModule.ResponseNode();
            responseNode.Result = new List<JsonHelperDataModule.ResultNode>();

            {
                JsonHelperDataModule.ResultNode resultNode1 = new JsonHelperDataModule.ResultNode();
                resultNode1.Decision = "allow";
                resultNode1.Status = new JsonHelperDataModule.StatusNode();
                resultNode1.Status.StatusCode = new JsonHelperDataModule.StatusCodeNode();
                resultNode1.Status.StatusMessage = "Success";
                resultNode1.Status.StatusCode.Value = "status:ok";

                resultNode1.Obligations = new List<JsonHelperDataModule.ObligationsNode>();

                JsonHelperDataModule.ObligationsNode obligation1 = new JsonHelperDataModule.ObligationsNode();
                obligation1.Id = "APPEND_MESSAGE";

                responseNode.Result.Add(resultNode1);
            }


            jsonHelperResponse.Response = responseNode;
            return jsonHelperResponse;
        }

        private static void AddPairToSrcAttres(CEAttres theSrcAttres, List<KeyValuePair<string, string>> lisPair)
        {
            if (null != lisPair)
            {
                foreach (var pair in lisPair)
                {
                    theSrcAttres.add_attre(pair.Key, pair.Value);
                }
            }
        }

        private static void AddPairToResourceNode(JsonHelperDataModule.ResourceNode resourceNode, List<KeyValuePair<string, string>> lisPair)
        {
            if (null != lisPair)
            {
                foreach (var pair in lisPair)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                    {
                        continue;
                    }

                    JsonHelperDataModule.AttributeNode attributeNodeClassification = new JsonHelperDataModule.AttributeNode();
                    attributeNodeClassification.AttributeId = "resource:" + pair.Key;
                    attributeNodeClassification.DataType = "http://www.w3.org/2001/XMLSchema#string";
                    attributeNodeClassification.IncludeInResult = false;
                    attributeNodeClassification.Value = pair.Value;
                    resourceNode.Attribute.Add(attributeNodeClassification);
                }
            }
        }

        private static void AddPairToSubjectNode(JsonHelperDataModule.SubjectNode subjectNode, List<KeyValuePair<string, string>> lisPair)
        {
            if (null != lisPair)
            {
                foreach (var pair in lisPair)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                    {
                        continue;
                    }

                    JsonHelperDataModule.AttributeNode attributeNodeSubjectSid = new JsonHelperDataModule.AttributeNode();
                    attributeNodeSubjectSid.AttributeId = "subject:" + pair.Key;
                    attributeNodeSubjectSid.DataType = "http://www.w3.org/2001/XMLSchema#string";
                    attributeNodeSubjectSid.IncludeInResult = false;
                    attributeNodeSubjectSid.Value = pair.Value;
                    subjectNode.Attribute.Add(attributeNodeSubjectSid);
                }
            }
        }

        private static void AddPairToRecipientNode(JsonHelperDataModule.RecipientNode recipientNode, List<KeyValuePair<string, string>> lisPair)
        {
            if (null != lisPair)
            {
                foreach (var pair in lisPair)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                    {
                        continue;
                    }

                    JsonHelperDataModule.AttributeNode attributeNodeRecipient = new JsonHelperDataModule.AttributeNode();
                    attributeNodeRecipient.DataType = "http://www.w3.org/2001/XMLSchema#string";
                    attributeNodeRecipient.AttributeId = "recipient:" + pair.Key;
                    attributeNodeRecipient.IncludeInResult = false;
                    attributeNodeRecipient.Value = pair.Value;
                    recipientNode.Attribute.Add(attributeNodeRecipient);
                }
            }
        }
        
    }

    [Serializable]
    public class QueryPCException : ApplicationException
    {
        public QueryPCException() { }
        public QueryPCException(string message) : base(message) { }
        public QueryPCException(string message, Exception inner) : base(message, inner) { }
    }
}
