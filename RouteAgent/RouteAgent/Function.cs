using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Exchange.Data.Transport;
using Microsoft.Exchange.Data.Transport.Email;
using Microsoft.Exchange.Data.Mime;
using System.IO;
using System.Diagnostics;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.CustomProperties;
using DocumentFormat.OpenXml.CustomXmlSchemaReferences;
using DocumentFormat.OpenXml.VariantTypes;
using System.Reflection;
using System.Security.Cryptography;
using System.Runtime.Serialization.Json;
using System.Web;
using System.Configuration;
using System.DirectoryServices;
using System.Security.Principal;
using CSBase.Diagnose;

namespace RouteAgent.Common
{
    public enum FileType : int
    {
        WORD,
        EXCEL,
        POWERPOINT,
        Nextlabs,
        OTHER
    }
    public enum PropertyTypes : int
    {
        YesNo,
        Text,
        DateTime,
        NumberInteger,
        NumberDouble
    }

    public class Function
    {
        public static bool NeedNotifyToAdmin()
        {
            bool bresult = true;
            string strNotifyWhenException = Config.NotifyWhenException;
            if (!string.IsNullOrEmpty(strNotifyWhenException))
            {
                if (strNotifyWhenException.Equals(Common.ConstVariable.Str_NO, StringComparison.OrdinalIgnoreCase))
                {
                    bresult = false;
                }
            }
            return bresult;
        }
        public static void DoExceptionNotify(object ex, SmtpServer Server, MailItem obMailItem)
        {
            if (NeedNotifyToAdmin())
            {
                if (!string.IsNullOrEmpty(Common.Config.EmailNotifyObligatinSenderName) &&
                    !string.IsNullOrEmpty(Common.Config.EmailNotifyObligatinSenderEmailAddress) &&
                    !string.IsNullOrEmpty(Common.Config.ExceptionNotifyTo) &&
                    !string.IsNullOrEmpty(Config.ExceptionNotifyAttachOriginEmail))
                {
                    EmailRecipient emailSender = new EmailRecipient(Common.Config.EmailNotifyObligatinSenderName, Common.Config.EmailNotifyObligatinSenderEmailAddress);
                    EmailRecipient emailRecipients = new EmailRecipient(Common.Config.ExceptionNotifyTo, Common.Config.ExceptionNotifyTo);
                    string strSubject = Common.Config.ExceptionNotifySubject;
                    string strBody = Common.Config.ExceptionNotifyBody;
                    if (ex is Exception)
                    {
                        Exception exception = ex as Exception;

                        string[] strArry = strBody.Split('%');

                        StringBuilder sbBody = new StringBuilder();
                        for (int i = 0; i < strArry.Length; i++)
                        {
                            if (i == 0)
                            {
                                sbBody.Append(strArry[0]);
                            }
                            else
                            {
                                if (strArry[i].Length > 0)
                                {
                                    if (char.ToLower(strArry[i][0]).CompareTo(ConstVariable.Char_Email_Subject_Split) == 0)
                                    {
                                        sbBody.Append(obMailItem.Message.Subject);
                                        sbBody.Append(strArry[i].Substring(1, strArry[i].Length - 1));
                                    }
                                    else if (char.ToLower(strArry[i][0]).CompareTo(ConstVariable.Char_Email_From_Split) == 0)
                                    {
                                        sbBody.Append(obMailItem.Message.From.SmtpAddress);
                                        sbBody.Append(strArry[i].Substring(1, strArry[i].Length - 1));
                                    }
                                    else if (char.ToLower(strArry[i][0]).CompareTo(ConstVariable.Char_Email_Exception_Split) == 0)
                                    {
                                        sbBody.Append(exception.Message + " " + exception.InnerException + " StackTrace:" + exception.StackTrace);
                                        sbBody.Append(strArry[i].Substring(1, strArry[i].Length - 1));
                                    }
                                    else if (char.ToLower(strArry[i][0]).CompareTo(ConstVariable.Char_Email_Recipients_Split) == 0)
                                    {
                                        string strRecipient = "</br>";
                                        foreach (var recipient in obMailItem.Recipients)
                                        {
                                            strRecipient += recipient.Address.ToString();
                                            strRecipient += "</br>";
                                        }
                                        sbBody.Append(strRecipient);
                                        sbBody.Append(strArry[i].Substring(1, strArry[i].Length - 1));
                                    }
                                    else
                                    {
                                        sbBody.Append("%" + strArry[i]);
                                    }
                                }
                            }
                        }
                        strBody = sbBody.ToString();
                    }
                    else
                    {
                        strBody = ex.ToString();
                    }
                    string strAttachOrigEmail = Config.ExceptionNotifyAttachOriginEmail;

                    SendEmail(obMailItem, emailSender , new List<EmailRecipient>() { emailRecipients }, strSubject, strBody, strAttachOrigEmail,Server,true);
                }
            }
        }
        public static void SendEmail(MailItem obMailItem,EmailRecipient senderRecipients, List<EmailRecipient> denyRecipients, string strSubject, string strBody, string strAttachOrigEmail, SmtpServer Server, bool bException = false)
        {
            NotifyObligation notifyOb = new NotifyObligation(obMailItem.Message, new List<EmailRecipient>() { senderRecipients }, strSubject, strBody, strAttachOrigEmail);
            foreach(var recipient in denyRecipients)
            {
                notifyOb.DenyRecipients.Add(recipient.SmtpAddress);
            }
            notifyOb.DoObligation(Server);
        }

