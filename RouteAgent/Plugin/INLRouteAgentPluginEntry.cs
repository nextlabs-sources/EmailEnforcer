using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Exchange.Data.Transport.Email;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

// Need pass a logger to plugin


namespace RouteAgent.Plugin
{
    // Note: for Multiple inheritance, the init and uninit will be invoke mutiple times
    // Because of the plugin manager will invoke init and uninit for each interface 
    public interface INPluginRoot
    {
        #region Init
        void Init(Type tyInterface);
        void Uninit(Type tyInterface);
        #endregion
    }

    public interface INLUserParser : INPluginRoot
    {
        #region User info
        string GetUserSecurityIDByEmailAddress(string strUserEmailAddress, bool bAutoCheckWithLogonName, string strDefaultSID);
        string GetStandardEmailAddressFromAD(string strUserEmailAddressOrg, bool bFailedReturnOriginalAddr);
        void AdjustUserAttributesInfo(string strUserStandardEmailAddress, ref List<KeyValuePair<string, string>> lsUserAttributesInfoRef);
        #endregion
    }

    public interface INLEmailParser : INPluginRoot
    {
        #region Email global parser
        void PreEvaluation(Microsoft.Exchange.Data.Transport.MailItem obMailItem, RouteAgent.Common.EmailEvalInfoManage obEmailEvalInfoMgr, Microsoft.Exchange.Data.Transport.SmtpServer Server);
        void AdjustClassificationInfo(List<KeyValuePair<string, string>> lsHeaders, Microsoft.Exchange.Data.Transport.MailItem obMailItem, ref List<KeyValuePair<string, string>> lsClassificatioInfo);
        #endregion
    }

    public interface INLAttachmentParser : INPluginRoot
    {
        #region Attachment parser extensions
        /// <summary>
        /// Check is current plugin support this attachment analysis
        /// </summary>
        /// <param name="obAttachment">the attachment object</param>
        /// <param name="bIsNeedSaveAttachmentAsLocalFileOut">out parameter, to specify if need to save the attachment to local drive. If need process on local file or do attachment obligation with local file, this should be return true.</param>
        /// <returns>true, support, otherwise do not support</returns>
        bool IsSupportParseClassificationInfo(Attachment obAttachment, out bool bIsNeedSaveAttachmentAsLocalFileOut);
        /// <summary>
        /// Get the attachment classification info
        /// </summary>
        /// <param name="obAttachment">the attachment object</param>
        /// <param name="strAttachmentLocalFilePath">if this file path is not empty, it specify a local file path which contains the attachment content</param>
        /// <returns></returns>
        List<KeyValuePair<string, string>> GetAttachmentClassificationInfo(Attachment obAttachment, string strAttachmentLocalFilePath);
        #endregion
    }
}
