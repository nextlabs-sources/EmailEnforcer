using Microsoft.Exchange.Data.Transport.Email;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using CSBase.Diagnose;

namespace RouteAgent.Common
{
    public class AppendMsgObligation
    {
        string strMsgPart = string.Empty;
        string strMsgPos = string.Empty;
        string strMsgVal = string.Empty;
        public AppendMsgObligation(string MessagePart,string MessagePosition,string MessageValue)
        {
            strMsgPart = MessagePart;
            strMsgPos = MessagePosition;
            strMsgVal = MessageValue;
        }

        public string MessagePart
        {
            get
            {
                return strMsgPart;
            }
            set
            {
                strMsgPart = value;
            }
        }
        public string MessagePosition
        {
            get
            {
                return strMsgPos;
            }
            set
            {
                strMsgPos = value;
            }
        }
        public string MessageValue
        {
            get
            {
                return strMsgVal;
            }
            set
            {
                strMsgVal = value;
            }
        }

        public static bool ObligationExits(AppendMsgObligation appendMessageObligation,List<AppendMsgObligation> lisAppendMessageObligations)
        {
            CSLogger.OutputLog(LogLevel.Debug, "ObligationExits Start");
            bool result = false;
            if (lisAppendMessageObligations != null)
            {
                foreach (AppendMsgObligation item in lisAppendMessageObligations)
                {
                    if (item.MessagePart.Equals(appendMessageObligation.MessagePart, StringComparison.OrdinalIgnoreCase) && item.MessagePosition.Equals(appendMessageObligation.MessagePosition, StringComparison.OrdinalIgnoreCase) && item.MessageValue.Equals(appendMessageObligation.MessageValue, StringComparison.OrdinalIgnoreCase))
                    {
                        result = true;
                        break;
                    }
                }
            }
            CSLogger.OutputLog(LogLevel.Debug, "ObligationExits End Result is " + result);
            return result;
        }

        public void DoObligation(EmailMessage message)
        {
            if (MessagePart.Equals(RouteAgent.Common.Policy.m_strObsAppendPartEmailSubject,StringComparison.OrdinalIgnoreCase))
            {
                AppendMessageToSubject(MessagePosition, MessageValue, message);
            }
            else if (MessagePart.Equals(RouteAgent.Common.Policy.m_strObsAppendPartEmailBody, StringComparison.OrdinalIgnoreCase))
            {
                if (!message.MapiMessageClass.Contains(ConstVariable.Str_MailClassify_MAPITASK))
                {
                    AppendMessageToBody(MessagePosition, MessageValue, message);
                }
                else
                {
                    CSLogger.OutputLog(LogLevel.Warn, "this mail MAPI class is " + message.MapiMessageClass + " we don't do append message obligation for email body");
                }
            }
            else
            {
                CSLogger.OutputLog(LogLevel.Debug, "Message Part:" + MessagePart);
            }
        }
        protected void AppendMessageToSubject(string strPosition, string strValue, EmailMessage message)
        {
            if (strPosition.Equals(RouteAgent.Common.Policy.m_strObsAppendPositionSuffix, StringComparison.OrdinalIgnoreCase))
            {
                message.Subject = message.Subject + strValue;
            }
            else
            {
                message.Subject = strValue + message.Subject;
            }
        }
        protected void AppendMessageToBody(string strPosition, string strValue, EmailMessage message)
        {
            Encoding bodyEncoding = null;
            try
            {
                bodyEncoding = Encoding.GetEncoding(message.Body.CharsetName);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception during append message to body, position:[{0}], value:[{1}], message:[{2}]", new object[] { strPosition, strValue, message }, ex);
                bodyEncoding = Encoding.Unicode;
            }
            //body format none can't modify email body
            if (message.Body.BodyFormat == BodyFormat.None)
            {
                CSLogger.OutputLog(LogLevel.Warn, "AppendMessageToBody for BodyFormatNone. can't append.");
                return;
            }

            //get original content
            Stream memStream;
            string strContent = string.Empty;
            if (message.Body.TryGetContentReadStream(out memStream))
            {
                using (StreamReader streamRead = new StreamReader(memStream, bodyEncoding))
                {
                    // TODO: May also want to decide on size of message and
                    // whether or not it should be processed if it is too large.
                    strContent = streamRead.ReadToEnd();
                }
            }

            //modify content
            string strNewContent = strContent;
            CSLogger.OutputLog(LogLevel.Debug, "AppendMessageToBody, body format:" + message.Body.BodyFormat.ToString());
            if (message.Body.BodyFormat == BodyFormat.Text)
            {
                strNewContent = AppendMessageBodyText(strContent, strPosition, strValue);
            }
            else if (message.Body.BodyFormat == BodyFormat.Html)
            {
                strNewContent = AppendMessageBodyHtml(strContent, strPosition, strValue);
            }
            else if (message.Body.BodyFormat == BodyFormat.Rtf)
            {
                strNewContent = AppendMessageBodyRtf(message.MapiMessageClass, strContent, strPosition, strValue);
            }
            else
            {
                CSLogger.OutputLog(LogLevel.Error, "Error body format:" + message.Body.BodyFormat.ToString());
            }


            //set the new content
            using (StreamWriter streamWriter = new StreamWriter(message.Body.GetContentWriteStream(), bodyEncoding))
            {
                streamWriter.Write(strNewContent);
            }

        }

