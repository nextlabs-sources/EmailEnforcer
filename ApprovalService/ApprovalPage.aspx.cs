using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using Microsoft.Exchange.WebServices.Data;
namespace ApprovalService
{
    public partial class ApprovalPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                this.divAllow.Visible = false;
                this.divDeny.Visible = false;
                this.divUnknow.Visible = false;
                this.divAutoClose.Visible = false;
                this.ErrorDiv.Visible = false;

                string strParm = Request.QueryString[Common.ConstVariable.Str_ApprovalPage_Parm];
                if (strParm != "")
                {
                    string[] arryStrParm = strParm.Split(',');
                    byte[] arryByteParm = new byte[arryStrParm.Length];
                    for (int i = 0; i < arryByteParm.Length; i++)
                    {
                        arryByteParm[i] = byte.Parse(arryStrParm[i]);
                    }
                    strParm = System.Text.Encoding.Unicode.GetString(arryByteParm);
                    string strDecode = Common.Function.Decode(strParm);
                    Common.Model.ApprovalParm ModelParm = Common.Function.JsonSerializer.LoadFromJson<Common.Model.ApprovalParm>(strDecode);
                    if (ModelParm != null)
                    {
                        string strMessageId = ModelParm.MessageId;
                        Common.Policy.CEResult CEApprovalResult = (Common.Policy.CEResult)Enum.Parse(typeof(Common.Policy.CEResult), ModelParm.ApprovalResult);
                        string strMsgFilePath = Common.Function.GetMsgFilePath(strMessageId);
                        if (System.IO.File.Exists(strMsgFilePath))
                        {
                            if (CEApprovalResult != Common.Policy.CEResult.Unknow)
                            {
                                SendMail(strParm);
                                if (CEApprovalResult.Equals(Common.Policy.CEResult.Allow))
                                {
                                    this.divAllow.Visible = true;

                                }
                                else if (CEApprovalResult.Equals(Common.Policy.CEResult.Deny))
                                {
                                    this.divDeny.Visible = true;

                                }

                                this.divAutoClose.Visible = true;
                                ClientScript.RegisterStartupScript(ClientScript.GetType(), "myscript", "<script>ClosePage();</script>");
                            }
                            else
                            {
                                this.divUnknow.Visible = true;

                                Common.Model.ApprovalParm approvalParm = new Common.Model.ApprovalParm();
                                approvalParm.MessageId = strMessageId;
                                approvalParm.Approver = "Unknow";

                                approvalParm.ApprovalResult = Common.Policy.CEResult.Allow.ToString();
                                string strAllowParm = Common.Function.JsonSerializer.SaveToJson(approvalParm);
                                strAllowParm = Common.Function.Encode(strAllowParm);
                                this.btAllow.CommandArgument = strAllowParm;

                                approvalParm.ApprovalResult = Common.Policy.CEResult.Deny.ToString();
                                string strDenyParm = Common.Function.JsonSerializer.SaveToJson(approvalParm);
                                strDenyParm = Common.Function.Encode(strDenyParm);
                                this.btDeny.CommandArgument = strDenyParm;

                            }
                        }
                        else
                        {
                            this.ErrorDiv.Visible = true;
                            this.spError.InnerText = "maybe you had finish this operator";

                        }

                    }
                    else
                    {
                        this.ErrorDiv.Visible = true;
                        this.spError.InnerText = "can not get json model , maybe parm error";
                    }
                }
                else
                {
                    this.ErrorDiv.Visible = true;
                    this.spError.InnerText = "can not get Parm!";
                }
            }
            catch (Exception ex)
            {
                ShowError("Page_Load", ex);
            }
        }
        private bool SendMail(string strBody)
        {
            bool result = false;

            ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallBack;
            string strExchangeVersion = ConfigurationManager.AppSettings[Common.ConstVariable.Str_MailServer_Version];
            string strEwsAddress = ConfigurationManager.AppSettings[Common.ConstVariable.Str_MailServer_EWSAddress];
            string strUserName = ConfigurationManager.AppSettings[Common.ConstVariable.Str_MailServer_LoginName];
            string strDomain = ConfigurationManager.AppSettings[Common.ConstVariable.Str_MailServer_LoginDomain];
            string strPassword = ConfigurationManager.AppSettings[Common.ConstVariable.Str_MailServer_LoginPassword];

            ExchangeVersion version = ExchangeVersion.Exchange2013_SP1;
            switch (strExchangeVersion)
            {
                case "2007_SP1": version = ExchangeVersion.Exchange2007_SP1; break;
                case "2010": version = ExchangeVersion.Exchange2010; break;
                case "2010_SP1": version = ExchangeVersion.Exchange2010_SP1; break;
                case "2010_SP2": version = ExchangeVersion.Exchange2010_SP2; break;
                case "2013": version = ExchangeVersion.Exchange2013; break;
                case "2013_SP1": version = ExchangeVersion.Exchange2013_SP1; break;
            }
            ExchangeService service = ConnectToService(version, new Uri(strEwsAddress), strUserName, strDomain, strPassword);
            EmailMessage message = new EmailMessage(service);

            if (!string.IsNullOrEmpty(strDomain))
            {
                message.ToRecipients.Add(new EmailAddress(strUserName + "@" + strDomain));
            }
            else
            {
                message.ToRecipients.Add(new EmailAddress(strUserName));
            }
            message.Subject = Common.ConstVariable.Str_Mail_Subject;

            StringBuilder sbBody = new StringBuilder();
            sbBody.Append(Common.ConstVariable.Str_Mail_BodyStartFlag);
            sbBody.Append(strBody);
            sbBody.Append(Common.ConstVariable.Str_Mail_BodyEndFlag);
            MessageBody body = new MessageBody(BodyType.Text, sbBody.ToString());
            message.Body = body;




            ExtendedPropertyDefinition xExperimentalHeader = new ExtendedPropertyDefinition(DefaultExtendedPropertySet.InternetHeaders,
                                                                                        Common.ConstVariable.Str_NextlabsHeader_Key,
                                                                                        MapiPropertyType.String);
            message.SetExtendedProperty(xExperimentalHeader, Common.ConstVariable.Str_MailClassify_ApprovalResult);
            message.SendAndSaveCopy(WellKnownFolderName.Outbox);



            return result;
        }

        public ExchangeService ConnectToService(ExchangeVersion version, Uri ewsAddress, string strUserName, string strDomain, string strPassword)
        {
            ExchangeService service = new ExchangeService(version);
            if (!string.IsNullOrEmpty(strDomain))
            {
                service.Credentials = new NetworkCredential(strUserName, strPassword, strDomain);
            }
            else
            {
                service.Credentials = new NetworkCredential(strUserName, strPassword);
            }
            service.Url = ewsAddress;

            return service;
        }
        private bool CertificateValidationCallBack(
         object sender,
         System.Security.Cryptography.X509Certificates.X509Certificate certificate,
         System.Security.Cryptography.X509Certificates.X509Chain chain,
         System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;

        }

        protected void btAllow_Click(object sender, EventArgs e)
        {
            try
            {
                string strParm = ((Button)sender).CommandArgument;
                SendMail(strParm);
                this.divAllow.Visible = true;
                this.divAutoClose.Visible = true;
                ClientScript.RegisterStartupScript(ClientScript.GetType(), "myscript", "<script>ClosePage();</script>");
            }
            catch (Exception ex)
            {
                ShowError("btAllow_Click", ex);
            }
        }
        private void ShowError(string strMethodName, Exception ex)
        {
            this.ErrorDiv.Visible = true;
            if (ex.Message.Contains("When making a request as an account that does not have a mailbox, you must specify the mailbox primary SMTP address for any distinguished folder Ids"))
            {
                this.spError.InnerText = "Please check you Exchange Login Name,Domain name can not Abbreviation , Can not connect to Exchange";
            }
            else if (ex.Message.Contains("The request failed. The remote server returned an error: (404) Not Found."))
            {
                this.spError.InnerText = "Please check you Exchange EWS Address, Can not connect to Exchange";
            }
            else if (ex.Message.Contains("The request failed. The remote server returned an error: (401) Unauthorized"))
            {
                this.spError.InnerText = "Please check you password ";
            }
            else
            {
                this.spError.InnerText = strMethodName + ":" + ex.Message + " " + ex.InnerException + " StackTrace:" + ex.StackTrace;
            }
        }

        protected void btDeny_Click(object sender, EventArgs e)
        {
            try
            {
                string strParm = ((Button)sender).CommandArgument;
                SendMail(strParm);
                this.divDeny.Visible = true;
                this.divAutoClose.Visible = true;
                ClientScript.RegisterStartupScript(ClientScript.GetType(), "myscript", "<script>ClosePage();</script>");
            }
            catch (Exception ex)
            {
                ShowError("btDeny_Click", ex);
            }
        }
    }
}