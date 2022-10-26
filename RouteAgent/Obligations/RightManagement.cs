using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SDKWrapperLib;
using CSBase.Diagnose;

namespace RouteAgent.Common
{
    public class RightManagement
    {
        static RightManagement()
        {

        }

        public static bool DoRMSEx(string sourcepath, string originalfilename, string[] tagnames, string[] tagvalues,
                                                            int tagcount, int mode, bool encryption,
                                                            string outpath, ref int errcode)
        {
#if true
            CSLogger.OutputLog(LogLevel.Error, string.Format("Currently do not support RMS: encrypt and tag attachment. File:[%s]\n", originalfilename));
            return true;
#else
            //
            INLRightsManager nlRManager = new NLRightsManager();

            //set tag
            char[] splits = new char[1];
            splits[0] = ConstVariable.RMSClassifyMultiValueSplit;
            for (int i = 0; i < tagcount; i++ )
            {
                //split value
                string[] arrayValue = tagvalues[i].Split(splits,  StringSplitOptions.RemoveEmptyEntries);
                foreach(string strvalue in arrayValue)
                {
                    nlRManager.NLSetTag(tagnames[i], strvalue);
                }
            }

            //encrypt
            nlRManager.NLEncrypt(sourcepath, outpath + "\\" + originalfilename + ".nxl");

            return true;
#endif
        }


        public static bool AddedResAttribute(string sourcepath,  string[] tagnames, string[] tagvalues,
                                                            int tagcount, bool bOverWrite, ref int errcode)
        {
            try
            {
                FileTagManager tagMgr = new FileTagManager();
                for (int i = 0; i < tagcount; i++)
                {
                    tagMgr.SetUpdateTag(tagnames[i], tagvalues[i]);
                }
                errcode = tagMgr.UpdateFileTagWithTagMethod(sourcepath, bOverWrite ? 1 : 0);
            }
            catch(Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error,  string.Format("AddedResAttribute failed.{0}\n", ex.Message ) );
            }

            return errcode == 0;
        }

    }
}
