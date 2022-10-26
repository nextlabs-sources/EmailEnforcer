using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
namespace RouteAgent.Common.Model
{
    [DataContract(Namespace = "Nextlabs.Model.Parm.ApprovalParm")]
    public class ApprovalParm
    {
        [DataMember(Order = 0)]
        public string MessageId
        {
            get;
            set;
        }
        [DataMember(Order = 1)]
        public string ApprovalResult
        {
            get;
            set;
        }
        [DataMember(Order = 2)]
        public string Approver
        {
            get;
            set;
        }
    }
}
