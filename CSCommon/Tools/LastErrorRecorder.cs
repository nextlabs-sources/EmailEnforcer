using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSBase.Common
{
    public static class LastErrorRecorder
    {
        #region Error code define
        public const int ERROR_SUCCESS =                0;
        public const int ERROR_UNKNOWN =                -1;
        public const int ERROR_ACCESS_DENY =            -2;
        public const int ERROR_DATA_READ_FAILED =       -3;
        public const int ERROR_DATA_WRITE_FAILED =      -4;
        public const int ERROR_DBCONNECTION_FAILED =    -5;
        public const int ERROR_KEY_INCORRECT =          -6;
        public const int ERROR_KEY_NOT_EXIST =          -7;
        public const int ERROR_REFERENCE_NOT_FOUND =    -8;
        #endregion

        static private Dictionary<int, int> s_dirLastError = new Dictionary<int,int>();

        static public void SetLastError(int nErrorCode)
        {
            int nCurThreadId = Thread.CurrentThread.ManagedThreadId;
            lock(s_dirLastError)
            {
                CommonTools.AddKeyValuesToDic(s_dirLastError, nCurThreadId, nErrorCode, false);
            }
        }
        static public int GetLastError()
        {
            int nCurThreadId = Thread.CurrentThread.ManagedThreadId;
            lock(s_dirLastError)
            {
                return CommonTools.GetValueByKeyFromDic(s_dirLastError, nCurThreadId, 0);
            }
        }
        

    }
}
