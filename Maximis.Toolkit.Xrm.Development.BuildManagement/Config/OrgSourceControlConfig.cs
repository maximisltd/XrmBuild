using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class OrgSourceControlConfig
    {
        [XmlArrayItem("AssemblyName")]
        public List<string> PluginAssemblies { get; set; }

        public string ReportsLocation { get; set; }
        public string SolutionLocation { get; set; }
    }
}