        public static void AddEmailHeader(string strName, string strValue, EmailMessage email, bool bOverWrite = true)
        {
            MimeDocument mdMimeDoc = email.MimeDocument;
            HeaderList headers = mdMimeDoc.RootPart.Headers;
            Microsoft.Exchange.Data.Mime.Header header = headers.FindFirst(strName);
            if (header == null)
            {
                // Add a custom header
                TextHeader nhNewHeader = new TextHeader(strName, strValue);
                mdMimeDoc.RootPart.Headers.InsertAfter(nhNewHeader, mdMimeDoc.RootPart.Headers.LastChild);
            }
            else
            {
                if(bOverWrite)
                {
                    header.Value = strValue;
                }
                else
                {
                    string strValueSplit = Config.EmailHeaderMultiValueSplit;
                    if(string.IsNullOrWhiteSpace(strValueSplit))
                    {
                        strValueSplit = ConstVariable.EmailHeaderMultiValueSplitDefValue;
                    }
                    header.Value += strValueSplit + strValue;
                }
            }
        }

        public static void RemoveEmailHeader(string strName,EmailMessage email)
        {
            MimeDocument mdMimeDoc = email.MimeDocument;
            HeaderList headers = mdMimeDoc.RootPart.Headers;
            Microsoft.Exchange.Data.Mime.Header header = headers.FindFirst(strName);
            if (header != null)
            {
                headers.RemoveAll(header.Name);
            }
        }

        public static string FindEmailHeaderValue(string strName, EmailMessage email)
        {
            Microsoft.Exchange.Data.Mime.Header header = email.MimeDocument.RootPart.Headers.FindFirst(strName);
            if (header != null)
            {
                return header.Value;
            }
            else
            {
                return null;
            }
        }

        public static string GetTempDirectory()
        {
            string strTempPath = System.IO.Path.GetTempPath();

            if (strTempPath[strTempPath.Length - 1] != '\\')
            {
                strTempPath += '\\';
            }

            return strTempPath;

        }





        public static List<EnvelopeRecipient> GetAllRecipients(MailItem mailItem)
        {
            List<EnvelopeRecipient> lstRecipient = new List<EnvelopeRecipient>(mailItem.Recipients.Count);

            foreach (EnvelopeRecipient recipient in mailItem.Recipients)
            {
                lstRecipient.Add(recipient);
            }

            return lstRecipient;
        }

