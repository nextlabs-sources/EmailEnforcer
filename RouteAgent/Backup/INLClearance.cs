using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nextlabs.RouteAgent.PlugIn
{
    /// <summary>
    /// usually thread-plugin storage information in hearlist, but in complex case, headlist maybe could not satisfy plugin's requirement
    /// so we provide another object "MailItem"
    /// </summary>
    public enum ObJectType
    {
        /// <summary>
        /// Byte array that convert from the object of Microsoft.Exchange.Data.Transport.MailItem. Message.MimeDocument.RootPart.Headers
        /// 
        /// </summary>
        XHeaderByteArry,
        /// <summary>
        /// Microsoft.Exchange.Data.Transport.MailItem
        /// Get mailItem from Microsoft.Exchange.Data.Transport.Routing.QueuedMessageEventArgs Object
        /// </summary>
        MailItem,
        /// <summary>
        /// List<KeyValuePair<string,string>> lisClearance
        /// Get from Microsoft.Exchange.Data.Mime.Headers object
        /// key is Header.Name, value is Header.Value
        /// </summary>
        HeadersKeyValuePair
    }
    public enum HRESULT
    {
        NO_ERROR,//Function succeeds with out any error
        ERROR_UNSUPPORTTYPE,//function error , because input object is not support
        ERROR_OTHER//function error , for example , it happen excetion or other uncontrollable problem
    }
    public interface INLClearance
    {
        /// <summary>
        /// thread-plugin can storage clearance anywhere, EmailItem Object or HeadList
        /// call this function, return which object this plugin supported
        /// </summary>
        /// <param name="SupportedObject">This object is out parameter</param>
        /// <returns></returns>
        HRESULT GetSupportObject(out ObJectType SupportedObject);

        /// <summary>
        /// This function is used to get Clearance information from mail.
        /// The clearance maybe an encrypt by Microsoft or third part plug-in.
        /// The third plug-in can implement this function to provide a way to get the mail clearance info.
        /// Nextlabs Exchange Enforcer can invoke it and get the right mail classification info.
        /// </summary>
        /// <param name="IntPut">this object is input object</param>
        /// <param name="lisClearance">
        ///     This object is out parameter,
        ///     if have same item , for example key itar have different value,you could push two keyvaluepair object to list.new KeyValuePair<string,string>("itar","yes") and new KeyValuePair<string,string>("itar","no")
        /// </param>
        /// <returns></returns>
        HRESULT GetEmailClearance(object IntPut, out List<KeyValuePair<string, string>> lisClearance);
    }

}
