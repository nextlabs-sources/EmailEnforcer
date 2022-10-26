using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RouteAgent.Plugin
{
    [XmlRootAttribute("pluginsConfig", Namespace = "", IsNullable = false)]
    public class PluginsConfigNode
    {
        [XmlArrayAttribute("plugins")]
        public plugin[] Plugins
        {
            get;
            set;
        }
    }
    [XmlRootAttribute("plugin", ElementName = "plugin")]
    public class plugin
    {
        [XmlAttribute("name")]
        public string Name
        {
            get;
            set;
        }
        [XmlAttribute("processorArchitecture")]
        public string ProcessorArchitecture
        {
            get;
            set;
        }
        [XmlAttribute("publicKeyToken")]
        public string PublicKeyToken
        {
            get;
            set;
        }
        [XmlAttribute("culture")]
        public string Culture
        {
            get;
            set;
        }
        [XmlAttribute("version")]
        public string Version
        {
            get;
            set;
        }
    }
}