        protected string AppendMessageBodyText(string strOldContent, string strPosition, string strAppendValue)
        {
            string strNewContent = strOldContent;
            if (strPosition.Equals(RouteAgent.Common.Policy.m_strObsAppendPositionSuffix, StringComparison.OrdinalIgnoreCase))
            {
                strNewContent = strOldContent + "\r\n" + strAppendValue;
            }
            else
            {
                strNewContent = strAppendValue + "\r\n" + strOldContent;
            }
            return strNewContent;
        }

        protected string AppendMessageBodyRtf(string strMsgCls, string strOldContent, string strPosition, string strAppendValue)
        {
            string strNewContent = strOldContent;
            if (strPosition.Equals(RouteAgent.Common.Policy.m_strObsAppendPositionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                //for fix bug 37384 , append message befor real content
                // look lost \\fs24 it alway work well
                // so don't modify else code range
                //const string strRtfBodyBegin = @"{\rtf1";
                //at joy's vm, this fix is not work well,
                //change another solutions,
                //check last char '{', insert at this localtion
                string strNewValueRtf = "{"+strAppendValue+@"\par}";
                //int nPos = strOldContent.IndexOf(strRtfBodyBegin, StringComparison.OrdinalIgnoreCase);
                int nPos = strOldContent.LastIndexOf('{');
                if (nPos >= 0)
                {
                    if (nPos + 1 < strOldContent.Length)
                    {
                        strNewContent = strOldContent.Insert(nPos + 1, strNewValueRtf);
                    }
                }
            }
            else
            {
                const string strRtfBodyEnd = "}";
                string strNewValueRtf = "\\par\r\n\\fs24" + strAppendValue;
                int nPos = strOldContent.LastIndexOf(strRtfBodyEnd, StringComparison.OrdinalIgnoreCase);

                if (nPos > 0)
                {
                    strNewContent = strOldContent.Insert(nPos, strNewValueRtf);
                }
            }

            return strNewContent;

        }

        protected string AppendMessageBodyHtml(string strOldContent, string strPosition, string strAppendValue)
        {
            string strNewContent = strOldContent;
            string strNewValueHtml = "<p>" + strAppendValue + "</p>";
            if (strPosition.Equals(RouteAgent.Common.Policy.m_strObsAppendPositionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                const string strHtmlBodyBegin = "<body";
                int nPos = strOldContent.IndexOf(strHtmlBodyBegin, StringComparison.OrdinalIgnoreCase);
                if (nPos > 0)
                {
                    int nPosBodyEnd = strOldContent.IndexOf('>', nPos);
                    if (nPosBodyEnd > 0)
                    {
                        strNewContent = strOldContent.Insert(nPosBodyEnd + 1, strNewValueHtml);
                    }
                }

            }
            else
            {
                const string strHtmlBodyEnd = "</body>";
                int nPos = strOldContent.IndexOf(strHtmlBodyEnd, StringComparison.OrdinalIgnoreCase);
                if (nPos > 0)
                {
                    strNewContent = strOldContent.Insert(nPos, strNewValueHtml);
                }
            }

            return strNewContent;
        }
    }
}
