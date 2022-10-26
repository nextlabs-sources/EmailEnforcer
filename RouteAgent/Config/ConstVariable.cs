using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RouteAgent.Common
{
    public class ConstVariable
    {
        public const string Str_NextlabsHeader_Key = "nxtype";
        public const string Str_MailClassify_DenyNotiy = "denynotify";
        public const string Str_MailClassify_Enforced = "enforced";
        public const string Str_MailClassify_ApprovalMail = "approvalmail";
        public const string Str_MailClassify_ApprovalResult = "approvalresult";
        public const string Str_MailClassify_MAPIMsgClsSubmitLamProbe = "IPM.Note.MapiSubmitLAMProbe";
        public const string Str_MailClassify_MAPIMsgClsReportNoteNDR = "Report.IPM.Note.NDR";
        public const string Str_MailClassify_MAPITASK = "IPM.TaskRequest";
        public const string Str_MailClassify_MAPIMEETING = "IPM.Schedule.Meeting.Request";

        public const string Str_EmailFile_Extension = ".txt";
        public const string Str_NextlabsFile_Extension = ".nxl";
        public const string Str_EmailFile_Prefix_Sunbject = "Subject_";
        public const string Str_EmailFile_Prefix_Body = "Body_";
        public const string Str_EmailFile_Prefix_Attach = "Attach_";
        public const int Int_ReadLenOneTimeBody = 1024;
        public const int Int_ReadLenOneTimeAttach = 1024 * 10;

        public const string Str_SimpleEncrypt_Key = "Moreqian";
        public const string Str_SimpleEncrypt_Iv = "Moreqian";

        public const string Str_ApprovalPage_Name = "ApprovalPage.aspx";
        public const string Str_ApprovalPage_Parm = "Parm";

        public const string Str_MailServer_Version = "ExchangeVersion";
        public const string Str_MailServer_EWSAddress = "ExchangeEWSAddress";
        public const string Str_MailServer_LoginName = "MailServerLoginName";
        public const string Str_MailServer_LoginDomain = "MailServerLoginDomain";
        public const string Str_MailServer_LoginPassword = "MailServerLoginPassword";
        public const int Int_MailServer_DefaultPort = 25;

        public const string Str_Mail_Subject = "ApprovalResult";
        public const string Str_Mail_BodyStartFlag = "THISISNEXTLABSBODYSTART";
        public const string Str_Mail_BodyEndFlag = "THISISNEXTLABSBODYEND";

        public const string Str_AppSetting_CacheFolder = "CacheFile";

        public const string Str_EERegisterPath = @"SOFTWARE\NextLabs\Compliant Enterprise\Exchange Enforcer";
        public const string Str_EEDefaultInstallPath = @"C:\Program Files\NextLabs\Exchange Enforcer\";

        public const string Str_BehaviorOnException_Block = "block";
        public const string Str_BehaviorOnException_Allow = "Allow";

        public const string Str_YES = "yes";
        public const string Str_NO = "no";

        public const string Str_Email_Header_Format_Split = "%c";
        public const string Str_Email_From_Split = "%f";
        public const char Char_Email_From_Split = 'f';
        public const string Str_Email_Subject_Split = "%s";
        public const char Char_Email_Subject_Split = 's';
        public const string Str_Email_Recipients_Split = "%t";
        public const char Char_Email_Recipients_Split = 't';
        public const string Str_Email_Exception_Split = "%e";
        public const char Char_Email_Exception_Split = 'e';
        public const string str_Configuration_Section_ClassificationMap = "classificationMap";
        public const string Str_Configuration_Section_Name_WhiteList = "whiteList";
        public const string Str_Configuration_Section_Name_SupportExtensionNames = "supportExtensionNames";

        public const string Str_Attribute_Name_ClientType = "ClientType";
        public const string Str_Attribute_Name_NoCache = "ce::nocache";

        public const string Str_Attribute_Name_FileSystemCheck = "ce::filesystemcheck";

        public const string Str_Attribute_Nmae_FSO = "fso";

        public const int Int_MultipleQueryLimite_Default = 200;

        public const int Int_RecipientsLimited_Default = 5000;

        public const string Str_PolicyHeader_Key = "nlpolicy";

        public const char Char_Support_Header_Key_Split = ';';

        public const string EmailHeaderMultiValueSplitDefValue = ",";
        public const char RMSClassifyMultiValueSplit = ',';
     

    }
}
