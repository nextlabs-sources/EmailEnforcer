using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CSBase.Common;

using CSBase.Diagnose;

namespace RouteAgent
{
    class MyDomainInfo
    {
        public string StandardEmailAddr { get; private set; }
        public string UserPrincipalName { get; private set; }
        public string SecurityID { get; private set; }

        public MyDomainInfo(string strStandardEmailAddrIn, string strUserPrincipalNameIn, string strSIDIn)
        {
            StandardEmailAddr = strStandardEmailAddrIn;
            UserPrincipalName = strUserPrincipalNameIn;
            SecurityID = strSIDIn;
        }
    }

    class DomainInfoMgr
    {
        #region Singleton
        static private object s_obLockForMyLogInstance = new object();
        static private DomainInfoMgr s_obDomainInfoMgrIns = null;
        static public DomainInfoMgr GetInstance()
        {
            if (null == s_obDomainInfoMgrIns)
            {
                lock (s_obLockForMyLogInstance)
                    if (null == s_obDomainInfoMgrIns)
                    {
                        s_obDomainInfoMgrIns = new DomainInfoMgr();
                    }
            }
            return s_obDomainInfoMgrIns;
        }
        private DomainInfoMgr()
        {

        }
        ~DomainInfoMgr() { }
		#endregion

		#region Public tools
		public string GetUserSecurityIDByEmailAddress(string strUserEmailAddress, bool bAutoCheckWithLogonName, string strDefaultSID, bool bDeepSearchForest, bool bIncludeGroup)
        {
            string strSIDOut = strDefaultSID;
            MyDomainInfo obMyDomainInfo = GetAndEstablishDomainInfoByEmailAddr(strUserEmailAddress, bAutoCheckWithLogonName, bDeepSearchForest, bIncludeGroup);
            if (null != obMyDomainInfo)
            {
                strSIDOut = obMyDomainInfo.SecurityID;
            }
            return strSIDOut;
        }
        public string GetStandardEmailAddressFromAD(string strUserEmailAddressOrg, bool bFailedReturnOriginalAddr, bool bDeepSearchForest, bool bIncludeGroup)
        {
            string strStandardEmailAddrOut = (bFailedReturnOriginalAddr ? strUserEmailAddressOrg : "");
            MyDomainInfo obMyDomainInfo = GetAndEstablishDomainInfoByEmailAddr(strUserEmailAddressOrg, true, bDeepSearchForest, bIncludeGroup);
            if (null != obMyDomainInfo)
            {
                strStandardEmailAddrOut = obMyDomainInfo.StandardEmailAddr;
            }
            return strStandardEmailAddrOut;
        }
		#endregion

		#region Inner tools
		private MyDomainInfo GetAndEstablishDomainInfoByEmailAddr(string strUserEmailAddress, bool bAutoCheckWithLogonName, bool bDeepSearchForest, bool bIncludeGroup)
        {
            MyDomainInfo obMyDomainInfo = m_cacheDomainInfo.GetValueByKey(strUserEmailAddress, null);
            if (null == obMyDomainInfo)
            {
                string strStandardEmailAddrRef = "";
                string strUserPrincipalNameRef = "";
                string strSIDRef = "";
                bool bSuccess = DomainHelper.GetUserBaseInfoFromAD(strUserEmailAddress, bAutoCheckWithLogonName, ref strStandardEmailAddrRef, ref strUserPrincipalNameRef, ref strSIDRef, bDeepSearchForest, bIncludeGroup);
                if (bSuccess)
                {
                    // Effective check, in exchange environment Email, UPN, SID must be exist
                    if (String.IsNullOrEmpty(strStandardEmailAddrRef) || String.IsNullOrEmpty(strStandardEmailAddrRef) || String.IsNullOrEmpty(strStandardEmailAddrRef))
                    {
                        CSLogger.OutputLog(LogLevel.Error, "Logic error, get user base info from AD by email:[{0}] with result:[{1}], but the user info:[Emai:{2}, UPN:{3}, SID:{4}] are not all effective, pelease check", new object[] { strUserEmailAddress, bSuccess, strStandardEmailAddrRef, strUserPrincipalNameRef, strSIDRef });
                    }
                    else
                    {
                        // Save to cache
                        obMyDomainInfo = new MyDomainInfo(strStandardEmailAddrRef, strUserPrincipalNameRef, strSIDRef);
                        m_cacheDomainInfo.AddValueByKey(strStandardEmailAddrRef, obMyDomainInfo, false);
                        if (!String.Equals(strStandardEmailAddrRef, strUserPrincipalNameRef, StringComparison.OrdinalIgnoreCase))
                        {
                            m_cacheDomainInfo.AddValueByKey(strUserPrincipalNameRef, obMyDomainInfo, false);
                        }
                    }
                }
            }
            return obMyDomainInfo;
        }
        #endregion

        #region Members
        DataMemoryCache<string, MyDomainInfo> m_cacheDomainInfo = new DataMemoryCache<string, MyDomainInfo>(StringComparer.OrdinalIgnoreCase);
        #endregion
    }
}
