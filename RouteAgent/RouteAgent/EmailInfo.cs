using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Exchange.Data.Transport.Email;
namespace RouteAgent.Common
{
    public class EmailInfo
    {
       public string strContentType; // subject, body, attachment
       public string strName;
       public string strSavedPath;
       //public Attachment attach; // just for attachment
       public AttachInfo attachInfo;
    }
}
