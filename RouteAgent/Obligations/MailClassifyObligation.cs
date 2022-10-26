using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RouteAgent.Common
{
    public class MailClassifyItem
    {
        public string strClassifyName;
        public string strClassifyValue;
        public string strClassifyMode;

        public static bool ClassifyItemExist(List<MailClassifyItem> lstItems, string strClassifyName)
        {
            MailClassifyItem item = lstItems.Find((MailClassifyItem clsItem) => clsItem.strClassifyName.Equals(strClassifyName, StringComparison.OrdinalIgnoreCase));
            return item != null;
        }
    }

    class MailClassifyObligation
    {
        
    }
}
