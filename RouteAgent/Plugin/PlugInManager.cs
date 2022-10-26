using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using DocumentFormat.OpenXml.Spreadsheet;

using CSBase.Diagnose;

using RouteAgent.Common;
using CSBase.Common;

namespace RouteAgent.Plugin
{
    public class PlugInManager
    {
        #region Const values
        static private readonly HashSet<Type> g_ksetSupportPlugins = new HashSet<Type>()
        {
            typeof(RouteAgent.Plugin.INLEmailParser),
            typeof(RouteAgent.Plugin.INLAttachmentParser)
        };
        #endregion

        #region Singleton
        static private object s_obLockForInstance = new object();
        static private PlugInManager s_obPlugInManagerIns = null;
        static public PlugInManager GetInstance()
        {
            if (null == s_obPlugInManagerIns)
            {
                lock (s_obLockForInstance)
                    if (null == s_obPlugInManagerIns)
                    {
                        s_obPlugInManagerIns = new PlugInManager();
                    }
            }
            return s_obPlugInManagerIns;
        }
        #endregion

        #region Init
        public void Init(string strConfigFilePath)
        {
            Dictionary<Type, List<RouteAgent.Plugin.INPluginRoot>> dicPlugins = LoadPluginsInfo(strConfigFilePath);
            if (null != dicPlugins)
            {
                InitPlguins(dicPlugins);
            }
        }
        public void Uninit()
        {
            UninitPlugins();
        }
        #endregion

        #region public
        public List<TPlugin> GetPlugins<TPlugin>() where TPlugin : class
        {
            List<TPlugin> lsPlguinsRet = null;
            List<RouteAgent.Plugin.INPluginRoot> lsRootPlugins = InnerGetPlugins(typeof(TPlugin));
            if (null != lsRootPlugins)
            {
                lsPlguinsRet = new List<TPlugin>();
                foreach (RouteAgent.Plugin.INPluginRoot obCurPlguinRoot in lsRootPlugins)
                {
                    TPlugin obCurPlugin = obCurPlguinRoot as TPlugin;
                    if (null == obCurPlguinRoot)
                    {
                        CSLogger.OutputLog(LogLevel.Error, "Plugin error, current plugin object:[{0}] convert to:[{1}] failed\n", new object[] { obCurPlguinRoot, typeof(TPlugin) });
                    }
                    else
                    {
                        lsPlguinsRet.Add(obCurPlugin);
                    }
                }
            }
            return lsPlguinsRet;
        }
        #endregion

        #region Plguin manager
        private void InitPlguins(Dictionary<Type, List<RouteAgent.Plugin.INPluginRoot>> dicPluginsIn)
        {
            if (null != dicPluginsIn)
            {
                foreach (KeyValuePair<Type, List<RouteAgent.Plugin.INPluginRoot>> pairPlugins in dicPluginsIn)
                {
                    foreach (INPluginRoot obNLPluginRoot in pairPlugins.Value)
                    {
                        obNLPluginRoot.Init(pairPlugins.Key);
                    }
                }

                m_rwlockForPlugins.EnterWriteLock();
                try
                {
                    m_dicPlugins = dicPluginsIn;
                }
                finally
                {
                    m_rwlockForPlugins.ExitWriteLock();
                }
            }
        }
        private void UninitPlugins()
        {
            m_rwlockForPlugins.EnterWriteLock();
            try
            {
                if (null != m_dicPlugins)
                {
                    foreach (KeyValuePair<Type, List<RouteAgent.Plugin.INPluginRoot>> pairPlugins in m_dicPlugins)
                    {
                        foreach (INPluginRoot obNLPluginRoot in pairPlugins.Value)
                        {
                            obNLPluginRoot.Init(pairPlugins.Key);
                        }
                    };
                    m_dicPlugins = null;
                }
            }
            finally
            {
                m_rwlockForPlugins.ExitWriteLock();
            }
        }
        private List<RouteAgent.Plugin.INPluginRoot> InnerGetPlugins(Type tyIn)
        {
            m_rwlockForPlugins.EnterReadLock();
            try
            {
                if ((null != m_dicPlugins) && (null != m_dicPlugins.Keys))
				{
					bool bExist = m_dicPlugins.Keys.Contains(tyIn);
					if (bExist)
					{
						return m_dicPlugins[tyIn];
					}
					else
					{
						return null;
					}
				}
                else
				{
					return null;
				}
            }
            finally
            {
                m_rwlockForPlugins.ExitReadLock();
            }
        }
        #endregion

