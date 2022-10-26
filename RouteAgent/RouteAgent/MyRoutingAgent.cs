using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Exchange.Data.Transport.Routing;
using Microsoft.Exchange.Data.Transport;
using Microsoft.Exchange.Data.Transport.Email;
using Microsoft.Exchange.Data.Mime;
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
using RouteAgent.Common;
using System.Reflection;
using RouteAgent.Common.Model;
using Microsoft.Exchange.Data.Transport.Smtp;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;

using RouteAgent.Plugin;
using CSBase.Diagnose;
using CSBase.Common;

namespace RouteAgent
{
    public class MyRoutingAgentFactory : RoutingAgentFactory
    {
        //use static construct to do init work
        static MyRoutingAgentFactory()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            //get current Exchange Enforcer install path
            string strEEInstallPath = Common.Function.GetExchangeEnforcerInstallPath();
            FileTools.MakeStandardFolderPath(ref strEEInstallPath);

            // Init log module
            DiagnoseGlobalInfo.GetInstance().Init("ExchangeEnforcer", strEEInstallPath + "Logs\\", strEEInstallPath + "Config\\logcfg.xml", strEEInstallPath + "Logs\\", "");

            FileSystemWatcher configFileWatcher = new FileSystemWatcher();
            configFileWatcher.Path = strEEInstallPath + "config";
            configFileWatcher.Filter = "*.xml";
            configFileWatcher.Changed += new FileSystemEventHandler(OnConfigFileChanged);
            configFileWatcher.EnableRaisingEvents = true;
            configFileWatcher.NotifyFilter = NotifyFilters.LastWrite;

            //read config file
            Common.Config.Init(strEEInstallPath + @"config\exchangepep.xml");

            //read plugin file
            PlugInManager obPluginManagerIns = PlugInManager.GetInstance();
            obPluginManagerIns.Init(strEEInstallPath + @"config\plugin.xml");

            if (Common.Config.SupportClientType.Equals("Yes", StringComparison.OrdinalIgnoreCase) && (m_readLogThread == null))
            {
                m_readLogThread = new Thread(new ThreadStart(ReadLogThread));
                m_readLogThread.Start();
            }

            //outputdebugstring capture
            OutputDebugStringCapture.Instance().Init();

            Process obCurProcess = Process.GetCurrentProcess();
            CSLogger.OutputLog(LogLevel.Info, string.Format("Current process:[{0} - {1}]\n", obCurProcess.Id, obCurProcess.ProcessName));
        }


        private static Thread m_readLogThread = null;

        public override RoutingAgent CreateAgent(SmtpServer server)
        {
            CSLogger.OutputLog(LogLevel.Debug, "Enter MyRoutingAgentFactory::CreateAgent");

            return new MyRoutingAgent(server);
        }

