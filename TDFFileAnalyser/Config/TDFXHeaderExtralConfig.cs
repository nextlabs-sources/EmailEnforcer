using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace Nextlabs.TDFFileAnalyser
{
    public enum EMNode_Element
    {
        Unknown,

        WholeNode,
        Attribute,
        Value,
    }

    public class TDFXHeaderExtralConfig
    {
        #region XML values
        static public readonly string kstrXmlNode_TDFConfig = "TDFConfig";
        static private readonly string kstrXmlNode_MaxStartEffectiveXHeaderLength = "MaxStartEffectiveXHeaderLength";
        static private readonly string kstrXmlNode_MaxEndEffectiveXHeaderLength = "MaxEndEffectiveXHeaderLength";
        static private readonly string kstrXmlNode_IgnoreNodes = "IgnoreNodes";
        static private readonly string kstrXmlNode_IgnoreNode = "IgnoreNode";
        static private readonly string kstrXmlNode_SupportExtensions = "SupportExtensions";

        static private readonly string kstrXmlAttr_Default = "default";
        static private readonly string kstrXmlAttr_Name = "name";
        static private readonly string kstrXmlAttr_Element = "element";

        static private readonly string kstrValue_Enpty = "empty";
        static private readonly string kstrValue_Star = "*";

        static private readonly char kchSepExtension = ',';
        #endregion

        #region Default config info
        static private int knDefaultMaxStartEffectiveXHeaderLength = 2 * 1024 * 1024;
        static private int knDefaultMaxEndEffectiveXHeaderLength = 1024;
        static private Dictionary<string, EMNode_Element> kdicDefaultIgnoreNode = new Dictionary<string, EMNode_Element>() { { "tdf:Base64BinaryPayload", EMNode_Element.Value } };
        static private HashSet<string> ksetDefaultSupportExtension = new HashSet<string>() { ".xml" };
        #endregion

        // 配置文件从 Config xml 中读取, 独立 node
        #region Fields
        public int MaxStartEffectiveXHeaderLength { get { return m_nMaxStartEffectiveXHeaderLength; } }
        public int MaxEndEffectiveXHeaderLength { get { return m_nMaxEndEffectiveXHeaderLength; } }
        public Dictionary<string, EMNode_Element> IgnoreNodes 
        {
            get 
            {
                Dictionary<string, EMNode_Element> dicIgnoreNodes = new Dictionary<string, EMNode_Element>();
                Interlocked.Exchange(ref dicIgnoreNodes, m_dicIgnoreNode);
                return dicIgnoreNodes; 
            } 
        }
        #endregion

        #region Members
        int m_nMaxStartEffectiveXHeaderLength = knDefaultMaxStartEffectiveXHeaderLength;
        int m_nMaxEndEffectiveXHeaderLength = knDefaultMaxEndEffectiveXHeaderLength;
       
        Dictionary<string, EMNode_Element> m_dicIgnoreNode = kdicDefaultIgnoreNode;

        ReaderWriterLockSlim m_rwSupportExtension = new ReaderWriterLockSlim();
        HashSet<string> m_setSupportExtension = ksetDefaultSupportExtension;      // contains "."
        #endregion

        #region Singleton
        static private object s_obLockForInstance = new object();
        static private TDFXHeaderExtralConfig s_obTDFXHeaderExtralConfigIns = null;
        static public TDFXHeaderExtralConfig GetInstance()
        {
            if (null == s_obTDFXHeaderExtralConfigIns)
            {
                lock (s_obLockForInstance)
                    if (null == s_obTDFXHeaderExtralConfigIns)
                    {
                        s_obTDFXHeaderExtralConfigIns = new TDFXHeaderExtralConfig(knDefaultMaxStartEffectiveXHeaderLength, knDefaultMaxEndEffectiveXHeaderLength, kdicDefaultIgnoreNode, ksetDefaultSupportExtension);
                    }
            }
            return s_obTDFXHeaderExtralConfigIns;
        }
        private TDFXHeaderExtralConfig(int nMaxEffectiveXHeaderStartLengthIn, int nMaxEffectiveXHeaderEndLengthIn, Dictionary<string, EMNode_Element> dicIgnoreNodeIn, HashSet<string> setSupportExtensionIn)
        {
            m_nMaxStartEffectiveXHeaderLength = nMaxEffectiveXHeaderStartLengthIn;
            m_nMaxEndEffectiveXHeaderLength = nMaxEffectiveXHeaderEndLengthIn;
            m_dicIgnoreNode = dicIgnoreNodeIn;
            m_setSupportExtension = setSupportExtensionIn;
        }
        #endregion

        public void Init(string strTDFConfigFile)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(strTDFConfigFile);

                // Select TDF Config
                XmlNode nodePluginConfig = xmlDoc.SelectSingleNode(kstrXmlNode_TDFConfig);
                if (null != nodePluginConfig)
                {
                    Init(nodePluginConfig);
                }
            }
            catch (Exception ex)
            {
                MyLog.OutputLog(LogLevel.Error, "Load and analysis TDF config file:[{0}] exception, {1}\n", strTDFConfigFile, ex.Message);
            }
        }

        public void Init(XmlNode nodeTDFConfig)
        {
            try
            {
                if (null == nodeTDFConfig)
                {
                    // Empty
                }
                else
                {
                    /*
                        <TDFConfig>
                          <MaxStartEffectiveXHeaderLength>2097152</MaxStartEffectiveXHeaderLength>
                          <MaxEndEffectiveXHeaderLength>1024</MaxEndEffectiveXHeaderLength>
                          <!-- default: classic, empty. 
                            classic: if do not config IngnoreNode, default ignore tdf:Base64BinaryPayload node
                            empty: if do not config IngnoreNode, default ignore nothing
                            if no this attribute or no IgnoreNodes node, using classic mode, ignore tdf:Base64BinaryPayload node
                          -->
                          <IgnoreNodes default="classic">
                            <!-- element: wholeNode, attribute, value -->
                            <IgnoreNode name="tdf:Base64BinaryPayload" element="value"/>
                          </IgnoreNodes>
                          <!-- using "," to split values and using "*" to support all files. If do not configured, only support xml extension files -->
                          <SupportExtensions>xml</SupportExtensions> 
                        </TDFConfig>
                    */

                    // Header length
                    m_nMaxStartEffectiveXHeaderLength = CommonHelper.ConvertStringToInt(XMLTools.GetXMLSingleNodeText(nodeTDFConfig, kstrXmlNode_MaxStartEffectiveXHeaderLength), knDefaultMaxStartEffectiveXHeaderLength);
                    m_nMaxEndEffectiveXHeaderLength = CommonHelper.ConvertStringToInt(XMLTools.GetXMLSingleNodeText(nodeTDFConfig, kstrXmlNode_MaxEndEffectiveXHeaderLength), knDefaultMaxEndEffectiveXHeaderLength);

                    // Ignore nodes
                    bool bDefaultIgnoreNothing = false;
                    Dictionary<string, EMNode_Element> dicIgnoreNode = null;
                    XmlNode nodeIgnoreNodes = nodeTDFConfig.SelectSingleNode(kstrXmlNode_IgnoreNodes);
                    if (null != nodeIgnoreNodes)
                    {
                        string strIgnoreNodesDefaultAttrValue = XMLTools.GetAttributeValue(nodeIgnoreNodes.Attributes, kstrXmlAttr_Default);
                        if ((!String.IsNullOrEmpty(strIgnoreNodesDefaultAttrValue)) && (kstrValue_Enpty.Equals(strIgnoreNodesDefaultAttrValue, StringComparison.OrdinalIgnoreCase)))
                        {
                            bDefaultIgnoreNothing = true;
                        }

                        List<XmlNode> lsIgnoreNode = XMLTools.GetAllSubNodes(nodeIgnoreNodes, kstrXmlNode_IgnoreNode);
                        if (null != lsIgnoreNode)
                        {
                            dicIgnoreNode = new Dictionary<string,EMNode_Element>();
                            foreach (XmlNode nodeIgnoreNode in lsIgnoreNode)
                            {
                                string strName = XMLTools.GetAttributeValue(nodeIgnoreNode.Attributes, kstrXmlAttr_Name);
                                string strElement = XMLTools.GetAttributeValue(nodeIgnoreNode.Attributes, kstrXmlAttr_Element);

                                EMNode_Element emElement = (EMNode_Element)CommonHelper.ConvertStringToEnum<EMNode_Element>(strElement, true, EMNode_Element.Unknown);
                                if (EMNode_Element.Unknown == emElement)
                                {
                                    // The ignore element is unknown, no means
                                }
                                else
                                {
                                    CommonHelper.AddKeyValuesToDir(dicIgnoreNode, strName, emElement);
                                }
                            }
                        }
                    }
                    if ((null == dicIgnoreNode) || (0 == dicIgnoreNode.Count))
                    {
                        if (bDefaultIgnoreNothing)
                        {
                            // Ignore nothing
                            Interlocked.Exchange(ref m_dicIgnoreNode, null);
                        }
                        else
                        {
                            // Classic mode
                            Interlocked.Exchange(ref m_dicIgnoreNode, kdicDefaultIgnoreNode);
                        }
                    }
                    else
                    {
                        Interlocked.Exchange(ref m_dicIgnoreNode, dicIgnoreNode);
                    }

                    // Support extensions
                    HashSet<string> setSupportExtension = null;
                    string strSupportExtensions = XMLTools.GetXMLSingleNodeText(nodeTDFConfig, kstrXmlNode_SupportExtensions);
                    if (!String.IsNullOrEmpty(strSupportExtensions))
                    {
                        setSupportExtension = new HashSet<string>();
                        string[] szExtensions = strSupportExtensions.Split(kchSepExtension);
                        foreach (string strExtension in szExtensions)
                        {
                            if (!String.IsNullOrEmpty(strExtension))
                            {
                                string strStandardExtension = MakeStandardFileSuffix(strExtension);
                                if (!setSupportExtension.Contains(strStandardExtension))
                                {
                                    setSupportExtension.Add(strStandardExtension);
                                }
                            }
                        }
                    }
                    if ((null == setSupportExtension) || (0 == setSupportExtension.Count))
                    {
                        // Config info is empty, using default value: only support xml
                        SetSupportExtensions(ksetDefaultSupportExtension);
                    }
                    else
                    {
                        SetSupportExtensions(setSupportExtension);
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.OutputLog(LogLevel.Error, "Load and analysis TDF config node:[{0}] exception, {1}\n", nodeTDFConfig, ex.Message);
            }
        }

        public bool IsSupportFile(string strFilePath)
        {
            bool bSupport = false;
            HashSet<string> setSupportExtension = GetSupportExtensions();
            if ((null == setSupportExtension) || (0 == setSupportExtension.Count))
            {
                bSupport = true;
            }
            else
            {
                if (setSupportExtension.Contains(kstrValue_Star))
                {
                    bSupport = true;
                }
                else
                {                   
                    string strExtension = GetFileSuffix(strFilePath, true);
                    if (String.IsNullOrEmpty(strExtension))
					{
                        bSupport = false;
                    }
                    else
					{
						bSupport = setSupportExtension.Contains(strExtension);
					}
				}
            }
            return bSupport;
        }

        private void SetSupportExtensions(HashSet<string> setSupportExtension)
        {
            try
            {
                m_rwSupportExtension.EnterWriteLock();
                m_setSupportExtension = setSupportExtension;
            }
            finally
            {
                m_rwSupportExtension.ExitWriteLock();
            }
        }
        private HashSet<string> GetSupportExtensions()
        {
            try
            {
                m_rwSupportExtension.EnterReadLock();
                return m_setSupportExtension;
            }
            finally
            {
                m_rwSupportExtension.ExitReadLock();
            }
        }

        static private string GetFileSuffix(string strFileName, bool bIncludeDot)
        {
            string strSuffix = "";
            if (!String.IsNullOrEmpty(strFileName))
			{
				int nPos = strFileName.LastIndexOf('.');
				if (nPos >= 0)
				{
					if (bIncludeDot)
					{
						strSuffix = strFileName.Substring(nPos);
					}
					else
					{
						strSuffix = strFileName.Substring(nPos + 1);
					}
				}
			}
            return strSuffix;
        }
        // Standard extension with dot start
        static private string MakeStandardFileSuffix(string strExtension)
        {
            if (!String.IsNullOrEmpty(strExtension))
            {
                if ('.' == strExtension[0])
                {
                    // Standard
                }
                else
                {
                    strExtension = '.' + strExtension;
                }
            }
            return strExtension;
        }
    }
}