        public static List<string> GetAllRecipientsToStr(MailItem mailItem)
        {
            List<string> lstRecipient = new List<string>(mailItem.Recipients.Count);

            foreach (EnvelopeRecipient recipient in mailItem.Recipients)
            {
                lstRecipient.Add(recipient.Address.ToString());
            }

            return lstRecipient;
        }

        public static string GetFileSuffix(string strFileName)
        {
            string strSuffix = string.Empty;
            int nPos = strFileName.LastIndexOf('.');
            if (nPos >= 0)
            {
                strSuffix = strFileName.Substring(nPos + 1);
            }
            return strSuffix;
        }
        /// <summary>
        /// Only support office 2007 file
        /// </summary>
        /// <param name="strFileName"></param>
        /// <returns></returns>
        public static FileType GetFileType(string strFileName)
        {
            List<string> wordSuffixs = new List<string>(new string[] { "docx", "docm", "dotx", "dotm" });
            List<string> excelSuffixs = new List<string>(new string[] { "xlsx", "xlsb", "xlam", "xlsm", "xltm", "xltx" });
            List<string> pptSuffixs = new List<string>(new string[] { "pptx", "pptm", "potx", "potm", "ppsx", "ppsm", "ppam" });
            List<string> nextlabsSuffixs = new List<string>(new string[] { "nxl" });
            //get file suffix
            string strSuffix = GetFileSuffix(strFileName);

            if (wordSuffixs.Contains(strSuffix))
            {
                return FileType.WORD;
            }
            else if (excelSuffixs.Contains(strSuffix))
            {
                return FileType.EXCEL;
            }
            else if (pptSuffixs.Contains(strSuffix))
            {
                return FileType.POWERPOINT;
            }
            else if (nextlabsSuffixs.Contains(strSuffix))
            {
                return FileType.Nextlabs;
            }
            else
            {
                return FileType.OTHER;
            }
        }

        public static string SetCustomProperty(string strFilePath, FileType ft, Dictionary<string, string> lstProps, PropertyTypes propertyType)
        {
            Trace.WriteLine("SetCustomProperty Start:" + strFilePath);
            if (ft != FileType.WORD && ft != FileType.EXCEL && ft != FileType.POWERPOINT)
            {
                Trace.WriteLine("Return ");
                return string.Empty;
            }
            // Given a document name, a property name/value, and the property type,
            // add a custom property to a document. The method returns the original
            // value, if it existed.
            string returnValue = null;
            if (ft == FileType.WORD)
            {
                //using (FileStream fs = new FileStream(strFilePath, FileMode.Open, FileAccess.ReadWrite))
                //{
                Trace.WriteLine("File type is word");
                var document = WordprocessingDocument.Open(strFilePath, true);
                Trace.WriteLine("open Document");
                var customProps = document.CustomFilePropertiesPart;
                Trace.WriteLine("Get CustomProps");
                if (customProps == null)
                {
                    Trace.WriteLine("Custom Props is null");
                    // No custom properties? Add the part, and the
                    // collection of properties now.
                    customProps = document.AddCustomFilePropertiesPart();
                    Trace.WriteLine("AddCustomFilePropertiesPart");
                    customProps.Properties = new DocumentFormat.OpenXml.CustomProperties.Properties();
                    Trace.WriteLine("new Properties");
                }

                Properties props = customProps.Properties;
                AddProperties(props, lstProps);
                document.Close();
                //}
            }
            else if (ft == FileType.EXCEL)
            {
                var document = SpreadsheetDocument.Open(strFilePath, true);

                var customProps = document.CustomFilePropertiesPart;
                if (customProps == null)
                {
                    // No custom properties? Add the part, and the
                    // collection of properties now.
                    customProps = document.AddCustomFilePropertiesPart();
                    customProps.Properties =
                        new DocumentFormat.OpenXml.CustomProperties.Properties();
                }

                Properties props = customProps.Properties;
                AddProperties(props, lstProps);
                document.Close();
            }
            else if (ft == FileType.POWERPOINT)
            {
                var document = PresentationDocument.Open(strFilePath, true);

                var customProps = document.CustomFilePropertiesPart;
                if (customProps == null)
                {
                    // No custom properties? Add the part, and the
                    // collection of properties now.
                    customProps = document.AddCustomFilePropertiesPart();
                    customProps.Properties =
                        new DocumentFormat.OpenXml.CustomProperties.Properties();
                }

                Properties props = customProps.Properties;
                AddProperties(props, lstProps);
                document.Close();
            }
            Trace.WriteLine("SetCustomProperty End:" + strFilePath);
            return returnValue;
        }

