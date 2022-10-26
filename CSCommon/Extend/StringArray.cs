using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSBase.Common
{
    static public class StringArray
    {
        static public bool IsExist(this string[] szStr, string strIn, StringComparison emStringComparison = StringComparison.Ordinal)
        {
            foreach (string strItem in szStr)
            {
                if (strItem.Equals(strIn, emStringComparison))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
