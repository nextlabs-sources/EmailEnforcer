using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace RouteAgent.Common.JsonHelperDataModule
{
    #region Json Helper Request Data Module
    [DataContract]
    public class JsonHelperRequest
    {
        [DataMember]
        public RequestNode Request { get; set; }
    }
    [DataContract]
    public class RequestNode
    {
        [DataMember]
        public bool CombinedDecision { get; set; }

        [DataMember]
        public bool ReturnPolicyIdList { get; set; }

        [DataMember]
        public string XPathVersion { get; set; }

        [DataMember]
        public List<CategoryNode> Category { get; set; }

        [DataMember]
        public List<SubjectNode> Subject { get; set; }

        [DataMember]
        public List<RecipientNode> Recipient { get; set; }

        [DataMember]
        public List<ActionNode> Action { get; set; }

        [DataMember]
        public List<ResourceNode> Resource { get; set; }

        [DataMember]
        public MultiRequestsNode MultiRequests { get; set; }
    }
    [DataContract]
    public class MultiRequestsNode
    {
        [DataMember]
        public List<RequestReferenceNode> RequestReference { get; set; }
    }
    [DataContract]
    public class RequestReferenceNode
    {
        [DataMember]
        public List<string> ReferenceId { get; set; }
    }
    [DataContract]
    public class ResourceNode
    {
        [DataMember]
        public string CategoryId { get; set; }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public List<AttributeNode> Attribute { get; set; }
    }
    [DataContract]
    public class ActionNode
    {
        [DataMember]
        public string CategoryId { get; set; }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public List<AttributeNode> Attribute { get; set; }
    }

    [DataContract]
    public class RecipientNode
    {
        [DataMember]
        public string CategoryId { get; set; }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public List<AttributeNode> Attribute { get; set; }
    }

    [DataContract]
    public class SubjectNode
    {
        [DataMember]
        public string CategoryId { get; set; }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public List<AttributeNode> Attribute { get; set; }
    }
    [DataContract]
    public class CategoryNode
    {
        [DataMember]
        public string CategoryId { get; set; }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public List<AttributeNode> Attribute { get; set; }
    }
    [DataContract]
    public class AttributeNode
    {
        [DataMember]
        public string AttributeId { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public string DataType { get; set; }

        [DataMember]
        public bool IncludeInResult { get; set; }
    }
#endregion

    #region Json helper Response Data Module
    [DataContract]
    public class JsonHelperResponse
    {
        [DataMember]
        public ResponseNode Response { get; set; }
    }

    [DataContract]
    public class ResponseNode
    {
        [DataMember]
        public List<ResultNode> Result { get; set; }
    }

    [DataContract]
    public class ResultNode
    {
        [DataMember]
        public string Decision { get; set; }

        [DataMember]
        public StatusNode Status { get; set; }

        [DataMember]
        public List<ObligationsNode> Obligations { get; set; }
    }

    [DataContract]
    public class StatusNode
    {
        [DataMember]
        public string StatusMessage { get; set; }
        [DataMember]
        public StatusCodeNode StatusCode { get; set; }
    }
    [DataContract]
    public class StatusCodeNode
    {
        [DataMember]
        public string Value { get; set; }
    }
    [DataContract]
    public class ObligationsNode
    {
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public List<AttributeAssignmentNode> AttributeAssignment { get; set; }
    }
    [DataContract]
    public class AttributeAssignmentNode
    {
        [DataMember]
        public string AttributeId { get; set; }
        [DataMember]
        public List<string> Value { get; set; }
    }
#endregion
}
