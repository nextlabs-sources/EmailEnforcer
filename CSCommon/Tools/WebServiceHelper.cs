using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

using CSBase.Diagnose;

namespace CSBase.Common
{
    /// <summary>
    /// Using HttpWebRequest and HttpWebResponse to invoke Web Service
    /// </summary>
    public static class WebServiceTools
    {
        #region Introduce
        /* Config
         * First the web service need support "Get", "Post", "Soap" action. We need add the following config info into the web.config file.
         * <webServices>
         *  <protocols>
         *      <add name="HttpGet"/>
         *      <add name="HttpPost"/>
         *      <add name="HttpSoap"/>
         *  </protocols>
         * </webServices>
         *
         * Invoke
         * Dictionary<string,string> dicParams = new Dictionary<string,string>(); //Hashtable, using to set the parmaters which the web service need.
         * dicParams.Add("strTest", "test");
         * dicParams.Add("bFlag", "true");
         * XmlDocument obResponse = WebSvcCaller.QuerySoapWebService("http://localhost/MyTestWeb/Services/TestService.asmx", "HelloWorld", dicParams);
         * MessageBox.Show(obResponse.OuterXml);
        */
        #endregion

        #region Const values: HTTP Method define
        public const string kstrHTTPMethodGet = "GET";
        public const string kstrHTTPMethodPost = "POST";
        #endregion

        #region Const/Read only values
        private const int knDefaultTimeOut = 5 * 60 * 1000; // 5 mins
        #endregion

        #region Static Members
        static private Dictionary<string, string> s_dicNamespaces = new Dictionary<string, string>();  // cache xmlNamespace，no need get namespace every time
        #endregion