        private static void AddProperties(Properties props, Dictionary<string, string> lstProps)
        {
            Trace.WriteLine("AddProperties Start");
            foreach (KeyValuePair<string, string> PropItem in lstProps)
            {
                CustomDocumentProperty newProp = CreateCustomDocProperty(PropItem.Value, PropertyTypes.Text);

                // Now that you have handled the parameters, start
                // working on the document.
                newProp.FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}";
                newProp.Name = PropItem.Key;
                Trace.WriteLine("PropItem Key:" + PropItem.Key + " PropItem Value" + PropItem.Value);
                AppendNewProperty(props, newProp);

            }
            Trace.WriteLine("AddProperties End");
        }
        private static CustomDocumentProperty CreateCustomDocProperty(object propertyValue, PropertyTypes propertyType)
        {
            var newProp = new CustomDocumentProperty();
            bool propSet = false;

            // Calculate the correct type.
            switch (propertyType)
            {
                case PropertyTypes.DateTime:

                    // Be sure you were passed a real date,
                    // and if so, format in the correct way.
                    // The date/time value passed in should
                    // represent a UTC date/time.
                    if ((propertyValue) is DateTime)
                    {
                        newProp.VTFileTime =
                            new VTFileTime(string.Format("{0:s}Z",
                                Convert.ToDateTime(propertyValue)));
                        propSet = true;
                    }

                    break;

                case PropertyTypes.NumberInteger:
                    if ((propertyValue) is int)
                    {
                        newProp.VTInt32 = new VTInt32(propertyValue.ToString());
                        propSet = true;
                    }

                    break;

                case PropertyTypes.NumberDouble:
                    if (propertyValue is double)
                    {
                        newProp.VTFloat = new VTFloat(propertyValue.ToString());
                        propSet = true;
                    }

                    break;

                case PropertyTypes.Text:
                    newProp.VTLPWSTR = new VTLPWSTR(propertyValue.ToString());
                    propSet = true;

                    break;

                case PropertyTypes.YesNo:
                    if (propertyValue is bool)
                    {
                        // Must be lowercase.
                        newProp.VTBool = new VTBool(
                          Convert.ToBoolean(propertyValue).ToString().ToLower());
                        propSet = true;
                    }
                    break;
            }

            if (!propSet)
            {
                // If the code was not able to convert the
                // property to a valid value, throw an exception.
                throw new InvalidDataException("propertyValue");
            }

            return newProp;
        }
        private static void AppendNewProperty(Properties props, CustomDocumentProperty newProp)
        {
            Trace.WriteLine("AppendNewProperty Start");
            if (props != null)
            {
                Trace.WriteLine("props !=null");
                // This will trigger an exception if the property's Name
                // property is null, but if that happens, the property is damaged,
                // and probably should raise an exception.
                var prop = default(CustomDocumentProperty);
                foreach (CustomDocumentProperty tempProp in props)
                {
                    Trace.WriteLine("tempProp.Name.Value:" + tempProp.Name.Value);
                    if (tempProp.Name.Value == newProp.Name)
                    {
                        prop = tempProp;
                        break;
                    }
                }

                // Does the property exist? If so, get the return value,
                // and then delete the property.
                if (prop != null)
                {
                    Trace.WriteLine("prop!=null");
                    prop.Remove();
                    Trace.WriteLine("prop.Remove()");
                }

                // Append the new property, and
                // fix up all the property ID values.
                // The PropertyId value must start at 2.
                props.AppendChild(newProp);
                Trace.WriteLine("props.AppendChild(newProp)");
                int pid = 2;
                foreach (CustomDocumentProperty item in props)
                {
                    item.PropertyId = pid++;
                }
                props.Save();
                Trace.WriteLine("props.Save()");
            }
            Trace.WriteLine("AppendNewProperty End");
        }

