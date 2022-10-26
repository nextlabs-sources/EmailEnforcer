using CSBase.Diagnose;

using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSBase.Common
{
    public static class RegisterTools
    {
        public static string GetRegStringValue(RegistryKey obRegRootKey, string strRegKey, string strRegItemKey, string strDefaultValue)
        {
            string strValueRet = strDefaultValue;
            RegistryKey obRegItemKey = null;
            try
            {
                obRegItemKey = obRegRootKey.OpenSubKey(strRegKey, false);
                if (null == obRegItemKey)
                {
                    // Using default value
                }
                else
                {
                    object ReglogInstallDir = obRegItemKey.GetValue(strRegItemKey);
                    if (ReglogInstallDir == null)
                    {
                        // Using default value
                    }
                    else
                    {
                        strValueRet = Convert.ToString(ReglogInstallDir);
                    }
                }
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during read reg string value from:[{0}, {1}, {2}], default value:[{3}]\n", new object[] { obRegRootKey, strRegKey, strRegItemKey, strDefaultValue }, ex);
            }
            finally
            {
                if (null != obRegItemKey)
                {
                    obRegItemKey.Close();
                }
            }
            return strValueRet;
        }
    }
}