        #region Public functions
        public static XmlDocument QueryPostWebService(String strUrl, String strMethodName, Dictionary<string, string> dicParams, string strUserAgent)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(strUrl + "/" + strMethodName);
                request.Method = kstrHTTPMethodPost;
                request.ContentType = "application/x-www-form-urlencoded";
                SetWebRequest(ref request, strUserAgent, knDefaultTimeOut);
                byte[] data = EncodePars(dicParams);
                WriteRequestData(ref request, data);
                return ReadXmlResponse(request.GetResponse());
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception in QueryPostWebService, [{0}]\n", new object[] { ex.Message });
            }
            return null;
        }
        public static XmlDocument QueryGetWebService(String strUrl, String strMethodName, Dictionary<string, string> dicParams, string strUserAgent)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(strUrl + "/" + strMethodName + "?" + ParsToString(dicParams));
                request.Method = kstrHTTPMethodGet;
                request.ContentType = "application/x-www-form-urlencoded";
                SetWebRequest(ref request, strUserAgent, knDefaultTimeOut);
                return ReadXmlResponse(request.GetResponse());
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception in QueryGetWebService, [{0}]\n", new object[] { ex.Message });
            }
            return null;
        }
        public static XmlDocument QuerySoapWebService(String strUrl, String strMethodName, Dictionary<string, string> dicParams, string strUserAgent)
        {
            try
            {
                string strXmlNs = GetXxmlNamespce(strUrl);
                if (string.IsNullOrEmpty(strXmlNs))
                {
                    return InnerQuerySoapWebService(strUrl, strMethodName, dicParams, GetNamespace(strUrl, strUserAgent), strUserAgent);
                }
                else
                {
                    return InnerQuerySoapWebService(strUrl, strMethodName, dicParams, strXmlNs, strUserAgent);
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception in QuerySoapWebService, [{0}]\n", new object[] { ex.Message });
            }
            return null;
        }
        #endregion

        #region Private functions
        static private XmlDocument InnerQuerySoapWebService(String strUrl, String strMethodName, Dictionary<string, string> htParams, string strXmlNs, string strUserAgent)
        {
            AddXmlNamespace(strUrl, strXmlNs);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(strUrl);
            request.Method = kstrHTTPMethodPost;
            request.ContentType = "text/xml; charset=utf-8";
            request.Headers.Add("SOAPAction", "\"" + strXmlNs + (strXmlNs.EndsWith("/") ? "" : "/") + strMethodName + "\"");
            SetWebRequest(ref request, strUserAgent, knDefaultTimeOut);
            byte[] data = EncodeParsToSoap(htParams, strXmlNs, strMethodName);
            WriteRequestData(ref request, data);
            XmlDocument obXMLDocFullSoapResponse = new XmlDocument();
            XmlDocument obXMLDocResponse = new XmlDocument();
            obXMLDocFullSoapResponse = ReadXmlResponse(request.GetResponse());

            XmlNamespaceManager mgr = new XmlNamespaceManager(obXMLDocFullSoapResponse.NameTable);
            mgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            String RetXml = obXMLDocFullSoapResponse.SelectSingleNode("//soap:Body/*/*", mgr).InnerXml;
            obXMLDocResponse.LoadXml("<root>" + RetXml + "</root>");
            AddDelaration(obXMLDocResponse);
            return obXMLDocResponse;
        }
        #endregion

        #region Inner tools
        static private void AddXmlNamespace(string strUrl, string strXmlNs)
        {
            lock (s_dicNamespaces)
            {
                CommonTools.AddKeyValuesToDic(s_dicNamespaces, strUrl, strXmlNs, false);
            }
        }
        static private string GetXxmlNamespce(string strUrl)
        {
            lock (s_dicNamespaces)
            {
                return CommonTools.GetValueByKeyFromDic(s_dicNamespaces, strUrl, "");
            }
        }
        static private string GetNamespace(String strUrl, string strUserAgent)
        {
            StreamReader obStreamReader = null;
            try
            {
                HttpWebRequest obNamespaceHTTPRequest = (HttpWebRequest)WebRequest.Create(strUrl + "?WSDL");
                SetWebRequest(ref obNamespaceHTTPRequest, strUserAgent, knDefaultTimeOut);
                WebResponse obResponse = obNamespaceHTTPRequest.GetResponse();
                obStreamReader = new StreamReader(obResponse.GetResponseStream(), Encoding.UTF8);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(obStreamReader.ReadToEnd());
                return doc.SelectSingleNode("//@targetNamespace").Value;
            }
            finally
            {
                if (null != obStreamReader)
                {
                    obStreamReader.Close();
                }
            }
        }
        static private byte[] EncodeParsToSoap(Dictionary<string, string> htParams, String strXmlNs, String strMethodName)
        {
            XmlDocument obXMLDocSoapBody = new XmlDocument();
            obXMLDocSoapBody.LoadXml("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"></soap:Envelope>");
            AddDelaration(obXMLDocSoapBody);
            XmlElement soapBody = obXMLDocSoapBody.CreateElement("soap", "Body", "http://schemas.xmlsoap.org/soap/envelope/");
            XmlElement soapMethod = obXMLDocSoapBody.CreateElement(strMethodName);
            soapMethod.SetAttribute("xmlns", strXmlNs);
            foreach (string k in htParams.Keys)
            {
                //XmlElement soapPar = doc.createElement_x_x(k);
                XmlElement soapPar = obXMLDocSoapBody.CreateElement(k);
                soapPar.InnerXml = ObjectToSoapXml(htParams[k]);
                soapMethod.AppendChild(soapPar);
            }
            soapBody.AppendChild(soapMethod);
            obXMLDocSoapBody.DocumentElement.AppendChild(soapBody);
            return Encoding.UTF8.GetBytes(obXMLDocSoapBody.OuterXml);
        }
        static private string ObjectToSoapXml(object obIn)
        {
            XmlSerializer mySerializer = new XmlSerializer(obIn.GetType());
            MemoryStream ms = new MemoryStream();
            mySerializer.Serialize(ms, obIn);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(Encoding.UTF8.GetString(ms.ToArray()));
            if (doc.DocumentElement != null)
            {
                return doc.DocumentElement.InnerXml;
            }
            else
            {
                return obIn.ToString();
            }
        }
        static private void SetWebRequest(ref HttpWebRequest obHTTPRequest, string strUserAgent, int nTimeOut)
        {
            obHTTPRequest.Credentials = CredentialCache.DefaultCredentials;
            obHTTPRequest.UserAgent = strUserAgent;
            obHTTPRequest.Timeout = nTimeOut;
        }
        static private void WriteRequestData(ref HttpWebRequest obHTTPRequest, byte[] data)
        {
            Stream obStreamWriter = null;
            try
            {
                obHTTPRequest.ContentLength = data.Length;
                obStreamWriter = obHTTPRequest.GetRequestStream();
                obStreamWriter.Write(data, 0, data.Length);
            }
            finally
            {
                if (null != obStreamWriter)
                {
                    obStreamWriter.Close();
                }
            }
        }
        static private byte[] EncodePars(Dictionary<string, string> dicParams)
        {
            return Encoding.UTF8.GetBytes(ParsToString(dicParams));
        }
        static private String ParsToString(Dictionary<string, string> dicParams)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string k in dicParams.Keys)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }
                sb.Append(HttpUtility.UrlEncode(k) + "=" + HttpUtility.UrlEncode(dicParams[k].ToString()));
            }
            return sb.ToString();
        }
        static private XmlDocument ReadXmlResponse(WebResponse obHTTPResponse)
        {
            StreamReader obStreamReader = null;
            try
            {
                obStreamReader = new StreamReader(obHTTPResponse.GetResponseStream(), Encoding.UTF8);
                String strRetXml = obStreamReader.ReadToEnd();
                CSLogger.OutputLog(LogLevel.Debug, "Service response info:[{0}]\n", new object[] { strRetXml });
                obStreamReader.Close();
                XmlDocument obXMLDocResponse = new XmlDocument();
                obXMLDocResponse.LoadXml(strRetXml);
                return obXMLDocResponse;
            }
            finally
            {
                if (null != obStreamReader)
                {
                    obStreamReader.Close();
                }
            }
        }
        static private void AddDelaration(XmlDocument obXMLDocIn)
        {
            XmlDeclaration decl = obXMLDocIn.CreateXmlDeclaration("1.0", "utf-8", null);
            obXMLDocIn.InsertBefore(decl, obXMLDocIn.DocumentElement);
        }
        #endregion
    }
}