        public static EmailInfo GetEmailInfo(string strContentType, List<EmailInfo> lisEmailInfos)
        {
            EmailInfo emailInfo = null;
            foreach (EmailInfo TempEmailInfo in lisEmailInfos)
            {
                if (TempEmailInfo.strContentType.Equals(strContentType))
                {
                    emailInfo = TempEmailInfo;
                    break;
                }
            }
            return emailInfo;
        }
        /// <summary>
        /// Get Email Infos by content type
        /// </summary>
        /// <param name="strContentType">Common.Policy</param>
        /// <param name="lisEmailInfos"></param>
        /// <returns></returns>
        public static List<EmailInfo> GetEmailInfos(string strContentType, List<EmailInfo> lisEmailInfos)
        {
            List<EmailInfo> lisResultEmailInfos = new List<EmailInfo>();
            foreach (EmailInfo TempEmailInfo in lisEmailInfos)
            {
                if (TempEmailInfo.strContentType.Equals(strContentType))
                {
                    lisResultEmailInfos.Add(TempEmailInfo);
                }
            }
            return lisResultEmailInfos;
        }

        public static List<PolicyResult> GetPolicyResult(string strContentType, List<PolicyResult> lisPolicyResults)
        {
            List<PolicyResult> lisResult = new List<PolicyResult>();
            foreach (PolicyResult pr in lisPolicyResults)
            {
                if (pr.emailInfo.strContentType.Equals(strContentType))
                {
                    lisResult.Add(pr);
                }
            }
            return lisResult;
        }


