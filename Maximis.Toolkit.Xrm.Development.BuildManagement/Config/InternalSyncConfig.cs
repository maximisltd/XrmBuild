using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class InternalSyncConfig
    {
        [XmlAttribute]
        public int ExportPass { get; set; }

        [XmlArrayItem("UniqueName")]
        public List<string> ImportManaged { get; set; }

        [XmlAttribute]
        public int ImportPass { get; set; }

        [XmlArrayItem("UniqueName")]
        public List<string> ImportUnmanaged { get; set; }

        [XmlAttribute]
        public bool MergeUnmanaged { get; set; }

        [XmlAttribute]
        public bool OverwriteUnmanaged { get; set; }
    }
}