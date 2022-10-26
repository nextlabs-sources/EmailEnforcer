using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace Common
{
    public class PlugIn
    {
        protected static CLog theLog = CLog.GetLogger(typeof(PlugIn));
        private static ReaderWriterLock rwlock = new ReaderWriterLock();
        private static List<Nextlabs.RouteAgent.PlugIn.INLClearance> m_lisInstance;
        public static List<Nextlabs.RouteAgent.PlugIn.INLClearance> Instances
        {
            get
            {
                try
                {
                    rwlock.AcquireReaderLock(60 * 1000);
                    if (!rwlock.IsReaderLockHeld)
                    {
                        CSLogger.OutputLog(LogLevel.Warn, "Get Reader Lock failed.");
                        return null;
                    }

                    return m_lisInstance;
                }
                finally
                {
                    rwlock.ReleaseReaderLock();
                }

            }
        }
        public static void Init(string strPath)
        {
            try
            {
                rwlock.AcquireWriterLock(60 * 1000);
                if (!rwlock.IsWriterLockHeld)
                {
                    CSLogger.OutputLog(LogLevel.Warn, "Get Writer Lock failed.");
                    return;
                }

                CSLogger.OutputLog(LogLevel.Info, "Read Plug config file from " + strPath);
                PluginsConfigNode pluginsConfig = LoadFromXml<PluginsConfigNode>(strPath);
                CSLogger.OutputLog(LogLevel.Info, "Read Plug config Success");
                if (pluginsConfig != null)
                {
                    if (pluginsConfig.Plugins != null)
                    {
                        foreach (plugin p in pluginsConfig.Plugins)
                        {
                            string strAssemble = string.Format("{0}, Version={1}, Culture={2}, PublicKeyToken={3}", p.Name, p.Version, p.Culture, p.PublicKeyToken);
                            CSLogger.OutputLog(LogLevel.Debug, "Assemble:" + strAssemble);
                            Assembly ass = LoadAssemble(strAssemble);
                            if (ass != null)
                            {
                                CSLogger.OutputLog(LogLevel.Debug, "Load Assemble Success");
                                Type[] types = GetTypesFromAssemble(ass);
                                if (types != null)
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "Get Types Success type");
                                    if (types.Length > 0)
                                    {
                                        foreach (Type t in types)
                                        {
                                            CSLogger.OutputLog(LogLevel.Debug, "Type Name:" + t.Name);
                                            object instance = Activator.CreateInstance(t);
                                            CSLogger.OutputLog(LogLevel.Debug, "Create Instance Success");
                                            if (instance is Nextlabs.RouteAgent.PlugIn.INLClearance)
                                            {
                                                CSLogger.OutputLog(LogLevel.Debug, "Instance Is NLClearance");
                                                if (m_lisInstance == null)
                                                {
                                                    m_lisInstance = new List<Nextlabs.RouteAgent.PlugIn.INLClearance>();
                                                }
                                                m_lisInstance.Add(instance as Nextlabs.RouteAgent.PlugIn.INLClearance);
                                            }
                                            else
                                            {
                                                CSLogger.OutputLog(LogLevel.Debug, "Instance Is Not NLClearance, Ignore It");
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
                CSLogger.OutputLog(LogLevel.Warn, "Exchange Enforcer will try Read it after 60 second");
                System.Timers.Timer timer = new System.Timers.Timer(60 * 1000);
                timer.AutoReset = false;
                timer.Elapsed += (sender, e) =>
                {
                    Init(strPath);
                };
                timer.Start();
            }
            finally
            {
                rwlock.ReleaseWriterLock();
            }

        }
        private static T LoadFromXml<T>(string filePath)
        {
            T result = default(T);
            try
            {
                if (File.Exists(filePath))
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader reader = new StreamReader(fs))
                        {
                            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                            result = (T)xmlSerializer.Deserialize(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                {
                    throw ex;
                }
                else
                {
                    CSLogger.OutputLog(LogLevel.Error, ex);
                }
            }
            return result;
        }

        private static Assembly LoadAssemble(string strAssemble)
        {
            Assembly resultAssemble = null;
            try
            {
                resultAssemble = Assembly.Load(strAssemble);
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                CSLogger.OutputLog(LogLevel.Error, errorMessage);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, ex);
            }
            return resultAssemble;
        }

        private static Type[] GetTypesFromAssemble(Assembly assemble)
        {
            Type[] types = null;
            try
            {
                types = assemble.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                CSLogger.OutputLog(LogLevel.Error, errorMessage);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, ex);
            }
            return types;
        }
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