        private static void ReadLogThread()
        {
            CSLogger.OutputLog(LogLevel.Debug, "ReadLogHelp: Start thread...");
            try
            {
                TransportMessageTraceLogReader rlh = new TransportMessageTraceLogReader();
                rlh.ThreadEntry();
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "ReadLogHelp: !Unhandled exception, read log thread is going to end. Exception: " + ex.Message);
            }
        }

        private static void OnConfigFileChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {

                if (e.Name.Equals("exchangepep.xml", StringComparison.OrdinalIgnoreCase))
                {
                    Common.Config.Init(e.FullPath);

                    if (Common.Config.SupportClientType.Equals("Yes", StringComparison.OrdinalIgnoreCase) && (m_readLogThread == null))
                    {
                        m_readLogThread = new Thread(new ThreadStart(ReadLogThread));
                        m_readLogThread.Start();
                    }
                }
                else if (e.Name.Equals("plugin.xml", StringComparison.OrdinalIgnoreCase))
                {
                    PlugInManager obPluginManagerIns = PlugInManager.GetInstance();
                    obPluginManagerIns.Init(e.FullPath);
                }
            }
        }

        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Important: do not invoke any code in other assembly, the assembly cannot load
            // Cannot using CSLogger to output logs, the log module do not load at first time and cannot load success
            // Check current load dll is log dll or not, it is no used, do not work
            System.Diagnostics.Debug.WriteLine("CurrentDomain_AssemblyResolve:" + args.Name);
            string path = System.IO.Path.Combine(Common.Function.GetExchangeEnforcerInstallPath(), @"bin\");
            path = System.IO.Path.Combine(path, args.Name.Split(',')[0]);
            path = String.Format(@"{0}.dll", path);
            System.Diagnostics.Debug.WriteLine("CurrentDomain_AssemblyResolve: resolve file path:[{0}]\n", path);
            if (File.Exists(path))
            {
                return System.Reflection.Assembly.LoadFrom(path);
            }
            else
            {
                return null;
            }
        }

        public static string GetAssemblyDir
        {
            get
            {
                Type t = typeof(MyRoutingAgent);
                Assembly assemFromType = t.Assembly;
                string codeBase = assemFromType.CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }

    public class MyRoutingAgent : RoutingAgent
    {
        #region Const/Read only values
        private const string g_kstrEETempFolderName = "EETemp_DB6FB866-379C-430D-AF66-CCFA280B8055";
        static private readonly string g_kstrEETempFolderFullPath = Common.Function.GetTempDirectory() + g_kstrEETempFolderName + "\\";
        #endregion

        protected SmtpServer m_smtpServer = null;
        protected SmtpServer Server
        {
            get { return m_smtpServer; }
        }

        protected string m_strMAPIMsgClsSubmitLamProbe = "IPM.Note.MapiSubmitLAMProbe";
        protected string m_strEmailOutputTempDir = null;
        protected List<string> listAttrachInfo = null;

        private bool bExtractGroup
        {
            get;
            set;
        }

        private EmailEvalInfoManage EmailEvalInfoMgr
        {
            get;
            set;
        }

        private int m_recipientCount = 0;
        private int RecipientCount
        {
            get
            {
                return m_recipientCount;
            }
            set
            {
                if (m_recipientCount == 0)
                {
                    m_recipientCount = value;
                }
            }
        }

        private bool m_bLastForked = false;
        private bool LastForked
        {
            get
            {
                return m_bLastForked;
            }
            set
            {
                m_bLastForked = value;
            }
        }

        private List<NotifyObligation> m_lisNotifyObligations = new List<NotifyObligation>();
        private List<NotifyObligation> NotifyObligationsForSender
        {
            get
            {
                return m_lisNotifyObligations;
            }
        }

        private List<RoutingAddress> m_lisNeedRemoveRecipients = new List<RoutingAddress>();
        private List<RoutingAddress> NeedRemoveRecipients
        {
            get
            {
                return m_lisNeedRemoveRecipients;
            }
        }

        private List<QueryPolicyResult> m_CachePolicyTable = new List<QueryPolicyResult>();
        private List<QueryPolicyResult> PolicyTable
        {
            get
            {
                return m_CachePolicyTable;
            }
        }

        private List<ForkItem> m_ForkList = new List<ForkItem>();
        public List<ForkItem> ForkList
        {
            get
            {
                return m_ForkList;
            }
        }


        private Semaphore m_SemaphorePolicyTable = new Semaphore(1, 1);
        public Semaphore SemaphorePolicyTable
        {
            get
            {
                return m_SemaphorePolicyTable;
            }
        }

        enum MessageHandlerType
        {
            OnSubmittedMessageHandler,
            OnResolvedMessageHandler
        }

        #region construction method
        public MyRoutingAgent(SmtpServer smtpServer)
        {
            if (Config.EnableEnforce)
            {
                m_smtpServer = smtpServer;
                this.OnSubmittedMessage += new SubmittedMessageEventHandler(this.OnSubmittedMessageHandler);
                this.OnResolvedMessage += new ResolvedMessageEventHandler(this.OnResolvedMessageHandler);
                //create output temp dir
                CreateEmailOutputTempDir();
            }
        }
        ~MyRoutingAgent()
        {
            CSLogger.OutputLog(LogLevel.Info, "Start Destructor Object:" + this.GetHashCode());
            SemaphorePolicyTable.Close();
            CleanEmailOutputTempDir();
        }
        #endregion

        #region Event Handler
        protected void OnSubmittedMessageHandler(SubmittedMessageEventSource source, QueuedMessageEventArgs e)
        {
            if (e.MailItem == null || e.MailItem.Message == null)
            {
                return;
            }

            Stopwatch tempWatch = new Stopwatch();
            try
            {
                tempWatch.Start();
                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "OnSubmittedMessageHandler Start", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                EmailEvalInfoMgr = new Common.EmailEvalInfoManage();
                EmailEvalInfoMgr.Init(e.MailItem, Server, m_strEmailOutputTempDir);

                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Finish Init Email Eval Info Manage", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));

                if (EmailEvalInfoMgr.NeedProcess)
                {
                    ProcessMailOnSubmittedMessageHandler(e.MailItem);
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception happened OnSubmittedMessageHandler", null, ex);
                Type exType = ex.GetType();
                if (exType == typeof(AggregateException))
                {
                    exType = ex.InnerException.GetType();
                }
                if (exType == typeof(NullReferenceException) || exType == typeof(IndexOutOfRangeException) ||
                    exType == typeof(ArgumentNullException) || exType == typeof(ArithmeticException) ||
                    exType == typeof(DivideByZeroException) || exType == typeof(StackOverflowException))
                {
                    MemoryDump.WriteCurrentMemoryDump();
                }
                if (exType == typeof(QueryPCException))
                {
                    CSLogger.OutputLog(LogLevel.Error, ex.Message);
                }
                //CSLogger.OutputLog(LogLevel.Error, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "OnSubmittedMessageHandler Have A Error", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                BehavionOnException(source, e, ex, MessageHandlerType.OnSubmittedMessageHandler);
            }
            finally
            {
                tempWatch.Stop();
                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "OnSubmittedMessageHandler End TimeSpan=" + tempWatch.ElapsedMilliseconds, e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
            }
        }
        protected void OnResolvedMessageHandler(ResolvedMessageEventSource source, QueuedMessageEventArgs e)
        {
            if (e.MailItem == null || e.MailItem.Message == null)
            {
                return;
            }

            Stopwatch tempWatch = new Stopwatch();
            try
            {
                tempWatch.Start();
                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "OnResolvedMessageHandler Start", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                if (EmailEvalInfoMgr != null)
                {
                    if (EmailEvalInfoMgr.NeedProcess)
                    {
                        ProcessMailOnResolvedMessageHandler(source, e);
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception happened OnResolvedMessageHandler", null, ex);
                Type exType = ex.GetType();
                if (exType == typeof(AggregateException))
                {
                    exType = ex.InnerException.GetType();
                }
                if (exType == typeof(NullReferenceException) || exType == typeof(IndexOutOfRangeException) ||
                    exType == typeof(ArgumentNullException) || exType == typeof(ArithmeticException) ||
                    exType == typeof(DivideByZeroException) || exType == typeof(StackOverflowException))
                {
                    MemoryDump.WriteCurrentMemoryDump();
                }
                if (exType == typeof(QueryPCException))
                {
                    CSLogger.OutputLog(LogLevel.Error, ex.Message);
                }
                //CSLogger.OutputLog(LogLevel.Error, string.Format("Object[{1}] Subject[{2}] MessageId[{3}] Message:{0}", "Initialization Email Evaluation Information Faild", this.GetHashCode(), e.MailItem.Message.Subject, e.MailItem.Message.MessageId), ex);
                BehavionOnException(source, e, ex, MessageHandlerType.OnResolvedMessageHandler);
            }
            finally
            {
                Common.Function.AddEmailHeader(ConstVariable.Str_NextlabsHeader_Key, ConstVariable.Str_MailClassify_Enforced, e.MailItem.Message);
                tempWatch.Stop();
                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "OnResolvedMessageHandler End TimeSpan=" + tempWatch.ElapsedMilliseconds, e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
            }
        }
        #endregion

        #region Process Method
        protected void ProcessMailOnSubmittedMessageHandler(MailItem mailItem)
        {
            CSLogger.OutputLog(LogLevel.Info, "OnSubmit begin: Subject:{0} | MessageId:{1} | Object:{2}", new object[] { mailItem.Message.Subject, mailItem.Message.MessageId, this.GetHashCode() });

            //At this Event , we only evaluation the group information
            if ((null != EmailEvalInfoMgr) && (null != EmailEvalInfoMgr.GroupInfos) && (EmailEvalInfoMgr.GroupInfos.Count > 0))
            {
                List<EnvelopeRecipient> lisRecipients = new List<EnvelopeRecipient>();
                foreach (var p in EmailEvalInfoMgr.GroupInfos)
                {
                    lisRecipients.Add(p.Recipient);
                }
                CSLogger.OutputLog(LogLevel.Info, "Start Query For Groups: Subject:{0} | MessageId:{1} | Object:{2}", new object[] { mailItem.Message.Subject, mailItem.Message.MessageId, this.GetHashCode() });

                List<QueryPolicyResult> lisQueryResult = Policy.QueryPolicyEx(EmailEvalInfoMgr.EmailInfos, EmailEvalInfoMgr.Action,
                                                    EmailEvalInfoMgr.ClientType, EmailEvalInfoMgr.Sender.SmtpAddress, EmailEvalInfoMgr.SendSid,
                                                    EmailEvalInfoMgr.Classifications, lisRecipients, EmailEvalInfoMgr.PairHeaders, EmailEvalInfoMgr.GroupInfos);

                CSLogger.OutputLog(LogLevel.Info, "End Query For Groups: Subject:{0} | MessageId:{1} | Object:{2}", new object[] { mailItem.Message.Subject, mailItem.Message.MessageId, this.GetHashCode() });

                foreach (QueryPolicyResult queryResult in lisQueryResult)
                {
                    Common.PolicyResult pr = Common.PolicyResult.GetDenyPolicyResult(queryResult.LisPolicyReslts);
                    if ((null != pr) && pr.bDeny)
                    {
                        CSLogger.OutputLog(LogLevel.Warn, " Deny By Nextlabs Email Enforcer Policy:[{0}], Email Subject: {1} From: {2} To:{3}", new object[] { pr.MatchedPolicyName, mailItem.Message.Subject, EmailEvalInfoMgr.Sender.SmtpAddress, queryResult.Address.ToString() });

                        //if policy result is deny , we will do deny obligation for recipient
                        DenyMessageForRecipients(mailItem, pr, queryResult.Address);

                        //Assemble the Deny Message for sender , but do this obligation at OnResolvedMessageHandler event
                        GetNotifyObligationForSender(this.NotifyObligationsForSender, mailItem, pr, queryResult.Address);

                        //NDR obligation will remove recipient ,and pass the deny message ,so if did not do NDR obligation ,we need remove this recipient manual
                        if (IsExitesNDRObligation(pr))
                        {
                            DoNdrObligation(mailItem, pr, queryResult.Recipient);
                        }
                        else
                        {
                            RemoveEnvelopeRecipients(mailItem, queryResult.Recipient);
                            if (Config.RemoveRecipients)
                            {
                                NeedRemoveRecipients.Add(queryResult.Address);
                                foreach (var p in NeedRemoveRecipients)
                                {
                                    RemoveStationeryRecipients(mailItem, p);
                                }
                            }
                        }
                    }
                }

                //if reciient count equal zeor , OnResolvedMessageHandler will not triggered  , so we need do deny message for sender
                if (mailItem.Recipients.Count == 0)
                {
                    DenyMessageForSender();
                }
                CSLogger.OutputLog(LogLevel.Info, "End Do Obligation For Groups, Subject:{0} | MessageId:{1} | Object:{2}", new object[] { mailItem.Message.Subject, mailItem.Message.MessageId, this.GetHashCode() });
            }
            else
            {
                CSLogger.OutputLog(LogLevel.Info, "This eamil do not exist group, Subject:{0} | MessageId:{1} | Object:{2}", new object[] { mailItem.Message.Subject, mailItem.Message.MessageId, this.GetHashCode() });
            }
            CSLogger.OutputLog(LogLevel.Info, "OnSubmit end: Subject:{0} | MessageId:{1} | Object:{2}", new object[] { mailItem.Message.Subject, mailItem.Message.MessageId, this.GetHashCode() });
        }
        protected void ProcessMailOnResolvedMessageHandler(ResolvedMessageEventSource source, QueuedMessageEventArgs e)
        {
            CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "ProcessMailOnResolvedMessageHandler Start", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));

            string strHeaderPolicy = Common.Function.FindEmailHeaderValue(ConstVariable.Str_PolicyHeader_Key, e.MailItem.Message);
            if (strHeaderPolicy == null)
            {
                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Can not get policy name from header", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));

                PlugInManager obPlugInManagerIns = PlugInManager.GetInstance();
                List<INLEmailParser> lsEmailParserPlugin = obPlugInManagerIns.GetPlugins<INLEmailParser>();
                if (null != lsEmailParserPlugin)
                {
                    foreach (RouteAgent.Plugin.INLEmailParser obNLRouteAgentPluginEntry in lsEmailParserPlugin)
                    {
                        obNLRouteAgentPluginEntry.PreEvaluation(e.MailItem, EmailEvalInfoMgr, Server);
                    }
                }

                List<EnvelopeRecipient> lisRecipients = Common.Function.GetAllRecipients(e.MailItem);

                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Recipient count is " + lisRecipients.Count, e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));

                if (lisRecipients.Count > 0)
                {
                    // 如果收件人的人数 > 规定的人数，进行分组 ， LastForked表示是不是最后一次分组
                    if (lisRecipients.Count > Config.RecipientsLimited)
                    {
                        CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Recipients count is more then recipients limited , we need Fork other recipients", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                        ForkMessage(source, lisRecipients.GetRange(0, Config.RecipientsLimited));
                        CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "End Fork", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                        lisRecipients = Common.Function.GetAllRecipients(e.MailItem);
                    }
                    else
                    {
                        CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Recipients count is less then recipients limited", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                        LastForked = true;
                    }
                    //pre query policy
                    int iQueryGroupCount = CalculateQueryGroupCount(lisRecipients.Count, EmailEvalInfoMgr.EmailInfos.Count);
                    int iRecipientsCount = CalculateRecipientCount(lisRecipients.Count, iQueryGroupCount);

                    List<QueryPolicyResult> lisMutileQueryResult = new List<QueryPolicyResult>();
                    List<ForkItem> lisForkItmes = new List<ForkItem>();

                    Stopwatch sw = new Stopwatch();

                    CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "QueryGroupCount=" + iQueryGroupCount + " RecipientsCount=" + iRecipientsCount + " Start Parallel", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                    sw.Start();
                    Parallel.For(0, iQueryGroupCount, (iParallelIndex) =>
                    {
                        int index = iParallelIndex * iRecipientsCount;
                        List<EnvelopeRecipient> lisQueryRecipientsGroup;
                        if (index + iRecipientsCount < lisRecipients.Count)
                        {
                            lisQueryRecipientsGroup = lisRecipients.GetRange(index, iRecipientsCount);
                        }
                        else
                        {
                            lisQueryRecipientsGroup = lisRecipients.GetRange(index, lisRecipients.Count - index);
                        }

                        CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", string.Format("Aync Start Task Id[{0}], QueryGroup[{1}], RecipientIndex[{2}],RecipientsCount[{3}],ParallelIndex[{4}]", Task.CurrentId, iParallelIndex, index, lisQueryRecipientsGroup.Count, iParallelIndex), e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));

                        List<QueryPolicyResult> queryPolicyResult = Policy.QueryPolicyEx(EmailEvalInfoMgr.EmailInfos, EmailEvalInfoMgr.Action, EmailEvalInfoMgr.ClientType, EmailEvalInfoMgr.Sender.SmtpAddress, EmailEvalInfoMgr.SendSid, EmailEvalInfoMgr.Classifications, lisQueryRecipientsGroup, EmailEvalInfoMgr.PairHeaders, null);

                        SemaphorePolicyTable.WaitOne();
                        lisMutileQueryResult.AddRange(queryPolicyResult);
                        SemaphorePolicyTable.Release();
                    });

                    sw.Stop();
                    CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Query Policy TimeSpan=" + sw.ElapsedMilliseconds, e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                    //all task is done
                    CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "All task is done", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                    //Do find deny obligation
                    foreach (QueryPolicyResult obQueryPolicyResult in lisMutileQueryResult)
                    {
                        if (obQueryPolicyResult.Vaild)
                        {
                            if (obQueryPolicyResult.LisPolicyReslts != null)
                            {
                                Common.PolicyResult pr = Common.PolicyResult.GetDenyPolicyResult(obQueryPolicyResult.LisPolicyReslts);
                                if (null != pr && pr.bDeny)
                                {
                                    CSLogger.OutputLog(LogLevel.Warn, string.Format("Email Subject: {0} From: {1} To:{2}  Deny By Nextlabs Email Enforcer Policy Name {3}", e.MailItem.Message.Subject, EmailEvalInfoMgr.Sender.SmtpAddress, obQueryPolicyResult.Address.ToString(), pr.MatchedPolicyName));
                                    DoDeny(obQueryPolicyResult, pr, e.MailItem);
                                }
                                else
                                {
                                    string strPolicyName = GetPolicyNameFromQueryResult(obQueryPolicyResult);
                                    Common.ForkItem.Add(strPolicyName, obQueryPolicyResult.Recipient, ForkList, obQueryPolicyResult.LisPolicyReslts);
                                    Common.ForkItem.Add(strPolicyName, obQueryPolicyResult.Recipient, lisForkItmes, obQueryPolicyResult.LisPolicyReslts);
                                }
                            }
                        }
                    }
                    CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Deny Obligation Is Done", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                    if (lisForkItmes.Count > 1)
                    {
                        //fork mail
                        CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Fork Allow Mail", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));

                        ForkMessage(source, e, lisForkItmes);
                        CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "End Fork", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                    }

                    if (lisForkItmes.Count == 1)
                    {
                        strHeaderPolicy = lisForkItmes[0].PolicyName;
                    }

                }

                // last forked
                if (LastForked)
                {
                    CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Is last forked , do email notify for sender", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                    DenyMessageForSender();
                }
            }
            else
            {
                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "This is forked mail , exites policy name on header, we need do allow obligation", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
            }

            if (strHeaderPolicy != null)
            {
                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "Header policy name  is " + strHeaderPolicy, e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));

                ForkItem fokrItem = ForkList.Find(dir => { return dir.PolicyName.Equals(strHeaderPolicy, StringComparison.OrdinalIgnoreCase); });
                if (fokrItem != null)
                {
                    List<PolicyResult> lstPolicyResult = fokrItem.LisPolicyResult;
                    if (lstPolicyResult != null)
                    {
                        if (Common.PolicyResult.HavePolicyNeedProcess(lstPolicyResult))
                        {
                            DoAllow(lstPolicyResult, source, e);
                        }
                    }
                }
                CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "End Do Allow Obligation", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
            }

            CSLogger.OutputLog(LogLevel.Info, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "ProcessMailOnResolvedMessageHandler End", e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
        }
        #endregion

        #region Get Infomation Method
        protected List<EmailRecipient> GetNotifyObligationEmailReceiver(string strTaget, MailItem origMsg)
        {
            CSLogger.OutputLog(LogLevel.Debug, "GetNotifyObligationEmailReceiver Start");
            List<EmailRecipient> lisResult = new List<EmailRecipient>();
            //added sender
            if (strTaget.Equals(Common.Policy.m_strObEmailNotifyTargetValueSender, StringComparison.OrdinalIgnoreCase))
            {
                CSLogger.OutputLog(LogLevel.Debug, "Add sender");
                lisResult.Add(new EmailRecipient(origMsg.FromAddress.LocalPart, origMsg.FromAddress.ToString()));
            }

            //add recipients
            else if (strTaget.Equals(Common.Policy.m_strObEmailNotifyTargetValueReceive, StringComparison.OrdinalIgnoreCase))
            {
                CSLogger.OutputLog(LogLevel.Debug, "Add receiver");
                foreach (EnvelopeRecipient emailRecipient in origMsg.Recipients)
                {
                    lisResult.Add(new EmailRecipient(emailRecipient.Address.LocalPart, emailRecipient.Address.ToString()));
                }
            }
            return lisResult;
        }
        protected bool GetExtractGroup()
        {
            bool bResult = true;
            Common.ExtractGroup ExtractGroup = Common.Config.GetSection<Common.ExtractGroup>("extractGroup");
            if (ExtractGroup != null)
            {
                if (ExtractGroup.Value != null)
                {
                    string strExtractGroup = ExtractGroup.Value.Trim();
                    if (strExtractGroup.Equals("no", StringComparison.OrdinalIgnoreCase))
                    {
                        bResult = false;
                    }
                }

            }
            return bResult;
        }
        #endregion

        #region Do Obligation Method
        protected void DoObligationForMessage(ResolvedMessageEventSource source, QueuedMessageEventArgs e, List<Common.PolicyResult> lstPolicyResult)
        {
            CSLogger.OutputLog(LogLevel.Debug, "DoObligationForMessage Start");
            List<AppendMsgObligation> lisAppendMsgObligations = new List<AppendMsgObligation>();
            foreach (Common.PolicyResult pr in lstPolicyResult)
            {
                List<ExchangeObligation> lstObAppendMessage = Common.PolicyResult.GetExchangeObligation(pr, Common.Policy.m_strObNameAppendMessage);
                if ((null == lstObAppendMessage) || lstObAppendMessage.Count == 0)
                {
                    continue;
                }

                foreach (ExchangeObligation ob in lstObAppendMessage)
                {
                    string strAppendMessagePart = ob.GetAttribute(Common.Policy.m_strObsAppendMessagePart);
                    string strAppendMessagePosition = ob.GetAttribute(Common.Policy.m_strObsAppendMessagePosition);
                    string strAppendMessageValue = ob.GetAttribute(Common.Policy.m_strObsAppendMessageValue);

                    AppendMsgObligation appendMsgOb = new AppendMsgObligation(strAppendMessagePart, strAppendMessagePosition, strAppendMessageValue);
                    if (!AppendMsgObligation.ObligationExits(appendMsgOb, lisAppendMsgObligations))
                    {
                        lisAppendMsgObligations.Add(appendMsgOb);
                    }
                    CSLogger.OutputLog(LogLevel.Debug, "Append message obligation info:strAppendMessagePart:" + strAppendMessagePart + " strAppendMessagePosition:" + strAppendMessagePosition + " strAppendMessageValue:" + strAppendMessageValue);

                }
            }

            foreach (AppendMsgObligation item in lisAppendMsgObligations)
            {
                item.DoObligation(e.MailItem.Message);

            }

            //Email classification obligation
            DoEmailClassifyObligationForMessage(e, lstPolicyResult);

            CSLogger.OutputLog(LogLevel.Debug, "DoObligationForMessage End");
        }
        protected void DoEmailClassifyObligationForMessage(QueuedMessageEventArgs e, List<Common.PolicyResult> lstPolicyResult)
        {
            List<ExchangeObligation> lisExObligations = new List<ExchangeObligation>();
            foreach (Common.PolicyResult pr in lstPolicyResult)
            {
                List<ExchangeObligation> lstObs = Common.PolicyResult.GetExchangeObligation(pr, Common.Policy.m_strObNameMailClassify);
                if ((null == lstObs) || lstObs.Count == 0)
                {
                    continue;
                }

                lisExObligations.AddRange(lstObs);
            }

            if (lisExObligations.Count > 0)
            {
                DoEmailClassifyObligationForMessage(e, lisExObligations);
            }
        }
        protected void DoEmailClassifyObligationForMessage(QueuedMessageEventArgs e, List<ExchangeObligation> lstObs)
        {
            //get all classify item
            List<MailClassifyItem> listClassifyItem = new List<MailClassifyItem>();
            foreach (ExchangeObligation exOb in lstObs)
            {
                string ClassifyMode = exOb.GetAttribute(Policy.m_strObMailClsMode);

                for (int i = 1; i <= 3; i++)
                {
                    string strClsName = exOb.GetAttribute(Policy.m_strObMailClsNameFmt + i);
                    string strClsValue = exOb.GetAttribute(Policy.m_strObMailClsValueFmt + i);

                    //ignore if classify name is empty
                    if (string.IsNullOrWhiteSpace(strClsName))
                    {
                        continue;
                    }

                    //ignore if classify value is empty and classify mode is append
                    if (string.IsNullOrWhiteSpace(strClsValue) && ClassifyMode.Equals(Policy.m_strObMailClsModeAppend, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    //added classify item
                    if (!MailClassifyItem.ClassifyItemExist(listClassifyItem, strClsName))
                    {
                        MailClassifyItem mailClsItem = new MailClassifyItem()
                        {
                            strClassifyName = strClsName,
                            strClassifyValue = strClsValue,
                            strClassifyMode = ClassifyMode
                        };

                        listClassifyItem.Add(mailClsItem);
                    }
                }
            }

            //do classify
            foreach (MailClassifyItem clsItem in listClassifyItem)
            {
                CSLogger.OutputLog(LogLevel.Debug, string.Format("Mail classify item, name={0}, value={1}, mode={2}", clsItem.strClassifyName, clsItem.strClassifyValue, clsItem.strClassifyMode));

                if (clsItem.strClassifyMode.Equals(Policy.m_strObMailClsModeOverWrite, StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(clsItem.strClassifyValue))
                {
                    //remove
                    CSLogger.OutputLog(LogLevel.Warn, string.Format("Remove x-header:{0}", clsItem.strClassifyName));
                    Function.RemoveEmailHeader(clsItem.strClassifyName, e.MailItem.Message);
                }
                else
                {
                    Function.AddEmailHeader(clsItem.strClassifyName, clsItem.strClassifyValue, e.MailItem.Message,
             clsItem.strClassifyMode.Equals(Policy.m_strObMailClsModeOverWrite, StringComparison.OrdinalIgnoreCase));//will do overwrite
                }
            }

        }
        protected void DoObligationForAttachment(ResolvedMessageEventSource source, QueuedMessageEventArgs e, List<Common.PolicyResult> lstPolicyResult)
        {
            CSLogger.OutputLog(LogLevel.Debug, "DoObligationForAttachment Start");
            foreach (Common.PolicyResult pr in lstPolicyResult)
            {
                if (pr.emailInfo.strContentType.Equals(Common.Policy.m_strContentTypeEmailAttachment) && (null != pr.lstExchangeObligations))
                {
                    if (pr.emailInfo.attachInfo.embeddedMessage != null)
                    {
                        CSLogger.OutputLog(LogLevel.Debug, "We don't do obligation for attachment EmbeddedMessage");
                        continue;
                    }
                    string strFilePath = pr.emailInfo.strSavedPath;

                    FileType ftAttachmentFile = Common.Function.GetFileType(strFilePath);
                    if (ftAttachmentFile == FileType.Nextlabs)
                    {
                        CSLogger.OutputLog(LogLevel.Debug, "this attachment:[{0}] is NEXTLABS file, do not support to do attachment obligation\n", new object[] { strFilePath });
                    }
                    else
                    {
                        List<ExchangeObligation> lisDoNormalObligations = PolicyResult.GetExchangeObligation(pr, Policy.m_strContentTypeEmailAttachment, Policy.m_strObNameNormalTag);
                        List<ExchangeObligation> lisDoRMSObligations = PolicyResult.GetExchangeObligation(pr, Policy.m_strContentTypeEmailAttachment, Policy.m_strObNameRMS);
                        if ((null != lisDoNormalObligations && lisDoNormalObligations.Count > 0) ||
                            ((null != lisDoNormalObligations && lisDoNormalObligations.Count > 0))
                            )
                        {
                            // Prepare files
                            if (!File.Exists(strFilePath))
                            {
                                bool bSaveToLoaclDriver = false;
                                try
                                {
                                    // Cannot get attachment object with pr.emailInfo.attachInfo.attach
                                    // Must get from QueuedMessageEventArgs e.MailItem.Message.Attachments[nIndex]
                                    int nAttachmentIndex = pr.emailInfo.attachInfo.index;
                                    if ((0 <= nAttachmentIndex) && (e.MailItem.Message.Attachments.Count > nAttachmentIndex))
                                    {
                                        Attachment obAttachment = e.MailItem.Message.Attachments[nAttachmentIndex];
                                        if (null != obAttachment)
                                        {
                                            // obAttachment.GetContentReadStream() will throw an null exception at here, we cannot save the documents here
                                            // if the attachment need support obligations, it must saved at before
                                            bSaveToLoaclDriver = EmailEvalInfoManage.SaveAttachmentToSpecifyFile(obAttachment, strFilePath);
                                        }
                                    }
                                    else
                                    {
                                        CSLogger.OutputLog(LogLevel.Error, "An wrong attachment index:[{0}], total attachment count:[{1}]\n", new object[] { nAttachmentIndex, e.MailItem.Message.Attachments.Count });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "Exception during try to do attachment obligations\n", null, ex);
                                }
                                if (!bSaveToLoaclDriver)
                                {
                                    CSLogger.OutputLog(LogLevel.Error, "DoObligationForAttachment the file:[{0}] do not exist, save failed:[{1}]\n", new object[] { strFilePath, bSaveToLoaclDriver });
                                    continue;
                                }
                            }
                            //ignore zero file size
                            long length = new System.IO.FileInfo(strFilePath).Length;
                            if (length == 0)
                            {
                                CSLogger.OutputLog(LogLevel.Debug, "DoObligationForAttachment ignore zero file:" + strFilePath);
                                continue;
                            }

                            // Do obligations
                            Dictionary<string, string> dirNromalTags = null;
                            Dictionary<string, string> dirRmsTags = null;
                            if (lisDoNormalObligations.Count > 0)
                            {
                                CSLogger.OutputLog(LogLevel.Debug, "Do Normal File Tag");
                                dirNromalTags = new Dictionary<string, string>();
                                foreach (Common.ExchangeObligation ob in lisDoNormalObligations)
                                {
                                    string strTagName = ob.GetAttribute(Common.Policy.m_strObsNormalTagNameKey);
                                    string strTagValue = ob.GetAttribute(Common.Policy.m_strObsNormalTagValueKey);
                                    if (dirNromalTags.ContainsKey(strTagName))
                                    {
                                        dirNromalTags.Remove(strTagName);
                                    }
                                    dirNromalTags.Add(strTagName, strTagValue);
                                }

                                Common.ObligationFile obligationFile = Common.ObligationFile.GetRecordFileObligation(pr.emailInfo.attachInfo.lisObligationFiles, dirNromalTags, dirRmsTags, false);

                                if (obligationFile == null)
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "Can't find file at cache, we will added tag for the file");
                                    string strDestDirectory = m_strEmailOutputTempDir + pr.emailInfo.attachInfo.lisObligationFiles.Count;
                                    string strDestFileFullPath = strDestDirectory + "\\" + Path.GetFileName(strFilePath);
                                    Directory.CreateDirectory(strDestDirectory);
                                    File.Copy(strFilePath, strDestFileFullPath);

                                    strFilePath = DoNormalTagForFile(strDestFileFullPath, dirNromalTags);
                                    Common.ObligationFile.RecordFileTagOperate(pr.emailInfo.attachInfo.lisObligationFiles, m_strEmailOutputTempDir, Path.GetFileName(strFilePath), dirNromalTags, null, false);
                                }
                                else
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "find file at cache:" + obligationFile.strFilePullPath);
                                    strFilePath = obligationFile.strFilePullPath;
                                }
                            }
                            if (lisDoRMSObligations.Count > 0)
                            {
                                CSLogger.OutputLog(LogLevel.Debug, "Do RMS File Tag");
                                dirRmsTags = new Dictionary<string, string>();
                                foreach (Common.ExchangeObligation ob in lisDoRMSObligations)
                                {
                                    //get classify info from obligation
                                    for (int i = 1; i <= 3; i++)
                                    {
                                        string strClsName = ob.GetAttribute(Policy.m_strObRMSTagNameKey + i);
                                        string strClsValue = ob.GetAttribute(Policy.m_strObRMSTagValueKey + i);

                                        //ignore if classify name or classify value is empty
                                        if (string.IsNullOrWhiteSpace(strClsName) || string.IsNullOrWhiteSpace(strClsValue))
                                        {
                                            continue;
                                        }

                                        if (!dirRmsTags.ContainsKey(strClsName))
                                        {
                                            dirRmsTags.Add(strClsName, strClsValue);
                                        }
                                    }

                                }
                                Common.ObligationFile obligationFile = Common.ObligationFile.GetRecordFileObligation(pr.emailInfo.attachInfo.lisObligationFiles, dirNromalTags, dirRmsTags, true);
                                if (obligationFile == null)
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "Can't find file at cache, we will do rms fot rhe file");
                                    string strDestDirectory = m_strEmailOutputTempDir + pr.emailInfo.attachInfo.lisObligationFiles.Count;
                                    Directory.CreateDirectory(strDestDirectory);
                                    strFilePath = DoRmsForFile(strFilePath, strDestDirectory, dirRmsTags, true);
                                    Common.ObligationFile.RecordFileTagOperate(pr.emailInfo.attachInfo.lisObligationFiles, m_strEmailOutputTempDir, Path.GetFileName(strFilePath), dirNromalTags, dirRmsTags, true);
                                }
                                else
                                {
                                    CSLogger.OutputLog(LogLevel.Debug, "find rms file at cache:" + obligationFile.strFilePullPath);
                                    strFilePath = obligationFile.strFilePullPath;

                                }
                            }
                            if (lisDoNormalObligations.Count > 0 || lisDoRMSObligations.Count > 0)
                            {
                                ReplaceAttrachMent(e.MailItem.Message, pr.emailInfo.attachInfo, strFilePath);
                            }
                        }
                        else
                        {
                            CSLogger.OutputLog(LogLevel.Debug, "There is no attachment obligation need to process for attachment:[{0}]\n", new object[] { strFilePath });
                        }
                    }
                }
            }

            CSLogger.OutputLog(LogLevel.Debug, "DoObligationForAttachment End");
        }
        protected void DoObligationForApproval(ResolvedMessageEventSource source, QueuedMessageEventArgs e, List<Common.PolicyResult> lstPolicyResult)
        {
            CSLogger.OutputLog(LogLevel.Debug, "DoObligationForApproval Start");
            List<ApprovalObligation> lisApprovalObligations = new List<ApprovalObligation>();
            foreach (Common.PolicyResult pr in lstPolicyResult)
            {
                List<ExchangeObligation> lstObApproval = Common.PolicyResult.GetExchangeObligation(pr, Common.Policy.m_strObNameApproval);
                if ((null == lstObApproval) || lstObApproval.Count == 0)
                {
                    continue;
                }

                foreach (ExchangeObligation ob in lstObApproval)
                {
                    string strApprover = ob.GetAttribute(Common.Policy.m_strObsApprovalApprover);
                    CSLogger.OutputLog(LogLevel.Debug, "Approver:" + strApprover);
                    EmailRecipient emailRecipi = new EmailRecipient(strApprover.Split('@')[0], strApprover);

                    ApprovalObligation ApprovalOb = new ApprovalObligation(m_smtpServer, e.MailItem.Message, emailRecipi);
                    if (!ApprovalObligation.ObligationExits(ApprovalOb, lisApprovalObligations))
                    {
                        lisApprovalObligations.Add(ApprovalOb);
                    }
                }
            }

            foreach (ApprovalObligation item in lisApprovalObligations)
            {
                item.DoObligation();

            }
            source.Delete();
            CSLogger.OutputLog(LogLevel.Debug, "DoObligationForApproval End");
        }
        protected string DoNormalTagForFile(string strFileFullPath, Dictionary<string, string> dirTags)
        {
            CSLogger.OutputLog(LogLevel.Debug, "DoNormalTagForFile Start:" + strFileFullPath);
            if (File.Exists(strFileFullPath))
            {
                FileType fileType = Common.Function.GetFileType(strFileFullPath);
                {
                    if (fileType != FileType.OTHER)
                    {
                        CSLogger.OutputLog(LogLevel.Debug, "File Type is office2007(.docx), use openxml to added tag");
                        Common.Function.SetCustomProperty(strFileFullPath, fileType, dirTags, PropertyTypes.Text); //added tag into the file
                    }
                    else
                    {
                        CSLogger.OutputLog(LogLevel.Debug, "File Type is pdf/office2003, use ResourceAttributeMgr to added tag");

                        strFileFullPath = DoAddedResAttribute(strFileFullPath, dirTags);

                    }

                }
            }
            else
            {
                CSLogger.OutputLog(LogLevel.Debug, "File Do not Exists:" + strFileFullPath);
            }
            return strFileFullPath;
        }
        protected string DoAddedResAttribute(string strFileFullPath, Dictionary<string, string> dirTags)
        {
            string[] arryStrTagName = new string[dirTags.Count];
            string[] arryStrTagValue = new string[dirTags.Count];
            List<string> lisTagKey = new List<string>(dirTags.Keys);
            for (int i = 0; i < dirTags.Count; i++)
            {
                arryStrTagName[i] = lisTagKey[i];
                arryStrTagValue[i] = dirTags[lisTagKey[i]];
                CSLogger.OutputLog(LogLevel.Debug, "DoAddedResAttribute Tag Key:" + arryStrTagName[i]);
                CSLogger.OutputLog(LogLevel.Debug, "DoAddedResAttribute Tag Value:" + arryStrTagValue[i]);
            }

            int nErrorCode = 0;
            bool bAddResResult = Common.RightManagement.AddedResAttribute(strFileFullPath, arryStrTagName, arryStrTagValue, arryStrTagName.Length, true, ref nErrorCode);
            CSLogger.OutputLog(LogLevel.Debug, "AddedResAttribute result:" + bAddResResult + " ErrorCode:" + nErrorCode);

            return strFileFullPath;
        }
        protected string DoRmsForFile(string strFileFullPath, string strDestDirectory, Dictionary<string, string> dirTags, bool bEncrypt)
        {
            CSLogger.OutputLog(LogLevel.Debug, "Start DoRmsForAttachment:" + strFileFullPath);

            int nErrorCode = 0;

            string[] arryStrTagName = new string[dirTags.Count];
            string[] arryStrTagValue = new string[dirTags.Count];
            List<string> lisTagKey = new List<string>(dirTags.Keys);
            for (int i = 0; i < dirTags.Count; i++)
            {
                arryStrTagName[i] = lisTagKey[i];
                arryStrTagValue[i] = dirTags[lisTagKey[i]];
                CSLogger.OutputLog(LogLevel.Debug, "Tag Key:" + arryStrTagName[i]);
                CSLogger.OutputLog(LogLevel.Debug, "Tag Value:" + arryStrTagValue[i]);
            }
            bool bRmsResult = Common.RightManagement.DoRMSEx(strFileFullPath, Path.GetFileName(strFileFullPath), arryStrTagName, arryStrTagValue, arryStrTagName.Length, 0, bEncrypt, strDestDirectory, ref nErrorCode);
            CSLogger.OutputLog(LogLevel.Debug, "DoRMS result:" + bRmsResult + " ErrorCode:" + nErrorCode);
            if (bRmsResult)
            {
                string strResultFilePath = strDestDirectory + "\\" + Path.GetFileName(strFileFullPath);
                if (bEncrypt)
                {
                    strResultFilePath += ".nxl";
                }
                return strResultFilePath;
            }
            else
            {
                return strFileFullPath;
            }
        }
        private void DoNdrObligation(MailItem mailItem, Common.PolicyResult policyResult, EnvelopeRecipient removeRecipients)
        {
            if (policyResult.lstExchangeObligations != null)
            {
                NDRObligation ndrObligation = null;
                foreach (Common.ExchangeObligation ob in policyResult.lstExchangeObligations)
                {
                    if (ob.ObligationName.Equals(Common.Policy.m_strObNameEmailNDR, StringComparison.OrdinalIgnoreCase))
                    {
                        string strErrorCode = ob.GetAttribute(Common.Policy.m_strObEmailNDRErrorKey);
                        string strErrorMsg = ob.GetAttribute(Common.Policy.m_strObEmailNDRErrorMsg);
                        ndrObligation = new NDRObligation(mailItem, strErrorCode, strErrorMsg);
                        break;
                    }
                }
                if (ndrObligation != null)
                {
                    ndrObligation.DoObligation(removeRecipients);
                }
            }
        }
        private void DoDeny(QueryPolicyResult obQueryPolicyResult, PolicyResult pr, MailItem mailItem)
        {
            CSLogger.OutputLog(LogLevel.Debug, "DoDeny Obligation Start For :" + obQueryPolicyResult.Address);
            //Deny Message to Receive
            DenyMessageForRecipients(mailItem, pr, obQueryPolicyResult.Address);
            // Assemble Deny Message For Sender
            GetNotifyObligationForSender(this.NotifyObligationsForSender, mailItem, pr, obQueryPolicyResult.Address);
            // Do Ndr Obligation



            obQueryPolicyResult.Vaild = false;
            //RemovePolicyCache(PolicyTable, policyCache.Recipient);

            if (IsExitesNDRObligation(pr))
            {
                DoNdrObligation(mailItem, pr, obQueryPolicyResult.Recipient);
            }
            else
            {
                RemoveEnvelopeRecipients(mailItem, obQueryPolicyResult.Recipient);
            }
            CSLogger.OutputLog(LogLevel.Debug, "DoDeny Obligation End For :" + obQueryPolicyResult.Address);

            if (Config.RemoveRecipients)
            {
                RemoveStationeryRecipients(mailItem, obQueryPolicyResult.Address);
            }
        }
        private void DoAllow(List<PolicyResult> lstPolicyResult, ResolvedMessageEventSource source, QueuedMessageEventArgs e)
        {

            CSLogger.OutputLog(LogLevel.Debug, "DoAllow Start");
            DoObligationForMessage(source, e, lstPolicyResult);
            DoObligationForAttachment(source, e, lstPolicyResult);
            CSLogger.OutputLog(LogLevel.Debug, "DoAllow End");
        }

        //protected void DenyMessage(MailItem mailItem, Common.PolicyResult policyResult, EnvelopeRecipient removeRecipients)
        //{
        //    CSLogger.OutputLog(LogLevel.Debug, "Enter DenyMessage111");

        //    EmailMessage origMsg = mailItem.Message;

        //    NDRObligation ndrObligation = null;

        //    //first: do deny obligation(e.g. Send notify email)
        //    if (policyResult.lstExchangeObligations != null)
        //    {
        //        List<NotifyObligation> lisNotifyObligations = new List<NotifyObligation>();
        //        foreach (Common.ExchangeObligation ob in policyResult.lstExchangeObligations)
        //        {
        //            CSLogger.OutputLog(LogLevel.Debug, "ob.ObligationName:" + ob.ObligationName);
        //            if (ob.ObligationName.Equals(Common.Policy.m_strObNameEmailNDR, StringComparison.OrdinalIgnoreCase))
        //            {
        //                string strErrorCode = ob.GetAttribute(Common.Policy.m_strObEmailNDRErrorKey);
        //                string strErrorMsg = ob.GetAttribute(Common.Policy.m_strObEmailNDRErrorMsg);
        //                ndrObligation = new NDRObligation(strErrorCode, strErrorMsg);
        //            }
        //            if (ob.ObligationName.Equals(Common.Policy.m_strObNameEmailNotify, StringComparison.OrdinalIgnoreCase))
        //            {

        //                EmailRecipient emailSender = new EmailRecipient(Common.Config.EmailNotifyObligatinSenderName, Common.Config.EmailNotifyObligatinSenderEmailAddress);

        //                List<EmailRecipient> lisEmailRecipients = GetNotifyObligationEmailReceiver(ob.GetAttribute(Common.Policy.m_strObEmailNotifyTargetKey), mailItem);

        //                string strSubject = ob.GetAttribute(Common.Policy.m_strObEmailNotifySubjectKey); ;

        //                string strBody = ob.GetAttribute(Common.Policy.m_strObEmailNotifyBodyKey);

        //                string strAttachOrigEmail = ob.GetAttribute(Common.Policy.m_strObEmailNotifyAttachOrigEmailKey);


        //                NotifyObligation notifyOb = new NotifyObligation(origMsg, emailSender, lisEmailRecipients, strSubject, strBody, strAttachOrigEmail);
        //                if (!NotifyObligation.ObligationExits(notifyOb, lisNotifyObligations))
        //                {
        //                    lisNotifyObligations.Add(notifyOb);
        //                }


        //            }
        //        }

        //        foreach (NotifyObligation item in lisNotifyObligations)
        //        {
        //            item.DoObligation(m_smtpServer);
        //        }


        //        //second: Remove Recipients

        //        if (ndrObligation != null)
        //        {
        //            try
        //            {
        //                SmtpResponse response = new SmtpResponse(ndrObligation.ErrorCode, string.Empty, ndrObligation.ErrorMessage);
        //                RemoveRecipients(mailItem, removeRecipients, response);
        //            }
        //            catch (Exception ex)
        //            {
        //                CSLogger.OutputLog(LogLevel.Debug, ex);
        //                RemoveRecipients(mailItem, removeRecipients);
        //            }
        //        }
        //        else
        //        {
        //            CSLogger.OutputLog(LogLevel.Debug, "ndrObligation is null");
        //            RemoveRecipients(mailItem, removeRecipients);
        //        }
        //    }
        //    else
        //    {
        //        CSLogger.OutputLog(LogLevel.Debug, "Deny Obligation is null");
        //        RemoveRecipients(mailItem, removeRecipients);
        //    }
        //    CSLogger.OutputLog(LogLevel.Info, "leave Enter DenyMessage:" + origMsg.Subject);
        //}
        protected void DenyMessageForRecipients(MailItem mailItem, Common.PolicyResult policyResult, RoutingAddress removeRecipients)
        {
            CSLogger.OutputLog(LogLevel.Debug, "Enter DenyMessageToRecipients");
            EmailMessage origMsg = mailItem.Message;
            //first: do deny obligation(e.g. Send notify email)
            if (policyResult.lstExchangeObligations != null)
            {
                List<NotifyObligation> lisNotifyObligations = new List<NotifyObligation>();
                foreach (Common.ExchangeObligation ob in policyResult.lstExchangeObligations)
                {
                    CSLogger.OutputLog(LogLevel.Debug, "ob.ObligationName:" + ob.ObligationName);
                    if (ob.ObligationName.Equals(Common.Policy.m_strObNameEmailNotify, StringComparison.OrdinalIgnoreCase))
                    {
                        string strTaget = ob.GetAttribute(Common.Policy.m_strObEmailNotifyTargetKey);
                        if (strTaget.Equals(Common.Policy.m_strObEmailNotifyTargetValueReceive, StringComparison.OrdinalIgnoreCase) || strTaget.Equals(Common.Policy.m_strObEmailNotifyTargetValueBoth, StringComparison.OrdinalIgnoreCase))
                        {
                            EmailRecipient emailSender = new EmailRecipient(Common.Config.EmailNotifyObligatinSenderName, Common.Config.EmailNotifyObligatinSenderEmailAddress);
                            //List<EmailRecipient> lisEmailRecipients = GetNotifyObligationEmailReceiver(ob.GetAttribute(Common.Policy.m_strObEmailNotifyTargetKey), mailItem);
                            List<EmailRecipient> lisEmailRecipients = new List<EmailRecipient>()
                            {
                                new EmailRecipient(removeRecipients.LocalPart,removeRecipients.ToString())
                            };
                            string strSubject = ob.GetAttribute(Common.Policy.m_strObEmailNotifySubjectKey);
                            string strBody = ob.GetAttribute(Common.Policy.m_strObEmailNotifyBodyKey);
                            string strAttachOrigEmail = ob.GetAttribute(Common.Policy.m_strObEmailNotifyAttachOrigEmailKey);
                            NotifyObligation notifyOb = new NotifyObligation(origMsg, lisEmailRecipients, strSubject, strBody, strAttachOrigEmail);
                            //notifyOb.DenyRecipients.Add(mailItem.FromAddress.ToString());
                            notifyOb.DenyRecipients.Add(removeRecipients.ToString());
                            if (!NotifyObligation.ObligationExits(notifyOb, lisNotifyObligations))
                            {
                                lisNotifyObligations.Add(notifyOb);
                            }
                        }

                    }
                }
                foreach (NotifyObligation item in lisNotifyObligations)
                {
                    item.DoObligation(m_smtpServer);
                }
            }
            CSLogger.OutputLog(LogLevel.Debug, "leave Enter DenyMessage:" + origMsg.Subject);
        }
        private void DenyMessageForSender()
        {
            foreach (var p in NotifyObligationsForSender)
            {
                p.DoObligation(Server);
            }
        }
        private void GetNotifyObligationForSender(List<NotifyObligation> lisNotify, MailItem mailItem, Common.PolicyResult policyResult, RoutingAddress removeRecipients)
        {
            if (policyResult.lstExchangeObligations != null)
            {
                EmailMessage origMsg = mailItem.Message;
                foreach (Common.ExchangeObligation ob in policyResult.lstExchangeObligations)
                {
                    if (ob.ObligationName.Equals(Common.Policy.m_strObNameEmailNotify, StringComparison.OrdinalIgnoreCase))
                    {
                        string strTaget = ob.GetAttribute(Common.Policy.m_strObEmailNotifyTargetKey);
                        if (strTaget.Equals(Common.Policy.m_strObEmailNotifyTargetValueSender, StringComparison.OrdinalIgnoreCase) || strTaget.Equals(Common.Policy.m_strObEmailNotifyTargetValueBoth, StringComparison.OrdinalIgnoreCase))
                        {
                            EmailRecipient emailSender = new EmailRecipient(Common.Config.EmailNotifyObligatinSenderName, Common.Config.EmailNotifyObligatinSenderEmailAddress);
                            List<EmailRecipient> lisEmailRecipients = new List<EmailRecipient>()
                            {
                                new EmailRecipient(mailItem.FromAddress.LocalPart,mailItem.FromAddress.ToString())
                            };
                            string strSubject = ob.GetAttribute(Common.Policy.m_strObEmailNotifySubjectKey);
                            string strBody = ob.GetAttribute(Common.Policy.m_strObEmailNotifyBodyKey);
                            string strAttachOrigEmail = ob.GetAttribute(Common.Policy.m_strObEmailNotifyAttachOrigEmailKey);
                            NotifyObligation notifyOb = new NotifyObligation(origMsg, lisEmailRecipients, strSubject, strBody, strAttachOrigEmail);


                            NotifyObligation ExitesNotify = NotifyObligation.GetExitsObligation(notifyOb, lisNotify);
                            if (ExitesNotify != null)
                            {
                                ExitesNotify.DenyRecipients.Add(removeRecipients.ToString());
                            }
                            else
                            {
                                notifyOb.DenyRecipients.Add(removeRecipients.ToString());
                                lisNotify.Add(notifyOb);
                            }
                        }
                    }
                }
            }
        }
        protected void RemoveRecipients(MailItem mailItem, EnvelopeRecipient recipient, RoutingAddress address, SmtpResponse smtpResponse)
        {
            CSLogger.OutputLog(LogLevel.Debug, "Start Remove Recipients with SMTPResponse");
            mailItem.Recipients.Remove(recipient, DsnType.Failure, smtpResponse);

            for (int i = mailItem.Message.To.Count; i > 0; i--)
            {
                if (mailItem.Message.To[i - 1].SmtpAddress.Equals(address.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.To.Remove(mailItem.Message.To[i - 1]);
                }
            }

            for (int i = mailItem.Message.Cc.Count; i > 0; i--)
            {
                if (mailItem.Message.Cc[i - 1].SmtpAddress.Equals(address.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.Cc.Remove(mailItem.Message.Cc[i - 1]);
                }
            }

            for (int i = mailItem.Message.Bcc.Count; i > 0; i--)
            {
                if (mailItem.Message.Bcc[i - 1].SmtpAddress.Equals(address.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.Bcc.Remove(mailItem.Message.Bcc[i - 1]);
                }
            }
            CSLogger.OutputLog(LogLevel.Debug, "End Remove Recipients with SMTPResponse");
        }
        protected void RemoveRecipients(MailItem mailItem, EnvelopeRecipient recipient, RoutingAddress address)
        {
            CSLogger.OutputLog(LogLevel.Debug, "Start Remove Recipients");
            mailItem.Recipients.Remove(recipient);
            for (int i = mailItem.Message.To.Count; i > 0; i--)
            {
                if (mailItem.Message.To[i - 1].SmtpAddress.Equals(address.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.To.Remove(mailItem.Message.To[i - 1]);
                }
            }
            for (int i = mailItem.Message.Cc.Count; i > 0; i--)
            {
                if (mailItem.Message.Cc[i - 1].SmtpAddress.Equals(address.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.Cc.Remove(mailItem.Message.Cc[i - 1]);
                }
            }
            for (int i = mailItem.Message.Bcc.Count; i > 0; i--)
            {
                if (mailItem.Message.Bcc[i - 1].SmtpAddress.Equals(address.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.Bcc.Remove(mailItem.Message.Bcc[i - 1]);
                }
            }
            CSLogger.OutputLog(LogLevel.Debug, "End Remove Recipients");
        }
        private void RemoveEnvelopeRecipients(MailItem mailItem, EnvelopeRecipient recipient)
        {
            mailItem.Recipients.Remove(recipient);
        }
        private void RemovePolicyCache(Hashtable policyTable, EnvelopeRecipient recipient)
        {
            policyTable.Remove(recipient);
        }
        private void RemoveStationeryRecipients(MailItem mailItem, RoutingAddress address)
        {
            for (int i = mailItem.Message.To.Count; i > 0; i--)
            {
                if (mailItem.Message.To[i - 1].SmtpAddress.Equals(address.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.To.Remove(mailItem.Message.To[i - 1]);
                }
            }
            for (int i = mailItem.Message.Cc.Count; i > 0; i--)
            {
                if (mailItem.Message.Cc[i - 1].SmtpAddress.Equals(address.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.Cc.Remove(mailItem.Message.Cc[i - 1]);
                }
            }
            for (int i = mailItem.Message.Bcc.Count; i > 0; i--)
            {
                if (mailItem.Message.Bcc[i - 1].SmtpAddress.Equals(address.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    mailItem.Message.Bcc.Remove(mailItem.Message.Bcc[i - 1]);
                }
            }
        }
        protected void ReplaceAttrachMent(EmailMessage emailMsg, AttachInfo sourceAttachmentInfo, string strFileFullPath)
        {
            CSLogger.OutputLog(LogLevel.Debug, "ReplaceAttrachMent Start");

            EmailMessage emailMessage;
            if (EmailEvalInfoMgr.MapiMessageClass.Contains(ConstVariable.Str_MailClassify_MAPITASK))
            {
                emailMessage = emailMsg.Attachments[0].EmbeddedMessage;
            }
            else
            {
                emailMessage = emailMsg;
            }
            string strFileName = sourceAttachmentInfo.strAttachName;
            string strFilePath = strFileFullPath;
            if (Path.GetExtension(strFileFullPath).Equals(Common.ConstVariable.Str_NextlabsFile_Extension, StringComparison.OrdinalIgnoreCase))
            {
                strFileName = strFileName + Common.ConstVariable.Str_NextlabsFile_Extension;
            }

            CSLogger.OutputLog(LogLevel.Debug, "strFileName:" + strFileName);
            CSLogger.OutputLog(LogLevel.Debug, "strFilePath:" + strFilePath);

            //fix bug , when after obligation file size is 0 , we will not replease attach
            if ((new FileInfo(strFilePath)).Length > 0)
            {

                Attachment attachment = emailMessage.Attachments[sourceAttachmentInfo.index];
                if (!attachment.FileName.Equals(strFileName, StringComparison.OrdinalIgnoreCase))
                {
                    attachment.FileName = strFileName;
                }
                using (Stream streamAttachWrite = attachment.GetContentWriteStream())
                {
                    using (FileStream fs = new FileStream(strFilePath, FileMode.Open, FileAccess.Read))
                    {
                        //streamAttachWrite.Position = 0;
                        const int nReadLenOneTime = 1024 * 20;
                        byte[] byteRead = new byte[nReadLenOneTime];
                        while (true)
                        {
                            int nReadLen = fs.Read(byteRead, 0, nReadLenOneTime);
                            streamAttachWrite.Write(byteRead, 0, nReadLen);

                            if (nReadLen < nReadLenOneTime)
                            {
                                break;
                            }
                        }
                    }
                }



            }
            else
            {
                CSLogger.OutputLog(LogLevel.Debug, "File Size is 0 ,don't replace file");
            }
            CSLogger.OutputLog(LogLevel.Debug, "ReplaceAttrachMent End");
        }
        private bool IsExitesNDRObligation(Common.PolicyResult policyResult)
        {
            bool bresult = false;
            if (policyResult.lstExchangeObligations != null)
            {
                foreach (Common.ExchangeObligation ob in policyResult.lstExchangeObligations)
                {
                    if (ob.ObligationName.Equals(Common.Policy.m_strObNameEmailNDR, StringComparison.OrdinalIgnoreCase))
                    {
                        bresult = true;
                        break;
                    }
                }
            }
            return bresult;
        }
        #endregion

        #region Query Policy
        /// <summary>
        /// Query policy when had save Email
        /// </summary>
        /// <param name="lisEmailInfors"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        protected List<Common.PolicyResult> QueryPolicy(List<EmailInfo> lisEmailInfors, string strFromAddress, string strFromUserSID, List<EnvelopeRecipient> lstRecipients, string strClientType, List<KeyValuePair<string, string>> lisPairClassification, string strAction, List<KeyValuePair<string, string>> lisPairHeader)
        {
            List<Common.PolicyResult> lstPolicyResult = new List<Common.PolicyResult>();
            try
            {
                //for each emailinfo, query policy
                for (int iMailInfo = 0; iMailInfo < lisEmailInfors.Count; iMailInfo++)
                {
                    Common.EmailInfo mailInfo = lisEmailInfors[iMailInfo];
                    Common.PolicyResult policyResult = Common.Policy.QueryPolicy(EmailEvalInfoMgr.ClientType, strFromAddress, strFromUserSID, lstRecipients, mailInfo.strSavedPath, mailInfo.strContentType, lisPairClassification, strAction, lisPairHeader);
                    policyResult.emailInfo = mailInfo;
                    lstPolicyResult.Add(policyResult);
                    //when get deny, we didn't continue query policy
                    if (policyResult.bDeny)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "List<Common.PolicyResult> QueryPolicy Exception:", null, ex);
                lstPolicyResult = null;
            }
            return lstPolicyResult;
        }
        #endregion

        #region Clean
        protected void CreateEmailOutputTempDir()
        {
            try
            {
                m_strEmailOutputTempDir = g_kstrEETempFolderFullPath + Guid.NewGuid().ToString() + "\\";
                System.IO.Directory.CreateDirectory(m_strEmailOutputTempDir);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception on CreateEmailOutputTempDir:", null, ex);
            }

        }
        protected void CleanEmailOutputTempDir()
        {
            try
            {
                if (Directory.Exists(m_strEmailOutputTempDir))
                {
                    System.IO.Directory.Delete(m_strEmailOutputTempDir, true);
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "Exception on CleanEmailOutputTempDir:", null, ex);
            }
        }
        #endregion

        protected void ForkMessage(ResolvedMessageEventSource source, QueuedMessageEventArgs e)
        {
            CSLogger.OutputLog(LogLevel.Debug, "Enter ForkMessage");
            while (e.MailItem.Recipients.Count > 1)
            {
                List<EnvelopeRecipient> recips = new List<EnvelopeRecipient>();
                for (int i = 0; i < e.MailItem.Recipients.Count - 1; i++)
                {
                    recips.Add(e.MailItem.Recipients[i]);
                }

                source.Fork(recips);
            }
        }
        protected void ForkMessage(ResolvedMessageEventSource source, QueuedMessageEventArgs e, List<ForkItem> forkLists)
        {

            CSLogger.OutputLog(LogLevel.Debug, "Enter ForkMessage For Fork List");
            while (forkLists.Count > 1)
            {
                List<EnvelopeRecipient> recips = new List<EnvelopeRecipient>();
                for (int i = 0; i < forkLists.Count - 1; i++)
                {
                    recips.AddRange(forkLists[i].Recipients);
                }
                Common.Function.AddEmailHeader(ConstVariable.Str_PolicyHeader_Key, forkLists[forkLists.Count - 1].PolicyName, e.MailItem.Message);
                forkLists.RemoveAt(forkLists.Count - 1);
                source.Fork(recips);
            }
            Common.Function.RemoveEmailHeader(ConstVariable.Str_PolicyHeader_Key, e.MailItem.Message);

        }
        protected void ForkMessage(ResolvedMessageEventSource source, List<EnvelopeRecipient> lisRecipients)
        {
            CSLogger.OutputLog(LogLevel.Debug, "Start Fork Message by Recipients");

            source.Fork(lisRecipients);
        }

        protected bool NeedProcess(ResolvedMessageEventSource source, QueuedMessageEventArgs e)
        {
            EmailMessage Msg = e.MailItem.Message;

            //no need process mapi message type
            string[] strNoProcessMsgCls = { m_strMAPIMsgClsSubmitLamProbe };
            foreach (string strMsgCls in strNoProcessMsgCls)
            {
                if (strMsgCls.Equals(e.MailItem.Message.MapiMessageClass, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            List<string> lisNextlabsHeaderVals = new List<string>()
            {
                Common.ConstVariable.Str_MailClassify_DenyNotiy,
                Common.ConstVariable.Str_MailClassify_ApprovalMail
            };
            foreach (Header header in e.MailItem.Message.MimeDocument.RootPart.Headers)
            {
                if (header.Name.Equals(Common.ConstVariable.Str_NextlabsHeader_Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (lisNextlabsHeaderVals.Exists(dir => { return dir.Equals(header.Value, StringComparison.OrdinalIgnoreCase); }))
                    {
                        return false;
                    }
                }
            }

            return true;

        }
        protected bool IsDenyBehaviorOnException()
        {
            bool bresult = true;
            string strDenyOnException = Config.DenyOnException;
            if (!string.IsNullOrEmpty(strDenyOnException))
            {

                if (strDenyOnException.Equals(Common.ConstVariable.Str_NO, StringComparison.OrdinalIgnoreCase))
                {
                    bresult = false;
                }

            }
            return bresult;
        }

        protected bool NeedNotifyToAdmin()
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
        private void BehavionOnException(QueuedMessageEventSource source, QueuedMessageEventArgs e, object ex, MessageHandlerType eventHandlerType)
        {
            //implement static function,use the new code
            Function.DoExceptionNotify(ex, Server, e.MailItem);
            //Get Default behaven
            //if (NeedNotifyToAdmin())
            //{
            //    if (!string.IsNullOrEmpty(Common.Config.EmailNotifyObligatinSenderName) &&
            //        !string.IsNullOrEmpty(Common.Config.EmailNotifyObligatinSenderEmailAddress) &&
            //        !string.IsNullOrEmpty(Common.Config.ExceptionNotifyTo) &&
            //        !string.IsNullOrEmpty(Config.ExceptionNotifyAttachOriginEmail))
            //    {
            //        EmailRecipient emailSender = new EmailRecipient(Common.Config.EmailNotifyObligatinSenderName, Common.Config.EmailNotifyObligatinSenderEmailAddress);
            //        EmailRecipient emailRecipients = new EmailRecipient(Common.Config.ExceptionNotifyTo, Common.Config.ExceptionNotifyTo);
            //        string strSubject = Common.Config.ExceptionNotifySubject;
            //        string strBody = Common.Config.ExceptionNotifyBody;
            //        if (ex is Exception)
            //        {
            //            Exception exception = ex as Exception;

            //            string[] strArry = strBody.Split('%');

            //            StringBuilder sbBody = new StringBuilder();
            //            for (int i = 0; i < strArry.Length; i++)
            //            {
            //                if (i == 0)
            //                {
            //                    sbBody.Append(strArry[0]);
            //                }
            //                else
            //                {
            //                    if (strArry[i].Length > 0)
            //                    {
            //                        if (char.ToLower(strArry[i][0]).CompareTo(ConstVariable.Char_Email_Subject_Split) == 0)
            //                        {
            //                            sbBody.Append(e.MailItem.Message.Subject);
            //                            sbBody.Append(strArry[i].Substring(1, strArry[i].Length - 1));
            //                        }
            //                        else if (char.ToLower(strArry[i][0]).CompareTo(ConstVariable.Char_Email_From_Split) == 0)
            //                        {
            //                            sbBody.Append(e.MailItem.Message.From.SmtpAddress);
            //                            sbBody.Append(strArry[i].Substring(1, strArry[i].Length - 1));
            //                        }
            //                        else if (char.ToLower(strArry[i][0]).CompareTo(ConstVariable.Char_Email_Exception_Split) == 0)
            //                        {
            //                            sbBody.Append(exception.Message + " " + exception.InnerException + " StackTrace:" + exception.StackTrace);
            //                            sbBody.Append(strArry[i].Substring(1, strArry[i].Length - 1));
            //                        }
            //                        else if (char.ToLower(strArry[i][0]).CompareTo(ConstVariable.Char_Email_Recipients_Split) == 0)
            //                        {
            //                            string strRecipient = "</br>";
            //                            foreach (var recipient in e.MailItem.Recipients)
            //                            {
            //                                strRecipient += recipient.Address.ToString();
            //                                strRecipient += "</br>";
            //                            }
            //                            sbBody.Append(strRecipient);
            //                            sbBody.Append(strArry[i].Substring(1, strArry[i].Length - 1));
            //                        }
            //                        else
            //                        {
            //                            sbBody.Append("%" + strArry[i]);
            //                        }
            //                    }
            //                }
            //            }
            //            strBody = sbBody.ToString();
            //        }
            //        else
            //        {
            //            strBody = ex.ToString();
            //        }
            //        string strAttachOrigEmail = Config.ExceptionNotifyAttachOriginEmail;
            //        NotifyObligation notifyOb = new NotifyObligation(e.MailItem.Message, new List<EmailRecipient>() { emailRecipients }, strSubject, strBody, strAttachOrigEmail, true);
            //        notifyOb.DoObligation(Server);
            //    }
            //}
            if (IsDenyBehaviorOnException())
            {
                //source.Delete();
                //why we don't delete source
                //A:for fix bug 37470, we need keep mail complete the every enent

                switch (eventHandlerType)
                {
                    case MessageHandlerType.OnSubmittedMessageHandler:
                        {
                            foreach (var recipient in EmailEvalInfoMgr.GroupInfos)
                            {
                                e.MailItem.Recipients.Remove(recipient.Recipient);
                            }
                        }
                        break;
                    case MessageHandlerType.OnResolvedMessageHandler:
                        {
                            e.MailItem.Recipients.Clear();
                        }
                        break;
                    default:
                        {
                            CSLogger.OutputLog(LogLevel.Warn, string.Format("Subject:{1} | MessageId:{2} | Object:{3} | TimeTick:{4} | Message:{0}", "BehavionOnException MessageHandlerType Is Not Define Value=" + eventHandlerType, e.MailItem.Message.Subject, e.MailItem.Message.MessageId, this.GetHashCode(), DateTime.Now.Ticks));
                        }
                        break;
                }



            }

        }

        private bool CheckNeedProcess(List<QueryPolicyResult> policyTable)
        {
            bool bresult = false;
            foreach (QueryPolicyResult policyCache in PolicyTable)
            {
                if (policyCache.Vaild)
                {
                    if (Common.PolicyResult.HavePolicyNeedProcess(policyCache.LisPolicyReslts))
                    {
                        bresult = true;
                    }
                    if (bresult)
                    {
                        break;
                    }
                }
            }
            return bresult;
        }
        private List<PolicyResult> GetPolicyResultList(EnvelopeRecipient recipient, List<QueryPolicyResult> policyTable)
        {
            List<PolicyResult> lisResult = null;
            foreach (QueryPolicyResult key in policyTable)
            {
                if (key.Vaild)
                {
                    if (key.Address.ToString().Equals(recipient.Address.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        lisResult = key.LisPolicyReslts;
                        break;
                    }
                }
            }
            return lisResult;
        }

        private int CalculateQueryGroupCount(int iRecipientsCount, int iMailInfoCount)
        {
            if (iMailInfoCount < Config.MultipleQueryLimite)
            {
                int iQueryCount = iRecipientsCount * iMailInfoCount;
                //if (iQueryCount < Config.MultipleQueryLimite)
                //{
                //    return 1;
                //}
                //else
                //{
                //if (iQueryCount % Config.MultipleQueryLimite == 0)
                //{
                //    return iQueryCount / Config.MultipleQueryLimite;
                //}
                //else
                //{
                //    return iQueryCount / Config.MultipleQueryLimite + 1;
                //}

                return (iQueryCount + (Config.MultipleQueryLimite - 1)) / Config.MultipleQueryLimite;
            }
            else
            {
                return iRecipientsCount;
            }
            //}
        }
        private int CalculateRecipientCount(int iRecipientsCount, int iQueryGroupCount)
        {
            if (iRecipientsCount % iQueryGroupCount == 0)
            {
                return iRecipientsCount / iQueryGroupCount;
            }
            else
            {
                return iRecipientsCount / iQueryGroupCount + 1;
            }
        }

        private string GetPolicyNameFromQueryResult(QueryPolicyResult queryResult)
        {
            StringBuilder sbPolicyName = new StringBuilder();
            foreach (Common.PolicyResult result in queryResult.LisPolicyReslts)
            {
                if (result.lstExchangeObligations != null)
                {
                    foreach (ExchangeObligation obligation in result.lstExchangeObligations)
                    {
                        sbPolicyName.Append(obligation.PolicyName);
                    }
                }
            }
            return sbPolicyName.ToString();
        }
    }

}
