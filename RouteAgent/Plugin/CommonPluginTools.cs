using RouteAgent.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteAgent.Plugin
{
    static class CommonPluginTools
    {
        #region User info
        public static string GetUserSecurityIDByEmailAddressByUserParserPlugin(List<INLUserParser> lsUserParserPlugin, string strUserEmailAddress, bool bAutoCheckWithLogonName, string strDefaultSID)
        {
            string strSIDRet = "";

            if (null != lsUserParserPlugin)
            {
                foreach (INLUserParser obUserParser in lsUserParserPlugin)
                {
                    strSIDRet = obUserParser.GetUserSecurityIDByEmailAddress(strUserEmailAddress, bAutoCheckWithLogonName, "");
                    if (!String.IsNullOrEmpty(strSIDRet))
                    {
                        break;
                    }
                }
            }

            if (String.IsNullOrEmpty(strSIDRet))
            {
                bool bDeapSearch = Config.IsNeedSearchUserAttrInForest();

                DomainInfoMgr obDomainInfoMgrIns = DomainInfoMgr.GetInstance();
                strSIDRet = obDomainInfoMgrIns.GetUserSecurityIDByEmailAddress(strUserEmailAddress, bAutoCheckWithLogonName, strDefaultSID, bDeapSearch, true);
            }
            return strSIDRet;
        }
        public static string GetStandardEmailAddressFromADByUserParserPlugin(List<INLUserParser> lsUserParserPlugin, string strEmailAddrIn, bool bFailedReturnOriginalAddr)
        {
            string strStandardEmailAddr = "";

            if (null != lsUserParserPlugin)
            {
                foreach (INLUserParser obUserParser in lsUserParserPlugin)
                {
                    strStandardEmailAddr = obUserParser.GetStandardEmailAddressFromAD(strEmailAddrIn, false);
                    if (!String.IsNullOrEmpty(strStandardEmailAddr))
                    {
                        break;
                    }
                }
            }

            if (String.IsNullOrEmpty(strStandardEmailAddr))
            {
                bool bDeapSearch = Config.IsNeedSearchUserAttrInForest();

                DomainInfoMgr obDomainInfoMgrIns = DomainInfoMgr.GetInstance();
                strStandardEmailAddr = obDomainInfoMgrIns.GetStandardEmailAddressFromAD(strEmailAddrIn, bFailedReturnOriginalAddr, bDeapSearch, true);
            }

            return strStandardEmailAddr;
        }
        public static List<KeyValuePair<string, string>> GetUserAttributesInfo(List<INLUserParser> lsUserParserPlugin, string strUserStandardEmailAddress)
        {
            List<KeyValuePair<string, string>> lsUserAttrRet = new List<KeyValuePair<string, string>>();

            if (null != lsUserParserPlugin)
            {
                foreach (INLUserParser obUserParser in lsUserParserPlugin)
                {
                    obUserParser.AdjustUserAttributesInfo(strUserStandardEmailAddress, ref lsUserAttrRet);
                }
            }

            return lsUserAttrRet;
        }
        #endregion
    }
}
