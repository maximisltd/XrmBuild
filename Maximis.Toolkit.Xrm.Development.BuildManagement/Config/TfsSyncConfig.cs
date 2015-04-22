using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class TfsSyncConfig
    {
        [XmlAttribute]
        public bool CheckInSolution { get; set; }

        [XmlArrayItem("AssemblyName")]
        public List<string> PluginAssemblies { get; set; }

        public string ReportsPath { get; set; }
    }
}