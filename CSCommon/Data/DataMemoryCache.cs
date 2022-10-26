using CSBase.Diagnose;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSBase.Common
{
    public class DataMemoryCache<TyKey, TyValue>
    {
        #region Constructor
        public DataMemoryCache(IEqualityComparer<TyKey> tyKeyComparer)
        {
            m_dicCacheInfo = new Dictionary<TyKey, TyValue>(tyKeyComparer);
        }
        #endregion

        #region Get, Add(Update), Remove
        public TyValue GetValueByKey(TyKey tyKeyIn, TyValue tyDefaultValue)
        {
            TyValue tyRetOut = tyDefaultValue;
            try
            {
                m_rwCacheInfo.EnterReadLock();

                tyRetOut = CommonTools.GetValueByKeyFromDic(m_dicCacheInfo, tyKeyIn, tyDefaultValue);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during get value from cache by key:[{0}], default value:[{1}]", new object[] { tyKeyIn, tyDefaultValue }, ex);
            }
            finally
            {
                m_rwCacheInfo.ExitReadLock();
            }
            return tyRetOut;
        }
        public bool AddValueByKey(TyKey tyKeyIn, TyValue tyValueIn, bool bFailedIfExist)
        {
            bool bRet = false;
            try
            {
                m_rwCacheInfo.EnterWriteLock();

                bRet = CommonTools.AddKeyValuesToDic(m_dicCacheInfo, tyKeyIn, tyValueIn, bFailedIfExist);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during add value to cache by key:[{0}], value:[{1}]", new object[] { tyKeyIn, tyValueIn }, ex);
            }
            finally
            {
                m_rwCacheInfo.ExitWriteLock();
            }
            return bRet;
        }
        public void RemoveItemByKey(TyKey tyKeyIn)
        {
            try
            {
                m_rwCacheInfo.EnterWriteLock();

                CommonTools.RemoveKeyValuesFromDic(m_dicCacheInfo, tyKeyIn);
            }
            catch (Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Debug, "Exception during remove item from cache by key:[{0}]", new object[] { tyKeyIn }, ex);
            }
            finally
            {
                m_rwCacheInfo.ExitWriteLock();
            }
        }
        #endregion

        #region Members
        private ReaderWriterLockSlim m_rwCacheInfo = new ReaderWriterLockSlim();
        private Dictionary<TyKey, TyValue> m_dicCacheInfo = null;
        #endregion
    }
}
