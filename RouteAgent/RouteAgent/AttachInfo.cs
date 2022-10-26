using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Exchange.Data.Transport.Email;
using System.Text;

namespace RouteAgent.Common
{
    public class AttachInfo
    {
        public Attachment attach;
        public string strAttrachContentType;
        public string strAttachName;
        public string strAttachSavedFileFullPath;
        public EmailMessage embeddedMessage;
        public List<ObligationFile> lisObligationFiles;
        public int index;
        public List<KeyValuePair<string, string>> listTags; //read tag in PEP for CE8.7 didn't read file tag when resource type is not "fso"


        public void AddClassificationInfo(List<KeyValuePair<string, string>> lsFileTagsIn)
        {
            if (null != lsFileTagsIn)
            {
                if (null == listTags)
                {
                    listTags = new List<KeyValuePair<string, string>>();
                }
                listTags.AddRange(lsFileTagsIn);
            }
        }
    }
}
