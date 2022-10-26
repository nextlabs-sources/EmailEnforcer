using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.ActiveDirectory;
using CSBase.Diagnose;
using CSBase.Common;

namespace CSharpCommonTest
{
    class DomainHelperTester
    {
        public static void Test()
        {
            {
                DomainHelper.LoopForestDomainProperties(Domain.GetCurrentDomain().Forest, DomainHelper.g_kstrADKey_ObjectClass, "*", DomainHelper.g_kstrADKey_Mail, "*");
                // CommonADHelper.LoopAllADUserProperties("mail", "*");
            }

#if true
            {
                Dictionary<string, bool> dirEmailInfo = new Dictionary<string, bool>()
                {
                    { "group1@auto.com", true},
                    { "auto3@auto.com", false},
                    {"kim.dev@lab11.auto.com", true},
                    {"kim.dev@auto.com", true},

                    {"doglab11.dev@lab11.auto.com", true},
                    {"dog.dev@auto.com", true},

                    {"piglab11.dev@lab11.auto.com", true},
                    {"piglab11.alias@lab11.auto.com", false},

                    {"pig.alias@auto.com", true},
                    {  "pig.dev@auto.com", true },
                };

                InnreTestApis(dirEmailInfo);
            }
#endif

#if false
            {
                List<string> lsEmailInfo = new List<string>()
                {
                    "chameleon.dev@office.dev",
                    "chameleon.alias@office.dev",

                    "kim.dev@office.dev",
                    "kim.alias@office.dev",

                    "Monkey.dev@office.dev",
                    "Monkey.alias@office.dev"
                };

                InnreTestApis(lsEmailInfo);
            }
#endif

#if false
            {
                Dictionary<string, bool> dirEmailInfo = new Dictionary<string, bool>()
                {
                //	{"kim.yang@nextlabs.com", false},
                    {"kyang@nextlabs.com", true},

                    {"joy.wu@nextlabs.com", true},
                    {"jxwu@nextlabs.com", true},
                    {"jwy@nextlabs.com", false}
                };

                InnreTestApis(dirEmailInfo);
            }
#endif
        }
        private static void InnreTestApis(Dictionary<string, bool> dirEmailInfo)
        {
            foreach (KeyValuePair<string, bool> pairItem in dirEmailInfo)
            {
                string strUserEmailAddress = pairItem.Key;

                {
                    string strSID = DomainHelper.GetUserSecurityIDByEmailAddress(strUserEmailAddress, true, "", true, true);
                    CSLogger.OutputLog(LogLevel.Debug, "GetUserSessionIDByEamilAddress, deep search: User:[{0}], SID:[{1}]", new object[] { strUserEmailAddress, strSID });

                    string strStandardEmailAddr = DomainHelper.GetStandardEmailAddressFromAD(strUserEmailAddress, false, true, true);
                    CSLogger.OutputLog(LogLevel.Debug, "GetStandardEamilAddressFromAD, deep search: User:[{0}], Standard Email:[{1}]", new object[] { strUserEmailAddress, strStandardEmailAddr });

                    Console.WriteLine("\n\tUser:[{0}], SID:[{1}], StandardEamil:[{2}], expect flag:[{3}]", new object[] { strUserEmailAddress, strSID, strStandardEmailAddr, pairItem.Value });
                }

                {
                    string strStandardEmailAddrRef = strUserEmailAddress;
                    string strUserPrincipalNameRef = strUserEmailAddress;
                    string strSIDRef = "s-1001";
                    bool bDeepSearchForest = true;
                    bool bRet = DomainHelper.GetUserBaseInfoFromAD(strUserEmailAddress, true, ref strStandardEmailAddrRef, ref strUserPrincipalNameRef, ref strSIDRef, bDeepSearchForest, true);

                    Console.WriteLine("\n\tGet user:[{0}] base info with result:[{1}], expect result:[{6}]\n\t\tstandard eamil:[{2}], UPN:[{3}], SID:[{4}], bDeepSearchForest:[{5}]", new object[] { strUserEmailAddress, bRet, strStandardEmailAddrRef, strUserPrincipalNameRef, strSIDRef, bDeepSearchForest, pairItem.Value });
                }
            }
        }
    }
}