        public static bool EqualTag(Dictionary<string, string> dirFileTag, Dictionary<string, string> dirObligationTag)
        {
            if (dirFileTag == null && dirObligationTag == null)
            {
                return true;
            }
            else if (dirFileTag == null && dirObligationTag != null)
            {
                if (dirObligationTag.Count > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else if (dirFileTag != null && dirObligationTag == null)
            {
                if (dirFileTag.Count > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (dirFileTag.Count != dirObligationTag.Count)
                {
                    return false;
                }
                else
                {
                    foreach (var itemFile in dirFileTag)
                    {
                        bool bKeyExits = false;
                        foreach (var itemOb in dirObligationTag)
                        {
                            if (itemFile.Key.Equals(itemOb.Key, StringComparison.OrdinalIgnoreCase))
                            {

                                if (itemFile.Value.Equals(itemOb.Value, StringComparison.OrdinalIgnoreCase))
                                {
                                    bKeyExits = true;
                                    break;

                                }
                            }
                        }
                        if (!bKeyExits)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }


        }

        public static string GetApplicationFilePath()
        {
            Assembly exeAssembly = System.Reflection.Assembly.GetEntryAssembly();
            if (null != exeAssembly)
            {
                string codeBase = exeAssembly.CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return path;
            }
            else
            {
                return string.Empty;
            }
        }

        public static string GetExchangeEnforcerInstallPath()
        {
            try
            {
                Microsoft.Win32.RegistryKey keyEERoot = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(ConstVariable.Str_EERegisterPath, false);
                string strPath = keyEERoot.GetValue("InstallDir") as string;
                keyEERoot.Close();
                return strPath;
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception on GetExchangeEnforcerInstallPath." + ex.ToString());
            }

            return ConstVariable.Str_EEDefaultInstallPath;
        }

        //public static string GetApprovalUrl(string strMessageId)
        //{
        //    Common.Model.ApprovalParm parm = new Model.ApprovalParm();
        //    parm.MessageId = strMessageId;
        //    parm.ApprovalResult = Common.Policy.CEResult.Allow.ToString();
        //    parm.Approver = "Unknow";
        //    string strJson= Function.JsonSerializer.SaveToJson(parm);
        //    string strEncodeResult= Encode(strJson);
        //    byte[] byteEncode = System.Text.Encoding.Unicode.GetBytes(strEncodeResult);
        //    strEncodeResult = string.Join(",", byteEncode);
        //    string strResult = Config.ApprovalService + ConstVariable.Str_ApprovalPage_Name + "?" + ConstVariable.Str_ApprovalPage_Parm+"=" + strEncodeResult;
        //    return strResult;
        //}
        //public static string GetNotApprovalUrl(string strMessageId)
        //{
        //    Common.Model.ApprovalParm parm = new Model.ApprovalParm();
        //    parm.MessageId = strMessageId;
        //    parm.ApprovalResult = Common.Policy.CEResult.Deny.ToString();
        //    parm.Approver = "Unknow";
        //    string strJson = Function.JsonSerializer.SaveToJson(parm);
        //    string strEncodeResult = Encode(strJson);
        //    byte[] byteEncode = System.Text.Encoding.Unicode.GetBytes(strEncodeResult);
        //    strEncodeResult = string.Join(",", byteEncode);
        //    string strResult = Config.ApprovalService + ConstVariable.Str_ApprovalPage_Name + "?" + ConstVariable.Str_ApprovalPage_Parm + "=" + strEncodeResult;
        //    return strResult;
        //}
        public static string GetApprovalInfoUrl(string strMessageId, Policy.CEResult approvalInfo)
        {
            RouteAgent.Common.Model.ApprovalParm parm = new Model.ApprovalParm();
            parm.MessageId = strMessageId;
            parm.ApprovalResult = approvalInfo.ToString();
            parm.Approver = "Unknow";
            string strJson = Function.JsonSerializer.SaveToJson(parm);
            string strEncodeResult = Encode(strJson);
            byte[] byteEncode = System.Text.Encoding.Unicode.GetBytes(strEncodeResult);
            strEncodeResult = string.Join(",", byteEncode);
            string strResult = Config.ApprovalService + ConstVariable.Str_ApprovalPage_Name + "?" + ConstVariable.Str_ApprovalPage_Parm + "=" + strEncodeResult;
            return strResult;
        }
        public static string SimpleEncode(string data)
        {
            string result = string.Empty;
            result = data.Substring(1, data.Length - 2);
            return result;
        }
        public static string SimpleDecode(string data)
        {
            string resuly = string.Empty;
            resuly = "<" + data + ">";
            return resuly;
        }

        public static byte[] GetByteFormStream(Stream stream)
        {
            List<byte> lisBytes = new List<byte>();
            byte[] byteBody = new byte[ConstVariable.Int_ReadLenOneTimeBody];
            while (true)
            {
                int nReadLen = stream.Read(byteBody, 0, ConstVariable.Int_ReadLenOneTimeBody);
                lisBytes.AddRange(byteBody);
                if (nReadLen < ConstVariable.Int_ReadLenOneTimeBody)
                {
                    break;
                }
            }
            return lisBytes.ToArray();
        }

        #region


        public static string Encode(string data)
        {
            CSLogger.OutputLog(LogLevel.Debug, "Encode------------------------Source:" + data);
            string strResult = string.Empty;

            byte[] byKey = System.Text.ASCIIEncoding.ASCII.GetBytes(ConstVariable.Str_SimpleEncrypt_Key);
            byte[] byIV = System.Text.ASCIIEncoding.ASCII.GetBytes(ConstVariable.Str_SimpleEncrypt_Iv);
            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            int i = cryptoProvider.KeySize;
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream();
                {
                    CryptoStream cst = null;
                    try
                    {
                        cst = new CryptoStream(ms, cryptoProvider.CreateEncryptor(byKey, byIV), CryptoStreamMode.Write);
                        ms = null;
                        using (StreamWriter sw = new StreamWriter(cst))
                        {
                            cst = null;
                            sw.Write(data);
                            sw.Flush();
                            cst.FlushFinalBlock();
                            sw.Flush();
                            strResult = Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
                        }

                    }
                    finally
                    {
                        if (cst != null)
                        {
                            cst.Dispose();
                        }
                    }

                }
            }
            finally
            {
                if(ms!=null)
                {
                    ms.Dispose();
                }
            }
            CSLogger.OutputLog(LogLevel.Debug, "Encode------------------------Result:" + strResult);
            return strResult;
        }

        public static string Decode(string data)
        {
            CSLogger.OutputLog(LogLevel.Debug, "Decode------------------------Source:" + data);
            string strResult = string.Empty;
            byte[] byKey = System.Text.ASCIIEncoding.ASCII.GetBytes(ConstVariable.Str_SimpleEncrypt_Key);
            byte[] byIV = System.Text.ASCIIEncoding.ASCII.GetBytes(ConstVariable.Str_SimpleEncrypt_Iv);
            byte[] byEnc;
            try
            {
                byEnc = Convert.FromBase64String(data);
            }
            catch
            {
                return strResult;
            }
            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream(byEnc);
                CryptoStream cst = null;
                try
                {
                    cst = new CryptoStream(ms, cryptoProvider.CreateDecryptor(byKey, byIV), CryptoStreamMode.Read);
                    ms = null;
                    using (StreamReader sr = new StreamReader(cst))
                    {
                        cst = null;
                        strResult = sr.ReadToEnd();
                    }
                }
                finally
                {
                    if (cst != null)
                    {
                        cst.Dispose();
                    }
                }
            }
            finally
            {
                if (ms != null)
                {
                    ms.Dispose();
                }
            }
            CSLogger.OutputLog(LogLevel.Debug, "Decode------------------------Result:" + strResult);
            return strResult;

        }
        #endregion
        public static string GetMsgFilePath(string strMessageId)
        {
            string strFilePath = Config.EmailEnforceInstalledPath;
            if (strFilePath.Equals("\\"))
            {
                strFilePath = ConfigurationManager.AppSettings["EmailEnforceInstalledPath"];
                if (!strFilePath.EndsWith("\\"))
                {
                    strFilePath += "\\";
                }
            }
            strFilePath += RouteAgent.Common.ConstVariable.Str_AppSetting_CacheFolder + "\\";
            strFilePath += Function.SimpleEncode(strMessageId);
            return strFilePath;
        }



        public static List<RouteAgent.Common.PolicyResult> GetRecipientPolicyResultFromGroupInfo(SmtpServer server, List<GroupInfo> lisGroupInfo, EnvelopeRecipient recipient)
        {
            List<RouteAgent.Common.PolicyResult> lisPolicyResult = null;
            foreach (GroupInfo gropuInfo in lisGroupInfo)
            {
                if (server.AddressBook.IsMemberOf(recipient.Address, gropuInfo.Address))
                {
                    if (lisPolicyResult == null)
                    {
                        //lisPolicyResult = new List<PolicyResult>();
                        lisPolicyResult = gropuInfo.PolicyResults;
                    }
                    else
                    {
                        lisPolicyResult = MergePolicyResultList(lisPolicyResult, gropuInfo.PolicyResults);
                    }
                }
            }
            return lisPolicyResult;
        }

        public static bool RecipientInGroup(SmtpServer server, RoutingAddress recipient, List<GroupInfo> lisgroup)
        {
            bool bresult = false;
            foreach (GroupInfo group in lisgroup)
            {
                if (server.AddressBook.IsMemberOf(recipient, group.Address))
                {
                    bresult = true;
                    break;
                }
            }
            return bresult;
        }

        public static List<PolicyResult> MergePolicyResultList(List<PolicyResult> source, List<PolicyResult> dest)
        {
            List<PolicyResult> lisResult = null;
            if (source != null && dest != null)
            {
                lisResult = new List<PolicyResult>();
                if (source.Count == dest.Count)
                {
                    for (int i = 0; i < source.Count; i++)
                    {
                        for (int j = 0; j < dest.Count; j++)
                        {
                            if (source[i].emailInfo.strSavedPath.Equals(dest[j].emailInfo.strSavedPath, StringComparison.OrdinalIgnoreCase))
                            {
                                if (source[i].bDeny.Equals(dest[j].bDeny))
                                {
                                    List<ExchangeObligation> lisObs = null;
                                    if (source[i].lstExchangeObligations != null)
                                    {
                                        if (lisObs == null)
                                        {
                                            lisObs = new List<ExchangeObligation>();
                                        }
                                        lisObs.AddRange(source[i].lstExchangeObligations);
                                    }
                                    if (dest[j].lstExchangeObligations != null)
                                    {
                                        if (lisObs == null)
                                        {
                                            lisObs = new List<ExchangeObligation>();
                                        }
                                        lisObs.AddRange(dest[j].lstExchangeObligations);
                                    }
                                    PolicyResult policyResult = new PolicyResult();
                                    policyResult.bDeny = source[i].bDeny;
                                    policyResult.emailInfo = source[i].emailInfo;
                                    policyResult.lstExchangeObligations = lisObs;
                                    lisResult.Add(policyResult);

                                }
                            }
                        }
                    }
                }
            }
            return lisResult;
        }

        //public static List<ExchangeObligation> MergeExchangeObligation(List<ExchangeObligation> source,List<ExchangeObligation> dest)
        //{
        //    List<ExchangeObligation> lisResult = new List<ExchangeObligation>();
        //    for(int i=0;i<source.Count;i++)
        //    {
        //        for(int j=0;j<dest.Count;j++)
        //        {
        //            if(source[i].)
        //        }
        //    }

        //}


        public static List<string> GetGroupAddress(EnvelopeRecipient recipient, List<GroupInfo> groupInfos)
        {
            List<string> lisAddress = new List<string>();
            foreach (var groupInfo in groupInfos)
            {
                if (groupInfo.Address.CompareTo(recipient.Address) == 0)
                {
                    lisAddress.Add(groupInfo.Address.ToString());
                    break;
                }
            }
            return lisAddress;
        }






        public static class JsonSerializer
        {
            public static string SaveToJson(object struceJson)
            {
                string result = string.Empty;
                try
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(struceJson.GetType());
                    MemoryStream stream = new MemoryStream();
                    serializer.WriteObject(stream, struceJson);
                    byte[] dataBytes = new byte[stream.Length];
                    stream.Position = 0;
                    stream.Read(dataBytes, 0, (int)stream.Length);
                    result = Encoding.Default.GetString(dataBytes);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("ERROR Method:SaveToJson,struceJson type:" + struceJson.GetType());
                    System.Diagnostics.Trace.WriteLine("Exception:" + ex.Message + " StackTrace:" + ex.StackTrace);
                }
                return result;
            }
            public static T LoadFromJson<T>(string strJson)
            {
                T read = default(T);
                try
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                    MemoryStream mStream = new MemoryStream(Encoding.Default.GetBytes(strJson));
                    read = (T)serializer.ReadObject(mStream);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("ERROR Method:LoadFromJson,parm:strJson" + strJson + " T type:" + typeof(T));
                    System.Diagnostics.Trace.WriteLine("Exception:" + ex.Message + " StackTrace:" + ex.StackTrace);
                }
                return read;
            }
        }
    }

}
