using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class ILMergeConfig
    {
        public string KeyFile { get; set; }

        [XmlArrayItem("AssemblyFileName")]
        public List<string> MergeAssemblies { get; set; }
    }
}