using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
namespace RouteAgent.Common.Model
{
    [DataContract(Namespace = "Nextlabs.Model.Email.EmailAddress")]
    public class EmailAddress
    {
        [DataMember(Order = 0)]
        public string DisplayName
        {
            get;
            set;
        }
        [DataMember(Order = 1)]
        public string SmtpAddress
        {
            get;
            set;
        }
    }

    [DataContract(Namespace = "Nextlabs.Model.Email.EmailHeader")]
    public class EmailHeader
    {
        [DataMember(Order = 0)]
        public string HeaderName
        {
            get;
            set;
        }
        [DataMember(Order = 1)]
        public string HeaderValue
        {
            get;
            set;
        }
    }

    [DataContract(Namespace = "Nextlabs.Model.Email.Attachment")]
    public class EmailAttachment
    {
        [DataMember(Order = 0)]
        public string FileName
        {
            get;
            set;
        }
        [DataMember(Order = 1)]
        public byte[] FileContent
        {
            get;
            set;
        }
        [DataMember(Order = 2)]
        public string ContentType
        {
            get;
            set;
        }
    }

    [DataContract(Namespace = "Nextlabs.Model.Email.EmailBody")]
    public class EmailBody
    {
        [DataMember(Order = 0)]
        public string BodyFormat
        {
            get;
            set;
        }
        [DataMember(Order = 1)]
        public byte[] BodyContent
        {
            get;
            set;
        }
    }

    [DataContract(Namespace = "Nextlabs.Model.Email.EmailModel")]
    public class EmailModel
    {
        [DataMember(Order = 0)]
        public EmailAddress From
        {
            get;
            set;
        }
        [DataMember(Order = 1)]
        public List<EmailAddress> To
        {
            get;
            set;
        }
        [DataMember(Order = 2)]
        public List<EmailAddress> Cc
        {
            get;
            set;
        }
        [DataMember(Order = 3)]
        public string Subject
        {
            get;
            set;
        }
        [DataMember(Order = 4)]
        public EmailBody Body
        {
            get;
            set;
        }
        [DataMember(Order = 5)]
        public List<EmailAttachment> EmailAttachments
        {
            get;
            set;
        }
        [DataMember(Order = 6)]
        public List<EmailHeader> Headers
        {
            get;
            set;
        }
    }
}