        #region Load plugins
        static private Dictionary<Type, List<RouteAgent.Plugin.INPluginRoot>> LoadPluginsInfo(string strPath)
        {
            Dictionary<Type, List<RouteAgent.Plugin.INPluginRoot>> dicPlugins = null;
            try
            {
                CSLogger.OutputLog(LogLevel.Info, "Read Plug config file from " + strPath);
                PluginsConfigNode pluginsConfig = XMLTools.LoadFromXml<PluginsConfigNode>(strPath);
                CSLogger.OutputLog(LogLevel.Info, "Read Plug config Success");
                if (pluginsConfig != null)
                {
                    if (pluginsConfig.Plugins != null)
                    {
                        foreach (plugin p in pluginsConfig.Plugins)
                        {
                            string strAssemble = string.Format("{0}, Version={1}, Culture={2}, PublicKeyToken={3}", p.Name, p.Version, p.Culture, p.PublicKeyToken);
                            CSLogger.OutputLog(LogLevel.Debug, "Assemble:" + strAssemble);
                            Assembly ass = FileTools.LoadAssemble(strAssemble);
                            if (ass != null)
                            {
                                CSLogger.OutputLog(LogLevel.Debug, "Load Assemble Success");
                                Type[] szTypes = FileTools.GetTypesFromAssemble(ass);
                                if (szTypes != null)
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "Get Types Success type");
                                    if (szTypes.Length > 0)
                                    {
                                        InnerSetPluginsEx(szTypes, ref dicPlugins, g_ksetSupportPlugins);
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
            return dicPlugins;
        }
        static private void InnerSetPluginsEx(Type[] szTypes, ref Dictionary<Type, List<RouteAgent.Plugin.INPluginRoot>> dicPlugins, HashSet<Type> setSupportTypes)
        {
            foreach (Type ty in szTypes)
            {
                CSLogger.OutputLog(LogLevel.Debug, string.Format("Type name:[{0}], full name:[{1}]", ty.Name, ty.FullName));

                foreach (Type tySupportType in setSupportTypes)
                {
                    if (null != ty.GetInterface(tySupportType.Name))
                    {
                        object obTempIns = Activator.CreateInstance(ty);
                        RouteAgent.Plugin.INLEmailParser obNLRouteAgentPluginEntryIns = obTempIns as RouteAgent.Plugin.INLEmailParser;

                        // RouteAgent.Plugin.INLRouteAgentPluginEntry obNLRouteAgentPluginEntryIns = Activator.CreateInstance(ty) as RouteAgent.Plugin.INLRouteAgentPluginEntry;
                        if (null == obNLRouteAgentPluginEntryIns)
                        {
                            CSLogger.OutputLog(LogLevel.Debug, "Establish route agent plugin entry instance failed");
                        }
                        else
                        {
                            CSLogger.OutputLog(LogLevel.Debug, "Establish route agent plugin entry instance Success");
                            if (null == dicPlugins)
                            {
                                dicPlugins = new Dictionary<Type, List<RouteAgent.Plugin.INPluginRoot>>();
                            }
                            AddPluginType(ref dicPlugins, tySupportType, obNLRouteAgentPluginEntryIns);
                        }
                    }
                }
            }
        }
        static private void AddPluginType(ref Dictionary<Type, List<RouteAgent.Plugin.INPluginRoot>> dicPlugins, Type tyIn, RouteAgent.Plugin.INPluginRoot obNewPlugin)
        {
            if (null == dicPlugins)
            {
                dicPlugins = new Dictionary<Type, List<INPluginRoot>>();
                dicPlugins[tyIn] = new List<INPluginRoot>();
            }
            else
            {
                bool bkeyExist = dicPlugins.Keys.Contains(tyIn);
                if (bkeyExist)
                {
                    if (null == dicPlugins[tyIn])
                    {
                        dicPlugins[tyIn] = new List<INPluginRoot>();
                    }
                }
                else
                {
                    dicPlugins[tyIn] = new List<INPluginRoot>();
                }
            }
            dicPlugins[tyIn].Add(obNewPlugin);
        }
        #endregion

        #region Members
        private ReaderWriterLockSlim m_rwlockForPlugins = new ReaderWriterLockSlim();
        private Dictionary<Type, List<RouteAgent.Plugin.INPluginRoot>> m_dicPlugins;
        #endregion

#if false
        #region Load plugins backup
        static private List<RouteAgent.Plugin.INLEmailParser> LoadRouteAgentPluginInfo(string strPath)
        {
            List<RouteAgent.Plugin.INLEmailParser> lsRouteAgentPlugins = null;
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
                                Type[] szTypes = CommonHelper.GetTypesFromAssemble(ass);
                                if (szTypes != null)
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "Get Types Success type");
                                    if (szTypes.Length > 0)
                                    {
                                        InnerSetPlugins(szTypes, ref lsRouteAgentPlugins);
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
        static private void InnerSetPlugins(Type[] szTypes, ref List<RouteAgent.Plugin.INLEmailParser> lsRouteAgentPlugins)
        {
            foreach (Type ty in szTypes)
            {
                CSLogger.OutputLog(LogLevel.Debug, string.Format("Type name:[{0}], full name:[{1}]", ty.Name, ty.FullName));
                if (null != ty.GetInterface(typeof(RouteAgent.Plugin.INLEmailParser).Name))
                {
                    object obTempIns = Activator.CreateInstance(ty);
                    RouteAgent.Plugin.INLEmailParser obNLRouteAgentPluginEntryIns = obTempIns as RouteAgent.Plugin.INLEmailParser;

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
                            lsRouteAgentPlugins = new List<RouteAgent.Plugin.INLEmailParser>();
                        }
                        lsRouteAgentPlugins.Add(obNLRouteAgentPluginEntryIns);
                    }
                }
            }
        }
        #endregion
#endif
    }
}
