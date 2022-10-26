using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace RouteAgent.Common
{
    public class PlugInManager<TPlugin> where TPlugin : RouteAgent.Plugin.INLInit, class
    {
        #region Logger
        protected static CLog theLog = CLog.GetLogger(typeof(PlugInManager<TPlugin>));
        #endregion

        #region Fields

        public List<TPlugin> EmailParserPlugins
        {
            get
            {
                try
                {
                    m_rwlockForRouteAgentPlugins.EnterReadLock();
                    return m_lsEmailParserPlugins;
                }
                finally
                {
                    m_rwlockForRouteAgentPlugins.ExitReadLock();
                }

            }
            private set
            {
                try
                {
                    m_rwlockForRouteAgentPlugins.EnterWriteLock();
                    m_lsEmailParserPlugins = value;
                }
                finally
                {
                    m_rwlockForRouteAgentPlugins.ExitWriteLock();
                }
            }
        }
        #endregion

        #region Init
        public void Init(string strConfigFilePath)
        {
            List<TPlugin> lsRouteAgentPlugins = LoadRouteAgentPluginInfo(strConfigFilePath);
            if (null != lsRouteAgentPlugins)
            {
                foreach (TPlugin obNLRouteAgentPluginEntry in lsRouteAgentPlugins)
                {
                    obNLRouteAgentPluginEntry.Init();
                }
                EmailParserPlugins = lsRouteAgentPlugins;
            }
        }
        public void Uninit()
        {
            List<TPlugin> lsRouteAgentPlugins = EmailParserPlugins;
            EmailParserPlugins = null;

            if (null != lsRouteAgentPlugins)
            {
                foreach (TPlugin obNLRouteAgentPluginEntry in lsRouteAgentPlugins)
                {
                    obNLRouteAgentPluginEntry.Uninit();
                }
            }
        }
        private static List<TPlugin> LoadRouteAgentPluginInfo(string strPath)
        {
            List<TPlugin> lsRouteAgentPlugins = null;
            try
            {
                CSLogger.OutputLog(LogLevel.Info, "Read Plug config file from " + strPath);
                PluginsConfigNode pluginsConfig = CommonHelper.LoadFromXml<PluginsConfigNode>(strPath);
                CSLogger.OutputLog(LogLevel.Info, "Read Plug config Success");
                if (pluginsConfig != null)
                {
                    if (pluginsConfig.Plugins != null)
                    {
                        foreach (plugin p in pluginsConfig.Plugins)
                        {
                            string strAssemble = string.Format("{0}, Version={1}, Culture={2}, PublicKeyToken={3}", p.Name, p.Version, p.Culture, p.PublicKeyToken);
                            CSLogger.OutputLog(LogLevel.Debug, "Assemble:" + strAssemble);
                            Assembly ass = CommonHelper.LoadAssemble(strAssemble);
                            if (ass != null)
                            {
                                CSLogger.OutputLog(LogLevel.Debug, "Load Assemble Success");
                                Type[] types = CommonHelper.GetTypesFromAssemble(ass);
                                if (types != null)
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "Get Types Success type");
                                    if (types.Length > 0)
                                    {
                                        foreach (Type ty in types)
                                        {
                                            CSLogger.OutputLog(LogLevel.Debug, string.Format("Type name:[{0}], full name:[{1}]", ty.Name, ty.FullName));
                                            if (null != ty.GetInterface(typeof(TPlugin).Name))
                                            {
                                                object obTempIns = Activator.CreateInstance(ty);
                                                TPlugin obNLRouteAgentPluginEntryIns = obTempIns as TPlugin;

                                                // RouteAgent.Plugin.INLRouteAgentPluginEntry obNLRouteAgentPluginEntryIns = Activator.CreateInstance(ty) as RouteAgent.Plugin.INLRouteAgentPluginEntry;
                                                if (null == obNLRouteAgentPluginEntryIns)
                                                {
                                                    CSLogger.OutputLog(LogLevel.Debug, "Establish route agent plugin entry instance failed");
                                                }
                                                else
                                                {
                                                    CSLogger.OutputLog(LogLevel.Debug, "Establish route agent plugin entry instance Success");
                                                    if (null == lsRouteAgentPlugins)
                                                    {
                                                        lsRouteAgentPlugins = new List<TPlugin>();
                                                    }
                                                    lsRouteAgentPlugins.Add(obNLRouteAgentPluginEntryIns);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        CSLogger.OutputLog(LogLevel.Debug, "Can Not Find Any Type on Assemble");
                                    }
                                }
                                else
                                {
                                    CSLogger.OutputLog(LogLevel.Error, "Get Type error, Can not find any type");
                                }
                            }
                            else
                            {
                                CSLogger.OutputLog(LogLevel.Error, "Load Assemble Error,Assemble Name:" + strAssemble);
                            }
                        }
                    }
                    else
                    {
                        CSLogger.OutputLog(LogLevel.Warn, "Can not get plugIn , because plugin list is empty");
                    }
                }
                else
                {
                    CSLogger.OutputLog(LogLevel.Error, "Load PlugIn Configuration File Error, Can not load it");
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Warn, "Read PlugIn config file Exception:" + ex.ToString());
            }
            finally
            {

            }
            return lsRouteAgentPlugins;
        }
        #endregion

        #region Members
        private ReaderWriterLockSlim m_rwlockForRouteAgentPlugins = new ReaderWriterLockSlim();
        private List<TPlugin> m_lsEmailParserPlugins;
        #endregion
    }

    [XmlRootAttribute("pluginsConfig", Namespace = "", IsNullable = false)]
    public class PluginsConfigNode
    {
        [XmlArrayAttribute("plugins")]
        public plugin[] Plugins
        {
            get;
            set;
        }
    }
    [XmlRootAttribute("plugin", ElementName = "plugin")]
    public class plugin
    {
        [XmlAttribute("name")]
        public string Name
        {
            get;
            set;
        }
        [XmlAttribute("processorArchitecture")]
        public string ProcessorArchitecture
        {
            get;
            set;
        }
        [XmlAttribute("publicKeyToken")]
        public string PublicKeyToken
        {
            get;
            set;
        }
        [XmlAttribute("culture")]
        public string Culture
        {
            get;
            set;
        }
        [XmlAttribute("version")]
        public string Version
        {
            get;
            set;
        }
    }
}
