using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSBase.Common
{
    public static class CommonValues
    {
        public static readonly Dictionary<EMCompareOp, string> g_kdicLogicOpEnumAndString = new Dictionary<EMCompareOp, string>()
        {
            { EMCompareOp.emUnknown, "" },

            { EMCompareOp.emAbove, ">" },
            { EMCompareOp.emAboveEqual, ">=" },

            { EMCompareOp.emEqual, "==" },
            { EMCompareOp.emEqualSingle, "=" },
            { EMCompareOp.emNotEqual, "!=" },

            { EMCompareOp.emLess, "<" },
            { EMCompareOp.emLessEqual, "<=" }
        };
        public static readonly Dictionary<string, EMCompareOp> g_kdicLogicOpStringAndEnum = new Dictionary<string, EMCompareOp>()
        {
            { "", EMCompareOp.emUnknown },

            { ">", EMCompareOp.emAbove },
            { ">=", EMCompareOp.emAboveEqual },

            { "==", EMCompareOp.emEqual },
            { "=", EMCompareOp.emEqualSingle },
            { "!=", EMCompareOp.emNotEqual },

            { "<", EMCompareOp.emLess },
            { "<=", EMCompareOp.emLessEqual }
        };
    }
}
